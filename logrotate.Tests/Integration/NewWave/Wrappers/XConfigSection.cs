using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace logrotate.Tests.Integration.GarbageTests.Wrappers
{
    internal class XConfigSection
    {
        private readonly List<string> _filePatterns = new List<string>();
        private readonly List<string> _directives = new List<string>();
        private readonly string _testDir;

        public XConfigSection(IEnumerable<string> filePatterns, string testDir)
        {
            _filePatterns = filePatterns.ToList();
            _testDir = testDir;
        }

        public XConfigSection With(string key)
        {
            _directives.Add(key);
            return this;
        }

        public XConfigSection With(string key, string value)
        {
            _directives.Add($"{key} {value}");
            return this;
        }

        public XConfigSection With(string key, int value)
        {
            return With(key, value.ToString());
        }

        public XConfigSection WithQuoted(string key, string value)
        {
            _directives.Add($"{key} \"{value}\"");
            return this;
        }

        public XConfigSection WithScript(string key, string value)
        {
            _directives.Add($"{key}\r\n\t\t{value}\r\n\tendscript");
            return this;
        }

        public XConfigSection WithEcho(string key, string filepath, string content = "")
        {
            if (string.IsNullOrEmpty(content))
                content = $"\t\techo content of {filepath} > {filepath}";
            _directives.Add($"{key}\r\n{content}\r\n\tendscript");
            return this;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            if (!_filePatterns.Any())  //global section
            { 
                foreach (var directive in _directives)
                    sb.AppendLine(directive);
                return sb.ToString();
            }

            sb.AppendLine(string.Join(" ", _filePatterns.Select(fp => TestHelpersGarbage.Quote(fp))) + " {");
            foreach (var str in _directives)
                sb.AppendLine($"\t{str}");
            sb.Append("}");

            return sb.ToString();
        }
    }
}
