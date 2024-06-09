using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object = UnityEngine.Object;

namespace MaxyGames.UNode.Editors {
	public static class GenerationUtility {
		#region Persistence Data
		[Serializable]
		public class Data {
			[SerializeField]
			public Dictionary<int, CachedScriptData> graphs = new Dictionary<int, CachedScriptData>();

			public CachedScriptData GetGraphData(UnityEngine.Object obj) {
				return GetGraphData(uNodeUtility.GetObjectID(obj));
			}

			public CachedScriptData GetGraphData(Func<CachedScriptData,bool> validation) {
				foreach(var (_, data) in graphs) {
					if(validation(data)) {
						return data;
					}
				}
				return null;
			}

			public CachedScriptData GetGraphData(int graphID) {
				if(!graphs.TryGetValue(graphID, out var scriptData)) {
					graphs[graphID] = scriptData = new CachedScriptData();
				}
				return scriptData;
			}
		}

		public class CachedScriptData {
			public string path;
			public int lastCompiledID;
			public int uniqueID;
			public string[] errors;
			public string generatedScript;

			public long fileHash;
			public bool isValid => (errors == null || errors.Length == 0) && !string.IsNullOrEmpty(generatedScript) && !string.IsNullOrEmpty(path);

			public void MarkDirty() {
				path = null;
				fileHash = default;
				lastCompiledID = 0;
			}
		}

		public static Data persistenceData => GetData();

		public static Data _data;
		public static Data GetData() {
			if(_data == null) {
				_data = uNodeEditorUtility.LoadEditorData<Data>("GeneratorData");
				if(_data == null) {
					_data = new Data();
					SaveData();
				}
			}
			return _data;
		}

		public static void SaveData() {
			if(_data != null)
				uNodeEditorUtility.SaveEditorData(_data, "GeneratorData");
		}

		public static void MarkGraphDirty(Object graphAsset) {
			if(persistenceData.graphs.TryGetValue(uNodeUtility.GetObjectID(graphAsset), out var data)) {
				data.MarkDirty();
			}
		}

		public static void MarkGraphDirty(IEnumerable<Object> graphAssets) {
			foreach(var graph in graphAssets) {
				if(graph == null)
					continue;
				MarkGraphDirty(graph);
			}
		}

		public static bool IsGraphCompiled(Object graphAsset) {
			var scriptData = persistenceData.GetGraphData(graphAsset);
			if(scriptData.isValid) {
				if(uNodePreference.preferenceData.generatorData.compilationMethod == CompilationMethod.Roslyn) {
					return File.Exists(tempAssemblyPath) && File.Exists(scriptData.path);
				} else {
					return true;
				}
			}
			return false;
		}

		public static bool IsGraphUpToDate(Object graphAsset) {
			var scriptData = persistenceData.GetGraphData(graphAsset);
			if(scriptData.isValid && File.Exists(scriptData.path)) {
				var hash = uNodeUtility.GetFileHash(AssetDatabase.GetAssetPath(graphAsset));
				return scriptData.fileHash == hash;
			}
			return false;
		}
		#endregion

		static uNodePreference.PreferenceData _preferenceData;
		private static uNodePreference.PreferenceData preferenceData {
			get {
				if(_preferenceData != uNodePreference.GetPreference()) {
					_preferenceData = uNodePreference.GetPreference();
				}
				return _preferenceData;
			}
		}
		public const string tempFolder = "TempScript";
		public static string tempGeneratedFolder => tempFolder + Path.DirectorySeparatorChar + "Generated";
		public static string tempRoslynFolder => tempFolder + Path.DirectorySeparatorChar + "Scripts";
		public static string tempAssemblyPath => tempRoslynFolder + Path.DirectorySeparatorChar + "RuntimeAssembly.dll";
		public static string generatedPath => "Assets" + Path.DirectorySeparatorChar + "uNode.Generated";
		public static string resourcesPath => generatedPath + Path.DirectorySeparatorChar + "Resources";
		public static string projectScriptPath => generatedPath + Path.DirectorySeparatorChar + "Scripts";
		public static string projectSceneScriptPath => projectScriptPath + Path.DirectorySeparatorChar + "Scene";

		public static uNodeDatabase GetDatabase() {
			var db = uNodeUtility.GetDatabase();
			if(db == null) {
				db = ScriptableObject.CreateInstance<uNodeDatabase>();
				var dbDir = resourcesPath;
				Directory.CreateDirectory(dbDir);
				var path = dbDir + Path.DirectorySeparatorChar + "uNodeDatabase.asset";
				Debug.Log($"No database found, creating new database in: {path}");
				AssetDatabase.CreateAsset(db, path);
			}
			return db;
		}

		[MenuItem("Tools/uNode/Generate C# Scripts", false, 22)]
		public static void GenerateCSharpScript() {
			if(preferenceData.generatorData.compilationMethod == CompilationMethod.Unity) {
				CompileProjectGraphs();
			} else {
				if(Directory.Exists(projectScriptPath)) {
					Debug.LogWarning($"Warning: You're using Roslyn Compilation method but there's a generated script located on: {projectScriptPath} folder, please delete it to ensure script is working.\nIf the generated script in {projectScriptPath} folder still exist the graph will run with that script.");
				}
				if(preferenceData.generatorData.compileInBackground && uNodeUtility.IsProVersion) {
					CompileGraphsInBackground();
				} else {
					CompileProjectGraphs(true, false);
				}
			}
		}

		/// <summary>
		/// Compile project graph.
		/// </summary>
		public static void CompileProjectGraphs(bool saveInTemporaryFolder = false, bool force = true) {
			try {
				var scripts = GenerationUtility.GenerateProjectScripts(force);
				var db = GetDatabase();
				EditorUtility.DisplayProgressBar("Saving Scripts", "", 1);
				string dir;
				List<string> scriptPaths = null;
				if(saveInTemporaryFolder) {
					dir = tempRoslynFolder;
					scriptPaths = new List<string>();
				} else {
					dir = projectScriptPath;
				}
				Directory.CreateDirectory(dir);
				foreach(var script in scripts) {
					var path = Path.GetFullPath(dir) + Path.DirectorySeparatorChar + script.fileName + ".cs";
					var assetPath = AssetDatabase.GetAssetPath(script.graphOwner);
					if(File.Exists(assetPath.RemoveLast(6).Add("cs"))) {
						//Skip when the graph has been compiled manually
						continue;
					}
					if(saveInTemporaryFolder)
						scriptPaths.Add(path);
					List<ScriptInformation> informations;
					var generatedScript = script.ToScript(out informations, true);
					using(StreamWriter sw = new StreamWriter(path)) {
						if(informations != null) {
							uNodeEditor.SavedData.RegisterGraphInfos(informations, script.graphOwner, path);
						}
						sw.Write(ConvertLineEnding(generatedScript, Application.platform != RuntimePlatform.WindowsEditor));
						sw.Close();
					}
					if(EditorUtility.IsPersistent(script.graphOwner)) {
						var scriptData = persistenceData.GetGraphData(script.graphOwner);
						scriptData.path = path;
						scriptData.fileHash = uNodeUtility.GetFileHash(AssetDatabase.GetAssetPath(script.graphOwner));
						scriptData.lastCompiledID = script.GetSettingUID();
						scriptData.generatedScript = generatedScript;
					}
					GraphUtility.UpdateDatabase(new[] { script });
				}
				if(saveInTemporaryFolder) {
					EditorUtility.DisplayProgressBar("Compiling Scripts", "", 1);
					var result = RoslynUtility.CompileFilesAndSave(Path.GetRandomFileName(), scriptPaths, Path.GetFullPath(dir) + Path.DirectorySeparatorChar + "RuntimeAssembly.dll", false);
					if(result.errors != null && result.errors.Any()) {
						Debug.LogError(result.GetErrorMessage());
					}
				} else {
					AssetDatabase.Refresh();
					AssetDatabase.SaveAssets();
				}
				uNodeDatabase.ClearCache();
				Debug.Log("Successful generating project script, project graphs will run with native c#." +
				"\nRemember to compiles the graph again if you made a changes to a graphs to keep the script up to date." +
				"\nRemoving generated scripts will makes the graph to run with reflection again." +
				"\nGenerated project script can be found on: " + dir);
			}
			finally {
				EditorUtility.ClearProgressBar();
			}
		}

		[MenuItem("Tools/uNode/Generate C# including Scenes", false, 23)]
		public static void GenerateCSharpScriptIncludingSceneGraphs() {
			if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) {
				return;
			}
			if(preferenceData.generatorData.compilationMethod == CompilationMethod.Unity) {
				CompileProjectGraphs();
				GenerateCSharpScriptForSceneGraphs();
			} else {
				if(Directory.Exists(projectScriptPath)) {
					Debug.LogWarning($"Warning: You're using Roslyn Compilation method but there's a generated script located on: {projectScriptPath} folder, please delete it to ensure script is working.\nIf the generated script in {projectScriptPath} folder still exist the graph will run with that script.");
				}
				CompileProjectGraphs(true, false);
				GenerateCSharpScriptForSceneGraphs();
			}
		}

		public static void GenerateCSharpScriptForSceneGraphs() {
			DeleteGeneratedCSharpScriptForScenes();//Removing previous files so there's no outdated scripts
			var scenes = EditorBuildSettings.scenes;
			var dir = projectSceneScriptPath;
			// uNodeEditorUtility.FindAssetsByType<SceneAsset>();
			for (int i = 0; i < scenes.Length;i++) {
				var scene = scenes[i];
				var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.path);
				if(sceneAsset == null || !scene.enabled) continue;
				EditorUtility.DisplayProgressBar($"Loading Scene: {sceneAsset.name} {i+1}-{scenes.Length}", "", 0);
				while(uNodeThreadUtility.IsNeedUpdate()) {
					uNodeThreadUtility.Update();
				}
				var currentScene = EditorSceneManager.OpenScene(scene.path);
				var graphs = GameObject.FindObjectsOfType<GraphComponent>();
				var scripts = new List<CG.GeneratedData>();
				int count = 0;
				foreach(var graph in graphs) {
					count++;
					scripts.Add(GenerationUtility.GenerateCSharpScript(graph, (progress, info) => {
						EditorUtility.DisplayProgressBar($"Generating C# for: {sceneAsset.name} {i+1}-{scenes.Length} current: {count}-{graphs.Length}", info, progress);
					}));
				}
				while(uNodeThreadUtility.IsNeedUpdate()) {
					uNodeThreadUtility.Update();
				}
				EditorSceneManager.SaveScene(currentScene);
				EditorUtility.DisplayProgressBar("Saving Scene Scripts", "", 1);
				Directory.CreateDirectory(dir);
				var startPath = Path.GetFullPath(dir) + Path.DirectorySeparatorChar;
				foreach(var script in scripts) {
					var path = startPath + currentScene.name + "_" + script.fileName + ".cs";
					int index = 1;
					while(File.Exists(path)) {//Ensure name to be unique
						path = startPath + currentScene.name + "_" + script.fileName + index + ".cs";
						index++;
					}
					using(StreamWriter sw = new StreamWriter(path)) {
						List<ScriptInformation> informations;
						var generatedScript = script.ToScript(out informations, true);
						if(informations != null) {
							uNodeEditor.SavedData.RegisterGraphInfos(informations, script.graphOwner, path);
						}
						sw.Write(GenerationUtility.ConvertLineEnding(generatedScript, false));
						sw.Close();
					}
				}
			}
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			EditorUtility.ClearProgressBar();
			Debug.Log("Successful generating scenes script, existing scenes graphs will run with native c#." +
				"\nRemember to compiles the graph again if you made a changes to a graphs to keep the script up to date." + 
				"\nRemoving generated scripts will makes the graph to run with reflection again." + 
				"\nGenerated scenes script can be found on: " + dir);
		}


		private static bool isGeneratingInBackground;
		/// <summary>
		/// Compile project runtime graphs in the background.
		/// </summary>
		public static void CompileGraphsInBackground() {
			if(!isGeneratingInBackground) {
				isGeneratingInBackground = true;
				uNodeThreadUtility.CreateThread(DoCompileGraphsInBackground).Start();
			}
		}

		/// <summary>
		/// Generate and compile all runtime graphs in background.
		/// Note: don't call it from main thread.
		/// </summary>
		public static void DoCompileGraphsInBackground() {
			try {
#if !UNODE_PRO
				if(uNodeUtility.IsProVersion == false) {
					throw new Exception("Compile in background is pro only feature");
				}
#endif
				uNodeThreadUtility.WaitOneFrame();
				var dir = tempRoslynFolder;
				var dirInfo = Directory.CreateDirectory(dir);
				var scripts = GenerationUtility.GenerateRuntimeGraphAsync(false);
				var scriptPaths = new List<string>();
				int skippedCount = 0;
				uNodeThreadUtility.QueueAndWait(() => {
					var db = GetDatabase();
					EditorProgressBar.ShowProgressBar("Saving Scripts", 1);
					foreach(var script in scripts) {
						if(script == null)
							continue;
						var path = Path.GetFullPath(dir) + Path.DirectorySeparatorChar + script.fileName + ".cs";
						//var assetPath = AssetDatabase.GetAssetPath(script.graphOwner);
						//if(File.Exists(assetPath.RemoveLast(6).Add("cs"))) {
						//	//Skip when the graph has been compiled manually
						//	continue;
						//}
						if(!script.isValid) {
							if(File.Exists(path)) {
								scriptPaths.Add(path);
								skippedCount++;
							}
							if(script.hasError) {
								string errors = null;
								foreach(var e in script.errors) {
									errors += e.ToString() + "\n";
								}
								Debug.LogError("Generating script in background has an errors, errors: \n" + errors);
							}
							continue;
						}
						List<ScriptInformation> informations;
						var generatedScript = script.ToScript(out informations, true);
						using(StreamWriter sw = new StreamWriter(path)) {
							if(informations != null) {
								uNodeEditor.SavedData.RegisterGraphInfos(informations, script.graphOwner, path);
							}
							sw.Write(ConvertLineEnding(generatedScript, Application.platform != RuntimePlatform.WindowsEditor));
							sw.Close();
						}
						scriptPaths.Add(path);
						if(EditorUtility.IsPersistent(script.graphOwner)) {
							var scriptData = persistenceData.GetGraphData(script.graphOwner);
							scriptData.path = path;
							scriptData.fileHash = uNodeUtility.GetFileHash(AssetDatabase.GetAssetPath(script.graphOwner));
							var lastCompiledID = script.GetSettingUID();
							if(scriptData.generatedScript != generatedScript || scriptData.lastCompiledID != lastCompiledID) {
								scriptData.generatedScript = generatedScript;
								scriptData.lastCompiledID = lastCompiledID;
							} else {
								skippedCount++;
							}
						}
						GraphUtility.UpdateDatabase(new[] { script });
					}
					uNodeDatabase.ClearCache();
					//foreach(var file in dirInfo.EnumerateFiles()) {
					//	if(file.Extension == ".cs") {
					//		scriptPaths.Add(Path.GetFullPath(dir) + Path.DirectorySeparatorChar + file.Name);
					//	}
					//}
				});
				if(scriptPaths.Count != skippedCount || !File.Exists(tempAssemblyPath)) {
					uNodeThreadUtility.QueueAndWait(() => {
						EditorProgressBar.ShowProgressBar("Compiling Scripts", 1);
					});
					var result = RoslynUtility.CompileFilesAndSave(Path.GetRandomFileName(), scriptPaths, Path.GetFullPath(dir) + Path.DirectorySeparatorChar + "RuntimeAssembly.dll", false);
					if(result == null) {
						throw new Exception("Something wrong with compile using Roslyn.");
					}
					if(result.errors != null && result.errors.Any()) {
						string additionalInfo = null;
						var map = persistenceData.graphs.ToList();
						foreach(var error in result.errors) {
							if(!string.IsNullOrEmpty(error.fileName)) {
								foreach(var pair in map) {
									if(pair.Value.path == error.fileName && !error.isWarning) {
										//Make sure to reset the hash for graph that has error message.
										pair.Value.fileHash = default;
										pair.Value.generatedScript = string.Empty;

										additionalInfo += "\n" + GraphException.GetMessage("", pair.Key, 0, null);
									}
								}
							}
							//uNodeDebug.LogError(error.errorMessage);
						}
						Debug.LogError(result.GetErrorMessage() + additionalInfo);
						//Debug.LogError($"Error compiling graphs. {uNodeLogger.uNodeConsoleWindow.KEY_OpenConsole}\n" + result.GetErrorMessage());
					} else {
						Debug.Log("Successful generating and compiling project script, project graphs will run with native c#." +
						"\nRemember to compiles the graph again if you made a changes to a graphs to keep the script up to date." +
						"\nRemoving generated scripts will makes the graph to run with reflection again." +
						"\nGenerated project script can be found on: " + dir);
					}
				}
				else if(scriptPaths.Count == skippedCount) {
					Debug.Log("Successful generating but skipping compiling because all script are up to date");
				}
			}
			finally {
				isGeneratingInBackground = false;
				uNodeThreadUtility.Queue(() => {
					EditorProgressBar.ClearProgressBar();
				});
			}
		}

#region Delete Generated Script
		[MenuItem("Tools/uNode/Delete Generated C# Scripts", false, 24)]
		public static void DeleteGeneratedCSharpScript() {
			EditorUtility.DisplayProgressBar("Deleting Generated C# Scripts", "", 1);
			if(Directory.Exists(projectScriptPath)) {
				Directory.Delete(projectScriptPath, true);
			}
			if(File.Exists(projectScriptPath + ".meta")) {
				File.Delete(projectScriptPath + ".meta");
			}
			if(File.Exists(tempAssemblyPath)) {
				File.Delete(tempAssemblyPath);
			}
			AssetDatabase.Refresh();
			EditorUtility.ClearProgressBar();
		}

		//public static void DeleteGeneratedCSharpScriptForProjects() {
		//	EditorUtility.DisplayProgressBar("Deleting Generated C# Scripts", "", 1);
		//	var dir = projectScriptPath;
		//	if(Directory.Exists(dir)) {
		//		Directory.Delete(dir, true);
		//	}
		//	if(File.Exists(dir + ".meta")) {
		//		File.Delete(dir + ".meta");
		//	}
		//	AssetDatabase.Refresh();
		//	EditorUtility.ClearProgressBar();
		//}

		public static void DeleteGeneratedCSharpScriptForScenes() {
			EditorUtility.DisplayProgressBar("Deleting Generated C# Scripts", "", 1);
			var dir = projectSceneScriptPath;
			if(Directory.Exists(dir)) {
				Directory.Delete(dir, true);
			}
			if(File.Exists(dir + ".meta")) {
				File.Delete(dir + ".meta");
			}
			AssetDatabase.Refresh();
			EditorUtility.ClearProgressBar();
		}
#endregion

		public static void GenerateNativeGraphInProject(bool enableLogging = true) {
			try {
				System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
				watch.Start();
				var scripts = GenerateNativeProjectScripts(true);
				watch.Stop();
				if(enableLogging)
					Debug.LogFormat("Generating C# took {0,8:N3} s.", watch.Elapsed.TotalSeconds);
				var dir = "TempScript" + Path.DirectorySeparatorChar + "GeneratedCSharpGraph";
				Directory.CreateDirectory(dir);
				HashSet<string> fileNames = new HashSet<string>();
				List<string> paths = new List<string>();
				Action saveAction = null;
				foreach(var script in scripts) {
					var fileName = script.fileName;
					int index = 2;
					while(!fileNames.Add(fileName)) {
						fileName = script.fileName + index;
					}
					if(CanCompileScript()) {//Save to temp
						var path = Path.GetFullPath(dir) + Path.DirectorySeparatorChar + fileName + ".cs";
						using(StreamWriter sw = new StreamWriter(path)) {
							var generatedScript = script.ToScript(out var informations, true);
							if(informations != null) {
								uNodeEditor.SavedData.RegisterGraphInfos(informations, script.graphOwner, path);
							}
							sw.Write(preferenceData.generatorData.convertLineEnding ? ConvertLineEnding(generatedScript, Application.platform != RuntimePlatform.WindowsEditor) : generatedScript);
							sw.Close();
						}
						paths.Add(path);
					}
					{//Save to project
						saveAction += () => {
							var path = (Path.GetDirectoryName(AssetDatabase.GetAssetPath(script.graphOwner)) + Path.DirectorySeparatorChar + script.graphOwner.name + ".cs");
							using(StreamWriter sw = new StreamWriter(path)) {
								var generatedScript = script.ToScript(out var informations, true);
								if(informations != null) {
									uNodeEditor.SavedData.RegisterGraphInfos(informations, script.graphOwner, path);
								}
								sw.Write(preferenceData.generatorData.convertLineEnding ? ConvertLineEnding(generatedScript, Application.platform != RuntimePlatform.WindowsEditor) : generatedScript);
								sw.Close();
							}
						};
					}
				}
				if(CanCompileScript()) {
					watch.Restart();
					EditorUtility.DisplayProgressBar("Loading", "Compiling", 1);
					CompileFromFile(paths.ToArray());
					watch.Stop();
					if(enableLogging)
						Debug.LogFormat("Compiling script took {0,8:N3} s.", watch.Elapsed.TotalSeconds);
				}
				saveAction?.Invoke();
				AssetDatabase.Refresh();
				AssetDatabase.SaveAssets();
				Debug.Log("Successful generating script for C# Graphs in the project.");
			}
			finally {
				uNodeThreadUtility.QueueOnFrame(() => {
					EditorUtility.ClearProgressBar();
				});
			}
		}

		private static CG.GeneratedData[] GenerateNativeProjectScripts(
			bool force,
			string label = "Generating C# Scripts",
			bool clearProgressOnFinish = true) {
			try {
				int count = 0;
				var objects = GraphUtility.FindAllGraphAssets().Where(obj => obj is IScriptGraph).ToList();
				var scripts = objects.Select(gameObject => {
					count++;
					return GenerateCSharpScript(gameObject, (progress, text) => {
						EditorUtility.DisplayProgressBar($"{label} {count}-{objects.Count}", text, progress);
					});
				}).Where(s => s != null);
				return scripts.ToArray();
			}
			finally {
				if(clearProgressOnFinish) {
					uNodeThreadUtility.QueueOnFrame(() => {
						EditorUtility.ClearProgressBar();
					});
				}
			}
		}

		public static void CompileNativeGraphInProject() {
			CompileNativeGraph(GraphUtility.FindAllGraphAssets().Where(obj => obj is IScriptGraph));
		}

		public static void CompileNativeGraph(IEnumerable<Object> graphs) {
			foreach(var graph in graphs) {
				CompileNativeGraph(graph);
			}
		}

		public static void CompileNativeGraph(Object graphObject, bool enableLogging = true) {
			if(EditorUtility.IsPersistent(graphObject)) {
				if(AssetDatabase.IsSubAsset(graphObject)) {
					if(graphObject is IScriptGraphType scriptGraphType && scriptGraphType.ScriptTypeData.scriptGraphReference != null) {
						graphObject = scriptGraphType.ScriptTypeData.scriptGraphReference;
					} else {
						if(graphObject == null) return;
						throw new Exception("Attemp to compile sub asset which is not supported.");
					}
				}
			}
			string fileName = graphObject.name;
			GameObject prefabContent = null;
			var objectToCompile = graphObject;
			if(uNodeEditorUtility.IsPrefab(graphObject)) {
				prefabContent = PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(graphObject));
				objectToCompile = prefabContent;
			}
			Directory.CreateDirectory(GenerationUtility.tempFolder);
			char separator = Path.DirectorySeparatorChar;
			string path = GenerationUtility.tempFolder + separator + fileName + ".cs";
			try {
				System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
				watch.Start();
				var script = GenerationUtility.GenerateCSharpScript(objectToCompile, (progress, text) => {
                    EditorUtility.DisplayProgressBar("Loading", text, progress);
                });
				if(preferenceData.generatorData.convertLineEnding) {
					script.postScriptModifier += input => {
						return GenerationUtility.ConvertLineEnding(input, Application.platform != RuntimePlatform.WindowsEditor);
					};
				}
				if(preferenceData.generatorData != null && preferenceData.generatorData.analyzeScript && preferenceData.generatorData.formatScript) {
					var codeFormatter = TypeSerializer.Deserialize("MaxyGames.UNode.Editors.CSharpFormatter", false);
					if(codeFormatter != null) {
						script.postScriptModifier += input => {
							return codeFormatter.
								GetMethod("FormatCode").
								Invoke(null, new object[] { input }) as string;
						};
					}
				}

				List<ScriptInformation> informations;
				var generatedScript = script.ToScript(out informations, true);
				using(StreamWriter sw = new StreamWriter(path)) {
					sw.Write(generatedScript);
					sw.Close();
				}
				watch.Stop();
				if(enableLogging)
					Debug.LogFormat("Generating C# took {0,8:N3} s.", watch.Elapsed.TotalSeconds);
				if(preferenceData.generatorData.compileBeforeSave && uNodeUtility.IsProVersion) {
					bool isBecauseOfAccessibility = false;
					try {
						watch.Reset();
						EditorUtility.DisplayProgressBar("Loading", "Compiling", 1);
						watch.Start();
						var compileResult = CompileScript(generatedScript);
						watch.Stop();
#if !NET_STANDARD_2_0
						if(enableLogging)
							Debug.LogFormat("Compiling script took {0,8:N3} s.", watch.Elapsed.TotalSeconds);
#endif

						if(compileResult.isSuccess == false) {
							isBecauseOfAccessibility = true;
							foreach(var error in compileResult.errors) {
								if(error.errorNumber != "CS0122") {
									isBecauseOfAccessibility = false;
									break;
								}
							}
							throw new Exception(compileResult.GetErrorMessage());
						}
					}
					catch (System.Exception ex) {
						watch.Stop();
						EditorUtility.ClearProgressBar();
						if(EditorUtility.DisplayDialog("Compile Errors", "Compile before save detect an error: \n" + ex.Message + "\n\n" +
							(isBecauseOfAccessibility ?
								"The initial errors may because of using a private class.\nWould you like to ignore the error and save it?" : 
								"Would you like to ignore the error and save it?"),
							"Ok, save it",
							"No, don't save")) {
							Debug.Log("Compile errors: " + ex.Message);
						} else {
							Debug.Log("Temp script saved to: " + Path.GetFullPath(path));
							throw ex;
						}
					}
				}
				if(EditorUtility.IsPersistent(graphObject)) {//For prefab and asset
					path = (Path.GetDirectoryName(AssetDatabase.GetAssetPath(graphObject)) + separator + fileName + ".cs");
					if(informations != null) {
						uNodeEditor.SavedData.RegisterGraphInfos(informations, script.graphOwner, path);
					}
					using(FileStream stream = File.Open(path, FileMode.Create, FileAccess.Write)) {
						using(StreamWriter writer = new StreamWriter(stream)) {
							writer.Write(generatedScript);
							writer.Close();
						}
						stream.Close();
					}
				} else {//For the scene object.
					path = EditorUtility.SaveFilePanel("Save Script", "Assets", fileName + ".cs", "cs");
					if(informations != null) {
						uNodeEditor.SavedData.RegisterGraphInfos(informations, script.graphOwner, path);
					}
					using(FileStream stream = File.Open(path, FileMode.Create, FileAccess.Write)) {
						using(StreamWriter writer = new StreamWriter(stream)) {
							writer.Write(generatedScript);
							writer.Close();
						}
						stream.Close();
					}
				}
				AssetDatabase.Refresh();
				Debug.Log("Script saved to: " + Path.GetFullPath(path));
				EditorUtility.ClearProgressBar();
			}
			catch {
				EditorUtility.ClearProgressBar();
				Debug.LogError("Aborting Generating C# Script because have error.");
				throw;
			} finally {
				if(prefabContent != null) {
					PrefabUtility.UnloadPrefabContents(prefabContent);
				}
			}
		}

		public static bool LoadRuntimeAssembly() {
			var pdbPath = Path.ChangeExtension(tempAssemblyPath, ".pdb");
			if(File.Exists(tempAssemblyPath)) {
				var rawAssembly = File.ReadAllBytes(tempAssemblyPath);
				Assembly assembly;
				if(pdbPath != null) {
					var pdb = File.ReadAllBytes(pdbPath);
					assembly = Assembly.Load(rawAssembly, pdb);
				} else {
					assembly = Assembly.Load(rawAssembly);
				}
				if(assembly != null) {
					ReflectionUtils.RegisterRuntimeAssembly(assembly);
					ReflectionUtils.UpdateAssemblies();
					ReflectionUtils.GetAssemblies();
					return true;
				}
			}
			return false;
		}

		public static void CompileProjectGraphsAnonymous() {
			if(CanCompileScript()) {
				var scripts = GenerateProjectScripts(true);
				var dir = "TempScript" + Path.DirectorySeparatorChar + "GeneratedOnPlay";
				Directory.CreateDirectory(dir);
				HashSet<string> fileNames = new HashSet<string>();
				List<string> paths = new List<string>();
				foreach(var script in scripts) {
					var fileName = script.fileName;
					int index = 2;
					while(!fileNames.Add(fileName)) {
						fileName = script.fileName + index;
					}
					var path = Path.GetFullPath(dir) + Path.DirectorySeparatorChar + script.fileName + ".cs";
					using(StreamWriter sw = new StreamWriter(path)) {
						var generatedScript = script.ToScript(out var informations, true);
						if(informations != null) {
							uNodeEditor.SavedData.RegisterGraphInfos(informations, script.graphOwner, path);
						}
						sw.Write(ConvertLineEnding(generatedScript, false));
						sw.Close();
					}
					GraphUtility.UpdateDatabase(new[] { script });
					paths.Add(path);
				}
				ReflectionUtils.RegisterRuntimeAssembly(CompileFromFile(paths.ToArray()));
				ReflectionUtils.UpdateAssemblies();
				ReflectionUtils.GetAssemblies();
			}
		}

		internal static void CompileAndPatchProjectGraphs() {
			if(CanCompileScript() && EditorBinding.patchType != null) {
				var scripts = GenerateProjectScripts(true);
				var dir = "TempScript" + Path.DirectorySeparatorChar + "GeneratedOnPlay";
				Directory.CreateDirectory(dir);
				List<Type> types = new List<Type>();
				HashSet<string> fileNames = new HashSet<string>();
				List<string> paths = new List<string>();
				foreach(var script in scripts) {
					var fileName = script.fileName;
					int index = 2;
					while(!fileNames.Add(fileName)) {
						fileName = script.fileName + index;
					}
					var path = Path.GetFullPath(dir) + Path.DirectorySeparatorChar + script.fileName + ".cs";
					using(StreamWriter sw = new StreamWriter(path)) {
						var generatedScript = script.ToScript(out var informations, true);
						if(informations != null) {
							uNodeEditor.SavedData.RegisterGraphInfos(informations, script.graphOwner, path);
						}
						sw.Write(ConvertLineEnding(generatedScript, false));
						sw.Close();
					}
					GraphUtility.UpdateDatabase(new[] { script });
					paths.Add(path);
					var ns = script.Namespace;
					foreach(var pair in script.classNames) {
						if(string.IsNullOrEmpty(ns)) {
							types.Add(pair.Value.ToType(false));
						} else {
							types.Add((ns + "." + pair.Value).ToType(false));
						}
					}
				}
				var assembly = CompileFromFile(paths.ToArray());
				if(assembly == null)
					return;
				for(int i=0;i<types.Count;i++) {
					if(types[i] == null)
						continue;
					var type = assembly.GetType(types[i].FullName);
					if(type != null) {
						EditorUtility.DisplayProgressBar("Patching", "Patch generated c# into existing script.", (float)i / types.Count);
						EditorBinding.patchType(types[i], type);
					}
				}
				ReflectionUtils.RegisterRuntimeAssembly(assembly);
				ReflectionUtils.UpdateAssemblies();
				ReflectionUtils.GetAssemblies();
			}
		}

		public static CG.GeneratedData[] GenerateProjectScripts(
			bool force, 
			string label = "Generating C# Scripts", 
			bool clearProgressOnFinish = true) {
			try {
				var objects = GraphUtility.FindGraphs<GraphAsset>().ToArray();
				int count = 0;
				var scripts = objects.Select(g => {
					count++;
					if (g is GraphAsset graphAsset) {
						var graphSystem = GraphUtility.GetGraphSystem(graphAsset);
						if (!graphSystem.allowAutoCompile) {
							return null;
						}
						if(graphAsset is ITypeWithScriptData) {
							ITypeWithScriptData typeWithScriptData = graphAsset as ITypeWithScriptData;
							if(typeWithScriptData.ScriptData.compileToScript == false) {
								//Skip if compile to script is false
								var cachedData = persistenceData.GetGraphData(graphAsset);
								if(cachedData != null && cachedData.isValid) {
									cachedData.MarkDirty();
									if(File.Exists(tempAssemblyPath)) {
										File.Delete(tempAssemblyPath);
									}
								}
								return null;
							}
						}
						return GenerateCSharpScript(graphAsset, (progress, text) => {
							EditorUtility.DisplayProgressBar($"{label} {count}-{objects.Length}", text, progress);
						});
					} else {
						throw new InvalidOperationException(g.GetType().FullName);
					}
				}).Where(s => s != null);
				return scripts.ToArray();
			} finally {
				if (clearProgressOnFinish) {
					uNodeThreadUtility.QueueOnFrame(() => {
						EditorUtility.ClearProgressBar();
					});
				}
			}
		}

#region GenerateCSharpScript
		public static CG.GeneratedData GenerateCSharpScript(UnityEngine.Object source, Action<float, string> updateProgress = null) {
			if(source == null) {
				return null;
			}
			GeneratedScriptData scriptData;
			bool debug, debugValue;
			if (source is IScriptGraph scriptGraph) {
				scriptData = scriptGraph.ScriptData;
				debug = scriptData.debug;
				debugValue = scriptData.debugValueNode;
			} else if(source is ITypeWithScriptData typeWithScriptData) {
				scriptData = typeWithScriptData.ScriptData;
				debug = scriptData.debug;
				debugValue = scriptData.debugValueNode;
			} else {
				throw new Exception("Unsupported graph type: " + source.GetType());
			}
			return CG.Generate(new CG.GeneratorSetting(source) {
				fullTypeName = preferenceData.generatorData.fullTypeName,
				fullComment = preferenceData.generatorData.fullComment,
				generationMode = preferenceData.generatorData.generationMode,
				runtimeOptimization = preferenceData.generatorData.optimizeRuntimeCode && uNodeUtility.IsProVersion,
				debugScript = debug,
				debugValueNode = debugValue,
				updateProgress = updateProgress,
			});
		}
#endregion

#region GenerateCSharpAsync
		/// <summary>
		/// Generate Project Script in background.
		/// Note: don't call it from main thread.
		/// </summary>
		/// <param name="force"></param>
		/// <param name="label"></param>
		/// <param name="clearProgressOnFinish"></param>
		/// <returns></returns>
		public static CG.GeneratedData[] GenerateRuntimeGraphAsync(
			bool force,
			string label = "Generating C# Scripts",
			bool clearProgressOnFinish = true) {
			try {
				List<UnityEngine.Object> objects = new List<UnityEngine.Object>();
				uNodeThreadUtility.QueueAndWait(() => {
					//Find graphs in main thread.
					objects.AddRange(GraphUtility.FindGraphs<GraphAsset>());
				});
				int count = 0;
				List<CG.GeneratedData> scripts = new List<CG.GeneratedData>();
				foreach(var g in objects) {
					count++;
					if(g is GraphAsset graphAsset) {
						uNodeThreadUtility.QueueAndWait(() => {
							if(graphAsset is ITypeWithScriptData) {
								ITypeWithScriptData typeWithScriptData = graphAsset as ITypeWithScriptData;
								if(typeWithScriptData.ScriptData.compileToScript == false) {
									//Skip if compile to script is false
									var cachedData = persistenceData.GetGraphData(graphAsset);
									if(cachedData != null && cachedData.isValid) {
										cachedData.MarkDirty();
										if(File.Exists(tempAssemblyPath)) {
											File.Delete(tempAssemblyPath);
										}
									}
									graphAsset = null;
									return;
								}
							}
							if(!force /*&& !GraphUtility.HasTempGraphObject(gameObject)*/ && IsGraphUpToDate(graphAsset)) {
								var settings = new CG.GeneratorSetting(graphAsset as Object) {
									fullTypeName = true /*preferenceData.generatorData.fullTypeName*/,
									fullComment = false /*preferenceData.generatorData.fullComment*/,
									generationMode = preferenceData.generatorData.generationMode,
									runtimeOptimization = preferenceData.generatorData.optimizeRuntimeCode && uNodeUtility.IsProVersion,
									//debugScript = debug,
									//debugValueNode = debugValue,
								};
								var script = new CG.GeneratedData(settings);
								script.InitOwner();
								scripts.Add(script);
								graphAsset = null;
							};
						});
						if(graphAsset == null || !GraphUtility.GetGraphSystem(graphAsset).allowAutoCompile) {
							continue;
						}
						scripts.Add(GenerateCSharpAsync(graphAsset, (progress, text) => {
							EditorProgressBar.ShowProgressBar($"{label} {count}-{objects.Count}", (float)count / (float)objects.Count);
						}));
					} 
					//TODO: fix me
					//else if(g is uNodeInterface iface) {
					//	EditorProgressBar.ShowProgressBar($"Generating interface {iface.name} {count}-{objects.Count}", (float)count / (float)objects.Count);
					//	uNodeThreadUtility.QueueAndWait(() => {
					//		scripts.Add(GenerateCSharpScript(iface));
					//	});
					//} 
					else {
						throw new InvalidOperationException(g.GetType().FullName);
					}
				}
				return scripts.ToArray();
			}
			finally {
				if(clearProgressOnFinish) {
					EditorProgressBar.ClearProgressBar();
				}
			}
		}

		/// <summary>
		/// Generate CSharp Script Async.
		/// Note: Don't call in main thread.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="updateProgress"></param>
		/// <returns></returns>
		public static CG.GeneratedData GenerateCSharpAsync(Object source, Action<float, string> updateProgress = null) {
			if(source == null) {
				return null;
			}
			GeneratedScriptData scriptData;
			bool debug, debugValue;
			if (source is IScriptGraph scriptGraph) {
				scriptData = scriptGraph.ScriptData;
				debug = scriptData.debug;
				debugValue = scriptData.debugValueNode;
			}
			else if (source is ITypeWithScriptData typeWithScriptData) {
				scriptData = typeWithScriptData.ScriptData;
				debug = scriptData.debug;
				debugValue = scriptData.debugValueNode;
			}
			else {
				throw new Exception("Unsupported graph type: " + source.GetType());
			}
			return CG.Generate(new CG.GeneratorSetting(source) {
				fullTypeName = true /*preferenceData.generatorData.fullTypeName*/,
				fullComment = false /*preferenceData.generatorData.fullComment*/,
				generationMode = preferenceData.generatorData.generationMode,
				runtimeOptimization = preferenceData.generatorData.optimizeRuntimeCode && uNodeUtility.IsProVersion,
				debugScript = debug,
				debugValueNode = debugValue,
				updateProgress = updateProgress,
				isAsync = true,
			});
		}
#endregion

		public static string ConvertLineEnding(string text, bool isUnixFormat) {
			var regex = new System.Text.RegularExpressions.Regex(@"(?<!\r)\n");
			const string LineEnd = "\r\n";

			string originalText = text;
			string changedText;
			changedText = regex.Replace(originalText, LineEnd);
			if(isUnixFormat) {
				changedText = changedText.Replace(LineEnd, "\n");
			}
			return changedText;
		}

#region Compile
		public static CompileResult CompileScript(params string[] source) {
			return RoslynUtility.CompileScript(source);
		}

		public static bool CanCompileScript() {
			return true;
		}

		public static Assembly Compile(params string[] source) {
			var compileResult = RoslynUtility.CompileScript(source);

			if(compileResult.isSuccess == false) {
				//compileResult.LogErrors();
				throw new Exception(compileResult.GetErrorMessage());
			}
			return compileResult.LoadAssembly();
		}

		public static Assembly CompileFromFile(params string[] files) {
			var compileResult = RoslynUtility.CompileFiles(files);

			if(compileResult.isSuccess == false) {
				//compileResult.LogErrors();
				throw new Exception(compileResult.GetErrorMessage());
			}
			return compileResult.LoadAssembly();
		}
#endregion
	}
}