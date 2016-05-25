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

namespace Kinect._1110.実験タスク_フィッツの法則_
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {

        Window1 subwindow = new Window1();

        //出力ファイルを格納するフォルダ
        public string workpath = "c:\\予備実験2\\";
        //出力ファイルのフルパスを格納する変数
        public string fullPath = "";
        //WSという名のファイルストリームの宣言
        public System.IO.StreamWriter WS;


        int start = 0;
        double time1, time2, time3;
        int count = 0;


        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            subwindow.Show();

            

        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //ファイルを閉じる
            WS.Close();
            subwindow.Close();


        }




        private void button1_Click(object sender, RoutedEventArgs e)
        {
            if(start == 0)
            {
                //ファイル名
                fullPath = workpath + subwindow.textBox.Text + "_" + subwindow.Control.Text + "_"
                                    + subwindow.ButtonSize.Text + "_" + subwindow.Distance.Text + ".csv";
                //書き込みファイルをオープン
                WS = new System.IO.StreamWriter(fullPath, false, System.Text.Encoding.Default);

                button1.Content = "Button";

                var L = Convert.ToInt32(subwindow.ButtonSize.Text);

                button1.Height = L;
                button1.Width = L;
                button2.Height = L;
                button2.Width = L;

                start = 1;

            }
            else
            {
                button1.IsEnabled = false;

                time1 = Environment.TickCount;

                //距離
                var r = Convert.ToInt32(subwindow.Distance.Text);

                //中心座標
                var a = Canvas.GetLeft(button1);
                var b = Canvas.GetTop(button1);

                //ボタンの位置決め
                //x座標をランダムで決める
                int seed = Environment.TickCount;
                Random rnd = new Random(seed++);
                var x = rnd.Next((int)a - r, (int)a + r + 1);

                //＋-を決める
                var h = rnd.Next(0, 2);
                int d = 0;
                switch (h)
                {
                    case 0:
                        d = 1;
                        break;
                    case 1:
                        d = -1;
                        break;
                }

                //y座標を求める
                var y_b = d * Math.Sqrt(r * r - (x - a) * (x - a));
                var y = b + y_b;

                //label1.Content = x;
                //label2.Content = y;

                Canvas.SetLeft(button2, x);
                Canvas.SetTop(button2, y);
                //CanvasButton.Children.Add(button2);
                button2.Visibility = Visibility.Visible;


            }



        }

        int enter = 0;

        private void button2_MouseEnter(object sender, MouseEventArgs e)
        {
            if (enter == 0)
            {
                time2 = Environment.TickCount;

                enter = 1;
            }


        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            count++;

            time3 = Environment.TickCount;
            //ファイルに反応時間を書き込む
            WS.WriteLine("{0} {1}", time2 - time1, time3 - time2);

            if (count == 30)
            {
                //ファイルを閉じる
                WS.Close();
                MessageBox.Show(fullPath + "に保存しました");

                button2.Visibility = Visibility.Hidden;
                button1.Content = "OK";
                button1.IsEnabled = true;
                count = 0;
                enter = 0;
                start = 0;

            }
            else
            {
                //初期化
                button2.Visibility = Visibility.Hidden;
                button1.IsEnabled = true;
                enter = 0;


            }



        }
    }
}
