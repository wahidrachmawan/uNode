#if UNITY_2019_4_OR_NEWER
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;

namespace MaxyGames.UNode.Editors {
	static class uNodeDomainReloader {
		[MenuItem("Tools/uNode/Advanced/Reload Scripts", false, 10003)]
		private static void RecompileScripts() {
			//EditorUtility.UnloadUnusedAssetsImmediate();
			var activeTex = Resources.FindObjectsOfTypeAll<Texture2D>();
			foreach(var texture in activeTex) {
				if(texture.hideFlags == HideFlags.HideAndDontSave && 
					texture.isReadable && string.IsNullOrEmpty(texture.name) && 
					string.IsNullOrEmpty(AssetDatabase.GetAssetPath(texture)) && !EditorUtility.IsPersistent(texture)) {
					//Debug.Log(texture.format);
					if(texture.format == TextureFormat.DXT5 || texture.format == TextureFormat.DXT5Crunched || texture.format == TextureFormat.RGBA32) {
						Object.DestroyImmediate(texture);
					} 
				}
			}
			EditorUtility.RequestScriptReload();
		}
	}
}
#endif