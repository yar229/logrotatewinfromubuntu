using System;
using System.IO;

namespace logrotate.Tests.Integration.GarbageTests.Wrappers;

internal class XFile: XBaseFile
{
    public XFile(string testDir, string filename)
    {
        _testDir = string.IsNullOrEmpty(testDir) ? TestHelpersGarbage.TestDirMy : testDir;
        Filename = string.IsNullOrEmpty(filename) ? $"file-{Guid.NewGuid()}" : filename;
    }

    public XFile(string filename)
        : this(string.Empty, filename)
    {
    }

    public XFile()
        : this(string.Empty, string.Empty)
    {
    }

    public override string Type => "file";
}
