using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace percentagToAmp
{
    public partial class CurrentStep : ObservableObject    
    {
        private readonly Func<double> _getBase; // 取得基準電流 CurrentA 的委派
        private bool _updating;

        public CurrentStep(Func<double> getBase, string name)
        {
            _getBase = getBase;
            Name = name;
        }

        [ObservableProperty] private string name = "";

        [ObservableProperty] private double current;     // 可編輯：電流(A)
        [ObservableProperty] private double percentage;  // 可編輯：百分比(%)

        partial void OnCurrentChanged(double value)
        {
            if (_updating) return;
            try
            {
                _updating = true;
                var baseA = _getBase();
                Percentage = baseA > 0 ? Clamp(Math.Round(value / baseA * 100.0, 2), 0, 100) : 0;
            }
            finally { _updating = false; }
        }

        partial void OnPercentageChanged(double value)
        {
            if (_updating) return;
            try
            {
                _updating = true;
                Current = Math.Round(_getBase() * Clamp(value, 0, 100) / 100.0, 2);
            }
            finally { _updating = false; }
        }

        // 當基準電流變動時呼叫：依目前百分比重算電流
        public void RecalcFromBase()
        {
            if (_updating) return;
            try
            {
                _updating = true;
                Current = Math.Round(_getBase() * Clamp(Percentage, 0, 100) / 100.0, 2);
            }
            finally { _updating = false; }
        }

        private static double Clamp(double x, double min, double max)
            => x < min ? min : (x > max ? max : x);
    }
}
