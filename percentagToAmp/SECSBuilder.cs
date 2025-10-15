using AutoIt;
using Newtonsoft.Json;
using Secs4Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace percentagToAmp
{
    public static class SECSBuilder
    {
        // 依你的程式習慣命名
        private static class Ceid
        {
            public const string MoveIn = "10001"; //Move IN MES定義
            public const string MachineStatus = "10002";
        }

        private static class RptId
        {
            public const string MoveIn = "100"; // 與你原碼一致
            public const string MachineStatus = "101";
        }

        /// <summary>
        /// 建 S6F11 (Event Report) 訊息。
        /// 內容格式: L  [A:TIME, A:CEID, L [ L [A:RPTID, L [ ...報告欄位 ] ] ] ]
        /// </summary>
        public static SecsMessage BuildS6F11Message(string command, IReadOnlyDictionary<string, string> paramInfo)
        {
            if (string.IsNullOrWhiteSpace(command))
                throw new ArgumentException("command 不可為空。", nameof(command));

            // 統一大小寫比對
            command = command.Trim().ToUpperInvariant();

            // 事件時間
            var timeA = Item.A(DateTime.Now.ToString("yyyyMMddHHmmssffff"));
            Item payload; // 這是整個 S6F11 的 SecsItem
            string name = string.Empty;
            name = command;
            switch (command)
            {
                case "MOVE_IN":
                    {

                        // 1) 先收集傳入參數 (注意：Dictionary 枚舉順序在 .NET Core 之後通常維持插入序，但規格上不保證。
                        //    若順序很重要，建議呼叫端改傳「有序集合」或額外傳參數順序索引。)
                        var values = (paramInfo?.Values ?? Array.Empty<string>()).ToList();

                        // 2) 取 S1F3 的機台資訊 (1012/1013/1015)
                        var svids = new List<string> { "1012", "1013", "1015" };
                        var machineValues = ViewModels.mainModelView.GetS1F3MachineValue(svids) ?? new List<string>();
                        values.AddRange(machineValues);

                        // 3) 報告: RPTID=100, 內容 = values 每個轉成 A
                        payload = MakeS6F11Payload(
                            time: timeA,
                            ceid: Ceid.MoveIn,
                            report: MakeReport(RptId.MoveIn, values)
                        );
                        break;
                    }

                case "MACHINE_STATUS":
                    {
                        if (paramInfo == null || !paramInfo.TryGetValue("STATUS", out var status) || string.IsNullOrEmpty(status))
                            throw new ArgumentException("MACHINE_STATUS 需要 Param_Info[\"STATUS\"].", nameof(paramInfo));

                        payload = MakeS6F11Payload(
                            time: timeA,
                            ceid: Ceid.MachineStatus,
                            report: MakeReport(RptId.MachineStatus, new[] { status })
                        );
                        break;
                    }

                default:
                    throw new NotSupportedException($"未知的 command：{command}");
            }

            // 建立 S6F11 訊息
            return new SecsMessage((byte)6, (byte)11)
            {
                Name = name,
                SecsItem = payload
            };
        }

        //== 小工具：把 RPTID + 欄位，組成 L[A:RPTID, L[...欄位...]]
        private static Item MakeReport(string rptId, IEnumerable<string> fields)
        {
            var fieldItems = (fields ?? Array.Empty<string>()).Select(Item.A).ToArray();
            return Item.L(
                Item.A(rptId),
                Item.L(fieldItems)
            );
        }

        //== 小工具：把 S6F11 的 3 大段組起來: L[A:TIME, A:CEID, L[ (Report)... ]]
        private static Item MakeS6F11Payload(Item time, string ceid, Item report)
        {
            // 可支援多個 Report 時改為 L[ report1, report2, ... ]
            return Item.L(
                time,
                Item.A(ceid),
                Item.L( // 單一 Report 的外層
                    report
                )
            );
        }

        // 你的既有實作（保留）
        //private static List<string> GetS1F3MachineValue(IEnumerable<string> svidInfo)
        //{
        //    //var result = new List<string>();

        //    //try
        //    //{
        //    //    bool triggeredKeyboard = false;
        //    //    bool needClick = false;
        //    //    string machineState = string.Empty;

        //    //    // 1. 處理一般 SVID (1000~1020)
        //    //    foreach (string key in svidInfo)
        //    //    {
        //    //        if (_Svid_Info.ContainsKey(key) &&
        //    //            int.TryParse(key, out int num) &&
        //    //            num >= 1000 && num <= 1020)
        //    //        {
        //    //            AutoItX.WinActivate(_Svid_Info["1001"][0]);

        //    //            if (!triggeredKeyboard)
        //    //            {
        //    //                ControlKeyboard();
        //    //                triggeredKeyboard = true;
        //    //            }

        //    //            string value = AutoItX.ControlGetText(
        //    //                _Svid_Info[key][0], "", _Svid_Info[key][1]);

        //    //            result.Add(value);
        //    //            needClick = true;
        //    //        }
        //    //    }

        //    //    if (needClick)
        //    //    {
        //    //        AutoItX.ControlClick(_Svid_Info["1001"][0], "",
        //    //            "[CLASS:WindowsForms10.BUTTON.app.0.34f5582_r9_ad1; INSTANCE:4]");
        //    //    }

        //    //    // 2. 特殊 SVID: 1022 → DataGridView
        //    //    foreach (string key in svidInfo)
        //    //    {
        //    //        if (_Svid_Info.ContainsKey(key) &&
        //    //            int.TryParse(key, out int num) &&
        //    //            num == 1022)
        //    //        {
        //    //            FocusAndClickButton();

        //    //            var consoleProcessValue = GetConsoleProcessValue(
        //    //                $"{AppDomain.CurrentDomain.BaseDirectory}\\UIControl\\_AutoCIMOrigin.exe",
        //    //                string.Join(",", "GetDataGridView", _Svid_Info[key][0], _Svid_Info[key][1]));

        //    //            if (consoleProcessValue.Key)
        //    //            {
        //    //                result.Add(consoleProcessValue.Value);
        //    //            }
        //    //            else
        //    //            {
        //    //                result.Add(string.Empty);
        //    //                MainForm.LogHelper.WriteLog(
        //    //                    $"取得DataGridView錯誤! 結果:{consoleProcessValue.Key} 訊息:{consoleProcessValue.Value}");
        //    //            }
        //    //        }
        //    //    }

        //    //    // 3. 特殊 SVID: 1021 → 判斷機台狀態
        //    //    foreach (string key in svidInfo)
        //    //    {
        //    //        if (_Svid_Info.ContainsKey(key) &&
        //    //            int.TryParse(key, out int num) &&
        //    //            num == 1021)
        //    //        {
        //    //            if (!_AlarmFlag)
        //    //            {
        //    //                FocusAndClickButton();

        //    //                var consoleProcessValue = GetConsoleProcessValue(
        //    //                    $"{AppDomain.CurrentDomain.BaseDirectory}\\UIControl\\_AutoCIMOrigin.exe",
        //    //                    string.Join(",", "GetDataGridView", _Svid_Info[key][0], _Svid_Info[key][1]));

        //    //                if (consoleProcessValue.Key)
        //    //                {
        //    //                    var info = JsonConvert.DeserializeObject<MainForm.AutoCIMDataGridInfo>(
        //    //                        consoleProcessValue.Value);

        //    //                    if (info.Rtn_Result.Trim().Equals("TRUE", StringComparison.OrdinalIgnoreCase) &&
        //    //                        info.Rtn_Value != null &&
        //    //                        info.Rtn_Value.Count > 0)
        //    //                    {
        //    //                        // 檢查第 10~14 列 (index 9~13) 的 Col9/Col10/Col11
        //    //                        if (info.Rtn_Value.Count >= 21)
        //    //                        {
        //    //                            machineState = "IDLE";
        //    //                            for (int i = 9; i <= 13; i++)
        //    //                            {
        //    //                                var row = info.Rtn_Value[i];
        //    //                                if (row.ContainsKey("Col9") && row.ContainsKey("Col10") && row.ContainsKey("Col11"))
        //    //                                {
        //    //                                    if (!string.IsNullOrEmpty(row["Col9"]) &&
        //    //                                        !string.IsNullOrEmpty(row["Col10"]) &&
        //    //                                        !string.IsNullOrEmpty(row["Col11"]))
        //    //                                    {
        //    //                                        machineState = "RUN";
        //    //                                        break;
        //    //                                    }
        //    //                                }
        //    //                            }
        //    //                        }
        //    //                        else
        //    //                        {
        //    //                            machineState = "IDLE";
        //    //                        }
        //    //                    }
        //    //                    else
        //    //                    {
        //    //                        machineState = "IDLE";
        //    //                    }
        //    //                }
        //    //                else
        //    //                {
        //    //                    machineState = string.Empty;
        //    //                }
        //    //            }
        //    //            else
        //    //            {
        //    //                machineState = "ALARM";
        //    //            }

        //    //            result.Add(machineState);
        //    //        }
        //    //    }
        //    //}
        //    //catch (Exception ex)
        //    //{
        //    //    result.Clear();
        //    //    //MainForm.LogHelper.WriteLog($"取得機台SVID流程異常! 原因【{ex}】");
        //    //}

        //    return null;
        //}
    }
}
