using AutoIt;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation;

namespace percentagToAmp
{
    public static class MenuInvoker
    {
        public static async Task ClickDataLoadAsync(string windowTitle)
        {
            if (AutoItX.WinActivate(windowTitle) <= 0)
            {
                return;
            }

            // 1) 先展開「參數」
            var window = AutomationElement.RootElement.FindFirst(
                TreeScope.Children,
                new PropertyCondition(AutomationElement.NameProperty, windowTitle))
                ?? throw new InvalidOperationException("找不到視窗");

            var paramMenu = window.FindFirst(
                TreeScope.Descendants,
                new AndCondition(
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.MenuItem),
                    new OrCondition(
                        new PropertyCondition(AutomationElement.NameProperty, "參數 [A]"),
                        new PropertyCondition(AutomationElement.NameProperty, "參數(&A)")
                    )))
                ?? throw new InvalidOperationException("找不到『參數』");

            if (paramMenu.TryGetCurrentPattern(ExpandCollapsePattern.Pattern, out var pExp))
                ((ExpandCollapsePattern)pExp).Expand();
            else if (paramMenu.TryGetCurrentPattern(InvokePattern.Pattern, out var pInv))
                ((InvokePattern)pInv).Invoke();

            await Task.Delay(120); // 等彈出

            // 2) 鎖定彈出選單（只掃 Root 的 Children，極快）
            var popup = await FindPopupMenuAsync(windowTitle)
                ?? throw new InvalidOperationException("找不到彈出選單");

            // 3) 在彈出選單子樹找「數據加載」（一次 FindFirst）
            var dataLoad = popup.FindFirst(
                TreeScope.Subtree,
                new AndCondition(
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.MenuItem),
                    new OrCondition(
                        new PropertyCondition(AutomationElement.NameProperty, "數據加載"),
                        new PropertyCondition(AutomationElement.NameProperty, "數據加載 ") // 有些尾巴帶空白
                    )))
                ?? throw new InvalidOperationException("找不到『數據加載』");

            if (dataLoad.TryGetCurrentPattern(InvokePattern.Pattern, out var p))
                ((InvokePattern)p).Invoke();
            else
                Console.WriteLine( "");
        }

        static async Task<AutomationElement> FindPopupMenuAsync(string windowTitle, int timeoutMs = 1500)
        {
            var end = Environment.TickCount + timeoutMs;
            while (Environment.TickCount < end)
            {
                // 只掃 Root 的直接兒子，找彈出式菜單（ControlType.Menu 且可見）
                var menus = AutomationElement.RootElement.FindAll(
                    TreeScope.Children,
                    new AndCondition(
                        new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Menu),
                        new PropertyCondition(AutomationElement.IsOffscreenProperty, false)
                    ));

                if (menus.Count > 0) return menus.Cast<AutomationElement>().First();

                await Task.Delay(50);
            }
            return null;
        }

        /// <summary>
        /// 自動打開 menu 參數 [A] -> 數據加載
        /// </summary>
        /// <param name="hWnd"></param>
        /// <returns></returns>
        [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr hWnd);
        public static async Task ClickParamDataLoadAsync(string windowTitle)
        {
            // 找主視窗
            var window = AutomationElement.RootElement.FindFirst(
                TreeScope.Children,
                new PropertyCondition(AutomationElement.NameProperty, windowTitle))
                ?? throw new InvalidOperationException("找不到視窗：" + windowTitle);

            // 視窗置前（有些 app 需要）
            var hwnd = new IntPtr(window.Current.NativeWindowHandle);
            if (hwnd != IntPtr.Zero) SetForegroundWindow(hwnd);

            // 1) 先展開「參數 [A] / 參數(&A)」
            var paramMenu = window.FindFirst(
                TreeScope.Descendants,
                new AndCondition(
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.MenuItem),
                    new OrCondition(
                        new PropertyCondition(AutomationElement.NameProperty, "參數 [A]"),
                        new PropertyCondition(AutomationElement.NameProperty, "參數(&A)")
                    )))
                ?? throw new InvalidOperationException("找不到『參數』");

            if (paramMenu.TryGetCurrentPattern(ExpandCollapsePattern.Pattern, out var pExp))
                ((ExpandCollapsePattern)pExp).Expand();
            else if (paramMenu.TryGetCurrentPattern(InvokePattern.Pattern, out var pInv))
                ((InvokePattern)pInv).Invoke();

            await Task.Delay(120); // 給下拉選單時間彈出

            // 2) 只在彈出的 Menu 子樹找「數據加載/數據加載 」
            var popupMenu = FindPopupMenu() ?? window; // 找不到就退回整窗
            var dataLoad = popupMenu.FindFirst(
                TreeScope.Subtree,
                new AndCondition(
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.MenuItem),
                    new OrCondition(
                        new PropertyCondition(AutomationElement.NameProperty, "數據加載"),
                        new PropertyCondition(AutomationElement.NameProperty, "數據加載 ") // 有些結尾有空白
                    )))
                ?? throw new InvalidOperationException("找不到『數據加載』");

            if (!dataLoad.TryGetCurrentPattern(InvokePattern.Pattern, out var pDo))
                throw new InvalidOperationException("『數據加載』不支援 Invoke");

            ((InvokePattern)pDo).Invoke();
        }

        // 彈出式選單通常是單獨的 Menu 視窗（速度快很多）
        private static AutomationElement FindPopupMenu()
        {
            return AutomationElement.RootElement.FindFirst(
                TreeScope.Children,
                new AndCondition(
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Menu),
                    new PropertyCondition(AutomationElement.IsOffscreenProperty, false)
                ));
        }
    }
}
