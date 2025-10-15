using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Automation;

namespace percentagToAmp
{
    public static class WinAuto
    {
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_RESTORE = 9; // 如果被最小化，就還原
        public static void clickDataLoad()
        {
            // 找視窗
            AutomationElement mainWindow = AutomationElement.RootElement.FindFirst(
                TreeScope.Children,
                new PropertyCondition(AutomationElement.NameProperty, "Data Monitoring System - [Data Monitoring]"));

            if (mainWindow == null)
            {
                CyntecLog.GeneralLogger.Error("找不到主視窗");
                return;
            }

            // 把視窗叫到前景
            IntPtr hWnd = new IntPtr(mainWindow.Current.NativeWindowHandle);
            if (hWnd != IntPtr.Zero)
            {
                ShowWindow(hWnd, SW_RESTORE); // 確保不是最小化
                SetForegroundWindow(hWnd);    // 把它帶到前景
            }

            // 找 "Data Load" 按鈕
            var condBtn = new AndCondition(
                new PropertyCondition(AutomationElement.NameProperty, "Data Load"),
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button));

            AutomationElement btnDataLoad = mainWindow.FindFirst(TreeScope.Descendants, condBtn);

            if (btnDataLoad == null)
            {
                CyntecLog.GeneralLogger.Error("找不到 Data Load 按鈕");
                return;
            }

            // 執行 Click
            if (btnDataLoad.GetCurrentPattern(InvokePattern.Pattern) is InvokePattern invoke)
            {
                invoke.Invoke();
                CyntecLog.GeneralLogger.Error("已點擊 Data Load");
            }
            else
            {
                CyntecLog.GeneralLogger.Error("按鈕不支援 Invoke");
            }
            Thread.Sleep(400);
        }

        public static void click清除當前()
        {
            // 找主視窗
            var mainWindow = AutomationElement.RootElement.FindFirst(
                TreeScope.Children,
                new PropertyCondition(AutomationElement.NameProperty, "Data Monitoring System - [Data Monitoring]"));

            if (mainWindow == null)
            {
                Console.WriteLine("找不到主視窗");
                return;
            }

            // 找子視窗（數據加載）
            var dialog = mainWindow.FindFirst(
                TreeScope.Descendants,
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window));
            if (dialog == null)
            {
                Console.WriteLine("找不到子視窗");
                return;
            }

            // 直接用 AutomationId 抓按鈕
            var btn = dialog.FindFirst(
                TreeScope.Descendants,
                new AndCondition(
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button),
                    new PropertyCondition(AutomationElement.AutomationIdProperty, "btnClearCurrent") // 這裡換成你的按鈕 ID
                ));

            if (btn == null)
            {
                Console.WriteLine("找不到 清除當前 按鈕");
                return;
            }

            // 點擊
            if (btn.GetCurrentPattern(InvokePattern.Pattern) is InvokePattern invoke)
            {
                invoke.Invoke();
                Console.WriteLine("已點擊 清除當前");
            }
            else
            {
                Console.WriteLine("按鈕不支援 Invoke");
            }
        }

        public static void click加載()
        {
            // 找視窗
            AutomationElement mainWindow = AutomationElement.RootElement.FindFirst(
                TreeScope.Children,
                new PropertyCondition(AutomationElement.NameProperty, "Data Monitoring System - [Data Monitoring]"));

            if (mainWindow == null)
            {
                Console.WriteLine("找不到主視窗");
                return;
            }

            // 把視窗叫到前景
            IntPtr hWnd = new IntPtr(mainWindow.Current.NativeWindowHandle);
            if (hWnd != IntPtr.Zero)
            {
                ShowWindow(hWnd, SW_RESTORE); // 確保不是最小化
                SetForegroundWindow(hWnd);    // 把它帶到前景
            }

            // 2) 在主視窗底下找「可見的子視窗」（很多 WinForms/WPF 對話框會掛在這裡）
            AutomationElement dialog = mainWindow.FindFirst(
                TreeScope.Descendants,
                new AndCondition(
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window),
                    new PropertyCondition(AutomationElement.IsOffscreenProperty, false)
                ));



            if (dialog == null) throw new Exception("找不到子視窗/對話框");

            // 3) 先抓所有 Button（有些自訂控件不是 Button，後面會降級找）
            var allButtons = dialog.FindAll(TreeScope.Descendants,
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button));

            AutomationElement target = null;

            // 3a) 以 Name 完整比對
            for (int i = 0; i < allButtons.Count; i++)
            {
                var e = allButtons[i];
                if (string.Equals(e.Current.Name?.Trim(), "加載", StringComparison.Ordinal))
                {
                    target = e; break;
                }
            }

            if (target == null) throw new Exception("還是找不到「加載」這顆，可能是自訂控件未暴露名稱");

            // 執行 Click
            if (target.GetCurrentPattern(InvokePattern.Pattern) is InvokePattern invoke)
            {
                invoke.Invoke();
                Console.WriteLine("已點擊 清除當前");
            }
            else
            {
                Console.WriteLine("按鈕不支援 Invoke");
            }
        }

        public static void click退出()
        {
            // 找視窗
            AutomationElement mainWindow = AutomationElement.RootElement.FindFirst(
                TreeScope.Children,
                new PropertyCondition(AutomationElement.NameProperty, "Data Monitoring System - [Data Monitoring]"));

            if (mainWindow == null)
            {
                Console.WriteLine("找不到主視窗");
                return;
            }

            // 把視窗叫到前景
            IntPtr hWnd = new IntPtr(mainWindow.Current.NativeWindowHandle);
            if (hWnd != IntPtr.Zero)
            {
                ShowWindow(hWnd, SW_RESTORE); // 確保不是最小化
                SetForegroundWindow(hWnd);    // 把它帶到前景
            }

            // 2) 在主視窗底下找「可見的子視窗」（很多 WinForms/WPF 對話框會掛在這裡）
            AutomationElement dialog = mainWindow.FindFirst(
                TreeScope.Descendants,
                new AndCondition(
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window),
                    new PropertyCondition(AutomationElement.IsOffscreenProperty, false)
                ));

            // 若知道標題可再加一條 NameProperty 比對，例如：
            // new PropertyCondition(AutomationElement.NameProperty, "數據加載")

            if (dialog == null) throw new Exception("找不到子視窗/對話框");

            // 3) 先抓所有 Button（有些自訂控件不是 Button，後面會降級找）
            var allButtons = dialog.FindAll(TreeScope.Descendants,
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button));

            AutomationElement target = null;

            // 3a) 以 Name 完整比對
            for (int i = 0; i < allButtons.Count; i++)
            {
                var e = allButtons[i];
                if (string.Equals(e.Current.Name?.Trim(), "退出", StringComparison.Ordinal))
                {
                    target = e; break;
                }
            }

            if (target == null) throw new Exception("還是找不到「退出」這顆，可能是自訂控件未暴露名稱");

            // 執行 Click
            if (target.GetCurrentPattern(InvokePattern.Pattern) is InvokePattern invoke)
            {
                invoke.Invoke();
                Console.WriteLine("已點擊 清除當前");
            }
            else
            {
                Console.WriteLine("按鈕不支援 Invoke");
            }
        }

        public static bool click彈出視窗確定()
        {
            var mainWindowTitle = "Data Monitoring System - [Data Monitoring]";
            // 找標題是「提示」的對話框
            // 1) 找主視窗
            // 1) 找主視窗，取同一個 ProcessId
            var main = AutomationElement.RootElement.FindFirst(
                TreeScope.Children,
                new AndCondition(
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window),
                    new PropertyCondition(AutomationElement.NameProperty, mainWindowTitle)
                ));
            if (main == null) 
                return false;

            int pid = main.Current.ProcessId;

            // 2) 在桌面頂層找同一個 Process 的視窗，標題為「提示」或「Information」
            var msgBox = AutomationElement.RootElement.FindFirst(
                TreeScope.Children,
                new AndCondition(
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window),
                    new PropertyCondition(AutomationElement.ProcessIdProperty, pid),
                    new OrCondition(
                        new PropertyCondition(AutomationElement.NameProperty, "提示"),
                        new PropertyCondition(AutomationElement.NameProperty, "Information")
                    )
                ));
            if (msgBox == null)
            {
                // 有些對話框不是頂層（或標題被裝飾），退一步從主視窗往下找
                msgBox = main.FindFirst(
                    TreeScope.Descendants,
                    new OrCondition(
                        new PropertyCondition(AutomationElement.NameProperty, "提示"),
                        new PropertyCondition(AutomationElement.NameProperty, "Information")
                    ));
                if (msgBox == null) 
                    return false;
            }

            // 3) 找「確定 / OK」按鈕
            var okBtn = msgBox.FindFirst(
                TreeScope.Descendants,
                new AndCondition(
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button),
                    new OrCondition(
                        new PropertyCondition(AutomationElement.NameProperty, "確定"),
                        new PropertyCondition(AutomationElement.NameProperty, "確定(&O)"),
                        new PropertyCondition(AutomationElement.NameProperty, "OK")
                    )
                ));
            if (okBtn == null) 
                return false;

            // 4) 觸發按鈕
            if (okBtn.TryGetCurrentPattern(InvokePattern.Pattern, out var p) && p is InvokePattern inv)
            {
                inv.Invoke();
                return true;
            }

            return false;
        }
        /// <summary>
        /// 在「數據加載」對話框中輸入資料編號 (cmbFB1PartNumber)
        /// </summary>
        /// <param name="mainWindowTitle">主視窗標題，例如 "Data Monitoring System - [Data Monitoring]"</param>
        /// <param name="value">要輸入的資料編號字串</param>
        /// <returns>成功 true / 失敗 false</returns>
        public static bool SetPartNumber(string value)
        {
            var mainWindowTitle = "Data Monitoring System - [Data Monitoring]";
            // 1) 找主視窗
            var main = AutomationElement.RootElement.FindFirst(
                TreeScope.Children,
                new PropertyCondition(AutomationElement.NameProperty, mainWindowTitle));
            if (main == null) 
                return false;

            // 2) 找子視窗「數據加載」
            var dialog = main.FindFirst(TreeScope.Descendants,
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window));
            if (dialog == null) 
                return false;

            // 3) 找 ComboBox (AutomationId = cmbFB1PartNumber)
            var combo = dialog.FindFirst(TreeScope.Descendants,
                new AndCondition(
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.ComboBox),
                    new PropertyCondition(AutomationElement.AutomationIdProperty, "cmbFB1PartNumber")
                ));
            if (combo == null) 
                return false;

            // 4) 嘗試展開
            if (combo.TryGetCurrentPattern(ExpandCollapsePattern.Pattern, out var ep) && ep is ExpandCollapsePattern ecp)
            {
                try { ecp.Expand(); } catch { /* 有些不支援 Expand 會例外 */ }
            }

            // 5) 嘗試 ValuePattern 設值
            if (combo.TryGetCurrentPattern(ValuePattern.Pattern, out var vp) && vp is ValuePattern val)
            {
                val.SetValue(value);
                return true;
            }
            return true;
        }
        public static bool SelectFirstItem(string comboAutomationId /* ex: cmbLoadTankProcess */)
        {
            var mainWindowTitle = "Data Monitoring System - [Data Monitoring]";
            var main = AutomationElement.RootElement.FindFirst(
                TreeScope.Children,
                new PropertyCondition(AutomationElement.NameProperty, mainWindowTitle));
            if (main == null) 
                return false;

            // 找到那顆 ComboBox（建議用 AutomationId；若不知道可改用 ClassName / NNClassName）
            var combo = main.FindFirst(
                TreeScope.Descendants,
                new AndCondition(
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.ComboBox),
                    new PropertyCondition(AutomationElement.AutomationIdProperty, comboAutomationId)
                ));
            if (combo == null) 
                return false;

            // 展開下拉
            if (combo.TryGetCurrentPattern(ExpandCollapsePattern.Pattern, out var ep) && ep is ExpandCollapsePattern ecp)
            {
                try { ecp.Expand(); } catch { /* 有些狀況可能已展開 */ }
            }

            // 在 Combo 下找 ListItem
            var items = combo.FindAll(TreeScope.Subtree,
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.ListItem));
            if (items.Count == 0) return false;

            var first = items[0];
            if (first.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var sp) && sp is SelectionItemPattern sel)
            {
                sel.Select();
                return true;
            }

            // 後援：雙擊第一項
            if (first.TryGetCurrentPattern(InvokePattern.Pattern, out var ip) && ip is InvokePattern inv)
            {
                inv.Invoke();
                return true;
            }

            return false;
        }
    }
}
