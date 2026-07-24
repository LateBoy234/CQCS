using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ThreeNH.ColorQualityControl.MultiHelper
{
    public static class CommHelper
    {
        public static bool Assert(ushort value, ushort max, ushort min)
        {
            return (value <= max) && (value >= min);
        }
        public static bool ValidateIPv4Regex(string ip)
        {
            string pattern = @"^(25[0-5]|2[0-4][0-9]|1[0-9]{2}|[1-9]?[0-9])\.(25[0-5]|2[0-4][0-9]|1[0-9]{2}|[1-9]?[0-9])\.(25[0-5]|2[0-4][0-9]|1[0-9]{2}|[1-9]?[0-9])\.(25[0-5]|2[0-4][0-9]|1[0-9]{2}|[1-9]?[0-9])$";
            var result = Regex.IsMatch(ip, pattern);
            return result;
        }
        public static bool ValidateIpWithPort(string ipPort)
        {
            // 正则匹配 IPv4:Port 格式
            string pattern =
                @"^((25[0-5]|2[0-4][0-9]|1[0-9]{2}|[1-9]?[0-9])\.){3}" +
                @"(25[0-5]|2[0-4][0-9]|1[0-9]{2}|[1-9]?[0-9]):" +
                @"([0-9]{1,4}|[1-5][0-9]{4}|6[0-4][0-9]{3}|65[0-4][0-9]{2}|655[0-2][0-9]|6553[0-5])$";

            var result = Regex.IsMatch(ipPort, pattern);
            return result;
        }
        public static async Task<bool> IsIPReachableAsync(string ipAddress)
        {
            try
            {
                return await Task.Run(() =>
                {
                    Ping pingSender = new Ping();
                    var reply = pingSender.Send(ipAddress);
                    if (reply != null && reply.Status == IPStatus.Success)
                    {
                        Console.WriteLine($"The network at {ipAddress} is reachable.");
                        return true;
                    }
                    else
                    {
                        Console.WriteLine($"The network at {ipAddress} is unreachable or timed out.");
                        return false;
                    }
                });
            }
            catch 
            {
                return false;
            }

        }
    }
}
