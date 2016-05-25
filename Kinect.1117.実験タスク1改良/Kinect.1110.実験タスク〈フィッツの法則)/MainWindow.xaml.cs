using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using Microsoft.Kinect;
using System.Runtime.InteropServices;
//using System.Windows.Forms;


namespace Kinect._1110.実験タスク_フィッツの法則_
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

        //const int R = 20;

        ColorFrameReader colorFrameReader;
        FrameDescription colorFrameDesc;

        ColorImageFormat colorFormat = ColorImageFormat.Bgra;

        // WPF
        WriteableBitmap colorBitmap;
        byte[] colorBuffer;
        int colorStride;
        Int32Rect colorRect;



        //マウス操作の関数宣言
        [DllImport("USER32.dll", CallingConvention = CallingConvention.StdCall)]
        static extern void SetCursorPos(int X, int Y);

        [DllImport("USER32.dll", CallingConvention = CallingConvention.StdCall)]
        static extern void mouse_event(int dFlags, int dx, int dy, int cButtons, int dwExtraInfo);

        private const int MOUSEEVENTF_LEFTDOWN = 0x2;
        private const int MOUSEEVENTF_LEFTUP = 0x4;



        Window1 subwindow = new Window1();

        //出力ファイルを格納するフォルダ
        public string workpath = "c:\\予備実験2\\";
        //出力ファイルのフルパスを格納する変数
        public string fullPath = "";
        //WSという名のファイルストリームの宣言
        public System.IO.StreamWriter WS;


        int start = 0;
        double time1, time2, time3, time4, time5;
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


            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
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

                firstclick = 0;


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

            time5 = Environment.TickCount;
            //ファイルに反応時間を書き込む
            WS.WriteLine("{0} {1} {2} {3}", time2 - time1, time3 - time2, time4 - time3, time5 - time4);

            if (count == 30)
            {
                //ファイルを閉じる
                WS.Close();
                MessageBox.Show(fullPath + "に保存しました");

                button2.Visibility = Visibility.Hidden;
                button1.Content = "OK";
                button1.IsEnabled = true;
                count = 0;
                start = 0;

            }
            else
            {
                //初期化
                button2.Visibility = Visibility.Hidden;
                button1.IsEnabled = true;

            }

            enter = 0;


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
                        //DrawEllipse(joint.Value, 10, Brushes.Blue);

                        // 左手を追跡していたら、手の状態を表示する
                        if (joint.Value.JointType == JointType.HandLeft)
                        {
                            //DrawHandState(body.Joints[JointType.HandLeft],
                            //body.HandLeftConfidence, body.HandLeftState);
                        }
                        // 右手を追跡していたら、手の状態を表示する
                        else if (joint.Value.JointType == JointType.HandRight)
                        {
                            //DrawHandState(body.Joints[JointType.HandRight],
                            //body.HandRightConfidence, body.HandRightState);
                        }
                        // 右人差し指を追跡していたら
                        else if (joint.Value.JointType == JointType.HandTipRight)
                        {
                            if (ComboChooseClick.Text != "mouse")
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



        //座標格納
        int nowx, nowy;
        double disx, disy;
        double vhandx, vhandy, vpointx, vpointy;
        double xpx, ypx, inchx, inchy;
        double prex, prey;
        int clickx, clicky;
        double nowz, prez;
        double depth, push;



        private void CursorControl(Joint joint, TrackingConfidence trackingConfidence, HandState handState)
        {
            //3次元座標を取得
            CameraSpacePoint h = new CameraSpacePoint();
            h = joint.Position;

            nowz = h.Z;

            MovePx(h.X, h.Y);

            //現在のカーソル位置取得
            nowx = System.Windows.Forms.Cursor.Position.X;
            nowy = System.Windows.Forms.Cursor.Position.Y;

            //カーソル位置の再定義
            System.Windows.Forms.Cursor.Position =
                 new System.Drawing.Point(nowx + (int)xpx, nowy - (int)ypx);

            //前フレームの座標として格納
            prex = h.X;
            prey = h.Y;


            if (ComboChooseClick.Text == "depth")
            {
                //クリック判定
                DepthClick();
            }
            else if (ComboChooseClick.Text == "gu")
            {
                GuClick(trackingConfidence, handState);

            }



        }

        double[] P = { 0.9, 2.25, 10.86, 50 };
        double[] K = { 0.4, 1.0, 2.0, 4.0 };

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
                int a = 2;

                S = S / a;

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
            xpx = (1280 / (5 / Math.Sqrt(41) * 17)) * inchx;
            ypx = (1024 / (4 / Math.Sqrt(41) * 17)) * inchy;

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

        int firstclick = 0;

        //クリック判定
        private void DepthClick()
        {
            if (nowz < prez)
            {
                //更新しない
                if (depth == 0)
                {
                    depth = nowz;

                    if (firstclick == 0)
                    {
                        time3 = Environment.TickCount;
                    }

                    time4 = Environment.TickCount;

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
                        //カーソル位置の再定義
                        System.Windows.Forms.Cursor.Position =
                             new System.Drawing.Point(clickx, clicky);

                        mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                        mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);

                        if (firstclick == 0)
                        {
                            firstclick = 1;
                        }

                    }

                    depth = 0;

                }

                //clickx = cx;
                //clicky = cy;

                clickx = System.Windows.Forms.Cursor.Position.X;
                clicky = System.Windows.Forms.Cursor.Position.Y;

            }

            prez = nowz;

        }

        int clickstate = 0;

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
                    clickstate = 1;

                    mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                    mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                }
            }


        }

        // ボディの表示
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
