using System.Collections.Generic;

namespace RondoFramework.ProjectManager {
	public interface IProject {
		string ProjectName { get; set; }
		string ProjectPath { get; set; }
		List<IProjectModule> Modules { get; set; }
	}
}
