using System;
using System.IO;
using System.Runtime.InteropServices;

namespace LogRotate
{
    /// <summary>
    /// Windows-friendly approximation of the POSIX 'struct stat' members that
    /// logrotate's logic actually relies on.
    /// </summary>
    public sealed class FileStat
    {
        public long Mode;          // approximation of st_mode
        public long Size;
        public long Uid;           // not really meaningful on Windows
        public long Gid;
        public long Nlink;         // hard link count (1 on most Windows fs)
        public DateTime Atime;
        public DateTime Mtime;
        public DateTime Ctime;
        public string? DeviceInfo; // volume serial for "different device" checks

        public static bool IsRegular(FileStat s) => (s.Mode & 0xF000) == 0x8000;
        public static bool IsDirectory(FileStat s) => (s.Mode & 0xF000) == 0x4000;
        public static bool IsSymlink(FileStat s) => (s.Mode & 0xF000) == 0xA000;

        public static FileStat? Stat(string path, bool followSymlinks = true)
        {
            bool isDir = Directory.Exists(path);
            bool isLink;
            var fi = new FileInfo(path);

            if (!fi.Exists && !isDir)
            {
                // path may contain characters that make it a dir check failure;
                // fall back to attributes
                try
                {
                    var fa = File.GetAttributes(path);
                    if ((fa & FileAttributes.Directory) != 0) isDir = true;
                    else if (fi.Exists) { /* ok */ }
                    else return null;
                }
                catch
                {
                    return null;
                }
            }

            var attrs = File.GetAttributes(path);
            isLink = (attrs & FileAttributes.ReparsePoint) != 0;

            if (isLink && followSymlinks)
            {
                // Resolve the symlink manually so we report the target's metadata.
                try
                {
                    string? linkTarget = null;
                    linkTarget = new FileInfo(path).LinkTarget;
                    if (linkTarget == null)
                    {
                        var di = new DirectoryInfo(path);
                        if (di.Exists) linkTarget = di.LinkTarget;
                    }
                    if (linkTarget != null)
                    {
                        string resolved;
                        try
                        {
                            string baseDir = Path.GetDirectoryName(Path.GetFullPath(path)) ?? ".";
                            resolved = Path.GetFullPath(Path.Combine(baseDir, linkTarget));
                        }
                        catch
                        {
                            resolved = linkTarget;
                        }
                        var inner = Stat(resolved, followSymlinks: true);
                        if (inner != null)
                            return inner;
                    }
                }
                catch
                {
                    // fall through to report the link itself
                }
            }

            long mode = isDir ? 0x4000 : 0x8000;
            // approximate perm bits: readonly => 0444, writable => 0644
            mode |= (long)((attrs & FileAttributes.ReadOnly) != 0 ? 0x124 : 0x1A4);

            var st = new FileStat
            {
                Mode = mode,
                Size = isDir ? 0 : (fi.Exists ? fi.Length : 0),
                Uid = 0, Gid = 0, Nlink = 1,
                Atime = fi.Exists ? fi.LastAccessTimeUtc : DateTime.UtcNow,
                Mtime = fi.Exists ? fi.LastWriteTimeUtc : DateTime.UtcNow,
                Ctime = fi.Exists ? fi.CreationTimeUtc : DateTime.UtcNow,
                DeviceInfo = null,
            };

            TryNativeInfo(path, st);
            return st;
        }

        public static FileStat? Lstat(string path)
        {
            if (!File.Exists(path) && !Directory.Exists(path))
            {
                try
                {
                    File.GetAttributes(path);
                }
                catch
                {
                    return null;
                }
            }

            var attrs = File.GetAttributes(path);
            bool isLink = (attrs & FileAttributes.ReparsePoint) != 0;
            bool isDir = (attrs & FileAttributes.Directory) != 0;
            long mode = isLink
                ? (isDir ? 0xA000 | 0x4000 : 0xA000 | 0x8000)
                : (isDir ? 0x4000 | 0x1A4 : 0x8000 | 0x1A4);

            var fi = new FileInfo(path);
            return new FileStat
            {
                Mode = mode,
                Size = isDir ? 0 : (File.Exists(path) ? new FileInfo(path).Length : 0),
                Uid = 0, Gid = 0, Nlink = 1,//me maybe isLink ? 1 : 0
                Atime = fi.LastAccessTime,  //me DateTime.UtcNow,
                Mtime = fi.LastWriteTime,   //DateTime.UtcNow,
                Ctime = fi.CreationTime,    // DateTime.UtcNow,
                DeviceInfo = null,
            };
        }

        private static void TryNativeInfo(string path, FileStat st)
        {
            try
            {
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                if (Native.GetFileInformationByHandle(fs.SafeFileHandle, out var info))
                {
                    st.Nlink = info.nNumberOfLinks;
                    st.DeviceInfo = info.dwVolumeSerialNumber.ToString("x8");
                    st.Atime = FromFileTime(info.ftLastAccessTime);
                    st.Mtime = FromFileTime(info.ftLastWriteTime);
                    st.Ctime = FromFileTime(info.ftCreationTime);
                    st.Size = ((long)info.nFileSizeHigh << 32) | info.nFileSizeLow;
                }
            }
            catch
            {
                // best effort
            }
        }

        private static DateTime FromFileTime(System.Runtime.InteropServices.ComTypes.FILETIME ft)
        {
            long high = ft.dwHighDateTime;
            long low = ft.dwLowDateTime;
            long combined = (high << 32) | (low & 0xFFFFFFFFL);
            return DateTime.FromFileTimeUtc(combined).ToLocalTime();
        }
    }
}