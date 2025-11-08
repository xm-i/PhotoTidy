using Microsoft.UI.Xaml.Media.Imaging;
using PhotoTidy.Models;

namespace PhotoTidy.ViewModels;

[AddTransient]
public class ImageItemViewModel(ImageItem imageItem) {
	public ImageItem ImageItem {
		get;
	} = imageItem;

	/// <summary>
	///     画像ファイルの絶対パスを取得します。
	/// </summary>
	public BindableReactiveProperty<string> FilePath {
		get;
	} = imageItem.FilePath.ToBindableReactiveProperty("");

	/// <summary>
	///     パスを除いたファイル名を取得します。
	/// </summary>
	public IReadOnlyBindableReactiveProperty<string> FileName {
		get {
			_ = this.ImageItem.EnsureThumbnailAsync();
			return field;
		}
	} = imageItem.FileName.ToBindableReactiveProperty("");

	/// <summary>
	///     読み込まれたサムネイルを取得します。読み込み前は null です。
	/// </summary>
	public BindableReactiveProperty<BitmapImage?> Thumbnail {
		get;
	} = imageItem.Thumbnail.ToBindableReactiveProperty();

	/// <summary>
	///     画像に付与されたタグ。
	/// </summary>
	public BindableReactiveProperty<TagInfo?> Tag {
		get;
	} = imageItem.Tag.ToBindableReactiveProperty();
}