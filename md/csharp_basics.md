## C# �̊�{�I�ȍ\���ɂ��Ẳ��

### 1. public partial class MainWindow : Window

```csharp
public partial class MainWindow : Window
```
���̍s�́AC#�ŃN���X���`���邽�߂̊�{�I�ȍ\���ł��B<br>
1. `public`�A�N�Z�X�C���q<br>
	* **�L�[���[�h**: `public`<br>
	* **����**: �A�N�Z�X�C���q<br>
	* **�Ӗ�**: ���̃N���X�����̃N���X�▼�O��Ԃ���A�N�Z�X�\�ł��邱�Ƃ������܂��B<br>

	**���**:�u����͓��������Ŏg���厖�ȕ��i������A�O����͐G��Ȃ��Łiprivate�j�v��A<br>
�u�N�ł��g���Ă����ł���ipublic�j�v�Ƃ��������J�͈͂����߂�K�v������܂��B<br>
MainWindow�́A�A�v���P�[�V�����iApp.xaml�j����u�ŏ��ɋN������E�B���h�E�v<br>
�Ƃ��Ďw�肳��邽�߁A�K���ł���public�K�v������܂��B<br>

1. `partial`�L�[���[�h<br>
	* **�L�[���[�h**: `partial`<br>
	* **����**: �N���X�C���q<br>
	* **�Ӗ�**: ���̃N���X�̒�`�������̃t�@�C���ɕ�������Ă��邱�Ƃ������܂��B<br>

	**���**: WPF�ł́AXAML�t�@�C���Œ�`���ꂽUI�v�f�ƁA���̔w��ɂ���<br>
    �R�[�h�r�n�C���h�t�@�C�����A�g���ē��삵�܂��B`partial`�L�[���[�h<br>
    ���g�p���邱�ƂŁAXAML�Œ�`���ꂽ�����ƃR�[�h�r�n�C���h�Œ�`���ꂽ������<br>
	�����N���X�Ƃ��Ĉ������Ƃ��ł��܂��B<br>
    * `MainWindow.xaml`:�u�����ځv�̐݌v�}�B������`<Button>`�Ȃǂ�UI�v�f��z�u���܂��B<br>
	* `MainWindow.xaml.cs`:�u����v�̃R�[�h�B�����Ƀ{�^���������ꂽ�Ƃ��̏����Ȃǂ������܂��B<br>
    * `MainWindow.g.i.cs`: �������������R�[�h�BXAML�̓��e��C#�R�[�h�ɕϊ��������̂ł��B<br>�����̃t�@�C���͎蓮�ŕҏW���܂���B<br>
	���Ȃ���`MainWindow.xaml`��`<ComboBox x:Name="CameraComboBox" />`���`����ƁA<br>
	Visual Studio�������I��`MainWindow.g.i.cs`��CameraComboBox�t�B�[���h�𐶐����܂��B<br>

1. `class MainWindow`
	* **�L�[���[�h**�F`class`
	* **�Ӗ�**�F�u�݌v�}�v
	
	**���**�F`class MainWindow`�́uMainWindow�Ƃ������O�̐݌v�}�v���`���܂��B

1. `: Window`
	* **�L�[���[�h**: `:`
	* **����**: �p��
	* **�Ӗ�**: **�u...��y��i�e�j�ɂ��č��܂��v** �Ƃ����Ӗ��ł��B
	
	**���**: `MainWindow`�N���X��`Window`�N���X���p�����Ă��邱�Ƃ������܂��B<br>
	