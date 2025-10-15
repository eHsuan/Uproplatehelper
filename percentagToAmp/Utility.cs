using System;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using Condition = System.Windows.Automation.Condition;


namespace percentagToAmp
{
    public class Utility
    {
        [DllImport("user32.dll")] static extern int GetSystemMetrics(int nIndex);
        [DllImport("user32.dll")] static extern IntPtr GetDesktopWindow();
        [DllImport("user32.dll")] static extern IntPtr GetWindowDC(IntPtr hWnd);
        [DllImport("gdi32.dll")] static extern int BitBlt(IntPtr hdcDest, int x, int y, int cx, int cy, IntPtr hdcSrc, int x1, int y1, int rop);
        [DllImport("user32.dll")] static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        const int SM_XVIRTUALSCREEN = 76;
        const int SM_YVIRTUALSCREEN = 77;
        const int SM_CXVIRTUALSCREEN = 78;
        const int SM_CYVIRTUALSCREEN = 79;
        const int SRCCOPY = 0x00CC0020;
        const int CAPTUREBLT = 0x40000000;

        public static void CaptureAllScreens(string prefix, int order)
        {
            var datetimestr = DateTime.Now.ToString("yyyyMMdd");
            string path = Path.Combine($"screenshot", $"{datetimestr}", $"{prefix}-{order:D2}-{DateTime.Now.ToString("yyyyMMddHHmmssffff")}.jpg");
            CaptureAllScreens(path);
        }

        public static void CaptureAllScreens(string path)
        {
            int left = GetSystemMetrics(SM_XVIRTUALSCREEN);
            int top = GetSystemMetrics(SM_YVIRTUALSCREEN);
            int width = GetSystemMetrics(SM_CXVIRTUALSCREEN);
            int height = GetSystemMetrics(SM_CYVIRTUALSCREEN);

            using var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using var g = Graphics.FromImage(bmp);
            var hdcDest = g.GetHdc();
            var hdcSrc = GetWindowDC(GetDesktopWindow());

            BitBlt(hdcDest, 0, 0, width, height, hdcSrc, left, top, SRCCOPY | CAPTUREBLT);

            g.ReleaseHdc(hdcDest);
            ReleaseDC(GetDesktopWindow(), hdcSrc);
            if (path == null) return;
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            try
            {
                bmp.Save(path, ImageFormat.Jpeg);
            }
            catch (Exception) { }
        
        }

        public enum VID
        {
            加載槽號 = 1000,
            資料編號,
            流程編號,
            面積A,
            總面積,
            批號,
            流程名稱,
            面積B,
            數量,
            模式,
            A面電流密度ASD = 1010,
            B面電流密度ASD,
            A面設定電流,
            B面設定電流,
            電流保護百分比,
            設定電鍍時間,
            Index,
            機台狀態 = 1021,
            DataGridView,

            清除當前 = 10000,
            加載,
            退出,
            警告DGV
        }

        //public static Dictionary<string, ControlInfo> Svid_Info_ { get; private set; }
        public static Dictionary<VID, List<string>> Svid_Info { get; private set; }
        static Utility()
        {
            //Svid_Info_ = new Dictionary<string, ControlInfo>();
            //Svid_Info_.Add("清除當前", new ControlInfo() { 
            //    BasicWindowsInfoTitle = "數據加載",
            //    BasicWindowsInfoClass = "WindowsForms10.Window.8.app.0.34f5582_r7_ad1",
            //    AdvancedClass = "[NAME:btnClearCurrent]"
            //});
            //Svid_Info_.Add("加載槽號", new ControlInfo()
            //{
            //    BasicWindowsInfoTitle = "數據加載",
            //    BasicWindowsInfoClass = "WindowsForms10.Window.8.app.0.34f5582_r7_ad1",
            //    AdvancedClass = "[NAME:cmbLoadTankProcess]"
            //});
            //Svid_Info_.Add("資料編號", new ControlInfo()
            //{
            //    BasicWindowsInfoTitle = "數據加載",
            //    BasicWindowsInfoClass = "WindowsForms10.Window.8.app.0.34f5582_r7_ad1",
            //    AdvancedClass = "[CLASS:Edit; INSTANCE:11]"
            //});
            //Svid_Info_.Add("批號", new ControlInfo()
            //{
            //    BasicWindowsInfoTitle = "數據加載",
            //    BasicWindowsInfoClass = "WindowsForms10.Window.8.app.0.34f5582_r7_ad1",
            //    AdvancedClass = "[NAME:txtFB1LotNumber]"
            //});
            //Svid_Info_.Add("數量", new ControlInfo()
            //{
            //    BasicWindowsInfoTitle = "數據加載",
            //    BasicWindowsInfoClass = "WindowsForms10.Window.8.app.0.34f5582_r7_ad1",
            //    AdvancedClass = "[CLASS:WindowsForms10.EDIT.app.0.34f5582_r7_ad1; INSTANCE:157]"
            //});
            //Svid_Info_.Add("加載", new ControlInfo()
            //{
            //    BasicWindowsInfoTitle = "數據加載",
            //    BasicWindowsInfoClass = "WindowsForms10.Window.8.app.0.34f5582_r7_ad1",
            //    AdvancedClass = "[NAME:btnDataLoad]"
            //});
            //Svid_Info_.Add("退出", new ControlInfo()
            //{
            //    BasicWindowsInfoTitle = "數據加載",
            //    BasicWindowsInfoClass = "WindowsForms10.Window.8.app.0.34f5582_r7_ad1",
            //    AdvancedClass = "[NAME:btnExit]"
            //});

            //Svid_Info_.Add("槽位狀態DGV", new ControlInfo()
            //{
            //    BasicWindowsInfoTitle = "",
            //    BasicWindowsInfoClass = "[CLASS:WindowsForms10.Window.8.app.0.34f5582_r7_ad1]",
            //    AdvancedClass = "WindowsForms10.Window.8.app.0.34f5582_r7_ad19"
            //});

            //Svid_Info_.Add("警告DGV", new ControlInfo()
            //{
            //    BasicWindowsInfoTitle = "",
            //    BasicWindowsInfoClass = "[CLASS:WindowsForms10.Window.8.app.0.34f5582_r7_ad1]",
            //    AdvancedClass = "WindowsForms10.Window.8.app.0.34f5582_r7_ad16"
            //});


            //Config.LoadConfig("config.json");
            Svid_Info = Config.Svid_Info;
            //var DataLoad = "數據加載";
            //Svid_Info.Add(VID.加載槽號, new List<string>() { DataLoad, "[NAME:cmbLoadTankProcess]" });
            //Svid_Info.Add(VID.資料編號, new List<string>() { DataLoad, "[CLASS:Edit; INSTANCE:11]" });
            //Svid_Info.Add(VID.流程編號, new List<string>() { DataLoad, "[NAME:cmbProcessNumber]" });
            //Svid_Info.Add(VID.面積A, new List<string>() { DataLoad, "[NAME:numLengh]" });
            //Svid_Info.Add(VID.總面積, new List<string>() { DataLoad, "[NAME:numArea]" });
            //Svid_Info.Add(VID.批號, new List<string>() { DataLoad, "[NAME:txtFB1LotNumber]" });
            //Svid_Info.Add(VID.流程名稱, new List<string>() { DataLoad, "[NAME:txtProcessName]" });
            //Svid_Info.Add(VID.面積B, new List<string>() { DataLoad, "[NAME:numWidt]" });
            //Svid_Info.Add(VID.數量, new List<string>() { DataLoad, "[CLASS:WindowsForms10.EDIT.app.0.34f5582_r7_ad1; INSTANCE:157]" });
            //Svid_Info.Add(VID.模式, new List<string>() { DataLoad, "[NAME:cmbCcCv1]" });

            //Svid_Info.Add(VID.A面電流密度ASD, new List<string>() { DataLoad, "[NAME:numCurrentDensity1]" });
            //Svid_Info.Add(VID.B面電流密度ASD, new List<string>() { DataLoad, "[NAME:numCurrentDensity2]" });
            //Svid_Info.Add(VID.A面設定電流, new List<string>() { DataLoad, "[NAME:numCurrent1]" });
            //Svid_Info.Add(VID.B面設定電流, new List<string>() { DataLoad, "[NAME:numCurrent2]" });

            //Svid_Info.Add(VID.電流保護百分比, new List<string>() { DataLoad, "[NAME:numWeakRate1]" });
            //Svid_Info.Add(VID.設定電鍍時間, new List<string>() { DataLoad, "[NAME:numPlateTime1]" });
            //Svid_Info.Add(VID.Index, new List<string>() { DataLoad, "[NAME:label104]" });

            //Svid_Info.Add(VID.機台狀態, new List<string>() { "[CLASS:WindowsForms10.Window.8.app.0.34f5582_r7_ad1]", "WindowsForms10.Window.8.app.0.34f5582_r7_ad19" });
            //Svid_Info.Add(VID.DataGridView, new List<string>() { "[CLASS:WindowsForms10.Window.8.app.0.34f5582_r7_ad1]", "WindowsForms10.Window.8.app.0.34f5582_r7_ad19" });

            //Svid_Info.Add(VID.清除當前, new List<string>() { DataLoad, "[NAME:btnClearCurrent]" });
            //Svid_Info.Add(VID.加載, new List<string>() { DataLoad, "[NAME:btnDataLoad]" });
            //Svid_Info.Add(VID.退出, new List<string>() { DataLoad, "[NAME:btnExit]" });
            //Svid_Info.Add(VID.警告DGV, new List<string>() { "[CLASS:WindowsForms10.Window.8.app.0.34f5582_r7_ad1]", "WindowsForms10.Window.8.app.0.34f5582_r7_ad16" });

            

        }
        public static bool 確認監控程式是否處於正常()
        {
            string targetWindow = @"Data Monitoring System - [Data Monitoring]";

            var mainWin = AutomationElement.RootElement.FindFirst(
                  TreeScope.Children,
                  new PropertyCondition(AutomationElement.NameProperty, targetWindow));

            if (mainWin == null)
            {
                Console.WriteLine("找不到視窗");
                return false;
            }

            // 找所有 Text 控件
            // 1) 先直接用 AutomationId 找（通常等於 WinForms 的控制項 Name）
            var byId = mainWin.FindFirst(
                TreeScope.Descendants,
                new PropertyCondition(AutomationElement.AutomationIdProperty, "staDisplayUserName"));

            if (byId != null)
            {
                //Console.WriteLine("[Hit: AutomationId] " + Dump(byId));
                return false;
            }


            Console.WriteLine("沒找到符合條件的文字");
            return false;
        }

        public static bool OperationAccessDataBaseFlag;

    }

    public class ControlInfo
    {
        public string? BasicWindowsInfoTitle { get; internal set; }
        public string? BasicWindowsInfoClass { get; internal set; }
        public string? AdvancedClass { get; internal set; }
    }

    public class ExternalGridReader
    {
        public static List<List<string>> ReadGrid(string winTitle)
        {
            var win = AutomationElement.RootElement.FindFirst(
                TreeScope.Children,
                new AndCondition(
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window),
                    new PropertyCondition(AutomationElement.NameProperty, winTitle)));

            if (win == null) throw new Exception("找不到視窗");

            // 1) 優先找 支援 Grid/Table 的元素
            var grid = FindElementWithPatterns(
                win,
                new[] { ControlType.DataGrid, ControlType.Table, ControlType.Custom, ControlType.Pane },
                tryGridOrTable: true);

            if (grid == null)
            {
                // 2) 退一步：找名稱或 ClassName 含 DataGridView 的元素
                grid = win.FindFirst(TreeScope.Descendants, new AndCondition(
                    new PropertyCondition(AutomationElement.NameProperty, "DataGridView", PropertyConditionFlags.IgnoreCase),
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Custom)));
            }
            if (grid == null) throw new Exception("找不到 DataGridView 元素");

            // A) GridPattern
            if (grid.TryGetCurrentPattern(GridPattern.Pattern, out var gpObj) && gpObj is GridPattern gp)
                return SnapshotViaGridPattern(gp);

            // B) TablePattern（有時只有 Table，仍可透過 TableItemPattern 取值）
            if (grid.TryGetCurrentPattern(TablePattern.Pattern, out var tpObj) && tpObj is TablePattern tp)
                return SnapshotViaTablePattern(grid, tp);

            // C) 最後退路：掃 rows/cells，再用 Value/Text/Legacy 讀值
            return SnapshotByWalking(grid);
        }

        private static AutomationElement FindElementWithPatterns(
            AutomationElement root,
            ControlType[] types,
            bool tryGridOrTable)
        {
            var candidates = root.FindAll(
                TreeScope.Descendants,
                new OrCondition(types.Select(t => new PropertyCondition(AutomationElement.ControlTypeProperty, t)).ToArray()));

            foreach (AutomationElement el in candidates)
            {
                if (tryGridOrTable)
                {
                    if (el.TryGetCurrentPattern(GridPattern.Pattern, out _) ||
                        el.TryGetCurrentPattern(TablePattern.Pattern, out _))
                        return el;
                }
            }
            return null!;
        }

        private static List<List<string>> SnapshotViaGridPattern(GridPattern gp)
        {
            var rows = new List<List<string>>();
            int rN = gp.Current.RowCount;
            int cN = gp.Current.ColumnCount;
            for (int r = 0; r < rN; r++)
            {
                var row = new List<string>();
                for (int c = 0; c < cN; c++)
                {
                    string val = "";
                    try { val = ReadCellLite(gp.GetItem(r, c)); } catch { }
                    row.Add(val);
                }
                rows.Add(row);
            }
            return rows;
        }

        private static List<List<string>> SnapshotViaTablePattern(AutomationElement grid, TablePattern tp)
        {
            // 以 rows (DataItem/ListItem) 為主再取子項
            return SnapshotByWalking(grid);
        }

        private static List<List<string>> SnapshotByWalking(AutomationElement grid)
        {
            var result = new List<List<string>>();

            // 找所有“資料列”
            var rows = grid.FindAll(TreeScope.Subtree,
                new OrCondition(
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.DataItem),
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.ListItem)));

            foreach (AutomationElement rowEl in rows)
            {
                var cells = rowEl.FindAll(TreeScope.Children,
                    new OrCondition(
                        new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Custom),
                        new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Text),
                        new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Edit),
                        new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.DataItem)));

                var row = new List<string>();
                foreach (AutomationElement cell in cells)
                    row.Add(ReadCellLite(cell));

                if (row.Count > 0) result.Add(row);
            }

            return result;
        }

        // 讀單格：Value → Text → Legacy( MSAA ) → Name

        private static string ReadCellLite(AutomationElement cell)
        {
            if (cell.TryGetCurrentPattern(ValuePattern.Pattern, out var vp) && vp is ValuePattern v)
                return v.Current.Value ?? "";

            if (cell.TryGetCurrentPattern(TextPattern.Pattern, out var tp) && tp is TextPattern t)
                return t.DocumentRange.GetText(int.MaxValue).TrimEnd('\r', '\n');

            return cell.Current.Name ?? "";
        }
    }

    public static class DgvWalker
    {
        public static List<List<string>> ReadAllRows(string mainTitle, string mdiChildTitle, string gridAutomationId)
        {

            // 1) 找主視窗
            var main = AutomationElement.RootElement.FindFirst(
                TreeScope.Children,
                new AndCondition(
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window),
                    new PropertyCondition(AutomationElement.NameProperty, mainTitle)));
            if (main == null) 
                throw new Exception("找不到主視窗");

            // 2) 在主視窗下找到 MDI 子窗（很多 WinForms 會把 MDI 子窗也標成 ControlType.Window 或 Pane）
            var childWin = main.FindFirst(
                TreeScope.Descendants,
                new AndCondition(
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window),
                    new PropertyCondition(AutomationElement.NameProperty, mdiChildTitle)));
            if (childWin == null)
            {
                // 某些程式把 MDI 子窗標成 Pane，備援再找一次
                childWin = main.FindFirst(
                    TreeScope.Descendants,
                    new AndCondition(
                        new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Pane),
                        new PropertyCondition(AutomationElement.NameProperty, mdiChildTitle)));
            }
            if (childWin == null) 
                throw new Exception("找不到 MDI 子視窗");

            // 3) 在 MDI 子窗底下找 Grid（用 AutomationId 最穩）
            var grid = childWin.FindFirst(
                TreeScope.Descendants,
                new PropertyCondition(AutomationElement.AutomationIdProperty, gridAutomationId));
            if (grid == null) 
                throw new Exception("找不到 DataGridView");


            // 3) 走子樹：先抓所有 "資料列"
            var all = grid.FindAll(TreeScope.Descendants, Condition.TrueCondition);
            var rows = new List<AutomationElement>();
            foreach (AutomationElement el in all)
            {
                var name = el.Current.Name ?? "";
                if (name.StartsWith("資料列 "))
                {
                    Debug.WriteLine(name);
                    rows.Add(el);
                }
            }
            Console.WriteLine("rows = " + rows.Count);

            var result = new List<List<string>>();

            foreach (AutomationElement rowEl in rows)
            {
                // 每列的每個 cell 通常是 Text/Edit/Custom
                var cells = rowEl.FindAll(TreeScope.Children,
                    new OrCondition(
                        new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Text),
                        new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Edit),
                        new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Custom),
                        new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.DataItem)
                    ));

                var row = new List<string>();
                foreach (AutomationElement cell in cells)
                    row.Add(ReadCell(cell));

                if (row.Count > 0) 
                    result.Add(row);
            }

            return result;
        }

        // 優先 Value → Text → LegacyIA → Name
        private static string ReadCell(AutomationElement el)
        {
            try
            {
                if (el.TryGetCurrentPattern(ValuePattern.Pattern, out var vp) && vp is ValuePattern v)
                    return v.Current.Value ?? "";

                if (el.TryGetCurrentPattern(TextPattern.Pattern, out var tp) && tp is TextPattern t)
                    return t.DocumentRange.GetText(int.MaxValue).TrimEnd('\r', '\n');

               // if (el.TryGetCurrentPattern(LegacyIAccessiblePattern.Pattern, out var lp) && lp is LegacyIAccessiblePattern acc)
               //     return acc.Current.Value ?? acc.Current.Name ?? "";
            }
            catch { /* ignore */ }

            return el.Current.Name ?? "";
        }
    }
}
