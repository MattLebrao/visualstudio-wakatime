using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WakaTime.Forms
{
    public partial class SolutionsSettingsForm : Form
    {
        public SolutionsSettingsForm()
        {
            InitializeComponent();
        }

        private void SolutionsSettingsForm_Load(object sender, EventArgs e)
        {
            radioButtonSolutionAsOne.Checked = SolutionSettings.TreatSolutionAsProject;
            radioButtonIndividualProjects.Checked = !SolutionSettings.TreatSolutionAsProject;
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            SolutionSettings.TreatSolutionAsProject = radioButtonSolutionAsOne.Checked;
        }
    }
}
