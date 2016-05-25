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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace カウント
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        int count = 0;

        private void button_Click(object sender, RoutedEventArgs e)
        {
            count++;

            if (count % 2 == 0)
            {
                label.Foreground = Brushes.Red;

            }
            else
            {
                label.Foreground = Brushes.Blue;

            }

            label.Content = count;


        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            count = 0;
            label.Foreground = Brushes.Black;
            label.Content = count;


        }
    }
}
