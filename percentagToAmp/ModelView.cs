using AutoIt;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using static Microsoft.Extensions.Logging.EventSource.LoggingEventSource;
using static percentagToAmp.Utility;

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
        private readonly CancellationTokenSource _cts = new();

        // 是否忙碌；變化時會觸發 CanExecute 重新評估
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(DispatchCommand))]
        private bool isBusy;

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
            Application.Current.Dispatcher.Invoke(() =>
            {
                _overlay?.SetClickThrough(enable);
            });
        }

        public mainModelView()
        {
            // 初次驗證（可選）
            ValidateAllProperties();
            S5F1_Timer = new System.Timers.Timer();
            S5F1_Timer.Interval = 1000 * 1;
            S5F1_Timer.Elapsed += S5F1_Timer_Elapsed;
            S5F1_Timer.Start();

            S6F1_Timer = new System.Timers.Timer();
            S6F1_Timer.Elapsed += S6F1_Timer_Elapsed;

            this._logger = (ISecsGemLogger)new SecsLogger(this, CyntecLog.SecsLogger);
            _S5f1AlarmInfo.OnChanged += _S5f1AlarmInfo_OnChanged;
        }

        private void S6F1_Timer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            if (_secsGem == null || _connector == null || _connector.State != Secs4Net.ConnectionState.Selected) return; //沒連上HOST就不做

            if (this._S2f23SvidInfo.Count <= 0) return;

            S6F1_Timer.Stop();

            try
            {
                var vidMsg_Info = GetS1F3MachineValue(this._S2f23SvidInfo);

                Item[] objArray = new Item[vidMsg_Info.Count];
                for (int index = 0; index < vidMsg_Info.Count; ++index)
                {
                    var dataGridBye = new DataGridByteInfo
                    {
                        FileContent = Encoding.UTF8.GetBytes(vidMsg_Info[index])//SECS 的世界只有ASCII 要傳中文只能先編碼
                    };
                    objArray[index] = Item.A(JsonConvert.SerializeObject(dataGridBye));
                }

                var SecsMsg = new SecsMessage((byte)6, (byte)1)
                {
                    Name = string.Empty,
                    SecsItem = Item.L(Item.A(this._S2f23RptId), Item.A("0"), Item.A(DateTime.Now.ToString("yyyyMMddHHmmssffff")), Item.L(objArray))
                };

                _ = _secsGem.SendAsync(SecsMsg, this._cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {

            }
            finally
            {
                S6F1_Timer.Start();
            }
        }

        private void _S5f1AlarmInfo_OnChanged(string action, string key, string value)
        {
            CyntecLog.GeneralLogger.Debug($"動作: {action}, Key={key}, Value={value}");
            string name = string.Empty;
            name = $"AlarmReport";
            var dataGridBye = new DataGridByteInfo
            {
                FileContent = Encoding.UTF8.GetBytes(value)
            };
            if (action == "Add" || action == "Update")
            {
                //應該永遠不會有 Update 除非 Alarm description 於 Runtime 中有變動
                var secsMsg = new SecsMessage(5, 1)
                {
                    Name = name,
                    SecsItem = Item.L(Item.A("Y"), Item.A(key), Item.A(JsonConvert.SerializeObject(dataGridBye)))
                };
                _ = _secsGem?.SendAsync(secsMsg, _cancellationTokenSource.Token);
            }
            else if (action == "Remove")
            {

                var secsMsg = new SecsMessage(5, 1)
                {
                    Name = name,
                    SecsItem = Item.L(Item.A("N"), Item.A(key), Item.A(JsonConvert.SerializeObject(dataGridBye)))
                };
                _ = _secsGem?.SendAsync(secsMsg, _cancellationTokenSource.Token);
            }
        }

        private bool _AlarmFlag = false;
        private static ObservableDictionary<string, string> _S5f1AlarmInfo = new();
        private void S5F1_Timer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            if (_secsGem == null || _connector == null || _connector.State != Secs4Net.ConnectionState.Selected) return; //沒連上HOST就不做

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
            Logs.Add(new LogItem { Message = msg, Color = color });
            if (Logs.Count > MaxLines) Logs.RemoveAt(0);
        }

        // 當屬性改變時做即時驗證
        partial void OnWorkOrderNoChanged(string value) => ValidateProperty(value, nameof(WorkOrderNo));
        partial void OnEmployeeIdChanged(string value) => ValidateProperty(value, nameof(EmployeeId));

        // 判斷是否可派送：欄位都有效且非忙碌
        private bool CanDispatch() => !IsBusy && !HasErrors && _connector?.State == Secs4Net.ConnectionState.Selected;

        // 派送（非同步）
        [RelayCommand(CanExecute = nameof(CanDispatch))]
        private async Task DispatchAsync()
        {
            //object value = Utility.確認監控程式是否處於正常();

            // 再保險驗證一次
            ValidateAllProperties();
            if (HasErrors) return;

            try
            {
                IsBusy = true;
                string lotId = WorkOrderNo.Trim();
                string userId = EmployeeId.Trim();

                if (string.IsNullOrEmpty(lotId) || string.IsNullOrEmpty(userId))
                {
                    //MainForm.LogHelper.WriteLog("人員未輸入批號&工號&QR_CODE!");
                    return;
                }

                // 檢查 Secs 連線狀態
                if (_secsGem == null || _connector == null || _connector.State != Secs4Net.ConnectionState.Selected)
                {
                    // 未連線狀態，不送 S6F11
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

                    CyntecLog.GeneralLogger.Debug($"S6F11, NOVE_IN, lotId: {lotId}, userId: {userId}");
                    // 發送 S6F11 MOVE_IN //帶入批號, 工號
                    SecsMessage reply = await _secsGem.SendAsync(
                        SECSBuilder.BuildS6F11Message("MOVE_IN", paramInfo),
                        _cancellationTokenSource.Token);

                    // reply 內容可以處理或忽略
                }
                catch (SecsException ex)
                {
                    CyntecLog.SecsLogger.Error(ex);
                    //MainForm.LogHelper.WriteLog($"派工傳送 Command 流程異常! 原因【{ex}】");
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
                //MainForm.LogHelper.WriteLog($"派工按鈕流程異常! 原因【{ex}】");
            }
            finally
            {
                IsBusy = false;
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

                // TODO: 在此呼叫你的 SECS/GEM 或後端服務
                // 模擬耗時工作
                await Task.Run(()=>{
                    SecsConnection();
                });

                // 成功後可清空欄位或顯示提示
                // WorkOrderNo = string.Empty;
                // EmployeeId  = string.Empty;
            }
            catch (OperationCanceledException)
            {
                // 被取消
            }
            finally
            {
                
            }
        }

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
            this._connector.ConnectionChanged += _connector_ConnectionChanged;
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
                                    MessageBox.Show(primaryMessage.PrimaryMessage.SecsItem.Items[1].GetString());
                                    _ = primaryMessage.TryReplyAsync(ReplyMsg, this._cancellationTokenSource.Token);
                                }
                                else if ($"S{sStr}F{fStr}".Equals("S2F41")) //應該做完才回
                                {
                                    bool 成功設定 = false;
                                    CyntecLog.GeneralLogger.Trace("S2F41 recived");
                                    // Handle S2F41 PP_SELECT message
                                    //if ($"S{sStr3}F{fStr3}".Equals("S2F41") &&
                                    if(primaryMessage.PrimaryMessage.SecsItem.Items[0].GetString().Trim().ToUpper().Equals("PP_SELECT"))
                                    {
                                        
                                        var 資料編號 = primaryMessage.PrimaryMessage.SecsItem.Items[1][0][0].GetString();
                                        var 批號 = primaryMessage.PrimaryMessage.SecsItem.Items[1][0][1].GetString();
                                        var 片數 = primaryMessage.PrimaryMessage.SecsItem.Items[1][0][2].GetString();

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
                                            //// Click button

                                            SetOverlayClickThrough(true);
                                            AutoItX.ControlClick(Utility.Svid_Info[Utility.VID.清除當前][0], "", Utility.Svid_Info[Utility.VID.清除當前][1]);
                                            SetOverlayClickThrough(false);
                                            Thread.Sleep(500);
                                            Utility.CaptureAllScreens($"S{sStr}F{fStr}", 3);

                                            SetOverlayClickThrough(true);
                                            //// Click prompt button
                                            AutoItX.ControlClick("提示", "", "[CLASS:Button; INSTANCE:1]"); //popout - 確定  Thread.Sleep(500);
                                            SetOverlayClickThrough(false);
                                            Thread.Sleep(1500); //PLC清除需要時間
                                            
                                            Utility.CaptureAllScreens($"S{sStr}F{fStr}", 4);
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

                                            AutoItX.ControlSetText(Utility.Svid_Info[Utility.VID.批號][0], "", Utility.Svid_Info[Utility.VID.批號][1], 批號);
                                            Thread.Sleep(500);
                                            Utility.CaptureAllScreens($"S{sStr}F{fStr}", 6);

                                            AutoItX.ControlFocus(Utility.Svid_Info[Utility.VID.資料編號][0], "", Utility.Svid_Info[Utility.VID.資料編號][1]);
                                            AutoItX.ControlSetText(Utility.Svid_Info[Utility.VID.資料編號][0], "", Utility.Svid_Info[Utility.VID.資料編號][1], 資料編號);
                                            //WinAuto.SetPartNumber(資料編號);
                                            Thread.Sleep(500);
                                            Utility.CaptureAllScreens($"S{sStr}F{fStr}", 7);
                                            AutoItX.Send("{ENTER}"); //必要
                                            Thread.Sleep(500);
                                            Utility.CaptureAllScreens($"S{sStr}F{fStr}", 8);
                                            AutoItX.ControlSetText(Utility.Svid_Info[Utility.VID.數量][0], "", Utility.Svid_Info[Utility.VID.數量][1], 片數);
                                            Thread.Sleep(500);
                                            Utility.CaptureAllScreens($"S{sStr}F{fStr}", 9);
                                            SetOverlayClickThrough(true);
                                            AutoItX.ControlClick(Utility.Svid_Info[Utility.VID.加載][0], "", Utility.Svid_Info[Utility.VID.加載][1]); //popout - 確定
                                            //SetOverlayClickThrough(false);
                                            Thread.Sleep(500);
                                            Utility.CaptureAllScreens($"S{sStr}F{fStr}", 10);
                                            //SetOverlayClickThrough(true);
                                            AutoItX.ControlClick("提示", "", "[CLASS:Button; INSTANCE:1]"); //popout - 確定 
                                            SetOverlayClickThrough(false);
                                            Thread.Sleep(1500); //寫入PLC需要一點時間
                                            Utility.CaptureAllScreens($"S{sStr}F{fStr}", 11);
                                            SetOverlayClickThrough(true);
                                            AutoItX.ControlClick(Utility.Svid_Info[Utility.VID.退出][0], "", Utility.Svid_Info[Utility.VID.退出][1]); //popout - 確定  Thread.Sleep(500);
                                            SetOverlayClickThrough(false);
                                            {
                                                KeyValuePair<bool, string> consoleProcessValue = GetConsoleProcessValue(@$"{AppDomain.CurrentDomain.BaseDirectory}\UIControl\_AutoCIMOrigin.exe",
                                                    string.Join(",", "GetDataGridView", Utility.Svid_Info[VID.DataGridView][0], Utility.Svid_Info[VID.DataGridView][1]));

                                                if (JsonHelpers.TryGetPartAndLot(consoleProcessValue.Value, out var part, out var lot, targetKey: "001#", requireRtnResultTrue: true))
                                                {
                                                    if (string.Equals(資料編號, part) && lot.StartsWith(批號))
                                                    {
                                                        成功設定 = true;
                                                    }
                                                    CyntecLog.GeneralLogger.Debug($"資料編號: {資料編號}, 批號: {批號}, 片數: {片數}, part: {part}, lot: {lot}, 成功設定: {成功設定}");
                                                }
                                            }
                                        }
                                        catch(Exception ex)
                                        {
                                            CyntecLog.GeneralLogger.Error(ex);
                                        }
                                        finally 
                                        {
                                            HideOverlay();
                                            //確認是否有被正確寫入PLC
                                            Application.Current.Dispatcher.Invoke(() =>
                                            ForegroundHelper.BringToFront(Application.Current.MainWindow));

                                            if (!成功設定) MessageBox.Show("設定失敗!");

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
        [ObservableProperty]
        private Secs4Net.ConnectionState connectionState;

        [ObservableProperty]
        private string connectionStatusText = "SCES: 未連線";

        private void _connector_ConnectionChanged(object? sender, Secs4Net.ConnectionState e)
        {

            ConnectionState = e;

            ConnectionStatusText = e switch
            {
                Secs4Net.ConnectionState.Connecting => "SCES: 連線中...",
                Secs4Net.ConnectionState.Connected => "SCES: 已連線 (等待Select)",
                Secs4Net.ConnectionState.Selected => "SCES: 已選擇 (Ready)",
                _ => "SCES: 未知"
            };
            //DispatchCommand.NotifyCanExecuteChanged();
            // 讓 RelayCommand 的 CanExecute 重新評估
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            {
                DispatchCommand.NotifyCanExecuteChanged();
            }));
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
            string str = string.Empty;
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
                            if ( false && _S5f1AlarmInfo.Count > 0)
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
                                        //9~13槽 只要有一槽內有東西在運作 設備狀態就是 "RUN"
                                        s1F3MachineValue.Add(RUN);
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
;                _vm.AddLog(msgStr, Brushes.Black);
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
        public void Dispose()
        {
            Debug.WriteLine("XXX");
            S5F1_Timer?.Stop();
            S5F1_Timer?.Dispose();
            S6F1_Timer?.Stop();
            S6F1_Timer?.Dispose();
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
            // 沒資料（只有表頭或完全無列）→ 清空
            if (rtnValue.Count <= 1)
            {
                if (_S5f1AlarmInfo.Count > 0)
                {
                    _S5f1AlarmInfo.Clear();   // 會觸發 Clear
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
