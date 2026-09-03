using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace logrotate.Tests.Integration.GarbageTests.Wrappers;

internal class XLogFile : XBaseFile
{

    public XLogFile(string testDir, string filename)
    {
        _testDir = string.IsNullOrEmpty(testDir) ? TestHelpersGarbage.TestDirMy : testDir;
        Filename = string.IsNullOrEmpty(filename) ? $"log-{Guid.NewGuid()}.log" : filename;
    }

    public XLogFile(string filename)
        :this(string.Empty, filename)
    {
    }

    public XLogFile()
        : this(string.Empty, string.Empty)
    {
    }

    public override string Type => "logfile";

    public XLogFile Create()
    {
        File.WriteAllText(Filepath, string.IsNullOrEmpty(_content) ? $"content of {Filename}" : _content);
        return this;
    }

    public static implicit operator string(XLogFile logFile)
    {
        return logFile.ToString();
    }

    public override string ToString() 
        => $"{Filepath}";
}
