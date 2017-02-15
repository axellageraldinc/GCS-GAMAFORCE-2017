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

namespace GCS_WPF_2
{
    /// <summary>
    /// Interaction logic for HUD_Big.xaml
    /// </summary>
    public partial class HUD_Big : Window
    {
        public HUD_Big()
        {
            InitializeComponent();
        }
        private UserControl control;

        public HUD_Big(UserControl control)
        : this()
        {
            this.control = control;
            control.Width = 642;
            control.Height = 619;
            this.Panel_HUD_2.Children.Add(this.control);
        }
    }
}
