using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LogRotate
{
    /// <summary>
    /// Replacement for poptParseArgvString(3): splits a string into argv-style
    /// tokens honoring single/double quotes. Inside double quotes a backslash
    /// escapes the next character; outside quotes backslashes are kept literally
    /// so that Windows paths survive tokenization.
    /// </summary>
    public static class ArgvParser
    {
        public static List<string>? Parse(string s)
        {
            var result = new List<string>();
            int len = s.Length;
            int i = 0;
            bool inQuote = false;
            bool inDoubleQuote = false;
            bool startToken = true;
            var sb = new StringBuilder();

            while (i < len)
            {
                char c = s[i];

                if (inQuote)
                {
                    if (c == '\'')
                        inQuote = false;
                    else
                        sb.Append(c);
                    i++;
                    continue;
                }

                if (inDoubleQuote)
                {
                    if (c == '"')
                    {
                        inDoubleQuote = false;
                    }
                    else if (c == '\\')
                    {
                        sb.Append(Path.DirectorySeparatorChar);
                    }
                    else if (c == '/')
                    {
                        sb.Append(Path.DirectorySeparatorChar);
                    }

                    //me
                    //else if (c == '\\')
                    //{
                    //    i++;
                    //    sb.Append(s[i]);

                    //    if (i < len)
                    //        sb.Append(s[i]);
                    //    else
                    //        return null;
                    //}
                    else
                    {
                        sb.Append(c);
                    }
                    i++;
                    continue;
                }

                if (startToken)
                {
                    if (c == '"')
                    {
                        inDoubleQuote = true;
                        startToken = false;
                    }
                    else if (c == '\'')
                    {
                        inQuote = true;
                        startToken = false;
                    }
                    else if (C.IsSpace(c))
                    {
                        // skip
                    }
                    else
                    {
                        startToken = false;
                        sb.Append(c);
                    }
                    i++;
                    continue;
                }

                // middle of token
                if (c == '"')
                {
                    inDoubleQuote = true;
                }
                else if (c == '\'')
                {
                    inQuote = true;
                }
                else if (C.IsSpace(c))
                {
                    result.Add(sb.ToString());
                    sb.Clear();
                    startToken = true;
                }
                else
                {
                    sb.Append(c);
                }
                i++;
            }

            if (inQuote || inDoubleQuote)
                return null;

            if (!startToken)
                result.Add(sb.ToString());

            return result;
        }
    }
}