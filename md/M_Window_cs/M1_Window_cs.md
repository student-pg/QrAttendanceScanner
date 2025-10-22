using System;<br>
using System.Drawing;<br>
using System.Windows;<br>
// ...�Ȃ�<br>
using AForge.Video.DirectShow;<br>
using ZXing;

#### **���p�����F`using`�f�B���N�e�B�u**
`using`�́A���̃v���O���}�[��������֗��ȋ@�\�i�N���X�⃁�\�b�h�j���u���̃v���O�����Ŏg���܂���v�Ɛ錾���邽�߂̂��̂ł��B�Ⴆ��Ȃ�A�������n�߂�O�ɁA�g���������i��A�܂ȔA�t���C�p���j���L�b�`����ɕ��ׂĂ����悤�ȃC���[�W�ł��B

���̃R�[�h�ł́A��Ɉȉ��̃O���[�v�̓�����������Ă��܂��B

* **`System`����n�܂����**: C#�̍ł���{�I�ȋ@�\�Q�ł��B���t(`DateTime`)��摜(`Bitmap`)�A��ʕ\��(`Window`)�ȂǁA������v���O�����̓y��ƂȂ�܂��B
* **`AForge.Video.DirectShow`**: �p�\�R���ɐڑ����ꂽ�J�����𑀍삷�邽�߂̊O�����C�u�����i����j�ł��B���̂������ŁA���G�ȃJ����������r�I�ȒP�ȃR�[�h�Ŏ����ł��܂��B
* **`ZXing`**: QR�R�[�h��o�[�R�[�h����́i�f�R�[�h�j���邽�߂́A���ɗL���ȊO�����C�u�����ł��B�J�������������摜����QR�R�[�h�������o���A���̒��Ɋ܂܂�镶�����iURL�Ȃǁj��ǂݎ���Ă���܂��B

�����āA`namespace QrAttendanceScanner`�́A���ꂩ����v���O�����S�̂��uQrAttendanceScanner�v�Ƃ������O�̔��ɓ����A�Ƃ����錾�ł��B����ɂ��A���̃v���O�����Ɩ��O������Ă��܂��̂�h���܂��B

---

### 2. `BitmapConverter`�N���X�i�قȂ�摜���q���u�|��@�v�j

���ɁA`BitmapConverter`�Ƃ��������ς�����N���X�����Ă����܂��傤�B����͂��̃A�v���̉��̉��̗͎����ł��B

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

#### **�Ȃ����̃N���X���K�v�Ȃ̂��H**

���̃A�v����**WPF (Windows Presentation Foundation)** �Ƃ����Z�p�ō���Ă��܂��B<br>
WPF�͔�r�I�V������ʕ\���̎d�g�݂ŁA�摜�������Ƃ���`BitmapSource`�Ƃ����`�����g���܂��B

����ŁA`AForge`���C�u�������J��������擾���Ă���摜�̌`���́A�Â�����g���Ă���<br>
`System.Drawing.Bitmap`�i`Bitmap`�Ɨ�����܂��j�ł��B

�܂�A
* **�J�����������摜�`��**: `Bitmap`
* **WPF�̉�ʂɕ\���ł���摜�`��**: `BitmapSource`

����2��**�݊������Ȃ��A���̂܂܂ł͕\���ł��܂���**�B�����ŁA`Bitmap`��`BitmapSource`�ɕϊ�����<bt>
�u**�|��@**�v���K�v�ɂȂ�܂��B���ꂪ����`BitmapConverter`�N���X�̖����ł��B


#### **�R�[�h�̐[�@��**

```csharp
[System.Runtime.InteropServices.DllImport("gdi32.dll")]
public static extern bool DeleteObject(IntPtr hObject);
```

����͏������x�ȕ����ł��BC#�͒ʏ�A���S�Ȋ��œ��삵�܂����A�����ł�Windows�V�X�e���̐S�����ɋ߂��@�\<br>
�i`gdi32.dll`�Ƃ����t�@�C���̒��ɂ���`DeleteObject`�Ƃ����@�\�j�𒼐ڌĂяo���Ă��܂��B<br>
`DllImport`�����́AC#���璼��Windows��API���Ăяo����悤�ɂ��܂��B

`Bitmap`�摜��`BitmapSource`�ɕϊ�����ہA�ꎞ�I��OS�iWindows�j�̃�������ɉ摜�f�[�^���쐬���܂��B<br>
C#�́A�����Ŏg�����������͎����I�ɑ|�����Ă���܂����AOS�ɒ��ڂ��肢���č���Ă�������������́A<br>
�����Łu**�|�����Ă�������**�v�Ƃ��肢���Ȃ��ƁA�S�~�Ƃ��Ďc�葱���Ă��܂��܂��B<br>
����`DeleteObject`�́A���̂��|�������肢���邽�߂̖��߂ł��B
(**���������[�N** �΍�)

* `extern`�L�[���[�h�́A���̃��\�b�h���O���Œ�`����Ă��邱�Ƃ�����
* `bool`�́A���̊֐��������������ǂ����������^�U�l��Ԃ����Ƃ��Ӗ�����
* `IntPtr hObject`�́A�폜������GDI�I�u�W�F�N�g�̃n���h���i�|�C���^�j���󂯎������ł��邱�Ƃ�����


����: .NET 6�ȍ~�ł́A`System.Runtime.InteropServices`���O��Ԃ��K�v�ł�<br>
����: 64�r�b�g����32�r�b�g���ł̃|�C���^�T�C�Y�̈Ⴂ�ɒ��ӂ��Ă�������<br>
����: ���̃R�[�h��WPF�A�v���P�[�V�����ł̂ݎg�p���Ă��������B<br>
WinForms�A�v���P�[�V�����ł͈قȂ���@�ŉ摜�������܂�<br>
����: GDI�I�u�W�F�N�g�̉���́A�g�p�シ���ɍs�����Ƃ���������܂�<br>
����: ���̃R�[�h��Windows�̓���̃o�[�W�����ł̂ݓ��삷��\��������܂�<br>

�Q�l: https://learn.microsoft.com/ja-jp/dotnet/api/system.intptr <br>
�Q�l: https://learn.microsoft.com/ja-jp/dotnet/api/system.runtime.interopservices.dllimportattribute

```csharp
public static BitmapSource ToBitmapSource(Bitmap bitmap)
{
    var hBitmap = bitmap.GetHbitmap(); // 1. OS�̃�������ɉ摜���쐬
    try
    {
        // 2. OS��̉摜����WPF�Ŏg����摜�֕ϊ�
        return Imaging.CreateBitmapSourceFromHBitmap(...);
    }
    finally
    {
        // 3. �ϊ����I�������A�K��OS�̃�������|��
        DeleteObject(hBitmap);
    }
}
```
���̃��\�b�h�́A`Bitmap`��`BitmapSource`�ɕϊ����邽�߂̂��̂ł��B<br>
1. **`bitmap.GetHbitmap()`**: �����ŁAOS�̃��������`Bitmap`�摜���쐬���A<br>
���̏ꏊ���w���n���h���i`hBitmap`�j���擾���܂��B
1. **`Imaging.CreateBitmapSourceFromHBitmap(...)`**: �擾����`hBitmap`���g���āA<br>
WPF�Ŏg����`BitmapSource`�摜�𐶐����܂��B
1. **`DeleteObject(hBitmap)`**: �Ō�ɁA`finally`�u���b�N�̒��ŁA<br>
�ō쐬����OS�̃�������K���|�����܂��B

* **`try...finally`�\��**: `try`�̒��̏����ŁA�����G���[�����������Ƃ��Ă��A<br>
`finally`�̒��ɏ����ꂽ������**�K�����s�����**�A�Ƃ������ɏd�v�ȍ\���ł��B<br>
�����ł́A�ϊ��Ɏ��s���Ă���΂Ƀ������̑|���͍s���A�Ƃ��������ӎu�̕\��ł��B<br>
���ꂪ�Ȃ��ƁA�A�v���𒷎��ԓ������Ă���ƃ��������[�N�i�S�~�����܂葱����PC���d���Ȃ錻�ہj�̌����ɂȂ�܂��B

---

### 3. `MainWindow`�N���X�̏����ƃR���X�g���N�^�i�A�v���N�����̏����ݒ�j

���悢�惁�C����`MainWindow`�N���X�ł��B���ꂪ�A�v���̉�ʂ��̂��̂�S�����܂��B

```csharp
public partial class MainWindow : Window
{
    // ... �t�B�[���h�i�N���X���g���ϐ��j ...

    public MainWindow()
    {
        // ... �R���X�g���N�^�i�����������j ...
    }
}

#### **�t�B�[���h�i�N���X�������ŕێ�������j**

`MainWindow`�N���X�̒����Ő錾����Ă���ϐ���**�t�B�[���h**�ƌĂт܂��B�����́A`MainWindow`�����삷�邽�߂ɕK�v�ȏ����o���Ă������߂̂��̂ł��B

* `videoDevices`: PC�ɐڑ�����Ă���J�����̈ꗗ��ۑ����܂��B
* `videoSource`: `videoDevices`�̒�������ۂɎg�p����J������ۑ����܂��B
* `qrReader`: QR�R�[�h��ǂݎ�邽�߂�`ZXing`���C�u�����̖{�̂ł��B
* `isDecoding`, `lastDecodeTime`, `decodeInterval`: �����̓p�t�H�[�}���X���œK�����邽�߂̕ϐ��ł��B�J���������1�b�Ԃ�30�����̉摜�������Ă��܂����A����QR�R�[�h��͂������PC�ɑ傫�ȕ��ׂ�������܂��B�����ŁA�u���݉�͒����H�v�u�Ō�ɉ�͂����̂͂����H�v���L�^���Ă����A���Ԋu�i���̃R�[�h�ł�300�~���b�j���󂯂ĉ�͂���悤�ɐ��䂵�Ă��܂��B
* `qrReaderLock`: �����̏�����������`qrReader`���g�����Ƃ��Ė�肪�N����̂�h�����߂́u���v�ł��B�i�ڍׂ͌�̉�ŉ�����܂��j

#### **�R���X�g���N�^ (`public MainWindow()`)**

�R���X�g���N�^�́A�N���X���������ꂽ�Ƃ��i���̏ꍇ�̓A�v���̃E�B���h�E���\������钼�O�j��**��x����**�Ă΂����ʂȃ��\�b�h�ł��B�����ł̓A�v�������삷�邽�߂̉��������s���܂��B

```csharp
public MainWindow()
{
    // 1. WPF��ʂ̕��i��������
    InitializeComponent();

    // 2. �C�x���g�n���h����o�^
    this.Loaded += MainWindow_Loaded;
    this.Closed += MainWindow_Closed;

    // 3. QR�R�[�h���[�_�[��������
    qrReader = new ZXing.BarcodeReader<Bitmap>(...) { ... };
}

1.  **`InitializeComponent()`**: XAML�i��ʂ̃f�U�C�����L�q����t�@�C���j�Œ�`���ꂽ�{�^����摜�\���G���A�Ȃǂ̕��i���A�v���O�����Ŏg����悤�ɏ��������邨�܂��Ȃ��ł��B
2.  **�C�x���g�n���h���̓o�^**:
    * `this.Loaded += ...`: �u�E�B���h�E�̕\��������**��������**�v�Ƃ����C�x���g������������A`MainWindow_Loaded`���\�b�h���Ăяo���悤�ɗ\�񂵂܂��B
    * `this.Closed += ...`: �u�E�B���h�E��**����ꂽ**�v�Ƃ����C�x���g������������A`MainWindow_Closed`���\�b�h���Ăяo���悤�ɗ\�񂵂܂��B
    * **���p�����F�C�x���g�ƃC�x���g�n���h��**
        �v���O�����̐��E�ł́A�{�^�����N���b�N���ꂽ�A�E�B���h�E���\�����ꂽ�A�ȂǗl�X�ȁu�o�����i**�C�x���g**�j�v���������܂��B���̃C�x���g�ɑΉ����ē���̏������s�����\�b�h���u**�C�x���g�n���h��**�v�ƌĂт܂��B`+=`�́A�C�x���g�ɃC�x���g�n���h�����u�o�^����v�Ƃ����Ӗ��ł��B

3.  **QR�R�[�h���[�_�[�̏�����**: `ZXing`��`BarcodeReader`�������Ő������Ă��܂��B
    * `PossibleFormats = { BarcodeFormat.QR_CODE }`: �ǂݎ��o�[�R�[�h�̎�ނ�QR�R�[�h�Ɍ��肵�Ă��܂��B����ɂ��A�]�v�ȉ�͂��Ȃ��A�p�t�H�[�}���X�����コ���Ă��܂��B
    * `TryHarder = false`: ��荂�x�ȁi���������Ԃ�������j��͂𖳌��ɂ��Ă��܂��B������p�t�H�[�}���X�̂��߂ł��B
    * �A�v���̋N�����Ɉ�x�����������A�t�B�[���h��`qrReader`�ɕۑ����Ă������ƂŁA�摜����͂��邽�тɐ������閳�ʂ��Ȃ��Ă��܂��B����͔��ɗǂ��݌v�ł��B

---

### ��1��̂܂Ƃ�

����́A�v���O���������ۂɓ����o���O�́u**�����i�K**�v�ɏœ_�𓖂Ăĉ�����܂����B

* `using`�ŕK�v�ȓ���i���C�u�����j�𑵂���B
* WPF�ƃJ�������C�u�����̉摜�`���̈Ⴂ���z�����邽�߁A`BitmapConverter`�Ƃ����|��@��p�ӂ���B
* �A�v���̃��C�����(`MainWindow`)���N������Ƃ��ɁAQR�R�[�h���[�_�[�̏�����A����̓���i��ʕ\���������A��ʏI�����j�̗\����s���B

�����̓y��̏�ɁA�J�����𓮂����A���A���^�C���ŉ摜���������Ă������W�b�N���\�z����Ă����܂��B

����**�y��2��z**�ł́A���悢��**�J�������N�����A���̉f������ʂɕ\�����镔�� (`InitializeCamera`��`VideoSource_NewFrame`)** �ɂ��ďڂ������Ă����܂��B

����̓��e�ŁA�����^��Ɏv�����_��A�����Əڂ����m�肽������������΁A���C�y�Ɏ��₵�Ă��������ˁI