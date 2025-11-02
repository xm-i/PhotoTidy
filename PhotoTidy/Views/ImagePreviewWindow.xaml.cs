using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using PhotoTidy.ViewModels;

namespace PhotoTidy.Views;

/// <summary>
///     単一画像の拡大プレビュー用ウィンドウです。
/// </summary>
[AddSingleton]
public sealed partial class ImagePreviewWindow {
	private readonly MainViewModel _mainViewModel = Ioc.Default.GetRequiredService<MainViewModel>();

	/// <summary>
	///     <see cref="ImagePreviewWindow" /> の新しいインスタンスを初期化します。
	/// </summary>
	public ImagePreviewWindow(ImagePreviewViewModel viewModel) {
		this.ViewModel = viewModel;
		this.InitializeComponent();
		this.RootGrid.DataContext = this.ViewModel; // Enable classic Binding for converter usage
		this.TrySetSize();
		this.RootGrid.Loaded += (_, _) => this.RootGrid.Focus(FocusState.Programmatic);
	}

	/// <summary>
	///     プレビュー対象の画像アイテム ViewModel を取得します。
	/// </summary>
	public ImagePreviewViewModel ViewModel {
		get;
	}

	private void TrySetSize() {
		this.AppWindow?.Resize(new(900, 700));
	}

	private void RootGrid_KeyDown(object sender, KeyRoutedEventArgs e) {
		this.ViewModel.ShortcutKeyCommand.Execute(e.Key);
	}
}