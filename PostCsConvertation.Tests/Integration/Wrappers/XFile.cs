using System;

namespace PostCsConvertation.Tests.Integration.Wrappers;

internal class XFile: XBaseFile
{
    public XFile(string testDir, string filename)
        : base(testDir)
    {
        Filename = string.IsNullOrEmpty(filename) ? $"file-{Guid.NewGuid()}" : filename;
    }

    public XFile()
        : this(string.Empty, string.Empty)
    {
    }

    public override string Type => "file";
}
