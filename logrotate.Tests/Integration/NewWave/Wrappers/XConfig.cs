using System;
using System.Collections.Generic;
using System.IO;

namespace logrotate.Tests.Integration.GarbageTests.Wrappers;

internal class XConfig
{
    private readonly string _testDir;
    private readonly string _filename;
    private readonly List<XConfigSection> _sections = new List<XConfigSection>();

    public XConfig(string testDir, string filename)
    {
        _testDir = string.IsNullOrEmpty(testDir) ? TestHelpersGarbage.TestDirMy : testDir;
        _filename = string.IsNullOrEmpty(_filename) ? $"config-{Guid.NewGuid()}.conf" : filename;
    }

    public XConfig(string filename)
        : this(string.Empty, filename)
    {
    }

    public XConfig()
        : this(string.Empty, string.Empty)
    {
    }


    public string Filepath
        => Path.Combine(_testDir, _filename);

    public XConfig WithSection(IEnumerable<string> patterns, Action<XConfigSection> init)
    {
        var section = new XConfigSection(patterns, _testDir);
        if (init != null)
            init(section);
        _sections.Add(section);
        return this;
    }

    public XConfig Create()
    {
        var content = string.Join("\r\n", _sections);
        File.WriteAllText(Filepath, content);
        return this;
    }

    public static implicit operator string(XConfig config) 
        => config.ToString();

    public override string ToString()
        => $"\"{Filepath}\"";
}
