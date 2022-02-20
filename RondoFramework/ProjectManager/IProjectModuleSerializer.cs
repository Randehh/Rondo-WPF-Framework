using System.Text.Json.Nodes;

namespace RondoFramework.ProjectManager {
	public interface IProjectModuleSerializer {
		int Version { get; }

		JsonNode Serialize(IProjectModule module);
		IProjectModule Deserialize(JsonNode document);
		JsonNode UpgradeFromPreviousVersion(JsonNode document);
	}
}
