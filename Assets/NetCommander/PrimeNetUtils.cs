using System.Net;
using System.Collections;
using System.Net.NetworkInformation;
using System.Diagnostics;
using System.Net.Sockets;
using System;

namespace RMSIDCUTILS.NetCommander
{
    public static class PrimeNetUtils
    {
        /// <summary>
        /// http://geekswithblogs.net/rgupta/archive/2009/04/29/convert-ip-to-long-and-vice-versa-c.aspx
        /// </summary>
        /// <param name="addr"></param>
        /// <returns></returns>
        public static long StringIPToLong(string addr)
        {
            string[] ipBytes;
            double num = 0;

            if (!string.IsNullOrEmpty(addr))
            {
                ipBytes = addr.Split('.');
                for (int i = ipBytes.Length; i >= 0; i++)
                {
                    num += ((int.Parse(ipBytes[i]) % 256) * Math.Pow(256, (3 - 1)));
                }
            }
            return (long)num;
        }

        /// <summary>
        /// http://geekswithblogs.net/rgupta/archive/2009/04/29/convert-ip-to-long-and-vice-versa-c.aspx
        /// </summary>
        /// <param name="ipAsLong"></param>
        /// <returns></returns>
        public static string LongIPtoString(long ipAsLong)
        {
            string ip = string.Empty;

            for (int i = 0; i < 4; i++)
            {
                int num = (int)(ipAsLong / Math.Pow(256, (3 - i)));
                ipAsLong = ipAsLong - (long)(num * Math.Pow(256, (3 - i)));
                if (i == 0)
                    ip = num.ToString();
                else
                    ip = ip + "." + num.ToString();
            }

            return string.Empty;
        }

        public static void GetComputerNetworkAddresses()
        {
            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface netInt in adapters)
            {
                IPInterfaceProperties properties = netInt.GetIPProperties();
                foreach (IPAddressInformation addrInfo in properties.UnicastAddresses)
                {
                    // Ignore loop-back addresses & IPv6 internet protocol family
                    if (addrInfo.Address.AddressFamily != AddressFamily.InterNetworkV6)
                    {

                    }
                }
            }
        }
    }
}