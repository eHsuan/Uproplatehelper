using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

namespace percentagToAmp
{
    public static class ForegroundHelper
    {
        public static void BringToFront(Window w)
        {
            if (w == null) return;

            var hwnd = new WindowInteropHelper(w).Handle;
            if (hwnd == IntPtr.Zero) return;

            // 還原最小化
            if (IsIconic(hwnd)) ShowWindow(hwnd, SW_RESTORE);

            // 讓視窗浮到最上，再取消（常用小技巧）
            w.Topmost = true;  // 只瞬間設一下
            w.Topmost = false;

            // 取得前景視窗的 thread，暫時附著，提升成功率
            var fg = GetForegroundWindow();
            uint foreThread = GetWindowThreadProcessId(fg, IntPtr.Zero);
            uint appThread = GetCurrentThreadId();
            AttachThreadInput(appThread, foreThread, true);
            SetForegroundWindow(hwnd);
            AttachThreadInput(appThread, foreThread, false);

            // 再保險叫一次 Activate
            w.Activate();
        }

        const int SW_RESTORE = 9;

        [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        [DllImport("user32.dll")] static extern bool IsIconic(IntPtr hWnd);
        [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr lpdwProcessId);
        [DllImport("kernel32.dll")] static extern uint GetCurrentThreadId();
        [DllImport("user32.dll")] static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);
    }
}
