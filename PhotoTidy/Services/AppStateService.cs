using System.IO;
using System.Text.Json;
using PhotoTidy.Models;

namespace PhotoTidy.Services;

[AddSingleton]
public sealed class AppStateService {
	private static readonly JsonSerializerOptions JsonOptions = new() {
		WriteIndented = true
	};

	private readonly IServiceProvider _serviceProvider;
	private readonly string _stateFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PhotoTidy", "appstate.json");

	public AppStateService(IServiceProvider serviceProvider) {
		this._serviceProvider = serviceProvider;
	}

	public AppStateConfig? Load() {
		try {
			if (!File.Exists(this._stateFilePath)) {
				return null;
			}

			var json = File.ReadAllText(this._stateFilePath);
			var dto = JsonSerializer.Deserialize<AppStateConfigForJson>(json, JsonOptions);
			return AppStateConfigForJson.CreateModel(dto, this._serviceProvider);
		} catch {
			return null;
		}
	}

	public void Save(AppStateConfig config) {
		try {
			var dir = Path.GetDirectoryName(this._stateFilePath);
			if (!string.IsNullOrEmpty(dir)) {
				Directory.CreateDirectory(dir);
			}

			var dto = AppStateConfigForJson.CreateJson(config);
			var json = JsonSerializer.Serialize(dto, JsonOptions);
			File.WriteAllText(this._stateFilePath, json);
		} catch {
			// ignore persistence failures
		}
	}
}
