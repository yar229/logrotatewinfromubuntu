using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace LogRotate
{
    /// <summary>
    /// C-locale character classification helpers, mirroring fcns from ctype.h
    /// as used in the original C code (bytes in the C locale).
    /// </summary>
    public static class C
    {
        public static bool IsBlank(char c) => c == ' ' || c == '\t';
        public static bool IsSpace(char c) => char.IsWhiteSpace(c);
        public static bool IsAlpha(char c) => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
        public static bool IsDigit(char c) => c >= '0' && c <= '9';
        public static bool IsPrint(char c) => !char.IsControl(c);

        /// <summary>
        /// isblank for the purposes of parsing.
        /// </summary>
        public static bool BlankOrSpace(char c) => char.IsWhiteSpace(c);

        /// <summary>
        /// Parses a number like strtoull(str, &end, 0): supports decimal,
        /// 0x hex and 0 leading octal. Returns parsed value and out end index.
        /// </summary>
        public static bool TryParseNumber(string s, int start, out ulong value, out int end)
        {
            value = 0;
            end = start;
            int i = start;
            int numBase = 10;

            if (i < s.Length && (s[i] == '+' || s[i] == '-'))
            {
                // strtoul allows a plus/minus sign (minus becomes a huge value).
                if (s[i] == '-')
                {
                    // We don't need negative numbers; treat as parse failure below.
                }
                i++;
            }

            if (i < s.Length - 1 && s[i] == '0' && (s[i + 1] == 'x' || s[i + 1] == 'X'))
            {
                numBase = 16;
                i += 2;
            }
            else if (i < s.Length && s[i] == '0' 
                && s.Length > 1) //fix for parsing single digit to base10
            {
                numBase = 8;
                i++;
            }

            ulong accum = 0;
            int digitStart = i;
            while (i < s.Length)
            {
                char c = s[i];
                int v;
                if (c >= '0' && c <= '9') v = c - '0';
                else if (c >= 'a' && c <= 'f') v = c - 'a' + 10;
                else if (c >= 'A' && c <= 'F') v = c - 'A' + 10;
                else break;

                if (v >= numBase) break;

                accum = checked(accum * (ulong)numBase + (ulong)v);
                i++;
            }

            if (i == digitStart)
            {
                end = digitStart;
                return false;
            }

            value = accum;
            end = i;
            return true;
        }

        /// <summary>
        /// strcoll()-style comparison for sorting file lists.
        /// </summary>
        public static int StrColl(string a, string b)
        {
            return string.Compare(a, b, CultureInfo.InvariantCulture, CompareOptions.None);
        }
    }

    internal static class Native
    {
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool GetFileInformationByHandle(
            Microsoft.Win32.SafeHandles.SafeFileHandle hFile,
            out BY_HANDLE_FILE_INFORMATION lpFileInformation);

        [StructLayout(LayoutKind.Sequential)]
        public struct BY_HANDLE_FILE_INFORMATION
        {
            public uint dwFileAttributes;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftCreationTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastAccessTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastWriteTime;
            public uint dwVolumeSerialNumber;
            public uint nFileSizeHigh;
            public uint nFileSizeLow;
            public uint nNumberOfLinks;
            public uint nFileIndexHigh;
            public uint nFileIndexLow;
        }
    }
}