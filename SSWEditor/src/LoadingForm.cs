using System.IO;
using System.Windows.Forms;

namespace SSWEditor
{
    public partial class LoadingForm : Form
    {
        public LoadingForm()
        {
            InitializeComponent();
            textBoxHistory.Text = File.ReadAllText("history.txt");
            labelVersion.Text = MainForm.CurrVersion;
        }

        private void LoadingForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape) Close();
        }
    }
}
