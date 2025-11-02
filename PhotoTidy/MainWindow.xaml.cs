using Windows.System;
using CommunityToolkit.Mvvm.DependencyInjection;
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

		private static bool IsModifier(VirtualKey key) {
			return key is VirtualKey.Shift or VirtualKey.Control or VirtualKey.Menu or VirtualKey.LeftWindows or VirtualKey.RightWindows;
		}

		private void TagKeyTextBox_KeyDown(object sender, KeyRoutedEventArgs e) {
			if (sender is not TextBox tb || tb.DataContext is not TagInfo tag) {
				return;
			}

			// ここでキーイベントを消費 (二重入力防止)
			e.Handled = true;

			if (IsModifier(e.Key)) {
				return;
			}

			if (e.Key is VirtualKey.Back or VirtualKey.Delete) {
				tag.Key.Value = null;
				return;
			}

			tag.Key.Value = e.Key;
		}

		private async void TargetFolderBrowseButton_Click(object sender, RoutedEventArgs e) {
			if (sender is not Button btn || btn.DataContext is not TagInfo tag) {
				return;
			}

			var folder = await this._folderPickerService.PickFolderAsync();
			if (!string.IsNullOrEmpty(folder)) {
				tag.TargetFolder.Value = folder;
			}
		}
	}
}