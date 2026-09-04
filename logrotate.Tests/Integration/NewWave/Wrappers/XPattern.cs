using System;
using System.IO;

namespace logrotate.Tests.Integration.GarbageTests.Wrappers;

internal class XPattern
{
    private readonly string _testDir;
    private readonly string _pattern;

    public XPattern(string testDir, string pattern)
    {
        _testDir = testDir;
        _pattern = string.IsNullOrEmpty(pattern) ? "*.*" : pattern;
    }

    public static string All => "*.*";

    public static string AllLogs => "*.log";


    public string Filepath
        => Path.Combine(_testDir, _pattern);

    public static implicit operator string(XPattern pattern) 
        => pattern.ToString();

    public override string ToString() 
        => Filepath;
}
