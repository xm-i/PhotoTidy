using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PhotoTidy.Services;

namespace PhotoTidy.Models;

[AddSingleton]
public class ImageList {
	private static readonly string[] ImageExtensions = [".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tif", ".tiff", ".webp"];
	private readonly IFolderPickerService _folderPickerService;
	private readonly SynchronizationContext _uiContext; // UI スレッド同期コンテキスト (非 null 前提)
	private FileSystemWatcher? _watcher;

	/// <summary>
	///     <see cref="ImageList" /> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="folderPickerService">フォルダ選択サービス。</param>
	public ImageList(IFolderPickerService folderPickerService) {
		this._folderPickerService = folderPickerService;
		// UI スレッドで生成される前提。取得できない場合は開発時に気づけるよう例外。
		this._uiContext = SynchronizationContext.Current ?? throw new InvalidOperationException("UI SynchronizationContext が利用できません");
		this.SelectedImage = this.SelectedIndex
			.Select(i => i >= 0 && i < this.Images.Count ? this.Images[i] : null)
			.ToReadOnlyReactiveProperty();
		// 前の画像
		this.PreviousImage = this.SelectedIndex
			.CombineLatest(this.Images.ObserveCountChanged(), (i, c) => i)
			.Select(i => {
				var prev = i - 1;
				return prev >= 0 && prev < this.Images.Count ? this.Images[prev] : null;
			})
			.ToReadOnlyReactiveProperty();
		// 次の画像
		this.NextImage = this.SelectedIndex
			.CombineLatest(this.Images.ObserveCountChanged(), (i, c) => i)
			.Select(i => {
				var next = i + 1;
				return next >= 0 && next < this.Images.Count ? this.Images[next] : null;
			})
			.ToReadOnlyReactiveProperty();
	}

	/// <summary>
	///     表示対象の画像アイテム集合を取得します。
	/// </summary>
	public ObservableList<ImageItem> Images {
		get;
	} = [];

	/// <summary>
	///     選択中のフォルダパスを取得します。
	/// </summary>
	public ReactiveProperty<string?> FolderPath {
		get;
	} = new();

	/// <summary>
	///     ステータス文字列を取得します。
	/// </summary>
	public ReactiveProperty<string?> Status {
		get;
	} = new();

	/// <summary>
	///     読み込み処理中かどうかを示す値を取得します。
	/// </summary>
	public ReactiveProperty<bool> IsBusy {
		get;
	} = new();

	/// <summary>
	///     選択中の画像インデックス (-1 は未選択)。
	/// </summary>
	public ReactiveProperty<int> SelectedIndex {
		get;
	} = new(-1);

	/// <summary>
	///     選択中の画像。
	/// </summary>
	public ReadOnlyReactiveProperty<ImageItem?> SelectedImage {
		get;
	}

	/// <summary>
	///     選択中の画像の1つ前の画像 (存在しなければ null)。
	/// </summary>
	public ReadOnlyReactiveProperty<ImageItem?> PreviousImage {
		get;
	}

	/// <summary>
	///     選択中の画像の1つ後の画像 (存在しなければ null)。
	/// </summary>
	public ReadOnlyReactiveProperty<ImageItem?> NextImage {
		get;
	}

	public async Task BrowseAsync() {
		var path = await this._folderPickerService.PickFolderAsync();
		if (!string.IsNullOrEmpty(path)) {
			this.FolderPath.Value = path;
			this.Load();
		}
	}

	public void Load() {
		this.DisposeWatcher();

		if (string.IsNullOrWhiteSpace(this.FolderPath.Value) || !Directory.Exists(this.FolderPath.Value)) {
			this.Status.Value = "無効なフォルダ";
			return;
		}

		try {
			this.IsBusy.Value = true;
			this.Status.Value = "読み込み中...";
			this.Images.Clear();

			var files = Directory.EnumerateFiles(this.FolderPath.Value, "*.*", SearchOption.TopDirectoryOnly)
				.Where(f => ImageExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
				.ToList();

			foreach (var file in files) {
				var item = new ImageItem(file);
				this.Images.Add(item);
			}

			this.Status.Value = $"{this.Images.Count} 件";
			this.SelectedIndex.Value = this.Images.Count > 0 ? 0 : -1;
			this.SetupWatcher();
		} catch (Exception ex) {
			this.Status.Value = "エラー: " + ex.Message;
		} finally {
			this.IsBusy.Value = false;
		}
	}

	private void SetupWatcher() {
		try {
			this._watcher = new(this.FolderPath.Value!) { IncludeSubdirectories = false, Filter = "*.*", NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime };
			this._watcher.Created += this.OnFileCreated;
			this._watcher.Renamed += this.OnFileCreated; // .tmp -> 画像拡張子 対応 (追加のみ)
			this._watcher.EnableRaisingEvents = true;
		} catch {
			// ignore watcher setup failures
		}
	}

	private void DisposeWatcher() {
		if (this._watcher != null) {
			this._watcher.Created -= this.OnFileCreated;
			this._watcher.Renamed -= this.OnFileCreated;
			this._watcher.Dispose();
			this._watcher = null;
		}
	}

	private void OnFileCreated(object? sender, FileSystemEventArgs e) {
		if (!this.IsTargetImage(e.FullPath)) {
			return;
		}

		// 既に存在する場合は無視
		if (this.Images.Any(i => string.Equals(i.FilePath.Value, e.FullPath, StringComparison.OrdinalIgnoreCase))) {
			return;
		}

		if (!File.Exists(e.FullPath)) {
			return;
		}

		void AddCore() {
			if (!File.Exists(e.FullPath)) {
				return; // 二重確認
			}

			var item = new ImageItem(e.FullPath);
			this.Images.Add(item);
			_ = item.EnsureThumbnailAsync();
			this.Status.Value = $"{this.Images.Count} 件";
		}

		if (SynchronizationContext.Current == this._uiContext) {
			try {
				AddCore();
			} catch (Exception ex) {
				this.Status.Value = "追加エラー: " + ex.Message;
			}
		} else {
			this._uiContext.Post(_ => {
				try {
					AddCore();
				} catch (Exception ex) {
					this.Status.Value = "追加エラー: " + ex.Message;
				}
			}, null);
		}
	}

	private bool IsTargetImage(string path) {
		var ext = Path.GetExtension(path);
		return ImageExtensions.Any(x => string.Equals(x, ext, StringComparison.OrdinalIgnoreCase));
	}

	public void MoveNext() {
		if (this.Images.Count == 0) {
			return;
		}

		var next = Math.Min(this.SelectedIndex.Value + 1, this.Images.Count - 1);
		this.SelectedIndex.Value = next;
	}

	public void MovePrevious() {
		if (this.Images.Count == 0) {
			return;
		}

		var prev = Math.Max(this.SelectedIndex.Value - 1, 0);
		this.SelectedIndex.Value = prev;
	}

	/// <summary>
	///     指定タグが付与されている全ての画像をそのタグのターゲットフォルダへ移動します。
	/// </summary>
	public void MoveImagesByTag() {
		try {
			var targets = this.Images.Where(image => image.Tag.Value != null).ToList();
			if (targets.Count == 0) {
				return;
			}

			foreach (var img in targets) {
				var srcPath = img.FilePath.Value;
				if (!File.Exists(srcPath)) {
					continue;
				}

				var tag = img.Tag.Value;
				if (tag?.TargetFolder.Value == null) {
					continue;
				}

				var destPath = Path.Combine(tag.TargetFolder.Value, Path.GetFileName(srcPath));

				destPath = this.EnsureUnique(destPath);
				try {
					File.Move(srcPath, destPath);
					img.FilePath.Value = destPath;
				} catch (Exception exMove) {
					// 個別失敗はスキップ (全体失敗ではない) ステータスに最後のエラー記録
					this.Status.Value = "移動エラー: " + exMove.Message;
				}
			} // 件数更新 & 選択インデックス補正
		} catch (Exception ex) {
			this.Status.Value = "移動エラー: " + ex.Message;
		}
	}

	private string EnsureUnique(string path) {
		if (!File.Exists(path)) {
			return path;
		}

		var dir = Path.GetDirectoryName(path)!;
		var name = Path.GetFileNameWithoutExtension(path);
		var ext = Path.GetExtension(path);
		var i = 1;
		while (true) {
			var candidate = Path.Combine(dir, $"{name}({i}){ext}");
			if (!File.Exists(candidate)) {
				return candidate;
			}

			i++;
		}
	}
}