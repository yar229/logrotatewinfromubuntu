using System;
namespace logrotate.Tests.Integration.NewWave.Wrappers;

internal class XLogFile : XBaseFile
{
    public XLogFile(string testDir, string filename)
        :base(testDir)
    {
        Filename = string.IsNullOrEmpty(filename) ? $"log-{Guid.NewGuid()}.log" : filename;
    }

    public override string Type => "logfile";

    public static implicit operator string(XLogFile logFile)
    {
        return logFile.ToString();
    }

    public override string ToString() 
        => Filepath;
}
