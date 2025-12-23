using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static percentagToAmp.Utility;

namespace percentagToAmp
{

    public static class ConfigMoveOut
    {
        private static string configPath = "ConfigMoveOut.json";

        public static Dictionary<string, MoveOutInfo> MoveOutInfos { get; set; } = new Dictionary<string, MoveOutInfo>();

        public static void LoadConfig()
        {
            if (!File.Exists(configPath))
            {
                MoveOutInfos = new Dictionary<string, MoveOutInfo>();
                return;
            }

            try
            {
                string json = File.ReadAllText(configPath);
                var config = JsonConvert.DeserializeObject<ConfigMoveOutRoot>(json);
                MoveOutInfos = config?.MoveOutInfos ?? new Dictionary<string, MoveOutInfo>();
            }
            catch (Exception)
            {
                // 若讀取或解析失敗，保留空的設定而不拋出
                MoveOutInfos = new Dictionary<string, MoveOutInfo>();
            }
        }

        public static void SaveConfig()
        {
            const int daysToKeep = 1; // 保留最近 1 天的記錄
            try
            {
                RemoveOutdatedMoveOutInfo(MoveOutInfos, daysToKeep);
                // 將設定包在 root 類別中序列化，與 LoadConfig 的反序列化相匹配
                var root = new ConfigMoveOutRoot { MoveOutInfos = MoveOutInfos ?? new Dictionary<string, MoveOutInfo>() };
                string json = JsonConvert.SerializeObject(root, Formatting.Indented);
                File.WriteAllText(configPath, json);
            }
            catch (Exception)
            {
                // 忽略寫檔錯誤或可改為記錄日誌
            }
        }


        public static void RemoveOutdatedMoveOutInfo(Dictionary<string, MoveOutInfo> dict, int days)
        {
            if (dict == null || dict.Count == 0)
                return;

            var threshold = DateTime.Now.AddDays(-days);

            var keysToRemove = dict
                .Where(kv => kv.Value.LastExeTime < threshold)
                .Select(kv => kv.Key)
                .ToList();

            foreach (var key in keysToRemove)
                dict.Remove(key);
        }     
    }

    public class ConfigMoveOutRoot
    {
        public Dictionary<string, MoveOutInfo> MoveOutInfos { get; set; } = new Dictionary<string, MoveOutInfo>();
    }

    public class MoveOutInfo
    {
        public string LotNo { get; set; } = string.Empty;
        public string NewLotNo { get; set; } = string.Empty;
        public string UserID { get; set; } = string.Empty;
        public string Count { get; set; } = string.Empty;
        public string TargetNo { get; set; } = string.Empty;
        public string LeftRack { get; set; } = string.Empty;
        public string RightRack { get; set; } = string.Empty;
        public string SlotNo { get; set; } = string.Empty;
        public string CurrentA { get; set; } = string.Empty;
        public string CurrentB { get; set; } = string.Empty;
        public string PlanTime { get; set; } = string.Empty;
  
        public DateTime LastExeTime { get; set; } = DateTime.MinValue;
    }
}
