using System;
using System.IO;

namespace logrotate.Tests.Integration.GarbageTests.Wrappers;

public static class TestHelpersGarbage
{
    //public const string TestDirMy = "c:\\1";

    //public static void CleanupTestDir(bool selfdelete = false)
    //{
    //    try
    //    {
    //        if (File.Exists(TestDirMy))
    //        {
    //            File.Delete(TestDirMy);
    //        }
    //        else if (Directory.Exists(TestDirMy))
    //        {
    //            Directory.Delete(TestDirMy, true);
    //            Directory.CreateDirectory(TestDirMy);
    //        }
    //        else
    //        {
    //            Directory.CreateDirectory(TestDirMy);
    //        }
    //    }
    //    catch
    //    {
    //        // Ignore cleanup errors in tests
    //    }
    //}

    public static string Quote(string filepath)
    {
        bool isQuoted = filepath.Length >= 2 && filepath.StartsWith("\"") && filepath.EndsWith("\"");
        if (!isQuoted)
            return $"\"{filepath}\"";
        return filepath;
    }
}
