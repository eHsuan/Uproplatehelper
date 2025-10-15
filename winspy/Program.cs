// See https://aka.ms/new-console-template for more information
using System.Runtime.InteropServices;
using System.Text;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

class WinSpy
{
    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

    [DllImport("user32.dll")]
    static extern bool EnumChildWindows(IntPtr hWndParent, EnumChildProc lpEnumFunc, IntPtr lParam);
    delegate bool EnumChildProc(IntPtr hwnd, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    static string GetWindowTextSafe(IntPtr h)
    {
        var sb = new StringBuilder(512);
        GetWindowText(h, sb, sb.Capacity);
        return sb.ToString();
    }

    static string GetClassSafe(IntPtr h)
    {
        var sb = new StringBuilder(256);
        GetClassName(h, sb, sb.Capacity);
        return sb.ToString();
    }

    static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        // 視窗標題（改成你的）
        string title = args.Length > 0 ? string.Join(' ', args) : "Data Monitoring System - [Data Monitoring]";
        IntPtr hMain = FindWindow(null, title);
        if (hMain == IntPtr.Zero)
        {
            Console.WriteLine($"找不到視窗：{title}");
            return;
        }

        Console.WriteLine($"主視窗: 0x{hMain.ToInt64():X}  \"{title}\"");
        Console.WriteLine("列舉子視窗…\n");

        // 記錄每個 class 出現次數，用來算 INSTANCE
        var classCounter = new Dictionary<string, int>(StringComparer.Ordinal);

        // BFS/DFS 都行；這裡用遞迴 + EnumChildWindows
        Traverse(hMain, 0, classCounter);
    }

    static void Traverse(IntPtr parent, int depth, Dictionary<string, int> classCounter)
    {
        EnumChildWindows(parent, (hwnd, l) =>
        {
            string cls = GetClassSafe(hwnd);
            string txt = GetWindowTextSafe(hwnd);
            // 計算 INSTANCE：同 ClassName 遞增
            int instance = 1;
            if (classCounter.TryGetValue(cls, out var n))
                instance = n + 1;
            classCounter[cls] = instance;

            string indent = new string(' ', depth * 2);
            string handleHex = $"0x{hwnd.ToInt64():X}";

            Console.WriteLine($"{indent}- Handle:{handleHex}  Class:{cls}  INSTANCE:{instance}  Text:\"{txt}\"");

            // 繼續往下
            Traverse(hwnd, depth + 1, classCounter);
            return true;
        }, IntPtr.Zero);
    }
}