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
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace WakaTime
{
    static class SolutionSettings
    {
        public static bool IsSync { get; private set; } = false;

        static ConcurrentDictionary<string, string> settings = new ConcurrentDictionary<string, string>();
        public static bool TreatSolutionAsProject 
        {
            get 
            {
                if (!IsSync)
                    Read();
                bool value;
                if (settings.ContainsKey(nameof(TreatSolutionAsProject)) && bool.TryParse(settings[nameof(TreatSolutionAsProject)], out value))
                    return value;
                else
                    return true; //This property must default to true. Which is the current behaviour.
            }
            set
            {
                if (value != TreatSolutionAsProject)
                {
                    settings[nameof(TreatSolutionAsProject)] = value.ToString();
                    IsSync = false;
                }                
            }
        }

        public static void Read()
        {
            var slnRoot = Path.GetDirectoryName(WakaTimePackage.ObjDte.Solution.FileName);
            var cfgFile = $"{slnRoot}\\wakatime.sln.cfg";
            if (File.Exists(cfgFile))
            {
                using (StreamReader reader = new StreamReader(File.OpenRead(cfgFile)))
                {
                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        var iEqual = line.IndexOf('=');
                        if (line.StartsWith("#"))
                            continue;
                        else if (iEqual > 0)
                        {
                            var prop = line.Substring(0, iEqual);
                            var value = line.Substring(iEqual + 1);
                            settings.AddOrUpdate(prop, value, (existing, _new) => _new);
                        }
                    }
                }
            }
            else
                File.Create(cfgFile).Dispose();
            IsSync = true;
        }

        public static void Save()
        {
            var slnRoot = Path.GetDirectoryName(WakaTimePackage.ObjDte.Solution.FileName);
            var cfgFile = $"{slnRoot}\\wakatime.sln.cfg";
            if (File.Exists($"{slnRoot}\\wakatime.sln.cfg"))
            {
                using (StreamWriter writer = new StreamWriter(cfgFile, false))
                {
                    foreach (var key in settings.Keys)
                    {
                        writer.WriteLine($"{key}={settings[key]}");
                    }
                }
            }
            else
                File.Create(cfgFile).Dispose();
            IsSync = true;
        }
    }
}
