using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RondoFramework.RecentFilesTracker {
	public class RecentFilesTracker {

		private string TrackerDataDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RondoFrameworkRFC");
		private string TrackerFilePath => Path.Combine(TrackerDataDirectory, $"{ApplicationName}.json");

		public string ApplicationName { get; set; }
		public List<RecentFile> RecentlyOpenedFiles { get; set; } = new List<RecentFile>();
		public Action OnUpdate { get; set; } = delegate { };

		private int m_FileLimit = 10;

		public RecentFilesTracker(string appName, int fileLimit = 10) {
			ApplicationName = appName;
			m_FileLimit = fileLimit;

			if (!Directory.Exists(TrackerDataDirectory)) {
				Directory.CreateDirectory(TrackerDataDirectory);
			}
		}

		public void AddRecentlyOpenedFile(string path) {
			for(int i = RecentlyOpenedFiles.Count - 1; i >= 0; i--) {
				RecentFile file = RecentlyOpenedFiles[i];
				if (string.Equals(file.FullPath, path)) {
					RecentlyOpenedFiles.Remove(file);
					break;
				}
			}

			RecentlyOpenedFiles.Add(new RecentFile(path));
			Save();
			OnUpdate();
		}

		public void Load() {
			if (!File.Exists(TrackerFilePath)) return;
			string jsonData = File.ReadAllText(TrackerFilePath);
			RecentlyOpenedFiles = JsonSerializer.Deserialize<List<RecentFile>>(jsonData);
			OnUpdate();
		}

		private void Save() {
			string jsonData = JsonSerializer.Serialize(RecentlyOpenedFiles);
			File.WriteAllText(TrackerFilePath, jsonData);
		}
	}
}
