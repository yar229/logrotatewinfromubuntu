using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace logrotate.Tests.Integration.GarbageTests.Wrappers;

internal class XStateFile
{
    private readonly string _testDir;
    private readonly string _filename;
    private const string _header1 = "";
    private const string _header2 = "";

    private static readonly string[] _headers = [
            //$"# logrotate state file created {DateTime.Now.ToString("dd.MM.yyyy hh:mm:ss")}",
            "logrotate state -- version 2"];

    private readonly List<string> _processed = new List<string>();

    public XStateFile(string testDir, string filename)
    {
        _testDir = string.IsNullOrEmpty(testDir) ? TestHelpersGarbage.TestDirMy : testDir;
        _filename = string.IsNullOrEmpty(filename) ? $"state-{Guid.NewGuid()}.txt" : filename;
    }

    public XStateFile(string filename)
        :this(string.Empty, filename)
    {
    }

    public XStateFile()
        : this(string.Empty, string.Empty)
    {
    }

    public string Filepath
        => Path.Combine(_testDir, _filename);

    public XStateFile WithProcessed(string filepath, DateTime datetime)
    {
        filepath = TestHelpersGarbage.Quote(filepath);

        _processed.Add($"{filepath} {DateTime.Now.ToString("yyyy-M-d-h:m:s")}");
        return this;
    }

    public XStateFile WithProcessed(string filepath)
    {
        return WithProcessed(filepath, DateTime.Now);
    }

    public XStateFile Create()
    {
        File.WriteAllLines(Filepath, Enumerable.Concat(_headers, _processed));
        return this;
    }

    public static implicit operator string(XStateFile stateFile) 
        => stateFile.ToString();

    public override string ToString() 
        => $"\"{Filepath}\"";
}
