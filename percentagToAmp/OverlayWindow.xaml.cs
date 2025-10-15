using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace percentagToAmp
{
    /// <summary>
    /// Interaction logic for OverlayWindow.xaml
    /// </summary>
    public partial class OverlayWindow : Window
    {
        public OverlayWindow()
        {
            InitializeComponent();
            // 跨多螢幕覆蓋整個虛擬桌面
            //Left = SystemParameters.VirtualScreenLeft;
            //Top = SystemParameters.VirtualScreenTop;
            //Width = SystemParameters.VirtualScreenWidth;
            //Height = SystemParameters.VirtualScreenHeight;

            //WC 2025/10/14 Modify 擋主螢幕中間即可
            // 取得主螢幕的解析度
            double screenWidth = SystemParameters.PrimaryScreenWidth;
            double screenHeight = SystemParameters.PrimaryScreenHeight;

            // 設定視窗大小
            Width = 500;
            Height = 300;

            // 計算置中位置
            Left = (screenWidth - Width) / 2;
            Top = (screenHeight - Height) / 2;
        }

        const int GWL_EXSTYLE = -20;
        const int WS_EX_TRANSPARENT = 0x00000020;
        const int WS_EX_LAYERED = 0x00080000;

        [DllImport("user32.dll")] static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")] static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        public void SetClickThrough(bool enable)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            int ex = GetWindowLong(hwnd, GWL_EXSTYLE);
            if (enable)
                SetWindowLong(hwnd, GWL_EXSTYLE, ex | WS_EX_TRANSPARENT | WS_EX_LAYERED);
            else
                SetWindowLong(hwnd, GWL_EXSTYLE, (ex & ~WS_EX_TRANSPARENT) | WS_EX_LAYERED);
        }
    }
}
