using Windows.System;
using R3.JsonConfig.Attributes;

namespace PhotoTidy.Models;

[AddTransient]
[GenerateR3JsonConfigDto]
public partial class AppStateConfig {
    public ReactiveProperty<string?> FolderPath {
        get;
    } = new();

    public ReactiveProperty<int> SelectedIndex {
        get;
    } = new(-1);

    public ObservableList<TagStateConfig> Tags {
        get;
    } = [];
}

[AddTransient]
[GenerateR3JsonConfigDto]
public partial class TagStateConfig {
    public VirtualKey? Key {
        get;
        set;
    }

    public string? Name {
        get;
        set;
    }

    public string? TargetFolder {
        get;
        set;
    }
}
