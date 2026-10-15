using Protocol.Drivers.Mitsubishi;
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

        public PLCMcProtocol() : this(Config.PlcIpAddress, Config.PlcPort)
        {
        }

        public PLCMcProtocol(string ip, int port)
        {
            PLCTcp = new Mitsubishi.McProtocolTcp(ip, port, Mitsubishi.McFrame.MC3E);
            PLCTcp.Open();
        }

        public async Task<int> ReadD(int address)
        {
            int result = await PLCTcp.GetDevice(Mitsubishi.PlcDeviceType.D, address);

            if (result != 0)
                throw new Exception($"PLC Error: {result}");

            return PLCTcp.Device;
        }
    }

    /// <summary>
    /// PLC 控制器，基於 Protocol.Drivers.Mitsubishi (MC Protocol Qna-3E)
    /// 連線參數預設自 config.json 載入
    /// </summary>
    public class PLCMXController : IDisposable
    {
        private McDriver plc;
        private readonly object _lock = new object();
        private string? _customIp;
        private int? _customPort;

        public string Ip => _customIp ?? Config.PlcIpAddress;
        public int Port => _customPort ?? Config.PlcPort;

        public bool IsConnected => plc?.IsConnected ?? false;

        public PLCMXController()
        {
            plc = new McDriver();
        }

        public PLCMXController(string ip, int port)
        {
            _customIp = ip;
            _customPort = port;
            plc = new McDriver();
        }

        /// <summary>
        /// 初始化 PLC 連線（MC 協議 TCP 連線）
        /// </summary>
        public bool Initialize(string? ip = null, int? port = null)
        {
            if (!string.IsNullOrEmpty(ip))
                _customIp = ip;
            if (port.HasValue && port.Value > 0)
                _customPort = port.Value;

            return EnsureConnected();
        }

        /// <summary>
        /// 相容舊版呼叫介面
        /// </summary>
        public bool Initialize(int logicalStationNo)
        {
            return EnsureConnected();
        }

        /// <summary>
        /// 連線檢查機制
        /// </summary>
        private bool EnsureConnected()
        {
            lock (_lock)
            {
                if (plc != null && plc.IsConnected)
                    return true;

                try
                {
                    plc?.Disconnect();
                }
                catch { }

                try
                {
                    if (plc == null)
                        plc = new McDriver();

                    string targetIp = Ip;
                    int targetPort = Port;

                    bool connected = plc.ConnectAsync(targetIp, targetPort).GetAwaiter().GetResult();

                    if (connected)
                        CyntecLog.GeneralLogger.Info($"PLC Reconnect Success ({targetIp}:{targetPort})");
                    else
                        CyntecLog.GeneralLogger.Warn($"PLC Reconnect Fail ({targetIp}:{targetPort})");

                    return connected;
                }
                catch (Exception ex)
                {
                    CyntecLog.GeneralLogger.Error(ex);
                    return false;
                }
            }
        }

        /// <summary>
        /// 讀取 D (16-bit Word)
        /// </summary>
        public int ReadD(ePlcDevice address)
        {
            if (!EnsureConnected())
                return 0;

            try
            {
                ushort value = plc.ReadWordAsync($"D{(int)address}").GetAwaiter().GetResult();
                return (short)value;
            }
            catch (Exception ex)
            {
                try { plc?.Disconnect(); } catch { }
                CyntecLog.GeneralLogger.Warn($"PLC Read Fail: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// 讀取 D 暫存器並依比例換算，具備 3 次 Retry 機制
        /// </summary>
        public double ReadDWithScale(ePlcDevice address, double scale)
        {
            const int maxRetry = 3;

            for (int retry = 1; retry <= maxRetry; retry++)
            {
                try
                {
                    if (!EnsureConnected())
                        continue;

                    ushort value = plc.ReadWordAsync($"D{(int)address}").GetAwaiter().GetResult();
                    return Math.Round((double)value * scale, 2);
                }
                catch (Exception ex)
                {
                    try { plc?.Disconnect(); } catch { }
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
        /// 寫入 D (16-bit Word)
        /// </summary>
        public bool WriteD(ePlcDevice address, int value)
        {
            if (!EnsureConnected())
                return false;

            try
            {
                plc.WriteWordAsync($"D{(int)address}", (ushort)value).GetAwaiter().GetResult();
                return true;
            }
            catch (Exception ex)
            {
                try { plc?.Disconnect(); } catch { }
                CyntecLog.GeneralLogger.Warn($"PLC Write Fail: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 讀取 32 位元 DWord (2 個 Word)
        /// </summary>
        public int ReadDWord(ePlcDevice addr)
        {
            if (!EnsureConnected())
                return 0;

            try
            {
                ushort low = plc.ReadWordAsync($"D{(int)addr}").GetAwaiter().GetResult();
                ushort high = plc.ReadWordAsync($"D{(int)addr + 1}").GetAwaiter().GetResult();
                return (high << 16) | (low & 0xFFFF);
            }
            catch (Exception ex)
            {
                try { plc?.Disconnect(); } catch { }
                CyntecLog.GeneralLogger.Warn($"PLC ReadDWord Fail: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// 寫入 32 位元 DWord (2 個 Word)
        /// </summary>
        public bool WriteDWord(ePlcDevice lowAddr, int value)
        {
            if (!EnsureConnected())
                return false;

            try
            {
                ushort low = (ushort)(value & 0xFFFF);
                ushort high = (ushort)((value >> 16) & 0xFFFF);

                plc.WriteWordAsync($"D{(int)lowAddr}", low).GetAwaiter().GetResult();
                plc.WriteWordAsync($"D{(int)lowAddr + 1}", high).GetAwaiter().GetResult();
                return true;
            }
            catch (Exception ex)
            {
                try { plc?.Disconnect(); } catch { }
                CyntecLog.GeneralLogger.Warn($"PLC WriteDWord Fail: {ex.Message}");
                return false;
            }
        }

        public void Dispose()
        {
            try
            {
                plc?.Dispose();
            }
            catch { }
        }
    }
}
