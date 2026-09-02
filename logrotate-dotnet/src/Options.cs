using System;
using System.IO;

namespace LogRotate
{
    /// <summary>
    /// Compile-time-ish defaults (mirrors #defines from configure).
    /// </summary>
    public static class Options
    {
        public const string Version = "3.22.0";

        // Defaults for the Windows port. External programs are looked up in
        // PATH, mirroring the original behavior of invoking gzip/gunzip.
        public static string DefaultCompressCommand { get; } =
            Environment.GetEnvironmentVariable("LOGROTATE_COMPRESS")
            ?? ""; //"gzip";

        public static string DefaultUncompressCommand { get; } =
            Environment.GetEnvironmentVariable("LOGROTATE_UNCOMPRESS")
            ?? "gunzip";

        public static string DefaultCompressExt { get; } = ".gz";

        public static string DefaultMailCommand { get; } =
            Environment.GetEnvironmentVariable("LOGROTATE_MAIL")
            ?? "mail";

        public static string DefaultStateFile =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "logrotate", "status");
    }
}