using UnityEngine;
using UnityEditor;
using System.IO;

namespace MaxyGames.UNode.Editors {
	public static class CustomAssetUtility {
		public static string GetCurrentPath() {
			string path = AssetDatabase.GetAssetPath(Selection.activeObject);
			if(path == "") {
				path = "Assets";
			} else if(Path.GetExtension(path) != "") {
				path = path.Replace(Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), "");
			}

			return path;
		}

		public static void CreateAsset<T>() where T : ScriptableObject {
			T asset = ScriptableObject.CreateInstance<T>();
			ProjectWindowUtil.CreateAsset(asset, $"New {typeof(T).ToString()}.asset");
			Selection.activeObject = asset;
		}

		public static void CreateAsset<T>(System.Action<T> action) where T : ScriptableObject {
			T asset = ScriptableObject.CreateInstance<T>();
			if(action != null) {
				action(asset);
			}
			ProjectWindowUtil.CreateAsset(asset, $"New {typeof(T).ToString()}.asset");
			Selection.activeObject = asset;
		}

		public static void CreateAsset(System.Type type) {
			CreateAsset(type, type.Name);
		}

		public static void CreateAsset(System.Type type, string assetName) {
			ScriptableObject asset = ScriptableObject.CreateInstance(type);

			string path = AssetDatabase.GetAssetPath(Selection.activeObject);
			if(path == "") {
				path = "Assets";
			} else if(Path.GetExtension(path) != "") {
				path = path.Replace(Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), "");
			}

			string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + "/" + assetName + ".asset");

			AssetDatabase.CreateAsset(asset, assetPathAndName);

			AssetDatabase.SaveAssets();
			EditorUtility.FocusProjectWindow();
			Selection.activeObject = asset;
		}
	}
}