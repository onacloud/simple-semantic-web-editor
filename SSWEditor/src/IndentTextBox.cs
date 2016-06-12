using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;

namespace SSWEditor
{
    public class IndentTextBox : RichTextBox
    {
        bool isShift;
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            isShift = e.Shift;
        }

        protected override void OnLinkClicked(LinkClickedEventArgs e)
        {
            Process.Start(e.LinkText);
            base.OnLinkClicked(e);
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            if (e.KeyChar == '@')
            {
                //autoCompleteBox.Parent = this;
                //Point cursorPt = Cursor.Position;
                //autoCompleteBox.Location = PointToClient(cursorPt);
                //autoCompleteBox.Items.Clear();
                //foreach (String s in autoCompleteList)
                //{
                //    autoCompleteBox.Items.Add(s);
                //}
                //autoCompleteBox.Show();
                //autoCompleteBox.Visible = true;
            } 
            else if (e.KeyChar == (char)Keys.Return)
            {
                e.Handled = true;

                var pos = SelectionStart;
                var lineNumber = GetLineFromCharIndex(pos) - 1;
                var currentLineStr = Lines[lineNumber];

                var firstChar = 0;
                while (firstChar != currentLineStr.Length)
                {
                    if (!Char.IsWhiteSpace(currentLineStr[firstChar])) break;
                    firstChar++;
                }
                var indent = currentLineStr.Substring(0, firstChar);
                SelectionFont = Font;
                SelectedText = indent;
            }
            else if (e.KeyChar == '\t')
            {
                e.Handled = true;

                if (!isShift && SelectionLength == 0)
                {
                    SelectionFont = Font;
                    SelectedText = "    ";
                }
                else
                {
                    var lineStart = GetLineFromCharIndex(SelectionStart);
                    var lineEnd = GetLineFromCharIndex(SelectionStart + SelectionLength);
                    var selStart = GetFirstCharIndexFromLine(lineStart);
                    var selEnd = GetFirstCharIndexFromLine(lineEnd) + Lines[lineEnd].Length;
                    var selLength = selEnd - selStart;
                    SelectionStart = selStart;
                    SelectionLength = selLength;

                    var lines = SelectedText.Split(new[] { '\n' }, StringSplitOptions.None);
                    var replacement = "";
                    if (isShift)
                    {
                        replacement = string.Join("\n", lines.Select(line => line.ReduceIndent(4)));
                    }
                    else
                    {
                        replacement = string.Join("\n", lines.Select(line => line.IncreaseIndent(4)));
                    }
                    SelectionFont = Font;
                    SelectedText = replacement;
                    SelectionStart = selStart;
                    SelectionLength = replacement.Length;
                }
            }

            base.OnKeyPress(e);
        }
    }
    public static class StringExtensions
    {
        public static string ReduceIndent(this string line, int level)
        {
            var unindentedChars = line.SkipWhile((c, index) => char.IsWhiteSpace(c) && index < level);
            return new string(unindentedChars.ToArray());
        }

        public static string IncreaseIndent(this string line, int level)
        {
            return new string(' ', level) + line;
        }
    }

}
