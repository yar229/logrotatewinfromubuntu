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
        private const uint OWNER_SECURITY_INFORMATION = 0x00000001;
        private const uint GROUP_SECURITY_INFORMATION = 0x00000008;
        private const byte ACCESS_ALLOWED_ACE_TYPE = 0;

        private const uint GENERIC_READ = 0x80000000;
        private const uint GENERIC_WRITE = 0x40000000;
        private const uint GENERIC_EXECUTE = 0x20000000;

        private static readonly SecurityIdentifier Everyone =
            new(WellKnownSidType.WorldSid, null);

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

        [DllImport("advapi32.dll")]
        private static extern int GetLengthSid(IntPtr pSid);

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
        /// Realizes a POSIX 'create MODE USER GROUP' directive on Windows:
        /// ownership/group are transferred to the resolved SIDs and the mode is
        /// materialized as a DACL (owner class -> owning account, group class ->
        /// primary-group account, other class -> Everyone). Owner transfer needs
        /// SeRestorePrivilege, so on failure it is skipped and the DACL is still
        /// applied (the named account then simply gets an explicit allow ACE).
        /// </summary>
        public static void ApplyCreateAcl(string path, long mode,
            SecurityIdentifier? ownerSid, SecurityIdentifier? groupSid)
        {
            GetFileOwnerGroup(path, ref ownerSid, ref groupSid);
            if (ownerSid == null || groupSid == null)
                return; /* no identities to build the DACL from */

            uint ownerMask = ModeClassMask((mode >> 6) & 7);
            uint groupMask = ModeClassMask((mode >> 3) & 7);
            uint otherMask = ModeClassMask(mode & 7);

            byte[] dacl = BuildDacl(
                (ownerMask != 0, ownerSid, ownerMask),
                (groupMask != 0, groupSid, groupMask),
                (otherMask != 0, Everyone, otherMask));

            var ownerBytes = ownerSid.SidBytes();
            var groupBytes = groupSid.SidBytes();

            var ownerHandle = GCHandle.Alloc(ownerBytes, GCHandleType.Pinned);
            var groupHandle = GCHandle.Alloc(groupBytes, GCHandleType.Pinned);
            var daclHandle = GCHandle.Alloc(dacl, GCHandleType.Pinned);
            try
            {
                uint rc = SetNamedSecurityInfoW(path, SE_FILE_OBJECT,
                    OWNER_SECURITY_INFORMATION | GROUP_SECURITY_INFORMATION | DACL_SECURITY_INFORMATION,
                    ownerHandle.AddrOfPinnedObject(), groupHandle.AddrOfPinnedObject(),
                    daclHandle.AddrOfPinnedObject(), IntPtr.Zero);
                if (rc == 0)
                    return;

                /* a normal user may not seize ownership nor change the group
                 * (both need SeRestorePrivilege/WRITE_OWNER); the chmod part -
                 * an explicit DACL - merely needs WRITE_DAC and is what the
                 * mode actually boils down to. */
                rc = SetNamedSecurityInfoW(path, SE_FILE_OBJECT,
                    DACL_SECURITY_INFORMATION,
                    IntPtr.Zero, IntPtr.Zero,
                    daclHandle.AddrOfPinnedObject(), IntPtr.Zero);
                if (rc == 0)
                    return;

                Log.Message(MESS.ERROR, "setting ACL mode for {0}: failed ({1})\n",
                    path, Win32ErrorText(rc));
            }
            finally
            {
                ownerHandle.Free();
                groupHandle.Free();
                daclHandle.Free();
            }
        }

        /// <summary>
        /// chown() analog: hands the file over to the given owner/group when
        /// both were resolved, skipping the owner part when denied.
        /// </summary>
        public static bool ApplyOwnerGroup(string path,
            SecurityIdentifier? ownerSid, SecurityIdentifier? groupSid)
        {
            if (ownerSid == null && groupSid == null)
                return true;

            var ownerBytes = ownerSid?.SidBytes();
            var groupBytes = groupSid?.SidBytes();
            var ownerHandle = ownerBytes == null ? default : GCHandle.Alloc(ownerBytes, GCHandleType.Pinned);
            var groupHandle = groupBytes == null ? default : GCHandle.Alloc(groupBytes, GCHandleType.Pinned);
            try
            {
                uint rc = SetNamedSecurityInfoW(path, SE_FILE_OBJECT,
                    GROUP_SECURITY_INFORMATION
                        | (ownerHandle.IsAllocated ? OWNER_SECURITY_INFORMATION : 0),
                    ownerHandle.IsAllocated ? ownerHandle.AddrOfPinnedObject() : IntPtr.Zero,
                    groupHandle.IsAllocated ? groupHandle.AddrOfPinnedObject() : IntPtr.Zero,
                    IntPtr.Zero, IntPtr.Zero);
                return rc == 0;
            }
            finally
            {
                if (ownerHandle.IsAllocated) ownerHandle.Free();
                if (groupHandle.IsAllocated) groupHandle.Free();
            }
        }

        private static void GetFileOwnerGroup(string path,
            ref SecurityIdentifier? ownerSid, ref SecurityIdentifier? groupSid)
        {
            uint rc = GetNamedSecurityInfoW(path, SE_FILE_OBJECT,
                OWNER_SECURITY_INFORMATION | GROUP_SECURITY_INFORMATION,
                out IntPtr pOwner, out IntPtr pGroup, out _, out _, out IntPtr pSD);
            try
            {
                if (rc == 0 && pSD != IntPtr.Zero)
                {
                    if (ownerSid == null && pOwner != IntPtr.Zero)
                        ownerSid = ReadSid(pOwner);
                    if (groupSid == null && pGroup != IntPtr.Zero)
                        groupSid = ReadSid(pGroup);
                }
            }
            finally
            {
                if (pSD != IntPtr.Zero)
                    LocalFree(pSD);
            }
        }

        private static SecurityIdentifier? ReadSid(IntPtr pSid)
        {
            int length = GetLengthSid(pSid);
            if (length <= 0)
                return null;
            var bytes = new byte[length];
            Marshal.Copy(pSid, bytes, 0, length);
            return new SecurityIdentifier(bytes, 0);
        }

        private static uint ModeClassMask(long bits)
        {
            uint mask = 0;
            if ((bits & 4) != 0) mask |= GENERIC_READ;
            if ((bits & 2) != 0) mask |= GENERIC_WRITE;
            if ((bits & 1) != 0) mask |= GENERIC_EXECUTE;
            return mask;
        }

        private static byte[] BuildDacl(
            (bool Include, SecurityIdentifier Sid, uint Mask) owner,
            (bool Include, SecurityIdentifier Sid, uint Mask) group,
            (bool Include, SecurityIdentifier Sid, uint Mask) other)
        {
            var entries = new[]
            {
                owner, group, other
            };

            var sids = new byte[entries.Length][];
            int aceCount = 0;
            int size = 8; /* ACL header */
            for (int i = 0; i < entries.Length; i++)
            {
                sids[i] = entries[i].Sid.SidBytes();
                if (entries[i].Include)
                {
                    aceCount++;
                    size += 8 + sids[i].Length;
                }
            }

            var acl = new byte[size];
            acl[0] = 2;                            /* ACL_REVISION */
            acl[1] = 0;                            /* Sbz1 */
            acl[2] = (byte)(size & 0xFF);
            acl[3] = (byte)((size >> 8) & 0xFF);   /* AclSize */
            acl[4] = (byte)(aceCount & 0xFF);
            acl[5] = 0;                            /* AceCount */

            int offset = 8;
            for (int i = 0; i < entries.Length; i++)
            {
                if (!entries[i].Include)
                    continue;

                int aceLength = 8 + sids[i].Length;
                acl[offset] = ACCESS_ALLOWED_ACE_TYPE;
                acl[offset + 1] = 0;               /* AceFlags */
                acl[offset + 2] = (byte)(aceLength & 0xFF);
                acl[offset + 3] = (byte)((aceLength >> 8) & 0xFF);
                uint mask = entries[i].Mask;
                acl[offset + 4] = (byte)(mask & 0xFF);
                acl[offset + 5] = (byte)((mask >> 8) & 0xFF);
                acl[offset + 6] = (byte)((mask >> 16) & 0xFF);
                acl[offset + 7] = (byte)((mask >> 24) & 0xFF);
                Array.Copy(sids[i], 0, acl, offset + 8, sids[i].Length);
                offset += aceLength;
            }
            return acl;
        }

        private static string Win32ErrorText(uint rc)
        {
            var msg = new System.Text.StringBuilder(512);
            FormatMessageW(0x00001000 /* FORMAT_MESSAGE_FROM_SYSTEM */,
                IntPtr.Zero, rc, 0, msg, msg.Capacity, IntPtr.Zero);
            return rc + " " + msg.ToString().Trim();
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern int FormatMessageW(uint dwFlags, IntPtr lpSource,
            uint dwMessageId, uint dwLanguageId, System.Text.StringBuilder lpBuffer,
            int nSize, IntPtr arguments);

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

    /// <summary>
    /// Small helper to marshal a SID into its binary form.
    /// </summary>
    public static class SecurityIdentifierExtensions
    {
        public static byte[] SidBytes(this SecurityIdentifier sid)
        {
            var bytes = new byte[sid.BinaryLength];
            sid.GetBinaryForm(bytes, 0);
            return bytes;
        }
    }
}