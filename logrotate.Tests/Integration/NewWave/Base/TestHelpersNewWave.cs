using System;
using System.IO;

namespace logrotate.Tests.Integration.NewWave.Base;

public static class TestHelpersNewWave
{
    public static string Quote(string filepath)
    {
        bool isQuoted = filepath.Length >= 2 && filepath.StartsWith("\"") && filepath.EndsWith("\"");
        if (!isQuoted)
            return $"\"{filepath}\"";
        return filepath;
    }
}
