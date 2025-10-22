using System;<br>
using System.Drawing;<br>
using System.Windows;<br>
// ...など<br>
using AForge.Video.DirectShow;<br>
using ZXing;

#### **専門用語解説：`using`ディレクティブ**
`using`は、他のプログラマーが作った便利な機能（クラスやメソッド）を「このプログラムで使いますよ」と宣言するためのものです。例えるなら、料理を始める前に、使う調理器具（包丁、まな板、フライパン）をキッチン台に並べておくようなイメージです。

このコードでは、主に以下のグループの道具を準備しています。

* **`System`から始まるもの**: C#の最も基本的な機能群です。日付(`DateTime`)や画像(`Bitmap`)、画面表示(`Window`)など、あらゆるプログラムの土台となります。
* **`AForge.Video.DirectShow`**: パソコンに接続されたカメラを操作するための外部ライブラリ（道具箱）です。このおかげで、複雑なカメラ制御を比較的簡単なコードで実現できます。
* **`ZXing`**: QRコードやバーコードを解析（デコード）するための、非常に有名な外部ライブラリです。カメラが捉えた画像からQRコードを見つけ出し、その中に含まれる文字情報（URLなど）を読み取ってくれます。

そして、`namespace QrAttendanceScanner`は、これから作るプログラム全体を「QrAttendanceScanner」という名前の箱に入れる、という宣言です。これにより、他のプログラムと名前が被ってしまうのを防ぎます。

---

### 2. `BitmapConverter`クラス（異なる画像を繋ぐ「翻訳機」）

次に、`BitmapConverter`という少し変わったクラスを見ていきましょう。これはこのアプリの縁の下の力持ちです。

```csharp
public static class BitmapConverter
{
    // ...
    public static BitmapSource ToBitmapSource(Bitmap bitmap)
    {
        // ...
    }
}
```

#### **なぜこのクラスが必要なのか？**

このアプリは**WPF (Windows Presentation Foundation)** という技術で作られています。<br>
WPFは比較的新しい画面表示の仕組みで、画像を扱うときは`BitmapSource`という形式を使います。

一方で、`AForge`ライブラリがカメラから取得してくる画像の形式は、古くから使われている<br>
`System.Drawing.Bitmap`（`Bitmap`と略されます）です。

つまり、
* **カメラがくれる画像形式**: `Bitmap`
* **WPFの画面に表示できる画像形式**: `BitmapSource`

この2つは**互換性がなく、そのままでは表示できません**。そこで、`Bitmap`を`BitmapSource`に変換する<bt>
「**翻訳機**」が必要になります。それがこの`BitmapConverter`クラスの役割です。


#### **コードの深掘り**

```csharp
[System.Runtime.InteropServices.DllImport("gdi32.dll")]
public static extern bool DeleteObject(IntPtr hObject);
```

これは少し高度な部分です。C#は通常、安全な環境で動作しますが、ここではWindowsシステムの心臓部に近い機能<br>
（`gdi32.dll`というファイルの中にある`DeleteObject`という機能）を直接呼び出しています。<br>
`DllImport`属性は、C#から直接WindowsのAPIを呼び出せるようにします。

`Bitmap`画像を`BitmapSource`に変換する際、一時的にOS（Windows）のメモリ上に画像データを作成します。<br>
C#は、自分で使ったメモリは自動的に掃除してくれますが、OSに直接お願いして作ってもらったメモリは、<br>
自分で「**掃除してください**」とお願いしないと、ゴミとして残り続けてしまいます。<br>
この`DeleteObject`は、そのお掃除をお願いするための命令です。
(**メモリリーク** 対策)

* `extern`キーワードは、このメソッドが外部で定義されていることを示す
* `bool`は、この関数が成功したかどうかを示す真偽値を返すことを意味する
* `IntPtr hObject`は、削除したいGDIオブジェクトのハンドル（ポインタ）を受け取る引数であることを示す


注意: .NET 6以降では、`System.Runtime.InteropServices`名前空間が必要です<br>
注意: 64ビット環境と32ビット環境でのポインタサイズの違いに注意してください<br>
注意: このコードはWPFアプリケーションでのみ使用してください。<br>
WinFormsアプリケーションでは異なる方法で画像を扱います<br>
注意: GDIオブジェクトの解放は、使用後すぐに行うことが推奨されます<br>
注意: このコードはWindowsの特定のバージョンでのみ動作する可能性があります<br>

参考: https://learn.microsoft.com/ja-jp/dotnet/api/system.intptr <br>
参考: https://learn.microsoft.com/ja-jp/dotnet/api/system.runtime.interopservices.dllimportattribute

```csharp
public static BitmapSource ToBitmapSource(Bitmap bitmap)
{
    var hBitmap = bitmap.GetHbitmap(); // 1. OSのメモリ上に画像を作成
    try
    {
        // 2. OS上の画像からWPFで使える画像へ変換
        return Imaging.CreateBitmapSourceFromHBitmap(...);
    }
    finally
    {
        // 3. 変換が終わったら、必ずOSのメモリを掃除
        DeleteObject(hBitmap);
    }
}
```
このメソッドは、`Bitmap`を`BitmapSource`に変換するためのものです。<br>
1. **`bitmap.GetHbitmap()`**: ここで、OSのメモリ上に`Bitmap`画像を作成し、<br>
その場所を指すハンドル（`hBitmap`）を取得します。
1. **`Imaging.CreateBitmapSourceFromHBitmap(...)`**: 取得した`hBitmap`を使って、<br>
WPFで使える`BitmapSource`画像を生成します。
1. **`DeleteObject(hBitmap)`**: 最後に、`finally`ブロックの中で、<br>
で作成したOSのメモリを必ず掃除します。

* **`try...finally`構文**: `try`の中の処理で、もしエラーが発生したとしても、<br>
`finally`の中に書かれた処理は**必ず実行される**、という非常に重要な構文です。<br>
ここでは、変換に失敗しても絶対にメモリの掃除は行う、という強い意志の表れです。<br>
これがないと、アプリを長時間動かしているとメモリリーク（ゴミが溜まり続けてPCが重くなる現象）の原因になります。

---

### 3. `MainWindow`クラスの準備とコンストラクタ（アプリ起動時の初期設定）

いよいよメインの`MainWindow`クラスです。これがアプリの画面そのものを担当します。

```csharp
public partial class MainWindow : Window
{
    // ... フィールド（クラスが使う変数） ...

    public MainWindow()
    {
        // ... コンストラクタ（初期化処理） ...
    }
}

#### **フィールド（クラスが内部で保持する情報）**

`MainWindow`クラスの直下で宣言されている変数を**フィールド**と呼びます。これらは、`MainWindow`が動作するために必要な情報を覚えておくためのものです。

* `videoDevices`: PCに接続されているカメラの一覧を保存します。
* `videoSource`: `videoDevices`の中から実際に使用するカメラを保存します。
* `qrReader`: QRコードを読み取るための`ZXing`ライブラリの本体です。
* `isDecoding`, `lastDecodeTime`, `decodeInterval`: これらはパフォーマンスを最適化するための変数です。カメラからは1秒間に30枚もの画像が送られてきますが、毎回QRコード解析をするとPCに大きな負荷がかかります。そこで、「現在解析中か？」「最後に解析したのはいつか？」を記録しておき、一定間隔（このコードでは300ミリ秒）を空けて解析するように制御しています。
* `qrReaderLock`: 複数の処理が同時に`qrReader`を使おうとして問題が起きるのを防ぐための「鍵」です。（詳細は後の回で解説します）

#### **コンストラクタ (`public MainWindow()`)**

コンストラクタは、クラスが生成されたとき（この場合はアプリのウィンドウが表示される直前）に**一度だけ**呼ばれる特別なメソッドです。ここではアプリが動作するための下準備を行います。

```csharp
public MainWindow()
{
    // 1. WPF画面の部品を初期化
    InitializeComponent();

    // 2. イベントハンドラを登録
    this.Loaded += MainWindow_Loaded;
    this.Closed += MainWindow_Closed;

    // 3. QRコードリーダーを初期化
    qrReader = new ZXing.BarcodeReader<Bitmap>(...) { ... };
}

1.  **`InitializeComponent()`**: XAML（画面のデザインを記述するファイル）で定義されたボタンや画像表示エリアなどの部品を、プログラムで使えるように初期化するおまじないです。
2.  **イベントハンドラの登録**:
    * `this.Loaded += ...`: 「ウィンドウの表示準備が**完了した**」というイベントが発生したら、`MainWindow_Loaded`メソッドを呼び出すように予約します。
    * `this.Closed += ...`: 「ウィンドウが**閉じられた**」というイベントが発生したら、`MainWindow_Closed`メソッドを呼び出すように予約します。
    * **専門用語解説：イベントとイベントハンドラ**
        プログラムの世界では、ボタンがクリックされた、ウィンドウが表示された、など様々な「出来事（**イベント**）」が発生します。そのイベントに対応して特定の処理を行うメソッドを「**イベントハンドラ**」と呼びます。`+=`は、イベントにイベントハンドラを「登録する」という意味です。

3.  **QRコードリーダーの初期化**: `ZXing`の`BarcodeReader`をここで生成しています。
    * `PossibleFormats = { BarcodeFormat.QR_CODE }`: 読み取るバーコードの種類をQRコードに限定しています。これにより、余計な解析を省き、パフォーマンスを向上させています。
    * `TryHarder = false`: より高度な（しかし時間がかかる）解析を無効にしています。これもパフォーマンスのためです。
    * アプリの起動時に一度だけ生成し、フィールドの`qrReader`に保存しておくことで、画像を解析するたびに生成する無駄を省いています。これは非常に良い設計です。

---

### 第1回のまとめ

今回は、プログラムが実際に動き出す前の「**準備段階**」に焦点を当てて解説しました。

* `using`で必要な道具（ライブラリ）を揃える。
* WPFとカメラライブラリの画像形式の違いを吸収するため、`BitmapConverter`という翻訳機を用意する。
* アプリのメイン画面(`MainWindow`)が起動するときに、QRコードリーダーの準備や、今後の動作（画面表示完了時、画面終了時）の予約を行う。

これらの土台の上に、カメラを動かし、リアルタイムで画像を処理していくロジックが構築されていきます。

次回**【第2回】**では、いよいよ**カメラを起動し、その映像を画面に表示する部分 (`InitializeCamera`と`VideoSource_NewFrame`)** について詳しく見ていきます。

今回の内容で、もし疑問に思った点や、もっと詳しく知りたい部分があれば、お気軽に質問してくださいね！