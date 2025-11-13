using Microsoft.UI.Xaml; // Added for Visibility

using PhotoTidy.Models;
using PhotoTidy.Services;

using Windows.System;

namespace PhotoTidy.ViewModels;

[AddTransient]
public class ImagePreviewViewModel {
	private readonly ImageList _imageList;

	public ImagePreviewViewModel(ImageList imageList, TagShortcutService tagShortcutService) {
		this._imageList = imageList;
		this.SelectedIndex = imageList.SelectedIndex.ToBindableReactiveProperty();
		this.SelectedImage = imageList.SelectedImage.Select(x => x == null ? null : new ImageItemViewModel(x)).ToBindableReactiveProperty();
		this.ShortcutKeyCommand.Subscribe(x => {
			switch (x) {
				case VirtualKey.Right:
					this._imageList.MoveNext();
					return;
				case VirtualKey.Left:
					this._imageList.MovePrevious();
					return;
				default:
					if (this.SelectedImage.Value == null) {
						return;
					}

					tagShortcutService.Apply(x, this.SelectedImage.Value.ImageItem);
					break;
			}
		});
		this.NextImage = imageList.NextImage.Select(x => x == null ? null : new ImageItemViewModel(x)).ToBindableReactiveProperty();
		this.PreviousImage = imageList.PreviousImage.Select(x => x == null ? null : new ImageItemViewModel(x)).ToBindableReactiveProperty();

		this.Title =
			this.SelectedImage
				.ObservePropertyChanged(x => x)
				.CombineLatest(this.SelectedIndex, (img, idx) => (img, idx))
				.Select(x => {
					return $"{x.img.Value?.FileName.Value} ({x.idx + 1} / {this._imageList.Images.Count})";
				}).ToBindableReactiveProperty(string.Empty);

		// Visibility projections
		this.NextImageVisibility = imageList.NextImage
			.Select(x => x == null ? Visibility.Collapsed : Visibility.Visible)
			.ToBindableReactiveProperty();
		this.PreviousImageVisibility = imageList.PreviousImage
			.Select(x => x == null ? Visibility.Collapsed : Visibility.Visible)
			.ToBindableReactiveProperty();
	}

	public IReadOnlyBindableReactiveProperty<string> Title {
		get;
	}

	/// <summary>
	///     現在選択されている画像アイテムのインデックスを取得または設定します。
	/// </summary>
	public BindableReactiveProperty<int> SelectedIndex {
		get;
	}

	/// <summary>
	///     現在選択されている画像アイテムを取得します。
	/// </summary>
	public IReadOnlyBindableReactiveProperty<ImageItemViewModel?> SelectedImage {
		get;
	}

	/// <summary>
	///     次に表示する画像アイテムを取得します。
	/// </summary>
	public IReadOnlyBindableReactiveProperty<ImageItemViewModel?> NextImage {
		get;
	}

	/// <summary>
	///     前に表示する画像アイテムを取得します。
	/// </summary>
	public IReadOnlyBindableReactiveProperty<ImageItemViewModel?> PreviousImage {
		get;
	}

	/// <summary>
	/// 次の画像のVisibility
	/// </summary>
	public IReadOnlyBindableReactiveProperty<Visibility> NextImageVisibility {
		get;
	}

	/// <summary>
	/// 前の画像のVisibility
	/// </summary>
	public IReadOnlyBindableReactiveProperty<Visibility> PreviousImageVisibility {
		get;
	}

	public ReactiveCommand<VirtualKey> ShortcutKeyCommand {
		get;
	} = new();
}