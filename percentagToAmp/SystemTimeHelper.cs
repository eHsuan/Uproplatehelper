using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace percentagToAmp.ViewModels
{
    internal static class SystemTimeHelper
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct SYSTEMTIME
        {
            public ushort wYear;
            public ushort wMonth;
            public ushort wDayOfWeek;
            public ushort wDay;
            public ushort wHour;
            public ushort wMinute;
            public ushort wSecond;
            public ushort wMilliseconds;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetLocalTime(
            ref SYSTEMTIME lpSystemTime);

        /// <summary>
        /// 設定 Windows 本機系統時間
        /// date 格式：yyyyMMddHHmmss
        /// 例如：20260819120000
        /// </summary>
        public static bool SetSystemTime(string date, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(date))
            {
                errorMessage = "時間不可為空白。";
                return false;
            }

            DateTime targetTime;

            if (!DateTime.TryParseExact(
                date.Trim(),
                "yyyyMMddHHmmss",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out targetTime))
            {
                errorMessage =
                    $"時間格式錯誤：{date}，預期格式 yyyyMMddHHmmss";

                return false;
            }

            SYSTEMTIME systemTime = new SYSTEMTIME
            {
                wYear = (ushort)targetTime.Year,
                wMonth = (ushort)targetTime.Month,
                wDay = (ushort)targetTime.Day,
                wHour = (ushort)targetTime.Hour,
                wMinute = (ushort)targetTime.Minute,
                wSecond = (ushort)targetTime.Second,
                wMilliseconds = 0
            };

            bool result = SetLocalTime(ref systemTime);

            if (!result)
            {
                int errorCode = Marshal.GetLastWin32Error();

                errorMessage =
                    $"SetLocalTime 失敗，Win32 Error={errorCode}";

                return false;
            }

            return true;
        }
    }
}