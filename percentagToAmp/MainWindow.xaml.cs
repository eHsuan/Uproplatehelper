using System.Collections.Specialized;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace percentagToAmp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ViewModels.mainModelView mvm;
        public MainWindow()
        {
            InitializeComponent();
            this.Title += $" {FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion}";
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            CyntecLog.GeneralLogger.Info("uPro外掛啟動");
            mvm = new ViewModels.mainModelView();
            DataContext = mvm;
            Config.LoadConfig("config.json");

            {
                mvm.LoadDBToGD();
            }
        }

        private void Tb_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            // 切到英文鍵盤（en-US 或你要的語系）
            InputLanguageManager.SetInputLanguage((System.Windows.DependencyObject)sender, new CultureInfo("en-US"));
        }
        public static class ListBoxBehavior
        {
            public static readonly DependencyProperty AutoScrollProperty =
                DependencyProperty.RegisterAttached("AutoScroll", typeof(bool), typeof(ListBoxBehavior),
                    new PropertyMetadata(false, OnAutoScrollChanged));

            public static bool GetAutoScroll(DependencyObject obj) => (bool)obj.GetValue(AutoScrollProperty);
            public static void SetAutoScroll(DependencyObject obj, bool value) => obj.SetValue(AutoScrollProperty, value);

            private static void OnAutoScrollChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            {
                if (d is ListBox listBox && e.NewValue is bool enabled && enabled)
                {
                    var data = listBox.ItemsSource as INotifyCollectionChanged;
                    if (data != null)
                    {
                        data.CollectionChanged += (s, ev) =>
                        {
                            if (listBox.Items.Count > 0)
                            {
                                listBox.ScrollIntoView(listBox.Items[^1]);
                            }
                        };
                    }
                }
            }
        }

        private void dg_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left) return;

            // 只在點到實際 row/cell 時才觸發（避免點到表頭/空白）
            var dep = e.OriginalSource as DependencyObject;
            while (dep != null && dep is not DataGridRow)
                dep = VisualTreeHelper.GetParent(dep);

            var row = dep as DataGridRow;
            if (row == null) return;

            var item = row.Item;

            // 你的資料若來自 DataTable：
            if (item is DataRowView drv)
            {
                string partNumber = drv["PartNumber"]?.ToString() ?? "";
                //double areaA = drv["BoardLengh"]?.ToString() ?? "";
                double areaA = drv.Row.IsNull("BoardLengh") ? 0.0 : Convert.ToDouble(drv.Row["BoardLengh"]);
                //double areaA = drv.Row.Field<double?>("BoardLengh") ?? 0.0;
                //string areaB = drv["BoardWidt"]?.ToString() ?? "";
                double areaB = drv.Row.IsNull("BoardWidt") ? 0.0 : Convert.ToDouble(drv.Row["BoardWidt"]);
                //string areaTotal = drv["BoardArea"]?.ToString() ?? "";
                //double areaTotal = drv.Row.Field<double?>("BoardArea") ?? 0.0;
                double areaTotal = drv.Row.IsNull("BoardArea") ? 0.0 : Convert.ToDouble(drv.Row["BoardArea"]);
                //string currentDensityA = drv["CurrentDensity1"]?.ToString() ?? "";
                //double currentDensityA = drv.Row.Field<double?>("CurrentDensity1") ?? 0.0;
                double currentDensityA = drv.Row.IsNull("CurrentDensity1") ? 0.0 : Convert.ToDouble(drv.Row["CurrentDensity1"]);
                //string currentDensityB = drv["CurrentDensity2"]?.ToString() ?? "";
                //double currentDensityB = drv.Row.Field<double?>("CurrentDensity2") ?? 0.0;
                double currentDensityB = drv.Row.IsNull("CurrentDensity2") ? 0.0 : Convert.ToDouble(drv.Row["CurrentDensity2"]);
                //string currentA = drv["Current1"]?.ToString() ?? "";
                //double currentA = drv.Row.Field<double?>("Current1") ?? 0.0;
                double currentA = drv.Row.IsNull("Current1") ? 0.0 : Convert.ToDouble(drv.Row["Current1"]);
                //string currentB = drv["Current2"]?.ToString() ?? "";
                //double currentB = drv.Row.Field<double?>("Current2") ?? 0.0;
                double currentB = drv.Row.IsNull("Current2") ? 0.0 : Convert.ToDouble(drv.Row["Current2"]);
                //string currentProtectPercentage = drv["WeakRate1"]?.ToString() ?? "";
                //double currentProtectPercentage = drv.Row.Field<double?>("WeakRate1") ?? 0.0;
                double currentProtectPercentage = drv.Row.IsNull("WeakRate1") ? 0.0 : Convert.ToDouble(drv.Row["WeakRate1"]);
                //string totalPlateTime = drv["PlateTime1"]?.ToString() ?? "";
                //int totalPlateTime = drv.Row.Field<int?>("PlateTime1") ?? 0;
                int totalPlateTime = drv.Row.IsNull("PlateTime1") ? 0 : Convert.ToInt16(drv.Row["PlateTime1"]);


                int plateTimeStep1 = drv.Row.IsNull("PlateTime2") ? 0 : Convert.ToInt16(drv.Row["PlateTime2"]);
                int plateTimeStep2 = drv.Row.IsNull("PlateTime3") ? 0 : Convert.ToInt16(drv.Row["PlateTime3"]);
                int plateTimeStep3 = drv.Row.IsNull("PlateTime4") ? 0 : Convert.ToInt16(drv.Row["PlateTime4"]);
                int plateTimeStep4 = drv.Row.IsNull("PlateTime5") ? 0 : Convert.ToInt16(drv.Row["PlateTime5"]);
                int plateTimeStep5 = drv.Row.IsNull("PlateTime6") ? 0 : Convert.ToInt16(drv.Row["PlateTime6"]);
                int plateTimeStep6 = drv.Row.IsNull("PlateTime7") ? 0 : Convert.ToInt16(drv.Row["PlateTime7"]);
                int plateTimeStep7 = drv.Row.IsNull("PlateTime8") ? 0 : Convert.ToInt16(drv.Row["PlateTime8"]);
                int plateTimeStep8 = drv.Row.IsNull("PlateTime9") ? 0 : Convert.ToInt16(drv.Row["PlateTime9"]);
                int plateTimeStep9 = drv.Row.IsNull("PlateTime10") ? 0 : Convert.ToInt16(drv.Row["PlateTime10"]);
                int plateTimeStep10 = drv.Row.IsNull("PlateTime11") ? 0 : Convert.ToInt16(drv.Row["PlateTime11"]);


                double currentPercentageStep1 = drv.Row.IsNull("WeakRate2") ? 0.0 : Convert.ToDouble(drv.Row["WeakRate2"]);
                double currentPercentageStep2  = drv.Row.IsNull("WeakRate3") ? 0.0 : Convert.ToDouble(drv.Row["WeakRate3"]);
                double currentPercentageStep3  = drv.Row.IsNull("WeakRate4") ? 0.0 : Convert.ToDouble(drv.Row["WeakRate4"]);
                double currentPercentageStep4  = drv.Row.IsNull("WeakRate5") ? 0.0 : Convert.ToDouble(drv.Row["WeakRate5"]);
                double currentPercentageStep5  = drv.Row.IsNull("WeakRate6") ? 0.0 : Convert.ToDouble(drv.Row["WeakRate6"]);
                double currentPercentageStep6  = drv.Row.IsNull("WeakRate7") ? 0.0 : Convert.ToDouble(drv.Row["WeakRate7"]);
                double currentPercentageStep7  = drv.Row.IsNull("WeakRate8") ? 0.0 : Convert.ToDouble(drv.Row["WeakRate8"]);
                double currentPercentageStep8  = drv.Row.IsNull("WeakRate9") ? 0.0 : Convert.ToDouble(drv.Row["WeakRate9"]);
                double currentPercentageStep9  = drv.Row.IsNull("WeakRate10") ? 0.0 : Convert.ToDouble(drv.Row["WeakRate10"]);
                double currentPercentageStep10 = drv.Row.IsNull("WeakRate11") ? 0.0 : Convert.ToDouble(drv.Row["WeakRate11"]);

                // TODO: 你的處理
                //MessageBox.Show($"選到料號：{partNumber}");
                if (DataContext is ViewModels.mainModelView vm)
                {
                    vm.PartNumber = partNumber; // 直接設定；UI 會因 INotifyPropertyChanged 自動更新
                    vm.AreaA = double.Round(areaA, 4);
                    vm.AreaB = double.Round(areaB, 4);
                    //vm.AreaTotal = double.Round(areaTotal, 4);
                    vm.CurrentDensityA = double.Round(currentDensityA, 4);
                    vm.CurrentDensityB = double.Round(currentDensityB, 4);
                    //vm.CurrentA = double.Round(currentA, 4);
                    //vm.CurrentB = double.Round(currentB, 4);
                    vm.CurrentProtectPercentage = currentProtectPercentage;
                    vm.TotalPlateTime = totalPlateTime;
                    vm.PlateTimeStep1 = plateTimeStep1;
                    vm.PlateTimeStep2 = plateTimeStep2;
                    vm.PlateTimeStep3 = plateTimeStep3;
                    vm.PlateTimeStep4 = plateTimeStep4;
                    vm.PlateTimeStep5 = plateTimeStep5;
                    vm.PlateTimeStep6 = plateTimeStep6;
                    vm.PlateTimeStep7 = plateTimeStep7;
                    vm.PlateTimeStep8 = plateTimeStep8;
                    vm.PlateTimeStep9 = plateTimeStep9;
                    vm.PlateTimeStep10 = plateTimeStep10;
                    

                    vm.CurrentPercentageStep1 = currentPercentageStep1;
                    vm.CurrentPercentageStep2 = currentPercentageStep2;
                    vm.CurrentPercentageStep3 = currentPercentageStep3;
                    vm.CurrentPercentageStep4 = currentPercentageStep4;
                    vm.CurrentPercentageStep5 = currentPercentageStep5;
                    vm.CurrentPercentageStep6 = currentPercentageStep6;
                    vm.CurrentPercentageStep7 = currentPercentageStep7;
                    vm.CurrentPercentageStep8 = currentPercentageStep8;
                    vm.CurrentPercentageStep9 = currentPercentageStep9;
                    vm.CurrentPercentageStep10 = currentPercentageStep10;

                }
            }

            // 若是 ObservableCollection<T> 的模型：
            // if (item is Order o) { ... }

            e.Handled = true;
        }

        private static readonly NumberFormatInfo NFI = CultureInfo.CurrentCulture.NumberFormat;
        private static readonly string DecSep = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
        private const bool AllowNegative = false;   // 要負數改成 true

        private void NumberBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var tb = (TextBox)sender;
            var text = (e.Text == "." || e.Text == ",") ? DecSep : e.Text;
            string candidate = ReplaceSelection(tb, text);

            // 禁止以小數點開頭
            if (candidate.StartsWith(DecSep))
            {
                e.Handled = true;
                return;
            }

            e.Handled = !IsPartialNumber(candidate);
        }

        private void NumberBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // 允許的功能鍵
            if (e.Key == Key.Back || e.Key == Key.Delete || e.Key == Key.Tab ||
                e.Key == Key.Left || e.Key == Key.Right || e.Key == Key.Home || e.Key == Key.End)
                return;

            // 允許快捷鍵：Ctrl+A/C/V/X
            if (Keyboard.Modifiers == ModifierKeys.Control &&
                (e.Key == Key.A || e.Key == Key.C || e.Key == Key.V || e.Key == Key.X))
                return;

            // 其他情況交給 PreviewTextInput 處理；這裡不另外攔
        }

        private void NumberBox_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(DataFormats.Text))
            {
                var pasteText = (string)e.DataObject.GetData(DataFormats.Text);
                var tb = (TextBox)sender;
                if (!WouldBeValidAfterInsert(tb, pasteText))
                    e.CancelCommand();
            }
            else
            {
                e.CancelCommand();
            }
        }

        private static bool WouldBeValidAfterInsert(TextBox tb, string incoming)
        {
            // 允許輸入小數點（依文化）；把逗號換成目前文化的小數點
            if (incoming == "." || incoming == ",")
                incoming = DecSep;

            // 允許單一負號（若允許負數）
            if (AllowNegative && incoming == "-" && (tb.SelectionStart == 0 && tb.SelectionLength == tb.Text.Length || tb.CaretIndex == 0))
            {
                // 組合後字串
                string newTextNeg = ReplaceSelection(tb, incoming);
                return IsPartialNumber(newTextNeg);
            }

            // 只允許數字與小數點（其餘直接擋）
            foreach (char ch in incoming)
            {
                if (!char.IsDigit(ch) && ch.ToString() != DecSep)
                    return false;
            }

            string newText = ReplaceSelection(tb, incoming);
            return IsPartialNumber(newText);
        }

        private static string ReplaceSelection(TextBox tb, string incoming)
        {
            string t = tb.Text ?? string.Empty;
            int start = tb.SelectionStart;
            int length = tb.SelectionLength;
            if (length > 0) t = t.Remove(start, length);
            t = t.Insert(start, incoming);
            return t;
        }

        // 允許「中間態」數字：空字串、單一負號、結尾小數點等
        private static bool IsPartialNumber(string text)
        {
            if (string.IsNullOrEmpty(text)) return true;
            if (AllowNegative && text == "-") return true;
            if (text == DecSep) return true;
            if (AllowNegative && text == "-" + DecSep) return true;

            // 不允許多個小數點
            if (text.Split(new[] { DecSep }, StringSplitOptions.None).Length - 1 > 1) return false;

            // 嘗試以目前文化解析
            return double.TryParse(text,
                NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite,
                CultureInfo.CurrentCulture, out _);
        }

        private void dg_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dg.SelectedItem is DataRowView drv)
            {
                // 取整列
                var row = drv.Row;
                if (DataContext is ViewModels.mainModelView vm)
                {
                    vm.selectedIndexID = drv.Row.IsNull("IndexID") ? 0 : Convert.ToInt16(drv.Row["IndexID"]);
                    vm.selectedPartNumber = drv["PartNumber"]?.ToString() ?? "";
                }
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var result = MessageBox.Show(
                        "確定要關閉程式嗎？",
                        "確認關閉",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question,
                        MessageBoxResult.No);

            if (result != MessageBoxResult.Yes)
            {
                e.Cancel = true;   // 使用者按了「否」→ 取消關閉
            }

            CyntecLog.GeneralLogger.Debug("==User Close APP==");
        }
    }
}