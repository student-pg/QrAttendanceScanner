// C#の最も基本的な機能群
using System;
using System.Drawing;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Threading.Tasks;
using System.Windows.Controls; // ComboBox, Button を使うために追加
// カメラフレーム取得用ライブラリ
using AForge.Video;
using AForge.Video.DirectShow;
//QRコードデコード用ライブラリ
using ZXing;
using ZXing.Common;
using ZXing.Windows.Compatibility; // 追加

namespace QrAttendanceScanner
{
    // GDI+のBitmapをWPFのImageコントロールで表示するためのヘルパークラス
    public static class BitmapConverter
    {
        // GDIオブジェクトを解放するためのDLLインポート
        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        // GDI+のBitmapをWPFのBitmapSourceに変換するメソッド(翻訳機)
        public static BitmapSource ToBitmapSource(Bitmap bitmap)
        {
            // BitmapオブジェクトからHBITMAPハンドルを取得
            var hBitmap = bitmap.GetHbitmap();
            try
            {
                // Int32Rect.Emptyは、ビットマップ全体を使用することを示す
                // BitmapSizeOptions.FromEmptyOptions()は、デフォルトのサイズオプションを使用することを示す
                return Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap,// ビットマップハンドル
                    IntPtr.Zero, // パレットハンドル（通常はゼロ、意味：特別な設定は不要）
                    Int32Rect.Empty, // ビットマップ全体を使用(画像の一部切り出しなし)
                    BitmapSizeOptions.FromEmptyOptions());// デフォルトのサイズオプション(画像の拡大、縮小不要)
            }
            finally
            {
                // 使用後は必ずGDIオブジェクトを解放してメモリリークを防止
                DeleteObject(hBitmap);
            }
        }
    }

    public partial class MainWindow : Window
    {
        // カメラ関連フィールド
        private FilterInfoCollection? videoDevices;// 接続されているカメラデバイスのリスト
        private VideoCaptureDevice? videoSource;// 選択されたカメラデバイス
        private ZXing.BarcodeReader<Bitmap>? qrReader;// QRコードリーダー

        // デコード制御用
        private volatile bool isDecoding = false;// デコード中フラグ
        private DateTime lastDecodeTime = DateTime.MinValue;// 最後にデコードを試みた時間
        private readonly TimeSpan decodeInterval = TimeSpan.FromMilliseconds(300); // デコード試行間隔
        private readonly object qrReaderLock = new object();// qrReaderのスレッドセーフ用ロックオブジェクト

        // コンストラクタ
        public MainWindow()
        {
            // 
            InitializeComponent();
            // += でイベントハンドラを登録
            this.Loaded += MainWindow_Loaded;
            this.Closed += MainWindow_Closed;

            // BarcodeReader を一度だけ生成して再利用する
            qrReader = new ZXing.BarcodeReader<Bitmap>(null, (bitmap) => new ZXing.Windows.Compatibility.BitmapLuminanceSource(bitmap), null)
            {
                Options = new DecodingOptions
                {
                    PossibleFormats = new System.Collections.Generic.List<BarcodeFormat> { BarcodeFormat.QR_CODE },
                    TryHarder = false,
                    TryInverted = false
                },
                AutoRotate = false
            };
        }


        // コンストラクタ内でイベントハンドラを登録済み。ウィンドウがロードされたときに呼び出される。
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // アプリ起動時にカメラを列挙してComboBoxに表示する
            EnumerateCameras();
        }

        // PCに接続されているカメラデバイスをすべて検出し、ComboBoxに設定します。
        private void EnumerateCameras()
        {
            try
            {
                // 接続されているカメラデバイスのリストを取得
                videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                if (videoDevices.Count == 0)
                {
                    // カメラが見つからない場合
                    ResultTextBox.Text = "使用可能なカメラが見つかりません。";
                    StartCameraButton.IsEnabled = false; // ボタンを無効化
                    return;
                }

                // 見つかったカメラの名前をComboBoxに追加
                foreach (FilterInfo device in videoDevices)
                {
                    CameraComboBox.Items.Add(device.Name);
                }
                CameraComboBox.SelectedIndex = 0; // 最初のカメラを選択状態にする
                StartCameraButton.IsEnabled = true; // ボタンを有効化
            }
            catch (Exception ex)
            {
                ShowCameraError("カメラの検出中にエラーが発生しました: " + ex.Message);
            }
        }


        // 「スキャン開始」ボタンがクリックされたときに呼び出されます。
        private void StartCameraButton_Click(object sender, RoutedEventArgs e)
        {
            // 既にカメラが起動していれば、一度停止する
            StopCamera();

            if (CameraComboBox.SelectedIndex == -1 || videoDevices == null)
            {
                return; // 選択肢がない、またはカメラリストがなければ何もしない
            }

            try
            {
                // ComboBoxで選択されているカメラの情報を取得
                int selectedIndex = CameraComboBox.SelectedIndex;
                string monikerString = videoDevices[selectedIndex].MonikerString;

                // 選択されたカメラで映像ストリームを開始
                videoSource = new VideoCaptureDevice(monikerString);
                videoSource.NewFrame += new NewFrameEventHandler(VideoSource_NewFrame);
                videoSource.Start();

                // ボタンの文言を変更
                StartCameraButton.Content = "カメラ切替";
            }
            catch (Exception ex)
            {
                ShowCameraError($"カメラの起動に失敗しました: {ex.Message}");
            }
        }


        // 現在動作しているカメラを安全に停止します。
        private void StopCamera()
        {
            if (videoSource != null && videoSource.IsRunning)
            {
                videoSource.SignalToStop();
                // NewFrameイベントの購読を解除 (重要)
                videoSource.NewFrame -= VideoSource_NewFrame;
                videoSource.WaitForStop();
                videoSource = null;
            }
        }

        // アプリが終了する直前にカメラを停止するためのイベントハンドラ
        private void MainWindow_Closed(object? sender, EventArgs e)
        {
            // アプリケーション終了時にカメラを停止
            StopCamera();
        }

        // カメラが新しいフレームを提供したときに呼び出されるイベントハンドラ
        private void VideoSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            Bitmap originalBitmap = (Bitmap)eventArgs.Frame.Clone();
            originalBitmap.RotateFlip(RotateFlipType.RotateNoneFlipX); // 左右反転

            Bitmap uiBitmap = (Bitmap)originalBitmap.Clone();
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                // 1. 映像をWPFのImageコントロールに表示
                CameraImage.Source = BitmapConverter.ToBitmapSource(uiBitmap);

                // BitmapSource化が完了したらBitmapを破棄
                uiBitmap.Dispose();
            }));

            // 2. デコードはバックグラウンドで行う（スロットリング）
            var now = DateTime.UtcNow;
            if (!isDecoding && (now - lastDecodeTime) > decodeInterval)
            {
                isDecoding = true;
                lastDecodeTime = now;
                Bitmap decodeBitmap = originalBitmap;
                Task.Run(() =>
                {
                    try
                    {
                        DecodeQrCode(decodeBitmap);
                    }
                    finally
                    {
                        // DecodeQrCodeではBitmapを破棄しないためここで破棄
                        try { decodeBitmap.Dispose(); } catch { }
                        isDecoding = false;
                    }
                });
            }
            else
            {
                originalBitmap.Dispose();
            }
        }


        // エラー表示用メソッド
        #region Unchanged Methods
        private void ShowCameraError(string message)
        {
            var textBlock = new System.Windows.Controls.TextBlock
            {
                Text = "カメラに接続できません。\n" + message,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                FontSize = 16,
                Foreground = System.Windows.Media.Brushes.Gray
            };

            if (CameraImage.Parent is System.Windows.Controls.Border parentBorder)
            {
                parentBorder.Child = textBlock;
                parentBorder.Background = System.Windows.Media.Brushes.White;
            }
            else
            {
                CameraImage.Source = null;
                ResultTextBox.Text = message;
            }

            ResultTextBox.Text = message;
        }

        // 受け取ったBitmapからQRコードをデコードし、結果をUIに出力します。
        private void DecodeQrCode(Bitmap bitmap)
        {
            try
            {
                if (qrReader == null) return;

                // スレッドセーフにqrReaderを使用
                Result? result = null;
                lock (qrReaderLock)
                {
                    result = qrReader.Decode(bitmap);
                }

                if (result != null)
                {
                    string scannedData = result.Text;

                    // UIスレッドで結果表示と後処理を行う
                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        ResultTextBox.Text = scannedData;
                        ProcessAttendance(scannedData);
                    }));
                }
            }
            catch (Exception)
            {
                // デコード中に発生する可能性のあるエラーは無視する
            }
        }

        /// スキャン成功時にTextBoxの背景色を一時的に変更し、フィードバックを与える
        private async void ShowSuccessFeedbackAsync(string data)
        {
            // C#でWPFのBrushを定義
            var successColor = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 144, 238, 144)); // LightGreen
            var initialColor = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 239, 239, 239)); // #EFEFEF

            // 1. テキストボックスの背景色を成功色に変更
            ResultTextBox.Background = successColor;
            ResultTextBox.Foreground = System.Windows.Media.Brushes.White;
            ResultTextBox.Text = $"✅ 出席確認完了: {data}";

            // 2. 1.5秒待機
            await Task.Delay(1500);

            // 3. 元の色に戻す
            ResultTextBox.Background = initialColor;
            ResultTextBox.Foreground = System.Windows.Media.Brushes.Black;
            ResultTextBox.Text = "QRコードをスキャンしてください...";
        }

        // スキャンされたデータを受け取り、出席管理のロジックを実行します。
        /// <param name="data">スキャンされたQRコードのデータ</param>
        private void ProcessAttendance(string data)
        {
            // ここに、出席管理アプリのコアロジックを実装します。
            // 例: スキャン成功後の確認メッセージやデータベースへの記録など
            // ★★★ 修正箇所: スキャンされた情報をポップアップで出力 ★★★

            // UI/UX改善ステップで追加したフィードバックを呼び出す (成功時の緑色表示など)
            ShowSuccessFeedbackAsync(data);

            // QRコードの内容を確認するためのポップアップ表示
            MessageBox.Show(
                $"出席データを受信しました:\n\n{data}",
                "✅ QRコードスキャン成功",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }

        // ... (MainWindowクラスの終わり)
    }

    #endregion

}