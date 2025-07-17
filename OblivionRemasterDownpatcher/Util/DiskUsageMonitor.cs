using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OblivionRemasterDownpatcher.Util
{
    public class DiskUsageMonitor
    {
        private struct IO_COUNTERS
        {
            public ulong ReadOperationCount;
            public ulong WriteOperationCount;
            public ulong OtherOperationCount;
            public ulong ReadTransferCount;
            public ulong WriteTransferCount;
            public ulong OtherTransferCount;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetProcessIoCounters(IntPtr ProcessHandle, out IO_COUNTERS IoCounters);

        private IntPtr processHandle;

        private double lastUsage;
        private DateTime lastCheckTime = DateTime.MinValue;

        public DiskUsageMonitor(Process process)
        {
            processHandle = process.Handle;
        }

        public double GetDiskUsageMbps()
        {
            IO_COUNTERS ioC1 = new IO_COUNTERS();
            GetProcessIoCounters(processHandle, out ioC1);

            double currentUsage = (ioC1.ReadTransferCount + ioC1.WriteTransferCount) / 1024f / 1024f;

            if (lastCheckTime == DateTime.MinValue)
            {
                lastCheckTime = DateTime.UtcNow;
                lastUsage = currentUsage;
                return 0.0;
            }

            var currentTime = DateTime.UtcNow;
            double delaySeconds = (currentTime - lastCheckTime).TotalSeconds;

            var usagePerSeconds = (currentUsage - lastUsage) / delaySeconds;

            lastCheckTime = currentTime;
            lastUsage = currentUsage;

            return Math.Round(usagePerSeconds, 2);
        }
    }
}
