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
		internal static Type codeFormatter { get; private set; }

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
			codeFormatter = "MaxyGames.UNode.Editors.CSharpFormatter".ToType(false);
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
		}
	}
}