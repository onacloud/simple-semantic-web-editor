using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;

namespace SSWEditor
{
    public class ConfigPredicate
    {
        public bool Usage = false;
        public string Url = "";
    }

    public class Config
    {
        private string globalPrefix;
        public string GlobalPrefix
        {
            get { return globalPrefix; }
            set
            {
                globalPrefix = value;
                if (globalPrefix.Length > 0 && globalPrefix[globalPrefix.Length - 1] != '/') globalPrefix += "/";
            }
        }

        public string FusekiServer { get; set; }

        private int fusekiPort = 3030;
        public int FusekiPort
        {
            get { return fusekiPort; }
            set
            {
                if (value > 0 && value < 65536) fusekiPort = value;
            }
        }

        public bool ShowFusekiConsole { get; set; }

        private string editorFont = "";
        public string EditorFont
        {
            get { return editorFont; }
            set { editorFont = value; }
        }

        public void SetEditorFont(Font font)
        {
            var cvt = new FontConverter();
            editorFont = cvt.ConvertToString(font);
        }

        public Font GetEditorFont()
        {
            var cvt = new FontConverter();
            return cvt.ConvertFromString(editorFont) as Font;
        }

        private List<ConfigPredicate> predicateList = new List<ConfigPredicate>();

        public Config()
        {
            ShowFusekiConsole = false;
        }

        public List<ConfigPredicate> PredicateList
        {
            get { return predicateList; }
            set { predicateList = value; }
        }
        public List<ConfigPredicate> FindPredicate(string str)
        {
            return predicateList.Where(e => e.Usage).Where(predicate => predicate.Url.IndexOf(str, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
        }

        public static string GetRandomString(int length)
        {
            var rand = new RNGCryptoServiceProvider();
            var chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890".ToCharArray();
            var s = "";
            for (var i = 0; i < length; i++)
            {
                var intBytes = new byte[4];
                rand.GetBytes(intBytes);
                var randomInt = BitConverter.ToUInt32(intBytes, 0);
                s += chars[randomInt % chars.Length];
            }
            return s;
        }
    }
}
