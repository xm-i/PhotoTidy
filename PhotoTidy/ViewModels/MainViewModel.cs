using System.Collections.Generic;
using System.IO;
using Windows.System;
using PhotoTidy.Models;
using PhotoTidy.Services;

namespace PhotoTidy.ViewModels;

/// <summary>
///     画像の読み込みと UI 状態を調整するビュー モデルです。
/// </summary>
[AddSingleton]
public sealed class MainViewModel {
	private readonly TagList _tagList;
	private readonly AppStateService _appStateService;
	private readonly TagShortcutService _tagShortcutService;

	/// <summary>
	///     <see cref="MainViewModel" /> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="imageList">イメージリストモデル</param>
	public MainViewModel(ImageList imageList, TagList tagList, AppStateService appStateService, TagShortcutService tagShortcutService) {
		this._tagList = tagList;
		this._appStateService = appStateService;
		this._tagShortcutService = tagShortcutService;
		this.Images = imageList.Images.ToNotifyCollectionChanged(x => new ImageItemViewModel(x));
		this.FolderPath = imageList.FolderPath.ToBindableReactiveProperty();
		this.Status = imageList.Status.ToBindableReactiveProperty();
		this.IsBusy = imageList.IsBusy.ToBindableReactiveProperty();
		this.IncludeSubdirectories = imageList.IncludeSubdirectories.ToBindableReactiveProperty();
		this.SelectedIndex = imageList.SelectedIndex.ToBindableReactiveProperty();
		this.SelectedImage = imageList.SelectedImage.Select(i => i != null ? new ImageItemViewModel(i) : null).ToReadOnlyBindableReactiveProperty();
		this.BrowseCommand.Subscribe(async _ => await imageList.BrowseAsync());
		this.LoadCommand = this.IsBusy.CombineLatest(this.FolderPath, (isBusy, folderPath) => (isBusy, folderPath)).Select(x => !x.isBusy && Directory.Exists(x.folderPath ?? string.Empty)).ToReactiveCommand();
		this.LoadCommand.Subscribe(_ => imageList.Load());
		this.FolderPath.Subscribe(x => {
			imageList.FolderPath.Value = x;
		});
		this.IncludeSubdirectories.Subscribe(x => {
			imageList.IncludeSubdirectories.Value = x;
		});
		this.SelectedIndex.Subscribe(x => {
			imageList.SelectedIndex.Value = x;
		});

		this.Tags = tagList.Tags.ToNotifyCollectionChanged();
		this.MoveFilesCommand.Subscribe(_ => imageList.MoveImagesByTag());

		this.RestoreState(imageList);

		this.FolderPath.Skip(1).Subscribe(_ => this.SaveState());
		this.IncludeSubdirectories.Skip(1).Subscribe(_ => this.SaveState());
		this.SelectedIndex.Skip(1).Subscribe(_ => this.SaveState());
	}

	public NotifyCollectionChangedSynchronizedViewList<TagInfo> Tags {
		get;
	}

	public void AddTag(TagInfo tag) {
		this._tagList.AddTag(tag);
		this.SaveState();
	}

	public void RemoveTag(TagInfo tag) {
		this._tagList.RemoveTag(tag);
		this.SaveState();
	}

	public void ApplyShortcutTag(VirtualKey key, IReadOnlyList<ImageItemViewModel> selectedItems) {
		if (selectedItems.Count == 0) {
			return;
		}

		if (key is VirtualKey.Delete or VirtualKey.Back) {
			foreach (var selectedItem in selectedItems) {
				this._tagShortcutService.Clear(selectedItem.ImageItem);
			}
			return;
		}

		foreach (var selectedItem in selectedItems) {
			this._tagShortcutService.Apply(key, selectedItem.ImageItem);
		}
	}

	public ReactiveCommand MoveFilesCommand {
		get;
	} = new();

	/// <summary>
	///     表示対象の画像アイテム集合を取得します。
	/// </summary>
	public NotifyCollectionChangedSynchronizedViewList<ImageItemViewModel> Images {
		get;
	}

	/// <summary>
	///     選択中のフォルダパスを取得します。
	/// </summary>
	public BindableReactiveProperty<string?> FolderPath {
		get;
	}

	/// <summary>
	///     ステータス文字列を取得します。
	/// </summary>
	public BindableReactiveProperty<string?> Status {
		get;
	}

	/// <summary>
	///     読み込み処理中かどうかを示す値を取得します。
	/// </summary>
	public BindableReactiveProperty<bool> IsBusy {
		get;
	}

	/// <summary>
	///     フォルダ参照ダイアログを開くコマンドを取得します。
	/// </summary>
	public ReactiveCommand BrowseCommand {
		get;
	} = new();

	/// <summary>
	///     画像読み込みコマンドを取得します。
	/// </summary>
	public ReactiveCommand LoadCommand {
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
	///     サブディレクトリを読み込み対象に含めるかどうか。
	/// </summary>
	public BindableReactiveProperty<bool> IncludeSubdirectories {
		get;
	}

	private void RestoreState(ImageList imageList) {
		var state = this._appStateService.Load();
		if (state == null) {
			return;
		}

		if (!string.IsNullOrWhiteSpace(state.FolderPath.Value)) {
			this.FolderPath.Value = state.FolderPath.Value;
		}
		this.IncludeSubdirectories.Value = state.IncludeSubdirectories.Value;

		foreach (var tagState in state.Tags) {
			var tag = new TagInfo();
			tag.Key.Value = tagState.Key;
			tag.Name.Value = tagState.Name;
			tag.TargetFolder.Value = tagState.TargetFolder;
			this._tagList.AddTag(tag);
		}

		if (!string.IsNullOrWhiteSpace(this.FolderPath.Value) && Directory.Exists(this.FolderPath.Value)) {
			imageList.Load();
			if (state.SelectedIndex.Value >= 0 && state.SelectedIndex.Value < imageList.Images.Count) {
				this.SelectedIndex.Value = state.SelectedIndex.Value;
			}
		}
	}

	private void SaveState() {
		var config = new AppStateConfig();
		config.FolderPath.Value = this.FolderPath.Value;
		config.IncludeSubdirectories.Value = this.IncludeSubdirectories.Value;
		config.SelectedIndex.Value = this.SelectedIndex.Value;

		foreach (var tag in this._tagList.Tags) {
			config.Tags.Add(new TagStateConfig {
				Key = tag.Key.Value,
				Name = tag.Name.Value,
				TargetFolder = tag.TargetFolder.Value
			});
		}

		this._appStateService.Save(config);
	}
}