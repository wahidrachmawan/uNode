using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MaxyGames.UNode {
	public class uNodeDatabase : ScriptableObject {
		[System.Serializable]
		public class GraphAssetDatabase {
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
					m_fileGuid = M_GetGraphGUID(asset);
					m_fileUniqueID = 0;
				}
#endif
			}
		}
		public List<GraphAssetDatabase> graphDatabases = new List<GraphAssetDatabase>();

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

		private HashSet<Type> m_nativeGraphTypes;
		/// <summary>
		/// Get all CLR types that's available to the native graphs in the database.
		/// </summary>
		public HashSet<Type> nativeGraphTypes {
			get {
				if(m_nativeGraphTypes == null) {
					m_nativeGraphTypes = new HashSet<Type>();
					foreach(var data in nativeGraphDatabases) {
						if(data.ScriptGraph == null) continue;
						//foreach(var typeData in data.ScriptGraph.ScriptData.typeDatas) {
						//	Debug.Log(typeData.typeName + " - " + typeData.typeName.ToType(false));
						//}
						foreach(var typeData in data.ScriptGraph.TypeList) {
							if(typeData is IScriptGraphType scriptGraphType) {
								var type = scriptGraphType.TypeName.ToType(false);
								if(type != null) {
									m_nativeGraphTypes.Add(type);
									continue;
								}
							}
							if(typeData is IReflectionType reflectionType) {
								var RType = reflectionType.ReflectionType;
								if(RType != null) {
									var type = RType.FullName.ToType(false);
									if(type != null) {
										m_nativeGraphTypes.Add(type);
										continue;
									}
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
				return m_nativeGraphTypes;
			}
		}

		/// <summary>
		/// Get the graph GUID from the graph asset, the value should be persistence.
		/// </summary>
		/// <param name="graphAsset"></param>
		/// <returns></returns>
		public string GetGraphGUID(GraphAsset graphAsset) {
			foreach(var db in graphDatabases) {
				if(db.asset == graphAsset) {
					return db.assetGuid;
				}
			}
			return M_GetGraphGUID(graphAsset);
		}

		private static string M_GetGraphGUID(GraphAsset graphAsset) {
#if UNITY_EDITOR
			if(graphAsset != null) {
				var path = UnityEditor.AssetDatabase.GetAssetPath(graphAsset);
				if(!string.IsNullOrEmpty(path)) {
					if(UnityEditor.AssetDatabase.IsMainAsset(graphAsset) == false) {
						if(UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier(graphAsset, out var guid, out long localID)) {
							return guid + "-" + localID;
						}
					}
					return UnityEditor.AssetDatabase.AssetPathToGUID(path);
				}
			}
#endif
			return string.Empty;
		}

		private Dictionary<string, GraphAssetDatabase> m_graphDBMap = new Dictionary<string, GraphAssetDatabase>();
		/// <summary>
		/// Get the graph database by unique graph ID, usually the full graph name or the class identifier.
		/// </summary>
		/// <param name="graphUID"></param>
		/// <returns></returns>
		public GraphAssetDatabase GetGraphDatabase(string graphUID) {
			if(!m_graphDBMap.TryGetValue(graphUID, out var data)) {
				foreach(var db in graphDatabases) {
					if(db.asset != null && db.uniqueID == graphUID) {
						data = db;
						m_graphDBMap[graphUID] = data;
						break;
					}
				}
			}
			return data;
		}

		public GraphAssetDatabase GetGraphDatabase<T>() where T : class {
			return GetGraphDatabase(typeof(T).FullName);
		}

		private Dictionary<string, GraphAssetDatabase> m_graphMap = new Dictionary<string, GraphAssetDatabase>();
		/// <summary>
		/// Get the graph database by asset GUID
		/// </summary>
		/// <param name="assetGuid"></param>
		/// <returns></returns>
		public GraphAssetDatabase GetGraphDatabaseByGuid(string assetGuid) {
			if(!m_graphMap.TryGetValue(assetGuid, out var data)) {
				foreach(var db in graphDatabases) {
					if(db.asset != null && db.assetGuid == assetGuid) {
						data = db;
						m_graphMap[assetGuid] = data;
						break;
					}
				}
			}
			return data;
		}

		/// <summary>
		/// Get the graph asset by unique graph ID, usually the full graph name or the class identifier.
		/// </summary>
		/// <param name="graphUID"></param>
		/// <param name="throwOnNull"></param>
		/// <returns></returns>
		/// <exception cref="System.Exception"></exception>
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

		private Dictionary<string, IGlobalEvent> m_globalEventDBMap = new Dictionary<string, IGlobalEvent>();
		/// <summary>
		/// Get the global event by its GUID.
		/// </summary>
		/// <param name="guid"></param>
		/// <returns></returns>
		/// <exception cref="Exception"></exception>
		public IGlobalEvent GetGlobalEvent(string guid) {
			if(!m_globalEventDBMap.TryGetValue(guid, out var data)) {
				foreach(var db in globalEventDatabases) {
					if(db.guid == guid) {
						data = db.asset as IGlobalEvent;
						m_globalEventDBMap[guid] = data;
						break;
					}
				}
#if UNITY_EDITOR
				if(data == null) {
					//If inside of Unity Editor, load the event from GUID instead for fix outdated databases.
					var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<ScriptableObject>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
					if(asset is IGlobalEvent evt) {
						data = evt;
						m_globalEventDBMap[guid] = data;
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

		/// <summary>
		/// Clear the cached data, usually called when the database has changed.
		/// </summary>
		public static void ClearCache() {
			if(instance != null) {
				instance.ResetCache();
			}
		}

		void ResetCache() {
			m_nativeGraphTypes = null;
			m_graphDBMap.Clear();
			m_graphMap.Clear();
			m_globalEventDBMap.Clear();
		}

		public static uNodeDatabase instance => uNodeUtility.GetDatabase();
	}
}