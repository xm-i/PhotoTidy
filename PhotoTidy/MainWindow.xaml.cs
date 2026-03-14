using Windows.System;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using PhotoTidy.Models;
using PhotoTidy.Services;
using PhotoTidy.ViewModels;
using PhotoTidy.Views;
using System.Threading.Tasks;

namespace PhotoTidy {
	/// <summary>
	///     画像一覧表示用のメインウィンドウを表します。
	/// </summary>
	[AddSingleton]
	public sealed partial class MainWindow : Window {
		private readonly IFolderPickerService _folderPickerService;

		/// <summary>
		///     <see cref="MainWindow" /> クラスの新しいインスタンスを初期化します。
		/// </summary>
		public MainWindow(MainViewModel mainViewModel, ImagePreviewViewModel previewViewModel, IFolderPickerService folderPickerService) {
			this.InitializeComponent();
			this.ViewModel = mainViewModel;
			this.PreviewViewModel = previewViewModel;
			this._folderPickerService = folderPickerService;
			this.RootGrid.Loaded += (_, _) => this.RootGrid.Focus(FocusState.Programmatic);
		}

		/// <summary>
		///     このウィンドウに関連付けられたビュー モデルを取得します。
		/// </summary>
		public MainViewModel ViewModel {
			get;
		}

		public ImagePreviewViewModel PreviewViewModel {
			get;
		}

		/// <summary>
		///     フォルダ入力テキストボックスで Enter キーが押下されたときに画像読み込みコマンドを実行します。
		/// </summary>
		/// <param name="_">イベント送信元。</param>
		/// <param name="e">キーイベントデータ。</param>
		private void FolderTextBox_KeyDown(object _, KeyRoutedEventArgs e) {
			if (e.Key == VirtualKey.Enter && this.ViewModel.LoadCommand.CanExecute()) {
				this.ViewModel.LoadCommand.Execute(Unit.Default);
			}
		}

		/// <summary>
		///     画像アイテムのダブルタップで拡大プレビューウィンドウを開きます。
		/// </summary>
		private async void ImageItem_DoubleTapped(object sender, DoubleTappedRoutedEventArgs _) {
			var previewWindow = Ioc.Default.GetRequiredService<ImagePreviewWindow>();
			await Task.Delay(100);
			previewWindow.Activate();
		}

		/// <summary>
		///     タグ定義追加ダイアログを表示します。
		/// </summary>
		private async void AddTagDefinition_Click(object sender, RoutedEventArgs e) {
			VirtualKey? selectedKey = null;

			var keyTextBox = new TextBox {
				PlaceholderText = "キーを押して割り当て (Delete で解除)",
				IsReadOnly = true
			};
			keyTextBox.KeyDown += (s, args) => {
				args.Handled = true;
				if (IsModifier(args.Key)) {
					return;
				}

				if (args.Key is VirtualKey.Back or VirtualKey.Delete) {
					selectedKey = null;
					keyTextBox.Text = "";
					return;
				}

				selectedKey = args.Key;
				keyTextBox.Text = args.Key.ToString();
			};

			var nameTextBox = new TextBox { PlaceholderText = "タグ名を入力" };

			var folderTextBox = new TextBox { PlaceholderText = "フォルダを選択", IsReadOnly = true };
			var browseButton = new Button { Content = "…", MinWidth = 32 };
			browseButton.Click += async (s, args) => {
				var folder = await this._folderPickerService.PickFolderAsync();
				if (!string.IsNullOrEmpty(folder)) {
					folderTextBox.Text = folder;
				}
			};

			var folderPanel = new Grid { ColumnSpacing = 4 };
			folderPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
			folderPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
			Grid.SetColumn(folderTextBox, 0);
			Grid.SetColumn(browseButton, 1);
			folderPanel.Children.Add(folderTextBox);
			folderPanel.Children.Add(browseButton);

			var content = new StackPanel { Spacing = 16, MinWidth = 400 };
			content.Children.Add(CreateField("ショートカットキー", keyTextBox));
			content.Children.Add(CreateField("タグ名", nameTextBox));
			content.Children.Add(CreateField("ターゲットフォルダ", folderPanel));

			var dialog = new ContentDialog {
				Title = "タグ定義追加",
				Content = content,
				PrimaryButtonText = "追加",
				CloseButtonText = "キャンセル",
				DefaultButton = ContentDialogButton.Primary,
				XamlRoot = this.Content.XamlRoot
			};

			if (await dialog.ShowAsync() == ContentDialogResult.Primary) {
				var tag = new TagInfo();
				tag.Key.Value = selectedKey;
				tag.Name.Value = string.IsNullOrWhiteSpace(nameTextBox.Text) ? null : nameTextBox.Text;
				tag.TargetFolder.Value = string.IsNullOrWhiteSpace(folderTextBox.Text) ? null : folderTextBox.Text;
				this.ViewModel.AddTag(tag);
			}
		}

		/// <summary>
		///     タグ定義を削除します。
		/// </summary>
		private void RemoveTag_Click(object sender, RoutedEventArgs e) {
			if (sender is FrameworkElement { DataContext: TagInfo tag }) {
				this.ViewModel.RemoveTag(tag);
			}
		}

		private static StackPanel CreateField(string label, UIElement input) {
			var panel = new StackPanel { Spacing = 4 };
			panel.Children.Add(new TextBlock { Text = label, FontWeight = FontWeights.SemiBold });
			panel.Children.Add(input);
			return panel;
		}

		private static bool IsModifier(VirtualKey key) {
			return key is VirtualKey.Shift or VirtualKey.Control or VirtualKey.Menu or VirtualKey.LeftWindows or VirtualKey.RightWindows;
		}
	}
}