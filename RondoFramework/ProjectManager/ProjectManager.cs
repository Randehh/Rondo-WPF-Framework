using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows.Forms;

namespace RondoFramework.ProjectManager {
	public class ProjectManager {

		private const string PROJECT_FILE_EXTENSION = "project";
		private const string MODULE_FILE_EXTENSION = "project.module";

		public IProject LoadedProject { get; set; }

		private string RootFolder => LoadedProject.ProjectPath;
		private string ProjectDataPath => Path.Combine(RootFolder, $"{LoadedProject.ProjectName}.{PROJECT_FILE_EXTENSION}");
		private string GetModuleFilePath(string moduleName) => Path.Combine(RootFolder, $"{moduleName}.{MODULE_FILE_EXTENSION}");

		private Dictionary<Type, Action<IProject, IProjectModule>> m_ModuleCreationCallbacks = new Dictionary<Type, Action<IProject, IProjectModule>>();
		private Dictionary<Type, Dictionary<int, IProjectModuleSerializer>> m_Serializers = new Dictionary<Type, Dictionary<int, IProjectModuleSerializer>>();

		public bool AddProjectModule<U>(U module) where U : IProjectModule {
			foreach(U existingModule in LoadedProject.Modules) {
				if (existingModule.ModuleName == module.ModuleName) return false;
			}

			LoadedProject.Modules.Add(module);

			Type moduleType = typeof(U);
			if (m_ModuleCreationCallbacks.ContainsKey(moduleType)) {
				m_ModuleCreationCallbacks[moduleType](LoadedProject, module);
			}
			return true;
		}

		public void NewProject<T>(string projectName) where T : IProject {
			CommonOpenFileDialog dialog = new CommonOpenFileDialog();
			dialog.InitialDirectory = "C:\\";
			dialog.IsFolderPicker = true;
			if (dialog.ShowDialog() == CommonFileDialogResult.Ok) {
				T project = Activator.CreateInstance<T>();
				project.ProjectName = projectName;
				project.ProjectPath = dialog.FileName;
				LoadedProject = project;
			}
		}

		public void Save() {
			if (!Directory.Exists(RootFolder)) {
				Directory.CreateDirectory(RootFolder);
			}

			ProjectBaseData projectData = new ProjectBaseData(LoadedProject.ProjectName, LoadedProject.GetType());
			foreach (IProjectModule module in LoadedProject.Modules) {
				if (!m_Serializers.ContainsKey(module.GetType())) return;

				string moduleFilePath = GetModuleFilePath(module.ModuleName);
				string moduleJsonData = SerializeModule(module, m_Serializers[module.GetType()]);
				File.WriteAllText(moduleFilePath, moduleJsonData);
				projectData.Modules.Add(module.ModuleName, moduleFilePath);
				projectData.ModuleTypes.Add(module.ModuleName, module.GetType().AssemblyQualifiedName);
			}

			string jsonData = JsonSerializer.Serialize(projectData);
			File.WriteAllText(ProjectDataPath, jsonData);
		}

		public IProject LoadFromDialog() {
			using (OpenFileDialog openFileDialog = new OpenFileDialog()) {
				openFileDialog.InitialDirectory = "c:\\";
				openFileDialog.Filter = $"Project files (*.{PROJECT_FILE_EXTENSION})|*.{PROJECT_FILE_EXTENSION}";

				if (openFileDialog.ShowDialog() == DialogResult.OK) {
					return Load(openFileDialog.FileName);
				}
			}
			return null;
		}

		public IProject Load(string path) {
			string jsonData = File.ReadAllText(path);
			ProjectBaseData projectData = JsonSerializer.Deserialize<ProjectBaseData>(jsonData);

			Type projectType = Type.GetType(projectData.ProjectClassType);
			IProject project = Activator.CreateInstance(projectType) as IProject;

			foreach (KeyValuePair<string, string> pair in projectData.Modules) {
				string typeString = projectData.ModuleTypes[pair.Key];
				Type moduleType = Type.GetType(typeString);
				if (!m_Serializers.ContainsKey(moduleType)) continue;

				string moduleJsonData = File.ReadAllText(pair.Value);
				IProjectModule moduleInstance = DeserializeModule(moduleJsonData, m_Serializers[moduleType]);
				project.Modules.Add(moduleInstance);

				if (m_ModuleCreationCallbacks.ContainsKey(moduleType)) {
					m_ModuleCreationCallbacks[moduleType](project, moduleInstance);
				}
			}

			LoadedProject = project;
			return project;
		}

		public void RegisterModule<U>(Action<IProject, IProjectModule> creationCallback, List<IProjectModuleSerializer> serializers) where U : IProjectModule {
			Type moduleType = typeof(U);
			if (!m_ModuleCreationCallbacks.ContainsKey(moduleType)) {
				m_ModuleCreationCallbacks.Add(moduleType, creationCallback);
			}

			Dictionary<int, IProjectModuleSerializer> serializerDictionary = new Dictionary<int, IProjectModuleSerializer>();
			foreach (IProjectModuleSerializer serializer in serializers) {
				serializerDictionary.Add(serializer.Version, serializer);
			}
			m_Serializers.Add(moduleType, serializerDictionary);
		}

		private IProjectModule DeserializeModule(string jsonData, Dictionary<int, IProjectModuleSerializer> serializers) {
			JsonNode data = JsonNode.Parse(jsonData);
			int dataVersion = data["data_version"].GetValue<int>();

			IProjectModuleSerializer serializer = serializers[dataVersion];
			while(serializers.ContainsKey(dataVersion + 1)) {
				dataVersion++;
				serializer = serializers[dataVersion];
				serializer.UpgradeFromPreviousVersion(data);
			}

			return serializer.Deserialize(data["data"]);
		}

		private string SerializeModule(IProjectModule module, Dictionary<int, IProjectModuleSerializer> serializers) {
			int highestVersion = 0;
			foreach(int versionKey in serializers.Keys) {
				if (versionKey > highestVersion) highestVersion = versionKey;
			}

			JsonNode moduleData = serializers[highestVersion].Serialize(module);
			JsonNode dataToSave = new JsonObject() {
				["data_version"] = highestVersion,
				["data"] = moduleData,
			};

			return dataToSave.ToJsonString();
		}
	}
}
