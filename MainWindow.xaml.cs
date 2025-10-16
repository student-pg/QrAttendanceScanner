using System;
using System.Drawing;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using AForge.Video;
using AForge.Video.DirectShow;
using ZXing;
using ZXing.Windows.Compatibility; // 追加

namespace QrAttendanceScanner
{
    // GDI+のBitmapをWPFのImageコントロールで表示するためのヘルパークラス
    public static class BitmapConverter
    {
        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        public static BitmapSource ToBitmapSource(Bitmap bitmap)
        {
            var hBitmap = bitmap.GetHbitmap();
            try
            {
                return Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                DeleteObject(hBitmap);
            }
        }
    }

    public partial class MainWindow : Window
    {
        private FilterInfoCollection? videoDevices;
        private VideoCaptureDevice? videoSource;
        private ZXing.BarcodeReader<Bitmap>? qrReader; // private BarcodeReader? qrReader; のままでOK

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
            this.Closed += MainWindow_Closed;
            qrReader = new ZXing.BarcodeReader<Bitmap>(null, (bitmap) => new ZXing.Windows.Compatibility.BitmapLuminanceSource(bitmap), null);
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeCamera();
        }

        private void InitializeCamera()
        {
            try
            {
                // 接続されているカメラデバイスのリストを取得
                videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            }
            catch (Exception ex)
            {
                // カメラデバイスの列挙中にエラーが発生した場合
                ShowCameraError("カメラデバイスの列挙中にエラーが発生しました: " + ex.Message);
                return;
            }

            // 💡 カメラ非搭載時の処理
            if (videoDevices == null || videoDevices.Count == 0)
            {
                ShowCameraError("PCにカメラ機能が搭載されていません。");
                return;
            }

            try
            {
                // 最初のカメラデバイスを選択し、ストリームを開始
                videoSource = new VideoCaptureDevice(videoDevices[0].MonikerString);
                videoSource.NewFrame += new NewFrameEventHandler(VideoSource_NewFrame);
                videoSource.Start();

                // 映像エリアのUIをリセット (カメラが接続された場合)
                CameraImage.Source = null;
            }
            catch (Exception ex)
            {
                ShowCameraError("カメラの初期化または起動中にエラーが発生しました: " + ex.Message);
            }
        }

        /// <summary>
        /// カメラ接続失敗時または非搭載時にUIを更新する
        /// </summary>
        /// <param name="message">表示するエラーメッセージ</param>
        private void ShowCameraError(string message)
        {
            // カメラ映像エリアにエラーメッセージを表示するためのTextBlockを動的に生成
            var textBlock = new System.Windows.Controls.TextBlock
            {
                Text = "カメラに接続できません。\n" + message,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                FontSize = 16,
                Foreground = System.Windows.Media.Brushes.Gray
            };

            // Imageコントロール（CameraImage）の親要素であるBorderにTextBlockを設定
            // Imageコントロールは非表示にするか、親要素にTextBlockを直接追加
            if (CameraImage.Parent is System.Windows.Controls.Border parentBorder)
            {
                parentBorder.Child = textBlock;
                parentBorder.Background = System.Windows.Media.Brushes.White;
            }
            else
            {
                // 親要素がBorderでない場合のフォールバック（今回はBorderを想定）
                CameraImage.Source = null;
                ResultTextBox.Text = message;
            }

            ResultTextBox.Text = message;
        }

        private void MainWindow_Closed(object? sender, EventArgs e)
        {
            // アプリケーション終了時にカメラを停止
            if (videoSource != null && videoSource.IsRunning)
            {
                videoSource.SignalToStop();
                videoSource.WaitForStop();
            }
        }

        private void VideoSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            // 新しいフレーム（画像）が届いた

            // 映像フレームをBitmapとしてコピー
            Bitmap bitmap = (Bitmap)eventArgs.Frame.Clone();

            // UIスレッドで処理を行う
            Application.Current.Dispatcher.Invoke(() =>
            {
                // 1. 映像をWPFのImageコントロールに表示
                CameraImage.Source = BitmapConverter.ToBitmapSource(bitmap);

                // 2. 映像フレームをQRコードデコード関数に渡す
                DecodeQrCode(bitmap);
            });

            // 3. Bitmapオブジェクトを解放
            bitmap.Dispose();
        }

        /// <summary>
        /// 受け取ったBitmapからQRコードをデコードし、結果をUIに出力します。
        /// </summary>
        private void DecodeQrCode(Bitmap bitmap)
        {
            try
            {
                // ZXingリーダーでQRコードを読み取る
                var result = new BarcodeReader().Decode(bitmap); // ← 修正: binaryBitmapではなくbitmapを渡す

                if (result != null)
                {
                    // QRコードが正常に読み取られた場合
                    string scannedData = result.Text;

                    // 即座にUI上のTextBoxに出力
                    ResultTextBox.Text = scannedData;

                    // 成功後の処理を実行
                    ProcessAttendance(scannedData);
                }
            }
            catch (Exception)
            {
                // デコード中に発生する可能性のあるエラーは無視する
            }
        }

        /// <summary>
        /// スキャンされたデータを受け取り、出席管理のロジックを実行します。
        /// </summary>
        /// <param name="data">スキャンされたQRコードのデータ</param>
        private void ProcessAttendance(string data)
        {
            // ここに、出席管理アプリのコアロジックを実装します。
            // 例: スキャン成功後の確認メッセージやデータベースへの記録など
        }

        // ... (MainWindowクラスの終わり)
    }
}