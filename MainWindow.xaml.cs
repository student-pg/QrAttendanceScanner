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
        // Windowsシステムの心臓部に近い機能（`gdi32.dll`というファイルの中にある`DeleteObject`という機能）を直接呼び出す
        // これを使わないと、メモリリーク（不要なメモリが解放されずに溜まっていく現象）が発生する可能性がある
        // `DllImport`属性は、C#のコードとネイティブコード（C/C++で書かれたコード）を橋渡しするためのもの
        // つまり、C#から直接WindowsのAPIを呼び出せるようにする
        // `extern`キーワードは、このメソッドが外部で定義されていることを示す
        // `bool`は、この関数が成功したかどうかを示す真偽値を返すことを意味する
        // 注意: .NET 6以降では、`System.Runtime.InteropServices`名前空間が必要です
        // 注意: 64ビット環境と32ビット環境でのポインタサイズの違いに注意してください
        // 注意: このコードはWPFアプリケーションでのみ使用してください。WinFormsアプリケーションでは異なる方法で画像を扱います
        // 注意: GDIオブジェクトの解放は、使用後すぐに行うことが推奨されます
        // 注意: このコードはWindowsの特定のバージョンでのみ動作する可能性があります
        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        // DeleteObject関数の宣言その引数について
        // `IntPtr`はポインタを表す型で、C#でネイティブリソースを扱う際に使われる
        // hObject: GDIが管理するグラフィックオブジェクト（画像、ペン、ブラシなど）のハンドル。
        // <param name="hObject">解放するGDIオブジェクトのハンドル</param>
        // 参考: https://learn.microsoft.com/ja-jp/dotnet/api/system.intptr
        // 参考: https://learn.microsoft.com/ja-jp/dotnet/api/system.runtime.interopservices.dllimportattribute
        public static extern bool DeleteObject(IntPtr hObject);

        // GDI+のBitmapをWPFのBitmapSourceに変換するメソッド(翻訳機)
        // <param name="bitmap">変換するGDI+のBitmapオブジェクト</param>
        public static BitmapSource ToBitmapSource(Bitmap bitmap)
        {
            // BitmapオブジェクトからHBITMAPハンドルを取得
            // GetHbitmapメソッドは、bitmapオブジェクトからWindowsのネイティブなビットマップオブジェクト（HBITMAP）を取得する
            var hBitmap = bitmap.GetHbitmap();
            try
            {
                // CreateBitmapSourceFromHBitmapメソッドは、HBITMAPハンドルをWPFのBitmapSourceオブジェクトに変換する
                // Int32Rect.Emptyは、ビットマップ全体を使用することを示す
                // BitmapSizeOptions.FromEmptyOptions()は、デフォルトのサイズオプションを使用することを示す
                // 参考: https://learn.microsoft.com/ja-jp/dotnet/api/system.windows.interop.imaging.createbitmapsourcefromhbitmap
                // 参考: https://learn.microsoft.com/ja-jp/dotnet/api/system.windows.int32rect
                // 参考: https://learn.microsoft.com/ja-jp/dotnet/api/system.windows.media.imaging.bitmapsizeoptions
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
        private FilterInfoCollection? videoDevices;
        private VideoCaptureDevice? videoSource;
        private ZXing.BarcodeReader<Bitmap>? qrReader;

        // デコード制御用
        private volatile bool isDecoding = false;
        private DateTime lastDecodeTime = DateTime.MinValue;
        private readonly TimeSpan decodeInterval = TimeSpan.FromMilliseconds(300); // 200msごとに1回デコード
        private readonly object qrReaderLock = new object();

        public MainWindow()
        {
            InitializeComponent();
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

        // =================================================================
        // ★★★ 変更点 1: ウィンドウロード時の処理 ★★★
        // =================================================================
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // アプリ起動時にカメラを列挙してComboBoxに表示する
            EnumerateCameras();
        }

        /// <summary>
        /// PCに接続されているカメラデバイスをすべて検出し、ComboBoxに設定します。
        /// </summary>
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

        // =================================================================
        // ★★★ 変更点 2: ボタンクリック時の処理（新規作成） ★★★
        // =================================================================
        /// <summary>
        /// 「スキャン開始」ボタンがクリックされたときに呼び出されます。
        /// </summary>
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

        // =================================================================
        // ★★★ 変更点 3: カメラ停止処理（新規作成＆共通化） ★★★
        // =================================================================
        /// <summary>
        /// 現在動作しているカメラを安全に停止します。
        /// </summary>
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

        private void MainWindow_Closed(object? sender, EventArgs e)
        {
            // アプリケーション終了時にカメラを停止
            StopCamera();
        }

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

        // (ShowCameraError, DecodeQrCode, ProcessAttendanceなどのメソッドは変更なし)
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

        /// <summary>
        /// 受け取ったBitmapからQRコードをデコードし、結果をUIに出力します。
        /// </summary>
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

        /// <summary>
        /// スキャン成功時にTextBoxの背景色を一時的に変更し、フィードバックを与える
        /// </summary>
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

        /// <summary>
        /// スキャンされたデータを受け取り、出席管理のロジックを実行します。
        /// </summary>
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