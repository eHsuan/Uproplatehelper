using Microsoft.Extensions.Logging;
using NLog;
using NLog.Config;
using NLog.Targets;
using System;
using System.Text;
using LogLevel = NLog.LogLevel;

namespace percentagToAmp
{
    public static class CyntecLog
    {
        public static Logger GeneralLogger { get; private set; } = null!;
        public static Logger SecsLogger { get; private set; } = null!;

        static CyntecLog()
        {
            Init();
        }
        public static void Init()
        {
            var config = new LoggingConfiguration();

            // Console log
            var debugTarget = new NLog.Targets.DebuggerTarget("debug")
            {
                Layout = "${longdate} | ${level:uppercase=true} | ${message} ${exception:format=tostring}"
            };
            config.AddRule(LogLevel.Trace, LogLevel.Fatal, debugTarget, "General*");

            // 一般 log
            var generalFile = new FileTarget("generalFile")
            {
                FileName = "${basedir}/logs/general/${shortdate}.log",
                Layout = "${longdate} | ${level:uppercase=true} | ${message} ${exception:format=tostring}",
                Encoding = Encoding.UTF8
            };
            config.AddRule(LogLevel.Trace, LogLevel.Fatal, generalFile, "General*"); 
            // Rule：General* → 同時寫檔 & Console
            config.AddRule(LogLevel.Trace, LogLevel.Fatal, debugTarget, "General*");
            // 專門給 SECS 的 log
            var secsFile = new FileTarget("secsFile")
            {
                FileName = "${basedir}/logs/secs/${shortdate}.log",
                Layout = "${longdate} | ${level:uppercase=true} | ${message}",
                Encoding = Encoding.UTF8
            };
            // 這裡用 loggerNamePattern 過濾 → 只有名字以 "SECS" 開頭的 logger 才會寫進來
            config.AddRule(LogLevel.Trace, LogLevel.Fatal, secsFile, "SECS*");

            // 套用設定
            LogManager.Configuration = config;

            // 取 logger
            GeneralLogger = LogManager.GetLogger("General");
            SecsLogger = LogManager.GetLogger("SECS");
        }
    }
}
