using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using FastColoredTextBoxNS;
using Newtonsoft.Json;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Query;
using VDS.RDF.Writing;
using StringWriter = VDS.RDF.Writing.StringWriter;

namespace SSWEditor
{
    /// <summary>
    ///     Reference
    ///     http://www.codeproject.com/Articles/161871/Fast-Colored-TextBox-for-syntax-highlighting
    /// </summary>
    public partial class GraphEditor : UserControl
    {
        private readonly TextStyle blueStyle = new TextStyle(Brushes.Blue, null, FontStyle.Underline);
        private readonly AutocompleteMenu popupMenu;
        private readonly Preview preview = new Preview();
        public Graph G = new Graph();
        public string GraphBase64;
        public string GraphLabel;
        public string GraphUri;

        private EditingContent editingContent = EditingContent.None;

        public GraphEditor()
        {
            InitializeComponent();

            try
            {
                textBoxTextEditor.Font = MainForm.config.GetEditorFont();
                textBoxTurtleEditor.Font = MainForm.config.GetEditorFont();
                textBoxQuery.Font = MainForm.config.GetEditorFont();
            }
            catch (Exception ex)
            {
            }

            popupMenu = new AutocompleteMenu(textBoxTextEditor);
            popupMenu.ForeColor = Color.White;
            popupMenu.BackColor = Color.Gray;
            popupMenu.SelectedColor = Color.Purple;
            popupMenu.SearchPattern = @"[\#\`\w]";
            popupMenu.MinFragmentLength = 2;
            popupMenu.Items.MaximumSize = new Size(400, 400);
            popupMenu.Items.Width = 400;
            popupMenu.Items.SetAutocompleteItems(new DynamicCollection(popupMenu, textBoxTextEditor, this));
        }

        public void SetGraph(string uri)
        {
            string loadUri;
            if (uri == "default")
            {
                loadUri = "";
                GraphUri = MainForm.config.GlobalPrefix + "default";
            }
            else
            {
                loadUri = uri;
                GraphUri = uri;
            }
            GraphLabel = GraphUri.Split(new[] {'/', '#'}).Last();

            MainForm.fuseki.LoadGraph(G, loadUri);
            GraphBase64 = Base64Encode(uri);
            InitNamespace();

            editingContent = EditingContent.Updating;
            ShowTextEditor();
            ShowTurtleEditor();
            ShowTableEditor();
            editingContent = EditingContent.None;
            UpdateStatistics();

            var args = new Dictionary<string, string>();
            args["name"] = "SSWEditor";
            args["abbreviation"] = "sswe";
            args["description"] = "Endpoint of the Simple Semantic Web Editor.";
            args["endpointURI"] = "http://localhost:3030/ds";
            args["dontAppendSPARQL"] = "false";
            args["defaultGraphURI"] = loadUri;
            args["isVirtuoso"] = "false";
            args["useProxy"] = "false";
            args["method"] = "POST";
            args["autocompleteLanguage"] = "en";
            args["autocompleteURIs"] = "http://www.w3.org/2000/01/rdf-schema#label";
            args["ignoredProperties"] = "";
            args["abstractURIs"] = "";
            args["imageURIs"] = "";
            args["linkURIs"] = "";
            args["maxRelationLegth"] = "0";

            List<string> encodedArgs = args.Select(kv => kv.Key + "=" + Base64Encode(kv.Value)).ToList();
            string relFinderUrl = "http://localhost:3030/RelFinder.swf?" + string.Join("&", encodedArgs);
            webBrowserRelfinder.Navigate(relFinderUrl);
            textBoxGraphUri.Text = GraphUri;
        }

        public static string Base64Encode(string plainText)
        {
            byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(plainTextBytes);
        }

        private void ShowTextEditor()
        {
            string filePath = string.Format(@"{0}\{1}.txt", MainForm.DocumentRoot, GraphBase64);
            if (File.Exists(filePath))
            {
                textBoxTextEditor.Text = File.ReadAllText(filePath);
            }
        }

        private void ShowTurtleEditor()
        {
            var w = new Notation3Writer();
            string data = StringWriter.Write(G, w);
            textBoxTurtleEditor.Text = data;
        }

        private void ShowTableEditor()
        {
            dataGridTableEditor.Rows.Clear();
            foreach (Triple spo in G.Triples)
            {
                dataGridTableEditor.Rows.Add(new[]
                {spo.Subject.ToString(), spo.Predicate.ToString(), spo.Object.ToString()});
            }

            for (int i = 0; i < dataGridTableEditor.RowCount; i++)
            {
                dataGridTableEditor.Rows[i].HeaderCell.Value = String.Format("{0}", i + 1);
            }
        }

        private void UpdateStatistics()
        {
            var staList = new Dictionary<string, string>();
            staList["Triples"] = G.Triples.Count.ToString("#,#");
            staList["Distinct Subjects"] = G.Triples.SubjectNodes.Count().ToString("#,#");
            staList["Distinct Predicates"] = G.Triples.PredicateNodes.Count().ToString("#,#");
            staList["Distinct Objects"] = G.Triples.ObjectNodes.Count().ToString("#,#");
            staList["Top10 Subjects"] = Environment.NewLine + "\t" +
                                        string.Join(Environment.NewLine + "\t",
                                            G.Triples.Select(e => e.Subject.ToString())
                                                .GroupBy(e => e)
                                                .OrderByDescending(e => e.Count())
                                                .Take(10)
                                                .Select(kv => kv.Key + ":" + kv.Count()));
            staList["Top10 Predicates"] = Environment.NewLine + "\t" +
                                          string.Join(Environment.NewLine + "\t",
                                              G.Triples.Select(e => e.Predicate.ToString())
                                                  .GroupBy(e => e)
                                                  .OrderByDescending(e => e.Count())
                                                  .Take(10)
                                                  .Select(kv => kv.Key + ":" + kv.Count()));
            staList["Top10 Objects"] = Environment.NewLine + "\t" +
                                       string.Join(Environment.NewLine + "\t",
                                           G.Triples.Select(e => e.Object.ToString())
                                               .GroupBy(e => e)
                                               .OrderByDescending(e => e.Count())
                                               .Take(10)
                                               .Select(kv => kv.Key + ":" + kv.Count()));

            textBoxStatistics.Text = string.Join(Environment.NewLine, staList.Select(kv => kv.Key + "\t" + kv.Value));
        }

        private void ReportMsg(Exception ex)
        {
            textBoxMsg.Text = ex.ToString();
        }

        private void ReportMsg(string ex)
        {
            textBoxMsg.Text = ex;
        }

        public bool RequestSave()
        {
            if (editingContent == EditingContent.None) return false;
            return true;
        }

        private void UpdateEditor()
        {
            if (editingContent == EditingContent.Updating) return;
            if (editingContent == EditingContent.None) return;

            if (editingContent == EditingContent.Text)
            {
                UpdateTextEditor();
                editingContent = EditingContent.Updating;
                ShowTurtleEditor();
                ShowTableEditor();
                UpdateStatistics();
                editingContent = EditingContent.None;
            }
            else if (editingContent == EditingContent.Turtle)
            {
                UpdateTupleEditor();
                editingContent = EditingContent.Updating;
                ShowTextEditor();
                ShowTableEditor();
                UpdateStatistics();
                editingContent = EditingContent.None;
            }
            else if (editingContent == EditingContent.Table)
            {
                UpdateTableEditor();
                editingContent = EditingContent.Updating;
                ShowTextEditor();
                ShowTurtleEditor();
                UpdateStatistics();
                editingContent = EditingContent.None;
            }
        }


        private void UpdateTextEditor()
        {
            Uri tryUri;
            try
            {
                InitGraph();

                var nLabel = G.CreateUriNode(UriFactory.Create("http://www.w3.org/2000/01/rdf-schema#label"));
                G.Assert(new Triple(G.CreateUriNode(UriFactory.Create(GraphUri)), nLabel,
                    G.CreateLiteralNode(GraphLabel)));

                var lines =
                    textBoxTextEditor.Text.Replace("\t", new string(' ', 4))
                        .Replace("\r", "")
                        .Split(new[] {'\n'})
                        .ToList();

                var parsedLines = new List<ParsedLine>();
                foreach (var line in lines)
                {
                    if (line.Trim() == "") continue;
                    var parsedLine = ParsedLine.Parse(line);
                    parsedLines.Add(parsedLine);
                    parsedLines.AddRange(parsedLine.InnerLines.Select(ParsedLine.Parse));
                }

                var rList = new List<string>();
                var pList = new List<string>();
                foreach (var parsedLine in parsedLines)
                {
                    var tab = parsedLine.Tab;

                    for (var i = rList.Count() - 1; i > tab; i--) rList.RemoveAt(i);
                    for (var i = rList.Count() - 1; i < tab; i++) rList.Add("");
                    for (var i = pList.Count() - 1; i > tab; i--) pList.RemoveAt(i);
                    for (var i = pList.Count() - 1; i < tab; i++) pList.Add("");

                    if (parsedLine.Predicate != "") pList[tab] = parsedLine.Predicate;
                    if (parsedLine.Resource == "") continue;

                    var predicate = "";
                    for (var i = pList.Count - 1; i >= 0 && predicate == ""; i--) predicate = pList[i];
                    if (predicate == "") predicate = "http://rdfs.org/sioc/ns#container_of";

                    if (!Uri.TryCreate(predicate, UriKind.Absolute, out tryUri))
                        predicate = Uri.EscapeUriString(
                            string.Format("{0}p#{1}", MainForm.config.GlobalPrefix, predicate));

                    var resource = parsedLine.Resource;

                    string currUri, currLabel;
                    if (resource[0] == '#')
                    {
                        currUri = Uri.EscapeUriString(
                            string.Format("{0}r#{1}", MainForm.config.GlobalPrefix,
                                resource.Substring(1)));
                        currLabel = resource.Substring(1);
                    }
                    else
                    {
                        if (Uri.TryCreate(resource, UriKind.Absolute, out tryUri))
                        {
                            currUri = tryUri.ToString();
                            currLabel = tryUri.Segments.Last().Replace("/", "");
                        }
                        else
                        {
                            var objUri = resource;
                            if (objUri.Length > 64) objUri = objUri.Substring(0, 64);

                            currUri = Uri.EscapeUriString(string.Format("{0}/r#{1}", GraphUri, objUri));
                            currLabel = resource;
                        }
                    }
                    rList[tab] = currUri;

                    var prevUri = "";
                    for (var i = tab - 1; i >= 0 && prevUri == "" ; i--) prevUri = rList[i];

                    if (prevUri != "")
                    {
                        var nS = G.CreateUriNode(UriFactory.Create(prevUri));
                        var nP = G.CreateUriNode(UriFactory.Create(predicate));
                        var nO = G.CreateUriNode(UriFactory.Create(currUri));
                        G.Assert(new Triple(nS, nP, nO));

                        var nLabelVal = G.CreateLiteralNode(currLabel);
                        G.Assert(new Triple(nO, nLabel, nLabelVal));
                    }
                    else
                    {
                        var nO = G.CreateUriNode(UriFactory.Create(currUri));
                        var nLabelVal = G.CreateLiteralNode(currLabel);
                        G.Assert(new Triple(nO, nLabel, nLabelVal));
                    }
                }
                ReportMsg("");
            }
            catch (Exception ex)
            {
                ReportMsg(ex);
            }
        }

        private void InitGraph()
        {
            G.Clear();
            InitNamespace();
        }

        private void InitNamespace()
        {
            G.NamespaceMap.Clear();
            G.NamespaceMap.AddNamespace("sioc", new Uri("http://rdfs.org/sioc/ns#"));
            G.NamespaceMap.AddNamespace("sur", new Uri(string.Format("{0}r#", MainForm.config.GlobalPrefix)));
            G.NamespaceMap.AddNamespace("sup", new Uri(string.Format("{0}p#", MainForm.config.GlobalPrefix)));
            G.NamespaceMap.AddNamespace("sugr", new Uri(string.Format("{0}/r#", GraphUri)));
        }

        private void UpdateTupleEditor()
        {
            try
            {
                InitGraph();
                var parser = new TurtleParser();
                TextReader sr = new StringReader(textBoxTurtleEditor.Text);
                parser.Load(G, sr);
                ReportMsg("");
            }
            catch (Exception ex)
            {
                ReportMsg(ex);
            }
        }

        private void UpdateTableEditor()
        {
            Uri tryUri;
            try
            {
                InitGraph();
                for (int i = 0; i < dataGridTableEditor.Rows.Count; i++)
                {
                    DataGridViewRow row = dataGridTableEditor.Rows[i];
                    string s, p, o;
                    if (row.Cells[0].Value == null || row.Cells[1].Value == null || row.Cells[2].Value == null)
                        continue;
                    s = row.Cells[0].Value.ToString();
                    p = row.Cells[1].Value.ToString();
                    o = row.Cells[2].Value.ToString();
                    Triple triple;
                    IUriNode ns = G.CreateUriNode(UriFactory.Create(s));
                    IUriNode np = G.CreateUriNode(UriFactory.Create(p));
                    if (Uri.TryCreate(o, UriKind.Absolute, out tryUri))
                    {
                        IUriNode no = G.CreateUriNode(UriFactory.Create(o));
                        triple = new Triple(ns, np, no);
                    }
                    else
                    {
                        ILiteralNode no = G.CreateLiteralNode(o);
                        triple = new Triple(ns, np, no);
                    }
                    G.Assert(triple);
                }
                ReportMsg("");
            }
            catch (Exception ex)
            {
                ReportMsg(ex);
            }
        }

        private void buttonSave_Click(object sender, EventArgs e)
        {
            SaveGraph();
        }

        public void SaveGraph()
        {
            UpdateEditor();

            string filePath = string.Format(@"{0}\{1}.txt", MainForm.DocumentRoot, GraphBase64);
            File.WriteAllText(filePath, textBoxTextEditor.Text);

            MainForm.fuseki.SaveGraph(G);

            ReportMsg("graph is saved");
        }

        private void textBoxTextEditor_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (editingContent == EditingContent.Updating) return;
            if (editingContent == EditingContent.None) editingContent = EditingContent.Text;
        }

        private void textBoxTurtleEditor_TextChanged(object sender, EventArgs e)
        {
            if (editingContent == EditingContent.Updating) return;
            if (editingContent == EditingContent.None) editingContent = EditingContent.Turtle;
        }

        private void dataGridTableEditor_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (editingContent == EditingContent.Updating) return;
            if (editingContent == EditingContent.None) editingContent = EditingContent.Table;
        }

        private void tabControl3_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateEditor();
        }

        private void dataGridTableEditor_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                foreach (DataGridViewTextBoxCell cell in dataGridTableEditor.SelectedCells)
                {
                    cell.Value = "";
                }
            }
            else if ((e.Shift && e.KeyCode == Keys.Insert) || (e.Control && e.KeyCode == Keys.V))
            {
                PasteTsv(dataGridTableEditor);
            }
        }

        public static void PasteTsv(DataGridView grid)
        {
            char[] rowSplitter = {'\r', '\n'};
            char[] columnSplitter = {'\t'};

            IDataObject dataInClipboard = Clipboard.GetDataObject();
            if (dataInClipboard == null) return;
            var stringInClipboard = (string) dataInClipboard.GetData(DataFormats.Text);
            string[] rowsInClipboard = stringInClipboard.Split(rowSplitter, StringSplitOptions.RemoveEmptyEntries);

            int r = grid.SelectedCells[0].RowIndex;
            int c = grid.SelectedCells[0].ColumnIndex;

            if (grid.Rows.Count < (r + rowsInClipboard.Length))
            {
                grid.Rows.Add(r + rowsInClipboard.Length - grid.Rows.Count);
            }

            for (int iRow = 0; iRow < rowsInClipboard.Length; iRow++)
            {
                string[] valuesInRow = rowsInClipboard[iRow].Split(columnSplitter);
                for (int iCol = 0; iCol < valuesInRow.Length; iCol++)
                {
                    if (grid.ColumnCount - 1 >= c + iCol)
                    {
                        DataGridViewCell cell = grid.Rows[r + iRow].Cells[c + iCol];

                        if (!cell.ReadOnly)
                        {
                            cell.Value = valuesInRow[iCol];
                        }
                    }
                }
            }
        }

        private void buttonQuery_Click(object sender, EventArgs e)
        {
            try
            {
                object results = G.ExecuteQuery(textBoxQuery.Text);
                if (results is SparqlResultSet)
                {
                    var keyIdxMap = new Dictionary<string, int>();
                    var rset = (SparqlResultSet) results;
                    int idx = 0;
                    foreach (SparqlResult result in rset)
                    {
                        foreach (var cell in result)
                        {
                            if (!keyIdxMap.ContainsKey(cell.Key)) keyIdxMap[cell.Key] = idx++;
                        }
                        break;
                    }

                    dataGridViewSPARQL.Columns.Clear();
                    dataGridViewSPARQL.ColumnCount = keyIdxMap.Keys.Count;
                    for (int i = 0; i < keyIdxMap.Keys.Count; i++)
                    {
                        dataGridViewSPARQL.Columns[i].Name = keyIdxMap.Keys.ToArray()[i];
                    }

                    dataGridViewSPARQL.Rows.Clear();
                    foreach (SparqlResult result in rset)
                    {
                        var row = new string[keyIdxMap.Keys.Count];
                        foreach (var cell in result)
                        {
                            int kidx = keyIdxMap[cell.Key];
                            row[kidx] = cell.Value.ToString();
                        }
                        dataGridViewSPARQL.Rows.Add(row.ToArray());
                    }
                }
            }
            catch
            {
                MessageBox.Show("invalid query");
            }
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

        private void textBoxTextEditor_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == (Keys.K | Keys.Control))
            {
                popupMenu.Show(true);
                e.Handled = true;
            }
        }

        private void textBoxTextEditor_MouseDown(object sender, MouseEventArgs e)
        {
            var p = textBoxTextEditor.PointToPlace(e.Location);
            if (CharIsHyperlink(p))
            {
                var url = textBoxTextEditor.GetRange(p, p).GetFragment(@"[\S]").Text;
                if (url.Length > 0 && url[0] == '`') url = url.Substring(1);
                if (url.Length > 2 && url.First() == '[' && url.Last() == ']') url = url.Substring(1, url.Length - 2);

                preview.ShowPreview(url);
            }
        }

        private void textBoxTextEditor_MouseMove(object sender, MouseEventArgs e)
        {
            Place p = textBoxTextEditor.PointToPlace(e.Location);
            if (CharIsHyperlink(p))
                textBoxTextEditor.Cursor = Cursors.Hand;
            else
                textBoxTextEditor.Cursor = Cursors.IBeam;
        }

        private void textBoxTextEditor_TextChangedDelayed(object sender, TextChangedEventArgs e)
        {
            e.ChangedRange.ClearStyle(blueStyle);
            e.ChangedRange.SetStyle(blueStyle,
                @"(http|ftp|https):\/\/[\w\-_]+(\.[\w\-_]+)+([\w\-\.,@?^=%&amp;:/~\+#\(\)]*[\w\-\@?^=%&amp;/~\+#\)])?");
        }

        private bool CharIsHyperlink(Place place)
        {
            StyleIndex mask = textBoxTextEditor.GetStyleIndexMask(new Style[] {blueStyle});
            if (place.iChar < textBoxTextEditor.GetLineLength(place.iLine))
                if ((textBoxTextEditor[place].style & mask) != 0)
                    return true;

            return false;
        }

        internal class DynamicCollection : IEnumerable<AutocompleteItem>
        {
            private readonly AutocompleteMenu menu;
            private GraphEditor editor;
            private FastColoredTextBox tb;

            public DynamicCollection(AutocompleteMenu menu, FastColoredTextBox tb, GraphEditor editor)
            {
                this.menu = menu;
                this.tb = tb;
                this.editor = editor;
            }

            public IEnumerator<AutocompleteItem> GetEnumerator()
            {
                string text = menu.Fragment.Text;
                if (text.Length < 1) yield break;
                if (text[0] != '#' && text[0] != '`') yield break;

                var subString = text.Substring(1);

                //string query = @"SELECT ?s ?l WHERE {  ?s <http://www.w3.org/2000/01/rdf-schema#label> ?l . FILTER regex(?l, '" + subString + @"', 'i') . }";
                //Object results = editor.g.ExecuteQuery(query);
                //predicate = string.Format("{0}p#{1}", MainForm.config.GlobalPrefix, rPredicateMatch.Value.Substring(1));
                //currUri = string.Format("{0}r#{1}", MainForm.config.GlobalPrefix, obj.Substring(1).Replace(" ", ""));
                //currUri = string.Format("{0}/r#{1}", graphUri, objUri);

                //if (results is SparqlResultSet)
                //{
                //    SparqlResultSet rset = (SparqlResultSet)results;
                //    foreach (SparqlResult result in rset)
                //    {
                //        yield return new MethodAutocompleteItem(kv.Key)
                //        {
                //            ToolTipTitle = kv.Value["title"],
                //            ToolTipText = kv.Value["text"]
                //        };
                //    }
                //}

                if (text[0] == '#')
                {
                    if (text.Length < 4) yield break;

                    string url = "https://tm.withcat.net/lod/json.php?db=dbp&word=" + subString;
                    string json = MainForm.GetJson(url);
                    if (json == "[]") yield break;

                    var items = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(json);
                    foreach (var kv in items)
                    {
                        yield return new MethodAutocompleteItem(kv.Key)
                        {
                            ToolTipTitle = kv.Value["title"],
                            ToolTipText = kv.Value["text"]
                        };
                    }
                }
                else if (text[0] == '`')
                {
                    foreach (var kv in MainForm.config.FindPredicate(subString))
                    {
                        string title = kv.Url.Split(new[] {'/', '#', '?'}).Last();
                        yield return new MethodAutocompleteItem(kv.Url)
                        {
                            ToolTipTitle = title,
                            ToolTipText = kv.Url
                        };
                    }
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private enum EditingContent
        {
            Updating,
            None,
            Text,
            Turtle,
            Table
        };

        public class ParsedLine
        {
            public List<string> InnerLines = new List<string>();
            public string Line;
            public string Predicate;
            public string Resource;
            public int Tab;

            public static string GetSafeFragment(string str)
            {
                Uri tryUri;
                if (Uri.TryCreate(str, UriKind.Absolute, out tryUri))
                {
                    return str;
                }
                return Regex.Replace(str, "[^a-z|A-Z|0-9|_]{1,}", "_");
            }

            public static ParsedLine Parse(string line)
            {
                var parsedLine = new ParsedLine {Line = line};

                var obj = line.TrimEnd();
                var lSpaceMatch = Regex.Match(obj, @"^[\s]+");
                if (lSpaceMatch.Success)
                {
                    parsedLine.Tab = lSpaceMatch.Length/4;
                }
                else
                {
                    parsedLine.Tab = 0;
                }

                foreach (var match in Regex.Matches(obj, @"\[(.+?)(\`.+?)?\]").Cast<Match>().Reverse())
                {
                    obj = obj.Remove(match.Index, match.Length).Insert(match.Index, match.Groups[1].Value);
                    parsedLine.InnerLines.Insert(0,
                        new string(' ', (parsedLine.Tab + 1) * 4) + match.Groups[1].Value + match.Groups[2].Value.Trim());
                }
                obj = obj.Trim();

                parsedLine.Predicate = "";
                obj = obj.Replace("``", "_SSWEDITOR_ESCAPE_PREDICATE_");
                var rPredicateMatch = Regex.Match(obj, @"\`(.+)$");
                if (rPredicateMatch.Success)
                {
                    parsedLine.Resource =
                        obj.Substring(0, rPredicateMatch.Index).Replace("_SSWEDITOR_ESCAPE_PREDICATE_", "`").Trim();
                    parsedLine.Predicate =
                        GetSafeFragment(
                            rPredicateMatch.Groups[1].Value.Replace("_SSWEDITOR_ESCAPE_PREDICATE_", "`").Trim());
                }
                else
                {
                    parsedLine.Resource = obj.Replace("_SSWEDITOR_ESCAPE_PREDICATE_", "`");
                }
                return parsedLine;
            }
        }
    }
}