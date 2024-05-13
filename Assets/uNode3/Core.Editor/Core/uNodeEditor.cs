using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.IMGUI.Controls;

namespace MaxyGames.UNode.Editors {
    public partial class uNodeEditor : EditorWindow {
		#region Const
		internal const string MESSAGE_PATCH_WARNING = "Patching should be done in 'Compatibility' generation mode or there will be visible/invisible errors, please use 'Compatibility' mode when trying to make live changes to compiled code.";
		#endregion

		#region Classes
		public class GraphExplorerTree : UnityEditor.IMGUI.Controls.TreeView {
			public HierarchyGraphTree selected;

			Dictionary<int, TreeViewItem> treeMap;

			public GraphExplorerTree() : base(new TreeViewState()) {
				showAlternatingRowBackgrounds = true;
				showBorder = true;
				Reload();
			}

			protected override TreeViewItem BuildRoot() {
				return new TreeViewItem { id = 0, depth = -1 };
			}

			protected override bool CanChangeExpandedState(TreeViewItem item) {
				return false;
			}

			protected override IList<TreeViewItem> BuildRows(TreeViewItem root) {
				var rows = GetRows() ?? new List<TreeViewItem>();
				rows.Clear();
				var assets = GraphUtility.FindAllGraphAssets();
				var graphDic = new Dictionary<string, List<(string, UnityEngine.Object)>>();
				void DoAddTree(UnityEngine.Object asset) {
					if(asset is IScriptGraph scriptGraph) {
						var types = scriptGraph.TypeList;
						foreach(var type in types) {
							DoAddTree(type);
						}
					}
					else if(asset is IGraph graph) {
						var id = graph is MacroGraph ? "Macro" : graph.GetGraphNamespace();
						if(string.IsNullOrEmpty(id)) {
							id = "global";
						}
						if(!graphDic.TryGetValue(id, out var graphs)) {
							graphs = new List<(string, UnityEngine.Object)>();
							graphDic.Add(id, graphs);
						}
						graphs.Add((graph.GetGraphName(), asset));
					}
				}
				foreach(var asset in assets) {
					DoAddTree(asset);
				}
				var dic = graphDic.ToList();
				dic.Sort((x, y) => string.Compare(x.Key, y.Key, StringComparison.OrdinalIgnoreCase));
				treeMap = new Dictionary<int, TreeViewItem>();
				foreach(var pair in dic) {
					var graphs = pair.Value;
					graphs.Sort((x, y) => string.Compare(x.Item1, y.Item1, StringComparison.OrdinalIgnoreCase));
					TreeViewItem nsTree = root;
					if(!hasSearch) {
						nsTree = new HiearchyNamespaceTree(string.IsNullOrEmpty(pair.Key) ? "global" : pair.Key, -1);
						root.AddChild(nsTree);
						rows.Add(nsTree);
						treeMap[nsTree.id] = nsTree;
					}
					foreach(var (displayName, asset) in graphs) {
						if(hasSearch) {
							if(displayName.IndexOf(searchString, StringComparison.OrdinalIgnoreCase) >= 0) {
								TreeViewItem tree;
								if(asset is GraphAsset graph) {
									tree = new HierarchyGraphTree(graph, -1);
									if(!string.IsNullOrEmpty(graph.GraphData.comment)) {
										var strs = graph.GraphData.comment.Split('\n');
										for(int i = 0; i < strs.Length; i++) {
											if(string.IsNullOrEmpty(strs[i]))
												continue;
											var summary = new HierarchySummaryTree(strs[i], tree);
											summary.id = uNodeEditorUtility.GetUIDFromString(tree.id.ToString() + "[SUMMARY]" + i.ToString());
											nsTree.AddChild(summary);
											rows.Add(summary);
											treeMap[summary.id] = summary;
										}
									}
								}
								else {
									//TODO: display enum
									tree = null;
								}
								if(tree != null) {
									nsTree.AddChild(tree);
									rows.Add(tree);
									treeMap[tree.id] = tree;
								}
							}
						}
						else {
							if(asset is GraphAsset graph) {
								var tree = new HierarchyGraphTree(graph, -1);
								if(!string.IsNullOrEmpty(graph.GraphData.comment)) {
									var strs = graph.GraphData.comment.Split('\n');
									for(int i = 0; i < strs.Length; i++) {
										if(string.IsNullOrEmpty(strs[i]))
											continue;
										var summary = new HierarchySummaryTree(strs[i], tree);
										summary.id = uNodeEditorUtility.GetUIDFromString(tree.id.ToString() + "[SUMMARY]" + i.ToString());
										nsTree.AddChild(summary);
										rows.Add(summary);
										treeMap[summary.id] = summary;
									}
								}
								nsTree.AddChild(tree);
								rows.Add(tree);
								treeMap[tree.id] = tree;
							}
						}
					}
				}
				SetupDepthsFromParentsAndChildren(root);
				return rows;
			}

			protected override void SelectionChanged(IList<int> selectedIds) {
				if(selectedIds.Count > 0) {
					var tree = treeMap[selectedIds.FirstOrDefault()];
					if(tree is HierarchyGraphTree graphTree) {
						selected = graphTree;
					}
					else if(tree is HierarchySummaryTree summaryTree) {
						selected = summaryTree.owner as HierarchyGraphTree;
					}
				}
				else {
					selected = null;
				}
			}

			protected override bool CanMultiSelect(TreeViewItem item) {
				return false;
			}

			protected override void DoubleClickedItem(int id) {
				if(treeMap.TryGetValue(id, out var tree)) {
					if(tree is HierarchyGraphTree graphTree) {
						Open(graphTree.graph);
					}
					else if(tree is HierarchySummaryTree summaryTree) {
						graphTree = summaryTree.owner as HierarchyGraphTree;
						if(graphTree != null) {
							Open(graphTree.graph);
						}
					}
				}
			}

			protected override void RowGUI(RowGUIArgs args) {
				Event evt = Event.current;
				if(evt.type == EventType.Repaint) {
					#region Draw Row
					Rect labelRect = args.rowRect;
					labelRect.x += GetContentIndent(args.item);
					//if(args.selected) {
					//	uNodeGUIStyle.itemStatic.Draw(labelRect, new GUIContent(args.label, icon), false, false, false, false);
					//} else {
					//	uNodeGUIStyle.itemNormal.Draw(labelRect, new GUIContent(args.label, icon), false, false, false, false);
					//}
					if(args.item is HierarchySummaryTree) {
						uNodeGUIStyle.itemNormal.Draw(labelRect, new GUIContent(uNodeUtility.WrapTextWithColor("//" + args.label, uNodeUtility.GetRichTextSetting().summaryColor), args.item.icon), false, false, false, false);
					}
					else {
						uNodeGUIStyle.itemNormal.Draw(labelRect, new GUIContent(args.label, args.item.icon), false, false, false, false);
					}
					#endregion
				}
				//base.RowGUI(args);
			}
		}

		[Serializable]
		class EditorTabDatas {
			public int selectedIndex;
			public TabData main = new TabData();
			public List<TabData> datas = new List<TabData>();

			[System.Runtime.Serialization.OnDeserialized]
			void OnDeserialized() {
				if (datas == null)
					datas = new List<TabData>();
				if (main == null)
					main = new TabData();
			}
		}
		[SerializeField]
		private EditorTabDatas tabList;

		[Serializable]
		public class TabData {
			public List<GraphEditorData> graphDatas = new List<GraphEditorData>();

			[SerializeField]
			private int selectedIndex;
			[SerializeField]
			private UnityEngine.Object _owner;

			[System.Runtime.Serialization.OnDeserialized]
			void OnDeserialized() {
				if (graphDatas == null)
					graphDatas = new List<GraphEditorData>();
			}

			internal void RemoveIncorrectDatas() {
				var selectedData = selectedGraphData;
				for(int y = 0; y < graphDatas.Count; y++) {
					if(graphDatas[y] != selectedData && !graphDatas[y].isValidGraph) {
						graphDatas.RemoveAt(y);
						y--;
					}
				}
				selectedGraphData = selectedData;
			}

			/// <summary>
			/// The current selected graph editor data
			/// </summary>
			/// <value></value>
			public GraphEditorData selectedGraphData {
				get {
					if(graphDatas.Count == 0 || selectedIndex >= graphDatas.Count || selectedIndex < 0) {
						graphDatas.Add(new GraphEditorData());
						selectedIndex = graphDatas.Count - 1;
						return graphDatas[selectedIndex];
					}
					return graphDatas[selectedIndex];
				}
				set {
					if(value == null) {
						selectedIndex = -1;
						return;
					}
					for(int i = 0; i < graphDatas.Count; i++) {
						if(graphDatas[i] == value) {
							selectedIndex = i;
							return;
						}
					}
					graphDatas.Add(value);
					selectedIndex = graphDatas.Count - 1;
				}
			}

			public string displayName {
				get {
					if(graph != null) {
						return graph.GetGraphName();
					}
					try {
						if(_owner == null || !_owner) {
							_owner = null;
							return "";
						}
						return _owner.name;
					}
					catch {
						//To fix sometime error at editor startup.
						_owner = null;
						return "";
					}
				}
			}

			/// <summary>
			/// This is the persistence graph
			/// </summary>
			/// <value></value>
			public IGraph graph {
				get {
					if(_owner == null || !_owner) {
						foreach(var d in graphDatas) {
							if(d.owner != null) {
								_owner = d.owner;
							}
						}
					}
					return _owner as IGraph;
				}
				set {
					_owner = value as UnityEngine.Object;
				}
			}

			public UnityEngine.Object owner {
				get => _owner;
				set => _owner = value;
			}

			public TabData() { }

			public TabData(UnityEngine.Object owner) {
				if (owner is IScriptGraphType || owner is not IGraph && owner is not IScriptGraph)
					throw new InvalidOperationException();
				this.owner = owner;
				if(owner is IScriptGraph) {
					var scriptGraph = owner as IScriptGraph;
					if(scriptGraph != null && scriptGraph.TypeList != null) {
						foreach(var type in scriptGraph.TypeList.references) {
							if(type is IGraph) {
								graphDatas.Add(new GraphEditorData(type));
								break;
							}
						}
					}
				} else if(owner is IGraph) {
					graphDatas.Add(new GraphEditorData(owner));
				}
			}
		}

		[System.Serializable]
		public class uNodeEditorData {
			public List<EditorScriptInfo> scriptInformations = new List<EditorScriptInfo>();

			/// <summary>
			/// Are the left panel is visible?
			/// </summary>
			public bool leftVisibility = true;
			/// <summary>
			/// Are the right panel is visible?
			/// </summary>
			public bool rightVisibility = false;
			/// <summary>
			/// The heigh of variable editor.
			/// </summary>
			public float variableEditorHeight = 150f;

			#region Panel
			[SerializeField]
			private float _rightPanelWidth = 300;
			[SerializeField]
			private float _leftPanelWidth = 250;
			public List<string> lastOpenedFile;

			/// <summary>
			/// The width of right panel.
			/// </summary>
			public float rightPanelWidth {
				get {
					if(!rightVisibility)
						return 0;
					return _rightPanelWidth;
				}
				set {
					_rightPanelWidth = value;
				}
			}

			/// <summary>
			/// The width of left panel.
			/// </summary>
			public float leftPanelWidth {
				get {
					if(!leftVisibility)
						return 0;
					return _leftPanelWidth;
				}
				set {
					_leftPanelWidth = value;
				}
			}
			#endregion

			#region Recent
			[Serializable]
			public class RecentItem {
				[SerializeField]
				private MemberData memberData;

				private MemberInfo _info;
				public MemberInfo info {
					get {
						if(_info == null && memberData != null) {
							switch(memberData.targetType) {
								case MemberData.TargetType.Type:
								case MemberData.TargetType.uNodeType:
									_info = memberData.startType;
									break;
								case MemberData.TargetType.Field:
								case MemberData.TargetType.Constructor:
								case MemberData.TargetType.Event:
								case MemberData.TargetType.Method:
								case MemberData.TargetType.Property:
									var members = memberData.GetMembers(false);
									if(members != null) {
										_info = members[members.Length - 1];
									}
									break;
							}
						}
						return _info;
					}
					set {
						_info = value;
						memberData = MemberData.CreateFromMember(_info);
					}
				}
				public bool isStatic {
					get {
						if(info == null)
							return false;
						return ReflectionUtils.GetMemberIsStatic(info);
					}
				}
			}

			/// <summary>
			/// The recent items data.
			/// </summary>
			public List<RecentItem> recentItems = new List<RecentItem>();

			public void AddRecentItem(RecentItem recentItem) {
				while(recentItems.Count >= 50) {
					recentItems.RemoveAt(recentItems.Count - 1);
				}
				recentItems.RemoveAll(item => item.info == recentItem.info);
				recentItems.Insert(0, recentItem);
				SaveOptions();
			}
			#endregion

			#region Favorites
			[SerializeField]
			Dictionary<string, Dictionary<string, object>> customFavoriteDatas;

			public List<RecentItem> favoriteItems;

			public void AddFavorite(MemberInfo member) {
				if(favoriteItems == null)
					favoriteItems = new List<RecentItem>();
				if(!HasFavorite(member)) {
					favoriteItems.Add(new RecentItem() {
						info = member
					});
					SaveOptions();
				}
			}

			public void AddFavorite(string kind, string guid, object data = null) {
				if(customFavoriteDatas == null)
					customFavoriteDatas = new Dictionary<string, Dictionary<string, object>>();
				if(!customFavoriteDatas.TryGetValue(kind, out var map)) {
					map = new Dictionary<string, object>();
					customFavoriteDatas[kind] = map;
				}
				map[guid] = data;
			}

			public void RemoveFavorite(string kind, string guid) {
				if(customFavoriteDatas == null)
					return;
				if(customFavoriteDatas.TryGetValue(kind, out var map)) {
					map.Remove(guid);
				}
			}

			public bool HasFavorite(string kind, string guid) {
				if(customFavoriteDatas == null)
					return false;
				if(customFavoriteDatas.TryGetValue(kind, out var map)) {
					return map.ContainsKey(guid);
				}
				return false;
			}

			public void RemoveFavorite(MemberInfo member) {
				if(favoriteItems == null)
					return;
				if(HasFavorite(member)) {
					favoriteItems.Remove(favoriteItems.First(item => item != null && item.info == member));
					SaveOptions();
				}
			}

			public bool HasFavorite(MemberInfo member) {
				if(favoriteItems == null)
					return false;
				return favoriteItems.Any(item => item != null && item.info == member);
			}

			[SerializeField]
			HashSet<string> _favoriteNamespaces;
			public HashSet<string> favoriteNamespaces {
				get {
					if(_favoriteNamespaces == null) {
						_favoriteNamespaces = new HashSet<string>() {
							"System",
							"System.Collections",
							"UnityEngine.AI",
							"UnityEngine.Events",
							"UnityEngine.EventSystems",
							"UnityEngine.SceneManagement",
							"UnityEngine.UI",
							"UnityEngine.UIElements",
						};
					}
					return _favoriteNamespaces;
				}
			}


			#endregion

			#region Graph Infos
			public void RegisterGraphInfos(IEnumerable<ScriptInformation> informations, UnityEngine.Object owner, string scriptPath) {
				if(informations != null) {
					EditorScriptInfo scriptInfo = new EditorScriptInfo() {
						guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(owner)),
						path = scriptPath,
					};
					scriptInfo.informations = informations.ToArray();
					var prevInfo = scriptInformations.FirstOrDefault(g => g.guid == scriptInfo.guid);
					if(prevInfo != null) {
						scriptInformations.Remove(prevInfo);
					}
					scriptInformations.Add(scriptInfo);
					uNodeThreadUtility.ExecuteOnce(uNodeEditor.SaveOptions, "unode_save_informations");
				}
			}

			public bool UnregisterGraphInfo(UnityEngine.Object owner) {
				var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(owner));
				var prevInfo = scriptInformations.FirstOrDefault(g => g.guid == guid);
				if(prevInfo != null) {
					return scriptInformations.Remove(prevInfo);
				}
				uNodeThreadUtility.ExecuteOnce(uNodeEditor.SaveOptions, "unode_save_informations");
				return false;
			}
			#endregion
		}

		[Serializable]
		public class EditorScriptInfo {
			public string guid;
			public string path;
			public ScriptInformation[] informations;
		}

		class CachedData {
			public GameObject oldTarget = null;
			internal int errorRefreshTime;
		}
		private CachedData cached = new CachedData();

		public static class EditorDataSerializer {
			[Serializable]
			class Data {
				public byte[] data;
				public DataReference[] references;
				public string type;

				public OdinSerializedData Load() {
					var data = new OdinSerializedData();
					data.data = this.data;
					data.serializedType = type;
					data.references = new List<UnityEngine.Object>();
					for(int i = 0; i < references.Length; i++) {
						data.references.Add(references[i].GetObject());
					}
					return data;
				}

				public static Data Create(OdinSerializedData serializedData) {
					var data = new Data();
					data.data = serializedData.data;
					data.type = serializedData.serializedType;
					data.references = new DataReference[serializedData.references.Count];
					for(int i = 0; i < data.references.Length; i++) {
						data.references[i] = DataReference.Create(serializedData.references[i]);
					}
					return data;
				}
			}

			[Serializable]
			class DataReference {
				public string path;
				public int uid;

				public UnityEngine.Object GetObject() {
					var obj = AssetDatabase.LoadAssetAtPath(path, typeof(UnityEngine.Object));
					if(obj == null)
						return null;
					if(uNodeUtility.GetObjectID(obj) == uid) {
						return obj;
					} else {
						var objs = AssetDatabase.LoadAllAssetsAtPath(path);
						if(objs != null) {
							for(int i = 0; i < objs.Length; i++) {
								if(uNodeUtility.GetObjectID(objs[i]) == uid) {
									return objs[i];
								}
							}
						}
						//if(obj is GameObject gameObject) {
						//	var comps = gameObject.GetComponentsInChildren<Component>(true);
						//	for(int i=0;i<comps.Length;i++) {
						//		if(uNodeUtility.GetObjectID(comps[i]) == uid) {
						//			return comps[i];
						//		}
						//	}
						//}
					}
					return null;
				}

				public static DataReference Create(UnityEngine.Object obj) {
					if(obj == null)
						return null;
					var path = AssetDatabase.GetAssetPath(obj);
					if(!string.IsNullOrEmpty(path)) {
						DataReference data = new DataReference();
						data.path = path;
						data.uid = uNodeUtility.GetObjectID(obj);
						return data;
					}
					return null;
				}
			}

			public static void Save<T>(T value, string fileName) {
				Directory.CreateDirectory(uNodePreference.preferenceDirectory);
				char separator = Path.DirectorySeparatorChar;
				string path = uNodePreference.preferenceDirectory + separator + fileName + ".json";
				File.WriteAllText(path, JsonUtility.ToJson(Data.Create(SerializerUtility.SerializeValue(value))));
			}

			public static T Load<T>(string fieldName) {
				char separator = Path.DirectorySeparatorChar;
				string path = uNodePreference.preferenceDirectory + separator + fieldName + ".json";
				try {
					if(File.Exists(path)) {
						var data = JsonUtility.FromJson<Data>(File.ReadAllText(path));
						if(data != null) {
							return SerializerUtility.Deserialize<T>(data.Load());
						}
					}
				}
				catch(Exception ex) {
					Debug.LogException(ex);
				}
				return default;
			}
		}
		#endregion

		#region Variables
		/// <summary>
		/// The uNode editor instance
		/// </summary>
		public static uNodeEditor window;
		#endregion

		#region Properties
		/// <summary>
		/// The graph editor.
		/// </summary>
		public NodeGraph graphEditor {
			get {
				return uNodePreference.nodeGraph;
			}
		}

		static uNodePreference.PreferenceData _preferenceData;
		/// <summary>
		/// The preference data.
		/// </summary>
		public static uNodePreference.PreferenceData preferenceData {
			get {
				if(_preferenceData != uNodePreference.GetPreference()) {
					_preferenceData = uNodePreference.GetPreference();
				}
				return _preferenceData;
			}
		}

		/// <summary>
		/// Are the main selection is locked?
		/// </summary>
		public static bool isLocked {
			get {
				return uNodePreference.preferenceData.isLocked;
			}
			set {
				if(uNodePreference.preferenceData.isLocked != value) {
					uNodePreference.preferenceData.isLocked = value;
					uNodePreference.SavePreference();
				}
			}
		}

		/// <summary>
		/// Are the node is dimmed?
		/// </summary>
		public static bool isDim {
			get {
				return uNodePreference.preferenceData.isDim;
			}
			set {
				if(uNodePreference.preferenceData.isDim != value) {
					uNodePreference.preferenceData.isDim = value;
					uNodePreference.SavePreference();
				}
			}
		}
		#endregion

		#region Events
		/// <summary>
		/// An event to be called on GUIChanged.
		/// </summary>
		public static event Action onChanged;
		/// <summary>
		/// An event to be called on Selection is changed.
		/// </summary>
		public static event Action<GraphEditorData> onSelectionChanged;
		/// <summary>
		/// Called for clear any cached values.
		/// </summary>
		public static event Action onClearCache;

		/// <summary>
		/// Do clear the cached values
		/// </summary>
		public static void ClearCache() {
			try {
				onClearCache?.Invoke();
			}
			catch(Exception ex) {
				Debug.LogException(ex);
			}
			uNodeEditor.RefreshEditor(true);
		}
		#endregion

		#region Save & Load Setting
		private static uNodeEditorData _savedData;
		public static uNodeEditorData SavedData {
			get {
				if(_savedData == null) {
					LoadOptions();
					if(_savedData == null) {
						_savedData = new uNodeEditorData();
					}
				}
				return _savedData;
			}
			set {
				_savedData = value;
			}
		}

		/// <summary>
		/// Save the current graph
		/// Note: this function will not work on playmode use SaveCurrentGraph if need to save either on editor or in playmode.
		/// </summary>
		public static void AutoSaveCurrentGraph() {
			if(Application.isPlaying)
				return;
			SaveCurrentGraph();
		}

		/// <summary>
		/// Save the current graph
		/// </summary>
		public static void SaveCurrentGraph() {
			if(window == null)
				return;
			if(window.selectedTab != null && window.selectedTab.owner != null) {
				GraphUtility.SaveGraph(window.selectedTab.owner);
			}
			//else if(Application.isPlaying && window.graphData.graph is uNodeRuntime runtime && runtime.originalGraph != null) {
			//	GraphUtility.SaveRuntimeGraph(runtime);
			//}
		}

		public static void SaveOptions() {
			EditorDataSerializer.Save(_savedData, "EditorData");
		}

		public static void LoadOptions() {
			_savedData = EditorDataSerializer.Load<uNodeEditorData>("EditorData");
			if(_savedData == null) {
				_savedData = new uNodeEditorData();
				SaveOptions();
			}
		}
		#endregion

		#region EditorData
		/// <summary>
		/// The graph editor data
		/// </summary>
		public GraphEditorData graphData {
			get {
				return selectedTab.selectedGraphData;
			}
		}

		public TabData mainTab { get => tabList.main; set => tabList.main = value; }
		public List<TabData> tabDatas => tabList.datas;

		public TabData selectedTab {
			get {
				if(tabList.selectedIndex > 0 && tabDatas.Count >= tabList.selectedIndex) {
					return tabDatas[tabList.selectedIndex - 1];
				} else {
					tabList.selectedIndex = 0;
				}
				return mainTab;
			}
			set {
				if(value != null && value != mainTab && tabDatas.Contains(value)) {
					tabList.selectedIndex = tabDatas.IndexOf(value) + 1;
				} else {
					tabList.selectedIndex = 0;
				}
			}
		}
		#endregion

		#region GUI
		private void OnEnable() {
			window = this;
			uNodeGUIUtility.onGUIChanged -= GUIChanged;
			uNodeGUIUtility.onGUIChanged += GUIChanged;
			EditorApplication.playModeStateChanged += OnPlaymodeStateChanged;
			LoadEditorData();
			if (!hasLoad) {
				LoadOptions();
				hasLoad = true;
			}
			graphEditor.window = this;
			graphEditor.OnEnable();
			Refresh();
		}

		bool hasLoad = false;
		void OnDisable() {
			uNodeGUIUtility.onGUIChanged -= GUIChanged;
			EditorApplication.playModeStateChanged -= OnPlaymodeStateChanged;
			SaveEditorData();
			if(!hasLoad) {
				LoadOptions();
				hasLoad = true;
			}
			SaveOptions();
			graphEditor.OnDisable();
		}

		void OnGUI() {
			if(graphEditor != null) {
				if(graphData != null && selectedTab != mainTab) {
					graphEditor.DrawCanvas(this);
				}
				else {
					graphEditor.OnNoTarget();
					graphEditor.DrawMainTab(this);
				}
			}
		}

		private void Update() {
			if(!EditorApplication.isPaused) {
				//GraphDebug.debugLinesTimer = Mathf.Repeat(GraphDebug.debugLinesTimer += 0.03f, 1f);
			}
			if(Selection.activeGameObject != null && (cached.oldTarget != Selection.activeGameObject)) {
				OnSelectionChange();
				cached.oldTarget = Selection.activeGameObject;
			}
			if(preferenceData.enableErrorCheck) {
				int nowSecond = System.DateTime.Now.Second;
				//if(nowSecond % 2 != 0 && nowSecond != errorRefreshTime)
				if(nowSecond != cached.errorRefreshTime) {
					CheckErrors();
					cached.errorRefreshTime = nowSecond;
					Repaint();
				}
			}
			if(selectedTab != null && selectedTab.graph != null) {
				if(selectedTab.owner != null) {
					//
				}
			}
			//InitGraph();
		}

		void OnSelectionChange() {
			if(selectedTab != mainTab) {
				if(Selection.activeObject != null && graphData.graph != null) {
					object selected = Selection.activeObject;
					if(graphData.debugAnyScript && Application.isPlaying) {
						var graph = graphData.graph;
						var type = graph.GetFullGraphName().ToType(false);
						if(type != null && type.IsSubclassOf(typeof(Component))) {
							if(selected is GameObject gameObject) {
								object obj;
								if(type is RuntimeType) {
									obj = gameObject.GetGeneratedComponent(type);
								}
								else {
									obj = gameObject.GetComponent(type);
								}
								if(obj != null) {
									selected = obj;
								}
							}
							else if(selected is IRuntimeClassContainer container){
								if(container.IsInitialized && container.RuntimeClass?.GetType() == type) {
									selected = container.RuntimeClass;
								}
							}
							if(selected.GetType() == type) {
								graphData.SetAutoDebugTarget(selected);
							}
						}
						else {
							if(selected is GameObject gameObject && graph is IReflectionType reflectionType && reflectionType.ReflectionType != null) {
								type = reflectionType.ReflectionType;
								var comps = gameObject.GetComponents<IRuntimeClassContainer>();
								foreach(var container in comps) {
									if(container.IsInitialized && container.RuntimeClass != null && type.IsInstanceOfType(container.RuntimeClass)) {
										graphData.SetAutoDebugTarget(container.RuntimeClass);
									}
								}
							}
							else if(selected is IRuntimeClassContainer container) {
								if(container.IsInitialized && container.RuntimeClass?.GetType() == type) {
									selected = container.RuntimeClass;
								}
							}
							else if(selected is IInstancedGraph instanced) {
								if(instanced.OriginalGraph == graph) {
									graphData.SetAutoDebugTarget(instanced);
								}
							}
						}
					}
				}
			}
			if(selectedTab == mainTab) {
				GUIChanged();
			}
		}

		void OnPlaymodeStateChanged(PlayModeStateChange state) {
			switch(state) {
				case PlayModeStateChange.EnteredPlayMode:
					LoadEditorData();
					//GraphDebug.useDebug = _useDebug;
					Refresh();
					break;
				case PlayModeStateChange.EnteredEditMode:
					CustomInspector.ResetEditor();
					LoadEditorData();
					//GraphDebug.useDebug = _useDebug;
					break;
				case PlayModeStateChange.ExitingEditMode:
				case PlayModeStateChange.ExitingPlayMode:
					//_isCompiling = false;
					//_useDebug = GraphDebug.useDebug;
					SaveEditorData();
					break;
			}
		}


		public void SaveEditorData() {
			uNodeEditorUtility.SaveEditorDataOnDatabase(tabList, "GraphEditorData");
		}

		void LoadEditorData() {
			tabList = uNodeEditorUtility.LoadEditorDataOnDatabase<EditorTabDatas>("GraphEditorData") ?? new EditorTabDatas();
		}

		/// <summary>
		/// Show the uNodeEditor.
		/// </summary>
		[MenuItem("Tools/uNode/uNode Editor", false, 0)]
		public static void ShowWindow() {
			window = (uNodeEditor)GetWindow(typeof(uNodeEditor), false);
			window.minSize = new Vector2(300, 250);
			window.autoRepaintOnSceneChange = true;
			window.wantsMouseMove = true;
			window.titleContent = new GUIContent("uNode Editor"/*, Resources.Load<Texture2D>("uNODE_Logo")*/);
			window.Show();
		}

		public static void ForceRepaint() {
			if(window != null) {
				window.Repaint();
				EditorApplication.RepaintHierarchyWindow();
				GUIChanged();
			}
		}

		public static void GUIChanged(object obj, UIChangeType changeType) {
			if(window != null && window.graphEditor != null) {
				window.graphEditor.GUIChanged(obj, changeType);
			}
			GUIChanged();
		}

		public static void GUIChanged() {
			if(window != null) {
				try {
					EditorUtility.SetDirty(window);
					if(window.selectedTab != null && window.selectedTab.owner != null) {
						EditorUtility.SetDirty(window.selectedTab.owner);
					}
				}
				catch {
					if(window.selectedTab != null) {
						window.selectedTab.owner = null;
					}
					throw;
				}
			}
			if(onChanged != null) {
				onChanged();
			}
		}

		private void OnMainTargetChange() {
			if(graphData == mainTab.selectedGraphData) {
				if(graphData.graph != null) {
					UpdatePosition();
					// editorData.selectedNodes.Clear();
				}
			}
		}

		public void ChangeEditorTarget(TabData data) {
			bool needRefresh = data == null || selectedTab != data || selectedTab.selectedGraphData.currentCanvas != data.selectedGraphData.currentCanvas;
			selectedTab = data;
			OnMainTargetChange();
			if(needRefresh) {
				Refresh();
			}
			UpdatePosition();
		}

		public void ChangeEditorSelection(UGraphElement value, bool addSelection = true) {
			if(value == null) {
				graphData.ClearSelection();
			} else {
				if(!addSelection) {
					graphData.ClearSelection();
				}
				graphData.AddToSelection(value);
			}
			EditorSelectionChanged();
		}

		public void ChangeEditorSelection(BaseReference reference) {
			if(reference == null) {
				graphData.ClearSelection();
			}
			else {
				graphData.AddToSelection(reference);
			}
			EditorSelectionChanged();
		}

		CustomInspector inspectorWrapper;
		internal void EditorSelectionChanged() {
			if(onSelectionChanged != null) {
				onSelectionChanged(graphData);
			}
			if(!uNodePreference.GetPreference().inspectorIntegration)
				return;
			if(!graphData.hasSelection && Selection.activeObject == inspectorWrapper) {
				if(inspectorWrapper != null) {
					inspectorWrapper.unserializedEditorData = graphData;
					CustomInspector.GetEditor(inspectorWrapper).Repaint();
				}
				return;
			}
			if(graphData.selectedCount == 1) {
				foreach(var selection in graphData.selecteds) {
					if(selection != null) {
						if(selection is NodeObject nodeObject && nodeObject.node != null) {
							Undo.SetCurrentGroupName($"Select node: {nodeObject.GetTitle()} ({nodeObject.node.GetType()})");
						}
						else if(selection is UGraphElement element) {
							Undo.SetCurrentGroupName($"Select: {element.name} ({selection.GetType()})");
						}
						else {
							Undo.SetCurrentGroupName($"Select: {selection} ({selection.GetType()})");
						}
						break;
					}
				}
			}
			else {
				if(graphData.selectedCount == 0) {
					Undo.SetCurrentGroupName($"Select graph");
				}
				else {
					Undo.SetCurrentGroupName($"Select nodes");
				}
			}
			if(SavedData.rightVisibility == false) {
				inspectorWrapper = ScriptableObject.CreateInstance<CustomInspector>();
				inspectorWrapper.editorData = new GraphEditorData(graphData);
				Selection.instanceIDs = new int[] { inspectorWrapper.GetInstanceID() };
			}
		}
		#endregion

		#region Functions
		/// <summary>
		/// Refresh uNode Editor
		/// </summary>
		public void Refresh() {
			Refresh(false);
		}

		/// <summary>
		/// Refresh uNode Editor
		/// </summary>
		public void Refresh(bool fullRefresh) {
			graphEditor.window = this;
			graphData.Refresh();
			graphEditor.ReloadView(fullRefresh);
			GUIChanged();
		}

		public static void RefreshEditor(bool fullRefresh) {
			if(window != null) {
				window.Refresh(fullRefresh);
			}
		}

		internal void UndoRedoPerformed() {
			graphEditor.UndoRedoPerformed();
			uNodeThreadUtility.ExecuteAfter(1, () => {
				Refresh(true);
			});
			EditorReflectionUtility.UpdateRuntimeTypes();
		}

		public void UpdatePosition() {
			graphEditor.MoveCanvas(graphData.position);
		}

		public static List<UnityEngine.Object> FindLastOpenedGraphs() {
			List<UnityEngine.Object> lastOpenedObjects = new List<UnityEngine.Object>();
			if(SavedData.lastOpenedFile == null) {
				return lastOpenedObjects;
			}
			for(int i = 0; i < SavedData.lastOpenedFile.Count; i++) {
				string path = SavedData.lastOpenedFile[i];
				if(!File.Exists(path))
					continue;
				if(path.EndsWith(".asset")) {
					GraphAsset asset = AssetDatabase.LoadAssetAtPath<GraphAsset>(path);
					if(asset != null) {
						lastOpenedObjects.Add(asset);
					} else {
						//var data = go.GetComponent<uNodeData>();
						//if(data != null) {
						//	lastOpenedObjects.Add(data);
						//}
					}
				}
				if(lastOpenedObjects.Count >= 10) {
					break;
				}
			}
			return lastOpenedObjects;
		}

		public static void ClearLastOpenedGraphs() {
			SavedData.lastOpenedFile = null;
			SaveOptions();
		}


		public void CheckErrors() {
			if(graphData == null)
				return;
#if UseProfiler
			Profiler.BeginSample("Check Errors");
#endif
			var analizer = GraphUtility.ErrorChecker.defaultAnalizer;
			
			analizer.ClearErrors(graphData.graph);
			analizer.CheckErrors(graphData.graph);

			graphEditor.OnErrorUpdated();
			ErrorCheckWindow.UpdateErrorMessages();
#if UseProfiler
			Profiler.EndSample();
#endif
		}

		internal int ErrorsCount() {
			var analizer = GraphUtility.ErrorChecker.defaultAnalizer;
			if(analizer.graphErrors != null) {
				int count = 0;
				foreach(var pair in analizer.graphErrors) {
					if(pair.Key != null && pair.Value != null) {
						var obj = pair.Key;
						if(obj == graphData.graph) {
							foreach(var pair2 in pair.Value) {
								count += pair2.Value.GetErrorCount(InfoType.Error);
							}
						}
						//if(ErrorCheckWindow.onlySelectedUNode) {
						//} else {
						//	count += pair.Value.Count;
						//}
					}
				}
				return count;
			}
			return 0;
		}

		/// <summary>
		/// Highlight the node for a second.
		/// </summary>
		/// <param name="node"></param>
		public static void HighlightNode(NodeObject node) {
			ShowWindow();
			UGraphElement canvas = node;
			if(node.node is ISuperNode)
				canvas = node.parent;
			Open(node.graphContainer, canvas);
			window.Refresh();
			//window.graphEditor.MoveCanvas(window.editorData.GetPosition(node));
			window.graphEditor.Highlight(node);
		}

		/// <summary>
		/// Highlight the node from a Script Information data with the given line number and column number
		/// </summary>
		/// <param name="scriptInfo"></param>
		/// <param name="line"></param>
		/// <param name="column"></param>
		/// <returns></returns>
		public static bool HighlightNode(EditorScriptInfo scriptInfo, int line, int column = -1) {
			if (scriptInfo == null)
				return false;
			if (scriptInfo.informations == null)
				return false;
			var path = AssetDatabase.GUIDToAssetPath(scriptInfo.guid);
			if (string.IsNullOrEmpty(path))
				return false;
			var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
			if (asset is IGraph) {
				return HighlightNode(asset as IGraph, scriptInfo.informations, line, column);
			}
			else if(asset is IScriptGraph scriptGraph) {
				foreach(var type in scriptGraph.TypeList) {
					if(type is IGraph) {
						if(CanHighlightNode(type as IGraph, scriptInfo.informations, line, column)) {
							return HighlightNode(type as IGraph, scriptInfo.informations, line, column);
						}
					}
				}
			}
			return false;
		}

		/// <summary>
		/// Highlight the node from a Script Information data with the given line number and column number
		/// </summary>
		/// <param name="informations"></param>
		/// <param name="line"></param>
		/// <param name="column"></param>
		/// <returns></returns>
		public static bool HighlightNode(IEnumerable<ScriptInformation> informations, int line, int column = -1) {
			return HighlightNode(null, informations, line, column);
		}

		/// <summary>
		/// Highlight the node from a Script Information data with the given line number and column number
		/// </summary>
		/// <param name="graph"></param>
		/// <param name="informations"></param>
		/// <param name="line"></param>
		/// <param name="column"></param>
		/// <returns></returns>
		public static bool HighlightNode(IGraph graph, IEnumerable<ScriptInformation> informations, int line, int column = -1) {
			if (informations == null)
				return false;
			List<ScriptInformation> information = new List<ScriptInformation>();
			foreach (var info in informations) {
				if (info == null)
					continue;
				if (info.startLine <= line && info.endLine >= line) {
					information.Add(info);
				}
			}
			if (column > 0) {
				information.Sort((x, y) => {
					int result = CompareUtility.Compare(x.lineRange, y.lineRange);
					if (result == 0) {
						int xColumn = int.MaxValue;
						if (x.startColumn <= column && x.endColumn >= column) {
							xColumn = x.columnRange;
						}
						int yColumn = int.MaxValue;
						if (y.startColumn <= column && y.endColumn >= column) {
							yColumn = y.columnRange;
						}
						return CompareUtility.Compare(xColumn, yColumn);
					}
					return result;
				});
			}
			else {
				information.Sort((x, y) => {
					int result = CompareUtility.Compare(x.lineRange, y.lineRange);
					if (result == 0) {
						return CompareUtility.Compare(y.columnRange, x.columnRange);
					}
					return result;
				});
			}
			foreach (var info in information) {
				if (info != null) {
					// Debug.Log(line + ":" + column);
					// Debug.Log(info.startLine + "-" + info.endLine);
					// Debug.Log(info.startColumn + "-" + info.endColumn);
					// Debug.Log(info.lineRange + ":" + info.columnRange);
					if (int.TryParse(info.id, out var id)) {
						UGraphElement element = null;
						if (graph != null) {
							element = graph.GetGraphElement(id);
							if (element == null && int.TryParse(info.ghostID, out var gID)) {
								element = graph.GetGraphElement(gID);
							}
						} else if(info.ownerID != 0) {
							var obj = EditorUtility.InstanceIDToObject(info.ownerID);
							if(obj is IGraph g) {
								element = g.GetGraphElement(id);
								if (element == null && int.TryParse(info.ghostID, out var gID)) {
									element = g.GetGraphElement(gID);
								}
							}
						}
						if (element != null) {
							if(element is NodeObject) {
								HighlightNode(element as NodeObject);
							}
							else if (element is NodeContainerWithEntry nodeContainer && nodeContainer.Entry != null) {
								HighlightNode(nodeContainer.Entry);
							}
							return true;
						}
					}
				}
			}
			return false;
		}

		public static bool CanHighlightNode(EditorScriptInfo scriptInfo, int line, int column = -1) {
			if(scriptInfo == null)
				return false;
			if(scriptInfo.informations == null)
				return false;
			var path = AssetDatabase.GUIDToAssetPath(scriptInfo.guid);
			if(string.IsNullOrEmpty(path))
				return false;
			var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
			if(asset is IGraph graph) {
				return CanHighlightNode(graph, scriptInfo.informations, line, column);
			} else if(asset is IScriptGraph scriptGraph) {
				foreach(var type in scriptGraph.TypeList) {
					if(type is IGraph) {
						if(CanHighlightNode(type as IGraph, scriptInfo.informations, line, column)) {
							return true;
						}
					}
				}
			} 
			return false;
		}

		public static bool CanHighlightNode(IGraph graph, IEnumerable<ScriptInformation> informations, int line, int column = -1) {
			if(informations == null)
				return false;
			List<ScriptInformation> information = new List<ScriptInformation>();
			foreach(var info in informations) {
				if(info == null)
					continue;
				if(info.startLine <= line && info.endLine >= line) {
					information.Add(info);
				}
			}
			if(column > 0) {
				information.Sort((x, y) => {
					int result = CompareUtility.Compare(x.lineRange, y.lineRange);
					if(result == 0) {
						int xColumn = int.MaxValue;
						if(x.startColumn <= column && x.endColumn >= column) {
							xColumn = x.columnRange;
						}
						int yColumn = int.MaxValue;
						if(y.startColumn <= column && y.endColumn >= column) {
							yColumn = y.columnRange;
						}
						return CompareUtility.Compare(xColumn, yColumn);
					}
					return result;
				});
			}
			else {
				information.Sort((x, y) => {
					int result = CompareUtility.Compare(x.lineRange, y.lineRange);
					if(result == 0) {
						return CompareUtility.Compare(y.columnRange, x.columnRange);
					}
					return result;
				});
			}
			foreach(var info in information) {
				if(info != null) {
					if(int.TryParse(info.id, out var id)) {
						UGraphElement element = null;
						if(graph != null) {
							element = graph.GetGraphElement(id);
							if(element == null && int.TryParse(info.ghostID, out var gID)) {
								element = graph.GetGraphElement(gID);
							}
						}
						else if(info.ownerID != 0) {
							var obj = EditorUtility.InstanceIDToObject(info.ownerID);
							if(obj is IGraph g) {
								element = g.GetGraphElement(id);
								if(element == null && int.TryParse(info.ghostID, out var gID)) {
									element = g.GetGraphElement(gID);
								}
							}
						}
						if(element != null) {
							return true;
						}
					}
				}
			}
			return false;
		}

		public static void Open(IScriptGraph scriptGraph) {
			if (scriptGraph == null)
				throw new ArgumentNullException(nameof(scriptGraph));
			if (window == null) {
				ShowWindow();
			}
			var cachedGraph = FindCachedGraph(scriptGraph as UnityEngine.Object);
			if (cachedGraph != null) {
				for (int i = 0; i < cachedGraph.graphDatas.Count; i++) {
					var d = cachedGraph.graphDatas[i];
					if (d.owner == null) {
						cachedGraph.graphDatas.RemoveAt(i);
						i--;
						continue;
					}
					window.selectedTab = cachedGraph;
					window.selectedTab.selectedGraphData = d;
					window.ChangeEditorSelection(null);
					window.Refresh();
					window.UpdatePosition();
					return;
				}
				var ED = new GraphEditorData();
				cachedGraph.graphDatas.Add(ED);
				cachedGraph.selectedGraphData = ED;
				window.selectedTab = cachedGraph;
				window.ChangeEditorSelection(null);
				window.Refresh();
				window.UpdatePosition();
				return;
			}
			var tabData = new TabData(scriptGraph as UnityEngine.Object);
			window.tabDatas.Add(tabData);
			window.selectedTab = tabData;
			window.ChangeEditorSelection(null);
			window.Refresh();
			window.UpdatePosition();
		}

		public static void Open(IScriptGraphType scriptGraphType) {
			if(scriptGraphType == null)
				throw new ArgumentNullException(nameof(scriptGraphType));
			if(window == null) {
				ShowWindow();
			}
			var owner = scriptGraphType.ScriptTypeData.scriptGraphReference;
			var cachedGraph = FindCachedGraph(owner);
			if(cachedGraph != null) {
				for(int i = 0; i < cachedGraph.graphDatas.Count; i++) {
					var d = cachedGraph.graphDatas[i];
					if(object.ReferenceEquals(d.owner, scriptGraphType)) {
						if(d.owner == null) {
							cachedGraph.graphDatas.RemoveAt(i);
							i--;
							continue;
						}
						window.selectedTab = cachedGraph;
						window.selectedTab.selectedGraphData = d;
						//if(canvas != null) {
						//	window.selectedTab.selectedGraphData.currentCanvas = canvas;
						//}
						window.ChangeEditorSelection(null);
						window.Refresh();
						window.UpdatePosition();
						return;
					}
				}
				var ED = new GraphEditorData(scriptGraphType as UnityEngine.Object);
				cachedGraph.graphDatas.Add(ED);
				cachedGraph.selectedGraphData = ED;
				window.selectedTab = cachedGraph;
				//if(canvas != null) {
				//	window.selectedTab.selectedGraphData.currentCanvas = canvas;
				//}
				window.ChangeEditorSelection(null);
				window.Refresh();
				window.UpdatePosition();
				return;
			}
			var tabData = new TabData(owner);
			window.tabDatas.Add(tabData);
			window.selectedTab = tabData;
			//if(canvas != null) {
			//	window.selectedTab.selectedGraphData.currentCanvas = canvas;
			//}
			window.ChangeEditorSelection(null);
			window.Refresh();
			window.UpdatePosition();
		}

		public static void Open(IGraph graph, UGraphElement canvas = null) {
			if(graph == null)
				throw new ArgumentNullException(nameof(graph));
			if(window == null) {
				ShowWindow();
			}
			if(canvas != null) {
				if(canvas.graphContainer != null && canvas.graphContainer != graph)
					throw new Exception("Invalid canvas");
				if(canvas is NodeContainer) {

				} else if(canvas is NodeObject) {
					UGraphElement current = canvas;
					while(current != null) {
						if(current is NodeContainer) {
							canvas = current;
							break;
						} else if(current is NodeObject node && node.node is ISuperNode) {
							canvas = current;
							break;
						}
						current = current.parent;
					}
					if(current == null)
						canvas = null;
				} else {
					throw new Exception("Invalid canvas");
				}
			}
			//if(graph == null) {
			//	ChangeMainTarget(target);
			//	window.selectedGraph = window.mainGraph;
			//	//window.selectedGraph.graph = graph;
			//	window.ChangeEditorSelection(window.editorData.graph);
			//	return;
			//}
			var owner = graph as UnityEngine.Object;
			if(graph is IScriptGraphType) {
				owner = (graph as IScriptGraphType).ScriptTypeData.scriptGraph as UnityEngine.Object;
			}
			var cachedGraph = FindCachedGraph(owner);
			if(cachedGraph != null) {
				for (int i = 0; i < cachedGraph.graphDatas.Count; i++) {
					var d = cachedGraph.graphDatas[i];
					if (d.graph == graph) {
						if (d.owner == null) {
							cachedGraph.graphDatas.RemoveAt(i);
							i--;
							continue;
						}
						window.selectedTab = cachedGraph;
						window.selectedTab.selectedGraphData = d;
						if (canvas != null) {
							window.selectedTab.selectedGraphData.currentCanvas = canvas;
						}
						window.ChangeEditorSelection(null);
						window.Refresh();
						window.UpdatePosition();
						return;
					}
				}
				var ED = new GraphEditorData(graph as UnityEngine.Object);
				cachedGraph.graphDatas.Add(ED);
				cachedGraph.selectedGraphData = ED;
				window.selectedTab = cachedGraph;
				if (canvas != null) {
					window.selectedTab.selectedGraphData.currentCanvas = canvas;
				}
				window.ChangeEditorSelection(null);
				window.Refresh();
				window.UpdatePosition();
				return;
			}
			var tabData = new TabData(owner);
			window.tabDatas.Add(tabData);
			window.selectedTab = tabData;
			if(canvas != null) {
				window.selectedTab.selectedGraphData.currentCanvas = canvas;
			}
			window.ChangeEditorSelection(null);
			window.Refresh();
			window.UpdatePosition();
		}

		static TabData FindCachedGraph(UnityEngine.Object owner) {
			foreach (var data in window.tabDatas) {
				if (data == null)
					continue;
				if (data.owner == owner) {
					return data;
				}
			}
			return null;
		}
		#endregion

		#region Utilities
		public void GenerateSource() {
			GenerationUtility.CompileNativeGraph(selectedTab.owner);
		}

		public void PreviewSource() {
			Directory.CreateDirectory(GenerationUtility.tempFolder);
			char separator = Path.DirectorySeparatorChar;
			try {
				System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
				watch.Start();
				var script = GenerationUtility.GenerateCSharpScript(selectedTab.owner, (progress, text) => {
					EditorUtility.DisplayProgressBar($"Generating C# Scripts", text, progress);
				});
				if(preferenceData.generatorData != null && preferenceData.generatorData.analyzeScript && preferenceData.generatorData.formatScript) {
					var codeFormatter = EditorBinding.codeFormatter;
					if(codeFormatter != null) {
						script.postScriptModifier += input => {
							return codeFormatter.
								GetMethod("FormatCode").
								InvokeOptimized(null, new object[] { input }) as string;
						};
					}
				}
				var generatedScript = script.ToScript(out var informations);
				string path = GenerationUtility.tempFolder + separator + script.fileName + ".cs";
				using (StreamWriter sw = new StreamWriter(path)) {
					sw.Write(generatedScript);
					sw.Close();
				}
				watch.Stop();
				string originalScript = generatedScript;
				EditorUtility.DisplayProgressBar($"Generating C# Scripts", "Analizing Generated C# Script", 1);
				//Debug.LogFormat("Generating C# took {0,8:N3} s.", watch.Elapsed.TotalSeconds);
				var syntaxHighlighter = EditorBinding.syntaxHighlighter;
				if(syntaxHighlighter != null) {
					string highlight = syntaxHighlighter.GetMethod("GetRichText").InvokeOptimized(null, new object[] { generatedScript }) as string;
					if(!string.IsNullOrEmpty(highlight)) {
						generatedScript = highlight;
					}
				}
				var previewWindow = PreviewSourceWindow.ShowWindow(generatedScript, originalScript);
				previewWindow.informations = informations?.ToArray();
				previewWindow.OnChanged(graphData);
				EditorUtility.ClearProgressBar();
#if UNODE_DEBUG
				uNodeEditorUtility.CopyToClipboard(script.ToRawScript());
#endif
			}
			catch (Exception ex) {
				EditorUtility.ClearProgressBar();
				Debug.LogError("Aborting Generating C# Script because of errors.\nErrors: " + ex.ToString());
				throw;
			}
		}

		public static void PatchScript(Type scriptType, TabData tab) {
			if(tab.owner == null || EditorBinding.patchType == null)
				return;
			try {
				System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
				watch.Start();
				var script = GenerationUtility.GenerateCSharpScript(tab.owner, (progress, text) => {
					EditorUtility.DisplayProgressBar($"Generating C# Scripts", text, progress);
				});
				var dir = "TempScript" + Path.DirectorySeparatorChar + "Patched";
				Directory.CreateDirectory(dir);
				var path = Path.GetFullPath(dir) + Path.DirectorySeparatorChar + script.fileName + ".cs";
				using(StreamWriter sw = new StreamWriter(path)) {
					var generatedScript = script.ToScript(out var informations, true);
					SavedData.UnregisterGraphInfo(script.graphOwner);
					if(informations != null) {
						SavedData.RegisterGraphInfos(informations, script.graphOwner, path);
					}
					sw.Write(generatedScript);
					sw.Close();
				}
				GraphUtility.UpdateDatabase(new[] { script });
				//if(generatorSettings.convertLineEnding) {
				//	generatedScript = ConvertLineEnding(generatedScript,
				//		Application.platform != RuntimePlatform.WindowsEditor);
				//}
				EditorUtility.DisplayProgressBar("Compiling", "", 1);
				var assembly = GenerationUtility.CompileFromFile(path);
				if(assembly != null) {
					string typeName;
					if(string.IsNullOrEmpty(script.Namespace)) {
						typeName = script.classNames.First().Value;
					}
					else {
						typeName = script.Namespace + "." + script.classNames.First().Value;
					}
					var type = assembly.GetType(typeName);
					if(type != null) {
						EditorUtility.DisplayProgressBar("Patching", "Patch generated c# into existing script.", 1);
						EditorBinding.patchType(scriptType, type);
						//ReflectionUtils.RegisterRuntimeAssembly(assembly);
						//ReflectionUtils.UpdateAssemblies();
						//ReflectionUtils.GetAssemblies();
						watch.Stop();
						Debug.LogFormat("Generating & Patching type: {1} took {0,8:N3} s.", watch.Elapsed.TotalSeconds, scriptType);
					}
					else {
						watch.Stop();
						Debug.LogError($"Error on patching script because type: {typeName} is not found.");
					}
				}
				EditorUtility.ClearProgressBar();
			}
			catch {
				EditorUtility.ClearProgressBar();
				Debug.LogError("Aborting Generating C# Script because have error.");
				throw;
			}
		}

		public static GraphDebug.DebugData GetDebugData(out object debugTarget) {
			if(uNodeUtility.isPlaying && window != null && window.graphData != null) {
				debugTarget = window.graphData.debugTarget;
				return GetDebugData(window.graphData);
			}
			debugTarget = null;
			return null;
		}

		public static GraphDebug.DebugData GetDebugData(GraphEditorData data) {
			object debugObject = data.debugTarget;
			if(uNodeUtility.isPlaying && debugObject != null) {
				if(debugObject is GraphDebug.DebugData) {
					return debugObject as GraphDebug.DebugData;
				}
				else if(GraphDebug.debugData.TryGetValue(data.graph.GetGraphID(), out var debugMap)) {
					debugMap.TryGetValue(debugObject, out var val);
					return val;
				}
				//var db = uNodeUtility.debugData;
				//var id = uNodeUtility.GetObjectID(editorData.graph);
			}
			return null;
		}
		#endregion
	}
}