using System;
using System.Linq;
using System.ComponentModel.Design;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using WakaTime.Forms;
using Task = System.Threading.Tasks.Task;
using EnvDTE;
using Microsoft.VisualStudio.Shell.Interop;
using WakaTime.Shared.ExtensionUtils;
using WakaTime.Shared.ExtensionUtils.AsyncPackageHelpers;
using Configuration = WakaTime.Shared.ExtensionUtils.Configuration;
using IAsyncServiceProvider = Microsoft.VisualStudio.Shell.Interop.IAsyncServiceProvider;
using PackageAutoLoadFlags = WakaTime.Shared.ExtensionUtils.AsyncPackageHelpers.PackageAutoLoadFlags;

namespace WakaTime
{
    [Guid(GuidList.GuidWakaTimePkgString)]
    [AsyncPackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Shared.ExtensionUtils.AsyncPackageHelpers.ProvideAutoLoad("ADFC4E64-0397-11D1-9F4E-00A0C911004F", PackageAutoLoadFlags.BackgroundLoad)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    public sealed class WakaTimePackage : Package, IAsyncLoadablePackageInitialize
    {
        #region Fields
        private static GlobalSettingsForm _globalSettingsForm;
        private static SolutionsSettingsForm _solutionsSettingsForm;

        private DocumentEvents _docEvents;
        private WindowEvents _windowEvents;
        private SolutionEvents _solutionEvents;
        private DebuggerEvents _debuggerEvents;
        private BuildEvents _buildEvents;
        private TextEditorEvents _textEditorEvents;
        private IVsSolution _vsSolution;
        private SolutionEventsHandler _vsSolutionEvents;
        private uint _vsSolutionEventsCookie;

        private bool _isBuildRunning = false;
        private string _runningBuildOutput;
        private string _buildingProject;

        public static DTE ObjDte;

        private static string _solutionName = string.Empty;

        internal Shared.ExtensionUtils.WakaTime WakaTime;
        #endregion

        #region Startup/Cleanup        
        protected override void Initialize()
        {
            base.Initialize();
            ObjDte = (DTE)GetService(typeof(DTE));

            var configuration = new Configuration
            {
                EditorName = "visualstudio",
                PluginName = "visualstudio-wakatime",
                EditorVersion = ObjDte == null ? string.Empty : ObjDte.Version
            };
            WakaTime = new Shared.ExtensionUtils.WakaTime(this, configuration, new Logger());

            // Only perform initialization if async package framework not supported
            if (WakaTime.IsAsyncLoadSupported) return;

            // Try force initializing in background
            WakaTime.Logger.Debug("Initializing in background thread.");
            Task.Run(() =>
            {
                InitializeAsync();
            });
        }

        public IVsTask Initialize(IAsyncServiceProvider pServiceProvider, IProfferAsyncService pProfferService,
            IAsyncProgressCallback pProgressCallback)
        {
            if (!WakaTime.IsAsyncLoadSupported)
                throw new InvalidOperationException(
                    "Async Initialize method should not be called when async load is not supported.");

            return ThreadHelper.JoinableTaskFactory.RunAsync<object>(async () =>
            {
                WakaTime.Logger.Debug("Initializing async.");
                InitializeAsync();

                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                OnStartupComplete();

                return null;
            }).AsVsTask();
        }

        private void InitializeAsync()
        {
            try
            {
                // VisualStudio Object
                _vsSolution = GetService(typeof(SVsSolution)) as IVsSolution;
                _vsSolutionEvents = new SolutionEventsHandler();
                _docEvents = ObjDte.Events.DocumentEvents;
                _windowEvents = ObjDte.Events.WindowEvents;
                _solutionEvents = ObjDte.Events.SolutionEvents;
                _debuggerEvents = ObjDte.Events.DebuggerEvents;
                _buildEvents = ObjDte.Events.BuildEvents;
                _textEditorEvents = ObjDte.Events.TextEditorEvents;

                // Settings Form
                _globalSettingsForm = new GlobalSettingsForm(ref WakaTime);
                _globalSettingsForm.ConfigSaved += SettingsFormOnConfigSaved;
                _solutionsSettingsForm = new SolutionsSettingsForm();

                // Add our command handlers for menu (commands must exist in the .vsct file)
                if (GetService(typeof(IMenuCommandService)) is OleMenuCommandService mcs)
                {
                    // Create the command for the menu item.
                    var globalMenuCommandId = new CommandID(GuidList.GuidWakaTimeCmdSet, (int)PkgCmdIdList.UpdateWakaTimeSettings);
                    var solutionMenuCommandId = new CommandID(GuidList.GuidWakaTimeCmdSet, (int)PkgCmdIdList.UpdateWakaTimeSolutionSettings);
                    var globalMenuItem = new MenuCommand(GlobalMenuItemCallback, globalMenuCommandId);
                    var solutionMenuItem = new MenuCommand(SolutionMenuItemCallback, solutionMenuCommandId);
                    mcs.AddCommand(globalMenuItem);
                    mcs.AddCommand(solutionMenuItem);
                }

                // setup event handlers                
                _docEvents.DocumentOpened += DocEventsOnDocumentOpened;
                _docEvents.DocumentSaved += DocEventsOnDocumentSaved;
                _windowEvents.WindowActivated += WindowEventsOnWindowActivated;
                _solutionEvents.Opened += SolutionEventsOnOpened;
                _debuggerEvents.OnEnterRunMode += DebuggerEventsOnEnterRunMode;
                _debuggerEvents.OnEnterDesignMode += DebuggerEventsOnEnterDesignMode;
                _debuggerEvents.OnEnterBreakMode += DebuggerEventsOnEnterBreakMode;
                _buildEvents.OnBuildProjConfigBegin += BuildEventsOnBuildProjConfigBegin;
                _buildEvents.OnBuildProjConfigDone += BuildEventsOnBuildProjConfigDone;
                _textEditorEvents.LineChanged += TextEditorEventsLineChanged;

                _vsSolution.AdviseSolutionEvents(_vsSolutionEvents, out _vsSolutionEventsCookie);

                WakaTime.InitializeAsync();
            }
            catch (Exception ex)
            {
                WakaTime.Logger.Error("Error Initializing WakaTime", ex);
            }
        }
        #endregion

        #region Event Handlers

        private void DocEventsOnDocumentOpened(Document document)
        {
            try
            {
                var activity = ObjDte.Debugger.CurrentMode == dbgDebugMode.dbgBreakMode ? HeartbeatCategory.debugging : HeartbeatCategory.coding;
                WakaTime.HandleActivity(document.FullName, false, GetProjectName(document), activity); //eventType defaults to "file"

                if (_isBuildRunning)
                    WakaTime.HandleActivity(_runningBuildOutput, false, _buildingProject, HeartbeatCategory.building); //eventType defaults to "file"
            }
            catch (Exception ex)
            {
                WakaTime.Logger.Error("DocEventsOnDocumentOpened", ex);
            }
        }

        private void DocEventsOnDocumentSaved(Document document)
        {
            try
            {
                var activity = ObjDte.Debugger.CurrentMode == dbgDebugMode.dbgBreakMode ? HeartbeatCategory.debugging : HeartbeatCategory.coding;
                WakaTime.HandleActivity(document.FullName, true, GetProjectName(document), HeartbeatCategory.debugging); //eventType defaults to "file"

                if (_isBuildRunning)
                    WakaTime.HandleActivity(_runningBuildOutput, false, _buildingProject, HeartbeatCategory.building); //eventType defaults to "file"
            }
            catch (Exception ex)
            {
                WakaTime.Logger.Error("DocEventsOnDocumentSaved", ex);
            }
        }

        private void WindowEventsOnWindowActivated(Window gotFocus, Window lostFocus)
        {
            try
            {
                var activity = ObjDte.Debugger.CurrentMode == dbgDebugMode.dbgBreakMode ? HeartbeatCategory.debugging : HeartbeatCategory.coding;
                var document = ObjDte.ActiveWindow.Document;
                if (document != null)
                    WakaTime.HandleActivity(document.FullName, false, GetProjectName(document), activity); //eventType defaults to "file"
                if (_isBuildRunning)
                    WakaTime.HandleActivity(_runningBuildOutput, false, GetProjectName(), HeartbeatCategory.building); //eventType defaults to "file"
            }
            catch (Exception ex)
            {
                WakaTime.Logger.Error("WindowEventsOnWindowActivated", ex);
            }
        }

        private void SolutionEventsOnOpened()
        {
            try
            {
                _solutionName = ObjDte.Solution.FullName;
                SolutionSettings.Read();
            }
            catch (Exception ex)
            {
                WakaTime.Logger.Error("SolutionEventsOnOpened", ex);
            }
        }

        private void DebuggerEventsOnEnterRunMode(dbgEventReason Reason)
        {
            try
            {
                var fullOutputName = GetCurrentProjectOutputForCurrentConfiguration();
                WakaTime.HandleActivity(fullOutputName, false, GetProjectName(), HeartbeatCategory.debugging); //eventType defaults to "file"
            }
            catch (Exception ex)
            {
                WakaTime.Logger.Error("DebuggerEventsOnEnterRunMode", ex);
            }
        }

        private void DebuggerEventsOnEnterDesignMode(dbgEventReason Reason)
        {
            try
            {
                var fullOutputName = GetCurrentProjectOutputForCurrentConfiguration();
                WakaTime.HandleActivity(fullOutputName, false, GetProjectName(), HeartbeatCategory.debugging); //eventType defaults to "file"
            }
            catch (Exception ex)
            {
                WakaTime.Logger.Error("DebuggerEventsOnEnterDesignMode", ex);
            }
        }

        private void DebuggerEventsOnEnterBreakMode(dbgEventReason Reason, ref dbgExecutionAction ExecutionAction)
        {
            try
            {
                var fullOutputName = GetCurrentProjectOutputForCurrentConfiguration();
                WakaTime.HandleActivity(fullOutputName, false, GetProjectName(), HeartbeatCategory.debugging); //eventType defaults to "file"
            }
            catch (Exception ex)
            {
                WakaTime.Logger.Error("DebuggerEventsOnEnterBreakMode", ex);
            }
        }

        private void BuildEventsOnBuildProjConfigBegin(string Project, string ProjectConfig, string Platform, string SolutionConfig)
        {
            try
            {
                _isBuildRunning = true;
                _buildingProject = GetProjectName(Project);
                var fullOutputName = GetProjectOutputForConfiguration(Project, Platform, ProjectConfig);
                _runningBuildOutput = fullOutputName;
                WakaTime.HandleActivity(fullOutputName, false, GetProjectName(Project), HeartbeatCategory.building); //eventType defaults to "file"
            }
            catch (Exception ex)
            {
                WakaTime.Logger.Error("BuildEventsOnBuildProjConfigBegin", ex);
            }
        }

        private void BuildEventsOnBuildProjConfigDone(string Project, string ProjectConfig, string Platform, string SolutionConfig, bool Success)
        {
            try
            {
                _isBuildRunning = false;
                var fullOutputName = GetProjectOutputForConfiguration(Project, Platform, ProjectConfig);
                WakaTime.HandleActivity(fullOutputName, Success, GetProjectName(Project), HeartbeatCategory.building); //eventType defaults to "file"
            }
            catch (Exception ex)
            {
                WakaTime.Logger.Error("BuildEventsOnBuildProjConfigDone", ex);
            }
        }

        private void TextEditorEventsLineChanged(TextPoint StartPoint, TextPoint EndPoint, int Hint)
        {
            try
            {
                var document = StartPoint.Parent.Parent;
                if (document != null)
                {
                    var activity = ObjDte.Debugger.CurrentMode == dbgDebugMode.dbgBreakMode ? HeartbeatCategory.debugging : HeartbeatCategory.coding;
                    WakaTime.HandleActivity(document.FullName, false, GetProjectName(document), activity); //eventType defaults to "file"

                    if (_isBuildRunning)
                        WakaTime.HandleActivity(_runningBuildOutput, false, _buildingProject, HeartbeatCategory.building); //eventType defaults to "file"
                }
            }
            catch (Exception ex)
            {
                WakaTime.Logger.Error("TextEditorEventsLineChanged", ex);
            }
        }        

        private void OnStartupComplete()
        {
            // Prompt for api key if not already set
            if (string.IsNullOrEmpty(WakaTime.Config.ApiKey))
                PromptApiKey();
        }
        #endregion

        #region Methods

        private void SettingsFormOnConfigSaved(object sender, EventArgs eventArgs)
        {
            WakaTime.Config.Read();
        }

        private void GlobalMenuItemCallback(object sender, EventArgs e)
        {
            try
            {
                GlobalSettingsPopup();
            }
            catch (Exception ex)
            {
                WakaTime.Logger.Error("GlobalMenuItemCallback", ex);
            }
        }

        private void SolutionMenuItemCallback(object sender, EventArgs e)
        {
            try
            {
                SolutionsSettingsPopup();
            }
            catch (Exception ex)
            {
                WakaTime.Logger.Error("SolutionMenuItemCallback", ex);
            }
        }

        private void PromptApiKey()
        {
            WakaTime.Logger.Info("Please input your api key into the wakatime window.");
            var form = new ApiKeyForm(ref WakaTime);
            form.ShowDialog();
        }

        private static void GlobalSettingsPopup()
        {
            _globalSettingsForm.ShowDialog();
        }

        internal static void SolutionsSettingsPopup()
        {
            if (_solutionsSettingsForm.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                SolutionSettings.Save();
            SolutionSettings.Read();
        }

        private static string GetProjectName(Document doc = null)
        {
            if (SolutionSettings.TreatSolutionAsProject)
            {
                return GetSolutionName();
            }
            else
            {
                if (doc != null)
                {
                    var projectName = Path.GetFileNameWithoutExtension(doc.ProjectItem.ContainingProject.UniqueName);
                    return $"{GetSolutionName()}\\{projectName}";
                }
                else
                {
                    var projectName = Path.GetFileNameWithoutExtension(((Project)((object[])ObjDte.ActiveSolutionProjects)[0]).UniqueName);
                    return $"{GetSolutionName()}\\{projectName}";
                }
            }
        }

        private static string GetProjectName(string project)
        {
            if (SolutionSettings.TreatSolutionAsProject)
                return GetProjectName();
            else
            {
                var projectName = Path.GetFileNameWithoutExtension(project);
                return $"{GetSolutionName()}\\{projectName}";
            }
        }

        private static string GetSolutionName()
        {
            return !string.IsNullOrEmpty(_solutionName)
                    ? Path.GetFileNameWithoutExtension(_solutionName)
                    : ObjDte.Solution != null && !string.IsNullOrEmpty(ObjDte.Solution.FullName)
                        ? Path.GetFileNameWithoutExtension(ObjDte.Solution.FullName)
                        : string.Empty;
        }

        private string GetProjectOutputForConfiguration(string Project, string Platform, string ProjectConfig)
        {
            var project = ObjDte.Solution.Projects.Cast<Project>().Where(proj => proj.UniqueName == Project).FirstOrDefault();
            var config = project.ConfigurationManager.Cast<EnvDTE.Configuration>().Where(conf => conf.PlatformName == Platform && conf.ConfigurationName == ProjectConfig).FirstOrDefault();
            var outputPath = config.Properties.Item("OutputPath");
            var outputFileName = project.Properties.Item("OutputFileName");
            var projectPath = project.Properties.Item("FullPath");

            return $"{projectPath.Value}{outputPath.Value}{outputFileName.Value}";
        }

        private string GetCurrentProjectOutputForCurrentConfiguration()
        {
            var project = (Project)((object[])ObjDte.ActiveSolutionProjects)[0];
            var config = project.ConfigurationManager.ActiveConfiguration;
            var outputPath = config.Properties.Item("OutputPath");
            var outputFileName = project.Properties.Item("OutputFileName");
            var projectPath = project.Properties.Item("FullPath");

            return $"{projectPath.Value}{outputPath.Value}{outputFileName.Value}";
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            _docEvents.DocumentOpened -= DocEventsOnDocumentOpened;
            _docEvents.DocumentSaved -= DocEventsOnDocumentSaved;
            _windowEvents.WindowActivated -= WindowEventsOnWindowActivated;
            _solutionEvents.Opened -= SolutionEventsOnOpened;
            _debuggerEvents.OnEnterRunMode -= DebuggerEventsOnEnterRunMode;
            _debuggerEvents.OnEnterDesignMode -= DebuggerEventsOnEnterDesignMode;
            _debuggerEvents.OnEnterBreakMode -= DebuggerEventsOnEnterBreakMode;
            _buildEvents.OnBuildProjConfigBegin -= BuildEventsOnBuildProjConfigBegin;
            _buildEvents.OnBuildProjConfigDone -= BuildEventsOnBuildProjConfigDone;
            _textEditorEvents.LineChanged -= TextEditorEventsLineChanged;
            _vsSolution.UnadviseSolutionEvents(_vsSolutionEventsCookie);

            WakaTime.Dispose();
        }
        #endregion        
    }
}
