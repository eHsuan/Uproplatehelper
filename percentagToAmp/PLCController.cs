using ActUtlTypeLib;
using MCProtocol;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace percentagToAmp
{
    public class PLCMcProtocol
    {
        public Mitsubishi.McProtocolTcp PLCTcp;

        public PLCMcProtocol()
        {
            PLCTcp = new Mitsubishi.McProtocolTcp("192.168.1.2", 5000, Mitsubishi.McFrame.MC3E);
            PLCTcp.Open();
        }

        //public int ReadD()
        //{
        //    int value = 0;
        //    value = PLCTcp.GetDevice(Mitsubishi.PlcDeviceType.D, 2000);

        //    return value;
        //}

        public async Task<int> ReadD(int address)
        {
            int result = await PLCTcp.GetDevice(Mitsubishi.PlcDeviceType.D, address);

            if (result != 0)
                throw new Exception($"PLC Error: {result}");

            return PLCTcp.Device;
        }
    }

    public class PLCMXController : IDisposable
    {
        private ActUtlType plc;
        private readonly object _lock = new object();

        public bool IsConnected { get; private set; }

        public PLCMXController()
        {
            plc = new ActUtlType();
            IsConnected = false;         
        }

        /// <summary>
        /// 初始化 PLC 連線（使用 Logical Station）
        /// </summary>
        public bool Initialize(int logicalStationNo = 1)
        {
            try
            {
                plc.ActLogicalStationNumber = logicalStationNo;

                int ret = plc.Open();

                if (ret != 0)
                {
                    IsConnected = false;
                    CyntecLog.GeneralLogger.Debug($"PLC Initialize Fail");
                    return false;
                }

                IsConnected = true;
                return true;
            }
            catch (Exception ex)
            {
                IsConnected = false;
                CyntecLog.GeneralLogger.Debug($"PLC Initialize Fail , {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// 2026/07/09 連線檢查機制
        /// </summary>
        /// <returns></returns>
        private bool EnsureConnected()
        {
            lock (_lock)
            {
                if (IsConnected)
                    return true;

                try
                {
                    plc.Close();
                }
                catch { }

                try
                {
                    int ret = plc.Open();

                    IsConnected = (ret == 0);

                    if (IsConnected)
                        CyntecLog.GeneralLogger.Info("PLC Reconnect Success");
                    else
                        CyntecLog.GeneralLogger.Warn($"PLC Reconnect Fail:{ret}");

                    return IsConnected;
                }
                catch (Exception ex)
                {
                    CyntecLog.GeneralLogger.Error(ex);
                    IsConnected = false;
                    return false;
                }
            }
        }

        /// <summary>
        /// 讀取 D
        /// </summary> 
        public int ReadD(ePlcDevice address)
        {
            if (!EnsureConnected())
                return 0;

            int value;

            int ret = plc.GetDevice($"D{(int)address}", out value);

            if (ret != 0)
            {
                IsConnected = false;
                return 0;
            }

            return value;
        }

        //WC 2026/07/09 新增重新連線，Retry 三次，避免 PLC 斷線時，資料為0
        public double ReadDWithScale(ePlcDevice address, double scale)
        {
            const int maxRetry = 3;

            for (int retry = 1; retry <= maxRetry; retry++)
            {
                try
                {
                    if (!EnsureConnected())
                        continue;

                    int value;
                    int ret = plc.GetDevice($"D{(int)address}", out value);

                    if (ret == 0)
                        return Math.Round(value * scale, 2);    //WC 2026/06/23 只保留小數兩位

                    // 通訊失敗，標記為斷線，下次重新連線
                    IsConnected = false;

                    CyntecLog.GeneralLogger.Warn($"PLC Read Fail. Retry={retry}/{maxRetry}, Address=D{(int)address}, ErrorCode={ret}");
                }
                catch (Exception ex)
                {
                    IsConnected = false;
                    CyntecLog.GeneralLogger.Warn($"PLC Read Exception. Retry={retry}/{maxRetry}, Address=D{(int)address}, {ex.Message}");
                }

                // 避免立即重試造成 PLC 或網路負擔
                if (retry < maxRetry)
                    System.Threading.Thread.Sleep(100);
            }

            CyntecLog.GeneralLogger.Error($"PLC Read Failed After {maxRetry} Retries. Address=D{(int)address}");
            return 0;
        }

        /// <summary>
        /// 寫入 D
        /// </summary>
        public bool WriteD(ePlcDevice address, int value)
        {
            if (!EnsureConnected())
                return false;

            int ret = plc.SetDevice($"D{(int)address}", value);

            if (ret != 0)
            {
                IsConnected = false;
                return false;
            }

            return true;
        }

        public int ReadDWord(ePlcDevice addr)
        {
            if (!EnsureConnected())
                return 0;

            int[] buffer = new int[2];

            int ret = plc.ReadDeviceBlock($"D{(int)addr}", 2, out buffer[0]);

            if (ret != 0)
            {
                IsConnected = false;
                return 0;
            }

            return (buffer[1] << 16) | (buffer[0] & 0xFFFF);
        }

        public bool WriteDWord(ePlcDevice lowAddr, int value)
        {
            if (!EnsureConnected())
                return false;

            int[] buffer = new int[2];

            buffer[0] = value & 0xFFFF;          // Low
            buffer[1] = (value >> 16) & 0xFFFF;  // High

            int ret = plc.WriteDeviceBlock("D" + (int)lowAddr, 2, ref buffer[0]);

            if (ret != 0)
            {
                IsConnected = false;
                return false;
            }

            return true;
        }

        public void Dispose()
        {
            try
            {
                if (plc != null && IsConnected)
                {
                    plc.Close();
                    IsConnected = false;
                }
            }
            catch { }
        }       
    }
}
