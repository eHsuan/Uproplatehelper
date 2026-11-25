using AutoIt;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CyntecMqttClient;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using NLog.Config;
using Secs4Net;
using Secs4Net.Sml;
using System;
using System;
using System.Buffers.Text;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using static Microsoft.Extensions.Logging.EventSource.LoggingEventSource;
using static percentagToAmp.Utility;
using ValidationResult = System.ComponentModel.DataAnnotations.ValidationResult;

namespace percentagToAmp.ViewModels
{

    /// <summary>
    /// ViewModel for: 自動載入Recipe(PP) - uPro DSM
    /// 綁定示例（XAML）：
    ///   <TextBox Text="{Binding WorkOrderNo, UpdateSourceTrigger=PropertyChanged, ValidatesOnNotifyDataErrors=True}" />
    ///   <TextBox Text="{Binding EmployeeId, UpdateSourceTrigger=PropertyChanged, ValidatesOnNotifyDataErrors=True}" />
    ///   <Button Content="派送" Command="{Binding DispatchCommand}" />
    ///   <Button Content="取消" Command="{Binding CancelCommand}" />
    /// </summary>
    public partial class mainModelView : ObservableValidator, IDisposable
    {
        [ObservableProperty]
        //[NotifyCanExecuteChangedFor(nameof(DeleteRecordCommand))]
        public int selectedIndexID = 0;

        public string selectedPartNumber = "";

        private System.Timers.Timer S5F1_Timer;
        private System.Timers.Timer S6F1_Timer;
        private System.Timers.Timer ConnectedTimer;
        private MessageBoxWindow? _messageBoxWindow;
        private MqttClientService _mqtt;

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        private System.Timers.Timer S6F1_CheckTimer;    //WC 2025/10/14 Add 每秒檢查滑鼠
        private DateTime lastMouseMoveTime;
        private POINT lastMousePos;
        bool isStopTimer = false;
        bool isReadyToStart = false; 
        readonly SemaphoreSlim S6F1Lock = new SemaphoreSlim(1, 1);      //WC 2026/05/11 Add     

        static PLCMXController MitsubishiPLC = new PLCMXController();   //WC 2026/05/07 Add PLC    

        private readonly CancellationTokenSource _cts = new();

        // 是否忙碌；變化時會觸發 CanExecute 重新評估
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(DispatchCommand))]
        private bool isBusy;

        // 是否忙碌；變化時會觸發 CanExecute 重新評估
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(MoveOutCommand))]
        private bool isBusyMoveOut;

        // 工單號碼
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(DispatchCommand))]
        [Required(ErrorMessage = "工單號碼不可空白")]
        [RegularExpression(@"^[A-Za-z0-9\-_.]{12}$", ErrorMessage = "工單格式不符")]
        private string workOrderNo = string.Empty;

        // 員工編號
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(DispatchCommand))]
        [Required(ErrorMessage = "員工編號不可空白")]
        [RegularExpression(@"^[1-9][0-9]{5,}$", ErrorMessage = "員工編號格式不符")]
        private string employeeId = string.Empty;

        // 飛靶編號     //WC 2025/11/03
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(DispatchCommand))]
        [Required(ErrorMessage = "飛靶編號不可空白")]
        [RegularExpression(@"^#[1-9][0-9]?$", ErrorMessage = "飛靶編號格式不符")]
        private string targetNo = string.Empty;

        // 左掛架
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(DispatchCommand))]
        [Required(ErrorMessage = "左掛架不可空白")]
        [RegularExpression(@"^[1-9][0-9]?$", ErrorMessage = "左掛架格式不符")]
        private string leftRack = string.Empty;

        // 右掛架
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(DispatchCommand))]
        [Required(ErrorMessage = "右掛架不可空白")]
        [RegularExpression(@"^[1-9][0-9]?$", ErrorMessage = "右掛架格式不符")]
        private string rightRack = string.Empty;

        // 工單號碼 MoveOut //WC 2025/12/09
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(MoveOutCommand))]
        [Required(ErrorMessage = "工單號碼不可空白")]
        [RegularExpression(@"^[A-Za-z0-9\-_.]{12}$", ErrorMessage = "工單格式不符")]
        private string workOrderNoMoveOut = string.Empty;

        // 員工編號 MoveOut
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(MoveOutCommand))]
        [Required(ErrorMessage = "員工編號不可空白")]
        [RegularExpression(@"^[1-9][0-9]{5,}$", ErrorMessage = "員工編號格式不符")]
        private string employeeIdMoveOut = string.Empty;

        //mark
        //// 槽位編號 MoveOut
        //[ObservableProperty]
        //[NotifyCanExecuteChangedFor(nameof(MoveOutCommand))]
        //[Required(ErrorMessage = "槽位編號不可空白")]
        //[RegularExpression(@"^[1-9][0-9]?$", ErrorMessage = "槽位編號格式不符")]
        //private string slotNoMoveOut = string.Empty;

        //public ObservableCollection<int> SlotList { get; set; } = new ObservableCollection<int>(Enumerable.Range(9, 5));

        //Dictionary<string, List<string>> _errors;
        //private readonly Dictionary<string, List<ValidationResult>> _moveOutErrors = new();

        [ObservableProperty]
        private string encodedText = string.Empty;

        [ObservableProperty]
        private string decodedText = string.Empty;

        [RelayCommand]
        private void TextDecode()
        {
            if (EncodedText == null) return;

            try
            {
                byte[] bytes = Convert.FromBase64String(EncodedText);

                DecodedText = Encoding.UTF8.GetString(bytes);
            }
            catch (Exception)
            {

            }

        }

        [ObservableProperty]
        private bool isSECSHostConnected;   // 綁定到 UI 按鈕 Enable

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AddRecordCommand))]
        private string partNumber;

        //[ObservableProperty]
        //[NotifyPropertyChangedFor(nameof(CurrentA))]
        //private double areaA;

        //[ObservableProperty]
        //private double areaB;

        //[ObservableProperty]
        //private double areaTotal;

        //[ObservableProperty]
        //[NotifyPropertyChangedFor(nameof(CurrentA))]
        //private double currentDensityA;

        //[ObservableProperty]
        //private double currentDensityB;

        //[ObservableProperty]
        //[NotifyPropertyChangedFor(nameof(Current1))]
        //private double currentA;

        //[ObservableProperty]
        //private double currentB;

        [ObservableProperty]
        private double currentProtectPercentage;

        [ObservableProperty]
        private int totalPlateTime;

        [ObservableProperty]
        private int plateTimeStep1;
        [ObservableProperty]
        private int plateTimeStep2;
        [ObservableProperty]
        private int plateTimeStep3;
        [ObservableProperty]
        private int plateTimeStep4;
        [ObservableProperty]
        private int plateTimeStep5;
        [ObservableProperty]
        private int plateTimeStep6;
        [ObservableProperty]
        private int plateTimeStep7;
        [ObservableProperty]
        private int plateTimeStep8;
        [ObservableProperty]
        private int plateTimeStep9;
        [ObservableProperty]
        private int plateTimeStep10;

        [ObservableProperty]
        private double currentPercentageStep1;
        [ObservableProperty]
        private double currentPercentageStep2;
        [ObservableProperty]
        private double currentPercentageStep3;
        [ObservableProperty]
        private double currentPercentageStep4;
        [ObservableProperty]
        private double currentPercentageStep5;
        [ObservableProperty]
        private double currentPercentageStep6;
        [ObservableProperty]
        private double currentPercentageStep7;
        [ObservableProperty]
        private double currentPercentageStep8;
        [ObservableProperty]
        private double currentPercentageStep9;
        [ObservableProperty]
        private double currentPercentageStep10;

        //private double? _current1;
        //public double? Current1
        //{
        //    get => (CurrentA, CurrentPercentageStep1) is (double a, double p) ? a * p / 100.0 : _current1;
        //    set
        //    {
        //        // 例：固定 Percent1，反推 ACurrentSetting
        //        CurrentA = Math.Round(CurrentA * 100.0 / CurrentPercentageStep1, 2);

        //        SetProperty(ref _current1, value);
        //        OnPropertyChanged(nameof(Current1)); // 依你的計算邏輯而定
        //    }
        //}

        //// 來源任一變更時回算
        ////partial void OnCurrentDensityAChanged(double value) => RecalcCurrentA();
        ////partial void OnAreaAChanged(double value) => RecalcCurrentA();

        //private void RecalcCurrentA()
        //    => CurrentA = Math.Round(CurrentDensityA * AreaA, 2);


        // === 來源：決定基準電流 ===
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CurrentA))]
        [NotifyPropertyChangedFor(nameof(CurrentA1))]
        [NotifyPropertyChangedFor(nameof(CurrentA2))]
        private double currentDensityA;      // A 面電流密度 (A/dm²)

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CurrentA))]
        [NotifyPropertyChangedFor(nameof(AreaTotal))]
        [NotifyPropertyChangedFor(nameof(CurrentA1))]
        [NotifyPropertyChangedFor(nameof(CurrentA2))]
        private double areaA;                // A 面面積 (dm²)

        public double CurrentA => Math.Round(CurrentDensityA * AreaA, 4);

        // === 來源：決定基準電流 ===
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CurrentB))]
        [NotifyPropertyChangedFor(nameof(CurrentB1))]
        [NotifyPropertyChangedFor(nameof(CurrentB2))]
        private double currentDensityB;      // B 面電流密度 (A/dm²)

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CurrentB))]
        [NotifyPropertyChangedFor(nameof(AreaTotal))]
        [NotifyPropertyChangedFor(nameof(CurrentB1))]
        [NotifyPropertyChangedFor(nameof(CurrentB2))]
        private double areaB;                // B 面面積 (dm²)

        public double AreaTotal => Math.Round(AreaA + AreaB, 4);

        // 唯讀：基準電流 B = 密度 × 面積
        public double CurrentB => Math.Round(CurrentDensityB * AreaB, 4);

        #region 運算

        // === 雙向：任一改動推算另一個 ===
        [ObservableProperty] private double currentA1;              // 第1段 電流 (A) ← 使用者可編輯
        [ObservableProperty] private double currentA2;
        [ObservableProperty] private double currentA3;
        [ObservableProperty] private double currentA4;
        [ObservableProperty] private double currentA5;
        [ObservableProperty] private double currentA6;
        [ObservableProperty] private double currentA7;
        [ObservableProperty] private double currentA8;
        [ObservableProperty] private double currentA9;
        [ObservableProperty] private double currentA10;

        [ObservableProperty] private double currentB1;              // 第1段 電流 (A) ← 使用者可編輯
        [ObservableProperty] private double currentB2;
        [ObservableProperty] private double currentB3;
        [ObservableProperty] private double currentB4;
        [ObservableProperty] private double currentB5;
        [ObservableProperty] private double currentB6;
        [ObservableProperty] private double currentB7;
        [ObservableProperty] private double currentB8;
        [ObservableProperty] private double currentB9;
        [ObservableProperty] private double currentB10;
        //[ObservableProperty] private double currentPercentageStep2; // 第1段 百分比 (%)

        private bool _updating;  // 防止互相觸發造成遞迴

        partial void OnCurrentPercentageStep1Changed(double value)
        {
            RecalcStep1(value);
        }
        partial void OnCurrentPercentageStep2Changed(double value)
        {
            RecalcStep2(value);
        }
        partial void OnCurrentPercentageStep3Changed(double value)
        {
            RecalcStep3(value);
        }
        partial void OnCurrentPercentageStep4Changed(double value)
        {
            RecalcStep4(value);
        }
        partial void OnCurrentPercentageStep5Changed(double value)
        {
            RecalcStep5(value);
        }
        partial void OnCurrentPercentageStep6Changed(double value)
        {
            RecalcStep6(value);
        }
        partial void OnCurrentPercentageStep7Changed(double value)
        {
            RecalcStep7(value);
        }
        partial void OnCurrentPercentageStep8Changed(double value)
        {
            RecalcStep8(value);
        }
        partial void OnCurrentPercentageStep9Changed(double value)
        {
            RecalcStep9(value);
        }
        partial void OnCurrentPercentageStep10Changed(double value)
        {
            RecalcStep10(value);
        }
        partial void OnCurrentDensityAChanged(double value)
        {
            RecalcStep1(CurrentPercentageStep1);
            RecalcStep2(CurrentPercentageStep2);
            RecalcStep3(CurrentPercentageStep3);
            RecalcStep4(CurrentPercentageStep4);
            RecalcStep5(CurrentPercentageStep5);
            RecalcStep6(CurrentPercentageStep6);
            RecalcStep7(CurrentPercentageStep7);
            RecalcStep8(CurrentPercentageStep8);
            RecalcStep9(CurrentPercentageStep9);
            RecalcStep10(CurrentPercentageStep10);
        }
        partial void OnCurrentDensityBChanged(double value)
        {
            RecalcStep1(CurrentPercentageStep1);
            RecalcStep2(CurrentPercentageStep2);
            RecalcStep3(CurrentPercentageStep3);
            RecalcStep4(CurrentPercentageStep4);
            RecalcStep5(CurrentPercentageStep5);
            RecalcStep6(CurrentPercentageStep6);
            RecalcStep7(CurrentPercentageStep7);
            RecalcStep8(CurrentPercentageStep8);
            RecalcStep9(CurrentPercentageStep9);
            RecalcStep10(CurrentPercentageStep10);
        }
        partial void OnAreaAChanged(double value)
        {
            RecalcStep1(CurrentPercentageStep1);
            RecalcStep2(CurrentPercentageStep2);
            RecalcStep3(CurrentPercentageStep3);
            RecalcStep4(CurrentPercentageStep4);
            RecalcStep5(CurrentPercentageStep5);
            RecalcStep6(CurrentPercentageStep6);
            RecalcStep7(CurrentPercentageStep7);
            RecalcStep8(CurrentPercentageStep8);
            RecalcStep9(CurrentPercentageStep9);
            RecalcStep10(CurrentPercentageStep10);
        }
        partial void OnAreaBChanged(double value)
        {
            RecalcStep1(CurrentPercentageStep1);
            RecalcStep2(CurrentPercentageStep2);
            RecalcStep3(CurrentPercentageStep3);
            RecalcStep4(CurrentPercentageStep4);
            RecalcStep5(CurrentPercentageStep5);
            RecalcStep6(CurrentPercentageStep6);
            RecalcStep7(CurrentPercentageStep7);
            RecalcStep8(CurrentPercentageStep8);
            RecalcStep9(CurrentPercentageStep9);
            RecalcStep10(CurrentPercentageStep10);
        }


        // 當使用者改「電流(A)」→ 反推百分比(%)
        partial void OnCurrentA1Changed(double value)
        {
            if (_updating) return;
            try
            {
                _updating = true;
                var baseA = CurrentA;
                CurrentPercentageStep1 = baseA > 0
                    ? Clamp(Math.Round(value / baseA * 100.0, 2), 0, 100)
                    : 0;
                CurrentB1 = (Math.Round(CurrentPercentageStep1 * CurrentB / 100.0, 2));
            }
            finally { _updating = false; }
        }
        partial void OnCurrentB1Changed(double value)
        {
            if (_updating) return;
            try
            {
                _updating = true;
                var baseB = CurrentB;
                CurrentPercentageStep1 = baseB > 0
                    ? Clamp(Math.Round(value / baseB * 100.0, 2), 0, 100)
                    : 0;
                CurrentA1 = (Math.Round(CurrentPercentageStep1 * CurrentA / 100.0, 2));
            }
            finally { _updating = false; }
        }

        // 共用：依百分比重算第1段
        private void RecalcStep1(double percent)
        {
            if (_updating) return;
            try
            {
                _updating = true;
                var p = Clamp(percent, 0, 100);
                CurrentA1 = Math.Round(CurrentA * p / 100.0, 2);
                CurrentB1 = Math.Round(CurrentB * p / 100.0, 2);
            }
            finally { _updating = false; }
        }
        partial void OnCurrentA2Changed(double value)
        {
            if (_updating) return;
            try
            {
                _updating = true;
                var baseA = CurrentA;
                CurrentPercentageStep2 = baseA > 0
                    ? Clamp(Math.Round(value / baseA * 100.0, 2), 0, 100)
                    : 0;
                CurrentB2 = (Math.Round(CurrentPercentageStep2 * CurrentB / 100.0, 2));
            }
            finally { _updating = false; }
        }
        partial void OnCurrentB2Changed(double value)
        {
            if (_updating) return;
            try
            {
                _updating = true;
                var baseB = CurrentB;
                CurrentPercentageStep2 = baseB > 0
                    ? Clamp(Math.Round(value / baseB * 100.0, 2), 0, 100)
                    : 0;
                CurrentA2 = (Math.Round(CurrentPercentageStep2 * CurrentA / 100.0, 2));
            }
            finally { _updating = false; }
        }
        private void RecalcStep2(double percent)
        {
            if (_updating) return;
            try
            {
                _updating = true;
                var p = Clamp(percent, 0, 100);
                CurrentA2 = Math.Round(CurrentA * p / 100.0, 2);
                CurrentB2 = Math.Round(CurrentB * p / 100.0, 2);
            }
            finally { _updating = false; }
        }
        partial void OnCurrentA3Changed(double value)
        {
            if (_updating) return;
            try
            {
                _updating = true;
                var baseA = CurrentA;
                CurrentPercentageStep3 = baseA > 0
                    ? Clamp(Math.Round(value / baseA * 100.0, 2), 0, 100)
                    : 0;
                CurrentB3 = (Math.Round(CurrentPercentageStep3 * CurrentB / 100.0, 2));
            }
            finally { _updating = false; }
        }
        partial void OnCurrentB3Changed(double value)
        {
            if (_updating) return;
            try
            {
                _updating = true;
                var baseB = CurrentB;
                CurrentPercentageStep3 = baseB > 0
                    ? Clamp(Math.Round(value / baseB * 100.0, 2), 0, 100)
                    : 0;
                CurrentA3 = (Math.Round(CurrentPercentageStep3 * CurrentA / 100.0, 2));
            }
            finally { _updating = false; }
        }
        private void RecalcStep3(double percent)
        {
            if (_updating) return;
            try
            {
                _updating = true;
                var p = Clamp(percent, 0, 100);
                CurrentA3 = Math.Round(CurrentA * p / 100.0, 2);
                CurrentB3 = Math.Round(CurrentB * p / 100.0, 2);
            }
            finally { _updating = false; }
        }

        partial void OnCurrentA4Changed(double value)
        {
            if (_updating) return;
            try
            {
                _updating = true;
                var baseA = CurrentA;
                CurrentPercentageStep4 = baseA > 0
                    ? Clamp(Math.Round(value / baseA * 100.0, 2), 0, 100)
                    : 0;
                CurrentB4 = (Math.Round(CurrentPercentageStep4 * CurrentB / 100.0, 2));
            }
            finally { _updating = false; }
        }
        partial void OnCurrentB4Changed(double value)
        {
            if (_updating) return;
            try
            {
                _updating = true;
                var baseB = CurrentB;
                CurrentPercentageStep4 = baseB > 0
                    ? Clamp(Math.Round(value / baseB * 100.0, 2), 0, 100)
                    : 0;
                CurrentA4 = (Math.Round(CurrentPercentageStep4 * CurrentA / 100.0, 2));
            }
            finally { _updating = false; }
        }
        private void RecalcStep4(double percent)
        {
            if (_updating) return;
            try
            {
                _updating = true;
                var p = Clamp(percent, 0, 100);
                CurrentA4 = Math.Round(CurrentA * p / 100.0, 2);
                CurrentB4 = Math.Round(CurrentB * p / 100.0, 2);
            }
            finally { _updating = false; }
        }

        partial void OnCurrentA5Changed(double value)
        {
            if (_updating) return;
            try
            {
                _updating = true;
                var baseA = CurrentA;
                CurrentPercentageStep5 = baseA > 0
                    ? Clamp(Math.Round(value / baseA * 100.0, 2), 0, 100)
                    : 0;
                CurrentB5 = (Math.Round(CurrentPercentageStep5 * CurrentB / 100.0, 2));
            }
            finally { _updating = false; }
        }
        partial void OnCurrentB5Changed(double value)
        {
            if (_updating) return;
            try
            {
                _updating = true;
                var baseB = CurrentB;
                CurrentPercentageStep5 = baseB > 0
                    ? Clamp(Math.Round(value / baseB * 100.0, 2), 0, 100)
                    : 0;
                CurrentA5 = (Math.Round(CurrentPercentageStep5 * CurrentA / 100.0, 2));
            }
            finally { _updating = false; }
        }
        private void RecalcStep5(double percent)
        {
            if (_updating) return;
            try
            {
                _updating = true;
                var p = Clamp(percent, 0, 100);
                CurrentA5 = Math.Round(CurrentA * p / 100.0, 2);
                CurrentB5 = Math.Round(CurrentB * p / 100.0, 2);
            }
            finally { _updating = false; }
        }

        partial void OnCurrentA6Changed(double value)
        {
            if (_updating) return;
            try
            {
                _updating = true;
                var baseA = CurrentA;
                CurrentPercentageStep6 = baseA > 0
                    ? Clamp(Math.Round(value / baseA * 100.0, 2), 0, 100)
                    : 0;
                CurrentB6 = (Math.Round(CurrentPercentageStep6 * CurrentB / 100.0, 2));
            }
            finally { _updating = false; }
        }
        partial void OnCurrentB6Changed(double value)
        {
            if (_updating) return;
            try
            {
                _updating = true;
                var baseB = CurrentB;
                CurrentPercentageStep6 = baseB > 0
                    ? Clamp(Math.Round(value / baseB * 100.0, 2), 0, 100)
                    : 0;
                CurrentA6 = (Math.Round(CurrentPercentageStep6 * CurrentA / 100.0, 2));
            }
            finally { _updating = false; }
        }
        private void RecalcStep6(double percent)
        {
            if (_updating) return;
            try
            {
                _updating = true;
                var p = Clamp(percent, 0, 100);
                CurrentA6 = Math.Round(CurrentA * p / 100.0, 2);
                CurrentB6 = Math.Round(CurrentB * p / 100.0, 2);
            }
            finally { _updating = false; }
        }

        partial void OnCurrentA7Changed(double value)
        {
            if (_updating) return;
            try
            {
                _updating = true;
                var baseA = CurrentA;
                CurrentPercentageStep7 = baseA > 0
                    ? Clamp(Math.Round(value / baseA * 100.0, 2), 0, 100)
                    : 0;
                CurrentB7 = (Math.Round(CurrentPercentageStep7 * CurrentB / 100.0, 2));
            }
            finally { _updating = false; }
        }
        partial void OnCurrentB7Changed(double value)
        {
            if (_updating) return;
            try
            {
                _updating = true;
                var baseB = CurrentB;
                CurrentPercentageStep7 = baseB > 0
                    ? Clamp(Math.Round(value / baseB * 100.0, 2), 0, 100)
                    : 0;
                CurrentA7 = (Math.Round(CurrentPercentageStep7 * CurrentA / 100.0, 2));
            }
            finally { _updating = false; }
        }
        private void RecalcStep7(double percent)
        {
            if (_updating) return;
            try
            {
                _updating = true;
                var p = Clamp(percent, 0, 100);
                CurrentA7 = Math.Round(CurrentA * p / 100.0, 2);
                CurrentB7 = Math.Round(CurrentB * p / 100.0, 2);
            }
            finally { _updating = false; }
        }

        partial void OnCurrentA8Changed(double value)
        {
            if (_updating) return;
            try
            {
                _updating = true;
                var baseA = CurrentA;
                CurrentPercentageStep8 = baseA > 0
                    ? Clamp(Math.Round(value / baseA * 100.0, 2), 0, 100)
                    : 0;
                CurrentB8 = (Math.Round(CurrentPercentageStep8 * CurrentB / 100.0, 2));
            }
            finally { _updating = false; }
        }
        partial void OnCurrentB8Changed(double value)
        {
            if (_updating) return;
            try
            {
                _updating = true;
                var baseB = CurrentB;
                CurrentPercentageStep8 = baseB > 0
                    ? Clamp(Math.Round(value / baseB * 100.0, 2), 0, 100)
                    : 0;
                CurrentA8 = (Math.Round(CurrentPercentageStep8 * CurrentA / 100.0, 2));
            }
            finally { _updating = false; }
        }
        private void RecalcStep8(double percent)
        {
            if (_updating) return;
            try
            {
                _updating = true;
                var p = Clamp(percent, 0, 100);
                CurrentA8 = Math.Round(CurrentA * p / 100.0, 2);
                CurrentB8 = Math.Round(CurrentB * p / 100.0, 2);
            }
            finally { _updating = false; }
        }

        partial void OnCurrentA9Changed(double value)
        {
            if (_updating) return;
            try
            {
                _updating = true;
                var baseA = CurrentA;
                CurrentPercentageStep9 = baseA > 0
                    ? Clamp(Math.Round(value / baseA * 100.0, 2), 0, 100)
                    : 0;
                CurrentB9 = (Math.Round(CurrentPercentageStep9 * CurrentB / 100.0, 2));
            }
            finally { _updating = false; }
        }
        partial void OnCurrentB9Changed(double value)
        {
            if (_updating) return;
            try
            {
                _updating = true;
                var baseB = CurrentB;
                CurrentPercentageStep9 = baseB > 0
                    ? Clamp(Math.Round(value / baseB * 100.0, 2), 0, 100)
                    : 0;
                CurrentA9 = (Math.Round(CurrentPercentageStep9 * CurrentA / 100.0, 2));
            }
            finally { _updating = false; }
        }
        private void RecalcStep9(double percent)
        {
            if (_updating) return;
            try
            {
                _updating = true;
                var p = Clamp(percent, 0, 100);
                CurrentA9 = Math.Round(CurrentA * p / 100.0, 2);
                CurrentB9 = Math.Round(CurrentB * p / 100.0, 2);
            }
            finally { _updating = false; }
        }

        partial void OnCurrentA10Changed(double value)
        {
            if (_updating) return;
            try
            {
                _updating = true;
                var baseA = CurrentA;
                CurrentPercentageStep10 = baseA > 0
                    ? Clamp(Math.Round(value / baseA * 100.0, 2), 0, 100)
                    : 0;
                CurrentB10 = (Math.Round(CurrentPercentageStep10 * CurrentB / 100.0, 2));
            }
            finally { _updating = false; }
        }
        partial void OnCurrentB10Changed(double value)
        {
            if (_updating) return;
            try
            {
                _updating = true;
                var baseB = CurrentB;
                CurrentPercentageStep10 = baseB > 0
                    ? Clamp(Math.Round(value / baseB * 100.0, 2), 0, 100)
                    : 0;
                CurrentA10 = (Math.Round(CurrentPercentageStep10 * CurrentA / 100.0, 2));
            }
            finally { _updating = false; }
        }
        private void RecalcStep10(double percent)
        {
            if (_updating) return;
            try
            {
                _updating = true;
                var p = Clamp(percent, 0, 100);
                CurrentA10 = Math.Round(CurrentA * p / 100.0, 2);
                CurrentB10 = Math.Round(CurrentB * p / 100.0, 2);
            }
            finally { _updating = false; }
        }


        #endregion

        // 當基準（密度或面積）變了 → 依「百分比」重新計算電流
        //partial void OnCurrentDensityAChanged(double v) => RecalcFromPercent();
        //partial void OnAreaAChanged(double v) => RecalcFromPercent();

        //private void RecalcFromPercent()
        //{
        //    if (_updating) return;
        //    try
        //    {
        //        _updating = true;
        //        CurrentA1 = Math.Round(CurrentA * CurrentPercentageStep1 / 100.0, 2);
        //    }
        //    finally { _updating = false; }
        //}

        private static double Clamp(double x, double min, double max)
            => x < min ? min : (x > max ? max : x);

        private static OverlayWindow _overlay = new OverlayWindow();

        static void ShowOverlay()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _overlay ??= new OverlayWindow { Topmost = true, ShowActivated = false };
                _overlay.Show();
                _overlay.Activate();
            });
        }

        static void HideOverlay()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _overlay?.Close();
                _overlay = null;
            });
        }

        static void SetOverlayClickThrough(bool enable)
        {
            //Application.Current.Dispatcher.Invoke(() =>
            //{
            //    _overlay?.SetClickThrough(enable);
            //});

            //WC 2025/10/14 加入 try catch
            if (Application.Current == null) return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    _overlay?.SetClickThrough(enable);
                }
                catch (Exception ex)
                {
                    // 記錄 log，避免 UI thread crash
                    CyntecLog.GeneralLogger.Error($"SetOverlayClickThrough error: {ex}");
                }
            });
        }

        private string _lastReportedStatus = string.Empty;

        public mainModelView()
        {
            // 初次驗證（可選）
            ValidateAllProperties();
            this._S2f23SvidInfo = new List<string> { "1021", "1022" };
            isReadyToStart = true;

            S5F1_Timer = new System.Timers.Timer();
            S5F1_Timer.Interval = 1000 * 3;
            S5F1_Timer.Elapsed += S5F1_Timer_Elapsed;
            S5F1_Timer.Start();

            // 初始化滑鼠位置
            GetCursorPos(out lastMousePos);
            lastMouseMoveTime = DateTime.Now;

            // 每秒檢查滑鼠
            S6F1_CheckTimer = new System.Timers.Timer(1000);
            S6F1_CheckTimer.Elapsed += S6F1_CheckTimer_Elapsed;
            S6F1_CheckTimer.AutoReset = true;
            S6F1_CheckTimer.Start();

            S6F1_Timer = new System.Timers.Timer(60000);
            S6F1_Timer.Elapsed += S6F1_Timer_Elapsed;
            S6F1_Timer.AutoReset = true;
            S6F1_Timer.Start();

            this._logger = (ISecsGemLogger)new SecsLogger(this, CyntecLog.SecsLogger);
            _S5f1AlarmInfo.OnChanged += _S5f1AlarmInfo_OnChanged;

            ConnectedTimer = new System.Timers.Timer(1000);
            ConnectedTimer.Elapsed += ConnectedTimer_Elapsed;
            ConnectedTimer.AutoReset = true;
            ConnectedTimer.Start();
        }

        //WC 2026/05/11 抽方法
        private async void S6F1_Timer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            S6F1_Timer.Stop();

            try
            {
                await SendS6F1();
            }
            finally
            {
                if (!isStopTimer)
                    S6F1_Timer.Start();
            }
        }

        //WC 2026/05/11 抽方法，可以先立即執行
        private async Task SendS6F1()
        {
            // 模擬模式下不執行實體機台爬畫面與 PLC 讀取
            if (IsSimulationMode)
                return;

            // 若正在進行派送或載入配方之 UI 自動化流程，不可執行 S6F1 爬畫面
            if (!isReadyToStart || IsBusy)
                return;

            // 已有執行中的 S6F1
            if (!await S6F1Lock.WaitAsync(0))
                return;

            try
            {
                if (!isReadyToStart || IsBusy)
                    return;

                // 沒連線不送 (改為檢查 MQTT 連線狀態)
                if (_mqtt == null || !_mqtt.IsConnected)
                    return;

                if (this._S2f23SvidInfo.Count <= 0)
                {
                    this._S2f23SvidInfo = new List<string> { "1021", "1022" };
                }

                var vidMsg_Info = GetS1F3MachineValue(this._S2f23SvidInfo);
                if (vidMsg_Info == null || vidMsg_Info.Count <= 0)
                    return;

                // GetS1F3MachineValue 回傳順序：
                // [0] = 1022 DataGridView JSON
                // [1] = 1021 機台狀態 ("RUN" / "IDLE" / "ALARM")
                // [2] = PLC D8010 用水量 (Water_Consumption)
                string rawDataGridJson = vidMsg_Info.Count > 0 ? vidMsg_Info[0] : string.Empty;
                string machineStatus = vidMsg_Info.Count > 1 ? vidMsg_Info[1] : IDLE;
                string waterConsumption = vidMsg_Info.Count > 2 ? vidMsg_Info[2] : "0";

                if (_S5f1AlarmInfo.Count > 0 && string.IsNullOrEmpty(machineStatus))
                {
                    machineStatus = ALARM;
                }
                else if (string.IsNullOrEmpty(machineStatus))
                {
                    machineStatus = IDLE;
                }

                // 若機台狀態有變化，主動發送 StatusChangeReport
                if (!string.Equals(_lastReportedStatus, machineStatus, StringComparison.OrdinalIgnoreCase))
                {
                    string oldStatus = string.IsNullOrEmpty(_lastReportedStatus) ? IDLE : _lastReportedStatus;
                    _lastReportedStatus = machineStatus;

                    var statusReq = new StatusChangeReportRequest
                    {
                        TransactionID = Guid.NewGuid().ToString(),
                        CMD = MqttCommands.StatusChangeReport,
                        DATE = DateTime.Now.ToString("yyyyMMddHHmmss"),
                        Machine = Config.EqpNo,
                        OldStatus = oldStatus,
                        NewStatus = machineStatus
                    };
                    await _mqtt.PublishReportAsync(statusReq);
                    CyntecLog.GeneralLogger.Trace($"[TraceData] StatusChangeReport Published Old={oldStatus}, New={machineStatus}");
                }

                var processDataList = BuildProcessDataParameters(rawDataGridJson, waterConsumption);

                var processDataReq = new ProcessDataReportRequest
                {
                    TransactionID = Guid.NewGuid().ToString(),
                    CMD = MqttCommands.ProcessDataReport,
                    DATE = DateTime.Now.ToString("yyyyMMddHHmmss"),
                    Machine = Config.EqpNo,
                    ProcessData = processDataList
                };

                await _mqtt.PublishReportAsync(processDataReq);
                CyntecLog.GeneralLogger.Trace($"[TraceData] ProcessDataReport Published TransactionID={processDataReq.TransactionID}, Status={machineStatus}, Water={waterConsumption}, ParamCount={processDataList.Count}");
            }
            catch (OperationCanceledException)
            {
                // Task 被取消
            }
            catch (Exception ex)
            {
                // 其它異常
                CyntecLog.GeneralLogger.Error($"SendS6F1 Error : {ex}");
            }
            finally
            {
                S6F1Lock.Release();
            }
        }

        /// <summary>
        /// 將 DataGridView 原始 JSON 與 PLC 用水量拆解為新版 EAP ProcessDataReport 所需之中文參數清單。
        /// 對應舊版 EAP CyntecMES.CreatParamDT 的槽位與欄位規則。
        /// </summary>
        private static List<ProcessDataParameter> BuildProcessDataParameters(string rawDataGridJson, string waterConsumption)
        {
            var processDataList = new List<ProcessDataParameter>();

            if (!string.IsNullOrWhiteSpace(rawDataGridJson))
            {
                try
                {
                    var dataGridInfo = JsonConvert.DeserializeObject<AutoCIMDataGridInfo>(rawDataGridJson);
                    if (dataGridInfo?.Rtn_Value != null)
                    {
                        foreach (var row in dataGridInfo.Rtn_Value)
                        {
                            if (row == null || !row.TryGetValue("Col1", out string? rawTankId) || string.IsNullOrWhiteSpace(rawTankId))
                                continue;

                            string tankId = rawTankId.Trim();
                            switch (tankId)
                            {
                                case "005#":
                                    processDataList.Add(new ProcessDataParameter { Parameter = "熱水洗#5-溫度", Value = ExtractTemperature(row) });
                                    break;
                                case "016#":
                                    processDataList.Add(new ProcessDataParameter { Parameter = "酸洗#16-溫度", Value = ExtractTemperature(row) });
                                    break;
                                case "019#":
                                    processDataList.Add(new ProcessDataParameter { Parameter = "熱水洗#19-溫度", Value = ExtractTemperature(row) });
                                    break;
                                case "020#":
                                    processDataList.Add(new ProcessDataParameter { Parameter = "酸脫#20-溫度", Value = ExtractTemperature(row) });
                                    break;
                                case "009#":
                                case "010#":
                                case "011#":
                                case "012#":
                                case "013#":
                                    string tankNo = tankId.TrimEnd('#').TrimStart('0');
                                    var (currentA, voltageA) = ParseCurrentVoltage(row, "Col15");
                                    var (currentB, voltageB) = ParseCurrentVoltage(row, "Col17");
                                    string temperature = ExtractTemperature(row);

                                    processDataList.Add(new ProcessDataParameter { Parameter = $"鍍銅#{tankNo}-A面-電流", Value = currentA });
                                    processDataList.Add(new ProcessDataParameter { Parameter = $"鍍銅#{tankNo}-B面-電流", Value = currentB });
                                    processDataList.Add(new ProcessDataParameter { Parameter = $"鍍銅#{tankNo}-A面-電壓", Value = voltageA });
                                    processDataList.Add(new ProcessDataParameter { Parameter = $"鍍銅#{tankNo}-B面-電壓", Value = voltageB });
                                    processDataList.Add(new ProcessDataParameter { Parameter = $"鍍銅#{tankNo}-溫度", Value = temperature });
                                    break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    CyntecLog.GeneralLogger.Error($"[TraceData] 解析 DataGridView JSON 拆解製程參數失敗: {ex}");
                }
            }

            // 固定附加 PLC 用水量 (D8010 * 0.01)
            processDataList.Add(new ProcessDataParameter
            {
                Parameter = $"{Config.EqpNo}用水量",
                Value = string.IsNullOrWhiteSpace(waterConsumption) ? "0" : waterConsumption.Trim()
            });

            // 固定附加未拆解的 DataGridView 原始 JSON 字串，供新版 EAP 轉發給 T1 看板系統
            processDataList.Add(new ProcessDataParameter
            {
                Parameter = "RAWDATA",
                Value = rawDataGridJson ?? string.Empty
            });

            return processDataList;
        }

        private static string ExtractTemperature(Dictionary<string, string> row)
        {
            if (row.TryGetValue("Col8", out string? rawTemp) && !string.IsNullOrWhiteSpace(rawTemp))
            {
                return rawTemp.Trim().Replace("℃", string.Empty).Trim();
            }
            return string.Empty;
        }

        private static (string Current, string Voltage) ParseCurrentVoltage(Dictionary<string, string> row, string columnKey)
        {
            if (!row.TryGetValue(columnKey, out string? rawValue) || string.IsNullOrWhiteSpace(rawValue))
            {
                return (string.Empty, string.Empty);
            }

            string cleaned = rawValue.Replace("A", string.Empty).Replace("V", string.Empty).Trim();
            string[] parts = cleaned.Split('/');
            string current = parts.Length > 0 ? parts[0].Trim() : string.Empty;
            string voltage = parts.Length > 1 ? parts[1].Trim() : string.Empty;
            return (current, voltage);
        }

        //WC 2025/10/14 Add 監控滑鼠事件，來停止 S6F1_Timer  //WC 2026/05/11 Add async/await
        private async void S6F1_CheckTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            const int MAX_WAIT_SEC = 30;

            try
            {
                // 取得全螢幕滑鼠位置
                GetCursorPos(out POINT currentPos);

                if (currentPos.X != lastMousePos.X || currentPos.Y != lastMousePos.Y)
                {
                    // 滑鼠有移動
                    lastMouseMoveTime = DateTime.Now;
                    lastMousePos = currentPos;

                    if (S6F1_Timer.Enabled && !isStopTimer)
                    {
                        isStopTimer = true;
                        S6F1_Timer.Stop();
                        CyntecLog.GeneralLogger.Debug("S6F1_Timer 停止 (因為滑鼠移動)");
                    }
                }
                else
                {
                    // 滑鼠沒動
                    if (isReadyToStart && (DateTime.Now - lastMouseMoveTime).TotalSeconds >= MAX_WAIT_SEC)
                    {
                        if (!S6F1_Timer.Enabled && isStopTimer)
                        {
                            isStopTimer = false;

                            //WC 2026/05/11 立即送一次
                            await SendS6F1();

                            S6F1_Timer.Start();
                            CyntecLog.GeneralLogger.Debug($"S6F1_Timer 啟動 (滑鼠靜止 {MAX_WAIT_SEC} 秒)");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CyntecLog.GeneralLogger.Error($"S6F1_CheckTimer_Elapsed ex: {ex}");
            }
        }

        private void ConnectedTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            {
                UpdateConnectionState();
            }));          
        }

        private async void _S5f1AlarmInfo_OnChanged(string action, string key, string value)
        {
            CyntecLog.GeneralLogger.Debug($"動作: {action}, Key={key}, Value={value}");
            if (_mqtt == null || !_mqtt.IsConnected)
                return;

            try
            {
                string alarmStatus = string.Empty;
                if (action == "Add" || action == "Update")
                {
                    alarmStatus = "1";
                }
                else if (action == "Remove")
                {
                    alarmStatus = "0";
                }
                else
                {
                    return;
                }

                var alarmReq = new AlarmReportRequest
                {
                    TransactionID = Guid.NewGuid().ToString(),
                    CMD = MqttCommands.AlarmReport,
                    DATE = DateTime.Now.ToString("yyyyMMddHHmmss"),
                    Machine = Config.EqpNo,
                    AlarmStatus = alarmStatus,
                    AlarmCode = key,
                    AlarmMsg = value ?? string.Empty
                };

                await _mqtt.PublishReportAsync(alarmReq);
                CyntecLog.GeneralLogger.Trace($"[Alarm] AlarmReport Published TransactionID={alarmReq.TransactionID}, Action={action}, Code={key}, Status={alarmStatus}");
                AddLog($"[警報上報] 狀態={(alarmStatus == "1" ? "發生" : "解除")}, 代碼={key}, 訊息={value}", alarmStatus == "1" ? Brushes.Red : Brushes.Green);
            }
            catch (Exception ex)
            {
                CyntecLog.GeneralLogger.Error($"_S5f1AlarmInfo_OnChanged Error: {ex}");
            }
        }

        private bool _AlarmFlag = false;
        private static ObservableDictionary<string, string> _S5f1AlarmInfo = new();
        private void S5F1_Timer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            if (IsSimulationMode) return; // 模擬模式下不爬警報畫面
            if (_mqtt == null || !_mqtt.IsConnected) return; //沒連上HOST就不做

            S5F1_Timer.Stop();
            try
            {
#warning 寫到設定檔案! 或是統一變數!
                var consolePath = @$"{AppDomain.CurrentDomain.BaseDirectory}\UIControl\_AutoCIMOrigin.exe";
                var consoleMsg = string.Join(",", "GetDataGridView", Utility.Svid_Info[Utility.VID.警告DGV][0], Utility.Svid_Info[Utility.VID.警告DGV][1]);
                var Result = GetConsoleProcessValue(consolePath, consoleMsg);
                //CyntecLog.GeneralLogger.Trace($"{Result.ToString()}");
                {
                    var r = SyncAlarmsFromRaw(Result.ToString());
                    if (r.result == SyncResult.Error)
                    {
                        CyntecLog.GeneralLogger.Error($"S5F1 SyncResult.Error: {r.message}, Result: {Result.ToString()}");
                    }
                }
            }
            catch (Exception ex)
            {
                CyntecLog.GeneralLogger.Error($"S5F1 ex: {ex}");
            }
            finally
            {
                S5F1_Timer.Start();
            }
        }

        public ObservableCollection<LogItem> Logs { get; } = new();
        public int MaxLines { get; set; } = 5000;

        public void AddLog(string msg, Brush color)
        {
            // 確保在 UI 執行緒
            if (Application.Current.Dispatcher.CheckAccess())
            {
                AddCore(msg, color);
            }
            else
            {
                Application.Current.Dispatcher.Invoke(() => AddCore(msg, color));
            }
        }

        private void AddCore(string msg, Brush color)
        {
            string timestampedMsg = $"[{DateTime.Now:HH:mm:ss}] {msg}";
            Logs.Add(new LogItem { Message = timestampedMsg, Color = color });
            if (Logs.Count > MaxLines) Logs.RemoveAt(0);
        }

        private void ShowMessageBoxWindow(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                message = "異常發生";

            if (Application.Current == null)
                return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    if (_messageBoxWindow == null || !_messageBoxWindow.IsVisible)
                    {
                        _messageBoxWindow = new MessageBoxWindow();
                    }
                    _messageBoxWindow.UpdateMessage(message);
                }
                catch (Exception ex)
                {
                    CyntecLog.GeneralLogger.Error($"ShowMessageBoxWindow Error: {ex}");
                }
            });
        }

        private string _activeTrackInTxId = string.Empty;
        private string _activeTrackOutTxId = string.Empty;
        private const int MqttReplyTimeoutMs = 30000;

        private async void StartTrackInTimeoutWatcher(string txId, string lotId)
        {
            try
            {
                await Task.Delay(MqttReplyTimeoutMs, _cts.Token);
                if (IsBusy && string.Equals(_activeTrackInTxId, txId, StringComparison.Ordinal))
                {
                    _activeTrackInTxId = string.Empty;
                    IsBusy = false;
                    isReadyToStart = true;
                    HideOverlay();
                    string timeoutMsg = $"工單 {lotId} 進站等待 EAP 回覆逾時 ({MqttReplyTimeoutMs / 1000}秒)";
                    CyntecLog.GeneralLogger.Warn($"[MoveIn] {timeoutMsg}, TransactionID={txId}");
                    AddLog(timeoutMsg, Brushes.Red);
                    ShowMessageBoxWindow(timeoutMsg);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                CyntecLog.GeneralLogger.Error($"StartTrackInTimeoutWatcher Error: {ex}");
            }
        }

        private async void StartTrackOutTimeoutWatcher(string txId, string workOrder)
        {
            try
            {
                await Task.Delay(MqttReplyTimeoutMs, _cts.Token);
                if (IsBusyMoveOut && string.Equals(_activeTrackOutTxId, txId, StringComparison.Ordinal))
                {
                    _activeTrackOutTxId = string.Empty;
                    SetIsBusyMoveOut(false);
                    HideOverlay();
                    string timeoutMsg = $"工單 {workOrder} 出站等待 EAP 回覆逾時 ({MqttReplyTimeoutMs / 1000}秒)";
                    CyntecLog.GeneralLogger.Warn($"[MoveOut] {timeoutMsg}, TransactionID={txId}");
                    AddLog(timeoutMsg, Brushes.Red);
                    ShowMessageBoxWindow(timeoutMsg);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                CyntecLog.GeneralLogger.Error($"StartTrackOutTimeoutWatcher Error: {ex}");
            }
        }

        // 當屬性改變時做即時驗證
        partial void OnWorkOrderNoChanged(string value) => ValidateProperty(value, nameof(WorkOrderNo));
        partial void OnEmployeeIdChanged(string value) => ValidateProperty(value, nameof(EmployeeId));

        //WC 2025/11/03
        partial void OnTargetNoChanged(string value) => ValidateProperty(value, nameof(TargetNo));
        partial void OnLeftRackChanged(string value) => ValidateProperty(value, nameof(LeftRack));
        partial void OnRightRackChanged(string value) => ValidateProperty(value, nameof(RightRack));

        //WC 2025/12/09 Add MoveOut
        partial void OnWorkOrderNoMoveOutChanged(string value) => ValidateProperty(value, nameof(WorkOrderNoMoveOut));
        partial void OnEmployeeIdMoveOutChanged(string value) => ValidateProperty(value, nameof(EmployeeIdMoveOut));

        //mark
        //partial void OnSlotNoMoveOutChanged(string value) => ValidateProperty(value, nameof(SlotNoMoveOut));

        // 判斷是否可派送：欄位都有效且非忙碌
        //private bool CanDispatch() => !IsBusy && !HasErrors && _connector?.State == Secs4Net.ConnectionState.Selected;
        //private bool CanDispatch() => !IsBusy && !DispatchHasErrors && _connector?.State == Secs4Net.ConnectionState.Selected;

        //private bool CanMoveOut() => !IsBusyMoveOut && !MoveOutHasErrors && _connector?.State == Secs4Net.ConnectionState.Selected;

        private bool CanDispatch() => !IsBusy && !DispatchHasErrors && ConnectionState == eConnectionState.Connected;

        private bool CanMoveOut() => !IsBusyMoveOut && !MoveOutHasErrors && ConnectionState == eConnectionState.Connected;

        //HasErrors 拆成兩個 HasErrors 的方法
        public bool DispatchHasErrors => GetDispatchErrors().Any();

        public bool MoveOutHasErrors => GetMoveOutErrors().Any();

        private IEnumerable<ValidationResult> GetDispatchErrors()
        {
            // 重新驗證
            ValidateProperty(WorkOrderNo, nameof(WorkOrderNo));
            ValidateProperty(EmployeeId, nameof(EmployeeId));
            ValidateProperty(TargetNo, nameof(TargetNo));
            ValidateProperty(LeftRack, nameof(LeftRack));
            ValidateProperty(RightRack, nameof(RightRack));

            // 彙整錯誤 (只針對派送區域)
            return GetErrors(nameof(WorkOrderNo)).Cast<ValidationResult>()
                  .Concat(GetErrors(nameof(EmployeeId)).Cast<ValidationResult>())
                  .Concat(GetErrors(nameof(TargetNo)).Cast<ValidationResult>())
                  .Concat(GetErrors(nameof(LeftRack)).Cast<ValidationResult>())
                  .Concat(GetErrors(nameof(RightRack)).Cast<ValidationResult>());
        }

        private IEnumerable<ValidationResult> GetMoveOutErrors()
        {
            ValidateProperty(WorkOrderNoMoveOut, nameof(WorkOrderNoMoveOut));
            ValidateProperty(EmployeeIdMoveOut, nameof(EmployeeIdMoveOut));
            //ValidateProperty(SlotNoMoveOut, nameof(SlotNoMoveOut)); //mark
            return GetErrors(nameof(WorkOrderNoMoveOut)).Cast<ValidationResult>()
                  .Concat(GetErrors(nameof(EmployeeIdMoveOut)).Cast<ValidationResult>());
            //.Concat(GetErrors(nameof(SlotNoMoveOut)).Cast<ValidationResult>()); //mark
        }

        // 派送（非同步）
        [RelayCommand(CanExecute = nameof(CanDispatch))]
        private async Task DispatchAsync()
        {
            //object value = Utility.確認監控程式是否處於正常();

            // 再保險驗證一次
            ValidateAllProperties();
            if (DispatchHasErrors) return;

            bool published = false;
            try
            {
                IsBusy = true;
                string lotId = WorkOrderNo.Trim();
                string userId = EmployeeId.Trim();

                if (string.IsNullOrEmpty(lotId) || string.IsNullOrEmpty(userId))
                {
                    //MainForm.CyntecLog.GeneralLogger.WriteLog("人員未輸入批號&工號&QR_CODE!");
                    return;
                }

                //// 檢查 Secs 連線狀態
                //if (_secsGem == null || _connector == null || _connector.State != Secs4Net.ConnectionState.Selected)
                //{
                //    // 未連線狀態，不送 S6F11
                //    return;
                //}

                // 檢查 MQTT / EAP 連線狀態
                if (_mqtt == null || !_mqtt.IsConnected || !_mqtt.IsMesAlive)
                {
                    AddLog("派送失敗: MQTT / EAP 未連線", Brushes.Red);
                    return;
                }

                try
                {
                    var paramInfo = new Dictionary<string, string>
                    {
                        { "LOTID", lotId },
                        { "USERID", userId },
                        { "QRCODE_01", ""}, //預留
                        { "QRCODE_02", ""},
                        { "QRCODE_03", ""},
                        { "QRCODE_04", ""},
                        { "remark01", ""},
                        { "remark02", ""},
                        { "remark03", ""},
                        { "remark04", ""} //預留
                    };

                    //============WC 2025/11/03 派送前將載入資料重置，避免人員使用到上次紀錄的 ==========================
                    if (!IsSimulationMode)
                    {
                        isReadyToStart = false;
                        S6F1_Timer.Stop();
                        ShowOverlay();

                        CyntecLog.GeneralLogger.Trace("S6F11 Send 1");
                        {
                            CloseIfExists("數據加載");
                            CloseIfExists("產品資料");
                        }

                        // Activate window
                        CyntecLog.GeneralLogger.Trace("clickDataLoad");
                        WinAuto.clickDataLoad();
                        Thread.Sleep(1500); //原程式本來開這頁就很久

                        #region 選到第一槽 001# 上下料
                        {
                            string win = Utility.Svid_Info[Utility.VID.加載槽號][0]; // 或整個主視窗標題
                            string comboId = Utility.Svid_Info[Utility.VID.加載槽號][1];// "[CLASS:WindowsForms10.COMBOBOX.app.0.34f5582_r7_ad1; INSTANCE:11]";

                            AutoItX.WinActivate(win);
                            AutoItX.WinWaitActive(win, "", 2);

                            // 聚焦到 Combo
                            AutoItX.ControlFocus(win, "", comboId);

                            // 展開下拉
                            AutoItX.ControlCommand(win, "", comboId, "ShowDropDown", "");

                            // 選第一項（移到清單頂端）
                            AutoItX.ControlSend(win, "", comboId, "{HOME}");
                            AutoItX.ControlSend(win, "", comboId, "{ENTER}");
                        }
                        Thread.Sleep(500);
                        #endregion

                        // Click button
                        SetOverlayClickThrough(true);
                        AutoItX.ControlClick(Utility.Svid_Info[Utility.VID.清除當前][0], "", Utility.Svid_Info[Utility.VID.清除當前][1]);
                        SetOverlayClickThrough(false);
                        Thread.Sleep(500);

                        Utility.CaptureAllScreens($"S6F11", 1);

                        // Click prompt button
                        SetOverlayClickThrough(true);
                        AutoItX.ControlClick("提示", "", "[CLASS:Button; INSTANCE:1]"); //popout - 確定  Thread.Sleep(500);
                        SetOverlayClickThrough(false);
                        Thread.Sleep(1500); //PLC清除需要時間

                        CyntecLog.GeneralLogger.Trace("S6F11 Send 2");
                        {
                            CloseIfExists("數據加載");
                            CloseIfExists("產品資料");
                        }
                    }
                    else
                    {
                        AddLog("[模擬-進站派送] 模擬模式跳過機台清除畫面，直接發送 TrackInReq", Brushes.DarkOrange);
                    }
                    //===========================================

                    //CyntecLog.GeneralLogger.Debug($"S6F11, MOVE_IN, lotId: {lotId}, userId: {userId}");
                    //// 發送 S6F11 MOVE_IN //帶入批號, 工號
                    //SecsMessage reply = await _secsGem.SendAsync(
                    //    SECSBuilder.BuildS6F11Message("MOVE_IN", paramInfo),
                    //    _cancellationTokenSource.Token);

                    ////reply 內容可以處理或忽略

                    // ========================================
                    // MES TrackIn
                    // ========================================

                    var request = new TrackInRequest
                    {
                        TransactionID = Guid.NewGuid().ToString(),
                        CMD = MqttCommands.TrackInReq,
                        DATE = DateTime.Now.ToString("yyyyMMddHHmmss"),
                        Machine = Config.EqpNo,
                        WorkOrder = lotId,
                        UserID = userId
                    };

                    _activeTrackInTxId = request.TransactionID;
                    await _mqtt.PublishReportAsync(request);
                    published = true;

                    CyntecLog.GeneralLogger.Trace($"[MoveIn] TrackInReq Published TransactionID={request.TransactionID}, WorkOrder={lotId}, UserID={userId}");
                    AddLog($"[進站派送] 已發送進站請求: 工單={lotId}, 工號={userId}", Brushes.Blue);
                    StartTrackInTimeoutWatcher(request.TransactionID, lotId);

                    // ========================================
                    // 注意：
                    // 這裡不等待 MES Reply
                    //
                    // ReplyTrackInReq 由 MqttService_ReplyReceived()
                    // 處理
                    // ========================================                   
                }
                catch (SecsException ex)
                {
                    CyntecLog.GeneralLogger.Error($"DispatchAsync SecsException: {ex}");
                    AddLog($"派送流程異常: {ex.Message}", Brushes.Red);
                }
            }
            catch (OperationCanceledException ex)
            {
                CyntecLog.GeneralLogger.Warn($"DispatchAsync Canceled: {ex.Message}");
            }
            catch (Exception ex)
            {
                CyntecLog.GeneralLogger.Error($"DispatchAsync Exception: {ex}");
                AddLog($"派送流程異常: {ex.Message}", Brushes.Red);
            }
            finally
            {
                if (!published)
                {
                    _activeTrackInTxId = string.Empty;
                    IsBusy = false;
                    isReadyToStart = true;
                    HideOverlay();
                }
            }
        }

        [RelayCommand(CanExecute = nameof(CanMoveOut))]
        private async Task MoveOutAsync()
        {
            // 實際執行邏輯
            ValidateAllProperties();
            if (MoveOutHasErrors) return;

            bool published = false;
            try
            {              
                SetIsBusyMoveOut(true);

                string workOrder = WorkOrderNoMoveOut.Trim();
                string userId = EmployeeIdMoveOut.Trim();
                //string slotNo = SlotNoMoveOut.Trim();   //mark

                //// 檢查 Secs 連線狀態
                //if (_secsGem == null || _connector == null || _connector.State != Secs4Net.ConnectionState.Selected)
                //{
                //    // 未連線狀態，不送 S6F11
                //    return;
                //}

                // 檢查 MQTT / EAP 連線狀態
                if (_mqtt == null || !_mqtt.IsConnected || !_mqtt.IsMesAlive)
                {
                    AddLog("出站失敗: MQTT / EAP 未連線", Brushes.Red);
                    return;
                }

                try
                {
                    ShowOverlay();
                 
                    CyntecLog.GeneralLogger.Debug($"S6F11, MOVE_OUT, lotId: {workOrder}, userId: {userId}");

                    //因為目前出站的管理項目已經不需要上報該參數了                    
                    //MoveOutInfo moveOutInfo = new MoveOutInfo();
                    //if (ConfigMoveOut.MoveOutInfos.ContainsKey(workOrder))
                    //{
                    //    moveOutInfo = ConfigMoveOut.MoveOutInfos[workOrder];
                    //    //moveOutInfo.SlotNo = slotNo; //更新槽位
                    //}
                    //else
                    //{
                    //    string msg = $"出站失敗! 工單號碼:{workOrder} 不存在於出站設定檔中!";
                    //    MessageBox.Show(msg);
                    //    return;
                    //}

                    var paramInfo = new Dictionary<string, string>
                    {
                        { "LOTID", workOrder },
                        { "USERID", userId },
                        { "QRCODE_01", ""}, //預留
                        { "QRCODE_02", ""},
                        { "QRCODE_03", ""},
                        { "QRCODE_04", ""},
                        { "remark01", ""},
                        { "remark02", ""},
                        { "remark03", ""},
                        { "remark04", ""} //預留
                    };

                    //var paramInfo = new Dictionary<string, string>
                    //{
                    //    { "LOTID", workOrder },
                    //    { "USERID", userId },
                    //    { "Count", moveOutInfo.Count},
                    //    { "TargetNo", moveOutInfo.TargetNo},
                    //    { "LeftRack", moveOutInfo.LeftRack},
                    //    { "RightRack", moveOutInfo.RightRack},
                    //    { "SlotNo", moveOutInfo.SlotNo},
                    //    { "CurrentA", moveOutInfo.CurrentA},
                    //    { "CurrentB", moveOutInfo.CurrentB},
                    //    { "PlanTime", moveOutInfo.PlanTime}
                    //};

                    //// WC 2025/12/09 Move Out 流程
                    //// 發送 S6F11 MOVE_OUT //帶入批號, 工號
                    //SecsMessage reply = await _secsGem.SendAsync(
                    //    SECSBuilder.BuildS6F11Message("MOVE_OUT", paramInfo),
                    //    _cancellationTokenSource.Token);

                    // ========================================
                    // MES TrackOut
                    // ========================================
                    // 
                    var request = new TrackOutRequest
                    {
                        TransactionID = Guid.NewGuid().ToString(),
                        CMD = MqttCommands.TrackOutReq,
                        Machine = Config.EqpNo,
                        WorkOrder = new List<string>
                        {
                            workOrder
                        },
                        UserID = userId,
                        Qty = "0"   //default 0
                    };

                    _activeTrackOutTxId = request.TransactionID;
                    await _mqtt.PublishReportAsync(request);
                    published = true;

                    CyntecLog.GeneralLogger.Trace($"[MoveOut] TrackOutReq Published TransactionID={request.TransactionID}, WorkOrder={workOrder}, UserID={userId}");
                    AddLog($"[工單出站] 已發送出站請求: 工單={workOrder}, 工號={userId}", Brushes.Blue);
                    StartTrackOutTimeoutWatcher(request.TransactionID, workOrder);

                    // ========================================
                    // 注意：
                    // 這裡不等待 MES Reply
                    //
                    // TrackOutRequest 由 MqttService_ReplyReceived()
                    // 處理
                    // ========================================

                    // reply 內容可以處理或忽略
                    //都只有OK，會透過 S10F3 來發錯誤資訊
                    //if (reply != null)
                    //{
                    //    if (reply.Name == "MOVE_OUT")
                    //    {
                    //        Secs4Net.Item? Item = reply?.SecsItem;
                    //        Memory<byte> mem = Item.GetMemory<byte>();
                    //        byte[] bytes = mem.ToArray();

                    //        if (bytes.Length > 0 && bytes[0] == 0x00)
                    //        {
                    //            CyntecLog.GeneralLogger.Trace($"MOVE_OUT OK,工單號碼: {workOrder}");
                    //        }
                    //        else
                    //            CyntecLog.GeneralLogger.Trace($"MOVE_OUT NG,工單號碼: {workOrder}");
                    //    }
                    //}
                }
                catch (SecsException ex)
                {
                    CyntecLog.SecsLogger.Error(ex);
                    AddLog($"出站流程異常: {ex.Message}", Brushes.Red);
                }
            }
            catch (OperationCanceledException ex)
            {
                CyntecLog.SecsLogger.Error(ex);
                // 被取消
            }
            catch (Exception ex)
            {
                CyntecLog.SecsLogger.Error(ex);
                AddLog($"出站流程異常: {ex.Message}", Brushes.Red);
            }
            finally
            {
                if (!published)
                {
                    _activeTrackOutTxId = string.Empty;
                    SetIsBusyMoveOut(false);
                    HideOverlay();
                }
            }
        }

        //WC 2025/12/29 Add 用於跨執行緒設定 IsBusyMoveOut
        private void SetIsBusyMoveOut(bool value)
        {
            var dispatcher = Application.Current?.Dispatcher;

            if (dispatcher == null || dispatcher.CheckAccess())
            {
                IsBusyMoveOut = value;
            }
            else
            {
                dispatcher.BeginInvoke(() =>
                {
                    IsBusyMoveOut = value;
                });
            }
        }

        // 按鈕是否可用 (Command CanExecute)
        private bool _hasClicked = false;
        private bool CanSecsConn() => !_hasClicked;

        [RelayCommand(CanExecute = nameof(CanSecsConn))]
        private async Task SecsConnAsync()
        {
            try
            {
                _hasClicked = true;

                await InitializeMqttAsync();
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                CyntecLog.GeneralLogger.Error(ex);
            }
        }

        private async Task InitializeMqttAsync()
        {
            try
            {
                if (_mqtt == null)
                {
                    _mqtt = new MqttClientService(Config.IpAddress, Config.Port, Config.Factory, Config.Floor, Config.Area, Config.EqpNo);

                    _mqtt.CommandReceived += MqttService_CommandReceived;
                    _mqtt.ReplyReceived += MqttService_ReplyReceived;
                    _mqtt.MessageError += MqttService_MessageError;                   
                    _mqtt.TimeCalibrateReceived += HandleTimeCalibrateAsync;
                    _mqtt.StatusReqReceived += HandleStatusReqAsync;
                    _mqtt.TerminalDisplayReceived += HandleTerminalDisplayAsync;                
                    _mqtt.LogLevel = LogLevel.Info;                  
                }

                MitsubishiPLC.Initialize();
                await _mqtt.ConnectAsync();
            }
            catch (Exception ex)
            {
                CyntecLog.GeneralLogger.Error(ex);
                throw;
            }
        }

        private async void MqttService_CommandReceived(object sender, MqttCommandEventArgs e)
        {
            try
            {
                CyntecLog.GeneralLogger.Trace($"[MQTT] Command Handler " +
                    $"TransactionID={e.TransactionID}, " +
                    $"CMD={e.CMD}, " +
                    $"RemoteCMDType={e.RemoteCMDType}, " +
                    $"RecipeID={e.RecipeID}");              

                if (string.IsNullOrWhiteSpace(e.TransactionID))
                {
                    CyntecLog.GeneralLogger.Error("[MQTT] Command Handler TransactionID is empty.");
                    return;
                }

                if (!string.Equals(e.CMD, MqttCommands.RemoteCMD, StringComparison.OrdinalIgnoreCase))
                {
                    CyntecLog.GeneralLogger.Error($"[MQTT] Ignore Command CMD={e.CMD}");
                    return;
                }

                switch (e.RemoteCMDType)
                {
                    case MqttRemoteCommandTypes.Start:

                        // await HandleStartCommandAsync(e);

                        break;

                    case MqttRemoteCommandTypes.Stop:

                        // await HandleStopCommandAsync(e);

                        break;

                    case MqttRemoteCommandTypes.PPSelect:

                        await HandlePPSelectCommandAsync(e);

                        break;

                    case MqttRemoteCommandTypes.Pause:

                        // await HandlePauseCommandAsync(e);

                        break;

                    case MqttRemoteCommandTypes.Resume:

                        //await HandleResumeCommandAsync(e);

                        break;

                    default:

                        CyntecLog.GeneralLogger.Error(
                            $"[MQTT] Unknown RemoteCMDType={e.RemoteCMDType}, " +
                            $"TransactionID={e.TransactionID}");

                        await _mqtt.PublishRemoteCommandReplyAsync(
                            e.TransactionID,
                            e.Machine,
                            e.RemoteCMDType,
                            "FAIL",
                            $"Unknown RemoteCMDType [{e.RemoteCMDType}]");

                        break;
                }
            }
            catch (Exception ex)
            {
                CyntecLog.GeneralLogger.Error(
                    $"[MQTT] Command Handler Exception " +
                    $"TransactionID={e.TransactionID}, " +
                    $"CMD={e.CMD}, " +
                    $"RemoteCMDType={e.RemoteCMDType}, " +
                    $"Exception={ex}");
            }
        }

        private async Task HandlePPSelectCommandAsync(MqttCommandEventArgs e)
        {
            string result = "FAIL";
            string message = string.Empty;

            try
            {
                CyntecLog.GeneralLogger.Trace($"[MQTT] PPSelect Start TransactionID={e.TransactionID}, RecipeID={e.RecipeID}");
                AddLog($"[配方載入] 收到 EAP PPSelect 指令, RecipeID={e.RecipeID}", Brushes.Blue);

                if (string.IsNullOrWhiteSpace(e.RecipeID))
                {
                    message = "RecipeID is empty.";

                    CyntecLog.GeneralLogger.Error($"[MQTT] PPSelect Failed TransactionID={e.TransactionID}, Reason={message}");
                    AddLog($"[配方載入] 失敗: {message}", Brushes.Red);
                }
                else
                {
                    bool success = false;

                    try
                    {
                        if (IsSimulationMode)
                        {
                            success = SimPPSelectPass;
                            if (!success)
                            {
                                message = $"[模擬模式] 設定為 PP_SELECT 失敗 (RecipeID={e.RecipeID})";
                            }
                            AddLog($"[模擬-配方載入] 跳過實體 LoadRecipe，依模擬設定回覆 {(success ? "PASS" : "FAIL")} (RecipeID={e.RecipeID})", Brushes.DarkOrange);
                        }
                        else
                        {
                            success = LoadRecipe(e.RecipeID);
                        }
                    }
                    catch (Exception ex)
                    {
                        CyntecLog.GeneralLogger.Error($"[MQTT] PPSelect LoadRecipe Exception TransactionID={e.TransactionID}, RecipeID={e.RecipeID}, Exception={ex}");

                        message = $"Recipe [{e.RecipeID}] load failed: {ex.Message}";
                    }

                    if (success)
                    {
                        result = "PASS";
                        message = $"Recipe [{e.RecipeID}] selected successfully.";
                        CyntecLog.GeneralLogger.Trace($"[MQTT] PPSelect Success TransactionID={e.TransactionID}, RecipeID={e.RecipeID}");
                        CyntecLog.GeneralLogger.Trace($"載入 Recipe 成功. Recipe No:{e.RecipeID}");
                        AddLog($"[配方載入] 載入 Recipe 成功. Recipe No: {e.RecipeID}", Brushes.Blue);
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(message))
                        {
                            message = $"Recipe [{e.RecipeID}] load failed.";
                        }

                        CyntecLog.GeneralLogger.Error($"[MQTT] PPSelect Failed TransactionID={e.TransactionID}, RecipeID={e.RecipeID}, Message={message}");
                        CyntecLog.GeneralLogger.Trace($"載入 Recipe 失敗. Recipe No:{e.RecipeID}");
                        AddLog($"[配方載入] 載入 Recipe 失敗. Recipe No: {e.RecipeID}, 原因: {message}", Brushes.Red);
                    }
                }

                // ============================================
                // 回覆 MES
                //
                // EQ -> MES
                // ReplyRemoteCMD
                // ============================================

                await _mqtt.PublishRemoteCommandReplyAsync(e.TransactionID, e.RemoteCMDType, result, message, e.Machine);

                // ============================================
                // 注意
                //
                // 這裡不要：
                //
                // await WaitForPPSelectAsync(...)
                //
                // 也不要：
                //
                // _ppSelectTcs.TrySetResult(...)
                //
                // 因為 PPSelect 本身就是 Command。
                //
                // TrackInReq 的結果會由 ReplyReceived
                // 另外處理。
                // ============================================
            }
            catch (Exception ex)
            {
                CyntecLog.GeneralLogger.Error($"[MQTT] HandlePPSelectCommandAsync Exception TransactionID={e.TransactionID}, RecipeID={e.RecipeID}, Exception={ex}");
            }
        }

        private bool LoadRecipe(string recipe)
        {
            isReadyToStart = false; //WC 2026/06/23 Add 防呆，以防還在運行中，觸發S6F1
            S6F1_Timer.Stop();
            bool 成功設定 = false;
            var 資料編號 = recipe;
            var 批號 = workOrderNo; // 直接抓 UI 上的，後續流程 MoveIn >> PP Select >> LoadRecipe >> MoveIn OK
            var 片數 = "10";  //WC 測試 這個需要嗎? 現場看起來是已經無法編輯

            //string[] aryLot = 批號.Split('-');

            //// 若不足3個元素，就補足
            //if (aryLot.Length < 3)
            //{
            //    var list = aryLot.ToList();
            //    while (list.Count < 3)
            //        list.Add(string.Empty);  // 可改成預設值，例如 "NA" 或 "0"

            //    aryLot = list.ToArray();
            //}

            //aryLot[0] = aryLot[0] + $"{TargetNo}";
            //aryLot[1] = LeftRack;
            //aryLot[2] = RightRack;
            //string newLot = string.Join("-", aryLot);

            string newLot = $"{批號}{TargetNo}-{LeftRack}-{RightRack}";

            try
            {
                //ShowOverlay();
                CyntecLog.GeneralLogger.Trace("LoadRecipe recived 1");
                {
                    CloseIfExists("數據加載");
                    CloseIfExists("產品資料");
                }
                //// Activate window
                CyntecLog.GeneralLogger.Trace("clickDataLoad");
                WinAuto.clickDataLoad();
                Utility.CaptureAllScreens($"LoadRecipe", 1);
                Thread.Sleep(1500); //原程式本來開這頁就很久
                Utility.CaptureAllScreens($"LoadRecipe", 2);

                #region 選到第一槽 001# 上下料
                {
                    string win = Utility.Svid_Info[Utility.VID.加載槽號][0]; // 或整個主視窗標題
                    string comboId = Utility.Svid_Info[Utility.VID.加載槽號][1];// "[CLASS:WindowsForms10.COMBOBOX.app.0.34f5582_r7_ad1; INSTANCE:11]";

                    AutoItX.WinActivate(win);
                    AutoItX.WinWaitActive(win, "", 2);

                    // 聚焦到 Combo
                    AutoItX.ControlFocus(win, "", comboId);

                    // 展開下拉
                    AutoItX.ControlCommand(win, "", comboId, "ShowDropDown", "");

                    // 選第一項（移到清單頂端）
                    AutoItX.ControlSend(win, "", comboId, "{HOME}");
                    AutoItX.ControlSend(win, "", comboId, "{ENTER}");
                }
                Thread.Sleep(500);
                Utility.CaptureAllScreens($"LoadRecipe", 5);
                #endregion

                //AutoItX.ControlSetText(Utility.Svid_Info[Utility.VID.批號][0], "", Utility.Svid_Info[Utility.VID.批號][1], 批號);
                AutoItX.ControlSetText(Utility.Svid_Info[Utility.VID.批號][0], "", Utility.Svid_Info[Utility.VID.批號][1], newLot);
                Thread.Sleep(500);
                Utility.CaptureAllScreens($"LoadRecipe", 6);

                AutoItX.ControlFocus(Utility.Svid_Info[Utility.VID.資料編號][0], "", Utility.Svid_Info[Utility.VID.資料編號][1]);
                AutoItX.ControlSetText(Utility.Svid_Info[Utility.VID.資料編號][0], "", Utility.Svid_Info[Utility.VID.資料編號][1], 資料編號);

                Thread.Sleep(500);
                Utility.CaptureAllScreens($"LoadRecipe", 7);

                AutoItX.Send("{ENTER}"); //必要
                Thread.Sleep(500);
                Utility.CaptureAllScreens($"LoadRecipe", 8);

                AutoItX.ControlSetText(Utility.Svid_Info[Utility.VID.數量][0], "", Utility.Svid_Info[Utility.VID.數量][1], 片數);
                AutoItX.Send("{ENTER}");    //WC 2025/11/04 Issue 先輸入數量再 Enter
                Thread.Sleep(1500);         //WC 2025/11/04 Issue 點擊太快，需要給系統載入時間 // Thread.Sleep(500);
                Utility.CaptureAllScreens($"LoadRecipe", 9);

                SetOverlayClickThrough(true);

                //mark
                //抓取數值
                //string SetCurrentA = AutoItX.ControlGetText(Utility.Svid_Info[Utility.VID.A面設定電流][0], "", Utility.Svid_Info[Utility.VID.A面設定電流][1]);
                //string SetCurrentB = AutoItX.ControlGetText(Utility.Svid_Info[Utility.VID.B面設定電流][0], "", Utility.Svid_Info[Utility.VID.B面設定電流][1]);
                //string SetPlanTime = AutoItX.ControlGetText(Utility.Svid_Info[Utility.VID.設定電鍍時間][0], "", Utility.Svid_Info[Utility.VID.設定電鍍時間][1]);
                //Thread.Sleep(500);

                AutoItX.ControlClick(Utility.Svid_Info[Utility.VID.加載][0], "", Utility.Svid_Info[Utility.VID.加載][1]); //popout - 確定
                Thread.Sleep(500);

                Utility.CaptureAllScreens($"LoadRecipe", 10);

                AutoItX.ControlClick("提示", "", "[CLASS:Button; INSTANCE:1]"); //popout - 確定
                SetOverlayClickThrough(false);
                Thread.Sleep(1500); //寫入PLC需要一點時間

                Utility.CaptureAllScreens($"LoadRecipe", 11);

                SetOverlayClickThrough(true);
                AutoItX.ControlClick(Utility.Svid_Info[Utility.VID.退出][0], "", Utility.Svid_Info[Utility.VID.退出][1]); //popout - 確定  Thread.Sleep(500);
                Thread.Sleep(1500); //WC 2026/07/22 在延遲一段時間
                SetOverlayClickThrough(false);
                {
                    KeyValuePair<bool, string> consoleProcessValue = GetConsoleProcessValue(@$"{AppDomain.CurrentDomain.BaseDirectory}\UIControl\_AutoCIMOrigin.exe",
                        string.Join(",", "GetDataGridView", Utility.Svid_Info[VID.DataGridView][0], Utility.Svid_Info[VID.DataGridView][1]));

                    if (JsonHelpers.TryGetPartAndLot(consoleProcessValue.Value, out var part, out var lot, targetKey: "001#", requireRtnResultTrue: true))
                    {
                        //if (string.Equals(資料編號, part) && lot.StartsWith(批號))
                        if (string.Equals(資料編號, part) && lot.StartsWith(newLot))
                        {
                            成功設定 = true;

                            ConfigMoveOut.MoveOutInfos[WorkOrderNo] = new MoveOutInfo
                            {
                                LotNo = WorkOrderNo,
                                NewLotNo = newLot,
                                Count = 片數,
                                TargetNo = TargetNo,
                                LeftRack = LeftRack,
                                RightRack = RightRack,
                                //CurrentA = SetCurrentA,
                                //CurrentB = SetCurrentB,
                                //PlanTime = SetPlanTime,
                                LastExeTime = DateTime.Now
                            };
                            ConfigMoveOut.SaveConfig();
                        }
                        //WC 2025/11/03 組合批號
                        CyntecLog.GeneralLogger.Debug($"資料編號: {資料編號}, 批號: {批號}, 組合批號: {newLot}, 片數: {片數}, part: {part}, lot: {lot}, 設定: {成功設定}");
                    }
                }
            }
            catch (Exception ex)
            {
                CyntecLog.GeneralLogger.Error(ex);
            }
            finally
            {
                HideOverlay();
                //確認是否有被正確寫入PLC
                Application.Current.Dispatcher.Invoke(() =>
                ForegroundHelper.BringToFront(Application.Current.MainWindow));

                if (!成功設定)
                {
                    //WC 2025/11/03 Add
                    CyntecLog.GeneralLogger.Debug($"資料編號: {資料編號}, 批號: {批號}, 組合批號: {newLot}, 片數: {片數}, 設定失敗: {成功設定}");
                    MessageBox.Show("設定失敗，請人員查明原因. Configuration failed. การตั้งค่าล้มเหลว. Error!"); //設定失敗
                }
             
                isReadyToStart = true;   //WC 2026/06/23 Add 防呆，以防還在運行中，觸發S6F1
                                         //把自己帶到前景               
            }
            return 成功設定;
        }

        private void MqttService_ReplyReceived(object? sender, MqttReplyEventArgs e)
        {
            try
            {
                if (e?.Message?.Payload == null)
                {
                    CyntecLog.GeneralLogger.Error("[MQTT] Reply Handler Invalid Reply.");
                    return;
                }

                MqttCommandPayload payload = e.Message.Payload;

                CyntecLog.GeneralLogger.Info($"[MQTT] Reply Received TransactionID={payload.TransactionID}, CMD={payload.CMD}, Result={payload.RtnResult}, Msg={payload.RtnMsg}");

                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                {
                    try
                    {
                        switch (payload.CMD)
                        {
                            case MqttCommands.IamHere:
                                //HandleIamHereReply(payload);
                                break;

                            case MqttCommands.ReplyTrackInReq:
                                HandleTrackInReply(payload);
                                break;

                            case MqttCommands.ReplyTrackOutReq:
                                HandleTrackOutReply(payload);
                                break;

                            case MqttCommands.ReplyStatusChangeReport:
                                HandleStatusChangeReply(payload);
                                break;

                            case MqttCommands.ReplyAlarm:
                                HandleAlarmReply(payload);
                                break;

                            case MqttCommands.ReplyEventReport:
                                HandleEventReportReply(payload);
                                break;

                            case MqttCommands.ReplyRtnTraceData:
                                CyntecLog.GeneralLogger.Trace($"[MQTT] RtnTraceData Reply Result={payload.RtnResult}, TransactionID={payload.TransactionID}");
                                break;

                            case MqttCommands.ReplyProcessDataReport:
                                CyntecLog.GeneralLogger.Trace($"[MQTT] ProcessDataReport Reply Result={payload.RtnResult}, TransactionID={payload.TransactionID}");
                                break;

                            default:
                                CyntecLog.GeneralLogger.Error($"[MQTT] Unknown Reply CMD={payload.CMD}, TransactionID={payload.TransactionID}");
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        CyntecLog.GeneralLogger.Error($"[MQTT] Reply Handler Exception: {ex}");
                    }
                }));
            }
            catch (Exception ex)
            {
                CyntecLog.GeneralLogger.Error($"[MQTT] Reply Dispatch Exception: {ex}");
            }
        }       

        private void HandleTrackInReply(MqttCommandPayload payload)
        {
            _activeTrackInTxId = string.Empty;

            if (string.Equals(payload.RtnResult, "PASS", StringComparison.OrdinalIgnoreCase))
            {
                CyntecLog.GeneralLogger.Trace($"[MQTT] TrackIn PASS TransactionID={payload.TransactionID}");

                //if (Config.LaserTrackInWorkOrder != TrackInWorkOrder)
                //{
                //    //把狀態存下來
                //    Config.LaserTrackInWorkOrder = TrackInWorkOrder;
                //    Config.IsUseCopyQRCode = TrackInUseCopyQRCode;
                //    Config.IsMainPos = TrackInIsMainPos;
                //    clsCommon.SaveJsonFile<clsConfig>(Application.StartupPath, "config.json", Config, true);
                //}               

                CyntecLog.GeneralLogger.Trace($"{WorkOrderNo} 工單進站成功.");
                AddLog($"[進站結果] {WorkOrderNo} 工單進站成功.", Brushes.Blue);
            }
            else
            {
                CyntecLog.GeneralLogger.Error($"[MQTT] TrackIn FAIL TransactionID={payload.TransactionID}, Message={payload.RtnMsg}");
                // 更新 TrackIn 失敗狀態
                string failMsg = $"{WorkOrderNo} 工單進站失敗. Message={payload.RtnMsg}";
                CyntecLog.GeneralLogger.Trace(failMsg);
                AddLog($"[進站結果] {failMsg}", Brushes.Red);
                ShowMessageBoxWindow(failMsg);
                //TrackInWorkOrder = string.Empty;
            }

            IsBusy = false;
            isReadyToStart = true;
            HideOverlay();
        }

        private void HandleTrackOutReply(MqttCommandPayload payload)
        {
            _activeTrackOutTxId = string.Empty;

            if (string.Equals(payload.RtnResult, "PASS", StringComparison.OrdinalIgnoreCase))
            {
                CyntecLog.GeneralLogger.Trace($"[MQTT] TrackOut PASS TransactionID={payload.TransactionID}");
                CyntecLog.GeneralLogger.Trace($"{WorkOrderNoMoveOut} 工單出站成功.");
                AddLog($"[出站結果] {WorkOrderNoMoveOut} 工單出站成功.", Brushes.Blue);
            }
            else
            {
                CyntecLog.GeneralLogger.Error($"[MQTT] TrackOut FAIL TransactionID={payload.TransactionID}, Message={payload.RtnMsg}");
                string failMsg = $"{WorkOrderNoMoveOut} 工單出站失敗. Message={payload.RtnMsg}";
                CyntecLog.GeneralLogger.Trace(failMsg);
                AddLog($"[出站結果] {failMsg}", Brushes.Red);
                ShowMessageBoxWindow(failMsg);
            }


            SetIsBusyMoveOut(false);
            HideOverlay();

        }

        private void HandleEventReportReply(MqttCommandPayload payload)
        {
            try
            {
                if (payload == null)
                {
                    CyntecLog.GeneralLogger.Error("[UserVerify] EventReport Reply Payload is null.");

                    return;
                }

                CyntecLog.GeneralLogger.Trace($"[UserVerify] EventReport Reply TransactionID={payload.TransactionID}, Result={payload.RtnResult}, Msg={payload.RtnMsg}");

                if (!string.Equals(payload.RtnResult, "PASS", StringComparison.OrdinalIgnoreCase))
                {
                    CyntecLog.GeneralLogger.Error($"[UserVerify] User Verify Failed TransactionID={payload.TransactionID}, Message={payload.RtnMsg}");

                    //HandleUserVerifyFailed(payload.RtnMsg);

                    return;
                }

                string userID = payload.RtnMsg;

                if (string.IsNullOrWhiteSpace(userID))
                {
                    CyntecLog.GeneralLogger.Error($"[UserVerify] MES UserID is empty TransactionID={payload.TransactionID}");

                    //HandleUserVerifyFailed("MES 回覆的人員工號為空。");
                    return;
                }

                //HandleUserVerifySuccess(userID);
            }
            catch (Exception ex)
            {
                CyntecLog.GeneralLogger.Error($"[UserVerify] HandleEventReportReply Exception: {ex}");
            }
        }

        private void HandleStatusChangeReply(MqttCommandPayload payload)
        {
            if (payload == null)
            {
                CyntecLog.GeneralLogger.Error("[MQTT] Status Change Reply Payload Null");

                return;
            }

            if (string.Equals(payload.RtnResult, "PASS", StringComparison.OrdinalIgnoreCase))
            {
                CyntecLog.GeneralLogger.Trace($"[MQTT] Status Change MES PASS, TransactionID={payload.TransactionID}");
            }
            else
            {
                CyntecLog.GeneralLogger.Error($"[MQTT] Status Change MES FAIL, TransactionID={payload.TransactionID}, Msg={payload.RtnMsg}");
            }
        }

        private void HandleAlarmReply(MqttCommandPayload payload)
        {
            if (payload == null)
            {
                CyntecLog.GeneralLogger.Error("[MQTT] Status Change Reply Payload Null");

                return;
            }

            if (string.Equals(payload.RtnResult, "PASS", StringComparison.OrdinalIgnoreCase))
            {
                CyntecLog.GeneralLogger.Trace($"[MQTT] Alarm Report MES PASS, TransactionID={payload.TransactionID}");
            }
            else
            {
                CyntecLog.GeneralLogger.Error($"[MQTT] Alarm Report MES FAIL, TransactionID={payload.TransactionID}, Msg={payload.RtnMsg}");
            }
        }        

        private void MqttService_MessageError(object sender, MqttMessageErrorEventArgs e)
        {
            try
            {
                CyntecLog.GeneralLogger.Error($"[MQTT] Message Error Topic={e.Topic}, Reason={e.ErrorMessage}");

                if (e.Exception != null)
                {
                    CyntecLog.GeneralLogger.Error($"[MQTT] Message Error Exception: {e.Exception}");
                }

                if (!string.IsNullOrWhiteSpace(e.Payload))
                {
                    CyntecLog.GeneralLogger.Error($"[MQTT] Message Error Payload={e.Payload}");
                }
            }
            catch (Exception ex)
            {
                CyntecLog.GeneralLogger.Error($"[MQTT] MessageError Handler Exception: {ex}");
            }
        }

        private async Task HandleTimeCalibrateAsync(MqttCommandPayload payload)
        {
            string result = "PASS";
            string message = string.Empty;
            string calibratedDate = payload.DATE;

            try
            {
                CyntecLog.GeneralLogger.Trace($"[TimeCalibrate] BC時間校正要求. TransactionID={payload.TransactionID}, DATE={payload.DATE}");

                if (IsSimulationMode)
                {
                    result = "PASS";
                    message = string.Empty;
                    CyntecLog.GeneralLogger.Trace($"[TimeCalibrate][模擬] 略過修改本機系統時間，直接回覆 PASS，DATE={payload.DATE}");
                    AddLog($"[模擬-時間校正] 收到校正要求 DATE={payload.DATE}，已略過修改本機時間並回覆 PASS", Brushes.DarkOrange);
                }
                else
                {
                    string errorMessage = string.Empty;
                    bool success = SystemTimeHelper.SetSystemTime(payload.DATE, out errorMessage);

                    if (success)
                    {
                        CyntecLog.GeneralLogger.Trace($"[TimeCalibrate] 系統時間設定成功，DATE={payload.DATE}");
                        AddLog($"[時間校正] 系統時間設定成功: {payload.DATE}", Brushes.Blue);
                    }
                    else
                    {
                        result = "FAIL";
                        message = string.IsNullOrEmpty(errorMessage) ? "系統時間設定失敗" : errorMessage;

                        CyntecLog.GeneralLogger.Error($"[TimeCalibrate] 系統時間設定失敗，DATE={payload.DATE}, Error={message}");
                        AddLog($"[時間校正] 系統時間設定失敗: {message}", Brushes.Red);
                    }
                }

                CyntecLog.GeneralLogger.Trace($"[TimeCalibrate] 時間校正完成. TransactionID={payload.TransactionID}, Result={result}, DATE={calibratedDate}");
            }
            catch (Exception ex)
            {
                result = "FAIL";
                message = ex.Message;

                CyntecLog.GeneralLogger.Error($"[TimeCalibrate] 時間校正失敗. TransactionID={payload.TransactionID}, Error={ex}");
                AddLog($"[時間校正] 時間校正例外: {ex.Message}", Brushes.Red);
            }

            await _mqtt.PublishTimeCalibrateReplyAsync(payload.TransactionID, result, message, payload.Machine);
        }      

        private async Task HandleTerminalDisplayAsync(MqttCommandPayload payload)
        {
            string result = "PASS";
            string message = string.Empty;

            try
            {
                CyntecLog.GeneralLogger.Trace(
                    $"[TerminalDisplay] BC訊息顯示要求. " +
                    $"TransactionID={payload.TransactionID}, " +
                    $"DATE={payload.DATE}, " +
                    $"Message={payload.Message}");

                string displayMsg = string.IsNullOrWhiteSpace(payload.Message) ? "異常發生" : payload.Message;
                CyntecLog.GeneralLogger.Trace($"EAP訊息顯示 [{displayMsg}]");
                AddLog($"[EAP訊息] {displayMsg}", Brushes.Red);
                ShowMessageBoxWindow(displayMsg);
            }
            catch (Exception ex)
            {
                result = "FAIL";
                message = ex.Message;

                CyntecLog.GeneralLogger.Error(
                    $"[TerminalDisplay] 訊息顯示失敗. " +
                    $"TransactionID={payload.TransactionID}, " +
                    $"Error={ex}");
            }

            // 回覆 BC
            await _mqtt.PublishTerminalDisplayReplyAsync(payload.TransactionID, result, message);
        }

        private async Task HandleStatusReqAsync(MqttCommandPayload payload)
        {
            try
            {
                string currentStatus = string.IsNullOrEmpty(_lastReportedStatus) ? IDLE : _lastReportedStatus;
                string alarmCode = "0";

                if (IsSimulationMode)
                {
                    if (!_simActiveAlarms.IsEmpty)
                    {
                        currentStatus = ALARM;
                        alarmCode = _simActiveAlarms.Keys.FirstOrDefault() ?? "0";
                    }
                }
                else if (_S5f1AlarmInfo.Count > 0)
                {
                    currentStatus = ALARM;
                    alarmCode = _S5f1AlarmInfo.Keys.FirstOrDefault() ?? "0";
                }

                await _mqtt.PublishStatusReqReplyAsync(payload.TransactionID, "PASS", string.Empty, currentStatus, alarmCode, payload.Machine);
                CyntecLog.GeneralLogger.Trace($"[StatusReq] ReplyStatusReq Published TransactionID={payload.TransactionID}, CurrentStatus={currentStatus}, AlarmCode={alarmCode}");
                AddLog($"[狀態查詢] 已回覆 StatusReq: CurrentStatus={currentStatus}, AlarmCode={alarmCode}", Brushes.Blue);
            }
            catch (Exception ex)
            {
                CyntecLog.GeneralLogger.Error($"[StatusReq] HandleStatusReqAsync Error: {ex}");
            }
        }

        //[RelayCommand(CanExecute = nameof(CanSecsConn))]
        //private async Task SecsConnAsync()
        //{
        //    try
        //    {
        //        _hasClicked = true;

        //        // TODO: 在此呼叫你的 SECS/GEM 或後端服務
        //        // 模擬耗時工作
        //        await Task.Run(() => {
        //            SecsConnection();
        //        });

        //        // 成功後可清空欄位或顯示提示
        //        // WorkOrderNo = string.Empty;
        //        // EmployeeId  = string.Empty;
        //    }
        //    catch (OperationCanceledException)
        //    {
        //        // 被取消
        //    }
        //    finally
        //    {

        //    }
        //}

        // 取消派送
        [RelayCommand]
        private void Cancel()
        {
            if (!_cts.IsCancellationRequested)
                _cts.Cancel();
        }

        private SecsGem _secsGem;
        private HsmsConnection _connector;
        private ISecsGemLogger _logger;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _IsActive = false;
        private string _IpAddress = "127.0.0.1";
        private int _Port = 5000;
        private int _SocketReceiveBufferSize;
        private int _DeviceId;
        //private Dictionary<string, string[]> _Svid_Info;
        //private Label ConnStatus_Lab;

        private async void SecsConnection()
        {
            // Dispose existing connections
            this._secsGem?.Dispose();
            if (this._connector != null)
                await this._connector.DisposeAsync();

            // Create options for SecsGem
            IOptions<SecsGemOptions> options = Microsoft.Extensions.Options.Options.Create<SecsGemOptions>(
                new SecsGemOptions()
                {
                    IsActive = this._IsActive,
                    IpAddress = Config.IpAddress,// this._IpAddress,
                    Port = Config.Port,// this._Port,
                    SocketReceiveBufferSize = this._SocketReceiveBufferSize,
                    //T3 = 1000 * 30,
                    DeviceId = 0
                    //DeviceId = this._DeviceId // from config
                });

            // Initialize connector and SecsGem
            this._connector = new HsmsConnection(options, this._logger);
           // this._connector.ConnectionChanged += _connector_ConnectionChanged;
            this._secsGem = new SecsGem(options, (ISecsConnection)this._connector, this._logger);

            // Start the connector

            _cancellationTokenSource = new CancellationTokenSource();

            this._connector.Start(this._cancellationTokenSource.Token);

            try
            {
                // Get primary message enumerator
                IAsyncEnumerator<PrimaryMessageWrapper> asyncEnumerator =
                    this._secsGem.GetPrimaryMessageAsync(this._cancellationTokenSource.Token)
                    .GetAsyncEnumerator(new CancellationToken());

                try
                {
                    while (true)
                    {
                        if (await asyncEnumerator.MoveNextAsync())
                        {
                            PrimaryMessageWrapper primaryMessage = asyncEnumerator.Current;

                            // Get S and F values
                            byte sValue = primaryMessage.PrimaryMessage.S;
                            string sStr = sValue.ToString();
                            byte fValue = primaryMessage.PrimaryMessage.F;
                            string fStr = fValue.ToString();

                            // Create message identifier
                            string messageId = $"S{sStr}F{fStr}";

                            // Get reply message
                            SecsMessage ReplyMsg = this.ReplyScenario(messageId, primaryMessage.PrimaryMessage);

                            if (ReplyMsg != null && primaryMessage.PrimaryMessage.SecsItem != null)
                            {
                                // Send reply
                                if ($"S{sStr}F{fStr}".Equals("S10F3")) // 把Host Message show messagebox
                                {
                                    // Handle S10F3 message
                                    //MessageBox.Show(primaryMessage.PrimaryMessage.SecsItem.Items[1].GetString());
                                    //_ = primaryMessage.TryReplyAsync(ReplyMsg, this._cancellationTokenSource.Token);
                                  
                                    // 傳送 S10F4 Reply
                                    _ = primaryMessage.TryReplyAsync(ReplyMsg, _cancellationTokenSource.Token);

                                    // 取得 Host 傳來的文字 (通常在 Items[1])
                                    string errorMsg = primaryMessage.PrimaryMessage.SecsItem.Items[1].GetString();

                                    errorMsg = BinaryToString(errorMsg);

                                    if (string.IsNullOrEmpty(errorMsg))
                                        errorMsg = "異常發生";

                                    CyntecLog.GeneralLogger.Trace($"S10F3 :{errorMsg}");

                                    SetIsBusyMoveOut(false);    //WC 由 2025/12/29 S6F11 移至 S10F3 才結束 Move Out 忙碌狀態  
                                    HideOverlay();

                                    Application.Current.Dispatcher.Invoke(() =>
                                        {
                                            if (_messageBoxWindow == null || !_messageBoxWindow.IsVisible)
                                            {
                                                _messageBoxWindow = new MessageBoxWindow();
                                            }

                                            _messageBoxWindow.UpdateMessage(errorMsg);
                                        });
                                   
                                }
                                else if ($"S{sStr}F{fStr}".Equals("S2F41")) //應該做完才回
                                {
                                    isReadyToStart = false; //WC 2026/06/23 Add 防呆，以防還在運行中，觸發S6F1
                                    bool 成功設定 = false;
                                    CyntecLog.GeneralLogger.Trace("S2F41 recived");
                                    // Handle S2F41 PP_SELECT message
                                    //if ($"S{sStr3}F{fStr3}".Equals("S2F41") &&
                                    if (primaryMessage.PrimaryMessage.SecsItem.Items[0].GetString().Trim().ToUpper().Equals("PP_SELECT"))
                                    {
                                        var 資料編號 = primaryMessage.PrimaryMessage.SecsItem.Items[1][0][0].GetString();
                                        var 批號 = primaryMessage.PrimaryMessage.SecsItem.Items[1][0][1].GetString();
                                        var 片數 = primaryMessage.PrimaryMessage.SecsItem.Items[1][0][2].GetString();

                                        //WC 2025/11/03 Add 處理 Lot 編號
                                        string[] aryLot = 批號.Split('-');

                                        // 若不足3個元素，就補足
                                        if (aryLot.Length < 3)
                                        {
                                            var list = aryLot.ToList();
                                            while (list.Count < 3)
                                                list.Add(string.Empty);  // 可改成預設值，例如 "NA" 或 "0"

                                            aryLot = list.ToArray();
                                        }

                                        aryLot[0] = aryLot[0] + $"{TargetNo}";
                                        aryLot[1] = LeftRack;
                                        aryLot[2] = RightRack;
                                        string newLot = string.Join("-", aryLot);

                                        try
                                        {
                                            ShowOverlay();

                                            CyntecLog.GeneralLogger.Trace("S2F41 recived 1");
                                            {
                                                CloseIfExists("數據加載");
                                                CloseIfExists("產品資料");
                                            }
                                            //// Activate window
                                            CyntecLog.GeneralLogger.Trace("clickDataLoad");
                                            WinAuto.clickDataLoad();
                                            Utility.CaptureAllScreens($"S{sStr}F{fStr}", 1);
                                            Thread.Sleep(1500); //原程式本來開這頁就很久
                                            Utility.CaptureAllScreens($"S{sStr}F{fStr}", 2);

                                            #region 選到第一槽 001# 上下料
                                            {
                                                string win = Utility.Svid_Info[Utility.VID.加載槽號][0]; // 或整個主視窗標題
                                                string comboId = Utility.Svid_Info[Utility.VID.加載槽號][1];// "[CLASS:WindowsForms10.COMBOBOX.app.0.34f5582_r7_ad1; INSTANCE:11]";

                                                AutoItX.WinActivate(win);
                                                AutoItX.WinWaitActive(win, "", 2);

                                                // 聚焦到 Combo
                                                AutoItX.ControlFocus(win, "", comboId);

                                                // 展開下拉
                                                AutoItX.ControlCommand(win, "", comboId, "ShowDropDown", "");

                                                // 選第一項（移到清單頂端）
                                                AutoItX.ControlSend(win, "", comboId, "{HOME}");
                                                AutoItX.ControlSend(win, "", comboId, "{ENTER}");
                                            }
                                            Thread.Sleep(500);
                                            Utility.CaptureAllScreens($"S{sStr}F{fStr}", 5);
                                            #endregion

                                            //AutoItX.ControlSetText(Utility.Svid_Info[Utility.VID.批號][0], "", Utility.Svid_Info[Utility.VID.批號][1], 批號);
                                            AutoItX.ControlSetText(Utility.Svid_Info[Utility.VID.批號][0], "", Utility.Svid_Info[Utility.VID.批號][1], newLot);
                                            Thread.Sleep(500);
                                            Utility.CaptureAllScreens($"S{sStr}F{fStr}", 6);

                                            AutoItX.ControlFocus(Utility.Svid_Info[Utility.VID.資料編號][0], "", Utility.Svid_Info[Utility.VID.資料編號][1]);
                                            AutoItX.ControlSetText(Utility.Svid_Info[Utility.VID.資料編號][0], "", Utility.Svid_Info[Utility.VID.資料編號][1], 資料編號);

                                            Thread.Sleep(500);
                                            Utility.CaptureAllScreens($"S{sStr}F{fStr}", 7);

                                            AutoItX.Send("{ENTER}"); //必要
                                            Thread.Sleep(500);
                                            Utility.CaptureAllScreens($"S{sStr}F{fStr}", 8);
                                            
                                            AutoItX.ControlSetText(Utility.Svid_Info[Utility.VID.數量][0], "", Utility.Svid_Info[Utility.VID.數量][1], 片數);
                                            AutoItX.Send("{ENTER}");    //WC 2025/11/04 Issue 先輸入數量再 Enter
                                            Thread.Sleep(1500);         //WC 2025/11/04 Issue 點擊太快，需要給系統載入時間 // Thread.Sleep(500);
                                            Utility.CaptureAllScreens($"S{sStr}F{fStr}", 9);

                                            SetOverlayClickThrough(true);

                                            //mark
                                            //抓取數值
                                            //string SetCurrentA = AutoItX.ControlGetText(Utility.Svid_Info[Utility.VID.A面設定電流][0], "", Utility.Svid_Info[Utility.VID.A面設定電流][1]);
                                            //string SetCurrentB = AutoItX.ControlGetText(Utility.Svid_Info[Utility.VID.B面設定電流][0], "", Utility.Svid_Info[Utility.VID.B面設定電流][1]);
                                            //string SetPlanTime = AutoItX.ControlGetText(Utility.Svid_Info[Utility.VID.設定電鍍時間][0], "", Utility.Svid_Info[Utility.VID.設定電鍍時間][1]);
                                            //Thread.Sleep(500);

                                            AutoItX.ControlClick(Utility.Svid_Info[Utility.VID.加載][0], "", Utility.Svid_Info[Utility.VID.加載][1]); //popout - 確定
                                            Thread.Sleep(500);

                                            Utility.CaptureAllScreens($"S{sStr}F{fStr}", 10);

                                            AutoItX.ControlClick("提示", "", "[CLASS:Button; INSTANCE:1]"); //popout - 確定
                                            SetOverlayClickThrough(false);
                                            Thread.Sleep(1500); //寫入PLC需要一點時間

                                            Utility.CaptureAllScreens($"S{sStr}F{fStr}", 11);

                                            SetOverlayClickThrough(true);
                                            AutoItX.ControlClick(Utility.Svid_Info[Utility.VID.退出][0], "", Utility.Svid_Info[Utility.VID.退出][1]); //popout - 確定  Thread.Sleep(500);
                                            Thread.Sleep(1500); //WC 2026/07/22 在延遲一段時間
                                            SetOverlayClickThrough(false);
                                            {
                                                KeyValuePair<bool, string> consoleProcessValue = GetConsoleProcessValue(@$"{AppDomain.CurrentDomain.BaseDirectory}\UIControl\_AutoCIMOrigin.exe",
                                                    string.Join(",", "GetDataGridView", Utility.Svid_Info[VID.DataGridView][0], Utility.Svid_Info[VID.DataGridView][1]));

                                                if (JsonHelpers.TryGetPartAndLot(consoleProcessValue.Value, out var part, out var lot, targetKey: "001#", requireRtnResultTrue: true))
                                                {
                                                    //if (string.Equals(資料編號, part) && lot.StartsWith(批號))
                                                    if (string.Equals(資料編號, part) && lot.StartsWith(newLot))
                                                    {
                                                        成功設定 = true;

                                                        ConfigMoveOut.MoveOutInfos[WorkOrderNo] = new MoveOutInfo
                                                        {
                                                            LotNo = WorkOrderNo,
                                                            NewLotNo = newLot,
                                                            Count = 片數,
                                                            TargetNo = TargetNo,
                                                            LeftRack = LeftRack,
                                                            RightRack = RightRack,
                                                            //CurrentA = SetCurrentA,
                                                            //CurrentB = SetCurrentB,
                                                            //PlanTime = SetPlanTime,
                                                            LastExeTime = DateTime.Now
                                                        };
                                                        ConfigMoveOut.SaveConfig();
                                                    }
                                                    //WC 2025/11/03 組合批號
                                                    CyntecLog.GeneralLogger.Debug($"資料編號: {資料編號}, 批號: {批號}, 組合批號: {newLot}, 片數: {片數}, part: {part}, lot: {lot}, 設定: {成功設定}");
                                                }
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            CyntecLog.GeneralLogger.Error(ex);
                                        }
                                        finally
                                        {
                                            HideOverlay();
                                            //確認是否有被正確寫入PLC
                                            Application.Current.Dispatcher.Invoke(() =>
                                            ForegroundHelper.BringToFront(Application.Current.MainWindow));

                                            if (!成功設定)
                                            {
                                                //WC 2025/11/03 Add
                                                CyntecLog.GeneralLogger.Debug($"資料編號: {資料編號}, 批號: {批號}, 組合批號: {newLot}, 片數: {片數}, 設定失敗: {成功設定}");
                                                MessageBox.Show("設定失敗，請人員查明原因. Configuration failed. การตั้งค่าล้มเหลว. Error!"); //設定失敗
                                            }

                                            isReadyToStart = true;   //WC 2026/06/23 Add 防呆，以防還在運行中，觸發S6F1
                                            //把自己帶到前景
                                        }


                                        _ = primaryMessage.TryReplyAsync(ReplyMsg, this._cancellationTokenSource.Token);
                                    }
                                }
                                else
                                {
                                    _ = primaryMessage.TryReplyAsync(ReplyMsg, this._cancellationTokenSource.Token);
                                }
                            }
                        }
                        else
                            break;
                    }
                }
                finally
                {
                    if (asyncEnumerator != null)
                        await ((IAsyncDisposable)asyncEnumerator).DisposeAsync();
                }
            }
            catch (OperationCanceledException ex)
            {
                options = null;
            }
        }

        // [ObservableProperty]
        //private Secs4Net.ConnectionState connectionState;

        [ObservableProperty]
        private string connectionStatusText = "MQTT / EAP: 未連線";
        //private string connectionStatusText = "SCES: 未連線";

        //private void _connector_ConnectionChanged(object? sender, Secs4Net.ConnectionState e)
        //{

        //    ConnectionState = e;

        //    ConnectionStatusText = e switch
        //    {
        //        Secs4Net.ConnectionState.Connecting => "SCES: 連線中...",
        //        Secs4Net.ConnectionState.Connected => "SCES: 已連線 (等待Select)",
        //        Secs4Net.ConnectionState.Selected => "SCES: 已選擇 (Ready)",
        //        _ => "SCES: 未知"
        //    };
        //    //DispatchCommand.NotifyCanExecuteChanged();
        //    // 讓 RelayCommand 的 CanExecute 重新評估
        //    Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
        //    {
        //        DispatchCommand.NotifyCanExecuteChanged();
        //    }));

        //    //WC 2025/12/09 Add
        //    Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
        //    {
        //        MoveOutCommand.NotifyCanExecuteChanged();
        //    }));
        //}

        [ObservableProperty]
        eConnectionState connectionState = eConnectionState.Connecting;

        private eConnectionState _lastLoggedConnectionState = eConnectionState.Connecting;

        private void UpdateConnectionState()
        {
            if (_mqtt == null)            
                return;            

            bool mqttConnected = _mqtt.IsConnected;
            bool eapConnected = _mqtt.IsMesAlive;

            if (mqttConnected && eapConnected)
            {
                ConnectionState = eConnectionState.Connected;
                ConnectionStatusText = "MQTT / EAP: 已連線";
            }
            else if (mqttConnected || eapConnected)
            {
                ConnectionState = eConnectionState.PartialConnected;
                ConnectionStatusText = mqttConnected
                    ? "MQTT: 已連線 / EAP: 未連線"
                    : "MQTT: 未連線 / EAP: 已連線";
            }
            else
            {
                ConnectionState = eConnectionState.NotConnected;
                ConnectionStatusText = "MQTT / EAP: 未連線";
            }

            if (_lastLoggedConnectionState != ConnectionState)
            {
                _lastLoggedConnectionState = ConnectionState;
                Brush logColor = ConnectionState switch
                {
                    eConnectionState.Connected => Brushes.Green,
                    eConnectionState.PartialConnected => Brushes.Orange,
                    _ => Brushes.Red
                };
                AddLog($"[連線狀態] {ConnectionStatusText}", logColor);
            }

            DispatchCommand.NotifyCanExecuteChanged();
            MoveOutCommand.NotifyCanExecuteChanged();
        }

        private List<string> _S2f23SvidInfo = new List<string>();
        private string _S2f23RptId = string.Empty;
        public SecsMessage ReplyScenario(string command, SecsMessage SecsMsg)
        {
            KeyValuePair<bool, string> keyValuePair = new KeyValuePair<bool, string>();
            SecsMessage secsMessage = null;
            List<string> stringList1 = new List<string>();
            List<string> Svid_Info = new List<string>();
            try
            {
                switch (command)
                {
                    case "S1F13":
                        secsMessage = new SecsMessage((byte)1, (byte)14, false)
                        {
                            Name = "APPVER",
                            SecsItem = Item.L(Item.B(new byte[1]), Item.L(Item.A("CyntecWAC"), Item.A(FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion)))
                        };
                        break;
                    case "S10F3":
                        secsMessage = new SecsMessage((byte)10, (byte)4, false)
                        {
                            Name = "Show Message",
                            SecsItem = Item.B(new byte[1])
                        };
                        break;
                    case "S2F23":
                        byte num;

                        if (SecsMsg.SecsItem != null &&
                            SecsMsg.SecsItem.Items.Length.Equals(5))
                        {
                            num = (byte)0;
                            this._S2f23SvidInfo.Clear();
                            this._S2f23RptId = SecsMsg.SecsItem.Items[0].GetString().Trim();
                            foreach (Item obj in SecsMsg.SecsItem.Items[4].Items)
                                this._S2f23SvidInfo.Add(obj.GetString().Trim());

                            this.S6F1_Timer.Interval = Convert.ToInt32(SecsMsg.SecsItem.Items[1].GetString().Trim());
                            this.S6F1_Timer.Enabled = true;
                            isReadyToStart = true;
                        }
                        else
                        {
                            this.S6F1_Timer.Enabled = false;
                            this.S6F1_Timer.Interval = 1000;
                            num = (byte)1;
                        }
                        secsMessage = new SecsMessage((byte)2, (byte)24, false)
                        {
                            Name = string.Empty,
                            SecsItem = Item.B(num)
                        };
                        break;
                    case "S2F41":
                        secsMessage = new SecsMessage((byte)2, (byte)42, false)
                        {
                            Name = "PP Select",
                            SecsItem = Item.L(Item.B(new byte[1]))
                        };
                        break;
                    case "S1F3":
                        foreach (Item obj in SecsMsg.SecsItem.Items)
                            Svid_Info.Add(obj.GetString());
                        List<string> stringList2 = GetS1F3MachineValue(Svid_Info);
                        if (stringList2.Count.Equals(0))
                            stringList2 = new List<string>() { "" };
                        Item[] objArray = new Item[stringList2.Count];
                        for (int index = 0; index < stringList2.Count; ++index)
                        {
#warning 看 HOST 系統 ask Arvin
                            var dataGridBye = new DataGridByteInfo
                            {
                                FileContent = Encoding.UTF8.GetBytes(stringList2[index])//SECS 的世界只有ASCII 要傳中文只能先編碼
                            };
                            objArray[index] = Item.A(JsonConvert.SerializeObject(dataGridBye));
                            //objArray[index] = Item.A(stringList2[index]);
                        }
                        secsMessage = new SecsMessage((byte)1, (byte)4, false)
                        {
                            Name = "StatusUpdate",
                            SecsItem = Item.L(objArray)
                        };
                        break;
                }
            }
            catch (Exception ex)
            {
                CyntecLog.SecsLogger.Error($"ReplyScenario, command: {command}, ex: {ex}");
                //secsMessage = null;
            }
            return secsMessage;
        }
        private static readonly string IDLE = "IDLE";
        private static readonly string ALARM = "ALARM";
        private static readonly string RUN = "RUN";
        public static List<string> GetS1F3MachineValue(List<string> Svid_Info)
        {
            KeyValuePair<bool, string> keyValuePair = new KeyValuePair<bool, string>();
            List<string> s1F3MachineValue = new List<string>();
            var autoCimDataGridInfo1 = new AutoCIMDataGridInfo();
            string str = IDLE;  //預設值
            try
            {
                var group1 = Svid_Info
                    .Select(k => int.TryParse(k, out int svid) ? svid : (int?)null)
                    .Where(svid => svid.HasValue && svid.Value >= 1000 && svid.Value <= 1016)
                    .Select(svid => svid.Value)
                    .ToList();
                var group2 = Svid_Info
                    .Select(k => int.TryParse(k, out int svid) ? svid : (int?)null)
                    .Where(svid => svid.HasValue && (svid.Value < 1000 || svid.Value > 1020))
                    .Select(svid => svid.Value)
                    .ToList();

                Utility.CaptureAllScreens("S1F3", 1);

                try
                {
                    ShowOverlay();
                    if (group1.Count > 0)
                    {

                        AutoItX.WinActivate(Utility.Svid_Info[VID.加載槽號][0]);
                        {
                            CloseIfExists("數據加載");
                            CloseIfExists("產品資料");
                        }
                        {
                            ControlKeyboard();
                        }
                        //收集資料
#warning 應該要一次拿完 在這一頁的訊息 1000~1020
                        foreach (var svid in group1)
                        {
                            VID vid = (VID)svid;
                            s1F3MachineValue.Add(AutoItX.ControlGetText(Utility.Svid_Info[vid][0], "", Utility.Svid_Info[vid][1]));
                        }
                        Utility.CaptureAllScreens("S1F3", 2);
                    }
                    {
                        CloseIfExists("數據加載");
                        CloseIfExists("產品資料");
                    }
                }
                catch (Exception)
                {

                }
                finally
                {
                    HideOverlay();
                }



                // 1022 DataGridView 處理
                foreach (int key in group2)
                {
                    //if (!int.TryParse(key, out int svid)) throw new Exception($"HOST 的 S1F3 Key: {key} 無法轉換為整數");
                    VID vid = (VID)key;
                    if (Utility.Svid_Info.ContainsKey(vid) &&
                        key.Equals((int)Utility.VID.DataGridView))
                    {
                        {
                            CloseIfExists("數據加載");
                            CloseIfExists("產品資料");
                        }

                        KeyValuePair<bool, string> consoleProcessValue = GetConsoleProcessValue(@$"{AppDomain.CurrentDomain.BaseDirectory}\UIControl\_AutoCIMOrigin.exe",
                            string.Join(",", "GetDataGridView", Utility.Svid_Info[vid][0], Utility.Svid_Info[vid][1]));
                        if (consoleProcessValue.Key)
                        {
                            s1F3MachineValue.Add(consoleProcessValue.Value);
                        }
                        else
                        {
                            s1F3MachineValue.Add("");
                            CyntecLog.GeneralLogger.Error($"取得DataGridView錯誤!  結果:{consoleProcessValue.Key}  訊息:{consoleProcessValue.Value}");
                        }
                    }
                    Utility.CaptureAllScreens("S1F3", 3);
                }
                // 1021 機台狀態 處理
                foreach (int key in group2)
                {
                    //if (!int.TryParse(key, out int svid)) throw new Exception($"HOST 的 S1F3 Key: {key} 無法轉換為整數");
                    VID vid = (VID)key;
                    if (Utility.Svid_Info.ContainsKey(vid))
                    {
                        if (key.Equals((int)Utility.VID.機台狀態))
                        {
                            if (false && _S5f1AlarmInfo.Count > 0)
                            {
                                //有任何 Alarm 加入 "ALARM" 跳脫;
                                s1F3MachineValue.Add(ALARM);
                                break;
                            }
                            //讀取datagridview確認 9 ,10 ,11, 12 ,13 槽的狀態
                            {
                                CloseIfExists("數據加載");
                                CloseIfExists("產品資料");
                            }
                            KeyValuePair<bool, string> consoleProcessValue = GetConsoleProcessValue(@$"{AppDomain.CurrentDomain.BaseDirectory}\UIControl\_AutoCIMOrigin.exe",
                                string.Join(",", "GetDataGridView", Utility.Svid_Info[vid][0], Utility.Svid_Info[vid][1]));
                            if (!consoleProcessValue.Key)
                            {
                                s1F3MachineValue.Add(string.Empty);
                                break;
                            }

                            var autoCimDataGridInfo2 = JsonConvert.DeserializeObject<AutoCIMDataGridInfo>(consoleProcessValue.Value);
                            if (!autoCimDataGridInfo2.Rtn_Result.Trim().ToUpper().Equals("TRUE"))
                            {
                                s1F3MachineValue.Add(IDLE);
                                break;
                            }

                            if (autoCimDataGridInfo2.Rtn_Value == null)
                            {
                                s1F3MachineValue.Add(IDLE);
                                break;
                            }

                            if (autoCimDataGridInfo2.Rtn_Value.Count < 21) //槽位少於21
                            {
                                s1F3MachineValue.Add(IDLE);
                                break;
                            }

                            //9~13槽
                            for (int index = 9; index <= 13; ++index)
                            {
                                //col9 => 資料號. col10 => 批號 col11 => 數量 是否有任何資料 in cell
                                if (autoCimDataGridInfo2.Rtn_Value[index].ContainsKey("Col9") &&
                                    autoCimDataGridInfo2.Rtn_Value[index].ContainsKey("Col10") &&
                                    autoCimDataGridInfo2.Rtn_Value[index].ContainsKey("Col11"))
                                {
                                    if (!string.IsNullOrEmpty(autoCimDataGridInfo2.Rtn_Value[index]["Col9"]) &&
                                        !string.IsNullOrEmpty(autoCimDataGridInfo2.Rtn_Value[index]["Col10"]) &&
                                        !string.IsNullOrEmpty(autoCimDataGridInfo2.Rtn_Value[index]["Col11"]))
                                    {
                                        //9~13槽 只要有一槽內有東西在運作 設備狀態就是 "RUN" WC 2026/05/07 Run 時只要加入一次
                                        //s1F3MachineValue.Add(RUN);
                                        str = RUN;
                                        break;
                                    }
                                    str = IDLE;
                                }
                                else
                                    str = IDLE;
                            }
                            s1F3MachineValue.Add(str);
                        }
                    }
                    Utility.CaptureAllScreens("S1F3", 4);
                }

                //WC 2026/05/07 Add 讀取 PLC數值 用水量
                double Scale = 0.01;
                double WaterConsumption = MitsubishiPLC.ReadDWithScale(ePlcDevice.Water_Consumption, Scale);
                CyntecLog.GeneralLogger.Debug($"PLC Read D{(int)ePlcDevice.Water_Consumption} ,Scale:{Scale} ,Water Consumption:{WaterConsumption}");
                s1F3MachineValue.Add(WaterConsumption.ToString());
            }
            catch (Exception ex)
            {
                s1F3MachineValue = new List<string>();
                CyntecLog.GeneralLogger.Error(ex.Message);
            }
            return s1F3MachineValue;
        }

        private sealed class SecsLogger : ISecsGemLogger
        {
            private readonly mainModelView _vm;      // ★ 取得實例
            private readonly NLog.ILogger _logger;

            public SecsLogger(mainModelView vm, NLog.ILogger logger)
            {
                _vm = vm;
                _logger = logger;
            }

            public void MessageIn(SecsMessage msg, int id)
            {
                string msgStr = $"<-- [0x{id:X8}] {msg.ToSml()}";
                ; _vm.AddLog(msgStr, Brushes.Black);
                _logger.Trace(msgStr);
            }

            public void MessageOut(SecsMessage msg, int id)
            {
                string msgStr = $"--> [0x{id:X8}] {msg.ToSml()}";
                _vm.AddLog(msgStr, Brushes.Black);
                _logger.Trace(msgStr);
            }

            public void Info(string msg)
            {
                _vm.AddLog(msg, Brushes.Blue);
                _logger.Info(msg);
            }

            public void Warning(string msg)
            {
                _vm.AddLog(msg, Brushes.Green);
                _logger.Warn(msg);
            }

            public void Error(string msg, SecsMessage? message, Exception? ex)
            {
                _vm.AddLog(msg, Brushes.Red);
                _logger.Error(msg);

                if (message != null)
                {
                    _vm.AddLog(message.ToSml(), Brushes.Red);
                    _logger.Error(message.ToSml());
                }
                if (ex != null)
                {
                    _vm.AddLog(ex.Message, Brushes.Gray);
                    _logger.Error(ex.Message);
                }
            }

            public void Debug(string msg)
            {
                _vm.AddLog(msg, Brushes.Yellow);
                _logger.Debug(msg);
            }
        }
        /// <summary>
        /// 打開"數據加載"頁面
        /// </summary>
        public static async void ControlKeyboard()
        {
            WinAuto.clickDataLoad();
            return;
            //var title = @"Data Monitoring System - [Data Monitoring]";
            //int i = AutoItX.WinActivate(title);
            //await Task.Delay(500);
            //MenuInvoker.ClickParamDataLoadAsync(title);
            //return;
            //AutoItX.Send(@"!a");          // Alt + A => 打開「參數」
            //await Task.Delay(250);
            //AutoItX.Send(@"{DOWN 1}");    // 例如第二項就是「數據加載」
            //await Task.Delay(250);
            //AutoItX.Send(@"{ENTER}");
            //await Task.Delay(2000);
        }

        public class LogItem
        {
            public required string Message { get; set; }
            public required Brush Color { get; set; }   // 可以是 Brushes.Red 等
        }
        /// <summary>
        /// 關閉視窗 By Title
        /// </summary>
        /// <param name="dialogTitle"></param>
        public static void CloseIfExists(string dialogTitle)
        {
            // 檢查視窗是否存在
            if (AutoItX.WinExists(dialogTitle) > 0)
            {
                // 把視窗關掉
                AutoItX.WinClose(dialogTitle);

                // 等待它消失 (最多 3 秒)
                AutoItX.WinWaitClose(dialogTitle, "", 3);
            }
        }

        public static KeyValuePair<bool, string> GetConsoleProcessValue(string filePath, string message)
        {
            KeyValuePair<bool, string> consoleProcessValue = new KeyValuePair<bool, string>();
            string empty = string.Empty;
            try
            {
                using (Process process = new Process())
                {
                    //地區UNICODE要設定成大陸!
                    process.StartInfo = new ProcessStartInfo()
                    {
                        FileName = filePath,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true,
                        Arguments = message
                    };
                    //process.StartInfo = psi;
                    process.Start();
                    string str = process.StandardOutput.ReadToEnd().Trim();

                    consoleProcessValue = !string.IsNullOrEmpty(str) ? new KeyValuePair<bool, string>(true, str) : new KeyValuePair<bool, string>(false, "回傳為空值請確認!");
                    process.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                consoleProcessValue = new KeyValuePair<bool, string>(false, ex.ToString());
            }
            return consoleProcessValue;
        }
        public class AutoCIMDataGridInfo
        {
            public string Rtn_Result { get; set; }

            public List<Dictionary<string, string>> Rtn_Value { get; set; }
        }
        public class DataGridByteInfo
        {
            public byte[] FileContent { get; set; }
        }

        #region EAP 模擬測試

        private volatile bool _isDisposed = false;
        private System.Timers.Timer? _simProcessDataTimer;
        private readonly SemaphoreSlim _simReportLock = new(1, 1);
        private readonly ConcurrentDictionary<string, string> _simActiveAlarms = new();

        public Visibility SimulationTabVisibility => Config.EnableSimulation ? Visibility.Visible : Visibility.Collapsed;

        [ObservableProperty]
        private bool isSimulationMode = false;

        partial void OnIsSimulationModeChanged(bool value)
        {
            if (value)
            {
                AddLog("[模擬模式] 已啟用（跳過現場機台 UI 自動化與 PLC 讀取）", Brushes.DarkOrange);
            }
            else
            {
                if (IsSimAutoReportEnabled)
                {
                    IsSimAutoReportEnabled = false;
                }
                AddLog("[模擬模式] 已停用（恢復正常現場模式）", Brushes.DarkOrange);
            }
        }

        [ObservableProperty]
        private string simWashTemp = "24.5";

        [ObservableProperty]
        private string simCopperTemp = "24.2";

        [ObservableProperty]
        private string simCurrentA = "12.5";

        [ObservableProperty]
        private string simCurrentB = "12.3";

        [ObservableProperty]
        private string simVoltageA = "2.15";

        [ObservableProperty]
        private string simVoltageB = "2.10";

        [ObservableProperty]
        private string simWaterConsumption = "26.8";

        [ObservableProperty]
        private int simAutoReportIntervalSec = 10;

        [ObservableProperty]
        private bool isSimAutoReportEnabled = false;

        partial void OnIsSimAutoReportEnabledChanged(bool value)
        {
            if (value)
            {
                if (!IsSimulationMode)
                {
                    IsSimulationMode = true;
                }

                int intervalSec = SimAutoReportIntervalSec < 1 ? 1 : SimAutoReportIntervalSec;
                SimAutoReportIntervalSec = intervalSec;

                if (_simProcessDataTimer != null)
                {
                    _simProcessDataTimer.Elapsed -= SimProcessDataTimer_Elapsed;
                    _simProcessDataTimer.Stop();
                    _simProcessDataTimer.Dispose();
                }

                _simProcessDataTimer = new System.Timers.Timer(intervalSec * 1000);
                _simProcessDataTimer.Elapsed += SimProcessDataTimer_Elapsed;
                _simProcessDataTimer.AutoReset = true;
                _simProcessDataTimer.Start();

                AddLog($"[模擬-定時上報] 已啟動（每 {intervalSec} 秒發送模擬 ProcessDataReport）", Brushes.DarkOrange);
            }
            else
            {
                _simProcessDataTimer?.Stop();
                AddLog("[模擬-定時上報] 已停止", Brushes.DarkOrange);
            }
        }

        private async void SimProcessDataTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            if (_isDisposed)
                return;

            try
            {
                await SimSendProcessDataAsync();
            }
            catch (ObjectDisposedException)
            {
                // 視窗關閉釋放資源期間忽略
            }
            catch (Exception ex)
            {
                CyntecLog.GeneralLogger.Error($"[Sim][Timer] SimProcessDataTimer_Elapsed Error: {ex}");
            }
        }

        public List<string> SimStatusOptions { get; } = new() { RUN, IDLE, ALARM };

        [ObservableProperty]
        private string simSelectedStatus = RUN;

        [ObservableProperty]
        private string simAlarmCode = "0001";

        [ObservableProperty]
        private string simAlarmMessage = "模擬測試警報";

        [ObservableProperty]
        private string simWorkOrder = "2P261125TEST01";

        [ObservableProperty]
        private string simEmployeeId = "A12345";

        [ObservableProperty]
        private string simTrackOutQty = "10";

        [ObservableProperty]
        private bool simPPSelectPass = true;

        /// <summary>
        /// 產生符合現場 DataGridView 結構的模擬 JSON，供正式 BuildProcessDataParameters() 拆解驗證。
        /// </summary>
        private string BuildSimulatedDataGridJson()
        {
            string washTemp = string.IsNullOrWhiteSpace(SimWashTemp) ? "24.5" : SimWashTemp.Trim();
            string copperTemp = string.IsNullOrWhiteSpace(SimCopperTemp) ? "24.2" : SimCopperTemp.Trim();
            string curA = string.IsNullOrWhiteSpace(SimCurrentA) ? "00.0" : SimCurrentA.Trim();
            string curB = string.IsNullOrWhiteSpace(SimCurrentB) ? "00.0" : SimCurrentB.Trim();
            string volA = string.IsNullOrWhiteSpace(SimVoltageA) ? "0.00" : SimVoltageA.Trim();
            string volB = string.IsNullOrWhiteSpace(SimVoltageB) ? "0.00" : SimVoltageB.Trim();

            var rows = new List<Dictionary<string, string>>
            {
                new() { ["Col1"] = "005#", ["Col8"] = $"{washTemp}℃" },
                new() { ["Col1"] = "009#", ["Col8"] = $"{copperTemp}℃", ["Col15"] = $"{curA}A/{volA}V", ["Col17"] = $"{curB}A/{volB}V" },
                new() { ["Col1"] = "010#", ["Col8"] = $"{copperTemp}℃", ["Col15"] = $"{curA}A/{volA}V", ["Col17"] = $"{curB}A/{volB}V" },
                new() { ["Col1"] = "011#", ["Col8"] = $"{copperTemp}℃", ["Col15"] = $"{curA}A/{volA}V", ["Col17"] = $"{curB}A/{volB}V" },
                new() { ["Col1"] = "012#", ["Col8"] = $"{copperTemp}℃", ["Col15"] = $"{curA}A/{volA}V", ["Col17"] = $"{curB}A/{volB}V" },
                new() { ["Col1"] = "013#", ["Col8"] = $"{copperTemp}℃", ["Col15"] = $"{curA}A/{volA}V", ["Col17"] = $"{curB}A/{volB}V" },
                new() { ["Col1"] = "016#", ["Col8"] = $"{washTemp}℃" },
                new() { ["Col1"] = "019#", ["Col8"] = $"{washTemp}℃" },
                new() { ["Col1"] = "020#", ["Col8"] = $"{washTemp}℃" }
            };

            var info = new AutoCIMDataGridInfo
            {
                Rtn_Result = "TRUE",
                Rtn_Value = rows
            };

            return JsonConvert.SerializeObject(info);
        }

        [RelayCommand]
        private async Task SimSendProcessDataAsync()
        {
            if (_isDisposed)
                return;

            if (_mqtt == null || !_mqtt.IsConnected)
            {
                AddLog("[模擬-ProcessData] 發送失敗: MQTT 未連線", Brushes.Red);
                return;
            }

            bool lockTaken = false;
            try
            {
                lockTaken = await _simReportLock.WaitAsync(0);
                if (!lockTaken)
                    return;

                string simJson = BuildSimulatedDataGridJson();
                string water = string.IsNullOrWhiteSpace(SimWaterConsumption) ? "0" : SimWaterConsumption.Trim();
                var processDataList = BuildProcessDataParameters(simJson, water);

                var processDataReq = new ProcessDataReportRequest
                {
                    TransactionID = Guid.NewGuid().ToString(),
                    CMD = MqttCommands.ProcessDataReport,
                    DATE = DateTime.Now.ToString("yyyyMMddHHmmss"),
                    Machine = Config.EqpNo,
                    ProcessData = processDataList
                };

                await _mqtt.PublishReportAsync(processDataReq);
                CyntecLog.GeneralLogger.Trace($"[Sim][ProcessDataReport] Published TransactionID={processDataReq.TransactionID}, ParamCount={processDataList.Count}");
                AddLog($"[模擬-ProcessData] 已發送 {processDataList.Count} 項參數 (含 RAWDATA, 用水量={water}, TxID={processDataReq.TransactionID[..8]})", Brushes.Green);
            }
            catch (ObjectDisposedException)
            {
                // 視窗關閉釋放資源期間忽略
            }
            catch (Exception ex)
            {
                CyntecLog.GeneralLogger.Error($"[Sim][ProcessDataReport] Error: {ex}");
                AddLog($"[模擬-ProcessData] 發送異常: {ex.Message}", Brushes.Red);
            }
            finally
            {
                if (lockTaken)
                {
                    try
                    {
                        _simReportLock.Release();
                    }
                    catch (ObjectDisposedException)
                    {
                        // 釋放過程中鎖已 Dispose 則忽略
                    }
                }
            }
        }

        [RelayCommand]
        private async Task SimSendStatusChangeAsync()
        {
            if (_mqtt == null || !_mqtt.IsConnected)
            {
                AddLog("[模擬-狀態變更] 發送失敗: MQTT 未連線", Brushes.Red);
                return;
            }

            try
            {
                string oldStatus = string.IsNullOrEmpty(_lastReportedStatus) ? IDLE : _lastReportedStatus;
                string newStatus = string.IsNullOrWhiteSpace(SimSelectedStatus) ? IDLE : SimSelectedStatus.Trim();
                _lastReportedStatus = newStatus;

                var statusReq = new StatusChangeReportRequest
                {
                    TransactionID = Guid.NewGuid().ToString(),
                    CMD = MqttCommands.StatusChangeReport,
                    DATE = DateTime.Now.ToString("yyyyMMddHHmmss"),
                    Machine = Config.EqpNo,
                    OldStatus = oldStatus,
                    NewStatus = newStatus
                };

                await _mqtt.PublishReportAsync(statusReq);
                CyntecLog.GeneralLogger.Trace($"[Sim][StatusChangeReport] Published Old={oldStatus}, New={newStatus}, TxID={statusReq.TransactionID}");
                AddLog($"[模擬-狀態變更] 已發送 StatusChangeReport: {oldStatus} -> {newStatus}", Brushes.Green);
            }
            catch (Exception ex)
            {
                CyntecLog.GeneralLogger.Error($"[Sim][StatusChangeReport] Error: {ex}");
                AddLog($"[模擬-狀態變更] 發送異常: {ex.Message}", Brushes.Red);
            }
        }

        [RelayCommand]
        private async Task SimSendAlarmStartAsync()
        {
            await SendSimulatedAlarmAsync("Start");
        }

        [RelayCommand]
        private async Task SimSendAlarmEndAsync()
        {
            await SendSimulatedAlarmAsync("End");
        }

        private async Task SendSimulatedAlarmAsync(string alarmStatus)
        {
            if (_mqtt == null || !_mqtt.IsConnected)
            {
                AddLog($"[模擬-警報{alarmStatus}] 發送失敗: MQTT 未連線", Brushes.Red);
                return;
            }

            try
            {
                string code = string.IsNullOrWhiteSpace(SimAlarmCode) ? "0001" : SimAlarmCode.Trim();
                string msg = string.IsNullOrWhiteSpace(SimAlarmMessage) ? "模擬測試警報" : SimAlarmMessage.Trim();

                if (alarmStatus == "Start")
                {
                    _simActiveAlarms[code] = msg;
                }
                else
                {
                    _simActiveAlarms.TryRemove(code, out _);
                }

                var alarmReq = new AlarmReportRequest
                {
                    TransactionID = Guid.NewGuid().ToString(),
                    CMD = MqttCommands.AlarmReport,
                    DATE = DateTime.Now.ToString("yyyyMMddHHmmss"),
                    Machine = Config.EqpNo,
                    AlarmStatus = alarmStatus,
                    AlarmCode = code,
                    AlarmMsg = msg,
                    AlarmType = "A"
                };

                await _mqtt.PublishReportAsync(alarmReq);
                CyntecLog.GeneralLogger.Trace($"[Sim][AlarmReport] Published Status={alarmStatus}, Code={code}, Msg={msg}");
                AddLog($"[模擬-警報上報] 已發送 AlarmReport ({alarmStatus}): Code={code}, Msg={msg}", alarmStatus == "Start" ? Brushes.OrangeRed : Brushes.Green);
            }
            catch (Exception ex)
            {
                CyntecLog.GeneralLogger.Error($"[Sim][AlarmReport] Error: {ex}");
                AddLog($"[模擬-警報上報] 發送異常: {ex.Message}", Brushes.Red);
            }
        }

        [RelayCommand]
        private async Task SimSendTrackInAsync()
        {
            if (_mqtt == null || !_mqtt.IsConnected || !_mqtt.IsMesAlive)
            {
                AddLog("[模擬-進站] 發送失敗: MQTT / EAP 未連線", Brushes.Red);
                return;
            }

            if (!IsSimulationMode)
            {
                IsSimulationMode = true;
            }

            string wo = string.IsNullOrWhiteSpace(SimWorkOrder) ? "2P261125TEST01" : SimWorkOrder.Trim();
            string uid = string.IsNullOrWhiteSpace(SimEmployeeId) ? "A12345" : SimEmployeeId.Trim();

            try
            {
                var request = new TrackInRequest
                {
                    TransactionID = Guid.NewGuid().ToString(),
                    CMD = MqttCommands.TrackInReq,
                    DATE = DateTime.Now.ToString("yyyyMMddHHmmss"),
                    Machine = Config.EqpNo,
                    WorkOrder = wo,
                    UserID = uid
                };

                _activeTrackInTxId = request.TransactionID;
                await _mqtt.PublishReportAsync(request);
                CyntecLog.GeneralLogger.Trace($"[Sim][TrackInReq] Published TxID={request.TransactionID}, WorkOrder={wo}, UserID={uid}");
                AddLog($"[模擬-進站] 已發送 TrackInReq: 工單={wo}, 工號={uid} (PP_SELECT模擬={(SimPPSelectPass ? "PASS" : "FAIL")})", Brushes.Blue);
                StartTrackInTimeoutWatcher(request.TransactionID, wo);
            }
            catch (Exception ex)
            {
                _activeTrackInTxId = string.Empty;
                CyntecLog.GeneralLogger.Error($"[Sim][TrackInReq] Error: {ex}");
                AddLog($"[模擬-進站] 發送異常: {ex.Message}", Brushes.Red);
            }
        }

        [RelayCommand]
        private async Task SimSendTrackOutAsync()
        {
            if (_mqtt == null || !_mqtt.IsConnected || !_mqtt.IsMesAlive)
            {
                AddLog("[模擬-出站] 發送失敗: MQTT / EAP 未連線", Brushes.Red);
                return;
            }

            if (!IsSimulationMode)
            {
                IsSimulationMode = true;
            }

            string wo = string.IsNullOrWhiteSpace(SimWorkOrder) ? "2P261125TEST01" : SimWorkOrder.Trim();
            string uid = string.IsNullOrWhiteSpace(SimEmployeeId) ? "A12345" : SimEmployeeId.Trim();
            string qty = string.IsNullOrWhiteSpace(SimTrackOutQty) ? "0" : SimTrackOutQty.Trim();

            try
            {
                var request = new TrackOutRequest
                {
                    TransactionID = Guid.NewGuid().ToString(),
                    CMD = MqttCommands.TrackOutReq,
                    DATE = DateTime.Now.ToString("yyyyMMddHHmmss"),
                    Machine = Config.EqpNo,
                    WorkOrder = new List<string> { wo },
                    UserID = uid,
                    Qty = qty
                };

                _activeTrackOutTxId = request.TransactionID;
                await _mqtt.PublishReportAsync(request);
                CyntecLog.GeneralLogger.Trace($"[Sim][TrackOutReq] Published TxID={request.TransactionID}, WorkOrder={wo}, UserID={uid}, Qty={qty}");
                AddLog($"[模擬-出站] 已發送 TrackOutReq: 工單={wo}, 工號={uid}, 數量={qty}", Brushes.Blue);
                StartTrackOutTimeoutWatcher(request.TransactionID, wo);
            }
            catch (Exception ex)
            {
                _activeTrackOutTxId = string.Empty;
                CyntecLog.GeneralLogger.Error($"[Sim][TrackOutReq] Error: {ex}");
                AddLog($"[模擬-出站] 發送異常: {ex.Message}", Brushes.Red);
            }
        }

        [RelayCommand]
        private void ClearLogs()
        {
            if (Application.Current?.Dispatcher != null && !Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(() => Logs.Clear());
            }
            else
            {
                Logs.Clear();
            }
        }

        #endregion

        public void Dispose()
        {
            _isDisposed = true;
            Debug.WriteLine("XXX");
            S5F1_Timer?.Stop();
            S5F1_Timer?.Dispose();
            S6F1_Timer?.Stop();
            S6F1_Timer?.Dispose();
            if (_simProcessDataTimer != null)
            {
                _simProcessDataTimer.Elapsed -= SimProcessDataTimer_Elapsed;
                _simProcessDataTimer.Stop();
                _simProcessDataTimer.Dispose();
                _simProcessDataTimer = null;
            }
            _simReportLock?.Dispose();
        }
        public class AutoCIMResponse
        {
            public string Rtn_Result { get; set; }
            public List<Row> Rtn_Value { get; set; } = new();
            public string Rtn_Msg { get; set; }
            public string Rtn_DateTime { get; set; }

            public class Row
            {
                public string Col1 { get; set; }  // 代碼/故障地址
                public string Col2 { get; set; }  // 訊息
                public string Col3 { get; set; }  // 時間字串
                public string Col4 { get; set; }  // 備註(可無)
            }
        }
        public class AlarmItem
        {
            public string Id { get; set; }           // Col1
            public string Message { get; set; }      // Col2
            public DateTime? OccurredAt { get; set; }// 由 Col3 解析
        }

        // 回傳執行狀態
        public enum SyncResult { Updated, NoChange, Cleared, Skipped, Error }

        public (SyncResult result, string message) SyncAlarmsFromRaw(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return (SyncResult.Skipped, "empty input");

            // 修正非標準 JSON 的 True/False 大寫
            raw = Regex.Replace(raw, @"\bTrue\b", "true");
            raw = Regex.Replace(raw, @"\bFalse\b", "false");

            JToken token;
            try { token = JToken.Parse(raw); }
            catch (Exception ex)
            {
                return (SyncResult.Error, $"json parse error: {ex.Message}");
            }

            // 取 payload 物件
            JObject payload = token switch
            {
                JArray arr when arr.Count >= 2 => (JObject)arr[1],
                JObject obj => obj,
                _ => null
            };
            if (payload == null) return (SyncResult.Error, "invalid envelope");

            // 取欄位
            string rtnResult = payload.Value<string>("Rtn_Result");
            var rtnValue = payload["Rtn_Value"] as JArray ?? new JArray();
            string rtnMsg = payload.Value<string>("Rtn_Msg") ?? "ok";

            // 判斷 Rtn_Result
            if (!string.Equals(rtnResult?.Trim(), "True", StringComparison.OrdinalIgnoreCase))
                return (SyncResult.Skipped, rtnMsg); // 例如 Win not exists


            if (rtnValue.Count == 0)
            {
                //沒有抓到的狀況
                return (SyncResult.Skipped, rtnMsg);

            }
            // 沒資料（只有表頭或完全無列）→ 逐筆移除以觸發 Remove 通知並發送 AlarmStatus = "0"
            if (rtnValue.Count <= 1)
            {
                if (_S5f1AlarmInfo.Count > 0)
                {
                    foreach (var existingId in _S5f1AlarmInfo.Keys.ToList())
                    {
                        _S5f1AlarmInfo.Remove(existingId);
                    }
                    return (SyncResult.Cleared, "no alarm");
                }
                return (SyncResult.NoChange, "no alarm");
            }

            // 有資料：收集這次所有 id，逐筆同步
            var newIds = new HashSet<string>();
            int changed = 0;

            for (int i = 1; i < rtnValue.Count; i++) // 跳過表頭 index 0
            {
                var row = rtnValue[i] as JObject;
                if (row == null) continue;

                string id = row.Value<string>("Col1")?.Trim();
                string msg = row.Value<string>("Col2")?.Trim();

                if (string.IsNullOrEmpty(id)) continue;
                newIds.Add(id);

                // AddOrUpdate：值相同不通知、不同才觸發 Update
                int before = _S5f1AlarmInfo.Count;
                _S5f1AlarmInfo.AddOrUpdate(id, msg ?? string.Empty);
                int after = _S5f1AlarmInfo.Count;

                // 粗略估算是否變更（新增或更新都算）
                if (after > before || (_S5f1AlarmInfo.TryGetValue(id, out var cur) && cur == (msg ?? string.Empty)))
                {
                    // 注意：這裡只是示意，實際要精準統計可改 ObservableDictionary 回報
                }
            }

            // 刪除這次沒出現的舊 alarm（表示已消失）
            foreach (var existingId in _S5f1AlarmInfo.Keys.ToList())
                if (!newIds.Contains(existingId))
                {
                    _S5f1AlarmInfo.Remove(existingId);
                    changed++;
                }

            return (changed > 0 ? SyncResult.Updated : SyncResult.NoChange, rtnMsg);
        }

        private bool CanAddRecord() => PartNumber != null && PartNumber != "";
        [RelayCommand(CanExecute = nameof(CanAddRecord))]
        private void AddRecord()
        {
            if (PartNumber == null || PartNumber == "") return;
            var exist = Dt?.AsEnumerable().Any(r =>
                (r.Field<string>("PartNumber") ?? "")
                .IndexOf(PartNumber, StringComparison.OrdinalIgnoreCase) >= 0);


            if (exist is true)
            {
                if (MessageBoxResult.Yes == (MessageBox.Show($"確定修改 {PartNumber} ?", "", MessageBoxButton.YesNo)))
                {
                    try
                    {
                        string DateTimeStr = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");


                        string sql = $@"
                                    UPDATE PartTable SET
                                        ProcessNumber={1}, ProcessName='{"Process 1"}', CC_CV_Mode='{"0"}',
                                        BoardLengh={AreaA}, BoardWidt={AreaB}, BoardArea={AreaTotal}, DefaultQty={1},
                                        CurrentDensity1={CurrentDensityA}, CurrentDensity2={CurrentDensityB}, Current1={CurrentA}, Current2={CurrentB},
                                        PlateTime1={TotalPlateTime}, PlateTime2={PlateTimeStep1}, PlateTime3={PlateTimeStep2}, PlateTime4={PlateTimeStep3}, PlateTime5={PlateTimeStep4},
                                        PlateTime6={PlateTimeStep5}, PlateTime7={PlateTimeStep6}, PlateTime8={PlateTimeStep7}, PlateTime9={PlateTimeStep8}, PlateTime10={PlateTimeStep9}, PlateTime11={PlateTimeStep10},
                                        WeakRate1={CurrentProtectPercentage}, WeakRate2={CurrentPercentageStep1}, WeakRate3={CurrentPercentageStep2}, WeakRate4={CurrentPercentageStep3}, WeakRate5={CurrentPercentageStep4}, WeakRate6={CurrentPercentageStep5},
                                        WeakRate7={CurrentPercentageStep6}, WeakRate8={CurrentPercentageStep7}, WeakRate9={CurrentPercentageStep8}, WeakRate10={CurrentPercentageStep9}, WeakRate11={CurrentPercentageStep10},
                                        AddOrEditDateTime='{DateTimeStr}'
                                    WHERE PartNumber='{PartNumber}'";

                        OperationAccessDataBase.OpenDataBase();

                        int r = OperationAccessDataBase.UpdateIntoDataTable(sql);
                        //if (r > 0) 
                    }
                    finally
                    {
                        OperationAccessDataBase.CloseDataBase();
                    }

                }

                LoadDBToGD();
            }
            else
            {
                if (MessageBoxResult.Yes == (MessageBox.Show($"確定新增 {PartNumber} ?", "", MessageBoxButton.YesNo)))
                {
                    try
                    {
                        string DateTimeStr = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        var sql = @$"Insert Into PartTable (PartNumber,ProcessNumber,ProcessName,CC_CV_Mode,BoardLengh,BoardWidt,BoardArea,DefaultQty, CurrentDensity1,CurrentDensity2,Current1,Current2,PlateTime1,PlateTime2,PlateTime3,PlateTime4,PlateTime5,PlateTime6,PlateTime7,PlateTime8,PlateTime9,PlateTime10,PlateTime11,WeakRate1,WeakRate2,WeakRate3,WeakRate4,WeakRate5,WeakRate6,WeakRate7,WeakRate8,WeakRate9,WeakRate10,WeakRate11,AddOrEditDateTime) Values('{PartNumber}',{1},'Process 1','0',{AreaA},{AreaB},{AreaTotal},{1},{CurrentDensityA},{CurrentDensityB},{CurrentA},{CurrentB},{TotalPlateTime},{PlateTimeStep1},{PlateTimeStep2},{PlateTimeStep3},{PlateTimeStep4},{PlateTimeStep5},{PlateTimeStep6},{PlateTimeStep7},{PlateTimeStep8},{PlateTimeStep9},{PlateTimeStep10},{CurrentProtectPercentage},{CurrentPercentageStep1},{CurrentPercentageStep2},{CurrentPercentageStep3},{CurrentPercentageStep4},{CurrentPercentageStep5},{CurrentPercentageStep6},{CurrentPercentageStep7},{CurrentPercentageStep8},{CurrentPercentageStep9},{CurrentPercentageStep10},'{DateTimeStr}')";


                        OperationAccessDataBase.OpenDataBase();

                        //var sql = $"Delete From PartTable Where IndexID={SelectedIndexID}";

                        int r = OperationAccessDataBase.InsertIntoDataTable(sql);
                        //if (r > 0) 
                    }
                    finally
                    {
                        OperationAccessDataBase.CloseDataBase();
                    }

                }
                LoadDBToGD();
            }
            return;
        }
        //private bool CanDeleteRecord() => SelectedIndexID > 0;
        [RelayCommand]
        private void DeleteRecord()
        {
            if (SelectedIndexID == 0) return;
            if (MessageBoxResult.Yes == (MessageBox.Show($"確認刪除 {selectedPartNumber} ?", "", MessageBoxButton.YesNo)))
            {
                try
                {
                    OperationAccessDataBase.OpenDataBase();

                    var sql = $"Delete From PartTable Where IndexID={SelectedIndexID}";

                    int r = OperationAccessDataBase.DeleteFromDataTable(sql);
                    //if (r > 0) 
                }
                finally
                {
                    OperationAccessDataBase.CloseDataBase();
                }
                LoadDBToGD();
            }
            return;
        }
        [ObservableProperty] private DataTable? dt;  // 產生 public DataTable? DT { get; set; }

        [RelayCommand]
        public void LoadDBToGD()
        {
            try
            {
                OperationAccessDataBase.OpenDataBase();
                Dt = OperationAccessDataBase.QueryToDataTable("select * from PartTable order by AddOrEditDateTime DESC");
            }
            catch (Exception) { }
            finally
            {
                OperationAccessDataBase.CloseDataBase();
            }

        }
    }
}
