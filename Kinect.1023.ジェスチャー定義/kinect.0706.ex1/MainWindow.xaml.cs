﻿using System;
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
using Microsoft.Kinect;
//using System.Windows.Forms;
using System.Runtime.InteropServices;
using Microsoft.Kinect.VisualGestureBuilder;



namespace kinect._0706.ex1
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

        VisualGestureBuilderDatabase gestureDatabase;
        VisualGestureBuilderFrameSource gestureFrameSource;
        VisualGestureBuilderFrameReader gestureFrameReader;
        VisualGestureBuilderFrame gestureFrame;

        Gesture depthclick;





        // 表示
        WriteableBitmap depthImage;
        ushort[] depthBuffer;
        byte[] depthBitmapBuffer;
        Int32Rect depthRect;
        int depthStride;

        Point depthPoint;
        const int R = 20;


        //マウス操作の関数宣言
        [DllImport("USER32.dll",CallingConvention=CallingConvention.StdCall)]
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




                // ジェスチャーリーダーを開く
                gestureFrameReader = gestureFrameSource.OpenReader();
                gestureFrameReader.IsPaused = true;
                gestureFrameReader.FrameArrived += gestureFrameReader_FrameArrived;

                // Gesturesの初期設定
                gestureDatabase = new VisualGestureBuilderDatabase(@"Depthclickv2.gba");
                gestureFrameSource = new VisualGestureBuilderFrameSource(kinect, 0);

                // 使用するジェスチャーをデータベースから取り出す
                foreach (var gesture in gestureDatabase.AvailableGestures)
                {
                    if (gesture.Name == "Depthclick")
                    {
                        depthclick = gesture;
                    }

                    this.gestureFrameSource.AddGesture(gesture);
                }



            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
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
            // 距離情報の表示を更新する
            //UpdateDepthValue();

            // 0-8000のデータを255ごとに折り返すようにする(見やすく)
            for (int i = 0; i < depthBuffer.Length; i++)
            {
                depthBitmapBuffer[i] = (byte)(depthBuffer[i] % 255);
            }

            depthImage.WritePixels(depthRect, depthBitmapBuffer, depthStride, 0);
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




        //座標格納
        int startpoint = 0;
        int cx, cy;
        int nowx, nowy;
        double k, t, j, r;
        double vhandx, vhandy, vpointx, vpointy;
        double xpx, ypx, inchx, inchy;

        //DateTime t0, time1;
        //TimeSpan timespan;



        // ボディの表示
        private void DrawBodyFrame()
        {
            CanvasBody.Children.Clear();

            foreach (var body in bodies.Where(b => b.IsTracked))
            {
                foreach (var joint in body.Joints)
                {

                    // 手の位置が追跡状態
                    if (joint.Value.TrackingState == TrackingState.Tracked)
                    {

                        // ジェスチャー判定対象としてbodyを選択
                        gestureFrameSource.TrackingId = body.TrackingId;
                        // ジェスチャー判定開始
                        gestureFrameReader.IsPaused = false;
                        
                        // ジェスチャーの判定結果がある場合
                        if (gestureFrame != null && gestureFrame.DiscreteGestureResults != null)
                        {
                            var result = gestureFrame.DiscreteGestureResults[depthclick];
                            if (result.Detected)
                            {
                                MessageBox.Show("CLICK");

                            }
                            else
                            {
                                
                            }

                        }
                        


                        //左手
                        if (joint.Value.JointType == JointType.HandTipRight)
                        {
                            if (startpoint == 0)
                            {
                                //t0 = DateTime.Now;

                                cx = 200;
                                cy = 200;

                                startpoint = 1;

                            }

                            /*
                            if (startpoint == 1)
                            {
                                //time1 = DateTime.Now;
                                //timespan = time1 - t0;
                                //label3.Content = timespan;

                                //3次元座標を取得
                                CameraSpacePoint s = new CameraSpacePoint();
                                s = joint.Value.Position;

                                //2次元座標に変換、表示
                                var handpoint2 = kinect.CoordinateMapper.MapCameraPointToDepthSpace(joint.Value.Position);

                                a = s.X;
                                b = s.Y;

                                //label1.Content = a;
                                //label2.Content = prex;
                                //label2.Content = b - prey;

                                startpoint = 2;

                            }
                            */



                            //3次元座標を取得
                            CameraSpacePoint h = new CameraSpacePoint();
                            h = joint.Value.Position;

                            //time1 = DateTime.Now;
                            //timespan = time1 - t0;
                            //label3.Content = timespan;

                            label1.Content = "x=" + h.X;
                            label2.Content = "y=" + h.Y;

                            //手の速度（インチ/秒）
                            j = PlusCheck(h.X - k);
                            r = PlusCheck(h.Y - t);

                            vhandx = (j / 0.0254) / 0.12;  //時間間隔は大体の値
                            vhandy = (r / 0.0254) / 0.12;

                            label3.Content = "速度=" + vhandx;

                            //ポインタの速度へ変換
                            vpointx = SpeedChange(vhandx);
                            vpointy = SpeedChange(vhandy);

                            //label3.Content = "速度=" + vpointx;

                            //ポインタの移動量
                            inchx = vpointx * 0.12;
                            inchy = vpointy * 0.12;

                            //インチをピクセル数へ
                            xpx = (1280 / (5 / Math.Sqrt(41) * 17)) * inchx;
                            ypx = (1024 / (4 / Math.Sqrt(41) * 17)) * inchy;

                            if (h.X - k < 0)
                            {
                                xpx = -1 * xpx;
                            }
                            if (h.Y - t < 0)
                            {
                                ypx = -1 * ypx;
                            }

                            //label3.Content = xpx;

                            DrawEllipse(joint.Value, 10, Brushes.Green);

                            DrawHandState(body.Joints[JointType.HandRight],
                               body.HandRightConfidence, body.HandRightState);

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
                            //DrawCursor(10, cx, cy);

                            //前フレームの座標として格納
                            k = h.X;
                            t = h.Y;

                            //t0 = time1;




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

        //速度変換
        private double SpeedChange(double s)
        {
            double S;

            //加速なし
            //S = s;
           
            //ポインタの速度を決定
            if (s < 0.9)
            {
                S = s * 0.4;
            }
            else if (s < 2.25)
            {
                S = s * 1.0;
            }
            else if (s < 10.86)
            {
                S = s * 2.0;
            }
            else if (s < 50)
            {
                S = s * 4.00;
            }
            else
            {
                S = 0;
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


        //ポインタを表示
        private void DrawCursor(int R, int x, int y)
        {
            CanvasPoint.Children.Clear();

            var ellipse = new Ellipse()
            {
                Width = R,
                Height = R,
                StrokeThickness = 2,
                Stroke = Brushes.Red,
            };
            Canvas.SetLeft(ellipse, x);
            Canvas.SetTop(ellipse, y);
            CanvasPoint.Children.Add(ellipse);

            Line line = new Line()
            {
                //始点
                X1 = x + R / 2,
                Y1 = y,
                //終点
                X2 = x + R / 2,
                Y2 = y + R,
                //色
                Stroke = new SolidColorBrush(Colors.Red),
                //太さ
                StrokeThickness = 2,
            };
            CanvasPoint.Children.Add(line);

            Line line2 = new Line()
            {
                //始点
                X1 = x,
                Y1 = y + R / 2,
                //終点
                X2 = x + R,
                Y2 = y + R / 2,
                //色
                Stroke = new SolidColorBrush(Colors.Red),
                //太さ
                StrokeThickness = 2,
            };
            CanvasPoint.Children.Add(line2);

            /*
            var text = new TextBlock()
            {
                Text = string.Format("Cursor"),
                FontSize = 20,
                Foreground = Brushes.Green,
            };
            Canvas.SetLeft(text, x+R);
            Canvas.SetTop(text, y+R);
            CanvasPoint.Children.Add(text);
            */
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

    }
}
