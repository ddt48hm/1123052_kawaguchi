//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.ColorBasics
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
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
    using System.IO;
    using System.Windows.Threading;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // 腕の状態
        enum State
        {
            Right,  //右手を挙げている状態
            Left,   //左手を挙げている状態
            WUp,    //左右両方とも挙げている状態
            WDoun,  //左右両方とも下げている状態
        };

        /// <summary>
        /// Kinectを扱うためのオブジェクト
        /// </summary>
        private KinectSensor sensor;

        /// <summary>
        /// 画面に表示するビットマップ
        /// </summary>
        private WriteableBitmap colorBitmap;

        /// <summary>
        /// Kinectセンサーからの画像情報を受け取る
        /// </summary>
        private byte[] colorPixels;

        private DispatcherTimer startTimer;
        private DispatcherTimer Sp_Timer;
        private DispatcherTimer Sp_Timer2;

        // ポイントをカウントする変数
        private int Point_Counter;

        private int ran;　　 // 乱数を格納する変数
        private int old_ran; // 前の問題の状態(前の乱数)を格納する変数

        private int Out_Check; //失敗した回数を数える変数

        private int time1;
        private int time2;
        private int time3;

        private int i;

        private bool f_Check;  // 問題提示のタイマーと動きの判定のタイマーの切り替えフラグ
        private bool f1;
        private bool f2;

        private bool f_LeftA;  // 左腕のフラグ
        private bool f_RightA; // 右腕のフラグ
        private bool f_LeftL;  // 左足のフラグ
        private bool f_RightL; // 右足のフラグ

        private float dot_LeftA;  // 左腕の内積記憶
        private float dot_RightA; // 右腕の内積記憶
        private float dot_LeftL;  // 左足の内積記憶
        private float dot_RightL; // 右足の内積記憶

        /// <summary>
        /// 現在の認識状態
        /// </summary>
        private State nowState;

        /// <summary>
        /// 比較する認識状態
        /// </summary>
        private State CheckState;

        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 画面がロードされたときに呼び出される。
        /// 初期化の処理はここに記入
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            //Kinectセンサーの接続を確認
            //接続が確認されたセンサーがあれば
            //それを扱うことにして処理を抜ける。
            foreach (var potentialSensor in KinectSensor.KinectSensors)
            {
                if (potentialSensor.Status == KinectStatus.Connected)
                {
                    this.sensor = potentialSensor;
                    break;
                }
            }

            //Kinectが確認できたら
            if (null != this.sensor)
            {
                //RGBカメラの使用。カラーストリームの準備
                this.sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
                this.colorPixels = new byte[this.sensor.ColorStream.FramePixelDataLength];
                this.colorBitmap = new WriteableBitmap(this.sensor.ColorStream.FrameWidth, this.sensor.ColorStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);
                this.Image.Source = this.colorBitmap;
                this.sensor.ColorFrameReady += this.SensorColorFrameReady;

                //骨格情報の使用。スケルトンストリームの準備
                this.sensor.SkeletonStream.Enable();
                this.sensor.SkeletonFrameReady += this.SensorSkeletonFrameReady;


                this.startTimer = new DispatcherTimer();
                this.startTimer.Interval = new TimeSpan(10000000);  // 1秒ごとに処理
                this.startTimer.Tick += new EventHandler(dispatcherTimer_Tick3);
                this.startTimer.Start();  // タイマー動作開始

                this.Sp_Timer = new DispatcherTimer();
                this.Sp_Timer.Interval = new TimeSpan(10000000);  // 1秒ごとに処理
                this.Sp_Timer.Tick += new EventHandler(dispatcherTimer_Tick);
                this.Sp_Timer.Start();  // タイマー動作開始

                this.Sp_Timer2 = new DispatcherTimer();
                this.Sp_Timer2.Interval = new TimeSpan(10000000);  // 1秒ごとに処理
                this.Sp_Timer2.Tick += new EventHandler(dispatcherTimer_Tick2);
                this.Sp_Timer2.Start();  // タイマー動作開始

                this.f_Check = true;
                this.f1 = false;
                this.f2 = false;
                ran = 0;
                i = 3;

                // Kinectセンサーの開始
                try
                {
                    this.sensor.Start();
                }
                catch (IOException)
                {
                    this.sensor = null;
                }
            }
        }

        /// <summary>
        /// 画面が閉じられたときの処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (null != this.sensor)
            {
                this.sensor.Stop();
            }
        }

        void dispatcherTimer_Tick3(object sender, EventArgs e)
        {
            time3++;
            /*if (f1 == true)
            {
                if (time3 == 10)
                   label4.Content = "スタート！";
                if (time3 == 12)
                    f2 = true;
            }*/
        }

        // 問題提示のタイマー
        void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            //this.batsu_R.Visibility = System.Windows.Visibility.Visible;

            //if (f2 == true)
            //{
                time1++;
                Console.WriteLine("1: " + time1);
                label4.Content = "";

                if (f_Check == true)
                {
                    // 2秒ごとに処理
                    if (time1 % 2 == 0 && time1 != 0)
                    {
                        // 問題の状態を作成するための乱数の発生
                        Random rand = new Random();
                        ran = rand.Next(0, 4); // 0 ～ 3
                        // 前の乱数結果と同じ場合は一度だけ乱数を作り直す
                        if (ran == old_ran)
                        {
                            ran = rand.Next(0, 4);
                        }
                        Console.WriteLine("[" + ran + "]");  // コンソール確認用

                        // 状態遷移
                        switch (ran)
                        {
                            // 両手を下げた状態
                            case 0:
                                this.CheckState = State.WDoun;
                                label2.Content = "×";
                                label3.Content = "×";
                                break;
                            // 両手を挙げた状態
                            case 1:
                                this.CheckState = State.WUp;
                                label2.Content = "○";
                                label3.Content = "○";
                                break;
                            case 2:
                                // 左手のみを挙げた状態
                                this.CheckState = State.Left;
                                label2.Content = "○";
                                label3.Content = "×";
                                break;
                            case 3:
                                // 右手のみを挙げた状態
                                this.CheckState = State.Right;
                                label2.Content = "×";
                                label3.Content = "○";
                                break;
                            default:
                                break;
                        }
                        old_ran = ran;
                        f_Check = false;
                    }
                }
            //}
        }

        // 問題の動作とあっているか確認するタイマーイベント
        void dispatcherTimer_Tick2(object sender, EventArgs e)
        {
            //if (f2 == true)
            //{
                time2++;
                label1.Content = Point_Counter; // ポイントの表示

                if (f_Check == false)
                {
                    // 4秒ごとに処理
                    if (time2 % 4 == 0 && time2 != 0)
                    {
                        Console.WriteLine(this.CheckState + " == " + this.nowState); //コンソール確認用
                        // 問題の動作と実際の動作があっているとき
                        if (this.CheckState == this.nowState)
                        {
                            Point_Counter++; // ポイント加算
                        }
                        else
                        {
                            Out_Check++;　// 失敗数を増やす
                        }
                        f_Check = true;
                    }
                }
            //}
        }

        private void SensorColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
            {
                if (colorFrame != null)
                {
           
                    // 一時配列にイメージからピクセルデータのコピーをする
                    colorFrame.CopyPixelDataTo(this.colorPixels);

                    // Write the pixel data into our bitmap
                    this.colorBitmap.WritePixels(
                        new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight),
                        this.colorPixels,
                        this.colorBitmap.PixelWidth * sizeof(int),
                        0);
                }
            }
        }

        private void SensorSkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            Skeleton[] skeletons = new Skeleton[0];

            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    skeletonFrame.CopySkeletonDataTo(skeletons);
                }
            }
                // 左腕
                if (this.dot_LeftA < 0.3f)
                    f_LeftA = false; // 肩と手の距離が遠い
                else
                    f_LeftA = true;　// 肩と手の距離が近い

                // 右腕
                if (this.dot_RightA < 0.3f)
                    f_RightA = false; // 肩と手の距離が遠い
                else
                    f_RightA = true;  // 肩と手の距離が近い

                // 左足
                if (this.dot_LeftL < 0.5f)
                    f_LeftL = false;

                // 右足
                if (this.dot_RightL < 0.5f)
                {

                }

             
                // 腕を曲げたときの処理
                if (f_LeftA == false && f_RightA == false) // 両腕が一定値曲がっていない
                {
                    this.nowState = State.WDoun;
                    //Console.WriteLine("wd");
                }
                else if (f_LeftA == true && f_RightA == true) // 両腕が一定値曲がっている
                {
                    this.nowState = State.WUp;
                    //Console.WriteLine("wu");

                }
                else if (f_LeftA == false && f_RightA == true) // 右腕が一定値曲がっている
                {
                    this.nowState = State.Right;
                    //Console.WriteLine("r");

                }
                else if (f_LeftA == true && f_RightA == false) // 左腕が一定値曲がっている
                {
                    this.nowState = State.Left;
                    //Console.WriteLine("l");

                }

                if (skeletons.Length != 0)
                {
                    foreach (Skeleton skel in skeletons)
                    {
                        if (skel.TrackingState == SkeletonTrackingState.Tracked)
                        {
                            // 左腕の内積を取得
                            dot_LeftA = dotMath.Inner_Product(skel, JointType.HandLeft, JointType.ElbowLeft, JointType.ShoulderLeft);

                            // 右腕の内積を取得
                            dot_RightA = dotMath.Inner_Product(skel, JointType.HandRight, JointType.ElbowRight, JointType.ShoulderRight);

                            // 左足の内積を取得
                            dot_LeftL = dotMath.Inner_Product(skel, JointType.AnkleLeft, JointType.KneeLeft, JointType.HipLeft);

                            // 右足の内積を取得
                            dot_RightL = dotMath.Inner_Product(skel, JointType.AnkleRight, JointType.KneeRight, JointType.HipRight);


                            f1 = true;
                        }
                        else
                            f1 = false;
                    }
                }
            }
        }
    }