using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.Reflection;
using System.Threading;
using UnityEngine.SceneManagement;
//using UnityEngine.Events;
//using UnityEngine.SceneManagement;

namespace MaxyGames.UNode.Editors {
	public static class EditorBinding {
		public static Action<Type, Type> patchType;
		public static Action<GameObject, UnityEngine.Object> savePrefabAsset;

		public static event EditorSceneManager.NewSceneCreatedCallback onNewSceneCreated;
		public static event EditorSceneManager.SceneClosingCallback onSceneClosing;
		//public static UnityAction<Scene, Scene> onSceneChanged;
		public static event EditorSceneManager.SceneSavingCallback onSceneSaving;
		public static event EditorSceneManager.SceneSavedCallback onSceneSaved;
		public static event EditorSceneManager.SceneOpeningCallback onSceneOpening;
		public static event EditorSceneManager.SceneOpenedCallback onSceneOpened;
		public static event Action onFinishCompiling;

		internal static Type syntaxHighlighter { get; private set; }
		private static Func<string, string> syntaxAnalizer;

		public static string AnalizeCode(string syntax) {
#if UNODE_PRO
			if(syntaxAnalizer != null) {
				return syntaxAnalizer(syntax);
			}
#endif
			return syntax;
		}

		public static string HighlightSyntax(string syntax) {
			var syntaxHighlighter = EditorBinding.syntaxHighlighter;
			if(syntaxHighlighter != null) {
				string highlight = syntaxHighlighter.GetMethod("GetRichText", new[] { typeof(string) }).InvokeOptimized(null, new object[] { syntax }) as string;
				if(!string.IsNullOrEmpty(highlight)) {
					return highlight;
				}
			}
			return null;
		}

		[InitializeOnLoadMethod]
		internal static void OnInitialize() {
			GraphUtility.Initialize();
			EditorSceneManager.newSceneCreated += OnNewSceneCreated;
			EditorSceneManager.sceneClosing += OnSceneClosing;
			EditorSceneManager.sceneSaving += OnSceneSaving;
			EditorSceneManager.sceneSaved += OnSceneSaved;
			EditorSceneManager.sceneOpening += OnSceneOpening;
			EditorSceneManager.sceneOpened += OnSceneOpened;

#if UNODE_PRO
			syntaxHighlighter = "MaxyGames.UNode.SyntaxHighlighter.CSharpSyntaxHighlighter".ToType(false);
			var analizer = "MaxyGames.UNode.Editors.SyntaxAnalizer".ToType(false);
			if(analizer != null) {
				var m_analizer = analizer.GetMethod("AnalizeCode", MemberData.flags);
				syntaxAnalizer = (val) => {
					return m_analizer.InvokeOptimized(null, val) as string;
				};
			}
#endif
		}

		private static void OnSceneOpened(Scene scene, OpenSceneMode mode) {
			onSceneOpened?.Invoke(scene, mode);
		}

		private static void OnSceneOpening(string path, OpenSceneMode mode) {
			onSceneOpening?.Invoke(path, mode);
		}

		private static void OnSceneSaved(Scene scene) {
			onSceneSaved?.Invoke(scene);
		}

		private static void OnSceneSaving(Scene scene, string path) {
			onSceneSaving?.Invoke(scene, path);
		}

		private static void OnSceneClosing(Scene scene, bool removingScene) {
			onSceneClosing?.Invoke(scene, removingScene);
		}

		private static void OnNewSceneCreated(Scene scene, NewSceneSetup setup, NewSceneMode mode) {
			onNewSceneCreated?.Invoke(scene, setup, mode);
		}

		[UnityEditor.Callbacks.DidReloadScripts]
		internal static void OnScriptReloaded() {
			if(onFinishCompiling != null)
				onFinishCompiling();
			if(uNodeUtility.temporaryObjects.Count > 0) {
				foreach(var obj in uNodeUtility.temporaryObjects) {
					if(obj != null && EditorUtility.IsPersistent(obj) == false) {
						//Make sure to destroy objects that are not destroyed yet when script reloads.
						UnityEngine.Object.DestroyImmediate(obj);
					}
				}
				uNodeUtility.temporaryObjects.Clear();
			}
		}
	}
}