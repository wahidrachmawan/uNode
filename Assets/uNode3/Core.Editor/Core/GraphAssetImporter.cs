using System.Collections;
using System.Collections.Generic;
using UnityEditor.AssetImporters;
using UnityEngine;
using UnityEditor;
using System.Linq;
using UnityEditorInternal;
using System.Reflection;

namespace MaxyGames.UNode.Editors {
	[ScriptedImporter(1, "unodescript")]
	class GraphAssetImporter : ScriptedImporter {
		public override void OnImportAsset(AssetImportContext ctx) {
			var objs = InternalEditorUtility.LoadSerializedFileAndForget(ctx.assetPath);
			if(objs != null && objs.Length > 0) {

			}
			else {
				objs = new[] { ScriptableObject.CreateInstance<ScriptGraph>() };
			}
			var loaded = objs[0] as ScriptGraph;
			//loaded.hideFlags &= ~HideFlags.DontUnloadUnusedAsset;
			if(objs.Length > 1) {
				for(int i = 1; i < objs.Length; i++) {
					ctx.AddObjectToAsset(i.ToString(), objs[i], uNodeEditorUtility.GetTypeIcon(objs[i]) as Texture2D);
					//objs[i].hideFlags &= ~HideFlags.DontUnloadUnusedAsset;
				}
			}

			ctx.AddObjectToAsset("main", loaded);
			ctx.SetMainObject(loaded);

			Texture icon = uNodeEditorUtility.GetTypeIcon(loaded);
			if(icon is Texture2D) {
				EditorGUIUtility.SetIconForObject(loaded, icon as Texture2D);
			}
		}

		/// =======================================================
		/// Auto-save Changes
		/// =======================================================
		[InitializeOnLoad]
		class GraphAssetSaver : AssetModificationProcessor {
			static GraphAssetSaver() {
				GraphUtility.SaveCallback += static (obj) => {
					if(obj is ScriptGraph scriptGraph && EditorUtility.IsPersistent(scriptGraph)) {
						var path = AssetDatabase.GetAssetPath(scriptGraph);
						if(string.IsNullOrEmpty(path) == false && path.EndsWith(".unodescript")) {
							WriteToFile(path, scriptGraph);
						}
					}
				};
			}
			
			static string[] OnWillSaveAssets(string[] paths) {
				foreach(string path in paths) {
					if(path.EndsWith(".unodescript")) {
						var obj = AssetDatabase.LoadAssetAtPath<ScriptGraph>(path);
						WriteToFile(path, obj);
					}
				}
				return paths;
			}

			static void WriteToFile(string path, ScriptGraph obj) {
				if(obj != null) {
					var val = Object.Instantiate(obj);
					val.name = obj.name;
					var classes = val.TypeList.references.Select(o => Object.Instantiate(o)).ToArray();
					var dic = new Dictionary<Object, Object>(1 + val.TypeList.references.Count);
					dic[obj] = val;
					for(int i = 0; i < val.TypeList.references.Count; i++) {
						classes[i].name = val.TypeList.references[i].name;
						dic[val.TypeList.references[i]] = classes[i];
						val.TypeList.references[i] = classes[i];
					}
					//Only redirect references when the objects more than 1
					if(dic.Count > 1) {
						RedirectReferences(obj, dic);
						for(int i = 0; i < val.TypeList.references.Count; i++) {
							RedirectReferences(val.TypeList.references[i], dic);
						}
					}
					InternalEditorUtility.SaveToSerializedFileAndForget(
						new UnityEngine.Object[] { val }.Concat(val.TypeList).ToArray(),
						path,
						true
					);
					//foreach(var (_, o) in dic) {
					//	DestroyImmediate(o);
					//}

					//InternalEditorUtility.SaveToSerializedFileAndForget(
					//	new UnityEngine.Object[] { obj }.Concat(obj.TypeList).ToArray(),
					//	path,
					//	true
					//);

					AssetDatabase.ImportAsset(path);
				}
			}

			static void RedirectReferences(Object obj, Dictionary<Object, Object> map) {
				var hash = StaticHashPool<object>.Allocate();

				const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy;
				bool Recursive(object obj) {
					if(hash.Add(obj) == false) return false;
					if(obj is Pointer) {
						return false;
					}
					bool changed = false;
					if(obj is IList list) {
						for(int i = 0; i < list.Count; i++) {
							if(list[i] == null) continue;
							if(list[i] is Object uobj) {
								if(map.TryGetValue(uobj, out var o)) {
									list[i] = o;
								}
								continue;
							}
							changed |= Recursive(list[i]);
						}
						return changed;
					}

					var fields = EditorReflectionUtility.GetFields(obj.GetType(), flags);

					foreach(var field in fields) {
						// If the field is static or readonly, skip it
						if(field.IsInitOnly) continue;
						// If the field is not serialized, skip it
						if(field.IsNotSerialized) continue;
						if(field.IsPrivate) {
							// If the field is private, check if it has SerializeField attribute
							if(!field.IsDefined(typeof(SerializeField)) && !field.IsDefined(typeof(SerializeReference))) {
								// If it doesn't have SerializeField, skip it
								continue;
							}
						}
						var fieldValue = field.GetValueOptimized(obj);
						if(fieldValue == null) continue;
						if(fieldValue is Object uobj) {
							if(map.TryGetValue(uobj, out var o)) {
								field.SetValueOptimized(obj, o);
							}
							continue;
						}
						if(Recursive(fieldValue)) {
							if(fieldValue is System.ValueType) {
								field.SetValueOptimized(obj, fieldValue);
							}
							changed = true;
						}
					}
					return changed;
				}
				Recursive(obj);

				StaticHashPool<object>.Free(hash);
			}
		}
	}
}