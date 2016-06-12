using System;
using System.Windows.Forms;

namespace SSWEditor
{
    public partial class SingleForm : Form
    {
        public string Title;
        public string Label;
        public string Content;

        public SingleForm()
        {
            InitializeComponent();
        }

        private void New_Load(object sender, EventArgs e)
        {
            Text = Title;
            label1.Text = Label;
            textBox1.Text = Content;
        }

        private void buttonOk_Click(object sender, EventArgs e)
        {
            Content = textBox1.Text;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void NewForm_KeyDown(object sender, KeyEventArgs e)
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
    }
}
