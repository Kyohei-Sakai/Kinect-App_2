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

        ColorFrameReader colorFrameReader;
        FrameDescription colorFrameDesc;

        ColorImageFormat colorFormat = ColorImageFormat.Bgra;

        // WPF
        WriteableBitmap colorBitmap;
        byte[] colorBuffer;
        int colorStride;
        Int32Rect colorRect;

        double rate = 1;

        //マウス操作の関数宣言
        [DllImport("USER32.dll", CallingConvention = CallingConvention.StdCall)]
        static extern void SetCursorPos(int X, int Y);

        [DllImport("USER32.dll", CallingConvention = CallingConvention.StdCall)]
        static extern void mouse_event(int dFlags, int dx, int dy, int cButtons, int dwExtraInfo);

        private const int MOUSEEVENTF_LEFTDOWN = 0x2;
        private const int MOUSEEVENTF_LEFTUP = 0x4;


        Window1 subwindow = new Window1();

        //出力ファイルを格納するフォルダ
        public string workpath = "c:\\予備実験5\\";
        //出力ファイルのフルパスを格納する変数
        public string fullPath = "";
        //WSという名のファイルストリームの宣言
        public System.IO.StreamWriter WS;

        DispatcherTimer timer, timer2, timer3;






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

                // カラー画像の情報を作成する(BGRAフォーマット)
                colorFrameDesc = kinect.ColorFrameSource.CreateFrameDescription(
                                                        colorFormat);

                // カラーリーダーを開く
                colorFrameReader = kinect.ColorFrameSource.OpenReader();
                colorFrameReader.FrameArrived += colorFrameReader_FrameArrived;

                // カラー用のビットマップを作成する
                colorBitmap = new WriteableBitmap(
                                    colorFrameDesc.Width, colorFrameDesc.Height,
                                    96, 96, PixelFormats.Bgra32, null);
                colorStride = colorFrameDesc.Width * (int)colorFrameDesc.BytesPerPixel;
                colorRect = new Int32Rect(0, 0,
                                    colorFrameDesc.Width, colorFrameDesc.Height);
                colorBuffer = new byte[colorStride * colorFrameDesc.Height];
                ImageColor.Source = colorBitmap;

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
                label.Background = Brushes.White;

                //タイマー設定
                timer = new DispatcherTimer();
                timer.Tick += timer_Tick;
                timer.Interval = new TimeSpan(0, 0, 1);

                timer2 = new DispatcherTimer();
                timer2.Tick += timer2_Tick;
                timer2.Interval = new TimeSpan(0, 0, 1);

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

            if (colorFrameReader != null)
            {
                colorFrameReader.Dispose();
                colorFrameReader = null;
            }

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


















        int start = 0;
        int buttonstate = 1;
        double centerx, centery;
        double time1, time2;
        int count = 0;
        int S;


        private void button1_Click(object sender, RoutedEventArgs e)
        {
            if (start == 0)
            {
                //ファイル名
                fullPath = workpath + subwindow.textBox.Text + "_" + subwindow.Experiment.Text +
                     "_" + ComboChooseClick.Text + "_" + subwindow.ButtonSize.Text + "_" + subwindow.TimeLimit.Text + ".csv";
                //書き込みファイルをオープン
                WS = new System.IO.StreamWriter(fullPath, false, System.Text.Encoding.Default);

                T = Convert.ToInt32(subwindow.TimeLimit.Text);
                S = Convert.ToInt32(subwindow.ButtonSize.Text);

                button2.Height = S;
                button2.Width = S;

                timer2.Start();
                start = 1;
                button1.Visibility = Visibility.Hidden;
            }

        }


        //button2を表示させる
        private void ButtonAppear()
        {
            int seed = Environment.TickCount;
            Random rnd = new Random(seed++);
            var x = rnd.Next(1050, 1400);
            var y = rnd.Next(400, 850);

            Canvas.SetLeft(button2, x);
            Canvas.SetTop(button2, y);
            button2.Visibility = Visibility.Visible;

            time1 = Environment.TickCount;
            timer.Start();

            //buttonの中心座標
            var a = Canvas.GetLeft(button2);
            var b = Canvas.GetTop(button2);
            var r = button2.Height;
            centerx = a + r / 2;
            centery = b + r / 2;

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

        void colorFrameReader_FrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            UpdateColorFrame(e);
            DrawColorFrame();
        }

        private void UpdateColorFrame(ColorFrameArrivedEventArgs e)
        {
            // カラーフレームを取得する
            using (var colorFrame = e.FrameReference.AcquireFrame())
            {
                if (colorFrame == null)
                {
                    return;
                }

                // BGRAデータを取得する
                colorFrame.CopyConvertedFrameDataToArray(
                                            colorBuffer, colorFormat);
            }
        }

        private void DrawColorFrame()
        {
            // ビットマップにする
            colorBitmap.WritePixels(colorRect, colorBuffer,
                                            colorStride, 0);
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
                                DrawEllipse(joint.Value, 20, Brushes.Red);
                                ButtonSelect(joint.Value);
                                GuStart(joint.Value);
                                GuClick(body.HandRightConfidence, body.HandRightState);
                                if (clickstate == 1)
                                {
                                    DrawEllipse(joint.Value, 20, Brushes.White);
                                }

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
                                DrawEllipse(joint.Value, 20, Brushes.Red);
                                ButtonSelect(joint.Value);
                                DepthClick(joint.Value);
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



        private void DrawEllipse(Joint joint, int R, Brush brush)
        {
            var ellipse = new Ellipse()
            {
                Width = R,
                Height = R,
                Fill = brush,
            };

            // カメラ座標系をカラー座標系に変換する
            var point = kinect.CoordinateMapper.MapCameraPointToColorSpace(joint.Position);
            if ((point.X < 0) || (point.Y < 0))
            {
                return;
            }

            //Color座標系で円を配置する
            Canvas.SetLeft(ellipse, point.X * rate - (R / 2));
            Canvas.SetTop(ellipse, point.Y * rate - (R / 2));

            CanvasBody.Children.Add(ellipse);

        }



        double nowz, prez;
        double depth, push;
        double clickX, clickY;


        //depthクリック
        private void DepthClick(Joint joint)
        {
            // カメラ座標系をカラー座標系に変換する
            var point = kinect.CoordinateMapper.MapCameraPointToColorSpace(joint.Position);
            if ((point.X < 0) || (point.Y < 0))
            {
                return;
            }

            //3次元座標を取得
            CameraSpacePoint h = new CameraSpacePoint();
            h = joint.Position;

            nowz = h.Z;

            if (nowz < prez)
            {
                //更新しない
                if (depth == 0)
                {
                    depth = nowz;

                    clickX = point.X * (float)rate;
                    clickY = point.Y * (float)rate;
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
                        time2 = Environment.TickCount;

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
                    time2 = Environment.TickCount;

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
        private void GuStart(Joint joint)
        {
            // カメラ座標系をカラー座標系に変換する
            var point = kinect.CoordinateMapper.MapCameraPointToColorSpace(joint.Position);
            if ((point.X < 0) || (point.Y < 0))
            {
                return;
            }

            //3次元ベクトル
            ha_ti = tip - hand;

            //手→指ベクトルの大きさ
            lha_til = Math.Sqrt((ha_ti.X) * (ha_ti.X) + (ha_ti.Y) * (ha_ti.Y) + (ha_ti.Z) * (ha_ti.Z));

            if (lha_til < prevec)
            {
                if (flaggu == 0)
                {
                    //現在のJoint位置取得
                    clickX = point.X * rate;
                    clickY = point.Y * rate;

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
        int clickFinished = 0;

        //クリックが認識された時の処理
        private void ClickRecognized()
        {
            //タイムリミット前にクリックされたとき
            if (buttonstate == 2)
            {
                button2.Visibility = Visibility.Hidden;
                buttonstate = 3;
                count++;
                DrawClickPoint(30, clickX, clickY);
                timer3.Start();
                timer.Stop();
                labelTime.Content = null;

                //クリック位置と目標位置の差を求める
                errord.X = clickX - centerx;
                errord.Y = clickY - centery;
                lerrordl = Math.Sqrt(errord.X * errord.X + errord.Y * errord.Y);

                //ファイルに反応時間を書き込む
                WS.WriteLine("{0},{1},{2}", time2 - time1, lerrordl, T * 1000 - (time2 - time1));


                if (count == 30)
                {
                    //ファイルにエラー回数を記録
                    WS.WriteLine("error,{0}", error);
                    //ファイルを閉じる
                    WS.Close();
                    MessageBox.Show(fullPath + "に保存しました");

                    CanvasBody.Children.Clear();
                    button1.Visibility = Visibility.Visible;
                    buttonstate = 1;
                    count = 0;
                    start = 0;                    

                }
                else
                {
                    clickFinished = 1;
                    timer2.Start();

                }

            }
            //タイムリミット後にクリックされたとき
            else if (buttonstate == 0)
            {
                if (subwindow.Experiment.Text == "実験②")
                {
                    count++;
                    DrawClickPoint(30, clickX, clickY);
                    timer3.Start();

                    //クリック位置と目標位置の差を求める
                    errord.X = clickX - centerx;
                    errord.Y = clickY - centery;
                    lerrordl = Math.Sqrt(errord.X * errord.X + errord.Y * errord.Y);

                    //ファイルに反応時間を書き込む
                    WS.WriteLine("{0},{1},{2}", time2 - time1, lerrordl, T * 1000 - (time2 - time1));

                    if (count == 30)
                    {
                        timer2.Stop();

                        //ファイルにエラー回数を記録
                        WS.WriteLine("error,{0}", error);
                        //ファイルを閉じる
                        WS.Close();
                        MessageBox.Show(fullPath + "に保存しました");

                        CanvasBody.Children.Clear();
                        button1.Visibility = Visibility.Visible;
                        buttonstate = 1;
                        count = 0;
                        start = 0;

                    }
                    else
                    {
                        //複数回のクリック認証を防ぐため
                        buttonstate = 3;
                        //エラーを分類するため
                        clickFinished = 1;

                    }
                }

            }


        }


        //選択されているボタンの視覚効果
        private void ButtonSelect(Joint joint)
        {
            // カメラ座標系をカラー座標系に変換する
            var point = kinect.CoordinateMapper.MapCameraPointToColorSpace(joint.Position);
            if ((point.X < 0) || (point.Y < 0))
            {
                return;
            }

            var x = point.X * rate;
            var y = point.Y * rate;

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

        //クリック位置を表示
        private void DrawClickPoint(int R, double x, double y)
        {
            var ellipse = new Ellipse()
            {
                Width = R,
                Height = R,
                StrokeThickness = 4,
                Stroke = Brushes.Red,
            };
            Canvas.SetLeft(ellipse, x - R / 2);
            Canvas.SetTop(ellipse, y - R / 2);
            CanvasPoint.Children.Add(ellipse);

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
                StrokeThickness = 4,
            };
            CanvasPoint.Children.Add(line);

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
                StrokeThickness = 4,
            };
            CanvasPoint.Children.Add(line2);
        }




        int countdown;
        int T;
        int error = 0;

        //タイマー処理
        void timer_Tick(object sender, EventArgs e)
        {
            countdown--;

            if (countdown == 1)
            {
                labelTime.Foreground = Brushes.Red;
            }
            else
            {
                labelTime.Foreground = Brushes.Black;
            }

            labelTime.Content = countdown;

            //時間切れ
            if (countdown == 0)
            {
                timer.Stop();
                timer2.Start();

                labelTime.Content = null;
                button2.Visibility = Visibility.Hidden;
                buttonstate = 0;

                if (subwindow.Experiment.Text == "実験①")
                {
                    error++;
                }
                else if (subwindow.Experiment.Text == "実験②")
                {
                    clickFinished = 1;
                }
            }

        }

        //1秒後にbuttonを表示
        void timer2_Tick(object sender, EventArgs e)
        {
            if (subwindow.Experiment.Text == "実験②")
            {
                int seed = Environment.TickCount;
                Random rnd = new Random(seed++);
                countdown = rnd.Next(3,5);
            }
            else
            {
                countdown = T;
            }

            labelTime.Foreground = Brushes.Black;
            labelTime.Content = countdown;

            if (subwindow.Experiment.Text == "実験②")
            {
                if (clickFinished == 0)
                {
                    error++;
                }

                clickFinished = 0;
            }

            ButtonAppear();
            timer2.Stop();
        }

        
        void timer3_Tick(object sender, EventArgs e)
        {
            CanvasPoint.Children.Clear();
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
            Min = new Point3D(-0.5, 0, 1.5),
            Max = new Point3D(0.5, 0, 2.0),
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
            CanvasBody.Children.Clear();

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
