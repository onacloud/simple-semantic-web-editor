using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using CefSharp;
using CefSharp.WinForms;
using Newtonsoft.Json;

namespace SSWEditor
{
    public partial class Preview : Form, IRequestHandler
    {
        public enum AgentType
        {
            None,
            Android,
            Chrome,
            Berry
        };

        public AgentType CurrAgent;
        public string PreviewUrl = "";
        public string RequestUrl = "";

        private ChromiumWebBrowser chromeBrowser;

        public Preview()
        {
            chromeBrowser = new ChromiumWebBrowser("")
            {
                Dock = DockStyle.Fill,
            };
            chromeBrowser.RequestHandler = this;
            Controls.Add(chromeBrowser);

            InitializeComponent();

            PerformLayout();
        }

        public bool GetAuthCredentials(IWebBrowser browser, bool isProxy, string host, int port, string realm,
            string scheme, ref string username, ref string password)
        {
            Debug.WriteLine("GetAuthCredentials");
            return false;
        }

        public bool OnBeforeBrowse(IWebBrowser browser, IRequest request, bool isRedirect)
        {
            Debug.WriteLine("OnBeforeBrowse");
            return false;
        }

        public bool OnBeforePluginLoad(IWebBrowser browser, string url, string policyUrl, IWebPluginInfo info)
        {
            Debug.WriteLine("OnBeforePluginLoad");
            return false;
        }

        public bool OnBeforeResourceLoad(IWebBrowser browser, IRequestResponse requestResponse)
        {
            var agent = "";
            switch (CurrAgent)
            {
                case AgentType.Android:
                    agent =
                        "Mozilla/5.0 (Linux; U; Android 2.3.4; en-us; Nexus S Build/GRJ22) AppleWebKit/533.1 (KHTML, like Gecko) Version/4.0 Mobile Safari/533.1";
                    break;
                case AgentType.Chrome:
                    agent =
                        "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/39.0.2171.65 Safari/537.36";
                    break;
                case AgentType.Berry:
                    agent =
                        "Mozilla/5.0 (BB10; Touch) AppleWebKit/537.10+ (KHTML, like Gecko) Version/10.0.9.2372 Mobile Safari/537.10+";
                    break;
            }
            requestResponse.Request.Headers.Set("User-Agent", agent);
            Debug.WriteLine("OnBeforeResourceLoad");
            return false;
        }

        public void OnPluginCrashed(IWebBrowser browser, string pluginPath)
        {
            Debug.WriteLine("OnPluginCrashed");
        }

        public void OnRenderProcessTerminated(IWebBrowser browser, CefTerminationStatus status)
        {
            Debug.WriteLine("OnRenderProcessTerminated");
        }

        private void Preview_Load(object sender, EventArgs e)
        {
            TopMost = true;
            Location = new Point(0, Screen.FromControl(this).Bounds.Height - 500);
            PerformLayout();
        }

        public void ShowPreview(string url)
        {
            Show();

            RequestUrl = url;
            PreviewUrl = url;

            linkLabel1.Text = RequestUrl;

            var extension = Path.GetExtension(url);
            if (extension != null && new[] {".jpg", ".png", ".gif", ".bmp"}.Contains(extension.ToLower()))
            {
                var imagePath = Application.StartupPath+ @"\preview.html";
                var imageHtml = @"
<!DOCTYPE html>

<html lang='en' xmlns='http://www.w3.org/1999/xhtml'>
<head>
    <meta charset='utf-8' />
    <title></title>
    <style>* { margin: 0;}</style>
</head>
<body>
    <img src='{src}' style='border: 0; width: 100%;' alt=''/>
</body>
</html>".Replace("{src}", url);
                File.WriteAllText(imagePath, imageHtml);
                chromeBrowser.Load(imagePath);
                chromeBrowser.Reload(true);
                PreviewUrl = imagePath;
                return;
            }

            CurrAgent = AgentType.None;
            Match m;
            if ((m = Regex.Match(url, @"//www\.youtube\.com/watch\?v=(.+)")).Success)
            {
                PreviewUrl = "http://www.youtube.com/embed/" + m.Groups[1].Value.Replace("&", "?");
            }
            else if ((m = Regex.Match(url, @"//www\.flickr\.com/(.+)")).Success)
            {
                PreviewUrl = "https://m.flickr.com/" + m.Groups[1].Value;
                CurrAgent = AgentType.Chrome;
            }
            else if ((m = Regex.Match(url, @"instagram\.com/")).Success)
            {
            }
            else if ((m = Regex.Match(url, @"pinterest\.com/")).Success)
            {
                CurrAgent = AgentType.Chrome;
            }
            else if ((m = Regex.Match(url, @"dbpedia\.org/resource/(.+)")).Success)
            {
                PreviewUrl = "http://en.m.wikipedia.org/wiki/" + m.Groups[1].Value;
            }
            else if ((m = Regex.Match(url, @"en\.wikipedia\.org/wiki/(.+)")).Success)
            {
                PreviewUrl = "http://en.m.wikipedia.org/wiki/" + m.Groups[1].Value;
            }
            if (CurrAgent == AgentType.None) CurrAgent = AgentType.Android;

            chromeBrowser.Load(PreviewUrl);
        }

        private string GetImageUrl(string url)
        {
            var imageUrl = "";
            var jsonUrl = "https://tm.withcat.net/lod/json.php?db=dbp&url=" + url;
            var jsonVal = MainForm.GetJson(jsonUrl);
            if (jsonVal != "[]")
            {
                var items = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(jsonVal);
                if (items.Count > 0)
                {
                    var kv = items.First();
                    if (kv.Value["image"] != "") imageUrl = kv.Value["image"] + "440";
                }
            }
            return imageUrl;
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(RequestUrl);
        }

        private void Preview_Deactivate(object sender, EventArgs e)
        {
        }

        private void Preview_Leave(object sender, EventArgs e)
        {
        }

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(PreviewUrl);
        }

        private void Preview_FormClosing(object sender, FormClosingEventArgs e)
        {
            Hide();
            e.Cancel = true;
        }
    }
}