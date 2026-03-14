namespace PhotoTidy.Models;

[AddSingleton]
public class TagList {
	public ObservableList<TagInfo> Tags {
		get;
	} = [];

	public void AddTagRow() {
		this.Tags.Add(new());
	}

	public void AddTag(TagInfo tag) {
		this.Tags.Add(tag);
	}

	public void RemoveTag(TagInfo tag) {
		this.Tags.Remove(tag);
	}
}