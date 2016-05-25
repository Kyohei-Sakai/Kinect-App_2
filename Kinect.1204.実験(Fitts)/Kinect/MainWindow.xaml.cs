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
using System.Windows.Media.Media3D;
using Microsoft.Kinect;
using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace Kinect
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        // Kinect SDK
        KinectSensor kinect;

        BodyFrameReader bodyFrameReader;
        Body[] bodies;

        DepthFrameReader depthFrameReader;
        FrameDescription depthFrameDesc;

        //マウス操作の関数宣言
        [DllImport("USER32.dll", CallingConvention = CallingConvention.StdCall)]
        static extern void SetCursorPos(int X, int Y);

        [DllImport("USER32.dll", CallingConvention = CallingConvention.StdCall)]
        static extern void mouse_event(int dFlags, int dx, int dy, int cButtons, int dwExtraInfo);

        private const int MOUSEEVENTF_LEFTDOWN = 0x2;
        private const int MOUSEEVENTF_LEFTUP = 0x4;


        Window1 subwindow = new Window1();

        //出力ファイルを格納するフォルダ
        public string workpath = "c:\\本実験\\";
        //出力ファイルのフルパスを格納する変数
        public string fullPath = "";
        //WSという名のファイルストリームの宣言
        public System.IO.StreamWriter WS;

        DispatcherTimer timer2, timer3;


        double time1, time2, time3;
        int count = 0;








        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Kinectを開く
                kinect = KinectSensor.GetDefault();
                kinect.Open();

                // 表示のためのデータを作成
                depthFrameDesc = kinect.DepthFrameSource.FrameDescription;

                // Bodyを入れる配列を作る
                bodies = new Body[kinect.BodyFrameSource.BodyCount];

                // ボディーリーダーを開く
                bodyFrameReader = kinect.BodyFrameSource.OpenReader();
                bodyFrameReader.FrameArrived += bodyFrameReader_FrameArrived;

                //コンボボックスに設定する
                foreach (var type in Enum.GetValues(typeof(ChooseTrackingPlayerType)))
                {
                    ComboChoosePlayerType.Items.Add(type.ToString());

                }
                ComboChoosePlayerType.SelectedIndex = 0;

                subwindow.Show();

                button2.Background = Brushes.Green;


                timer2 = new DispatcherTimer();
                timer2.Tick += timer2_Tick;
                timer2.Interval = new TimeSpan(0, 0, 2);

                timer3 = new DispatcherTimer();
                timer3.Tick += timer3_Tick;
                timer3.Interval = new TimeSpan(0, 0, 0, 0, 100);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                Close();
            }



        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //ファイルを閉じる
            WS.Close();
            subwindow.Close();

            if (depthFrameReader != null)
            {
                depthFrameReader.Dispose();
                depthFrameReader = null;
            }

            if (bodyFrameReader != null)
            {
                bodyFrameReader.Dispose();
                bodyFrameReader = null;
            }

            if (kinect != null)
            {
                kinect.Close();
                kinect = null;
            }

        }




















        int buttonstate = 1;
        double centerx, centery;
        int S;
        double dbuttonx, dbuttony;
        double lbuttonl;
        int N;


        private void button1_Click(object sender, RoutedEventArgs e)
        {
            //ファイル名
            fullPath = workpath + subwindow.textBox.Text + "_" + ComboChooseClick.Text
                + "_" + subwindow.ButtonSize.Text + "_" + subwindow.CountNumber.Text + ".csv";
            //書き込みファイルをオープン
            WS = new System.IO.StreamWriter(fullPath, false, System.Text.Encoding.Default);

            S = Convert.ToInt32(subwindow.ButtonSize.Text);
            N = Convert.ToInt32(subwindow.CountNumber.Text);
            label.Content = N;

            button2.Height = S;
            button2.Width = S;
            timer2.Start();

            button1.Visibility = Visibility.Hidden;
            buttonstate = 0;

        }


        //button2を表示させる
        private void ButtonAppear()
        {
            int seed = Environment.TickCount;
            Random rnd = new Random(seed++);
            var x = rnd.Next(100, 1800);
            var y = rnd.Next(100, 900);

            Canvas.SetLeft(button2, x);
            Canvas.SetTop(button2, y);
            button2.Visibility = Visibility.Visible;

            time1 = Environment.TickCount;

            //buttonの中心座標
            var a = Canvas.GetLeft(button2);
            var b = Canvas.GetTop(button2);
            var r = button2.Height;
            centerx = a + r / 2;
            centery = b + r / 2;

            //ボタン表示時のポインタ位置とボタンの距離
            dbuttonx = centerx - pointerx;
            dbuttony = centery - pointery;
            lbuttonl = Math.Sqrt(dbuttonx * dbuttonx + dbuttony * dbuttony);


            //DrawClickPoint(S, centerx, centery);

            if (buttonstate != 2)
            {
                buttonstate = 2;
            }

        }



        void bodyFrameReader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            UpdateBodyFrame(e);
            DrawBodyFrame();
        }

        // ボディの更新
        private void UpdateBodyFrame(BodyFrameArrivedEventArgs e)
        {
            using (var bodyFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyFrame == null)
                {
                    return;
                }

                // ボディデータを取得する
                bodyFrame.GetAndRefreshBodyData(bodies);
            }
        }


        //Jointの表示
        public void DrawJointPosition(Body body)
        {
            if (body != null)
            {
                foreach (var joint in body.Joints)
                {
                    // 手の位置が追跡状態
                    if (joint.Value.TrackingState == TrackingState.Tracked)
                    {
                        // 右手を追跡していたら、手の状態を表示する
                        if (joint.Value.JointType == JointType.HandRight)
                        {
                            //3次元座標を取得
                            CameraSpacePoint hr = new CameraSpacePoint();
                            hr = joint.Value.Position;

                            hand.X = hr.X;
                            hand.Y = hr.Y;
                            hand.Z = hr.Z;

                            if (ComboChooseClick.Text == "gu")
                            {
                                GuStart();
                                CursorControl(body.Joints[JointType.HandRight],
                                              body.HandRightConfidence, body.HandRightState);
                            }
                        }
                        // 右人差し指を追跡していたら
                        else if (joint.Value.JointType == JointType.HandTipRight)
                        {
                            //3次元座標を取得
                            CameraSpacePoint htr = new CameraSpacePoint();
                            htr = joint.Value.Position;

                            tip.X = htr.X;
                            tip.Y = htr.Y;
                            tip.Z = htr.Z;

                            if (ComboChooseClick.Text == "depth")
                            {
                                CursorControl(body.Joints[JointType.HandTipRight],
                                              body.HandRightConfidence, body.HandRightState);
                            }
                        }

                    }
                    // 手の位置が推測状態
                    else if (joint.Value.TrackingState == TrackingState.Inferred)
                    {
                        //DrawEllipse(joint.Value, 10, Brushes.Yellow);
                    }
                }
            }
        }



        int nowx, nowy;
        double disx, disy;
        double vhandx, vhandy, vpointx, vpointy;
        double xpx, ypx, inchx, inchy;
        double prex, prey;
        int clickx, clicky;
        double nowz, prez;
        double depth, push;
        int pointerx = 960, pointery = 540;


        private void CursorControl(Joint joint, TrackingConfidence trackingConfidence, HandState handState)
        {
            //3次元座標を取得
            CameraSpacePoint h = new CameraSpacePoint();
            h = joint.Position;

            nowz = h.Z;

            //移動量計算
            MovePx(h.X, h.Y);

            //現在のカーソル位置取得
            //nowx = System.Windows.Forms.Cursor.Position.X;
            //nowy = System.Windows.Forms.Cursor.Position.Y;

            nowx = pointerx;
            nowy = pointery;


            //範囲内でカーソル位置を定義
            Area(nowx + (int)xpx, nowy - (int)ypx);

            //前フレームの座標として格納
            prex = h.X;
            prey = h.Y;

            //ポインタがボタン上にある
            ButtonSelect();


            if (ComboChooseClick.Text == "depth")
            {
                DepthClick();
            }
            else if (ComboChooseClick.Text == "gu")
            {
                GuClick(trackingConfidence, handState);
                CanvasGu.Children.Clear();
                if (clickstate == 1)
                {
                    DrawEllipse(pointerx, pointery, 30, new SolidColorBrush(new Color()
                    {
                        R = 255,
                        B = 255,
                        A = 128
                    }));
                }
            }

        }

        //グークリックロック状態
        private void DrawEllipse(double x, double y, int R, Brush brush)
        {
            var ellipse = new Ellipse()
            {
                Width = R,
                Height = R,
                StrokeThickness = 5,
                Stroke = brush,
                Opacity = 0.7,
            };

            //Color座標系で円を配置する
            Canvas.SetLeft(ellipse, x - R / 2);
            Canvas.SetTop(ellipse, y - R / 2);

            CanvasGu.Children.Add(ellipse);

        }

        //エリア制御
        private void Area(int x, int y)
        {
            if (x > 1920)
            {
                x = 1920;
            }
            else if (x < 0)
            {
                x = 0;
            }

            if (y > 1080)
            {
                y = 1080;
            }
            else if (y < 0)
            {
                y = 0;
            }

            //カーソル位置の再定義
            //System.Windows.Forms.Cursor.Position = new System.Drawing.Point(x, y);

            pointerx = x;
            pointery = y;
            CanvasPointer.Children.Clear();
            DrawPointer(20, x, y);

        }


        double[] P = { 0.9, 2.25, 10.86, 50 };
        double[] K = { 0.4, 1.0, 2.0, 4.0 };
        //double[] K = { 0.4, 1.0, 3.0, 5.0 };

        //速度変換
        private double SpeedChange(double s)
        {
            double S;

            //ポインタの速度を決定
            if (s < P[0])
            {
                S = s * K[0];
            }
            else if (s < P[1])
            {
                S = s * K[1];
            }
            else if (s < P[2])
            {
                S = s * K[2];
            }
            else if (s < P[3])
            {
                S = s * K[3];
            }
            else
            {
                S = 0;
            }

            //push操作中
            if (nowz < prez)
            {
                S = S / 1.5;

            }

            if (ComboChooseClick.Text == "gu")
            {
                if (flaggu == 1)
                {
                    S = S / 1;
                }

            }

            return S;

        }

        //絶対値
        private double PlusCheck(double p)
        {
            if (p < 0)
            {
                p = -1 * p;
            }

            return p;
        }

        //移動方向と距離の決定
        private void MovePx(double x, double y)
        {

            //手の速度（インチ/秒）
            disx = PlusCheck(x - prex);
            disy = PlusCheck(y - prey);

            vhandx = (disx / 0.0254) / 0.12;  //時間間隔は大体の値
            vhandy = (disy / 0.0254) / 0.12;

            //ポインタの速度へ変換
            vpointx = SpeedChange(vhandx);
            vpointy = SpeedChange(vhandy);

            //ポインタの移動量
            inchx = vpointx * 0.12;
            inchy = vpointy * 0.12;

            //インチをピクセル数へ
            //xpx = (1280 / (5 / Math.Sqrt(41) * 17)) * inchx;
            //ypx = (1080 / (4 / Math.Sqrt(41) * 17)) * inchy;

            //インチをピクセル数へ
            xpx = (1920 / (16 / Math.Sqrt(334) * 21.5)) * inchx;
            ypx = (1024 / (9 / Math.Sqrt(334) * 21.5)) * inchy;


            //移動方向の決定
            if (x - prex < 0)
            {
                xpx = -1 * xpx;
            }
            if (y - prey < 0)
            {
                ypx = -1 * ypx;
            }


        }




        //depthクリック
        private void DepthClick()
        {
            if (nowz < prez)
            {
                //更新しない
                if (depth == 0)
                {
                    depth = nowz;

                    time2 = Environment.TickCount;

                    clickx = pointerx;
                    clicky = pointery;

                }

            }
            else
            {
                //更新する
                if (depth != 0)
                {
                    push = depth - nowz;

                    if (0.1 < push && push < 0.2)
                    {
                        time3 = Environment.TickCount;

                        //カーソル位置の再定義
                        //System.Windows.Forms.Cursor.Position =
                        //new System.Drawing.Point(clickx, clicky);

                        //mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                        //mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);

                        DrawPointer(20, clickx, clicky);
                        ClickRecognized();
                    }

                    depth = 0;

                }

            }

            prez = nowz;

        }


        int clickstate = 0;

        //グークリック
        private void GuClick(TrackingConfidence trackingConfidence, HandState handState)
        {
            // 手の追跡信頼性が高い
            if (trackingConfidence != TrackingConfidence.High)
            {
                return;
            }

            // 手が開いている(パー)
            if (handState == HandState.Open)
            {
                if (clickstate == 1)
                {
                    clickstate = 0;
                }
            }
            // 手が閉じている(グー)
            else if (handState == HandState.Closed)
            {
                if (clickstate == 0)
                {
                    time3 = Environment.TickCount;

                    //カーソル位置の再定義
                    //System.Windows.Forms.Cursor.Position =
                         //new System.Drawing.Point(clickx, clicky);

                    //mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                    //mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);

                    DrawPointer(20, clickx, clicky);
                    ClickRecognized();

                    flaggu = 0;
                    clickstate = 1;

                }
            }
        }



        Point3D hand, tip;
        Vector3D ha_ti;
        double lha_til;
        double prevec;
        int flaggu = 0;
        double vecsize;

        //グークリック動作開始地点を求める
        private void GuStart()
        {
            //3次元ベクトル
            ha_ti = tip - hand;

            //手→指ベクトルの大きさ
            lha_til = Math.Sqrt((ha_ti.X) * (ha_ti.X) + (ha_ti.Y) * (ha_ti.Y) + (ha_ti.Z) * (ha_ti.Z));

            if (lha_til < prevec)
            {
                if (flaggu == 0)
                {
                    time2 = Environment.TickCount;

                    //現在のカーソル位置取得
                    clickx = pointerx;
                    clicky = pointery;

                    vecsize = lha_til;
                    flaggu = 1;
                }
            }
            else
            {
                //クリック動作認識中
                if (flaggu == 1)
                {
                    if (vecsize < lha_til)
                    {
                        flaggu = 0;
                    }
                    else if (vecsize - lha_til < 0.01)
                    {
                        flaggu = 0;
                    }
                }

            }

            prevec = lha_til;

        }


        Vector errord;
        double lerrordl;
        int error = 0;

        //クリックが認識された時の処理
        private void ClickRecognized()
        {
            //タイムリミット前にクリックされたとき
            if (buttonstate == 2)
            {
                button2.Visibility = Visibility.Hidden;
                buttonstate = 0;
                DrawClickPoint(30, clickx, clicky);
                timer3.Start();

                //クリック位置と目標位置の差を求める
                errord.X = clickx - centerx;
                errord.Y = clicky - centery;
                lerrordl = Math.Sqrt(errord.X * errord.X + errord.Y * errord.Y);

                //クリック位置がボタン内でないとき
                if (lerrordl > S / 2)
                {
                    error++;
                    //ファイルに反応時間を書き込む
                    WS.WriteLine("{0},{1},{2},{3},{4},{5}", lbuttonl, time2 - time1, time3 - time2, time3 - time1, lerrordl, 1);

                }
                //クリック位置がボタン内であるとき
                else
                {
                    count++;
                    //ファイルに反応時間を書き込む
                    WS.WriteLine("{0},{1},{2},{3},{4},{5}", lbuttonl, time2 - time1, time3 - time2, time3 - time1, lerrordl, 0);

                    var n = N - count;
                    label.Content = n;

                }                


                if (count == N)
                {
                    WS.WriteLine("{0},{1}", "error", error);
                    //ファイルを閉じる
                    WS.Close();
                    MessageBox.Show(fullPath + "に保存しました");

                    CanvasBody.Children.Clear();
                    button1.Visibility = Visibility.Visible;
                    buttonstate = 1;
                    count = 0;
                    error = 0;
                    label.Content = null;

                }
                else
                {
                    /*
                    if (subwindow.Experiment.Text == "間隔")
                    {
                        timer2.Start();
                    }
                    else
                    {
                        ButtonAppear();
                    }
                    */

                    ButtonAppear();

                }

            }
            else if (buttonstate == 1)
            {
                DrawClickPoint(30, clickx, clicky);
                timer3.Start();

                var a = Canvas.GetLeft(button1);
                var b = Canvas.GetTop(button1);
                var r = button1.Height;

                if ((a <= pointerx && pointerx <= a + r) && (b <= pointery && pointery <= b + r))
                {
                    //ファイル名
                    fullPath = workpath + subwindow.textBox.Text + "_" + ComboChooseClick.Text
                        + "_" + subwindow.ButtonSize.Text + "_" + subwindow.CountNumber.Text + ".csv";

                    //書き込みファイルをオープン
                    WS = new System.IO.StreamWriter(fullPath, false, System.Text.Encoding.Default);

                    S = Convert.ToInt32(subwindow.ButtonSize.Text);
                    N = Convert.ToInt32(subwindow.CountNumber.Text);
                    label.Content = N;

                    button2.Height = S;
                    button2.Width = S;
                    timer2.Start();

                    button1.Visibility = Visibility.Hidden;
                    buttonstate = 0;

                }

            }

        }


        //選択されているボタンの視覚効果
        private void ButtonSelect()
        {

            var x = pointerx;
            var y = pointery;

            var lx = Canvas.GetLeft(button2);
            var ly = Canvas.GetTop(button2);


            if ((lx <= x && x <= lx + S) && (ly <= y && y <= ly + S))
            {
                button2.Background = Brushes.GreenYellow;
            }
            else
            {
                button2.Background = Brushes.Green;

            }

        }


        //クリック位置のフィードバック
        private void DrawClickPoint(int R, double x, double y)
        {
            var ellipse = new Ellipse()
            {
                Width = R,
                Height = R,
                StrokeThickness = 3,
                Stroke = Brushes.Red,
            };
            Canvas.SetLeft(ellipse, x - R / 2);
            Canvas.SetTop(ellipse, y - R / 2);
            CanvasBody.Children.Add(ellipse);

            Line line = new Line()
            {
                //始点
                X1 = x,
                Y1 = y - R / 2,
                //終点
                X2 = x,
                Y2 = y + R / 2,
                //色
                Stroke = new SolidColorBrush(Colors.Red),
                //太さ
                StrokeThickness = 3,
            };
            CanvasBody.Children.Add(line);

            Line line2 = new Line()
            {
                //始点
                X1 = x - R / 2,
                Y1 = y,
                //終点
                X2 = x + R / 2,
                Y2 = y,
                //色
                Stroke = new SolidColorBrush(Colors.Red),
                //太さ
                StrokeThickness = 3,
            };
            CanvasBody.Children.Add(line2);

        }

        //ポインタの表示
        private void DrawPointer(int R, double x, double y)
        {
            var ellipse = new Ellipse()
            {
                Width = R,
                Height = R,
                StrokeThickness = 3,
                Stroke = Brushes.Black,
            };
            Canvas.SetLeft(ellipse, x - R / 2);
            Canvas.SetTop(ellipse, y - R / 2);
            CanvasPointer.Children.Add(ellipse);

            Line line = new Line()
            {
                //始点
                X1 = x,
                Y1 = y - R / 2,
                //終点
                X2 = x,
                Y2 = y + R / 2,
                //色
                Stroke = new SolidColorBrush(Colors.Black),
                //太さ
                StrokeThickness = 2,
            };
            CanvasPointer.Children.Add(line);

            Line line2 = new Line()
            {
                //始点
                X1 = x - R / 2,
                Y1 = y,
                //終点
                X2 = x + R / 2,
                Y2 = y,
                //色
                Stroke = new SolidColorBrush(Colors.Black),
                //太さ
                StrokeThickness = 2,
            };
            CanvasPointer.Children.Add(line2);

        }


        //1秒後にbuttonを表示
        void timer2_Tick(object sender, EventArgs e)
        {
            ButtonAppear();
            timer2.Stop();

        }


        void timer3_Tick(object sender, EventArgs e)
        {
            CanvasBody.Children.Clear();
            timer3.Stop();

        }











































        enum ChooseTrackingPlayerType
        {
            ClosestPlayer,
            FirstTrackedPlayer,
            BoundingBox_1p,
            BoundingBox_2p,
        }

        //左側のプレイヤー
        BoundingBox left = new BoundingBox()
        {
            Min = new Point3D(-1.5, 0, 1.5),
            Max = new Point3D(-0.5, 0, 2.0),
        };

        //中心のプレイヤー
        BoundingBox center = new BoundingBox()
        {
            Min = new Point3D(-0.5, 0, 1.3),
            Max = new Point3D(0.5, 0, 1.5),
        };

        //右側のプレイヤー
        BoundingBox right = new BoundingBox()
        {
            Min = new Point3D(0.5, 0, 1.5),
            Max = new Point3D(1.5, 0, 2.0),
        };

        //ボディの表示
        private void DrawBodyFrame()
        {

            //選択を列挙型に変換
            var type = (ChooseTrackingPlayerType)Enum.Parse(typeof(ChooseTrackingPlayerType),
                                                              ComboChoosePlayerType.SelectedItem as string);

            //選択ごとの処理
            switch (type)
            {
                case ChooseTrackingPlayerType.FirstTrackedPlayer:
                    FristTrackedPlayer();
                    break;
                case ChooseTrackingPlayerType.ClosestPlayer:
                    ClosestPlayer();
                    break;
                case ChooseTrackingPlayerType.BoundingBox_1p:
                    BoundingBox_1P();
                    break;
                case ChooseTrackingPlayerType.BoundingBox_2p:
                    BoundingBox_2P();
                    break;
            }

        }
        //最初に追跡した人
        private void FristTrackedPlayer()
        {
            //最初に追跡している人を有効にする
            var body = bodies.FirstOrDefault(b => b.IsTracked);

            DrawJointPosition(body);

        }


        //一番近くにいる人
        private void ClosestPlayer()
        {
            var body = ChooseClosestBody(bodies, new CameraSpacePoint());

            DrawJointPosition(body);

        }


        private Body ChooseClosestBody(Body[] bodies, CameraSpacePoint center = new CameraSpacePoint(),
                                                    float closestDistance = 2.0f)
        {
            Body closestBody = null;

            //比較する関節位置
            var baseType = JointType.SpineBase;

            //追跡しているBodyから選ぶ
            foreach (var body in bodies.Where(b => b.IsTracked))
            {
                //比較する関節位置が追跡状態になければ対象外
                if (body.Joints[baseType].TrackingState == TrackingState.NotTracked)
                {
                    continue;
                }

                //中心からの距離が近い人を選ぶ
                var distance = Distance(center, body.Joints[baseType].Position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestBody = body;
                }
            }

            return closestBody;

        }

        float Distance(CameraSpacePoint p1, CameraSpacePoint p2)
        {
            return (float)Math.Sqrt((p2.X - p1.X) * (p2.X - p1.X) +
                                    (p2.Y - p1.Y) * (p2.Y - p1.Y) +
                                    (p2.Z - p1.Z) * (p2.Z - p1.Z));
        }


        private void BoundingBox_1P()
        {
            //1人プレイ用の検出
            var body = ChooseBoundigBox(bodies, center);
            DrawJointPosition(body);


        }

        private void BoundingBox_2P()
        {
            //2人プレイ用の検出
            var body1 = ChooseBoundigBox(bodies, right);
            DrawJointPosition(body1);

            var body2 = ChooseBoundigBox(bodies, left);
            DrawJointPosition(body2);

        }


        private Body ChooseBoundigBox(Body[] bodies, BoundingBox boundingbox)
        {
            //中心位置にいる人を返す
            foreach (var body in bodies)
            {
                if (boundingbox.IsValidPosition(body))
                {
                    return body;
                }

            }

            return null;

        }

        public class BoundingBox
        {
            public Point3D Min;
            public Point3D Max;

            public BoundingBox()
            {
                Min = new Point3D();
                Max = new Point3D();
            }

            public bool IsValidPosition(Body body)
            {
                if (body == null)
                {
                    return false;
                }

                var baseType = JointType.SpineBase;
                if (body.Joints[baseType].TrackingState == TrackingState.NotTracked)
                {
                    return false;
                }

                var position = body.Joints[baseType].Position;
                return (Min.X <= position.X) && (position.X <= Max.X) &&
                       (Min.Z <= position.Z) && (position.Z <= Max.Z);


            }
        }
    }
}
