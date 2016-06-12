using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using VDS.RDF.Storage;

namespace SSWEditor
{
    public partial class ConnectForm : Form
    {
        private int cnt = 0;
        public ConnectForm()
        {
            InitializeComponent();
        }

        private void LoadingForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape) Application.Exit();
        }

        private void button1_Click(object sender, System.EventArgs e)
        {
            Application.Exit();
        }

        private void timer1_Tick(object sender, System.EventArgs e)
        {
            cnt++;
            labelTime.Text = cnt.ToString();

            if (CheckFusekiConnection())
            {
                Close();
            }
        }
       
        private void ConnectForm_Load(object sender, EventArgs e)
        {
        }

        public static void ConnectFuseki()
        {
            try
            {
                var server = MainForm.config.FusekiServer;
                Fuseki.Start(MainForm.config.ShowFusekiConsole);
                MainForm.fuseki = new FusekiConnector(string.Format("http://{0}:{1}/ds/data", server, MainForm.config.FusekiPort));
            }
            catch (Exception ex)
            {
                MessageBox.Show("error during building connection to fuseki server. " + ex);
            }

            var connectForm = new ConnectForm();
            connectForm.ShowDialog();
        }

        public static bool CheckFusekiConnection()
        {
            try
            {
                MainForm.fuseki.ListGraphs();
            }
            catch
            {
                return false;
            }
            return true;
        }
    }
}
