using Newtonsoft.Json;
using NLog.Config;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static percentagToAmp.Utility;

namespace percentagToAmp
{

    public static class Config
    {
        public static string IpAddress { get; private set; } = string.Empty;
        public static int Port { get; private set; } = 0;
        public static string Factory { get; set; } = string.Empty;
        public static string Floor { get; set; } = string.Empty;
        public static string Area { get; set; } = string.Empty;
        public static string EqpNo { get; set; } = string.Empty;
        public static string AccessDBPath { get; private set; }
        public static Dictionary<VID, List<string>> Svid_Info { get; private set; } = new Dictionary<VID, List<string>>();

        public static void LoadConfig(string configPath)
        {
            string json = File.ReadAllText(configPath);
            var config = JsonConvert.DeserializeObject<ConfigRoot>(json);

            // IP & Port
            IpAddress = config.Connection.IpAddress;
            Port = config.Connection.Port;
            Factory = config.Connection.Factory;
            Floor = config.Connection.Floor;
            Area = config.Connection.Area;
            EqpNo = config.Connection.EqpNo;
            AccessDBPath = config.Connection.AccessDBPath;
            Svid_Info.Clear();
            foreach (var map in config.SvidMappings)
            {
                if (Enum.IsDefined(typeof(VID), map.Vid))
                {
                    VID vid = (VID)map.Vid;
                    Svid_Info[vid] = map.Controls;
                }
            }
        }

    }
    public class ConnectionConfig
    {
        public string IpAddress { get; set; } = string.Empty;
        public int Port { get; set; } = 0;
        public string Factory { get; set; } = string.Empty;
        public string Floor { get; set; } = string.Empty;
        public string Area { get; set; } = string.Empty;
        public string EqpNo { get; set; } = string.Empty;
        public string AccessDBPath { get; set; } = string.Empty;
    }
    public class SvidMapping
    {
        public int Vid { get; set; }
        public List<string> Controls { get; set; }
    }

    public class ConfigRoot
    {
        public ConnectionConfig Connection { get; set; }
        public List<SvidMapping> SvidMappings { get; set; }
    }
}
