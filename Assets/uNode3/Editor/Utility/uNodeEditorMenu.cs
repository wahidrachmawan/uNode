using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEditor.Callbacks;
using System.Reflection;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using MaxyGames.OdinSerializer.Editor;
using Object = UnityEngine.Object;

namespace MaxyGames.UNode.Editors {
	internal static class uNodeEditorMenu {
		internal class MyCustomBuildProcessor : UnityEditor.Build.IPreprocessBuildWithReport, UnityEditor.Build.IPostprocessBuildWithReport {
			public int callbackOrder => 0;

			public void OnPreprocessBuild(BuildReport report) {
				uNodeEditorInitializer.OnPreprocessBuild();
			}

			public void OnPostprocessBuild(BuildReport report) {
				uNodeEditorInitializer.OnPostprocessBuild();
			}
		}

		[MenuItem("Tools/uNode/Advanced/Scan AOT Type", false, 1000010)]
		public static void ScanAOTType() {
			uNodeEditorInitializer.AOTScan(out var types);
			var db = uNodeUtility.GetDatabase();
			db.aotTypes.Clear();
			Debug.Log(types.Count);
			foreach(var t in types) {
				Debug.Log(t);
				db.aotTypes.Add(t);
			}
		}

		[MenuItem("Tools/uNode/Update Graph Database", false, 2)]
		private static void UpdateDatabase() {
			GraphUtility.UpdateDatabase();
		}

#if UNODE_COMPILE_ON_PLAY
		[MenuItem("Tools/uNode/Advanced/Compile On Play: Enabled", false, 10001)]
#else
		[MenuItem("Tools/uNode/Advanced/Compile On Play: Disabled", false, 10001)]
#endif
		private static void AdvancedCompileOnPlay() {
			if(uNodeEditorUtility.DisplayRequiredProVersion()) {
				return;
			}
#if UNODE_COMPILE_ON_PLAY
			uNodeEditorUtility.RemoveDefineSymbols(new string[] { "UNODE_COMPILE_ON_PLAY" });
#else
			uNodeEditorUtility.AddDefineSymbols(new string[] { "UNODE_COMPILE_ON_PLAY" });
#endif
		}



#if UNODE_DEBUG
		[MenuItem("Tools/uNode/Advanced/Developer Mode: Enabled", false, 10002)]
#else
		[MenuItem("Tools/uNode/Advanced/Developer Mode: Disabled", false, 10002)]
#endif
		private static void AdvancedDeveloperMode() {
#if UNODE_DEBUG
			uNodeEditorUtility.RemoveDefineSymbols(new string[] { "UNODE_DEBUG" });
#else
			uNodeEditorUtility.AddDefineSymbols(new string[] { "UNODE_DEBUG" });
#endif
		}

#if UNODE_TRIM_ON_BUILD
		[MenuItem("Tools/uNode/Advanced/Trim Graph On Build: Enabled", false, 10002)]
#else
		[MenuItem("Tools/uNode/Advanced/Trim Graph On Build: Disabled", false, 10002)]
#endif
		private static void AdvancedTrimOnBuild() {
			if(uNodeEditorUtility.DisplayRequiredProVersion()) {
				return;
			}
#if UNODE_TRIM_ON_BUILD
			uNodeEditorUtility.RemoveDefineSymbols(new string[] { "UNODE_TRIM_ON_BUILD" });
#else
			uNodeEditorUtility.AddDefineSymbols(new string[] { "UNODE_TRIM_ON_BUILD" });
#endif
		}

#if UNODE_TRIM_ON_BUILD
#if UNODE_TRIM_AGGRESSIVE
		[MenuItem("Tools/uNode/Advanced/Trim Mode: Aggressive", false, 10002)]
#else
		[MenuItem("Tools/uNode/Advanced/Trim Mode: Safe", false, 10002)]
#endif
		private static void AdvancedTrimAggressive() {
			if(uNodeEditorUtility.DisplayRequiredProVersion()) {
				return;
			}
#if UNODE_TRIM_AGGRESSIVE
			uNodeEditorUtility.RemoveDefineSymbols(new string[] { "UNODE_TRIM_AGGRESSIVE" });
#else
			uNodeEditorUtility.AddDefineSymbols(new string[] { "UNODE_TRIM_AGGRESSIVE" });
#endif
		}
#endif

		[MenuItem("Assets/Create/Create Asset Instance", false, 19)]
		public static void CreateAssetInstance() {
			if (Selection.activeObject is ClassDefinition classDefinition && classDefinition.model is ClassAssetModel) {
				var classAsset = ScriptableObject.CreateInstance<ClassAsset>();
				classAsset.target = classDefinition;
				ProjectWindowUtil.CreateAsset(classAsset, $"New_{classDefinition.GetGraphName()}.asset");
			}
			else {
				var items = ItemSelector.MakeCustomItemsForInstancedType(
					new Type[] { typeof(ClassDefinition) }, 
					(val) => {
						var graph = val as ClassDefinition;
						var classAsset = ScriptableObject.CreateInstance<ClassAsset>();
						classAsset.target = graph;
						ProjectWindowUtil.CreateAsset(classAsset, $"New_{graph.GetGraphName()}.asset");
					}, 
					false,
					obj => {
						if(obj is ClassDefinition definition && definition.model is ClassAssetModel) {
							return true;
						}
						return false;
					});
				var pos = EditorWindow.mouseOverWindow?.position ?? EditorWindow.focusedWindow?.position ?? Rect.zero;
				ItemSelector.ShowCustomItem(items).ChangePosition(pos);
			}
		}

		static List<string> GetObjDependencies(Object obj, HashSet<Object> scannedObjs) {
			List<string> result = new List<string>();
			if(!scannedObjs.Add(obj)) {
				return result;
			}
			result.AddRange(AssetDatabase.GetDependencies(AssetDatabase.GetAssetPath(obj), true));
			if(obj is MonoScript) {
				var monoScript = obj as MonoScript;
				var path = AssetDatabase.GetAssetPath(monoScript);
				if(path.EndsWith(".cs")) {
					var graphPath = path.RemoveLast(3).Add(".asset");
					if(File.Exists(graphPath)) {
						var graphObj = AssetDatabase.LoadAssetAtPath<ScriptableObject>(graphPath);
						if(graphObj is IGraph || graphObj is IScriptGraph) {
							result.AddRange(GetObjDependencies(graphObj, scannedObjs));
						}
					}
				}
				return result;
			}
			else if(obj is IScriptGraph) {
				var path = AssetDatabase.GetAssetPath(obj);
				if(path.EndsWith(".asset")) {
					var graphPath = Path.ChangeExtension(path, "cs");
					if(File.Exists(graphPath)) {
						var monoScript = AssetDatabase.LoadAssetAtPath<MonoScript>(graphPath);
						if(monoScript != null) {
							result.AddRange(GetObjDependencies(monoScript, scannedObjs));
						}
					}
				}
			}
			for(int i=0;i<result.Count;i++) {
				var o = AssetDatabase.LoadAssetAtPath<Object>(result[i]);
				if(o != null) {
					result.AddRange(GetObjDependencies(o, scannedObjs));
				}
			}
			return result;
		}

		static Dictionary<string, HashSet<MonoScript>> scriptsMaps;

		static void UpdateScriptMap() {
			scriptsMaps = new Dictionary<string, HashSet<MonoScript>>();
			var unodePath = uNodeEditorUtility.GetUNodePath();
			string[] assetPaths = AssetDatabase.GetAllAssetPaths();
			foreach(string assetPath in assetPaths) {
				if(assetPath.EndsWith(".cs") && !assetPath.StartsWith(unodePath + "/", StringComparison.Ordinal)) {
					var monoScript = AssetDatabase.LoadAssetAtPath<MonoScript>(assetPath);
					var type = monoScript.GetType();
					var assName = type.GetMethod("GetAssemblyName", MemberData.flags).InvokeOptimized(monoScript) as string;
					if(!scriptsMaps.TryGetValue(assName, out var monoScripts)) {
						monoScripts = new HashSet<MonoScript>();
						scriptsMaps[assName] = monoScripts;
					}
					monoScripts.Add(monoScript);
				}
			}
		}

		[MenuItem("Assets/Export uNode Graphs", false, 30)]
		public static void ExportSelectedGraphs() {
			EditorUtility.DisplayProgressBar("Finding Graphs Dependencies", "", 0);
			UpdateScriptMap();
			var guids = Selection.assetGUIDs;
			List<string> exportPaths = new List<string>();
			var hash = new HashSet<Object>();
			foreach(var guid in guids) {
				var path = AssetDatabase.GUIDToAssetPath(guid);
				var obj = AssetDatabase.LoadAssetAtPath<Object>(path);
				if(AssetDatabase.IsValidFolder(path)) {//Skip if folder or unknow type.
					var paths = AssetDatabase.GetAllAssetPaths().Where(p => p.StartsWith(path + "/", StringComparison.Ordinal));
					foreach(var subPath in paths) {
						var subAsset = AssetDatabase.LoadAssetAtPath<Object>(subPath);
						exportPaths.Add(subPath);
						exportPaths.AddRange(GetObjDependencies(subAsset, hash));
					}
					continue;
				}
				exportPaths.Add(path);
				exportPaths.AddRange(GetObjDependencies(obj, hash));
			}
			var unodePath = uNodeEditorUtility.GetUNodePath();
			var projectDir = Directory.GetCurrentDirectory();
			for(int i = 0; i < exportPaths.Count; i++) {
				var path = exportPaths[i];
				if(path.StartsWith(unodePath, StringComparison.Ordinal) || path == unodePath || !path.StartsWith("Assets", StringComparison.Ordinal) && !path.StartsWith("ProjectSettings", StringComparison.Ordinal)) {
					exportPaths.RemoveAt(i);
					i--;
					continue;
				}
			}
			EditorUtility.ClearProgressBar();
			ExportGraphWindow.Show(exportPaths.Distinct().OrderBy(p => p).ToArray());
		}

		class ExportGraphWindow : EditorWindow {
			[Serializable]
			class ExportData {
				public string path;
				public bool enable;
			}
			ExportData[] exportPaths;
			Vector2 scroll;

			static ExportGraphWindow window;

			public static ExportGraphWindow Show(string[] exportedPath) {
				window = GetWindow<ExportGraphWindow>(true);
				window.exportPaths = exportedPath.Select(p => new ExportData() { path = p, enable = true }).ToArray();
				window.minSize = new Vector2(300, 250);
				window.titleContent = new GUIContent("Export Graphs");
				window.Show();
				return window;
			}

			private void OnGUI() {
				if(exportPaths.Length == 0) {
					EditorGUILayout.HelpBox("Nothing to export", MessageType.Info);
					return;
				}
				GUILayout.BeginVertical();
				scroll = EditorGUILayout.BeginScrollView(scroll);
				for(int i = 0; i < exportPaths.Length; i++) {
					var data = exportPaths[i];
					var obj = AssetDatabase.LoadAssetAtPath<Object>(data.path);
					if(obj == null)
						continue;
					using(new GUILayout.HorizontalScope()) {
						data.enable = EditorGUILayout.Toggle(data.enable, GUILayout.Width(EditorGUIUtility.singleLineHeight));
						Texture icon = uNodeEditorUtility.GetTypeIcon(obj);
						if(obj is GameObject go) {
							var customIcon = go.GetComponent<ICustomIcon>();
							if(customIcon != null) {
								icon = uNodeEditorUtility.GetTypeIcon(customIcon);
							} else {
								//var unode = go.GetComponent<uNodeComponentSystem>();
								//if(unode != null) {
								//	icon = uNodeEditorUtility.GetTypeIcon(unode);
								//}
							}
						}
						EditorGUILayout.LabelField(new GUIContent(icon), GUILayout.Width(EditorGUIUtility.singleLineHeight));
						EditorGUILayout.LabelField(new GUIContent(data.path));
					}
				}
				EditorGUILayout.EndScrollView();
				GUILayout.FlexibleSpace();
				if(GUILayout.Button("Export")) {
					var savePath = EditorUtility.SaveFilePanel("Export Graphs", "", "", "unitypackage");
					if(!string.IsNullOrEmpty(savePath)) {
						AssetDatabase.ExportPackage(exportPaths.Where(p => p.enable).Select(p => p.path).ToArray(), savePath);
						Close();
					}
				}
				GUILayout.EndVertical();
			}
		}

		[MenuItem("Assets/Create Instance", false, 19)]
		public static void CreateUNodeAssetInstance() {
			if(Selection.activeObject is ClassDefinition classDefinition) {
				var classAsset = ScriptableObject.CreateInstance<ClassAsset>();
				classAsset.target = classDefinition;
				ProjectWindowUtil.CreateAsset(classAsset, $"New_{classDefinition.GetGraphName()}.asset");
			}
		}

		[MenuItem("Assets/Create Instance", true, 19)]
		public static bool CanCreateUNodeAssetInstance() {
			if(Selection.activeObject is ClassDefinition classDefinition) {
				bool IsValid(ClassDefinition definition) {
					if(definition == null) return false;
					if(definition.model is InheritedModel inheritedModel) {
						return IsValid(inheritedModel.inheritFrom);
					}
					else if(definition.model is ClassAssetModel) {
						return true;
					}
					return false;
				}
				return IsValid(classDefinition);
			}
			return false;
		}

		[MenuItem("Assets/Create/uNode/New Graph", false, -10001)]
		private static void CreateClassAsset() {
			GraphCreatorWindow.ShowWindow();
		}

		[MenuItem("Assets/Create/uNode/Editor/Graph Theme")]
		private static void CreateTheme() {
			CustomAssetUtility.CreateAsset<UIElementEditorTheme>((theme) => {
				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();
				EditorUtility.FocusProjectWindow();
			});
		}

		private static void CreatePrefabWithComponent<T>(string name) where T : Component {
			GameObject go = new GameObject(name);
			go.AddComponent<T>();
			string path = CustomAssetUtility.GetCurrentPath() + "/New_" + go.name + ".prefab";
			int index = 0;
			while(File.Exists(path)) {
				index++;
				path = CustomAssetUtility.GetCurrentPath() + "/New_" + go.name + index + ".prefab";
			}
#if UNITY_2018_3_OR_NEWER
			GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
#else
			GameObject prefab = PrefabUtility.CreatePrefab(path, go);
#endif
			Object.DestroyImmediate(go);
			AssetDatabase.SaveAssets();
			EditorUtility.FocusProjectWindow();
			Selection.activeObject = prefab;
		}
	}
}