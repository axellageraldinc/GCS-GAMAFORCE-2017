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
    /// Interaction logic for ChooseUAV.xaml
    /// </summary>
    public partial class ChooseUAV : Window
    {
        int statusUAV=0; //0 = FW, 1 = Quad
        public ChooseUAV()
        {
            InitializeComponent();
            txtSelectedUAV.Content = "Fixed Wing";
        }

        private void btnFW_Click(object sender, RoutedEventArgs e)
        {
            statusUAV = 0;
            txtSelectedUAV.Content = "Fixed Wing";
            FW_Biasa.Visibility = Visibility.Hidden;
            FW_Selected.Visibility = Visibility.Visible;
            Quad_Biasa.Visibility = Visibility.Visible;
            Quad_Selected.Visibility = Visibility.Hidden;
        }

        private void btnQuad_Click(object sender, RoutedEventArgs e)
        {
            statusUAV = 1;
            txtSelectedUAV.Content = "Quadcopter";
            FW_Biasa.Visibility = Visibility.Visible;
            FW_Selected.Visibility = Visibility.Hidden;
            Quad_Biasa.Visibility = Visibility.Hidden;
            Quad_Selected.Visibility = Visibility.Visible;
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
            var newMyWindow2 = new MainWindow(statusUAV);
            newMyWindow2.Show();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            App.Current.Shutdown();
        }
    }
}
