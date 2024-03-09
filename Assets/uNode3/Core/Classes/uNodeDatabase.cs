using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MaxyGames.UNode {
	public class uNodeDatabase : ScriptableObject {
		[System.Serializable]
		public class RuntimeGraphDatabase {
			[SerializeField]
			private GraphAsset graph;
			public GraphAsset asset {
				get {
					return graph;
				}
				set {
					graph = value;
					Update();
				}
			}

			[SerializeField]
			private string m_fileGuid;
			/// <summary>
			/// Get the graph GUID, the value should be persistence.
			/// </summary>
			public string assetGuid {
				get {
#if UNITY_EDITOR
					if(string.IsNullOrEmpty(m_fileGuid)) {
						Update();
					}
#endif
					return m_fileGuid;
				}
			}

			public string uniqueID => asset is IClassIdentifier ? (asset as IClassIdentifier).uniqueIdentifier : asset.GetFullGraphName();
			[NonSerialized]
			private int m_fileUniqueID;
			/// <summary>
			/// Get the file ID, the value are not persistence
			/// </summary>
			public int fileUniqueID {
				get {
					if(m_fileUniqueID == 0 && asset != null) {
						m_fileUniqueID = uNodeUtility.GetObjectID(asset);
					}
					return m_fileUniqueID;
				}
			}
			
			public void Update() {
#if UNITY_EDITOR
				if(graph != null) {
					m_fileGuid = UnityEditor.AssetDatabase.AssetPathToGUID(UnityEditor.AssetDatabase.GetAssetPath(asset));
					m_fileUniqueID = 0;
				}
#endif
			}
		}
		public List<RuntimeGraphDatabase> graphDatabases = new List<RuntimeGraphDatabase>();

		[Serializable]
		public class NativeGraphDatabase {
			[SerializeField]
			private ScriptableObject scriptGraph;

			public IScriptGraph ScriptGraph {
				get => scriptGraph as IScriptGraph;
				set => scriptGraph = value as ScriptableObject;
			}
		}
		public List<NativeGraphDatabase> nativeGraphDatabases = new List<NativeGraphDatabase>();

		[System.Serializable]
		public class RuntimeGlobalEventDatabase {
			public ScriptableObject asset;
			public string guid;
		}
		public List<RuntimeGlobalEventDatabase> globalEventDatabases = new List<RuntimeGlobalEventDatabase>();

		#region Editors
#if UNITY_EDITOR
		[System.Serializable]
		public class EditorData {
			public string guid;
			public OdinSerializedData serializedData;
		}
		[SerializeField]
		private List<EditorData> editorDatas = new List<EditorData>();
		[SerializeField]
		internal List<SerializedType> aotTypes = new List<SerializedType>();


		internal void SaveEditorData<T>(string guid, T value) {
			var data = SerializerUtility.SerializeValue(value);
			var idx = editorDatas.FindIndex(item => item.guid == guid);
			if(idx >= 0) {
				editorDatas[idx].serializedData = data;
			} else {
				editorDatas.Add(new EditorData() {
					guid = guid,
					serializedData = data,
				});
			}
		}

		internal T LoadEditorData<T>(string guid) {
			var idx = editorDatas.FindIndex(item => item.guid == guid);
			if(idx >= 0) {
				return SerializerUtility.Deserialize<T>(editorDatas[idx].serializedData);
			} else {
				return default;
			}
		}
#endif
		#endregion

		private static HashSet<Type> m_nativeGraphTypes;
		public static HashSet<Type> nativeGraphTypes {
			get {
				if(m_nativeGraphTypes == null) {
					var db = uNodeUtility.GetDatabase();
					if(db == null) {
						m_nativeGraphTypes = new HashSet<Type>();
					}
					else {
						m_nativeGraphTypes = new HashSet<Type>();
						foreach(var data in db.nativeGraphDatabases) {
							if(data.ScriptGraph == null) continue;
							//foreach(var typeData in data.ScriptGraph.ScriptData.typeDatas) {
							//	Debug.Log(typeData.typeName + " - " + typeData.typeName.ToType(false));
							//}
							foreach(var typeData in data.ScriptGraph.TypeList) {
								if(typeData is IScriptGraphType scriptGraphType) {
									var type = scriptGraphType.TypeName.ToType(false);
									if(type != null) {
										m_nativeGraphTypes.Add(type);
									}
								}
							}
						}
						//m_nativeGraphTypes = new HashSet<Type>(
						//	db.graphDatabases
						//		.Where(data => data.asset is IScriptGraphType graphType && graphType.TypeName.ToType(false) != null)
						//		.Select(data => (data.asset as IScriptGraphType).TypeName.ToType(false))
						//);
					}
				}
				return m_nativeGraphTypes;
			}
		}

		private Dictionary<string, RuntimeGraphDatabase> graphDBMap = new Dictionary<string, RuntimeGraphDatabase>();
		public RuntimeGraphDatabase GetGraphDatabase(string graphUID) {
			if (!graphDBMap.TryGetValue(graphUID, out var data)) {
				foreach (var db in graphDatabases) {
					if (db.asset != null && db.uniqueID == graphUID) {
						data = db;
						graphDBMap[graphUID] = data;
						break;
					}
				}
			}
			return data;
		}

		private Dictionary<string, IGlobalEvent> globalEventDBMap = new Dictionary<string, IGlobalEvent>();
		public IGlobalEvent GetGlobalEvent(string guid) {
			if(!globalEventDBMap.TryGetValue(guid, out var data)) {
				foreach(var db in globalEventDatabases) {
					if(db.guid == guid) {
						data = db.asset as IGlobalEvent;
						globalEventDBMap[guid] = data;
						break;
					}
				}
#if UNITY_EDITOR
				if(data == null) {
					//If inside of Unity Editor, load the event from GUID instead for fix outdated databases.
					var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<ScriptableObject>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
					if(asset is IGlobalEvent evt) {
						data = evt;
						globalEventDBMap[guid] = data;
						globalEventDatabases.Add(new RuntimeGlobalEventDatabase() {
							asset = asset,
							guid = guid,
						});
					}
				}
#endif
				if(data == null) {
					throw new Exception($"No global event with id: {guid} found");
				}
			}
			return data;
		}

//		private Dictionary<string, GraphAsset> m_graphByGUID = new Dictionary<string, GraphAsset>();
//		public GraphAsset GetGraphByGUID(string guid, bool throwOnNull = true) {
//			if(!m_graphByGUID.TryGetValue(guid, out var data)) {
//				foreach(var db in graphDatabases) {
//					if(db.guid == guid) {
//						data = db.graph;
//						m_graphByGUID[guid] = data;
//						break;
//					}
//				}
//#if UNITY_EDITOR
//				if(data == null) {
//					//If inside of Unity Editor, load the event from GUID instead for fix outdated databases.
//					var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<ScriptableObject>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
//					if(asset is GraphAsset evt) {
//						data = evt;
//						m_graphByGUID[guid] = data;
//					}
//				}
//#endif
//			}
//			return data;
//		}

		public GraphAsset GetGraphByUID(string graphUID, bool throwOnNull = true) {
			var data = GetGraphDatabase(graphUID);
			if(data != null) {
				return data.asset;
			}
			if(throwOnNull) {
				throw new System.Exception($"There's no graph with id: {graphUID} or maybe the database is outdated if so please refresh the database.");
			}
			else {
				return null;
			}
		}

		public RuntimeGraphDatabase GetGraphDatabase<T>() where T : class {
			return GetGraphDatabase(typeof(T).FullName);
		}

		public static void ClearCache() {
			if(instance != null) {
				instance.graphDBMap.Clear();
			}
			m_nativeGraphTypes = null;
		}

		public static uNodeDatabase instance => uNodeUtility.GetDatabase();
	}
}