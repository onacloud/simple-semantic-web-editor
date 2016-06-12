using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace SSWEditor
{
    public partial class Preference : Form
    {
        public Preference()
        {
            InitializeComponent();
        }

        private void Preference_Load(object sender, EventArgs e)
        {
            try
            {
                textBoxGraphPrefix.Text = MainForm.config.GlobalPrefix;
                numericUpDownFusekiPort.Minimum = 1;
                numericUpDownFusekiPort.Maximum = 65535;
                numericUpDownFusekiPort.Value = MainForm.config.FusekiPort;
                checkBoxShowFusekiConsole.Checked = MainForm.config.ShowFusekiConsole;

                fontDialog1.Font = MainForm.config.GetEditorFont();
                textBoxFont.Text = fontDialog1.Font.Name;
                textBoxFont.Font = fontDialog1.Font;

                listViewPredicate.Items.Clear();
                foreach (var predicate in MainForm.config.PredicateList)
                {
                    var item = new ListViewItem(predicate.Url) {Checked = predicate.Usage};
                    listViewPredicate.Items.Add(item); 
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("error during load font config. " + ex);
            }
        }

        private void buttonOk_Click(object sender, EventArgs e)
        {
            MainForm.config.GlobalPrefix = textBoxGraphPrefix.Text;
            MainForm.config.FusekiServer = textBoxFusekiServer.Text;
            MainForm.config.FusekiPort = (int)numericUpDownFusekiPort.Value;
            MainForm.config.ShowFusekiConsole = checkBoxShowFusekiConsole.Checked;
            MainForm.config.SetEditorFont(fontDialog1.Font);

            ConnectForm.ConnectFuseki();

            MainForm.config.PredicateList.Clear();
            foreach (ListViewItem item in listViewPredicate.Items)
            {
                MainForm.config.PredicateList.Add(new ConfigPredicate { Url = item.Text, Usage = item.Checked });
            }

            MainForm.SaveConfig();
            DialogResult = DialogResult.OK;
            Close();
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void Preference_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                Close();
            }
            else if (e.KeyCode == Keys.Enter)
            {
                buttonOk_Click(null, null);
            }

        }

        private void buttonFont_Click(object sender, EventArgs e)
        {
            var result = fontDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                textBoxFont.Text = fontDialog1.Font.Name;
                textBoxFont.Font = fontDialog1.Font;
            }
        }

        private void buttonFusekiRestart_Click_1(object sender, EventArgs e)
        {
            try
            {
                Fuseki.Start(checkBoxShowFusekiConsole.Checked);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                return;
            }
            if (!checkBoxShowFusekiConsole.Checked)
            {
                MessageBox.Show("restarted");
            }
        }

        private void insertToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var form = new SingleForm {Title = "New Predicate", Label = "URL", Content = ""};

            if (form.ShowDialog() != DialogResult.OK) return;

            var item = new ListViewItem(form.Content) {Checked = true};
            listViewPredicate.Items.Add(item);
        }

        private void editToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var item = listViewPredicate.SelectedItems[0];

            var form = new SingleForm {Title = "New Graph", Label = "URI", Content = item.Text};

            if (form.ShowDialog() != DialogResult.OK) return;

            item.Text = form.Content;
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listViewPredicate.SelectedItems)
            {
                listViewPredicate.Items.Remove(item);
            }
        }

        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {
            if (listViewPredicate.SelectedItems.Count == 0)
            {
                editToolStripMenuItem.Enabled = false;
                deleteToolStripMenuItem.Enabled = false;
            }
            else 
            {
                editToolStripMenuItem.Enabled = true;
                deleteToolStripMenuItem.Enabled = true;
            }
        }

        private void listViewPredicate_DoubleClick(object sender, EventArgs e)
        {
            editToolStripMenuItem_Click(null, null);
        }
    }
}
