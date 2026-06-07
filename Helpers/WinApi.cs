using System.Runtime.InteropServices;
using System.Text;

namespace ProcessExplorerPro.Helpers
{
    public static class WinApi
    {
        public const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;
        public const uint PROCESS_QUERY_INFORMATION = 0x0400;
        public const uint PROCESS_VM_READ = 0x0010;

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, int processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool QueryFullProcessImageName(IntPtr hProcess, int dwFlags, StringBuilder lpExeName, ref int lpdwSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetProcessHandleCount(IntPtr hProcess, out uint pdwHandleCount);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetGuiResources(IntPtr hProcess, uint uiFlags);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

        public const uint TOKEN_QUERY = 0x0008;

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool GetTokenInformation(
            IntPtr TokenHandle,
            int TokenInformationClass,
            IntPtr TokenInformation,
            int TokenInformationLength,
            out int ReturnLength);

        // TokenInformationClass values
        public const int TokenIntegrityLevel = 25;

        [StructLayout(LayoutKind.Sequential)]
        public struct SID_AND_ATTRIBUTES
        {
            public IntPtr Sid;
            public uint Attributes;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TOKEN_MANDATORY_LABEL
        {
            public SID_AND_ATTRIBUTES Label;
        }

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern IntPtr GetSidSubAuthority(IntPtr pSid, uint nSubAuthority);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern IntPtr GetSidSubAuthorityCount(IntPtr pSid);

        public const uint SECURITY_MANDATORY_UNTRUSTED_RID = 0x00000000;
        public const uint SECURITY_MANDATORY_LOW_RID = 0x00001000;
        public const uint SECURITY_MANDATORY_MEDIUM_RID = 0x00002000;
        public const uint SECURITY_MANDATORY_HIGH_RID = 0x00003000;
        public const uint SECURITY_MANDATORY_SYSTEM_RID = 0x00004000;
        public const uint SECURITY_MANDATORY_PROTECTED_PROCESS_RID = 0x00005000;

        public static string GetProcessImageName(int processId)
        {
            IntPtr hProcess = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, processId);
            if (hProcess == IntPtr.Zero)
            {
                return "Access Denied";
            }

            try
            {
                int capacity = 2048;
                StringBuilder sb = new StringBuilder(capacity);
                if (QueryFullProcessImageName(hProcess, 0, sb, ref capacity))
                {
                    return sb.ToString();
                }
            }
            finally
            {
                CloseHandle(hProcess);
            }

            return "Unknown";
        }

        public static int GetProcessHandles(int processId)
        {
            IntPtr hProcess = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, processId);
            if (hProcess == IntPtr.Zero)
            {
                return 0;
            }

            try
            {
                if (GetProcessHandleCount(hProcess, out uint handleCount))
                {
                    return (int)handleCount;
                }
            }
            finally
            {
                CloseHandle(hProcess);
            }

            return 0;
        }

        public static string GetProcessIntegrityLevel(int processId)
        {
            IntPtr hProcess = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, processId);
            if (hProcess == IntPtr.Zero)
            {
                return "Unknown (Access Denied)";
            }

            IntPtr hToken = IntPtr.Zero;
            try
            {
                if (!OpenProcessToken(hProcess, TOKEN_QUERY, out hToken))
                {
                    return "Unknown (Token Access Denied)";
                }

                int dwLength = 0;
                // Query length first
                GetTokenInformation(hToken, TokenIntegrityLevel, IntPtr.Zero, 0, out dwLength);
                if (dwLength == 0) return "Medium";

                IntPtr pTokenInfo = Marshal.AllocHGlobal(dwLength);
                try
                {
                    if (GetTokenInformation(hToken, TokenIntegrityLevel, pTokenInfo, dwLength, out dwLength))
                    {
                        TOKEN_MANDATORY_LABEL tml = Marshal.PtrToStructure<TOKEN_MANDATORY_LABEL>(pTokenInfo);
                        IntPtr pSubAuthorityCount = GetSidSubAuthorityCount(tml.Label.Sid);
                        int subAuthorityCount = Marshal.ReadByte(pSubAuthorityCount);
                        IntPtr pRid = GetSidSubAuthority(tml.Label.Sid, (uint)subAuthorityCount - 1);
                        uint rid = (uint)Marshal.ReadInt32(pRid);

                        if (rid >= SECURITY_MANDATORY_PROTECTED_PROCESS_RID) return "Protected";
                        if (rid >= SECURITY_MANDATORY_SYSTEM_RID) return "System";
                        if (rid >= SECURITY_MANDATORY_HIGH_RID) return "High";
                        if (rid >= SECURITY_MANDATORY_MEDIUM_RID) return "Medium";
                        if (rid >= SECURITY_MANDATORY_LOW_RID) return "Low";
                        return "Untrusted";
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(pTokenInfo);
                }
            }
            catch
            {
                // Fallback
            }
            finally
            {
                if (hToken != IntPtr.Zero) CloseHandle(hToken);
                CloseHandle(hProcess);
            }

            return "Medium";
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct IO_COUNTERS
        {
            public ulong ReadOperationCount;
            public ulong WriteOperationCount;
            public ulong OtherOperationCount;
            public ulong ReadTransferCount;
            public ulong WriteTransferCount;
            public ulong OtherTransferCount;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetProcessIoCounters(IntPtr hProcess, out IO_COUNTERS lpIoCounters);

        public static (long ReadBytes, long WriteBytes) GetProcessIoBytes(int processId)
        {
            IntPtr hProcess = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, processId);
            if (hProcess == IntPtr.Zero)
            {
                return (0, 0);
            }

            try
            {
                if (GetProcessIoCounters(hProcess, out IO_COUNTERS ioCounters))
                {
                    return ((long)ioCounters.ReadTransferCount, (long)ioCounters.WriteTransferCount);
                }
            }
            finally
            {
                CloseHandle(hProcess);
            }

            return (0, 0);
        }

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool LookupAccountSid(
            string? lpSystemName,
            IntPtr lpSid,
            StringBuilder lpName,
            ref int cchName,
            StringBuilder lpReferencedDomainName,
            ref int cchReferencedDomainName,
            out int peUse);

        public static string GetProcessOwner(int processId)
        {
            if (processId <= 4) return "SYSTEM";

            IntPtr hProcess = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, processId);
            if (hProcess == IntPtr.Zero)
            {
                return "SYSTEM";
            }

            IntPtr hToken = IntPtr.Zero;
            try
            {
                if (OpenProcessToken(hProcess, TOKEN_QUERY, out hToken))
                {
                    int tokenInfoLength = 0;
                    GetTokenInformation(hToken, 1, IntPtr.Zero, 0, out tokenInfoLength);
                    if (tokenInfoLength > 0)
                    {
                        IntPtr pTokenUser = Marshal.AllocHGlobal(tokenInfoLength);
                        try
                        {
                            if (GetTokenInformation(hToken, 1, pTokenUser, tokenInfoLength, out tokenInfoLength))
                            {
                                IntPtr pSid = Marshal.ReadIntPtr(pTokenUser);
                                StringBuilder name = new StringBuilder(256);
                                int nameLen = name.Capacity;
                                StringBuilder domain = new StringBuilder(256);
                                int domainLen = domain.Capacity;
                                int use;

                                if (LookupAccountSid(null, pSid, name, ref nameLen, domain, ref domainLen, out use))
                                {
                                    string user = name.ToString();
                                    string dom = domain.ToString();
                                    return string.IsNullOrEmpty(dom) ? user : $"{dom}\\{user}";
                                }
                            }
                        }
                        finally
                        {
                            Marshal.FreeHGlobal(pTokenUser);
                        }
                    }
                }
            }
            catch
            {
                // Fallback
            }
            finally
            {
                if (hToken != IntPtr.Zero) CloseHandle(hToken);
                CloseHandle(hProcess);
            }

            return "SYSTEM";
        }
    }
}
