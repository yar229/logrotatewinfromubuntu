using System;
using System.IO;

namespace LogRotate
{
    /// <summary>
    /// POSIX-like file operations implemented for Windows.
    /// </summary>
    public static class FileUtil
    {
        /// <summary>
        /// POSIX rename(): moves src to dst, overwriting dst if it exists.
        /// </summary>
        public static bool Rename(string src, string dst)
        {
            try
            {
                if (File.Exists(dst) || Directory.Exists(dst))
                {
                    // POSIX rename overwrites; .NET File.Move doesn't.
                    var attrs = File.GetAttributes(dst);
                    bool isDir = (attrs & FileAttributes.Directory) != 0;
                    if (isDir)
                    {
                        // POSIX can't overwrite a non-empty dir with a file either
                        if (Directory.Exists(src) && DirectoryIsEmpty(dst))
                        {
                            Directory.Delete(dst);
                            Directory.Move(src, dst);
                            return true;
                        }
                        return false;
                    }
                    DeleteFile(dst);
                }
                File.Move(src, dst);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool DirectoryIsEmpty(string path)
        {
            return !Directory.EnumerateFileSystemEntries(path).Any();
        }

        /// <summary>
        /// POSIX unlink(): remove file, ignoring "not found".
        /// Returns 0 on success, -1 with errno on failure.
        /// </summary>
        public static int Unlink(string path, out int errno)
        {
            errno = 0;
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                    return 0;
                }
                if (Directory.Exists(path))
                {
                    // unlink on a directory -> EISDIR
                    errno = 21; // EISDIR
                    return -1;
                }
                errno = 2; // ENOENT
                return -1;
            }
            catch (IOException)
            {
                errno = 13; // EACCES
                return -1;
            }
            catch (UnauthorizedAccessException)
            {
                errno = 13;
                return -1;
            }
        }

        /// <summary>
        /// Remove file if exists (swallows not-found).
        /// </summary>
        public static void DeleteFile(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
                // best effort
            }
        }

        /// <summary>
        /// Truncates the file to zero bytes while keeping it open (ftruncate emulation).
        /// </summary>
        public static bool Truncate(FileStream fs)
        {
            try
            {
                fs.SetLength(0);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Creates a directory hierarchy (mkdir -p emulation).
        /// </summary>
        public static bool MkPath(string path)
        {
            try
            {
                Directory.CreateDirectory(path);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    internal static class LinqShim
    {
        public static bool Any(this System.Collections.Generic.IEnumerable<string> enumerable)
        {
            foreach (var _ in enumerable) return true;
            return false;
        }
    }
}