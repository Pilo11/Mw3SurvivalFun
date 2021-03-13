using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace FunExecuter
{
    internal static class MemoryManager
    {

        const int PROCESS_ALL = 0x001F0FFF;
        const int T_KEY = 0x54; //This is the T key.
        const int Z_KEY = 0x5A; //This is the Z key.
        const int U_KEY = 0x55; //This is the U key.
        const int I_KEY = 0x49; //This is the I key.
        const int P_KEY = 0x50; //This is the P key.

        [Flags]
        internal enum AllocationType
        {
            Commit = 0x1000,
            Reserve = 0x2000,
            Decommit = 0x4000,
            Release = 0x8000,
            Reset = 0x80000,
            Physical = 0x400000,
            TopDown = 0x100000,
            WriteWatch = 0x200000,
            LargePages = 0x20000000
        }

        [Flags]
        internal enum MemoryProtection
        {
            Execute = 0x10,
            ExecuteRead = 0x20,
            ExecuteReadWrite = 0x40,
            ExecuteWriteCopy = 0x80,
            NoAccess = 0x01,
            ReadOnly = 0x02,
            ReadWrite = 0x04,
            WriteCopy = 0x08,
            GuardModifierflag = 0x100,
            NoCacheModifierflag = 0x200,
            WriteCombineModifierflag = 0x400
        }

        [DllImport("kernel32.dll")]
        static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, AllocationType flAllocationType, MemoryProtection flProtect);

        [DllImport("kernel32.dll")]
        static extern bool ReadProcessMemory(int hProcess, int lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteProcessMemory(IntPtr hProcess, int lpBaseAddress, byte[] lpBuffer, int nSize, int lpNumberOfBytesWritten);

        [DllImport("User32.dll")]
        static extern short GetAsyncKeyState(int vKey);

        internal static IntPtr AllocSpace(IntPtr handle, uint byteArraySize)
        {
            return VirtualAllocEx(handle, IntPtr.Zero, byteArraySize, AllocationType.Commit | AllocationType.Reserve, MemoryProtection.ExecuteReadWrite);
        }

        internal static bool IsTKeyClicked()
        {
            var keyState = GetAsyncKeyState(T_KEY);
            return ((keyState >> 15) & 0x0001) == 0x0001;
        }

        internal static bool IsZKeyClicked()
        {
            var keyState = GetAsyncKeyState(Z_KEY);
            return ((keyState >> 15) & 0x0001) == 0x0001;
        }

        internal static bool IsUKeyClicked()
        {
            var keyState = GetAsyncKeyState(U_KEY);
            return ((keyState >> 15) & 0x0001) == 0x0001;
        }

        internal static bool IsIKeyClicked()
        {
            var keyState = GetAsyncKeyState(I_KEY);
            return ((keyState >> 15) & 0x0001) == 0x0001;
        }

        internal static bool IsPKeyClicked()
        {
            var keyState = GetAsyncKeyState(P_KEY);
            return ((keyState >> 15) & 0x0001) == 0x0001;
        }

        internal static byte[] GetByteArrayFromMemory(IntPtr handle, int address, int length)
        {
            int bytesRead = 0;
            byte[] buffer = new byte[length];
            ReadProcessMemory(handle.ToInt32(), address, buffer, buffer.Length, ref bytesRead);
            return buffer;
        }

        internal static void WriteByteArrayFromMemory(IntPtr handle, int address, byte[] bytes)
        {
            WriteProcessMemory(handle, address, bytes, bytes.Length, 0);
        }

        internal static IntPtr GetProcHandle()
        {
            return OpenProcess(PROCESS_ALL, true, Process.GetProcessesByName("iw5sp").FirstOrDefault()?.Id ?? 0);
        }

    }
}