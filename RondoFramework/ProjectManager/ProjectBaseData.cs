using System;
using System.Collections.Generic;

namespace RondoFramework.ProjectManager {
	public class ProjectBaseData {

		public string AppName { get; set; }
		public string ProjectClassType { get; set; }
		public Dictionary<string, string> Modules { get; set; } = new Dictionary<string, string>();
		public Dictionary<string, string> ModuleTypes { get; set; } = new Dictionary<string, string>();

		public ProjectBaseData() { }

		public ProjectBaseData(string appName, Type projectType) {
			AppName = appName;
			ProjectClassType = projectType.AssemblyQualifiedName;
		}
	}
}
