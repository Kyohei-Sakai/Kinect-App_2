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

namespace Kinect._1020.ユーザビリティ評価タスク作成
{
    /// <summary>
    /// Window1.xaml の相互作用ロジック
    /// </summary>
    public partial class Window1 : Window
    {
        public Window1()
        {
            InitializeComponent();
        }

        //MainWindow Mainwindow = new MainWindow();

        private void buttonName_Click(object sender, RoutedEventArgs e)
        {           
            /*
            if (textBoxName.Text == "")
            {
                MessageBox.Show("名前を入力して下さい");
            }
            else
            {
                //ファイル名
                Mainwindow.fullPath = Mainwindow.workpath + textBoxName.Text + "_result" + ".txt";
                //書き込みファイルをオープン
                Mainwindow.WS = new System.IO.StreamWriter(Mainwindow.fullPath, false, System.Text.Encoding.Default);

                this.IsEnabled = false;
                Mainwindow.IsEnabled = true;
                //this.Close();
            }
            */

        }
    }
}
