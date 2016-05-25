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
using System.Drawing;

namespace Kinect._1020.ユーザビリティ評価タスク作成
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



        //出力ファイルを格納するフォルダ
        public string workpath = "c:\\予備実験\\";
        //出力ファイルのフルパスを格納する変数
        public string fullPath = "";
        //WSという名のファイルストリームの宣言
        public System.IO.StreamWriter WS;

        Window1 Namewindow = new Window1();

        int start = 0;

        private System.Windows.Controls.Button[] B;

        int i, j;
        int a, b;
        int time1 = 0, time2 = 0;
        double disX, disY, disBtn;
        int count = 0;


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {            
            Namewindow.Show();

            this.B = new System.Windows.Controls.Button[12];

            this.B[0] = this.button1;
            this.B[1] = this.button2;
            this.B[2] = this.button3;
            this.B[3] = this.button4;
            this.B[4] = this.button5;
            this.B[5] = this.button6;
            this.B[6] = this.button7;
            this.B[7] = this.button8;
            this.B[8] = this.button9;
            this.B[9] = this.button10;
            this.B[10] = this.button11;
            this.B[11] = this.button12;

            for (i = 1; i < 12; i++)
            {
                this.B[i].IsEnabled = false;

            }

            //this.IsEnabled = false;

        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //ファイルを閉じる
            //WS.Close();
            Namewindow.Close();
            //MessageBox.Show(fullPath + "に保存しました");

        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            if (start == 0)
            {
                start = 1;

                //ファイル名
                fullPath = workpath + Namewindow.textBoxName.Text + "_" + Namewindow.comboBox.Text + "_result" + ".txt";
                //書き込みファイルをオープン
                WS = new System.IO.StreamWriter(fullPath, false, System.Text.Encoding.Default);

                B[0].IsEnabled = false;

                Change();
            }
            else
            {
                CheckA(this.button1);
                CheckB(this.button1);
            }




        }


        private void button2_Click(object sender, RoutedEventArgs e)
        {
            CheckA(this.button2);
            CheckB(this.button2);

        }

        private void button3_Click(object sender, RoutedEventArgs e)
        {
            CheckA(this.button3);
            CheckB(this.button3);

        }
        private void button4_Click(object sender, RoutedEventArgs e)
        {
            CheckA(this.button4);
            CheckB(this.button4);

        }
        private void button5_Click(object sender, RoutedEventArgs e)
        {
            CheckA(this.button5);
            CheckB(this.button5);

        }
        private void button6_Click(object sender, RoutedEventArgs e)
        {
            CheckA(this.button6);
            CheckB(this.button6);

        }
        private void button7_Click(object sender, RoutedEventArgs e)
        {
            CheckA(this.button7);
            CheckB(this.button7);

        }
        private void button8_Click(object sender, RoutedEventArgs e)
        {
            CheckA(this.button8);
            CheckB(this.button8);

        }
        private void button9_Click(object sender, RoutedEventArgs e)
        {
            CheckA(this.button9);
            CheckB(this.button9);

        }
        private void button10_Click(object sender, RoutedEventArgs e)
        {
            CheckA(this.button10);
            CheckB(this.button10);

        }
        private void button11_Click(object sender, RoutedEventArgs e)
        {
            CheckA(this.button11);
            CheckB(this.button11);

        }
        private void button12_Click(object sender, RoutedEventArgs e)
        {
            CheckA(this.button12);
            CheckB(this.button12);
        }
        

        private void Change()
        {
            int seed = Environment.TickCount;
            Random rnd = new Random(seed++);

            a = rnd.Next(0, 12);
            b = rnd.Next(0, 12);
            
            while (a == b)
            {
                b = rnd.Next(0, 12);

            }

            //this.B[a].Background = Brushes.Red;
            B[a].IsEnabled = true;
            B[a].Content = 1;
            B[a].FontSize = 24;

            B[b].IsEnabled = true;
            B[b].Content = 2;
            B[b].FontSize = 24;


        }

        private void CheckA(System.Windows.Controls.Button f)
        {
            if (f == B[a])
            {
                time1 = Environment.TickCount;
                f.IsEnabled = false;
                f.Content = "button";
                B[a].FontSize = 9;

            }

        }

        private void CheckB(System.Windows.Controls.Button l)
        {
            if (l == B[b])
            {
                if (B[a].IsEnabled == false)
                {                    
                    count++;

                    time2 = Environment.TickCount;
                    l.IsEnabled = false;
                    l.Content = "button";
                    B[b].FontSize = 9;


                    disX = Math.Abs(B[b].Margin.Left - B[a].Margin.Left);
                    disY = Math.Abs(B[b].Margin.Top - B[a].Margin.Top);


                    disBtn = Math.Sqrt(Math.Pow(disX, 2) + Math.Pow(disY, 2));


                    //ファイルに反応時間を書き込む
                    WS.WriteLine("{0} {1}", (int)disBtn, time2 - time1);


                    if (count == 30)
                    {
                        //this.IsEnabled = false;

                        //ファイルを閉じる
                        WS.Close();
                        MessageBox.Show(fullPath + "に保存しました");

                        start = 0;
                        count = 0;
                        this.button1.IsEnabled = true;

                    }
                    else
                    {
                        Change();
                    }


                }
            }
            
            
        }





    }

}
