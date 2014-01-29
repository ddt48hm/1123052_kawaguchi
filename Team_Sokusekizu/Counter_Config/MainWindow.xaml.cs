//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------


namespace Microsoft.Samples.Kinect.ColorBasics
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Microsoft.Kinect;
    using System.Windows.Threading;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor sensor;


        /// <summary>
        /// Bitmap that will hold color information
        /// </summary>
        private WriteableBitmap colorBitmap;


        /// <summary>
        /// Intermediate storage for the color data received from the camera
        /// </summary>
        private byte[] colorPixels;


        /// <summary>
        /// タイマーイベント用変数
        /// </summary>
        private DispatcherTimer dispatcherTimer;

        /// <summary>
        /// 人物認識フラグ
        /// </summary>
        private bool[] flag;
        private bool[] old;

        /// <summary>
        /// 認識人物カウント用変数
        /// </summary>
        private int[] count = new[]{0,0,0,0,0,0};

        /// <summary>
        /// 総認識人数用変数
        /// </summary>
        private int Asum;

        /// <summary>
        /// 現在の認識人数用変数
        /// </summary>
        private int NewSum;

        /// <summary>
        /// 前回の認識人数用変数
        /// </summary>
        private int OldSum;

        /// <summary>
        /// 増分用の変数
        /// </summary>
        private int Zoubun;

        /// <summary>
        /// 時間カウント用の変数
        /// </summary>
        private int Tcount;

        /// <summary>
        /// テキストボックスの時間を取得する変数
        /// </summary>
        private int TextHour;
        private int TextMin;
        private int TextSec;

        private int fno;

        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            Asum = 0;
            NewSum = 0;
            OldSum = 0;
            Zoubun = 0;
            Tcount = 0;
            fno = 1;

            // 初期値
            TextHour = 0;
            TextMin = 0;
            TextSec = 1;
        }

        /// <summary>
        /// Execute startup tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            // Look through all sensors and start the first connected one.
            // This requires that a Kinect is connected at the time of app startup.
            // To make your app robust against plug/unplug, 
            // it is recommended to use KinectSensorChooser provided in Microsoft.Kinect.Toolkit (See components in Toolkit Browser).
            foreach (var potentialSensor in KinectSensor.KinectSensors)
            {
                if (potentialSensor.Status == KinectStatus.Connected)
                {
                    this.sensor = potentialSensor;
                    break;
                }
            }


            if (null != this.sensor)
            {
                // Turn on the color stream to receive color frames
                this.sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);


                // Allocate space to put the pixels we'll receive
                this.colorPixels = new byte[this.sensor.ColorStream.FramePixelDataLength];


                // This is the bitmap we'll display on-screen
                this.colorBitmap = new WriteableBitmap(this.sensor.ColorStream.FrameWidth, this.sensor.ColorStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);


                // Set the image we display to point to the bitmap where we'll put the image data
                this.Image.Source = this.colorBitmap;


                // Add an event handler to be called whenever there is new color frame data
                this.sensor.ColorFrameReady += this.SensorColorFrameReady;


                // Turn on the skeleton stream to receive skeleton frames
                this.sensor.SkeletonStream.Enable();


                // Add an event handler to be called whenever there is new color frame data
                this.sensor.SkeletonFrameReady += this.sensor_SkeletonFrameReady;


                // Start the sensor!
                try
                {
                    this.sensor.Start();
                }
                catch (IOException)
                {
                    this.sensor = null;
                }
            }


            if (null == this.sensor)
            {
                this.statusBarText.Text = Properties.Resources.NoKinectReady;
            }

            // スレッドでの呼び出し優先度指定
            this.dispatcherTimer = new DispatcherTimer();

            this.TextHour = int.Parse(textBox1.Text);
            this.TextMin = int.Parse(textBox2.Text);
            this.TextSec = int.Parse(textBox3.Text);

            // n秒ごとに処理()
            this.dispatcherTimer.Interval = new TimeSpan(TextHour, TextMin, TextSec);

            // イベント追加
            this.dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);

            // タイマー動作開始
            this.dispatcherTimer.Start();
        }



        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (null != this.sensor)
            {
                this.sensor.Stop();
            }
        }


        /// <summary>
        /// タイマーイベント
        /// </summary>
        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            Tcount++;

            // チェックボックスの判定
            // チェックが入っているときは、CSVファイルを出力する。
            if (checkbox.IsChecked == true)
            {
                OutPutCsv.CSVSave(this.flag,NewSum,Asum,fno);
                label4.Content = "録画中";
            }
            else if (checkbox.IsChecked == false)
            {
                label4.Content = "";
            }
            label5.Content = "チーム名：即席ーず\n学籍番号：1123033 小島健太郎、1123051古川佳樹、1123052 川口悠哉、1123183 斎間勇喜";

        }


        /// <summary>
        /// Event handler for Kinect sensor's ColorFrameReady event
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void SensorColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
            {
                if (colorFrame != null)
                {
                    // Copy the pixel data from the image to a temporary array
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


        private void sensor_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            Skeleton[] skeletons = new Skeleton[0];

            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    skeletonFrame.CopySkeletonDataTo(skeletons);
                    this.flag = new bool[skeletonFrame.SkeletonArrayLength];
                    this.old = new bool[skeletonFrame.SkeletonArrayLength];
                }
            }

            if (skeletons.Length != 0)
            {
                
                int skeletonNum = 0;
                foreach (Skeleton skel in skeletons)
                {
                    if (skel.TrackingState == SkeletonTrackingState.Tracked || skel.TrackingState == SkeletonTrackingState.PositionOnly)
                    {
                        this.flag[skeletonNum] = true;
                        // 認識されたら1をたてる
                        this.count[skeletonNum] = 1;
                    }
                    else if (skel.TrackingState == SkeletonTrackingState.NotTracked)
                    {
                        this.flag[skeletonNum] = false;
                        // 認識されないときは0
                        this.count[skeletonNum] = 0;
                    }

                    // 確認用
                    //Console.Write(this.flag[skeletonNum] + "  ");

                    skeletonNum++;
                   

                    //人数計算(1の数を足す)
                    NewSum = count[0] + count[1] + count[2] + count[3] + count[4] + count[5];

                    //認識中の人数表示
                    label3.Content =NewSum+"人";
                   
                    //認識総数の表示
                    // 人数が認識している時間分の数を数えないためのストッパー
                    // これにより、正式な人数がカウントできる

                    // 前回の認識人数より今の認識人数が大きかった時のみカウントする
                    if(NewSum > OldSum) 
                    {
                        // 今の認識人数と前回の認識人数から増分を出す
                        Zoubun = NewSum - OldSum;

                        // 増分を足していくことで正式な総認識人数を得る
                        Asum = Asum + Zoubun;

                        // 総人数の表示
                        label2.Content = Asum + "人";
                    }
                    // 今の認識人数を前回の認識人数に代入し、新たな前回の認識人数にする
                    OldSum = NewSum;
                    
                }
                //Console.WriteLine("\n");
            }
        }


        /// <summary>
        /// Handles the user clicking on the screenshot button
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void ButtonScreenshotClick(object sender, RoutedEventArgs e)
        {
            this.ScreenShot();
        }




        /// <summary>
        /// 画像保存
        /// </summary>
        private void ScreenShot()
        {
            if (null == this.sensor)
            {
                this.statusBarText.Text = Properties.Resources.ConnectDeviceFirst;
                return;
            }

            // create a png bitmap encoder which knows how to save a .png file
            // pngファイル用の符号化変数を作成
            BitmapEncoder encoder = new PngBitmapEncoder();

            Dialog1 dialog1 = new Dialog1();

            // create frame from the writable bitmap and add to encoder
            // 書き込み可能なbitmapからフレームを作成し、符号化変数に保存
            encoder.Frames.Add(BitmapFrame.Create(this.colorBitmap));

            string time = System.DateTime.Now.ToString("hh'-'mm'-'ss", CultureInfo.CurrentUICulture.DateTimeFormat);

            string filename = ("KinectSnapshot-" + time + ".png");

            string myPhotos = dialog1.SavePictureDialog();

            Console.WriteLine("@@" + myPhotos + "@@");

            string path = Path.Combine(myPhotos, filename);

            // write the new file to disk
            // ディスクに新しいファイルを書き込み
            try
            {
                using (FileStream fs = new FileStream(path, FileMode.Create))
                {
                    encoder.Save(fs);
                    OutPutCsv.config(path,filename);
                }

                this.statusBarText.Text = string.Format(CultureInfo.InvariantCulture, "{0} {1}", Properties.Resources.ScreenshotWriteSuccess, path);
            }
            catch (IOException)
            {
                this.statusBarText.Text = string.Format(CultureInfo.InvariantCulture, "{0} {1}", Properties.Resources.ScreenshotWriteFailed, path);
            }
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            this.TextHour = int.Parse(textBox1.Text);
            this.TextMin = int.Parse(textBox2.Text);
            this.TextSec = int.Parse(textBox3.Text);

            // n秒ごとに処理()
            this.dispatcherTimer.Interval = new TimeSpan(TextHour, TextMin, TextSec);
        }
    }
}