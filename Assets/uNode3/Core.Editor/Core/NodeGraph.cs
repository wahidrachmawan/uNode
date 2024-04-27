using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;

namespace MaxyGames.UNode.Editors {
	[Serializable]
	public class GraphSearchQuery {
		public enum SearchType {
			None,
			Node,
			Port,
			NodeType,
		}

		public List<string> query = new List<string>();
		public SearchType type = SearchType.None;

		public static HashSet<string> csharpKeyword = new HashSet<string>() {
			"false",
			"true",
			"null",
			"bool",
			"byte",
			"char",
			"decimal",
			"double",
			"float",
			"int",
			"long",
			"object",
			"sbyte",
			"short",
			"string",
			"uint",
			"ulong",
		};
	}

	public abstract class NodeGraph {
		#region Variables
		public uNodeEditor window;

		public Vector2 topMousePos;
		#endregion

		#region Static
		public static NodeGraph openedGraph;
		
		public const string TabDragKEY = "[uNode-Tab]";

		public static HashSet<string> GetOpenedGraphUsingNamespaces() {
			HashSet<string> ns = null;
			if(openedGraph != null) {
				ns = openedGraph.graphData.GetNamespaces();
			}
			return ns;
		}

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

		public void SelectionChanged() {
			window?.EditorSelectionChanged();
		}

		public void GUIChanged() {
			uNodeEditor.GUIChanged();
		}

		public virtual void GUIChanged(object obj, UIChangeType changeType) {

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

		public void Compile() {
			if(tabData.owner is IScriptGraph) {

			}
			else {
				if(graphData.graphSystem == null) return;
				if(graphData.graphSystem.allowCompileToScript == false) {
					uNodeEditorUtility.DisplayErrorMessage("The current edited graph doesn't support for compile to c# scripts.");
					return;
				}
			}

			var preferenceData = uNodePreference.preferenceData;
			if(tabData.owner is IScriptGraph || graphData.graphSystem.isScriptGraph) {
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
				menu.AddItem(new GUIContent("Compile All C# Graph"), false, () => {
					if(Application.isPlaying) {
						uNodeEditorUtility.DisplayErrorMessage("Cannot compile all graph on playmode");
						return;
					}
					uNodeEditor.AutoSaveCurrentGraph();
					GenerationUtility.GenerateNativeGraphInProject();
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

		public float zoomScale {
			get {
				return graphData.GetCurrentCanvasData().zoomScale;
			}
			protected set {
				graphData.GetCurrentCanvasData().zoomScale = value;
			}
		}

		public bool isZoom {
			get {
				return zoomScale != 1;
			}
		}

		public float loadingProgress { get; set; }

		public GraphEditorData graphData => window?.graphData;

		public uNodeEditor.TabData tabData => window?.selectedTab;

		public IEnumerable<NodeObject> nodes {
			get {
				return graphData != null ? graphData.nodes : null;
			}
		}
		#endregion

		#region Menu
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
			if(members != null && members.Length > 0 && members[members.Length - 1] is MethodInfo method && method.Name.StartsWith("op_", StringComparison.Ordinal)) {
				string name = method.Name;
				switch(name) {
					case "op_Addition":
					case "op_Subtraction":
					case "op_Multiply":
					case "op_Division":
						NodeEditorUtility.AddNewNode<Nodes.MultiArithmeticNode>(editorData, null, null, position, n => {
							switch(name) {
								case "op_Addition":
									n.operatorKind = ArithmeticType.Add;
									break;
								case "op_Subtraction":
									n.operatorKind = ArithmeticType.Subtract;
									break;
								case "op_Multiply":
									n.operatorKind = ArithmeticType.Multiply;
									break;
								case "op_Division":
									n.operatorKind = ArithmeticType.Divide;
									break;
							}
							n.EnsureRegistered();
							var param = method.GetParameters();
							for(int i = 0; i < param.Length; i++) {
								n.inputs[i].type = param[i].ParameterType;
								n.inputs[i].port.AssignToDefault(MemberData.Default(param[i].ParameterType));
							}
							n.nodeObject.Register();
							onCreated?.Invoke(n);
							PostAction();
						});
						return true;
				}
			}
			if(member.targetType.IsTargetingReflection() &&
				member.isStatic == false &&
				member.isDeepTarget == false &&
				member.instance is IClassGraph classGraph &&
				(member.startType == classGraph.InheritType || classGraph.InheritType.IsSubclassOf(member.startType))) {

				NodeEditorUtility.AddNewNode<NodeBaseCaller>(editorData, position, n => {
					n.target = member;
					n.EnsureRegistered();
					onCreated?.Invoke(n);
					PostAction();
				});
				return true;
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

		public void ShowFavoriteMenu(Vector2 position,
			FilterAttribute filter = null,
			Action<Node> onAddNode = null,
			NodeFilter nodeFilter = NodeFilter.None) {
			if(uNodeEditorUtility.DisplayRequiredProVersion()) {
				return;
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
								NodeEditorUtility.AddNewNode<Node>(graphData, menuItem.nodeName ?? menuItem.name.Split(' ')[0], menuItem.type, position, onAddNode);
								Refresh();
							},
							icon: menuItem.icon != null ? uNodeEditorUtility.GetTypeIcon(menuItem.icon) : null,
							category: "Nodes"));
					}
				}
				return favoriteItems;
			}, filter);
			ItemSelector w = ItemSelector.ShowWindow(
				graphData.graph,
				filter,
				delegate (MemberData value) {
					CreateNodeProcessor(value, graphData, position, (n) => {
						if(onAddNode != null) {
							onAddNode(n);
						}
						Refresh();
					});
				}).ChangePosition(valueMenuPos);
			w.displayDefaultItem = false;
			w.CustomTrees = customItems;
		}

		public void ShowNodeMenu(Vector2 position,
			FilterAttribute filter = null,
			Action<Node> onAddNode = null,
			NodeFilter nodeFilter = NodeFilter.None,
			List<ItemSelector.CustomItem> additionalItems = null,
			IEnumerable<string> expandedCategory = null) {
			var valueMenuPos = GetMenuPosition();
			if(filter == null) {
				filter = new FilterAttribute();
				//filter.CanSelectType = true;
				//filter.HideTypes.Add(typeof(void));
			} 
			//else {
			//	filter = new FilterAttribute(filter);
			//}
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
					CreateNodeProcessor(value, graphData, position, (n) => {
						if(onAddNode != null) {
							onAddNode(n);
						}
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
								NodeEditorUtility.AddNewNode<Node>(graphData, menuItem.nodeName ?? menuItem.name.Split(' ')[0], menuItem.type, position, onAddNode);
								Refresh();
							},
							icon: menuItem.icon != null ? uNodeEditorUtility.GetTypeIcon(menuItem.icon) : null,
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
				return;//Return on set member is true.
			List<ItemSelector.CustomItem> customItems = new List<ItemSelector.CustomItem>();
			if(additionalItems != null) {
				customItems.AddRange(additionalItems);
			}
			var actualType = filter.GetActualType();
			{
				customItems.AddRange(ItemSelector.MakeCustomItemsForMacros(graphData.currentCanvas, position, nodeFilter, actualType, node => {
					onAddNode?.Invoke(node);
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
							NodeEditorUtility.AddNewNode<Node>(graphData, menuItem.nodeName ?? menuItem.name.Split(' ')[0], menuItem.type, position, onAddNode);
							Refresh();
						},
						icon: menuItem.icon != null ? uNodeEditorUtility.GetTypeIcon(menuItem.icon) : null));
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
							NodeEditorUtility.AddNewNode<MultipurposeNode>(graphData, position, n => {
								n.target = types[0];
								n.Register();
								if(onAddNode != null) {
									onAddNode(n);
								}
								Refresh();
								w.Close();
							});
						});
						win.ChangePosition(valueMenuPos);
						GUIUtility.ExitGUI();
					}, "Data"));
				}
				var nodeMenuItems = NodeEditorUtility.FindCreateNodeCommands();
				foreach(var n in nodeMenuItems) {
					n.graph = this;
					n.filter = filter;
					if(!n.IsValid()) {
						continue;
					}
					customItems.Add(ItemSelector.CustomItem.Create(n.name, () => {
						var createdNode = n.Setup(position);
						if(onAddNode != null) {
							onAddNode(createdNode);
						}
					}, n.category, icon: uNodeEditorUtility.GetTypeIcon(n.icon)));
				}
			}
			ItemSelector.SortCustomItems(customItems);
			w.customItems = customItems;
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

		}

		public virtual void OnDisable() {

		}

		public virtual void OnNoTarget() {

		}

		public virtual void DrawTabbar(Vector2 position) {

		}

		public virtual void OnErrorUpdated() {

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
		protected GraphDebug.DebugData GetDebugData() {
			return uNodeEditor.GetDebugData(graphData);
		}

		public Vector2 GetMousePosition() {
			return topMousePos;
		}

		public void ClearSelection() {
			window.ChangeEditorSelection(null);
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

		public void UnselectNode(NodeObject node) {
			graphData.RemoveFromSelection(node);
		}

		public void SelectNode(IEnumerable<NodeObject> nodes) {
			ClearSelection();
			foreach(var node in nodes) {
				graphData.AddToSelection(node);
			}
			SelectionChanged();
			Refresh();
		}

		public void SelectRoot(NodeContainer root) {
			graphData.GetPosition(root);
			graphData.AddToSelection(root);
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
		}

		public void PasteNode(Vector2 position, bool removeOtherConnections = false) {
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
			}
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
					Compile();
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