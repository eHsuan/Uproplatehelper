using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace percentagToAmp
{
    /// <summary>
    /// Window1.xaml 的互動邏輯
    /// </summary>
    public partial class MessageBoxWindow : Window
    {
        public MessageBoxWindow()
        {
            InitializeComponent();
        }

        public void UpdateMessage(string message)
        {
            txtMessage.Text = message;

            this.Topmost = true;   // 永遠頂層
            this.Show();
            this.Activate();       // Bring to front
        }
    }
}
