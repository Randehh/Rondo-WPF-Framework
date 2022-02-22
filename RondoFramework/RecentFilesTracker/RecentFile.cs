using System.IO;

namespace RondoFramework.RecentFilesTracker {
	public class RecentFile {

		public RecentFile() { }

		public RecentFile(string path) {
			FullPath = path;
		}

		public string FullPath { get; set; }
		public string FileName {
			get => Path.GetFileName(FullPath);
			set { }
		}
	}
}
