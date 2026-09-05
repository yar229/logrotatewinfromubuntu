using System;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace LogRotate
{
    /// <summary>
    /// Minimal NTFS ACL support standing in for logrotate's POSIX ACL support
    /// (WITH_ACL acl_get_fd/acl_set_fd). The .NET FileSystem.AccessControl API
    /// is not referenced by this project, so the provider is wrapped directly:
    ///   - the access ACL (DACL) of the source file is read into a standalone
    ///     self-relative buffer (ReadDacl);
    ///   - the buffer is written verbatim onto the target file (WriteDacl);
    ///   - DefinesAccessAce inspects a file's DACL for an allow ACE belonging
    ///     to a given SID (used by the test suite in place of getfacl).
    /// Failures are never fatal: like logrotate without libacl, the rotation
    /// continues with the default ACL when the platform has no ACL to copy.
    /// </summary>
    public static class AclApi
    {
        private const uint SE_FILE_OBJECT = 1;
        private const uint DACL_SECURITY_INFORMATION = 0x00000004;
        private const byte ACCESS_ALLOWED_ACE_TYPE = 0;

        [DllImport("advapi32.dll", EntryPoint = "GetNamedSecurityInfoW",
            CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern uint GetNamedSecurityInfoW(
            string pObjectName, uint ObjectType, uint SecurityInfo,
            out IntPtr ppsidOwner, out IntPtr ppsidGroup,
            out IntPtr ppDacl, out IntPtr ppSacl,
            out IntPtr ppSecurityDescriptor);

        [DllImport("advapi32.dll", EntryPoint = "SetNamedSecurityInfoW",
            CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern uint SetNamedSecurityInfoW(
            string pObjectName, uint ObjectType, uint SecurityInfo,
            IntPtr psidOwner, IntPtr psidGroup, IntPtr pDacl, IntPtr pSacl);

        [DllImport("kernel32.dll")]
        private static extern IntPtr LocalFree(IntPtr hMem);

        /// <summary>
        /// Reads the access ACL of <paramref name="path"/> into a standalone
        /// self-relative buffer, or returns null when the file has no explicit
        /// DACL (nothing to copy).
        /// </summary>
        public static byte[]? ReadDacl(string path)
        {
            if (!TryGetDacl(path, out IntPtr pDacl, out IntPtr pSD))
                return null;
            try
            {
                if (pDacl == IntPtr.Zero || pSD == IntPtr.Zero)
                    return null;

                int size = Marshal.ReadInt16(pDacl, 2); /* ACL.AclSize */
                var buffer = new byte[size];
                Marshal.Copy(pDacl, buffer, 0, size);
                return buffer;
            }
            finally
            {
                LocalFree(pSD);
            }
        }

        /// <summary>
        /// Applies a previously read access ACL to <paramref name="path"/>.
        /// </summary>
        public static bool WriteDacl(string path, byte[] dacl)
        {
            if (dacl == null || dacl.Length == 0)
                return true;

            var handle = GCHandle.Alloc(dacl, GCHandleType.Pinned);
            try
            {
                return SetNamedSecurityInfoW(path, SE_FILE_OBJECT, DACL_SECURITY_INFORMATION,
                    IntPtr.Zero, IntPtr.Zero, handle.AddrOfPinnedObject(), IntPtr.Zero) == 0;
            }
            finally
            {
                handle.Free();
            }
        }

        /// <summary>
        /// True when the file's DACL contains an allow ACE for
        /// <paramref name="sid"/> (POSIX "getfacl | grep user:...:rwx").
        /// </summary>
        public static bool DefinesAccessAce(string path, SecurityIdentifier sid)
        {
            if (!TryGetDacl(path, out IntPtr pDacl, out IntPtr pSD))
                return false;
            try
            {
                if (pDacl == IntPtr.Zero || pSD == IntPtr.Zero)
                    return false;

                int aclSize = Marshal.ReadInt16(pDacl, 2);
                int aceCount = Marshal.ReadInt16(pDacl, 4);
                int offset = 8; /* start of the first ACE */

                for (int i = 0; i < aceCount; i++)
                {
                    byte aceType = Marshal.ReadByte(pDacl, offset);
                    int aceSize = Marshal.ReadInt16(pDacl, offset + 2);
                    if (aceSize < 8 || offset + aceSize > aclSize)
                        return false;

                    if (aceType == ACCESS_ALLOWED_ACE_TYPE)
                    {
                        int sidBytesLength = aceSize - 8;
                        var sidBytes = new byte[sidBytesLength];
                        Marshal.Copy(IntPtr.Add(pDacl, offset + 8), sidBytes, 0, sidBytesLength);
                        if (new SecurityIdentifier(sidBytes, 0) == sid)
                            return true;
                    }
                    offset += aceSize;
                }
                return false;
            }
            finally
            {
                LocalFree(pSD);
            }
        }

        private static bool TryGetDacl(string path, out IntPtr pDacl, out IntPtr pSD)
        {
            pDacl = IntPtr.Zero;
            pSD = IntPtr.Zero;
            return GetNamedSecurityInfoW(path, SE_FILE_OBJECT, DACL_SECURITY_INFORMATION,
                out _, out _, out pDacl, out _, out pSD) == 0;
        }
    }
}