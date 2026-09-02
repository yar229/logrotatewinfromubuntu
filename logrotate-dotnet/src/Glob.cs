using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LogRotate
{
    public enum GlobResultCode
    {
        GLOB_SUCCESS = 0,
        GLOB_NOMATCH = 1,
        GLOB_ABORTED = 2,
    }

    /// <summary>
    /// Portable replacement for glob(3). Supports '*', '?', '[...]' and
    /// '{a,b}' patterns and '~' tilde expansion.
    /// </summary>
    public static class Glob
    {
        /// <summary>
        /// Expands {a,b,c} braces producing all combinations.
        /// </summary>
        public static List<string> ExpandBraces(string pattern)
        {
            var output = new List<string> { string.Empty };
            int i = 0;
            while (i < pattern.Length)
            {
                char c = pattern[i];
                if (c == '{')
                {
                    int depth = 1;
                    int j = i + 1;
                    var group = new List<string>();
                    var current = new System.Text.StringBuilder();

                    while (j < pattern.Length && depth > 0)
                    {
                        char d = pattern[j];
                        if (d == '{')
                        {
                            depth++;
                            current.Append(d);
                        }
                        else if (d == '}')
                        {
                            depth--;
                            if (depth == 0)
                            {
                                group.Add(current.ToString().Trim());
                                break;
                            }
                            current.Append(d);
                        }
                        else if (d == ',' && depth == 1)
                        {
                            group.Add(current.ToString().Trim());
                            current.Clear();
                        }
                        else
                        {
                            current.Append(d);
                        }
                        j++;
                    }

                    if (depth != 0)
                    {
                        // unmatched '{' - treat literally
                        output = output.Select(o => o + c).ToList();
                        i++;
                        continue;
                    }

                    var combos = new List<string>();
                    foreach (var prefix in output)
                        foreach (var g in group)
                            combos.Add(prefix + g);
                    output = combos;
                    i = j + 1;
                }
                else
                {
                    output = output.Select(o => o + c).ToList();
                    i++;
                }
            }

            return output;
        }

        /// <summary>
        /// GLOB_NOCHECK|GLOB_TILDE semantics: a pattern with no wildcard always
        /// yields itself as the single result; a wildcard pattern yields matches
        /// or nothing.
        /// </summary>
        public static (GlobResultCode rc, List<string> paths) GlobNoCheck(string pattern)
        {
            var result = new List<string>();
            foreach (var alt in ExpandBraces(pattern))
            {
                var (code, paths) = ExpandSingle(alt, noCheck: true);
                if (code == GlobResultCode.GLOB_ABORTED)
                    return (GlobResultCode.GLOB_ABORTED, result);
                foreach (var p in paths)
                {
                    if (!result.Contains(p, StringComparer.Ordinal))
                        result.Add(p);
                }
            }

            if (result.Count == 0)
                return (GlobResultCode.GLOB_NOMATCH, result);

            result.Sort(StringComparer.Ordinal);
            return (GlobResultCode.GLOB_SUCCESS, result);
        }

        private static (GlobResultCode, List<string>) ExpandSingle(string pattern, bool noCheck)
        {
            if (pattern.Length > 0 && pattern[0] == '~'
                && (pattern.Length == 1 || pattern[1] == '/' || pattern[1] == '\\'))
            {
                var home = Environment.GetEnvironmentVariable("HOME")
                    ?? Environment.GetEnvironmentVariable("USERPROFILE")
                    ?? ".";
                pattern = pattern.Length == 1 ? home : home + "\\" + pattern.Substring(2);
            }

            bool hasWildcard = pattern.IndexOfAny(new[] { '*', '?', '[' }) >= 0;

            if (!hasWildcard)
            {
                try
                {
                    if (File.Exists(pattern) || Directory.Exists(pattern))
                        return (GlobResultCode.GLOB_SUCCESS, new List<string> { pattern });
                }
                catch
                {
                    return (GlobResultCode.GLOB_ABORTED, new List<string>());
                }
                if (noCheck)
                    return (GlobResultCode.GLOB_NOMATCH, new List<string> { pattern });
                return (GlobResultCode.GLOB_NOMATCH, new List<string>());
            }

            int lastSep = Math.Max(pattern.LastIndexOf('/'), pattern.LastIndexOf('\\'));
            string dirPart, filePart;
            if (lastSep < 0)
            {
                dirPart = ".";
                filePart = pattern;
            }
            else
            {
                dirPart = pattern.Substring(0, lastSep + 1);
                filePart = pattern.Substring(lastSep + 1);
            }

            var result = ExpandDir(dirPart, filePart);
            if (result.Count == 0)
                return (GlobResultCode.GLOB_NOMATCH, result);

            result = result.Distinct(StringComparer.Ordinal).ToList();
            result.Sort(StringComparer.Ordinal);
            return (GlobResultCode.GLOB_SUCCESS, result);
        }

        private static List<string> ExpandDir(string dirPart, string filePart)
        {
            var matches = new List<string>();
            bool dirHasWild = dirPart.IndexOfAny(new[] { '*', '?', '[' }) >= 0;

            IEnumerable<string> dirs;
            if (dirHasWild)
                dirs = ExpandWildDirs(dirPart);
            else
                dirs = new[] { dirPart };

            foreach (var dir in dirs)
            {
                string fullDir = dir;
                if (!Directory.Exists(fullDir))
                    continue;

                try
                {
                    foreach (var entry in Directory.GetFileSystemEntries(fullDir))
                    {
                        string name = Path.GetFileName(entry);
                        if (Fnmatch(filePart, name, fnmPeriod: false))
                        {
                            string pp = Join(dir, name);
                            if (!matches.Contains(pp, StringComparer.Ordinal))
                                matches.Add(pp);
                        }
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    // EACCES -> caller aborts (globerr would return nonzero)
                    throw;
                }
                catch
                {
                    // ignore unreadable directories
                }
            }
            return matches;
        }

        private static string Join(string dir, string name)
        {
            if (dir == "." || string.IsNullOrEmpty(dir))
                return name;
            string sep = dir.EndsWith("\\") || dir.EndsWith("/") ? "" : "\\";
            return dir + sep + name;
        }

        private static IEnumerable<string> ExpandWildDirs(string dirPart)
        {
            var root = new List<string>();
            // Handle an optional leading drive letter or root separator.
            string drive = "";
            int i = 0;
            if (dirPart.Length >= 2 && dirPart[1] == ':')
            {
                drive = dirPart.Substring(0, 2);
                i = 2;
            }
            else if (dirPart.StartsWith("\\") || dirPart.StartsWith("/"))
            {
                root.Add(drive); // root of current volume
                i = 1;
            }

            if (root.Count == 0)
                root.Add(drive);

            var segments = dirPart.Substring(i)
                .Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var seg in segments)
            {
                var next = new List<string>();
                foreach (var basePath in root)
                {
                    string current = basePath.Length == 0 ? seg : basePath + "\\" + seg;
                    if (seg.IndexOfAny(new[] { '*', '?', '[' }) >= 0)
                    {
                        string searchRoot = basePath.Length == 0 ? "." : basePath;
                        try
                        {
                            foreach (var d in Directory.GetDirectories(searchRoot, seg))
                                next.Add(d);
                        }
                        catch (UnauthorizedAccessException) { throw; }
                        catch { }
                    }
                    else
                    {
                        next.Add(current);
                    }
                }
                root = next;
            }
            return root;
        }

        /// <summary>
        /// Matches a shell-wildcard pattern against a file name (subset of
        /// fnmatch(3): *, ?, [..], [!..]). If fnmPeriod is set, a leading '.' in
        /// the name must be matched explicitly by a leading '.' in the pattern.
        /// </summary>
        public static bool Fnmatch(string pattern, string name, bool fnmPeriod = false)
        {
            return FnmatchCore(pattern, 0, name, 0, fnmPeriod);
        }

        private static bool FnmatchCore(string pattern, int pi, string name, int ni, bool period)
        {
            if (period && ni == 0 && name.Length > 0 && name[0] == '.'
                && (pi >= pattern.Length || pattern[0] != '.'))
                return false;

            while (pi < pattern.Length)
            {
                char pc = pattern[pi];
                if (pc == '*')
                {
                    while (pi < pattern.Length && pattern[pi] == '*')
                        pi++;
                    if (pi == pattern.Length)
                        return true;
                    for (int k = ni; k <= name.Length; k++)
                    {
                        if (FnmatchCore(pattern, pi, name, k, period))
                            return true;
                    }
                    return false;
                }
                else if (pc == '?')
                {
                    if (ni >= name.Length)
                        return false;
                    pi++;
                    ni++;
                }
                else if (pc == '[')
                {
                    if (ni >= name.Length)
                        return false;
                    int close = pattern.IndexOf(']', pi + 1);
                    if (close < 0)
                    {
                        if (name[ni] != '[')
                            return false;
                        pi++;
                        ni++;
                        continue;
                    }
                    string range = pattern.Substring(pi + 1, close - pi - 1);
                    bool negate = range.StartsWith('!') || range.StartsWith('^');
                    if (negate)
                        range = range.Substring(1);
                    bool matched = false;
                    for (int r = 0; r < range.Length; r++)
                    {
                        if (r + 2 < range.Length && range[r + 1] == '-')
                        {
                            if (name[ni] >= range[r] && name[ni] <= range[r + 2])
                            {
                                matched = true;
                                break;
                            }
                            r += 2;
                        }
                        else if (name[ni] == range[r])
                        {
                            matched = true;
                            break;
                        }
                    }
                    if (negate)
                        matched = !matched;
                    if (!matched)
                        return false;
                    pi = close + 1;
                    ni++;
                }
                else
                {
                    if (ni >= name.Length || name[ni] != pc)
                        return false;
                    pi++;
                    ni++;
                }
            }
            return ni == name.Length;
        }
    }
}