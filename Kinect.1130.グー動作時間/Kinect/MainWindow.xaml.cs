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

        // 表示
        WriteableBitmap depthImage;
        ushort[] depthBuffer;
        byte[] depthBitmapBuffer;
        Int32Rect depthRect;
        int depthStride;

        Point depthPoint;
        const int R = 20;


        //マウス操作の関数宣言
        [DllImport("USER32.dll", CallingConvention = CallingConvention.StdCall)]
        static extern void SetCursorPos(int X, int Y);

        [DllImport("USER32.dll", CallingConvention = CallingConvention.StdCall)]
        static extern void mouse_event(int dFlags, int dx, int dy, int cButtons, int dwExtraInfo);

        private const int MOUSEEVENTF_LEFTDOWN = 0x2;
        private const int MOUSEEVENTF_LEFTUP = 0x4;

        //出力ファイルを格納するフォルダ
        string workPath = "c:\\gutime\\";
        //出力ファイルのフルパスを格納する変数
        string fullPath = "";
        //WSという名のファイルストリームの宣言
        System.IO.StreamWriter WS;



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

                // 表示のためのビットマップに必要なものを作成
                depthImage = new WriteableBitmap(
                    depthFrameDesc.Width, depthFrameDesc.Height,
                    96, 96, PixelFormats.Gray8, null);
                depthBuffer = new ushort[depthFrameDesc.LengthInPixels];
                depthBitmapBuffer = new byte[depthFrameDesc.LengthInPixels];
                depthRect = new Int32Rect(0, 0,
                                        depthFrameDesc.Width, depthFrameDesc.Height);
                depthStride = (int)(depthFrameDesc.Width);

                ImageDepth.Source = depthImage;

                // 初期の位置表示座標(中心点)
                depthPoint = new Point(depthFrameDesc.Width / 2,
                                        depthFrameDesc.Height / 2);

                // Depthリーダーを開く
                depthFrameReader = kinect.DepthFrameSource.OpenReader();
                depthFrameReader.FrameArrived += depthFrameReader_FrameArrived;

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

                //ファイル名を被験者名_刺激間隔_刺激頻度_数字呈示有無.txtにする．
                fullPath = workPath + "gutime2" + ".csv";
                //書き込みファイルをオープンする．第２引数がfalseで上書モード，trueで追記モード
                WS = new System.IO.StreamWriter(fullPath, false, System.Text.Encoding.Default);


            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
                Close();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (WS != null)
            {
                //ファイルをクローズする．
                WS.Close();
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

        void depthFrameReader_FrameArrived(object sender, DepthFrameArrivedEventArgs e)
        {
            UpdateDepthFrame(e);
            DrawDepthFrame();
        }

        // Depthフレームの更新
        private void UpdateDepthFrame(DepthFrameArrivedEventArgs e)
        {
            using (var depthFrame = e.FrameReference.AcquireFrame())
            {
                if (depthFrame == null)
                {
                    return;
                }

                // Depthデータを取得する
                depthFrame.CopyFrameDataToArray(depthBuffer);
            }
        }

        // Depthフレームの表示
        private void DrawDepthFrame()
        {
            // 0-8000のデータを255ごとに折り返すようにする(見やすく)
            for (int i = 0; i < depthBuffer.Length; i++)
            {
                depthBitmapBuffer[i] = (byte)(depthBuffer[i] % 255);
            }

            depthImage.WritePixels(depthRect, depthBitmapBuffer, depthStride, 0);
        }


        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            depthPoint = e.GetPosition(this);
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
            FirstTrackedPlayer,
            ClosestPlayer,
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

        Point3D hand, tip;
        Vector3D ha_ti;
        double lha_til;
        double prevec;
        double time1, time2;
        int flaggu = 0;
        double vecsize;

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
                            DrawHandState(body.Joints[JointType.HandLeft],
                                body.HandLeftConfidence, body.HandLeftState);
                        }
                        // 右手を追跡していたら、手の状態を表示する
                        else if (joint.Value.JointType == JointType.HandRight)
                        {
                            DrawHandState(body.Joints[JointType.HandRight],
                                body.HandRightConfidence, body.HandRightState);

                            //3次元座標を取得
                            CameraSpacePoint hr = new CameraSpacePoint();
                            hr = joint.Value.Position;

                            hand.X = hr.X;
                            hand.Y = hr.Y;
                            hand.Z = hr.Z;

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

                            /*
                            if (ComboChooseClick.Text != "mouse")
                            {
                                CursorControl(body.Joints[JointType.HandTipRight],
                                              body.HandRightConfidence, body.HandRightState);
                                GuStart();
                                DrawEllipse(joint.Value, 10, Brushes.Red);
                            }
                            */
                        }
                    }
                    // 手の位置が推測状態
                    else if (joint.Value.TrackingState == TrackingState.Inferred)
                    {
                        DrawEllipse(joint.Value, 10, Brushes.Yellow);
                    }

                }

                if (ComboChooseClick.Text == "gu")
                {
                    GuClick(body.HandRightConfidence, body.HandRightState);
                    GuStart();
                }

            }
        }
        

        //グークリック動作開始時刻を求める
        private void GuStart()
        {
            //3次元ベクトル
            ha_ti = tip - hand;

            //手→指ベクトルの大きさ
            lha_til = Math.Sqrt((ha_ti.X) * (ha_ti.X) + (ha_ti.Y) * (ha_ti.Y) + (ha_ti.Z) * (ha_ti.Z));
            WS.WriteLine(lha_til);

            if (lha_til < prevec)
            {
                if (flaggu == 0)
                {
                    time1 = Environment.TickCount;
                    vecsize = lha_til;
                    flaggu = 1;
                }
            }
            else
            {
                flaggu = 0;
                //クリック動作認識中
                if (flaggu == 1)
                {
                    if (vecsize > lha_til)
                    {
                        flaggu = 1;
                    }

                }
            }

            prevec = lha_til;

        }

        
        //座標格納
        int cx, cy;
        int nowx, nowy;
        double k, t, j, r;
        double vhandx, vhandy, vpointx, vpointy;
        double xpx, ypx, inchx, inchy;

        int clickx, clicky;
        double nowz, prez;
        double depth, push;



        private void CursorControl(Joint joint,TrackingConfidence trackingConfidence, HandState handState)
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

            //ポインタの位置決定
            cx = nowx + (int)xpx;
            cy = nowy - (int)ypx;

            //ポインタの位置決定
            //cx = cx + (int)xpx;
            //cy = cy - (int)ypx;

            //ポインタの移動範囲の制御
            //Area(cx, cy);

            //ポインタを描く
            //DrawCursor(20, cx, cy);

            //前フレームの座標として格納
            k = h.X;
            t = h.Y;


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


        //速度変換
        private double SpeedChange(double s)
        {
            double S;

            if (nowz < prez)
            {
                int b = 2;

                //push操作中
                if (s < 0.9)
                {
                    S = s * 0.4 / b;
                }
                else if (s < 2.25)
                {
                    S = s * 1.0 / 2;
                }
                else if (s < 10.86)
                {
                    S = s * 2.0 / 2;
                }
                else if (s < 50)
                {
                    S = s * 4.00 / 2;
                }
                else
                {
                    S = 0;
                }
            }
            else
            {
                //ポインタの速度を決定
                if (s < 0.9)
                {
                    //S = s * 0.4;
                    S = s * slider1.Value;
                    label1.Content = slider1.Value;
                }
                else if (s < 2.25)
                {
                    //S = s * 1.0;
                    S = s * slider2.Value;
                    label2.Content = slider2.Value;
                }
                else if (s < 10.86)
                {
                    //S = s * 2.0;
                    S = s * slider3.Value;
                    label3.Content = slider3.Value;
                }
                else if (s < 50)
                {
                    //S = s * 4.00;
                    S = s * slider4.Value;
                    label4.Content = slider4.Value;
                }
                else
                {
                    S = 0;
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
            j = PlusCheck(x - k);
            r = PlusCheck(y - t);

            vhandx = (j / 0.0254) / 0.12;  //時間間隔は大体の値
            vhandy = (r / 0.0254) / 0.12;

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
            if (x - k < 0)
            {
                xpx = -1 * xpx;
            }
            if (y - t < 0)
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

            //label1.Content = "x=" + clickx;
            //label2.Content = "y=" + clicky;
            //label4.Content = depth;

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

                    time2 = Environment.TickCount;
                    flaggu = 0;
                    label.Content = time2 - time1;
                }
            }


        }


        //エリア制御
        private void Area(int x, int y)
        {
            if (x > 1200)
            {
                cx = 1200;
            }
            else if (x < 0)
            {
                cx = 0;
            }

            if (y > 950)
            {
                cy = 950;
            }
            else if (y < 0)
            {
                cy = 0;
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

            // カメラ座標系をDepth座標系に変換する
            var point = kinect.CoordinateMapper.MapCameraPointToDepthSpace(joint.Position);
            if ((point.X < 0) || (point.Y < 0))
            {
                return;
            }

            // Depth座標系で円を配置する
            Canvas.SetLeft(ellipse, point.X - (R / 2));
            Canvas.SetTop(ellipse, point.Y - (R / 2));

            CanvasBody.Children.Add(ellipse);
        }







    }
}
