## C# の基本的な構文についての解説

### 1. public partial class MainWindow : Window

```csharp
public partial class MainWindow : Window
```
この行は、C#でクラスを定義するための基本的な構文です。<br>
1. `public`アクセス修飾子<br>
	* **キーワード**: `public`<br>
	* **分類**: アクセス修飾子<br>
	* **意味**: このクラスが他のクラスや名前空間からアクセス可能であることを示します。<br>

	**解説**:「これは内部だけで使う大事な部品だから、外からは触らないで（private）」や、<br>
「誰でも使っていいですよ（public）」といった公開範囲を決める必要があります。<br>
MainWindowは、アプリケーション（App.xaml）から「最初に起動するウィンドウ」<br>
として指定されるため、必ずであるpublic必要があります。<br>

1. `partial`キーワード<br>
	* **キーワード**: `partial`<br>
	* **分類**: クラス修飾子<br>
	* **意味**: このクラスの定義が複数のファイルに分割されていることを示します。<br>

	**解説**: WPFでは、XAMLファイルで定義されたUI要素と、その背後にある<br>
    コードビハインドファイルが連携して動作します。`partial`キーワード<br>
    を使用することで、XAMLで定義された部分とコードビハインドで定義された部分を<br>
	同じクラスとして扱うことができます。<br>
    * `MainWindow.xaml`:「見た目」の設計図。ここに`<Button>`などのUI要素を配置します。<br>
	* `MainWindow.xaml.cs`:「動作」のコード。ここにボタンが押されたときの処理などを書きます。<br>
    * `MainWindow.g.i.cs`: 自動生成されるコード。XAMLの内容をC#コードに変換したものです。<br>※このファイルは手動で編集しません。<br>
	あなたが`MainWindow.xaml`と`<ComboBox x:Name="CameraComboBox" />`を定義すると、<br>
	Visual Studioが自動的に`MainWindow.g.i.cs`にCameraComboBoxフィールドを生成します。<br>

1. `class MainWindow`
	* **キーワード**：`class`
	* **意味**：「設計図」
	
	**解説**：`class MainWindow`は「MainWindowという名前の設計図」を定義します。

1. `: Window`
	* **キーワード**: `:`
	* **分類**: 継承
	* **意味**: **「...を土台（親）にして作ります」** という意味です。
	
	**解説**: `MainWindow`クラスが`Window`クラスを継承していることを示します。<br>
	