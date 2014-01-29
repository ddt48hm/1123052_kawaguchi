//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.DepthBasics
{
    using System;
    using System.Globalization; // グローバライズ関数
    using System.IO; //ファイルアクセス
    using System.Windows; // WPFの基本要素クラス
    using System.Windows.Media;　// WPFアプリケーション内の描画
    using System.Windows.Media.Imaging;
    using Microsoft.Kinect;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window // publuc
    {
        /// <summary>
        /// Active Kinect sensor
        /// kinectセンサーの有無
        /// </summary>
        private KinectSensor sensor;

        /// <summary>
        /// Bitmap that will hold color information
        /// 色情報を保存するBitmap型のオブジェクトを用意
        /// </summary>
        private WriteableBitmap colorBitmap;

        /// <summary>
        /// Intermediate storage for the depth data received from the camera
        /// カメラから取得した深度カメラの中間記憶データを保持する配列
        /// </summary>
        private DepthImagePixel[] depthPixels;

        /// <summary>
        /// Intermediate storage for the depth data converted to color
        /// 色変換深度データを保持する配列
        /// </summary>
        private byte[] colorPixels;

        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// MainWindow classのインスタンス初期化
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Execute startup tasks
        /// スタートアップタスク（下準備）
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        /// ウィンドウがはじめて呼ばれたときの処理
        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            // Look through all sensors and start the first connected one
            // 接続されている全てのセンサーを確認後、接続された1台目から初期化処理を開始します
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
                //使えるものを探して接続
            }

            if (null != this.sensor)
            {
                // Turn on the depth stream to receive depth frames
                // 深度情報を(フレーム)を受信するために深度ストリームを開始
                this.sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
                
                // Allocate space to put the depth pixels we'll receive
                // 受信した深度ピクセル情報を割り当てるスペースを用意
                this.depthPixels = new DepthImagePixel[this.sensor.DepthStream.FramePixelDataLength];

                // Allocate space to put the color pixels we'll create
                // カラーピクセルを割り当てるスペースを用意
                this.colorPixels = new byte[this.sensor.DepthStream.FramePixelDataLength * sizeof(int)];

                // This is the bitmap we'll display on-screen
                // 画面上に描画するためのBitmapSourceを用意
                this.colorBitmap = new WriteableBitmap(this.sensor.DepthStream.FrameWidth, this.sensor.DepthStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);

                // Set the image we display to point to the bitmap where we'll put the image data
                // 画像データ生成のための設定
                this.Image.Source = this.colorBitmap;

                // Add an event handler to be called whenever there is new depth frame data
                // 新しい深度情報(フレーム)を取得の際に呼び出されるイベントハンドラの追加
                this.sensor.DepthFrameReady += this.SensorDepthFrameReady;

                // Start the sensor!
                // Kinectの動作を開始
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
                //　エラーの表示
                this.statusBarText.Text = Properties.Resources.NoKinectReady;
            }

            
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
        /// Event handler for Kinect sensor's DepthFrameReady event
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void SensorDepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        {
            using (DepthImageFrame depthFrame = e.OpenDepthImageFrame())
            {
                if (depthFrame != null)
                {
                    // Copy the pixel data from the image to a temporary array
                    // 一時配列にイメージからピクセルデータをコピーする
                    depthFrame.CopyDepthImagePixelDataTo(this.depthPixels);

                    // Get the min and max reliable depth for the current frame
                    // 現在のフレームの最小、最大の深度情報を取得する

                    int minDepth = depthFrame.MinDepth;
                    int maxDepth = depthFrame.MaxDepth;

                    // Convert the depth to RGB
                    // RGBに深度情報を変換
                    int colorPixelIndex = 0;
                    float tmp = 256f / (maxDepth - minDepth);
                    for (int i = 0; i < this.depthPixels.Length; ++i)
                    {
                        // Get the depth for this pixel
                        // ピクセルの深さを取得
                        short depth = depthPixels[i].Depth;

                        // To convert to a byte, we're discarding the most-significant
                        // rather than least-significant bits.
                        // We're preserving detail, although the intensity will "wrap."
                        // Values outside the reliable depth range are mapped to 0 (black).

                        // Note: Using conditionals in this loop could degrade performance.
                        // Consider using a lookup table instead when writing production code.
                        // See the KinectDepthViewer class used by the KinectExplorer sample
                        // for a lookup table example.
                        byte intensity = (byte)(depth >= minDepth && depth <= maxDepth ? depth : 0);
                        

                        // オプション1
                        //// Write out blue byte
                        /*this.colorPixels[colorPixelIndex++] = intensity % 100;

                        //// Write out green byte
                        //this.colorPixels[colorPixelIndex++] = intensity % 100;

                        //// Write out red byte                        
                        this.colorPixels[colorPixelIndex++] = intensity % 100;*/

                        //画面の色を変更
                        //青＋赤＝紫
                        /*this.colorPixels[colorPixelIndex++] = (byte)(depth * tmp);
                        this.colorPixels[colorPixelIndex++] = 0;
                        this.colorPixels[colorPixelIndex++] = (byte)(depth * tmp);*/


                        // Write out blue byte
                        // 青を描画
                        this.colorPixels[colorPixelIndex++] = (byte)(depth * tmp);
                        
                        // Write out green byte
                        // 緑を描画
                        this.colorPixels[colorPixelIndex++] = (byte)(depth * tmp);
                        
                        // Write out red byte      
                        // 赤を描画
                        this.colorPixels[colorPixelIndex++] = (byte)(depth * tmp);
                                                                        
                        // We're outputting BGR, the last byte in the 32 bits is unused so skip it
                        // If we were outputting BGRA, we would write alpha here.
                        // BGRを出力し、32ビットの最後はBGRAを先に出力していた場合αはスキップする
                        ++colorPixelIndex;
                    }

                    //中央のピクセルの深度情報
                    label1.Content = depthPixels[this.depthPixels.Length / 2 - 320].Depth;
                    label1.FontSize = 50;

                    // Write the pixel data into our bitmap
                    // bitmapにpixel dataを書き込む
                    this.colorBitmap.WritePixels(
                        new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight),
                        this.colorPixels,
                        this.colorBitmap.PixelWidth * sizeof(int),
                        0);
                }
            }
        }

        /// <summary>
        /// Handles the user clicking on the screenshot button
        /// スクリーンショットを押しているとき
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        /// オブジェクトを送信するメソッド
        /// イベントの引数
        private void ButtonScreenshotClick(object sender, RoutedEventArgs e)
        {
            if (null == this.sensor)
            {
                this.statusBarText.Text = Properties.Resources.ConnectDeviceFirst;
                return;
            }

            // create a png bitmap encoder which knows how to save a .png file
            // pngでファイルを保存する
            BitmapEncoder encoder = new PngBitmapEncoder();

            // create frame from the writable bitmap and add to encoder
            // 書き込み可能なビットマップからフレームを作成して、エンコーダに追加
            encoder.Frames.Add(BitmapFrame.Create(this.colorBitmap));

            string time = System.DateTime.Now.ToString("hh'-'mm'-'ss", CultureInfo.CurrentUICulture.DateTimeFormat);

            string myPhotos = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

            string path = Path.Combine(myPhotos, "KinectSnapshot-" + time + ".png");

            // write the new file to disk
            // ディスクに新しいファイルを作成
            try
            {
                using (FileStream fs = new FileStream(path, FileMode.Create))
                {
                    encoder.Save(fs);
                }

                this.statusBarText.Text = string.Format(CultureInfo.InvariantCulture, "{0} {1}", Properties.Resources.ScreenshotWriteSuccess, path);
            }
            catch (IOException)
            {
                this.statusBarText.Text = string.Format(CultureInfo.InvariantCulture, "{0} {1}", Properties.Resources.ScreenshotWriteFailed, path);
            }
        }
        
        /// <summary>
        /// Handles the checking or unchecking of the near mode combo box
        /// near modeのチェックを入れる、外すとき
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void CheckBoxNearModeChanged(object sender, RoutedEventArgs e)
        {
            if (this.sensor != null)
            {
                // will not function on non-Kinect for Windows devices
                // ウィンドウズ用のkinectで無くて機能しない場合
                try
                {
                    if (this.checkBoxNearMode.IsChecked.GetValueOrDefault())
                    {
                        this.sensor.DepthStream.Range = DepthRange.Near;
                    }
                    else
                    {
                        this.sensor.DepthStream.Range = DepthRange.Default;
                    }


                }
                catch (InvalidOperationException)
                {
                }
            }
        }
    }
}