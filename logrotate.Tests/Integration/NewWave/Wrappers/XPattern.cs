using System;
using System.IO;

namespace logrotate.Tests.Integration.GarbageTests.Wrappers;

internal class XPattern
{
    private readonly string _testDir;
    private readonly string _pattern;

    public XPattern(string testDir, string pattern)
    {
        _testDir = string.IsNullOrEmpty(testDir) ? TestHelpersGarbage.TestDirMy : testDir;
        _pattern = string.IsNullOrEmpty(pattern) ? "*.*" : pattern;
    }

    public XPattern(string pattern)
        : this(string.Empty, pattern)
    {
    }

    public XPattern()
        : this(string.Empty, string.Empty)
    {
    }

    public static XPattern All
        => new XPattern("*.*");

    public static XPattern AllLogs
        => new XPattern("*.log");


    public string Filepath
        => Path.Combine(_testDir, _pattern);

    public static implicit operator string(XPattern pattern) 
        => pattern.ToString();

    public override string ToString() 
        => $"\"{Filepath}\"";
}
