using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Windows;

namespace percentagToAmp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private Mutex? _singleMutex;

        private const string AppGuid = "9f1a67e9-8c1a-4b5a-9d7a-123456789abc"; // 換成你自己的固定 GUID

        private void App_OnStartup(object sender, StartupEventArgs e)
        {
            // 建議用「每位使用者」的唯一名，避免不同使用者互相影響
            string userSid = WindowsIdentity.GetCurrent().User?.Value ?? "nouser";
            string mutexName = $@"Local\{AppGuid}_{userSid}";

            bool createdNew;
            _singleMutex = new Mutex(true, mutexName, out createdNew);

            if (!createdNew)
            {
                // 已有實例 → 嘗試把現有視窗帶到前景，然後結束自己
                BringExistingToFront();
                Shutdown();
                return;
            }

            var win = new MainWindow();
            MainWindow = win;
            win.Show();
        }

        private void App_OnExit(object? sender, ExitEventArgs e)
        {
            try { _singleMutex?.ReleaseMutex(); } catch { }
            _singleMutex?.Dispose();
        }

        // ===== 將已存在的實例帶到前景 =====
        private static void BringExistingToFront()
        {
            var me = Process.GetCurrentProcess();
            var name = me.ProcessName; // exe 名稱(不含副檔名)

            foreach (var p in Process.GetProcessesByName(name))
            {
                if (p.Id == me.Id) continue;
                IntPtr h = p.MainWindowHandle;
                if (h == IntPtr.Zero) continue;

                // 若最小化，先還原
                if (IsIconic(h)) ShowWindow(h, SW_RESTORE);
                SetForegroundWindow(h);
                break;
            }
        }

        const int SW_RESTORE = 9;
        [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        [DllImport("user32.dll")] private static extern bool IsIconic(IntPtr hWnd);
        protected override void OnStartup(StartupEventArgs e)
        {
            // UI執行緒上的未處理例外（WPF）
            this.DispatcherUnhandledException += (s, ex) =>
            {
                LogError("DispatcherUnhandledException", ex.Exception);
                // 告知使用者，避免整個程式關閉
                ShowNonBlockingError("發生未預期錯誤，已記錄。您可繼續使用。");
                ex.Handled = true; // 重要：標記為已處理，避免關閉
            };

            // 非UI執行緒 / 背景 Thread 的未處理例外
            AppDomain.CurrentDomain.UnhandledException += (s, ex) =>
            {
                LogError("UnhandledException", ex.ExceptionObject as Exception);
                // 這裡**無法**完全阻止終止；可選擇嘗試自動重啟
                //TryRestart("偵測到嚴重錯誤，將自動重新啟動。");
            };

            // Task 未觀察的例外（async 忘了 await）
            TaskScheduler.UnobservedTaskException += (s, ex) =>
            {
                LogError("UnobservedTaskException", ex.Exception);
                ex.SetObserved(); // 標記已處理，避免升級為未處理例外
            };

            base.OnStartup(e);
        }
        private void LogError(string tag, Exception? ex)
        {
            // TODO: 你用的 NLog / Serilog
            CyntecLog.GeneralLogger.Error($"{tag} - {ex}");
        }

        private void ShowNonBlockingError(string msg)
        {
            // 避免 MessageBox 卡住 UI。可改為 Snackbar / 狀態列提示。
            // new ToastWindow(msg).Show();
        }
    }

}
