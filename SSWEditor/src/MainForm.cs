using System;
using System.Collections.Generic;
using System.Deployment.Application;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Serialization;
using Microsoft.Win32;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Storage;
using VDS.RDF.Writing;

namespace SSWEditor
{
    /// <summary>
    ///     Reference
    ///     * https://bitbucket.org/dotnetrdf/dotnetrdf/wiki/Home
    /// </summary>
    public partial class MainForm : Form
    {
        public static String DocumentRoot = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) +
                                            @"\SSWEditor";

        public static Config config = new Config();
        public static FusekiConnector fuseki;
        public static string CurrVersion = "offline";

        public MainForm()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            UpdateApplication();
            closeGraphToolStripMenuItem.Enabled = false;

            try
            {
                if (!Directory.Exists(DocumentRoot)) Directory.CreateDirectory(DocumentRoot);
            }
            catch
            {
                MessageBox.Show("error during document root creation");
            }
            CheckJava();

            LoadConfig();

            ConnectForm.ConnectFuseki();


            UpdateListViewGraph();

            //NewMethod();
        }

        private static void CheckJava()
        {
            var installPath = GetJavaInstallationPath();
            var javaPath = Path.Combine(installPath, "bin\\Java.exe");
            if (!File.Exists(javaPath))
            {
                if (MessageBox.Show(string.Format("Java is not found. If you do not set JAVA_HOME environment value, you could see this message. Do you install java?")
                    , "Infomation", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    Process.Start("http://www.oracle.com/technetwork/java/javase/downloads/index.html");
                }
                Application.Exit();
            }
        }

        
        public static string GetJavaInstallationPath()
        {
            var environmentPath = Environment.GetEnvironmentVariable("JAVA_HOME");
            if (!string.IsNullOrEmpty(environmentPath))
            {
                return environmentPath;
            }

            const string javaKey = "SOFTWARE\\JavaSoft\\Java Runtime Environment\\";
            using (var rk = Registry.LocalMachine.OpenSubKey(javaKey))
            {
                if (rk != null)
                {
                    var currentVersion = rk.GetValue("CurrentVersion").ToString();
                    using (var key = rk.OpenSubKey(currentVersion))
                    {
                        if (key != null) return key.GetValue("JavaHome").ToString();
                    }
                }
            }
            return "";
        }

        public static void UpdateApplication(bool force = false)
        {
            try
            {
                CurrVersion = ApplicationDeployment.CurrentDeployment.CurrentVersion.ToString();
                var updateCheckInfo = ApplicationDeployment.CurrentDeployment.CheckForDetailedUpdate(false);
                if (!updateCheckInfo.UpdateAvailable) return;
                if (MessageBox.Show(
                    string.Format(
                        "There is a new version {0}. Do you want to update the application to the lastest version?",
                        updateCheckInfo.AvailableVersion)
                    , "Information", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
                ApplicationDeployment.CurrentDeployment.Update();
                MessageBox.Show("Updating is completed. Now, restart the application.");
                Application.Restart();
            }
            catch
            {
                if (force)
                {
                    MessageBox.Show(string.Format("There is no update available."));
                }
            }
        }

        public static void LoadConfig()
        {
            if (File.Exists(DocumentRoot + @"\config.xml"))
            {
                try
                {
                    var m = new XmlSerializer(config.GetType());
                    TextReader r = new StreamReader(DocumentRoot + @"\config.xml");
                    config = (Config) m.Deserialize(r);
                    r.Close();
                }
                catch
                {
                    if (
                        MessageBox.Show("Warning", "config.xml is invalid. Do you recreate the config.xml?",
                            MessageBoxButtons.OKCancel) == DialogResult.OK)
                    {
                        SaveConfig();
                    }
                }
            }
            else
            {
                var randomString = Config.GetRandomString(6);
                var form = new SingleForm {Title = "Set unique userid", Label = "userid", Content = randomString};

                if (form.ShowDialog() == DialogResult.OK)
                {
                    config.GlobalPrefix = string.Format("http://ah.withcat.net/{0}/", form.Content);
                }
                else
                {
                    config.GlobalPrefix = string.Format("http://ah.withcat.net/{0}/", randomString);
                }
                SaveConfig();
            }


            if (config.FusekiServer == null || config.FusekiServer == "" || config.PredicateList.Count == 0)
            {
                SaveConfig();
            }
        }

        public static void SaveConfig()
        {
            config.PredicateList.RemoveAll(ee => ee.Url.Trim() == "");
            if (!config.PredicateList.Any())
            {
                config.PredicateList.Add(new ConfigPredicate
                {
                    Usage = false,
                    Url = "http://eagle-i.org/ont/app/1.0/resource_description"
                });
                config.PredicateList.Add(new ConfigPredicate
                {
                    Usage = false,
                    Url = "http://eagle-i.org/ont/repo/1.0/hasWorkflowState"
                });
                config.PredicateList.Add(new ConfigPredicate
                {
                    Usage = false,
                    Url = "http://purl.obolibrary.org/obo/ERO_0000480"
                });
                config.PredicateList.Add(new ConfigPredicate
                {
                    Usage = false,
                    Url = "http://purl.org/dc/elements/1.1/creator"
                });
                config.PredicateList.Add(new ConfigPredicate
                {
                    Usage = false,
                    Url = "http://purl.org/dc/elements/1.1/description"
                });
                config.PredicateList.Add(new ConfigPredicate
                {
                    Usage = false,
                    Url = "http://purl.org/dc/elements/1.1/format"
                });
                config.PredicateList.Add(new ConfigPredicate
                {
                    Usage = false,
                    Url = "http://purl.org/dc/elements/1.1/title"
                });
                config.PredicateList.Add(new ConfigPredicate
                {
                    Usage = true,
                    Url = "http://purl.org/dc/terms/contributor"
                });
                config.PredicateList.Add(new ConfigPredicate {Usage = true, Url = "http://purl.org/dc/terms/created"});
                config.PredicateList.Add(new ConfigPredicate {Usage = true, Url = "http://purl.org/dc/terms/creator"});
                config.PredicateList.Add(new ConfigPredicate {Usage = true, Url = "http://purl.org/dc/terms/date"});
                config.PredicateList.Add(new ConfigPredicate
                {
                    Usage = true,
                    Url = "http://purl.org/dc/terms/description"
                });
                config.PredicateList.Add(new ConfigPredicate {Usage = true, Url = "http://purl.org/dc/terms/format"});
                config.PredicateList.Add(new ConfigPredicate {Usage = true, Url = "http://purl.org/dc/terms/hasFormat"});
                config.PredicateList.Add(new ConfigPredicate {Usage = true, Url = "http://purl.org/dc/terms/identifier"});
                config.PredicateList.Add(new ConfigPredicate {Usage = true, Url = "http://purl.org/dc/terms/isPartOf"});
                config.PredicateList.Add(new ConfigPredicate {Usage = true, Url = "http://purl.org/dc/terms/license"});
                config.PredicateList.Add(new ConfigPredicate {Usage = true, Url = "http://purl.org/dc/terms/modified"});
                config.PredicateList.Add(new ConfigPredicate {Usage = true, Url = "http://purl.org/dc/terms/publisher"});
                config.PredicateList.Add(new ConfigPredicate {Usage = true, Url = "http://purl.org/dc/terms/source"});
                config.PredicateList.Add(new ConfigPredicate {Usage = true, Url = "http://purl.org/dc/terms/subject"});
                config.PredicateList.Add(new ConfigPredicate {Usage = true, Url = "http://purl.org/dc/terms/title"});
                config.PredicateList.Add(new ConfigPredicate
                {
                    Usage = false,
                    Url = "http://www.obofoundry.org/ro/ro.owl#located_in"
                });
                config.PredicateList.Add(new ConfigPredicate
                {
                    Usage = false,
                    Url = "http://www.w3.org/1999/02/22-rdf-syntax-ns#_1"
                });
                config.PredicateList.Add(new ConfigPredicate
                {
                    Usage = false,
                    Url = "http://www.w3.org/1999/02/22-rdf-syntax-ns#_2"
                });
                config.PredicateList.Add(new ConfigPredicate
                {
                    Usage = false,
                    Url = "http://www.w3.org/1999/02/22-rdf-syntax-ns#_3"
                });
                config.PredicateList.Add(new ConfigPredicate
                {
                    Usage = false,
                    Url = "http://www.w3.org/1999/02/22-rdf-syntax-ns#first"
                });
                config.PredicateList.Add(new ConfigPredicate
                {
                    Usage = false,
                    Url = "http://www.w3.org/1999/02/22-rdf-syntax-ns#rest"
                });
                config.PredicateList.Add(new ConfigPredicate
                {
                    Usage = true,
                    Url = "http://www.w3.org/1999/02/22-rdf-syntax-ns#type"
                });
                config.PredicateList.Add(new ConfigPredicate
                {
                    Usage = true,
                    Url = "http://www.w3.org/1999/02/22-rdf-syntax-ns#value"
                });
                config.PredicateList.Add(new ConfigPredicate
                {
                    Usage = true,
                    Url = "http://www.w3.org/2000/01/rdf-schema#comment"
                });
                config.PredicateList.Add(new ConfigPredicate
                {
                    Usage = true,
                    Url = "http://www.w3.org/2000/01/rdf-schema#domain"
                });
                config.PredicateList.Add(new ConfigPredicate
                {
                    Usage = true,
                    Url = "http://www.w3.org/2000/01/rdf-schema#isDefinedBy"
                });
                config.PredicateList.Add(new ConfigPredicate
                {
                    Usage = true,
                    Url = "http://www.w3.org/2000/01/rdf-schema#label"
                });
                config.PredicateList.Add(new ConfigPredicate
                {
                    Usage = true,
                    Url = "http://www.w3.org/2000/01/rdf-schema#range"
                });
                config.PredicateList.Add(new ConfigPredicate
                {
                    Usage = true,
                    Url = "http://www.w3.org/2000/01/rdf-schema#seeAlso"
                });
                config.PredicateList.Add(new ConfigPredicate
                {
                    Usage = true,
                    Url = "http://www.w3.org/2000/01/rdf-schema#subClassOf"
                });
                config.PredicateList.Add(new ConfigPredicate
                {
                    Usage = true,
                    Url = "http://www.w3.org/2000/01/rdf-schema#subPropertyOf"
                });
                config.PredicateList.Add(new ConfigPredicate
                {
                    Usage = true,
                    Url = "http://www.w3.org/2002/07/owl#imports"
                });
                config.PredicateList.Add(new ConfigPredicate
                {
                    Usage = true,
                    Url = "http://www.w3.org/2002/07/owl#sameAs"
                });
                config.PredicateList.Add(new ConfigPredicate
                {
                    Usage = false,
                    Url = "http://www.w3.org/2003/01/geo/wgs84_pos#lat"
                });
                config.PredicateList.Add(new ConfigPredicate
                {
                    Usage = false,
                    Url = "http://www.w3.org/2003/01/geo/wgs84_pos#long"
                });
                config.PredicateList.Add(new ConfigPredicate
                {
                    Usage = true,
                    Url = "http://www.w3.org/2004/02/skos/core#altLabel"
                });
                config.PredicateList.Add(new ConfigPredicate
                {
                    Usage = true,
                    Url = "http://www.w3.org/2004/02/skos/core#prefLabel"
                });
                config.PredicateList.Add(new ConfigPredicate {Usage = true, Url = "http://xmlns.com/foaf/0.1/homepage"});
                config.PredicateList.Add(new ConfigPredicate
                {
                    Usage = true,
                    Url = "http://xmlns.com/foaf/0.1/isPrimaryTopicOf"
                });
                config.PredicateList.Add(new ConfigPredicate {Usage = true, Url = "http://xmlns.com/foaf/0.1/name"});
                config.PredicateList.Add(new ConfigPredicate {Usage = true, Url = "http://xmlns.com/foaf/0.1/page"});
                config.PredicateList.Add(new ConfigPredicate
                {
                    Usage = true,
                    Url = "http://xmlns.com/foaf/0.1/primaryTopic"
                });
                config.PredicateList.Add(new ConfigPredicate {Usage = true, Url = "http://xmlns.com/foaf/0.1/topic"});
            }
            if (config.FusekiServer == null || config.FusekiServer == "")
            {
                config.FusekiServer = "localhost";
                SaveConfig();
            }

            try
            {
                var xs = new XmlSerializer(config.GetType());
                var sw = new StreamWriter(DocumentRoot + @"\config.xml", false);
                xs.Serialize(sw, config);
                sw.Close();
            }
            catch
            {
                MessageBox.Show("error during config.xml creation");
            }
        }

       
        private void UpdateListViewGraph()
        {
            try
            {
                listViewGraph.Items.Clear();

                var graphs = new List<string>();
                graphs.Add("default");
                graphs.AddRange(fuseki.ListGraphs().Select(uri => uri.ToString()).OrderBy(ee=>ee));
                foreach (var uri in graphs)
                {
                    var graphBase64 = GraphEditor.Base64Encode(uri);
                    var filePath = string.Format(@"{0}\{1}.txt", DocumentRoot, graphBase64);
                    var updateDate = "";
                    if (File.Exists(filePath))
                    {
                        var info = new FileInfo(filePath);
                        updateDate = info.LastWriteTime.ToShortDateString();
                    }

                    var item = new ListViewItem(new[] {uri, updateDate}) {Tag = uri};
                    listViewGraph.Items.Add(item);
                }

                UpdateListViewGraphColor("");
            }
            catch (Exception ex)
            {
                MessageBox.Show("error during UpdateListViewGraph. " + ex);
            }
        }

        private string selectedUri = "";
        private void UpdateListViewGraphColor(string selected)
        {
            if (selected == "") selected = selectedUri;
            foreach (ListViewItem item in listViewGraph.Items)
            {
                if (selected == (string)item.Tag) item.BackColor = Color.MediumTurquoise;
                else item.BackColor = Color.White;
            }
            selectedUri = selected;
        }

        private void listViewGraph_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listViewGraph.SelectedItems.Count != 1) return;
            var uri = (string) listViewGraph.SelectedItems[0].Tag;
            UpdateListViewGraphColor(uri);

            var tabpage = GetTabPage(uri);
            if (tabpage == null)
            {
                tabpage = new TabPage(uri);
                var graphEditor = new GraphEditor();
                graphEditor.SetGraph(uri);
                graphEditor.Dock = DockStyle.Fill;
                tabpage.Controls.Add(graphEditor);
                tabpage.Tag = graphEditor;

                tabControlGraph.TabPages.Add(tabpage);

                closeGraphToolStripMenuItem.Enabled = true;
            }
            tabControlGraph.SelectedTab = tabpage;
        }

        private TabPage GetTabPage(string uri)
        {
            TabPage tabpage = null;
            foreach (TabPage page in tabControlGraph.TabPages)
            {
                if (page.Text == uri) tabpage = page;
            }
            return tabpage;
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            SSWEditor.Fuseki.Stop();
        }

        /// <summary>
        ///     make new graph
        ///     https://bitbucket.org/dotnetrdf/dotnetrdf/wiki/UserGuide/Triple%20Store%20Integration
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void newGraphToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                var form = new SingleForm
                {
                    Title = "New Graph",
                    Label = "URI",
                    Content = config.GlobalPrefix + Config.GetRandomString(6)
                };

                if (form.ShowDialog() != DialogResult.OK) return;

                var uri = form.Content;
                var g = new Graph {BaseUri = new Uri(uri)};
                SetNewGraphTriple(g, uri);
                fuseki.SaveGraph(g);

                UpdateListViewGraph();
            }
            catch (Exception ex)
            {
                MessageBox.Show("error during creating new graph. " + ex);
            }
        }

        private void SetNewGraphTriple(Graph g, string uri)
        {
            var label = uri.Split(new[] {'/', '#'}).Last();
            var nt = string.Format("<{0}> <{1}> \"{2}\"."
                , uri
                , "http://www.w3.org/2000/01/rdf-schema#label"
                , label);
            StringParser.Parse(g, nt);
        }

        private void preferenceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var form = new Preference();
            form.ShowDialog();
        }

        private void closeGraphToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (tabControlGraph.TabPages.Count == 0) return;

            var graphEditor = (GraphEditor) tabControlGraph.SelectedTab.Tag;
            if (graphEditor.RequestSave())
            {
                if (
                    MessageBox.Show(string.Format("save graph before closing graph {0}", graphEditor.GraphUri),
                        "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    graphEditor.SaveGraph();
                }
            }

            var removeIdx = tabControlGraph.SelectedIndex;
            tabControlGraph.TabPages.Remove(tabControlGraph.SelectedTab);
            if (removeIdx == tabControlGraph.TabPages.Count)
            {
                removeIdx--;
            }
            if (removeIdx >= 0)
            {
                tabControlGraph.SelectedIndex = removeIdx;
            }
            if (tabControlGraph.TabPages.Count == 0)
            {
                closeGraphToolStripMenuItem.Enabled = false;
            }
        }

        private void closeAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CloseAllTabs();
        }

        private void CloseAllTabs()
        {
            if (tabControlGraph.TabPages.Count == 0) return;
            foreach (TabPage page in tabControlGraph.TabPages)
            {
                var graphEditor = (GraphEditor) page.Tag;
                if (graphEditor.RequestSave())
                {
                    if (
                        MessageBox.Show(string.Format("save graph before closing graph {0}", graphEditor.GraphUri),
                            "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        graphEditor.SaveGraph();
                    }
                }
            }
            tabControlGraph.TabPages.Clear();
        }

        private void refreshToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UpdateListViewGraph();
        }

        private void updateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UpdateApplication(true);
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var form = new LoadingForm();
            form.ShowDialog();
        }

        private void restartToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Restart();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CloseAllTabs();
            Application.Exit();
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (tabControlGraph.TabPages.Count == 0) return;
            var graphEditor = (GraphEditor) tabControlGraph.SelectedTab.Tag;
            graphEditor.SaveGraph();
            UpdateListViewGraph();
        }

        private void saveAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (tabControlGraph.TabPages.Count == 0) return;
            foreach (TabPage page in tabControlGraph.TabPages)
            {
                var graphEditor = (GraphEditor) page.Tag;
                graphEditor.SaveGraph();
            }
            UpdateListViewGraph();
        }

        private void fromFileToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            if (listViewGraph.SelectedItems.Count != 1)
            {
                MessageBox.Show("no graph is selected");
                return;
            }
            var graphUri = (string) listViewGraph.SelectedItems[0].Tag;
            var tabPage = GetTabPage(graphUri);
            if (tabPage != null)
            {
                var graphEditor = (GraphEditor) tabPage.Tag;
                if (graphEditor.RequestSave())
                {
                    if (
                        MessageBox.Show(string.Format("save graph before closing graph {0}", graphEditor.GraphUri),
                            "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        graphEditor.SaveGraph();
                    }
                }
                tabControlGraph.TabPages.Remove(tabPage);
            }

            var dialogResult = openFileDialog1.ShowDialog();
            if (dialogResult != DialogResult.OK) return;

            var path = openFileDialog1.FileName;
            var extension = Path.GetExtension(path);
            if (extension == null) return;
            var ext = extension.ToLower();

            var ng = new Graph();
            try
            {
                switch (ext)
                {
                    case ".ttl":
                        new TurtleParser().Load(ng, path);
                        break;
                    case ".nt":
                        new NTriplesParser().Load(ng, path);
                        break;
                    case ".rdf":
                        FileLoader.Load(ng, path);
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Invalid format. " + ex);
                return;
            }

            var g = new Graph();
            fuseki.LoadGraph(g, graphUri);
            var cnt = 0;
            foreach (var t in ng.Triples)
            {
                if (g.Assert(t)) cnt++;
            }
            fuseki.SaveGraph(g);
            MessageBox.Show(string.Format("successfully load {0} triples", cnt));
        }

        private void fromURIToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            if (listViewGraph.SelectedItems.Count != 1)
            {
                MessageBox.Show("no graph is selected");
                return;
            }
            var graphUri = (string) listViewGraph.SelectedItems[0].Tag;
            var tabPage = GetTabPage(graphUri);
            if (tabPage != null)
            {
                var graphEditor = (GraphEditor) tabPage.Tag;
                if (graphEditor.RequestSave())
                {
                    if (
                        MessageBox.Show(string.Format("save graph before closing graph {0}", graphEditor.GraphUri),
                            "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        graphEditor.SaveGraph();
                    }
                }
                tabControlGraph.TabPages.Remove(tabPage);
            }

            var form = new SingleForm {Title = "Set URI", Label = "URI"};
            if (form.ShowDialog() != DialogResult.OK) return;

            var ng = new Graph();
            try
            {
                UriLoader.Load(ng, new Uri(form.Content));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Invalid format. " + ex);
                return;
            }

            var g = new Graph();
            fuseki.LoadGraph(g, graphUri);
            var cnt = ng.Triples.Count(g.Assert);
            fuseki.SaveGraph(g);
            MessageBox.Show(string.Format("successfully insert {0} triples", cnt));
        }

        private void exportToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (listViewGraph.SelectedItems.Count != 1)
            {
                MessageBox.Show("no graph is selected");
                return;
            }
            var graphUri = (string) listViewGraph.SelectedItems[0].Tag;

            var dialogResult = saveFileDialog1.ShowDialog();
            if (dialogResult != DialogResult.OK) return;

            var path = saveFileDialog1.FileName;
            var extension = Path.GetExtension(path);
            if (extension == null) return;
            var ext = extension.ToLower();

            var g = new Graph();
            fuseki.LoadGraph(g, graphUri);
            try
            {
                switch (ext)
                {
                    case ".ttl":
                        new CompressingTurtleWriter().Save(g, path);
                        break;
                    case ".nt":
                        new NTriplesWriter().Save(g, path);
                        break;
                    case ".rdf":
                        new RdfXmlWriter().Save(g, path);
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Invalid format. " + ex);
                return;
            }
            MessageBox.Show(string.Format("successfully export {0} triples", g.Triples.Count));
        }

        private void truncateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listViewGraph.SelectedItems.Count != 1)
            {
                MessageBox.Show("no graph is selected");
                return;
            }
            var graphUri = (string) listViewGraph.SelectedItems[0].Tag;
            if (
                MessageBox.Show(string.Format("are you want to truncate {0}", graphUri), "Warning",
                    MessageBoxButtons.YesNo) != DialogResult.Yes) return;

            var tabPage = GetTabPage(graphUri);
            if (tabPage != null)
            {
                tabControlGraph.TabPages.Remove(tabPage);
            }

            var loadUri = graphUri;
            if (graphUri == "default") loadUri = "";

            var g = new Graph();
            fuseki.LoadGraph(g, loadUri);
            g.Clear();
            if (graphUri != "default") SetNewGraphTriple(g, graphUri);
            fuseki.SaveGraph(g);

            var graphBase64 = GraphEditor.Base64Encode(graphUri);
            var filePath = string.Format(@"{0}\{1}.txt", DocumentRoot, graphBase64);
            File.WriteAllText(filePath, "");
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listViewGraph.SelectedItems.Count != 1)
            {
                MessageBox.Show("no graph is selected");
                return;
            }

            var graphUri = (string) listViewGraph.SelectedItems[0].Tag;
            if (graphUri == "default")
            {
                MessageBox.Show("default(unnamed) graph cannot delete");
                return;
            }
            if (
                MessageBox.Show(string.Format("are you want to delete {0}", graphUri), "Warning",
                    MessageBoxButtons.YesNo) != DialogResult.Yes) return;

            var tabPage = GetTabPage(graphUri);
            if (tabPage != null)
            {
                tabControlGraph.TabPages.Remove(tabPage);
            }

            fuseki.DeleteGraph(graphUri);
            UpdateListViewGraph();

            TabPage tabpage = null;
            foreach (TabPage page in tabControlGraph.TabPages)
            {
                if (page.Text == graphUri) tabpage = page;
            }

            if (tabpage != null)
            {
                tabControlGraph.TabPages.Remove(tabpage);
                if (tabControlGraph.TabPages.Count == 0)
                {
                    closeGraphToolStripMenuItem.Enabled = false;
                }
            }
        }

        private void duplicateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listViewGraph.SelectedItems.Count != 1)
            {
                MessageBox.Show("no graph is selected");
                return;
            }
            var graphUri = (string) listViewGraph.SelectedItems[0].Tag;

            var g = new Graph();
            fuseki.LoadGraph(g, graphUri);

            var form = new SingleForm {Title = "Set New Graph URI", Label = "URI", Content = graphUri};
            if (form.ShowDialog() != DialogResult.OK) return;

            var srcUri = graphUri;
            var srcGraphBase64 = GraphEditor.Base64Encode(srcUri);
            var dstUri = form.Content;
            var dstGraphLabel = dstUri.Split(new[] {'/', '#'}).Last();
            var dstGraphBase64 = GraphEditor.Base64Encode(dstUri);

            var newg = new Graph {BaseUri = new Uri(dstUri)};

            var nLabel = newg.CreateUriNode(UriFactory.Create("http://www.w3.org/2000/01/rdf-schema#label"));
            newg.Assert(new Triple(newg.CreateUriNode(UriFactory.Create(dstUri)), nLabel,
                newg.CreateLiteralNode(dstGraphLabel)));

            var cnt = 0;
            foreach (var triple in g.Triples)
            {
                INode news = newg.CreateUriNode(UriFactory.Create(triple.Subject.ToString().Replace(srcUri, dstUri)));
                INode newp = newg.CreateUriNode(UriFactory.Create(triple.Predicate.ToString().Replace(srcUri, dstUri)));
                INode newo = null;
                if (triple.Object.NodeType == NodeType.Uri)
                    newo = newg.CreateUriNode(UriFactory.Create(triple.Object.ToString().Replace(srcUri, dstUri)));
                else if (triple.Object.NodeType == NodeType.Literal)
                    newo = newg.CreateLiteralNode(triple.Object.ToString().Replace(srcUri, dstUri));
                var asserted = newg.Assert(new Triple(news, newp, newo));
                if (asserted) cnt++;
            }

            var srcPath = string.Format(@"{0}\{1}.txt", DocumentRoot, srcGraphBase64);
            var dstPath = string.Format(@"{0}\{1}.txt", DocumentRoot, dstGraphBase64);
            File.Copy(srcPath, dstPath);

            fuseki.SaveGraph(newg);

            UpdateListViewGraph();

            MessageBox.Show(string.Format("total {0} triples cloned", cnt));
        }

        public static string GetJson(string url)
        {
            var request = (HttpWebRequest) WebRequest.Create(url);
            try
            {
                var response = request.GetResponse();
                using (var responseStream = response.GetResponseStream())
                {
                    if (responseStream != null)
                    {
                        var reader = new StreamReader(responseStream, Encoding.UTF8);
                        return reader.ReadToEnd();
                    }
                }
            }
            catch (WebException ex)
            {
                var errorResponse = ex.Response;
                using (var responseStream = errorResponse.GetResponseStream())
                {
                    if (responseStream != null)
                    {
                        var reader = new StreamReader(responseStream, Encoding.GetEncoding("utf-8"));
                        return reader.ReadToEnd();
                    }
                }
                throw;
            }
            return "";
        }

        private void tabControlGraph_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControlGraph.SelectedTab != null) UpdateListViewGraphColor(tabControlGraph.SelectedTab.Text);
        }
    }
}