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
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;
using Microsoft.Kinect;
using System.Runtime.InteropServices;
using System.Collections;


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


        //出力ファイルを格納するフォルダ
        public string workpath = "c:\\予備実験3\\";
        //出力ファイルのフルパスを格納する変数
        public string fullPath = "";
        //WSという名のファイルストリームの宣言
        public System.IO.StreamWriter WS;


        int start = 0;
        double time1, time2, time3;
        int count = 0;

        Window1 subwindow = new Window1();

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
                button1.Background = Brushes.Yellow;
                button2.Background = Brushes.Yellow;

            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
                Close();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
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
                                DrawEllipse(joint.Value, 10, Brushes.Red);
                            }
                            else
                            {
                                DrawEllipse(joint.Value, 10, Brushes.Red);
                                ButtonSelect(body.Joints[JointType.HandTipRight]);
                                DepthClick2(body.Joints[JointType.HandTipRight]);
                            }
                        }
                        //首の付け根
                        else if (joint.Value.JointType == JointType.SpineShoulder)
                        {
                            //DrawEllipse(joint.Value, 10, Brushes.Green);
                            DecisionButtonLocation(body.Joints[JointType.SpineShoulder]);
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
        
        private void DecisionButtonLocation(Joint joint)
        {
            //3次元座標を取得
            CameraSpacePoint ss = new CameraSpacePoint();
            ss = joint.Position;

            var ssx = ss.X;
            var ssy = ss.Y;
            var ssz = ss.Z;

            Joint a = new Joint();

            a.Position.X = ssx + (float)0.3;
            a.Position.Y = ssy + (float)0.0;
            a.Position.Z = ssz + (float)0.4;

            //DrawEllipse(a, 10, Brushes.Red);
            
            Joint b = new Joint();

            b.Position.X = a.Position.X + (float)0.2;
            b.Position.Y = a.Position.Y - (float)0.2;
            b.Position.Z = a.Position.Z;

            //DrawEllipse(b, 10, Brushes.Red);

            DrawButton(a,b);

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



        private  void CursorControl(Joint joint,TrackingConfidence trackingConfidence, HandState handState)
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


        //クリック判定
        private void DepthClick()
        {
            if (nowz < prez)
            {
                //更新しない
                if (depth == 0)
                {
                    depth = nowz;

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


        private void DrawHandState(Joint joint,
            TrackingConfidence trackingConfidence, HandState handState)
        {
            // 手の追跡信頼性が高い
            if (trackingConfidence != TrackingConfidence.High)
            {
                return;
            }

            // 手が開いている(パー)
            if (handState == HandState.Open)
            {
                DrawEllipse(joint, 40, new SolidColorBrush(new Color()
                {
                    R = 255,
                    G = 255,
                    A = 128
                }));
            }
            // チョキのような感じ
            else if (handState == HandState.Lasso)
            {
                DrawEllipse(joint, 40, new SolidColorBrush(new Color()
                {
                    R = 255,
                    B = 255,
                    A = 128
                }));
            }
            // 手が閉じている(グー)
            else if (handState == HandState.Closed)
            {
                DrawEllipse(joint, 40, new SolidColorBrush(new Color()
                {
                    G = 255,
                    B = 255,
                    A = 128
                }));
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
            Canvas.SetLeft(ellipse, point.X / 2 - (R / 2));
            Canvas.SetTop(ellipse, point.Y / 2 - (R / 2));

            CanvasBody.Children.Add(ellipse);

        }


        //距離
        int D = 200;
        int nowbutton = 1;
        double v1, v2;


        //ボタン2の位置決め
        private void RandomTarget()
        {
            //距離
            D = Convert.ToInt32(subwindow.Distance.Text);

            //中心座標
            var a = Canvas.GetLeft(button1);
            var b = Canvas.GetTop(button1);

            //ボタンの位置決め
            //x座標をランダムで決める
            int seed = Environment.TickCount;
            Random rnd = new Random(seed++);
            var x = rnd.Next((int)a - D, (int)a + D + 1);

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
            var y_b = d * Math.Sqrt(D * D - (x - a) * (x - a));
            var y = b + y_b;

            //ボタンの移動距離
            v1 = x - a;
            v2 = y - b;

        }

        int buttonstate = 0;
        int enter = 0;

        //選択されているボタンサイズを変える
        private void ButtonSelect(Joint joint)
        {
            // カメラ座標系をカラー座標系に変換する
            var point = kinect.CoordinateMapper.MapCameraPointToColorSpace(joint.Position);
            if ((point.X < 0) || (point.Y < 0))
            {
                return;
            }

            var x = point.X / 2;
            var y = point.Y / 2;

            var lx = locx;
            var ly = locy;

            if (nowbutton == 2)
            {
                lx = loc2x;
                ly = loc2y;
            }

            if ((lx <= x && x <= lx + W) && (ly <= y && y <= ly + W))
            {
                R = W * 1.2;
                buttonstate = 1;

                if (nowbutton == 2 && enter == 0)
                {
                    time2 = Environment.TickCount;
                    enter = 1;
                }

            }
            else
            {
                buttonstate = 0;
            }


        }

        double locx, locy;
        double loc2x, loc2y;
        //大きさ
        double W = 30, R;
        int end = 0;

        //ボタンの表示
        private void DrawButton(Joint joint,Joint joint2)
        {
            CanvasButton.Children.Clear();

            // カメラ座標系をカラー座標系に変換する
            var point = kinect.CoordinateMapper.MapCameraPointToColorSpace(joint.Position);
            if ((point.X < 0) || (point.Y < 0))
            {
                return;
            }

            //button1の位置
            locx = point.X / 2;
            locy = point.Y / 2;


            if (nowbutton == 1)
            {
                if (buttonstate == 0)
                {
                    button1.Height = W;
                    button1.Width = W;
                    Canvas.SetLeft(button1, locx);
                    Canvas.SetTop(button1, locy);
                }
                else
                {
                    button1.Height = R;
                    button1.Width = R;
                    Canvas.SetLeft(button1, locx - (R - W) / 2);
                    Canvas.SetTop(button1, locy - (R - W) / 2);
                }

                CanvasButton.Children.Add(button1);

            }
            else
            {
                loc2x = locx + v1;
                loc2y = locy + v2;

                if (buttonstate == 0)
                {
                    button2.Height = W;
                    button2.Width = W;
                    Canvas.SetLeft(button2, loc2x);
                    Canvas.SetTop(button2, loc2y);
                }
                else
                {
                    button2.Height = R;
                    button2.Width = R;
                    Canvas.SetLeft(button2, loc2x - (R - W) / 2);
                    Canvas.SetTop(button2, loc2y - (R - W) / 2);
                }

                CanvasButton.Children.Add(button2);

            }

            if (end == 1)
            {
                //長方形
                Rectangle Rect = new Rectangle()
                {
                    Width = 450,
                    Height = 30,
                    Fill = Brushes.White,
                };
                Canvas.SetLeft(Rect, 300);
                Canvas.SetTop(Rect, 300);
                CanvasButton.Children.Add(Rect);

                var text = new TextBlock()
                {
                    Text = string.Format(fullPath + "に保存しました"),
                    FontSize = 20,
                    Foreground = Brushes.Red,
                };
                Canvas.SetLeft(text, 300);
                Canvas.SetTop(text, 300);
                CanvasButton.Children.Add(text);



            }

        }

        float clickX, clickY;
        

        //クリック判定
        private void DepthClick2(Joint joint)
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

                    clickX = point.X / 2;
                    clickY = point.Y / 2;
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
                        DrawClickPoint(40, clickX, clickY);

                        if (nowbutton == 1)
                        {
                            //クリックした位置がボタン1
                            if ((locx <= clickX && clickX <= locx + W) && (locy <= clickY && clickY <= locy + W))
                            {
                                if (start == 0)
                                {
                                    //ファイル名
                                    fullPath = workpath + subwindow.textBox.Text + "_" + subwindow.Control.Text + "_"
                                                        + subwindow.ButtonSize.Text + "_" + subwindow.Distance.Text + ".csv";
                                    //書き込みファイルをオープン
                                    WS = new System.IO.StreamWriter(fullPath, false, System.Text.Encoding.Default);

                                    W = Convert.ToInt32(subwindow.ButtonSize.Text);

                                    start = 1;
                                    end = 0;

                                }
                                else
                                {
                                    time1 = Environment.TickCount;
                                    CanvasButton.Children.Clear();
                                    RandomTarget();
                                    nowbutton = 2;
                                }

                            }

                        }
                        else
                        {
                            //クリックした位置がボタン2
                            if ((loc2x <= clickX && clickX <= loc2x + W) && (loc2y <= clickY && clickY <= loc2y + W))
                            {
                                if (count == 30)
                                {
                                    //ファイルを閉じる
                                    WS.Close();

                                    end = 1;
                                    count = 0;
                                    enter = 0;
                                    start = 0;

                                }
                                else
                                {
                                    count++;

                                    time3 = Environment.TickCount;
                                    //ファイルに反応時間を書き込む
                                    WS.WriteLine("{0} {1}", time2 - time1, time3 - time2);
                                    enter = 0;
                                }

                                CanvasButton.Children.Clear();
                                nowbutton = 1;

                            }


                        }

                    }

                    depth = 0;

                }
            }

            prez = nowz;

        }

        private void DrawClickPoint(int R, float x, float y)
        {
            var ellipse = new Ellipse()
            {
                Width = R,
                Height = R,
                StrokeThickness = 5,
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
                StrokeThickness = 5,
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
                StrokeThickness = 5,
            };
            CanvasBody.Children.Add(line2);



        }



    }
}
