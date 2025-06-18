using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace OblivionRemasterDownpatcher.Util
{
    public class NetworkMonitor
    {

        private Dictionary<string, long> _privateBytesReceivedPerNIC = [];
        private Dictionary<string, PerformanceCounter> _downloadCounterPerNIC  = [];

        public NetworkMonitor()
        {
            var nics = new PerformanceCounterCategory("Network Interface").GetInstanceNames();

            foreach(var nic in nics)
            {
                _downloadCounterPerNIC[nic] = new PerformanceCounter("Network Interface", "Bytes Received/sec", nic);
                _privateBytesReceivedPerNIC[nic] = (long)_downloadCounterPerNIC[nic].NextValue();
            }
        }

        public double GetDownloadSpeedMbps()
        {
            double max_speed = 0.0;
            foreach (var nic in _privateBytesReceivedPerNIC.Keys)
            {
                var previousBytesReceived = _privateBytesReceivedPerNIC[nic];
                var bytesReceived = (long)_downloadCounterPerNIC[nic].NextValue();

                var speed = (bytesReceived - previousBytesReceived) / 125_000;
                max_speed = Math.Max(max_speed, speed);
            }

            return max_speed;
        }
    }
}
