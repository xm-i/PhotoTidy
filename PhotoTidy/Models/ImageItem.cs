using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media.Imaging;

namespace PhotoTidy.Models;

/// <summary>
///     遅延読み込みされるサムネイルを持つ画像ファイルを表します。
/// </summary>
[AddTransient]
public sealed class ImageItem {
	private bool _loaded;

	/// <summary>
	///     <see cref="ImageItem" /> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="filePath">画像ファイルの絶対パス。</param>
	public ImageItem(string filePath) {
		this.FilePath.Value = filePath;
		this.FileName = this.FilePath.Select(Path.GetFileName).ToReadOnlyReactiveProperty()!;
	}

	/// <summary>
	///     画像ファイルの絶対パスを取得します。
	/// </summary>
	public ReactiveProperty<string> FilePath {
		get;
	} = new();

	/// <summary>
	///     パスを除いたファイル名を取得します。
	/// </summary>
	public ReadOnlyReactiveProperty<string> FileName {
		get;
	}

	/// <summary>
	///     読み込まれたサムネイルを取得します。読み込み前は null です。
	/// </summary>
	public ReactiveProperty<BitmapImage?> Thumbnail {
		get;
	} = new();

	/// <summary>
	///     画像に付与されたタグ (ショートカット操作で設定)。
	/// </summary>
	public ReactiveProperty<TagInfo?> Tag {
		get;
	} = new();

	/// <summary>
	///     サムネイルを非同期で読み込みます。
	/// </summary>
	/// <returns>非同期操作を表すタスク。</returns>
	public async Task EnsureThumbnailAsync() {
		if (this._loaded) {
			return;
		}
		var count = 0;
		while (count < 10) {
			try {
				var bitmap = new BitmapImage();
				await using var stream = File.OpenRead(this.FilePath.Value);
				await bitmap.SetSourceAsync(stream.AsRandomAccessStream());
				this.Thumbnail.Value = bitmap;
			} catch {
				count++;
				await Task.Delay(100 * count);
				continue;
			}
			this._loaded = true;
			break;
		}
	}
}