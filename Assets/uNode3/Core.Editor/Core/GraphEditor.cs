using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;

namespace MaxyGames.UNode.Editors {
	public abstract class GraphEditor {
		public class CanvasData {
			public readonly HashSet<string> features = new();

			public bool SupportSurroundWith => features.Contains(nameof(GraphManipulator.Feature.SurroundWith));
			public bool SupportMacro => features.Contains(nameof(GraphManipulator.Feature.Macro));
			public bool SupportPlaceFit => features.Contains(nameof(GraphManipulator.Feature.PlaceFit));
			public bool ShowAddNodeContextMenu => features.Contains(nameof(GraphManipulator.Feature.ShowAddNodeContextMenu));

			public bool IsFeatureSupported(string feature) => feature.Contains(feature);

			public void Reset() {
				features.Clear();
			}
		}


		#region Variables
		public uNodeEditor window;
		public CanvasData canvasData = new();

		public Vector2 topMousePos;
		#endregion

		#region Static
		public static GraphEditor openedGraph;

		public const string TabDragKEY = "[uNode-Tab]";

		/// <summary>
		/// Refresh opened graph.
		/// </summary>
		public static void RefreshOpenedGraph() {
			if(openedGraph != null) {
				openedGraph.Refresh();
			}
		}
		#endregion

		#region Event
		public abstract void Highlight(UGraphElement element);

		public void UpdatePosition() {
			window?.UpdatePosition();
		}

		[NonSerialized]
		private UGraphElement m_prevCanvas;
		public void CanvasChanged() {
			if(graphData != null) {
				if(m_prevCanvas != graphData.currentCanvas) {
					m_prevCanvas = graphData.currentCanvas;
					OnCanvasChanged();
				}
			}
		}

		public void SelectionChanged() {
			uNodeThreadUtility.ExecuteOnce(() => {
				window?.EditorSelectionChanged();
				OnSelectionChanged();
			}, "[GRAPH_SELECTION_CHANGED]");
		}

		public void GUIChanged() {
			uNodeEditor.GUIChanged();
		}

		public virtual void GUIChanged(object obj, UIChangeType changeType) {

		}

		public virtual void MarkRepaint(NodeObject node) {
			MarkRepaint(new NodeObject[] { node });
		}

		public virtual void MarkRepaint(IEnumerable<NodeObject> nodes) {

		}

		/// <summary>
		/// Refresh the graph.
		/// </summary>
		public void Refresh() {
			window?.Refresh();
			GUIChanged();
		}

		/// <summary>
		/// Repaint the graph.
		/// </summary>
		public void Repaint() {
			window?.Repaint();
		}

		public GraphSearchQuery searchQuery = new GraphSearchQuery();
		public virtual void OnSearchChanged(string search) {
			if(search == null || string.IsNullOrEmpty(search.Trim())) {
				searchQuery = new GraphSearchQuery();
			} else {
				searchQuery.query.Clear();
				var strs = search.Split(':');
				if(strs.Length == 2) {
					if(string.IsNullOrEmpty(strs[1].Trim())) {
						searchQuery.type = GraphSearchQuery.SearchType.None;
						return;
					}
					searchQuery.query.AddRange(strs[1].Split('&').Where(s => !string.IsNullOrEmpty(s.Trim())).Select(s => s.Trim()));
					switch(strs[0].Trim()) {
						case "p":
						case "port":
							searchQuery.type = GraphSearchQuery.SearchType.Port;
							break;
						case "t":
						case "type":
							searchQuery.type = GraphSearchQuery.SearchType.NodeType;
							break;
						case "n":
						case "node":
							searchQuery.type = GraphSearchQuery.SearchType.Node;
							break;
					}
				} else {
					searchQuery.query.AddRange(strs[0].Split('&').Where(s => !string.IsNullOrEmpty(s.Trim())).Select(s => s.Trim()));
					searchQuery.type = GraphSearchQuery.SearchType.Node;
				}
			}
		}

		public virtual void OnSelectionChanged() { }

		public void Compile() {
			Compile(true);
		}

		public void Compile(bool openAdditionalMenu) {
			if(tabData.owner is IScriptGraph) {

			}
			else {
				if(graphData.graphSystem == null) return;
				if(graphData.graphSystem.allowCompileToScript == false) {
					uNodeEditorUtility.DisplayErrorMessage("The current edited graph doesn't support for compile to c# scripts.");
					return;
				}
				if(graphData.graph is IInstancedGraph) {
					uNodeEditorUtility.DisplayErrorMessage("The current edited graph doesn't support for compile to c# scripts with this button, instead try use menu: Tools > uNode > Generate C# including Scenes.");
					return;
				}
			}

			var preferenceData = uNodePreference.preferenceData;
			if(tabData.owner is IScriptGraph || graphData.graphSystem.isScriptGraph) {
				if(openAdditionalMenu == false) {
					window?.GenerateSource();
					EditorUtility.ClearProgressBar();
					return;
				}
				GenericMenu menu = new GenericMenu();
				if(Application.isPlaying && EditorBinding.patchType != null) {
					if(graphData.graph != null) {
						var type = TypeSerializer.Deserialize(uNodeEditorUtility.GetFullScriptName(graphData.graph), false);
						if(type != null) {
							menu.AddItem(new GUIContent("Patch Current Graph"), false, () => {
								if(preferenceData.generatorData.generationMode != GenerationKind.Compatibility) {
									if(EditorUtility.DisplayDialog(
										"Warning!",
										uNodeEditor.MESSAGE_PATCH_WARNING + $"\n\nDo you want to ignore and patch in '{preferenceData.generatorData.generationMode}' mode?",
										"Yes", "No")) {
										uNodeEditor.PatchScript(type, tabData);
										EditorUtility.ClearProgressBar();
									}
								}
								else {
									uNodeEditor.PatchScript(type, tabData);
									EditorUtility.ClearProgressBar();
								}
							});
						}
						//menu.AddItem(new GUIContent("Patch Project Graphs"), false, () => {
						//	GenerationUtility.CompileAndPatchProjectGraphs();
						//});
						menu.AddSeparator("");
					}
				}
				menu.AddSeparator("");
				//if(Application.isPlaying) {
				//	menu.AddDisabledItem(new GUIContent("Compile Current Graph"), false);
				//	menu.AddDisabledItem(new GUIContent("Compile All C# Graph"), false);
				//	menu.AddSeparator("");
				//	menu.AddDisabledItem(new GUIContent("Compile Graphs (Project)"), false);
				//	menu.AddDisabledItem(new GUIContent("Compile Graphs (Project + Scenes)"), false);
				//}
				//else {
				//}
				menu.AddItem(new GUIContent("Compile Current Graph"), false, () => {
					window?.GenerateSource();
					EditorUtility.ClearProgressBar();
				});
				menu.AddItem(new GUIContent("Compile Opened C# Graphs"), false, () => {
					uNodeEditor.AutoSaveCurrentGraph();

					List<IScriptGraph> graphs = new();
					foreach(var tab in window.tabDatas) {
						if(tab != null && tab.owner is IScriptGraph) {
							graphs.Add(tab.owner as IScriptGraph);
						}
					}
					if(graphs.Count > 0)
						GenerationUtility.GenerateNativeGraphs(graphs);
				});
				menu.AddItem(new GUIContent("Compile All C# Graphs in project"), false, () => {
					if(Application.isPlaying) {
						uNodeEditorUtility.DisplayErrorMessage("Cannot compile all graph on playmode");
						return;
					}
					uNodeEditor.AutoSaveCurrentGraph();
					GenerationUtility.GenerateNativeGraphsInProject();
				});
				menu.AddSeparator("");
				menu.AddItem(new GUIContent("Compile Graphs (Project)"), false, () => {
					uNodeEditor.AutoSaveCurrentGraph();
					GenerationUtility.GenerateCSharpScript();
				});
				menu.AddItem(new GUIContent("Compile Graphs (Project + Scenes)"), false, () => {
					uNodeEditor.AutoSaveCurrentGraph();
					GenerationUtility.GenerateCSharpScriptIncludingSceneGraphs();
				});
				menu.ShowAsContext();
				uNodeEditor.AutoSaveCurrentGraph();
			}
			else if(graphData.graphSystem.allowAutoCompile) {
				GenericMenu menu = new GenericMenu();
				if(Application.isPlaying) {
					if(graphData.graph != null && EditorBinding.patchType != null) {
						var type = TypeSerializer.Deserialize(uNodeEditorUtility.GetFullScriptName(graphData.graph), false);
						if(type != null) {
							menu.AddItem(new GUIContent("Patch Current Graph"), false, () => {
								if(preferenceData.generatorData.generationMode != GenerationKind.Compatibility) {
									if(EditorUtility.DisplayDialog(
										"Warning!",
										uNodeEditor.MESSAGE_PATCH_WARNING + $"\n\nDo you want to ignore and patch in '{preferenceData.generatorData.generationMode}' mode?",
										"Yes", "No")) {
										uNodeEditor.PatchScript(type, tabData);
										EditorUtility.ClearProgressBar();
									}
								}
								else {
									uNodeEditor.PatchScript(type, tabData);
									EditorUtility.ClearProgressBar();
								}
							});
						}
						menu.AddItem(new GUIContent("Patch Project Graphs"), false, () => {
							if(preferenceData.generatorData.generationMode != GenerationKind.Compatibility) {
								if(EditorUtility.DisplayDialog(
									"Warning!",
									uNodeEditor.MESSAGE_PATCH_WARNING + $"\n\nDo you want to ignore and patch in '{preferenceData.generatorData.generationMode}' mode?",
									"Yes", "No")) {
									GenerationUtility.CompileAndPatchProjectGraphs();
								}
							}
							else {
								GenerationUtility.CompileAndPatchProjectGraphs();
							}
						});
					}
					menu.AddDisabledItem(new GUIContent("Compile Graphs (Project)"), false);
					menu.AddDisabledItem(new GUIContent("Compile Graphs (Project + Scenes)"), false);
				}
				else {
					if(preferenceData.generatorData.compilationMethod == CompilationMethod.Unity) {
						menu.AddItem(new GUIContent("Compile Graphs (Project)"), false, () => {
							uNodeEditor.AutoSaveCurrentGraph();
							GenerationUtility.GenerateCSharpScript();
						});
						menu.AddItem(new GUIContent("Compile Graphs (Project + Scenes)"), false, () => {
							uNodeEditor.AutoSaveCurrentGraph();
							GenerationUtility.GenerateCSharpScriptIncludingSceneGraphs();
						});
					}
					else {
						uNodeEditor.AutoSaveCurrentGraph();
						GenerationUtility.GenerateCSharpScript();
						return;
					}
				}
				menu.ShowAsContext();
			}
			else {
				uNodeEditorUtility.DisplayErrorMessage("The current edited graph doesn't support for compile to c# scripts.");
			}
		}

		public virtual void OnSearchNext() {

		}

		public virtual void OnSearchPrev() {

		}
		#endregion

		#region Properties
		public object debugTarget {
			get {
				return graphData?.debugTarget;
			}
			set {
				if(graphData != null)
					graphData.debugTarget = value;
			}
		}

		/// <summary>
		/// Return the zoom scale
		/// </summary>
		public float zoomScale {
			get {
				return graphData.GetCurrentCanvasData().zoomScale;
			}
			protected set {
				graphData.GetCurrentCanvasData().zoomScale = value;
			}
		}

		/// <summary>
		/// Return the canvas position
		/// </summary>
		public Vector2 position {
			get {
				return graphData.position;
			}
		}

		/// <summary>
		/// True if the canvas is currently zooming
		/// </summary>
		public bool isZoom {
			get {
				return zoomScale != 1;
			}
		}

		public float loadingProgress { get; set; }

		/// <summary>
		/// Return the graph data
		/// </summary>
		public GraphEditorData graphData => window?.graphData;

		/// <summary>
		/// Return the tab data
		/// </summary>
		public uNodeEditor.TabData tabData => window?.selectedTab;

		public IEnumerable<NodeObject> nodes {
			get {
				return graphData != null ? graphData.nodes : null;
			}
		}
		#endregion

		#region Menu

		private static Lazy<List<CreateNodeProcessor>> m_createNodeProcessors = new(() => {
			var value = EditorReflectionUtility.GetListOfType<CreateNodeProcessor>();
			value.Sort((x, y) => CompareUtility.Compare(x.order, y.order));
			return value;
		});

		public static bool CreateNodeProcessor(MemberData member, GraphEditorData editorData, Vector2 position, Action<Node> onCreated) {
			var members = member.GetMembers(false);
			void PostAction() {
				if(uNodePreference.preferenceData.autoAddNamespace) {
					if(members != null && members.Length > 0 || member.targetType.IsTargetingReflection() || member.targetType == MemberData.TargetType.Values) {
						var type = member.startType;
						if(type != null && type.IsPrimitive == false && type != typeof(string) && type != typeof(object)) {
							var ns = type.Namespace;
							if(string.IsNullOrWhiteSpace(ns) == false) {
								if(editorData.owner is IUsingNamespace usingNamespace) {
									if(usingNamespace.UsingNamespaces.Contains(ns) == false) {
										usingNamespace.UsingNamespaces.Add(ns);
									}
								}
							}
						}
					}
				}
			}
			
			foreach(var processor in m_createNodeProcessors.Value) {
				if(processor.Process(member, editorData, position, onCreated)) {
					PostAction();
					return true;
				}
			}

			NodeEditorUtility.AddNewNode<MultipurposeNode>(editorData, position, n => {
				n.target = member;
				n.Register();
				onCreated?.Invoke(n);
				//For auto assign instance port
				if(n.instance != null && n.instance.isAssigned == false) {
					var graphType = editorData.graph.GetGraphType();
					if(graphType != null) {
						var instanceType = n.instance.type;
						if(graphType.IsCastableTo(instanceType) == false && NodeEditorUtility.CanAutoConvertType(graphType, instanceType)) {
							NodeEditorUtility.AddNewNode<MultipurposeNode>(editorData, new Vector2(position.x - 200, position.y), thisNode => {
								thisNode.target = MemberData.This(editorData.graph);
								thisNode.Register();

								NodeEditorUtility.AutoConvertPort(graphType, instanceType, thisNode.output, n.instance, convertNode => {
									n.instance.ConnectTo(NodeEditorUtility.GetPort<ValueOutput>(convertNode));
								}, editorData.currentCanvas);
							});
						}
					}
				}
				PostAction();
			});
			return false;
		}

		public Vector2 GetMenuPosition() {
			return window.GetMousePositionForMenu(topMousePos);
		}

		public Vector2 GetMenuPosition(Vector2 position) {
			return window.GetMousePositionForMenu(position);
		}

		public ItemSelector ShowFavoriteMenu(Vector2 position,
			FilterAttribute filter = null,
			Action<Node> onAddNode = null,
			NodeFilter nodeFilter = NodeFilter.None,
			Func<MemberData, bool> processMember = null) {
			if(uNodeEditorUtility.DisplayRequiredProVersion()) {
				return null;
			}

			var valueMenuPos = GetMenuPosition();
			if(filter == null) {
				filter = new FilterAttribute();
				//filter.CanSelectType = true;
				//filter.HideTypes.Add(typeof(void));
			} else {
				filter = new FilterAttribute(filter);
			}
			filter.DisplayInstanceOnStatic = false;
			filter.MaxMethodParam = int.MaxValue;
			filter.Public = true;
			filter.Instance = true;
			if(nodeFilter == NodeFilter.None || nodeFilter.HasFlags(NodeFilter.FlowInput)) {
				filter.VoidType = true;
			}
			var customItems = ItemSelector.MakeFavoriteTrees(() => {
				var favoriteItems = new List<ItemSelector.CustomItem>();
				if(filter.OnlyGetType == false) {
					var actualType = filter.GetActualType();
					foreach(var menuItem in NodeEditorUtility.FindNodeMenu()) {
						if(!uNodeEditor.SavedData.HasFavorite("NODES", menuItem.type.FullName))
							continue;
						if(NodeEditorUtility.IsValidMenu(menuItem, actualType, nodeFilter, graphData) == false) {
							continue;
						}
						favoriteItems.Add(ItemSelector.CustomItem.Create(
							menuItem,
							() => {
								NodeEditorUtility.AddNewNode<Node>(graphData, menuItem.nodeName, menuItem.type, position, onAddNode);
								Refresh();
							},
							icon: uNodeEditorUtility.GetTypeIcon(menuItem.GetIcon()),
							category: "Nodes"));
					}
				}
				return favoriteItems;
			}, filter);
			ItemSelector w = ItemSelector.ShowWindow(
				graphData.graph,
				filter,
				delegate (MemberData value) {
					if(processMember?.Invoke(value) == true) {
						Refresh();
						return;
					}
					CreateNodeProcessor(value, graphData, position, (n) => {
						if(onAddNode != null) {
							onAddNode(n);
						}
						Refresh();
					});
				}).ChangePosition(valueMenuPos);
			w.displayDefaultItem = false;
			w.CustomTrees = customItems;
			return w;
		}

		public ItemSelector ShowNodeMenu(Vector2 position,
			FilterAttribute filter = null,
			Action<Node> onAddNode = null,
			NodeFilter nodeFilter = NodeFilter.None,
			List<ItemSelector.CustomItem> additionalItems = null,
			IEnumerable<string> expandedCategory = null,
			Func<MemberData, bool> processMember = null,
			bool stackCreateNode = true) {
			stackedActions.Clear();
			return ShowCreateNodeMenu(position, filter, onAddNode, nodeFilter, additionalItems, expandedCategory, processMember, stackCreateNode);
		}


		static List<Action> stackedActions = new List<Action>();

		static void PopStackedAction() {
			if(stackedActions.Count > 0) {
				uNodeThreadUtility.Queue(static () => {
					var action = stackedActions[stackedActions.Count - 1];
					stackedActions.RemoveAt(stackedActions.Count - 1);
					action?.Invoke();
				});
			}
		}

		private ItemSelector ShowCreateNodeMenu(Vector2 position,
			FilterAttribute filter = null,
			Action<Node> onAddNode = null,
			NodeFilter nodeFilter = NodeFilter.None,
			List<ItemSelector.CustomItem> additionalItems = null,
			IEnumerable<string> expandedCategory = null,
			Func<MemberData, bool> processMember = null,
			bool stackCreateNode = true) {

			void ExecuteStackedAction(NodeObject nodeObject) {
				if(nodeObject.ValueInputs.Count > 0) {
					if(Event.current != null) {
						if(Event.current.control) {
							for(int i = nodeObject.ValueInputs.Count - 1; i >= 0; i--) {
								var port = nodeObject.ValueInputs[i];
								if(port.hasValidConnections == false) {
									stackedActions.Add(() => {
										ShowCreateNodeMenu(new Vector2(position.x - 200, position.y), new FilterAttribute(port.type) { MaxMethodParam = int.MaxValue }, (n) => {
											var otherPort = n.nodeObject.primaryValueOutput ?? n.nodeObject.ValueOutputs.FirstOrDefault();
											if(otherPort != null) {
												port.ConnectTo(otherPort);
											}
										}, NodeFilter.ValueInput, additionalItems, expandedCategory, processMember);
									});
								}
							}
						}
					}
				}
				PopStackedAction();
			}

			var valueMenuPos = GetMenuPosition();
			if(filter == null) {
				filter = new FilterAttribute();
				//filter.CanSelectType = true;
				//filter.HideTypes.Add(typeof(void));
			}
			else {
				filter = new FilterAttribute(filter);
			}
			filter.DisplayInstanceOnStatic = true;
			filter.MaxMethodParam = int.MaxValue;
			filter.Public = true;
			filter.Instance = true;
			if(nodeFilter == NodeFilter.None || nodeFilter.HasFlags(NodeFilter.FlowInput | NodeFilter.FlowOutput)) {
				filter.VoidType = true;
			}
			ItemSelector w = ItemSelector.ShowWindow(
				graphData.currentCanvas,
				filter,
				delegate (MemberData value) {
					if(processMember?.Invoke(value) == true) {
						Refresh();
						return;
					}
					CreateNodeProcessor(value, graphData, position, (n) => {
						if(onAddNode != null) {
							onAddNode(n);
						}
						ExecuteStackedAction(n);
						Refresh();
					});
				}).ChangePosition(valueMenuPos);
			w.favoriteHandler = () => {
				var favoriteItems = new List<ItemSelector.CustomItem>();
				if(filter.OnlyGetType == false) {
					var actualType = filter.GetActualType();
					foreach(var menuItem in NodeEditorUtility.FindNodeMenu()) {
						if(!uNodeEditor.SavedData.HasFavorite("NODES", menuItem.type.FullName))
							continue;
						if(NodeEditorUtility.IsValidMenu(menuItem, actualType, nodeFilter, graphData) == false) {
							continue;
						}
						favoriteItems.Add(ItemSelector.CustomItem.Create(
							menuItem,
							() => {
								NodeEditorUtility.AddNewNode<Node>(graphData, menuItem.nodeName, menuItem.type, position, onAddNode);
								Refresh();
							},
							icon: uNodeEditorUtility.GetTypeIcon(menuItem.GetIcon()),
							category: "Nodes"));
					}
				}
				return favoriteItems;
			};
			w.displayNoneOption = false;
			w.displayCustomVariable = false;
			w.customItemDefaultExpandState = false;
			w.defaultExpandedItems = expandedCategory;
			if(filter.SetMember)
				return w;//Return on set member is true.
			List<ItemSelector.CustomItem> customItems = new List<ItemSelector.CustomItem>();
			if(additionalItems != null) {
				customItems.AddRange(additionalItems);
			}
			var actualType = filter.GetActualType();
			{
				customItems.AddRange(ItemSelector.MakeCustomItemsForMacros(graphData.currentCanvas, position, nodeFilter, actualType, node => {
					onAddNode?.Invoke(node);
					ExecuteStackedAction(node);
					Refresh();
				}));
			}
			if(filter.OnlyGetType == false) {
				foreach(var menuItem in NodeEditorUtility.FindNodeMenu()) {
					if(filter.OnlyGetType && menuItem.type != typeof(Type)) {
						continue;
					}
					if(NodeEditorUtility.IsValidMenu(menuItem, actualType, nodeFilter, graphData) == false) {
						continue;
					}
					customItems.Add(ItemSelector.CustomItem.Create(
						menuItem,
						() => {
							NodeEditorUtility.AddNewNode<Node>(graphData, menuItem.nodeName, menuItem.type, position, n => {
								onAddNode?.Invoke(n);
								ExecuteStackedAction(n);
							});
							Refresh();
						},
						icon: uNodeEditorUtility.GetTypeIcon(menuItem.GetIcon())));
				}
			}

			#region Flow
			//TODO: fixme
			//if(flowNodes && !filter.SetMember && filter.IsValidType(typeof(void))) {
			//	if(!(editorData.selectedRoot != null && editorData.selectedGroup != null)) {
			//		customItems.Add(ItemSelector.CustomItem.Create("Continue", delegate () {
			//			NodeEditorUtility.AddNewNode<NodeJumpStatement>(
			//				editorData,
			//				"Continue",
			//				position,
			//				delegate (NodeJumpStatement n) {
			//					n.statementType = JumpStatementType.Continue;
			//				});
			//			Refresh();
			//		}, "JumpStatement", icon: uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.FlowIcon))));
			//		customItems.Add(ItemSelector.CustomItem.Create("Break", delegate () {
			//			NodeEditorUtility.AddNewNode<NodeJumpStatement>(
			//				editorData,
			//				"Break",
			//				position,
			//				delegate (NodeJumpStatement n) {
			//					n.statementType = JumpStatementType.Break;
			//					if(onAddNode != null) {
			//						onAddNode(n);
			//					}
			//				});
			//			Refresh();
			//		}, "JumpStatement", icon: uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.FlowIcon))));
			//	}
			//	if(editorData.selectedRoot != null) {
			//		customItems.Add(ItemSelector.CustomItem.Create("Return", delegate () {
			//			NodeEditorUtility.AddNewNode<NodeReturn>(
			//				editorData,
			//				"Return",
			//				position,
			//				delegate (NodeReturn n) {
			//					if(onAddNode != null) {
			//						onAddNode(n);
			//					}
			//				});
			//			Refresh();
			//		}, "Return", icon: uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.FlowIcon))));
			//	}
			//}
			#endregion

			if(filter.IsValidTarget(MemberData.TargetType.NodePort)) {
				if(filter.IsValidType(typeof(Type))) {
					customItems.Add(ItemSelector.CustomItem.Create("typeof()", delegate () {
						var win = TypeBuilderWindow.Show(Vector2.zero, graphData.currentCanvas, new FilterAttribute() { OnlyGetType = true, DisplayGeneratedRuntimeType = false }, delegate (MemberData[] types) {
							if(processMember?.Invoke(types[0]) == true) {
								Refresh();
								return;
							}
							NodeEditorUtility.AddNewNode<MultipurposeNode>(graphData, position, n => {
								n.target = types[0];
								n.Register();
								if(onAddNode != null) {
									onAddNode(n);
								}
								ExecuteStackedAction(n);
								Refresh();
								w.Close();
							});
						});
						win.ChangePosition(valueMenuPos);
						GUIUtility.ExitGUI();
					}, "Data"));
				}
				var nodeMenuItems = NodeEditorUtility.FindCreateNodeCommands(this, nodeFilter, filter);
				foreach(var n in nodeMenuItems) {
					if(!n.IsValid()) {
						continue;
					}
					customItems.Add(ItemSelector.CustomItem.Create(n.name, () => {
						var createdNode = n.Setup(position);
						if(onAddNode != null) {
							onAddNode(createdNode);
						}
						ExecuteStackedAction(createdNode);
					}, n.category, icon: uNodeEditorUtility.GetTypeIcon(n.icon)));
				}
			}
			ItemSelector.SortCustomItems(customItems);
			w.customItems = customItems;
			return w;
		}
		#endregion

		#region Functions
		public virtual void FrameGraph() {

		}

		public void ReloadView() {
			ReloadView(false);
		}

		public virtual void ReloadView(bool fullReload) {

		}

		public virtual void UndoRedoPerformed() {

		}

		public virtual void OnEnable() {
			CanvasChanged();
		}

		public virtual void OnDisable() {

		}

		public virtual void OnNoTarget() {

		}

		public virtual void OnErrorUpdated() {

		}

		public void Validate() {
			if(graphData != null && graphData.IsSupportMainGraph == false && graphData.graphData != null) {
				var container = graphData.graphData.mainGraphContainer;
				if(container.childCount > 0) {
					container.Destroy();
				}
			}
		}

		protected virtual void OnCanvasChanged() {
			var data = canvasData;
			data.Reset();

			data.features.Add(nameof(GraphManipulator.Feature.Macro));
			data.features.Add(nameof(GraphManipulator.Feature.PlaceFit));
			data.features.Add(nameof(GraphManipulator.Feature.SurroundWith));
			data.features.Add(nameof(GraphManipulator.Feature.ShowAddNodeContextMenu));

			var manipulators = NodeEditorUtility.FindGraphManipulators();
			foreach(var manipulator in manipulators) {
				manipulator.graphEditor = this;
				if(manipulator.IsValid(nameof(manipulator.GetCanvasFeatures))) {
					var obj = manipulator.GetCanvasFeatures();
					if(obj != null) {
						data.features.AddRange(obj);
					}
				}
			}
			foreach(var manipulator in manipulators) {
				if(manipulator.IsValid(nameof(manipulator.ManipulateCanvasFeatures))) {
					manipulator.ManipulateCanvasFeatures(data.features);
				}
			}
		}

		/// <summary>
		/// Draw a canvas ( called every frame )
		/// </summary>
		/// <param name="window"></param>
		public virtual void DrawCanvas(uNodeEditor window) {
			this.window = window;
			openedGraph = this;
			topMousePos = Event.current.mousePosition;
		}

		public virtual void DrawMainTab(uNodeEditor window) {
			this.window = window;
			openedGraph = this;
			topMousePos = Event.current.mousePosition;
		}
		#endregion

		#region Utility
		/// <summary>
		/// Get the debug data
		/// </summary>
		/// <returns></returns>
		protected GraphDebug.DebugData GetDebugData() {
			return uNodeEditor.GetDebugData(graphData);
		}

		/// <summary>
		/// Get mouse position on the canvas
		/// </summary>
		/// <returns></returns>
		public Vector2 GetMousePosition() {
			return topMousePos;
		}

		/// <summary>
		/// Clear the selections
		/// </summary>
		public void ClearSelection() {
			window.ChangeEditorSelection(null);
			OnSelectionChanged();
		}

		public void SelectNode(NodeObject node, bool clearSelectedNodes = true) {
			if(clearSelectedNodes)
				graphData.ClearSelection();
			graphData.AddToSelection(node);
			SelectionChanged();
		}

		public void Select(UGraphElement value) {
			graphData.ClearSelection();
			graphData.AddToSelection(value);
			SelectionChanged();
		}

		public void Select(BaseReference reference) {
			graphData.ClearSelection();
			graphData.AddToSelection(reference);
			SelectionChanged();
		}

		public void Unselect(UGraphElement element) {
			graphData.RemoveFromSelection(element);
		}

		public void Select(IEnumerable<UGraphElement> elements) {
			ClearSelection();
			foreach(var node in elements) {
				graphData.AddToSelection(node);
			}
			SelectionChanged();
			Refresh();
		}

		/// <summary>
		/// Move the canvas to the position
		/// </summary>
		/// <param name="position"></param>
		public virtual void MoveCanvas(Vector2 position) {
			if(graphData == null)
				return;
			graphData.position = position;
			CanvasChanged();
		}

		/// <summary>
		/// Set zoom scale of the graph
		/// </summary>
		/// <param name="zoom"></param>
		public virtual void SetZoomScale(float zoom) {
			zoomScale = zoom;
		}

		/// <summary>
		/// Paste the copied nodes
		/// </summary>
		/// <param name="position"></param>
		/// <param name="removeOtherConnections"></param>
		public NodeObject[] PasteNode(Vector2 position, bool removeOtherConnections = false) {
			if(graphData.currentCanvas != null && GraphUtility.CopyPaste.IsCopiedNodes) {
				var nodes = GraphUtility.CopyPaste.Paste(graphData.currentCanvas, removeOtherConnections: removeOtherConnections).Select(n => n as NodeObject).ToArray();

				Vector2 center = Vector2.zero;
				foreach(var node in nodes) {
					center.x += node.position.x;
					center.y += node.position.y;
				}
				center /= nodes.Length;
				foreach(var node in nodes) {
					node.position.x = node.position.x - center.x + position.x;
					node.position.y = node.position.y - center.y + position.y;
				}
				Refresh();
				return nodes;
			}
			return Array.Empty<NodeObject>();
		}

		public virtual void CreateLinkedMacro(MacroGraph macro, Vector2 position) {
			NodeEditorUtility.AddNewNode<Nodes.LinkedMacroNode>(graphData, null, null, position, (node) => {
				node.macroAsset = macro;
				node.Refresh();
				node.Register();
				NodeEditorUtility.AutoAssignNodePorts(node);
			});
			Refresh();
		}

		public virtual void SelectionAddRegion(Vector2 position) {
			var nodes = graphData.selectedNodes.ToArray();
			Rect rect;
			if(nodes.Length > 0) {
				rect = NodeEditorUtility.GetNodeRect(nodes);
			}
			else {
				rect = new Rect(position.x, position.y, 200, 130);
			}
			uNodeEditorUtility.RegisterUndo(graphData.owner, "Create region");
			NodeEditorUtility.AddNewNode<Nodes.NodeRegion>(graphData, default, (node) => {
				rect.x -= 30;
				rect.y -= 50;
				rect.width += 60;
				rect.height += 70;
				node.position = rect;
				node.nodeColor = new Color(UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f));
			});
			Refresh();
		}
		#endregion

		#region Shortcut
		public virtual void HandleShortcut(GraphShortcutType type) {
			switch(type) {
				case GraphShortcutType.Save: {
					GraphUtility.SaveAllGraph();
					break;
				}
				case GraphShortcutType.AddNode: {

					break;
				}
				case GraphShortcutType.CopySelectedNodes: {

					break;
				}
				case GraphShortcutType.CreateRegion: {

					break;
				}
				case GraphShortcutType.DuplicateNodes: {

					break;
				}
				case GraphShortcutType.FrameGraph: {
					FrameGraph();
					break;
				}
				case GraphShortcutType.OpenCommand: {

					break;
				}
				case GraphShortcutType.PasteNodesClean: {

					break;
				}
				case GraphShortcutType.PasteNodesWithLink: {

					break;
				}
				case GraphShortcutType.PreviewScript: {
					window.PreviewSource();
					break;
				}
				case GraphShortcutType.CompileScript: {
					Compile(false);
					break;
				}
				case GraphShortcutType.Refresh: {
					window.Refresh(true);
					break;
				}
			}
		}
		#endregion
	}
}