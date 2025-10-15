using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static percentagToAmp.Utility;
using System.IO;

namespace percentagToAmp
{

    public static class Config
    {
        public static string IpAddress { get; private set; }
        public static int Port { get; private set; }
        public static string AccessDBPath { get; private set; }
        public static Dictionary<VID, List<string>> Svid_Info { get; private set; } = new Dictionary<VID, List<string>>();

        public static void LoadConfig(string configPath)
        {
            string json = File.ReadAllText(configPath);
            var config = JsonConvert.DeserializeObject<ConfigRoot>(json);

            // IP & Port
            IpAddress = config.Connection.IpAddress;
            Port = config.Connection.Port;
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
        public string IpAddress { get; set; }
        public int Port { get; set; }
        public string AccessDBPath { get; set; }
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
