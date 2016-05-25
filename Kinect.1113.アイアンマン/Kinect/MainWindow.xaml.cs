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

            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
                Close();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
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
                            DrawHandStateL(body.Joints[JointType.HandLeft],
                                body.HandLeftConfidence, body.HandLeftState);
                            //3次元座標を取得
                            CameraSpacePoint h = new CameraSpacePoint();
                            h = joint.Value.Position;
                            hl = h.Z;
                        }
                        // 右手を追跡していたら、手の状態を表示する
                        else if (joint.Value.JointType == JointType.HandRight)
                        {
                            DrawHandStateR(body.Joints[JointType.HandRight],
                                body.HandRightConfidence, body.HandRightState);
                            //3次元座標を取得
                            CameraSpacePoint h = new CameraSpacePoint();
                            h = joint.Value.Position;
                            hr = h.Z;
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
                                //DrawEllipse(joint.Value, 10, Brushes.Red);
                                //ButtonSelect(body.Joints[JointType.HandTipRight]);
                                //DepthClick2(body.Joints[JointType.HandTipRight]);
                            }
                        }
                        //首の付け根
                        else if (joint.Value.JointType == JointType.SpineShoulder)
                        {
                            //DrawEllipse(joint.Value, 10, Brushes.Green);
                            DecisionButtonLocation(body.Joints[JointType.SpineShoulder]);
                            //3次元座標を取得
                            CameraSpacePoint h = new CameraSpacePoint();
                            h = joint.Value.Position;
                            kubi = h.Z;
                        }
                        //頭
                        else if (joint.Value.JointType == JointType.Head)
                        {
                            DrawEllipse(joint.Value, 10, Brushes.Green);
                            DecisionFaceLocation(body.Joints[JointType.Head]);
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

        double kubi, hl, hr;

        private void DecisionButtonLocation(Joint joint)
        {
            //3次元座標を取得
            CameraSpacePoint ss = new CameraSpacePoint();
            ss = joint.Position;

            var ssx = ss.X;
            var ssy = ss.Y;
            var ssz = ss.Z;

            Joint a = new Joint();

            a.Position.X = ssx + (float)0.2;
            a.Position.Y = ssy + (float)0.2;
            a.Position.Z = ssz + (float)0.4;

            //DrawEllipse(a, 10, Brushes.Red);

            Joint b = new Joint();

            b.Position.X = a.Position.X + (float)0.2;
            b.Position.Y = a.Position.Y - (float)0.2;
            b.Position.Z = a.Position.Z;

            //DrawEllipse(b, 10, Brushes.Red);

            DrawButton(a, b);
            
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


        private void DrawHandStateL(Joint joint,
            TrackingConfidence trackingConfidence, HandState handState)
        {
            CanvasHandL.Children.Clear();
            CanvasBeamL.Children.Clear();

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

                HandPowerL(joint);
                Beam(joint);

            }
        }


        private void DrawHandStateR(Joint joint,
            TrackingConfidence trackingConfidence, HandState handState)
        {
            CanvasHandR.Children.Clear();
            CanvasBeamR.Children.Clear();

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

                HandPowerR(joint);
                Beam(joint);

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

            // Depth座標系で円を配置する
            Canvas.SetLeft(ellipse, point.X / 2 - (R / 2));
            Canvas.SetTop(ellipse, point.Y / 2 - (R / 2));

            CanvasBody.Children.Add(ellipse);

        }



        //ターゲットの位置決め
        private void RandomTarget()
        {
            int seed = Environment.TickCount;
            Random rnd = new Random(seed++);

            locx = rnd.Next(480, 580);
            locy = rnd.Next(270, 370);

        }

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

            if ((locx <= x && x <= locx + r) && (locy <= y && y <= locy + r))
            {
                CanvasButton.Children.Clear();

                var R = r * 1.5;

                //長方形
                Rectangle rect = new Rectangle()
                {
                    Width = R,
                    Height = R,
                    Fill = Brushes.Yellow,
                    Opacity = 0.5,
                };

                Canvas.SetLeft(rect, locx - (R - r) / 2);
                Canvas.SetTop(rect, locy - (R - r) / 2);

                CanvasButton.Children.Add(rect);

            }


        }

        float r = 50;
        int locx = 500;
        int locy = 300;

        private void DrawButton2(Joint joint, Joint joint2)
        {
            //CanvasButton.Children.Clear();

            //RandomTarget();

            // カメラ座標系をカラー座標系に変換する
            var point = kinect.CoordinateMapper.MapCameraPointToColorSpace(joint.Position);
            if ((point.X < 0) || (point.Y < 0))
            {
                return;
            }
            // カメラ座標系をカラー座標系に変換する
            var point2 = kinect.CoordinateMapper.MapCameraPointToColorSpace(joint2.Position);
            if ((point2.X < 0) || (point2.Y < 0))
            {
                return;
            }

            r = (point2.X - point.X) / 2;

            //長方形
            Rectangle rect = new Rectangle()
            {
                Width = r,
                Height = r,
                Fill = Brushes.Yellow,
                Opacity = 0.5,

            };

            locx = (int)(point.X / 2);
            locy = (int)(point.Y / 2);

            Canvas.SetLeft(rect, locx);
            Canvas.SetTop(rect, locy);

            CanvasButton.Children.Add(rect);


        }


        private void DrawButton(Joint joint,Joint joint2)
        {
            //CanvasButton.Children.Clear();

            //RandomTarget();

            // カメラ座標系をカラー座標系に変換する
            var point = kinect.CoordinateMapper.MapCameraPointToColorSpace(joint.Position);
            if ((point.X < 0) || (point.Y < 0))
            {
                return;
            }
            // カメラ座標系をカラー座標系に変換する
            var point2 = kinect.CoordinateMapper.MapCameraPointToColorSpace(joint2.Position);
            if ((point2.X < 0) || (point2.Y < 0))
            {
                return;
            }

            r = (point2.X - point.X) / 2;

            //長方形
            Rectangle rect = new Rectangle()
            {
                Width = r,
                Height = r,
                Fill = Brushes.Yellow,
                Opacity = 0.5,

            };

            locx = (int)(point.X / 2);
            locy = (int)(point.Y / 2);

            Canvas.SetLeft(rect, locx);
            Canvas.SetTop(rect, locy);

            //CanvasButton.Children.Add(rect);


        }

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
                        DrawClickPoint(40, point.X / 2, point.Y / 2);

                        if ((locx <= point.X / 2 && point.X / 2 <= locx + r) && (locy <= point.Y / 2 && point.Y / 2 <= locy + r))
                        {

                            CanvasButton.Children.Clear();
                            RandomTarget();

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
        

        private void DecisionFaceLocation(Joint joint)
        {
            CanvasFace.Children.Clear();


            // カメラ座標系をカラー座標系に変換する
            var point = kinect.CoordinateMapper.MapCameraPointToColorSpace(joint.Position);
            if ((point.X < 0) || (point.Y < 0))
            {
                return;
            }

            var R = r * 2.5;

            this.imageFace.Height = R;
            this.imageFace.Width = R;

            Canvas.SetLeft(imageFace, point.X / 2 - R / 2);
            Canvas.SetTop(imageFace, point.Y / 2 - R / 2);

            CanvasFace.Children.Add(imageFace);

        }

        private void HandPowerL(Joint joint)
        {
            CanvasHandL.Children.Clear();

            // カメラ座標系をカラー座標系に変換する
            var point = kinect.CoordinateMapper.MapCameraPointToColorSpace(joint.Position);
            if ((point.X < 0) || (point.Y < 0))
            {
                return;
            }

            var R = r * 4;

            this.LeftHand.Height = R;
            this.LeftHand.Width = R;

            Canvas.SetLeft(LeftHand, point.X / 2 - R / 4);
            Canvas.SetTop(LeftHand, point.Y / 2 - R * 1.2 / 2);

            CanvasHandL.Children.Add(LeftHand);

            if (kubi > hl + 0.4)
            {
                var W = r * 6;

                this.ImageBeamL.Height = R;
                this.ImageBeamL.Width = R;

                Canvas.SetLeft(ImageBeamL, point.X / 2 - R / 2);
                Canvas.SetTop(ImageBeamL, point.Y / 2 - R / 2);

                CanvasBeamL.Children.Add(ImageBeamL);

            }

        }

        private void HandPowerR(Joint joint)
        {
            CanvasHandR.Children.Clear();

            // カメラ座標系をカラー座標系に変換する
            var point = kinect.CoordinateMapper.MapCameraPointToColorSpace(joint.Position);
            if ((point.X < 0) || (point.Y < 0))
            {
                return;
            }

            var R = r * 4;

            this.RightHand.Height = R;
            this.RightHand.Width = R;

            Canvas.SetLeft(RightHand, point.X / 2 - R / 2);
            Canvas.SetTop(RightHand, point.Y / 2 - R / 2);

            CanvasHandR.Children.Add(RightHand);

            if (kubi > hr + 0.4)
            {
                var W = r * 6;

                this.ImageBeamR.Height = R;
                this.ImageBeamR.Width = R;

                Canvas.SetLeft(ImageBeamR, point.X / 2 - R / 2);
                Canvas.SetTop(ImageBeamR, point.Y / 2 - R / 2);

                CanvasBeamR.Children.Add(ImageBeamR);

            }

        }


        private void Beam(Joint joint)
        {
            // カメラ座標系をカラー座標系に変換する
            var point = kinect.CoordinateMapper.MapCameraPointToColorSpace(joint.Position);
            if ((point.X < 0) || (point.Y < 0))
            {
                return;
            }

            var R = r * 6;

            var ellipse = new Ellipse()
            {
                Width = R,
                Height = R,
                Fill = Brushes.Gold,
                Opacity = 0.5,
            };

        }


    }
}
