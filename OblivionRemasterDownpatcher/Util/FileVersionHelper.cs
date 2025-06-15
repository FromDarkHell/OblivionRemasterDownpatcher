using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OblivionRemasterDownpatcher.Util
{
    static class FileVersionHelper
    {
        [DllImport("version", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool GetFileVersionInfoEx(Int32 dwFlags, IntPtr lpwstrFilename, Int32 dwHandle, Int32 dwLen, IntPtr lpData);

        [DllImport("version", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern Int32 GetFileVersionInfoSizeEx(Int32 dwFlags, IntPtr lpwstrFilename, IntPtr lpdwHandle);

        [DllImport("version", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool VerQueryValue(IntPtr pBlock, IntPtr lpSubBlock, IntPtr lplpBuffer, UIntPtr puLen);

        public static string GetProductVersion(string filename)
        {
            // There's apparently a bug with `FileVersionInfo.GetVersionInfo()` where if the `FileVersion` is an empty
            //  string, the `ProductVersion` will be forced to one too, even if it has a value, hence the need for this

            IntPtr filePtr = Marshal.StringToCoTaskMemAuto(filename);
            Int32 size = GetFileVersionInfoSizeEx(0, filePtr, IntPtr.Zero);
            if (size == 0)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            IntPtr infoPtr = Marshal.AllocHGlobal(size);
            bool success = GetFileVersionInfoEx(0, filePtr, 0, size, infoPtr);
            if (!success)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            IntPtr verPtr = Marshal.AllocHGlobal(IntPtr.Size);
            success = VerQueryValue(infoPtr, Marshal.StringToHGlobalAuto("\\VarFileInfo\\Translation"), verPtr, UIntPtr.Zero);
            if (!success)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            IntPtr langPtr = Marshal.ReadIntPtr(verPtr);
            Int32 langCode = (UInt16)Marshal.ReadInt16(langPtr) << 16 | (UInt16)Marshal.ReadInt16(IntPtr.Add(langPtr, 2));

            success = VerQueryValue(
              infoPtr,
              Marshal.StringToHGlobalAuto($"\\StringFileInfo\\{langCode:X8}\\ProductVersion"),
              verPtr,
              UIntPtr.Zero
            );
            if (!success)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            string? output = Marshal.PtrToStringAuto(Marshal.ReadIntPtr(verPtr));

            Marshal.FreeHGlobal(verPtr);
            Marshal.FreeHGlobal(infoPtr);

            return output ?? "";
        }
    }
}
