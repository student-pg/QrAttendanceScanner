using System;
using System.Drawing;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using ZXing;
using ZXing.Common;
using ZXing.Windows.Compatibility;

namespace QrAttendanceScanner.Benchmarks
{
    [MemoryDiagnoser]
    public class DecodeBenchmark
    {
        private Bitmap qrBitmap;
        private BarcodeReader<Bitmap> reader;
        [GlobalSetup]
        public void Setup()
        {
            // QRコードを作成してBitmapに変換
            var writer = new BarcodeWriterPixelData
            {
                Format = BarcodeFormat.QR_CODE,
                Options = new EncodingOptions
                {
                    Height = 300,
                    Width = 300,
                    Margin = 1
                }
            };
            var pixelData = writer.Write("BenchmarkTest:1234567890");
            qrBitmap = new Bitmap(pixelData.Width, pixelData.Height, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
            var bmpData = qrBitmap.LockBits(new Rectangle(0, 0, pixelData.Width, pixelData.Height), System.Drawing.Imaging.ImageLockMode.WriteOnly, qrBitmap.PixelFormat);
            try
            {
                Marshal.Copy(pixelData.Pixels, 0, bmpData.Scan0, pixelData.Pixels.Length);
            }
            finally
            {
                qrBitmap.UnlockBits(bmpData);
            }

            // デコード用のリーダー（Bitmap向け）を初期化
            reader = new BarcodeReader<Bitmap>(null, (bitmap) => new BitmapLuminanceSource(bitmap), null)
            {
                Options = new DecodingOptions
                {
                    PossibleFormats = new System.Collections.Generic.List<BarcodeFormat>
                    {
                        BarcodeFormat.QR_CODE
                    },
                    TryHarder = false
                }
            };
        }

        [Benchmark]
        public string DecodeBitmap()
        {
            var result = reader.Decode(qrBitmap);
            return result?.Text ?? string.Empty;
        }
    }
}