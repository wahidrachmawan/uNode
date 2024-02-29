using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace MaxyGames.UNode.Editors {
	#region TreeViews
	internal class HiearchyNamespaceTree : TreeViewItem {
		public HiearchyNamespaceTree() {

		}

		public HiearchyNamespaceTree(string @namespace, int depth) : base(uNodeEditorUtility.GetUIDFromString("[NAMESPACES]" + @namespace), depth, @namespace) {
			icon = uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.NamespaceIcon)) as Texture2D;
		}
	}

	public class HierarchyGraphTree : TreeViewItem {
		public GraphAsset graph;

		public HierarchyGraphTree() {

		}

		public HierarchyGraphTree(GraphAsset graph, int depth) : base(graph.GetHashCode(), depth, graph.GetGraphName()) {
			this.graph = graph;
			icon = uNodeEditorUtility.GetTypeIcon(graph) as Texture2D;
		}
	}

	internal class HierarchySummaryTree : TreeViewItem {
		public TreeViewItem owner;

		public HierarchySummaryTree() {

		}

		public HierarchySummaryTree(string displayName, TreeViewItem owner, int depth = -1) : base(uNodeEditorUtility.GetUIDFromString(owner.id.ToString() + "[SUMMARY]"), depth, displayName) {
			this.owner = owner;
		}
	}

	internal class HierarchyVariableSystemTree : TreeViewItem {
		public VariableContainer variableSystem;

		public HierarchyVariableSystemTree() {

		}

		public HierarchyVariableSystemTree(VariableContainer variableSystem, int id, int depth) : base(id, depth, "Variables") {
			this.variableSystem = variableSystem;
			icon = uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.FieldIcon)) as Texture2D;
		}
	}

	internal class HierarchyVariableTree : TreeViewItem {
		public Variable variable;

		public HierarchyVariableTree() {

		}

		public HierarchyVariableTree(Variable variable, int id, int depth) : base(id, depth, $"{variable.name} : {variable.type.PrettyName()}") {
			this.variable = variable;
			icon = uNodeEditorUtility.GetTypeIcon(variable.type) as Texture2D;
		}
	}

	internal class HierarchyPropertySystemTree : TreeViewItem {
		public PropertyContainer propertySystem;

		public HierarchyPropertySystemTree() {

		}

		public HierarchyPropertySystemTree(PropertyContainer propertySystem, int id, int depth) : base(id, depth, "Properties") {
			this.propertySystem = propertySystem;
			icon = uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.PropertyIcon)) as Texture2D;
		}
	}

	internal class HierarchyPropertyTree : TreeViewItem {
		public Property property;

		public HierarchyPropertyTree() {

		}

		public HierarchyPropertyTree(Property property, int depth) : base(property.GetHashCode(), depth, $"{property.name} : {property.type.prettyName}") {
			this.property = property;
			icon = uNodeEditorUtility.GetTypeIcon(property.type) as Texture2D;
		}
	}

	internal class HierarchyFunctionSystemTree : TreeViewItem {
		public FunctionContainer functionSystem;

		public HierarchyFunctionSystemTree() {

		}

		public HierarchyFunctionSystemTree(FunctionContainer functionSystem, int id, int depth) : base(id, depth, "Functions") {
			this.functionSystem = functionSystem;
			icon = uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.MethodIcon)) as Texture2D;
		}
	}

	internal class HierarchyFunctionTree : TreeViewItem {
		public Function function;

		public HierarchyFunctionTree() {

		}

		public HierarchyFunctionTree(Function function, int depth) : base(function.GetHashCode(), depth, $"{function.name}({ string.Join(", ", function.Parameters.Select(p => p.type.prettyName)) }) : {function.returnType.prettyName}") {
			this.function = function;
			icon = uNodeEditorUtility.GetTypeIcon(function.returnType) as Texture2D;
		}
	}

	public class HierarchyNodeTree : TreeViewItem {
		public NodeObject node;

		public HierarchyNodeTree() {

		}

		public HierarchyNodeTree(NodeObject node, int depth) : base(node.GetHashCode(), depth, node.GetRichName()) {
			this.node = node;
		}
	}

	public class HierarchyTransitionTree : TreeViewItem {
		public TransitionEvent transition;

		public HierarchyTransitionTree() {

		}

		public HierarchyTransitionTree(TransitionEvent transition, int depth) : base(transition.GetHashCode(), depth, transition.name) {
			this.transition = transition;
		}
	}

	public class HierarchyRefNodeTree : TreeViewItem {
		public HierarchyNodeTree tree;

		public HierarchyRefNodeTree() {

		}

		public HierarchyRefNodeTree(HierarchyNodeTree tree, int depth) : base(tree.id, depth, tree.displayName) {
			this.tree = tree;
			icon = tree.icon;
		}
	}

	public class HierarchyPortTree : TreeViewItem {
		public FlowInput port;
		public NodeObject node;

		public HierarchyPortTree() {

		}

		public HierarchyPortTree(NodeObject node, FlowInput port, int id, int depth, string displayName) : base(id, depth, displayName) {
			this.node = node;
			this.port = port;
		}
	}

	public class HierarchyFlowTree : TreeViewItem {
		public NodeObject owner;
		public FlowPort flow;

		public HierarchyFlowTree() {

		}

		public HierarchyFlowTree(NodeObject owner, FlowPort flow, int id, int depth, string displayName) : base(id, depth, displayName) {
			this.flow = flow;
			this.owner = owner;
		}
	}
	#endregion

	public class GraphHierarchyTree : TreeView {
		public uNodeEditor graphEditor;
		public GraphEditorData graphData => graphEditor?.graphData;

		private NodeObject refSelectedTree;
		private IGraph graph;
		private HashSet<int> expandStates;
		private Dictionary<NodeObject, HierarchyNodeTree> nodeTreesMap = new Dictionary<NodeObject, HierarchyNodeTree>();
		private Dictionary<FlowInput, HierarchyPortTree> flowPortsMap = new Dictionary<FlowInput, HierarchyPortTree>();

		public GraphHierarchyTree(TreeViewState state) : base(state) {
			graphEditor = uNodeEditor.window;
			showAlternatingRowBackgrounds = true;
			showBorder = true;
			Reload(true);
		}

		public void Reload(bool initReload) {
			if(initReload) {
				expandStates = new HashSet<int>(GetExpanded());
				Reload();
				expandStates = null;
			}
			else {
				Reload();
			}
		}

		protected override TreeViewItem BuildRoot() {
			return new TreeViewItem { id = 0, depth = -1 };
		}

		protected override IList<TreeViewItem> BuildRows(TreeViewItem root) {
			graphEditor = uNodeEditor.window;
			var rows = GetRows() ?? new List<TreeViewItem>();
			rows.Clear();
			nodeTreesMap.Clear();
			flowPortsMap.Clear();
			if(graphEditor == null) {
				return rows;
			}
			if(graphData.graph != null) {
				var graph = graphData.graph;
				this.graph = graph;
				CreateTreeElement(graph.GraphData, root, rows);
				//int prevCount = rows.Count;
				//var item = new HierarchyGraphTree(graph, -1);
				//if(expandStates != null && !expandStates.Contains(item.id)) {
				//	SetExpanded(item.id, true);
				//}
				//if(IsExpanded(item.id)) {
				//	AddChildren(item, rows);
				//} else {
				//	item.children = CreateChildListForCollapsedParent();
				//}
				//root.AddChild(item);
				//rows.Insert(rows.Count - (rows.Count - prevCount), item);
			}
			SetupDepthsFromParentsAndChildren(root);
			return rows;
		}

		private void AddSummary(string summary, TreeViewItem owner, TreeViewItem parent, IList<TreeViewItem> rows) {
			if(string.IsNullOrEmpty(summary))
				return;
			var strs = summary.Split('\n');
			for(int i = 0; i < strs.Length; i++) {
				if(string.IsNullOrEmpty(strs[i]))
					continue;
				var tree = new HierarchySummaryTree(strs[i], owner);
				parent.AddChild(tree);
				rows.Add(tree);
			}
		}

		static bool ShowChildElements(UGraphElement element) {
			if(element is VariableContainer) {
				return true;
			}
			else if(element is PropertyContainer) {
				return true;
			}
			else if(element is FunctionContainer) {
				return true;
			}
			else if(element is ConstructorContainer) {
				return true;
			}
			else if(element is UGroupElement) {
				return true;
			}
			else if(element is Property) {
				return true;
			}
			return false;
		}

		void CreateTreeElement(UGraphElement element, TreeViewItem parent, IList<TreeViewItem> rows) {
			if(element is Variable) {
				var variable = element as Variable;
				var childItem = new HierarchyVariableTree(variable, uNodeEditorUtility.GetUIDFromString($"V:{variable.id}"), -1);
				AddSummary(variable.comment, childItem, parent, rows);
				parent.AddChild(childItem);
				rows.Add(childItem);
			}
			else if(element is Property) {
				var property = element as Property;
				var childItem = new HierarchyPropertyTree(property, -1);
				AddSummary(property.comment, childItem, parent, rows);
				parent.AddChild(childItem);
				rows.Add(childItem);
			}
			else if(element is Function) {
				var function = element as Function;
				var childItem = new HierarchyFunctionTree(function, -1);
				AddSummary(function.comment, childItem, parent, rows);
				parent.AddChild(childItem);
				rows.Add(childItem);
				if(expandStates != null && !expandStates.Contains(childItem.id)) {
					SetExpanded(childItem.id, true);
				}
				if(IsExpanded(childItem.id)) {
					AddNodes(function.Entry, childItem, rows);
				}
				else {
					childItem.children = CreateChildListForCollapsedParent();
				}
			}
			else if(element is Graph) {
				var graph = element as Graph;
				CreateTreeElement(graph.variableContainer, parent, rows);
				CreateTreeElement(graph.propertyContainer, parent, rows);
				CreateTreeElement(graph.functionContainer, parent, rows);
			}
			else if(element is VariableContainer) {
				var container = element as VariableContainer;
				int prevCount = rows.Count;
				var childItem = new HierarchyVariableSystemTree(container, uNodeEditorUtility.GetUIDFromString(graph.GetHashCode() + ":V"), -1);
				if(expandStates != null && !expandStates.Contains(childItem.id)) {
					SetExpanded(childItem.id, true);
				}
				if(container.Any()) {
					if(IsExpanded(childItem.id)) {
						foreach(var ele in element) {
							CreateTreeElement(ele, childItem, rows);
						}
					}
					else {
						childItem.children = CreateChildListForCollapsedParent();
					}
				}
				parent.AddChild(childItem);
				rows.Insert(rows.Count - (rows.Count - prevCount), childItem);
			}
			else if(element is PropertyContainer) {
				var container = element as PropertyContainer;
				int prevCount = rows.Count;
				var childItem = new HierarchyPropertySystemTree(container, uNodeEditorUtility.GetUIDFromString(graph.GetHashCode() + ":P"), -1);
				if(expandStates != null && !expandStates.Contains(childItem.id)) {
					SetExpanded(childItem.id, true);
				}
				if(container.Any()) {
					if(IsExpanded(childItem.id)) {
						foreach(var ele in element) {
							CreateTreeElement(ele, childItem, rows);
						}
					}
					else {
						childItem.children = CreateChildListForCollapsedParent();
					}
				}
				parent.AddChild(childItem);
				rows.Insert(rows.Count - (rows.Count - prevCount), childItem);
			}
			else if(element is FunctionContainer) {
				var container = element as FunctionContainer; int prevCount = rows.Count;
				var childItem = new HierarchyFunctionSystemTree(container, uNodeEditorUtility.GetUIDFromString(graph.GetHashCode() + ":F"), -1);
				if(expandStates != null && !expandStates.Contains(childItem.id)) {
					SetExpanded(childItem.id, true);
				}

				if(container.Any()) {
					if(IsExpanded(childItem.id)) {
						foreach(var ele in element) {
							CreateTreeElement(ele, childItem, rows);
						}
					}
					else {
						childItem.children = CreateChildListForCollapsedParent();
					}
				}

				parent.AddChild(childItem);
				rows.Insert(rows.Count - (rows.Count - prevCount), childItem);
			}
			else if(element is NodeContainer) {
				var childItem = new TreeViewItem(-1) {
					displayName = element.name,
					icon = uNodeEditorUtility.GetTypeIcon(element) as Texture2D,
				};
				if(element is MainGraphContainer) {
					childItem.displayName = (element as MainGraphContainer).GetPrettyName();
				}
				AddSummary(element.comment, childItem, parent, rows);
				parent.AddChild(childItem);
				rows.Add(childItem);
				if(expandStates != null && !expandStates.Contains(childItem.id)) {
					SetExpanded(childItem.id, true);
				}
				if(IsExpanded(childItem.id)) {
					if(element is NodeContainerWithEntry) {
						var entry = (element as NodeContainerWithEntry).Entry;
						AddNodes(entry, childItem, rows);
					}
					if(graph is MacroGraph) {
						var root = graph as MacroGraph;
						var flows = root.inputFlows;
						if(flows != null) {
							foreach(var flow in flows) {
								AddNodes(flow, childItem, rows);
							}
						}
					}
					foreach(var ele in element) {
						if(ele is NodeObject nodeObject && nodeObject.node is BaseEventNode) {
							//CreateTreeElement(ele, childItem, rows);
							AddNodes(nodeObject, childItem, rows);
						}
					}
				}
				else {
					childItem.children = CreateChildListForCollapsedParent();
				}
			}
			else {
				var childItem = new TreeViewItem(-1) {
					displayName = element.name,
					icon = uNodeEditorUtility.GetTypeIcon(element) as Texture2D,
				};
				if(element is MainGraphContainer) {
					childItem.displayName = (element as MainGraphContainer).GetPrettyName();
				}
				AddSummary(element.comment, childItem, parent, rows);
				parent.AddChild(childItem);
				rows.Add(childItem);
				if(expandStates != null && !expandStates.Contains(childItem.id)) {
					SetExpanded(childItem.id, true);
				}
				if(IsExpanded(childItem.id)) {
					foreach(var ele in element) {
						CreateTreeElement(ele, childItem, rows);
					}
				}
				else {
					childItem.children = CreateChildListForCollapsedParent();
				}
			}
		}

		protected override void SelectionChanged(IList<int> selectedIds) {
			bool flag = true;
			if(selectedIds?.Count > 0) {
				int firstSelection = selectedIds[0];
				var rows = GetRows();
				for(int i = 0; i < rows.Count; i++) {
					if(rows[i]?.id == firstSelection) {
						var tree = rows[i];
						if(tree is HierarchyFlowTree flowTree) {
							refSelectedTree = flowTree.owner;
							flag = false;
						}
						break;
					}
				}
			}
			if(flag) {
				refSelectedTree = null;
			}
			base.SelectionChanged(selectedIds);
		}

		protected override bool CanChangeExpandedState(TreeViewItem item) {
			if(!string.IsNullOrEmpty(searchString) || item is HierarchyFlowTree || item is HierarchyNodeTree || item is HierarchyPortTree || item is HierarchyTransitionTree) {
				return false;
			}
			return item.hasChildren;
		}

		protected override bool CanMultiSelect(TreeViewItem item) {
			return false;
		}

		protected override void RowGUI(RowGUIArgs args) {
			Event evt = Event.current;
			if(args.rowRect.Contains(evt.mousePosition)) {
				if(evt.type == EventType.ContextClick) {
					ContextClick(args.item, evt);
				}
				else if(evt.type == EventType.MouseDown) {
					if(evt.clickCount == 2 && evt.button == 0) {//Double click
						HighlightTree(args.item);
					}
					else if(evt.modifiers == EventModifiers.Shift && evt.button == 0) {//Left click + Shift
						Inspect(args.item, GUIUtility.GUIToScreenPoint(evt.mousePosition));
					}
				}
			}
			if(evt.type == EventType.Repaint) {
				#region Debug
				if(args.item is HierarchyNodeTree || args.item is HierarchyRefNodeTree) {
					NodeObject node = null;
					if(args.item is HierarchyNodeTree) {
						node = (args.item as HierarchyNodeTree).node;
					}
					else if(args.item is HierarchyRefNodeTree) {
						node = (args.item as HierarchyRefNodeTree).tree.node;
					}
					//bool hasBreakpoint = uNodeUtility.HasBreakpoint(uNodeUtility.GetObjectID(node));
					if(node != null && Application.isPlaying && GraphDebug.useDebug) {
						var debugData = GetDebugInfo();
						if(debugData != null && debugData.nodeDebug.TryGetValue(node.id, out var nodeDebug)) {
							var oldColor = GUI.color;
							switch(nodeDebug.nodeState) {
								case StateType.Success:
									GUI.color = UIElementUtility.Theme.nodeSuccessColor;
									break;
								case StateType.Failure:
									GUI.color = UIElementUtility.Theme.nodeFailureColor;
									break;
								case StateType.Running:
									GUI.color = UIElementUtility.Theme.nodeRunningColor;
									break;
							}
							GUI.color = new Color(GUI.color.r, GUI.color.g, GUI.color.b, 0.2f);
							Rect debugRect = args.rowRect;
							debugRect.width = GetContentIndent(args.item);
							GUI.DrawTexture(new Rect(debugRect.x + debugRect.width - 10, debugRect.y, 10, debugRect.height), Texture2D.whiteTexture);
							float time = (GraphDebug.debugTime - nodeDebug.calledTime) * GraphDebug.transitionSpeed * 2;
							GUI.color = Color.Lerp(GUI.color, Color.clear, time);
							if(time < 1) {
								GUI.DrawTexture(new Rect(debugRect.x, debugRect.y, debugRect.width - 10, debugRect.height), Texture2D.whiteTexture);
							}
							GUI.color = oldColor;
						}
					}
				}
				else if(args.item is HierarchyFlowTree) {
					var tree = args.item as HierarchyFlowTree;
					var flow = tree.flow;
					var node = (tree.parent as HierarchyNodeTree)?.node ?? (tree.parent as HierarchyPortTree)?.node;
					if(node != null && flow != null && Application.isPlaying && GraphDebug.useDebug) {
						var debugData = GetDebugInfo();
						if(debugData != null) {
							var oldColor = GUI.color;
							float times = -1;
							if(flow is FlowInput) {
								var flowDebugData = debugData.GetDebugValue(flow as FlowInput);
								if(flowDebugData != null) {
									times = GraphDebug.debugTime - flowDebugData.calledTime;
								}
							}
							else {
								var flowDebugData = debugData.GetDebugValue(flow as FlowOutput);
								if(flowDebugData.time > 0) {
									times = GraphDebug.debugTime - flowDebugData.time;
								}
							}
							if(times >= 0) {
								GUI.color = UIElementUtility.Theme.nodeSuccessColor;
								GUI.color = new Color(GUI.color.r, GUI.color.g, GUI.color.b, 0.2f);
								Rect debugRect = args.rowRect;
								debugRect.width = GetContentIndent(args.item);
								GUI.DrawTexture(new Rect(debugRect.x + debugRect.width - 10, debugRect.y, 10, debugRect.height), Texture2D.whiteTexture);
								GUI.color = Color.Lerp(GUI.color, Color.clear, times * GraphDebug.transitionSpeed * 2);
								GUI.DrawTexture(new Rect(debugRect.x, debugRect.y, debugRect.width - 10, debugRect.height), Texture2D.whiteTexture);
								GUI.color = oldColor;
							}
						}
					}
				}
				#endregion

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
					if(!args.selected && refSelectedTree != null) {
						DrawHighlightedBackground(args.item, args.rowRect);
					}
					if(args.item is HierarchyNodeTree) {
						var node = (args.item as HierarchyNodeTree).node;
						if(GraphDebug.Breakpoint.HasBreakpoint(node.graphContainer.GetGraphID(), node.id)) {
							var oldColor = GUI.color;
							GUI.color = Color.red;
							GUI.DrawTexture(new Rect(args.rowRect.x, args.rowRect.y, 16, 16), uNodeUtility.DebugPoint);
							GUI.color = oldColor;
						}
					}
					uNodeGUIStyle.itemNormal.Draw(labelRect, new GUIContent(args.label, args.item.icon), false, false, false, false);
				}
				#endregion
			}
			//base.RowGUI(args);
		}

		#region Private Functions
		private void ContextClick(TreeViewItem tree, Event evt) {
			if(tree is HierarchyNodeTree nodeTree) {
				var node = nodeTree.node;
				var mPOS = GUIUtility.GUIToScreenPoint(evt.mousePosition);
				GenericMenu menu = new GenericMenu();
				menu.AddItem(new GUIContent("Inspect..."), false, () => {
					Inspect(nodeTree, mPOS);
				});
				menu.AddItem(new GUIContent("Hightlight Node"), false, () => {
					uNodeEditor.HighlightNode(node);
				});
				MonoScript ms = uNodeEditorUtility.GetMonoScript(node);
				if(ms != null) {
					menu.AddSeparator("");
					menu.AddItem(new GUIContent("References/Find Script"), false, delegate () {
						EditorGUIUtility.PingObject(ms);
					});
					menu.AddItem(new GUIContent("References/Edit Script"), false, delegate () {
						AssetDatabase.OpenAsset(ms);
					});
				}
				if(!GraphDebug.Breakpoint.HasBreakpoint(node.graphContainer.GetGraphID(), node.id)) {
					menu.AddItem(new GUIContent("Add Breakpoint"), false, delegate () {
						GraphDebug.Breakpoint.AddBreakpoint(node.graphContainer.GetGraphID(), node.id);
						uNodeGUIUtility.GUIChanged(node);
					});
				}
				else {
					menu.AddItem(new GUIContent("Remove Breakpoint"), false, delegate () {
						GraphDebug.Breakpoint.RemoveBreakpoint(node.graphContainer.GetGraphID(), node.id);
						uNodeGUIUtility.GUIChanged(node);
					});
				}
				menu.ShowAsContext();
			}
			else if(tree is HierarchyRefNodeTree) {
				ContextClick((tree as HierarchyRefNodeTree).tree, evt);
			}
			else if(tree is HierarchySummaryTree) {
				ContextClick((tree as HierarchySummaryTree).owner, evt);
			}
		}

		private void DrawHighlightedBackground(TreeViewItem tree, Rect position) {
			if(tree is HierarchyNodeTree) {
				var nTree = tree as HierarchyNodeTree;
				if(nTree.node == refSelectedTree) {
					var oldColor = GUI.color;
					var color = Color.yellow;
					color.a = 0.2f;
					GUI.color = color;
					GUI.DrawTexture(position, Texture2D.whiteTexture);
					GUI.color = oldColor;
				}
			}
			else if(tree is HierarchyRefNodeTree) {
				DrawHighlightedBackground((tree as HierarchyRefNodeTree).tree, position);
			}
		}

		private bool HighlightTree(TreeViewItem tree) {
			if(tree is HierarchyNodeTree) {
				var node = (tree as HierarchyNodeTree).node;
				uNodeEditor.HighlightNode(node);
				return true;
			}
			else if(tree is HierarchyPortTree) {
				uNodeEditor.HighlightNode((tree as HierarchyPortTree).node);
				return true;
			}
			else if(tree is HierarchyFlowTree) {
				uNodeEditor.HighlightNode((tree as HierarchyFlowTree).owner);
				return true;
			}
			else if(tree is HierarchyRefNodeTree) {
				return HighlightTree((tree as HierarchyRefNodeTree).tree);
			}
			else if(tree is HierarchySummaryTree) {
				return HighlightTree((tree as HierarchySummaryTree).owner);
			}
			return false;
		}

		private GraphDebug.DebugData GetDebugInfo() {
			if(graphData.graph != graph)
				return null;
			return uNodeEditor.GetDebugData(graphData);
		}
		#endregion

		#region Functions
		private void Inspect(TreeViewItem treeView, Vector2 position) {
			if(treeView is HierarchyNodeTree nodeTree) {
				ActionPopupWindow.ShowWindow(Vector2.zero, () => {
					CustomInspector.ShowInspector(new GraphEditorData(graph as UnityEngine.Object, new[] { nodeTree.node }));
				}, 300, 300).ChangePosition(position);
			}
			else if(treeView is HierarchyFunctionTree functionTree) {
				ActionPopupWindow.ShowWindow(Vector2.zero, () => {
					CustomInspector.ShowInspector(new GraphEditorData(graph as UnityEngine.Object, new[] { functionTree.function }));
				}, 300, 300).ChangePosition(position);
			}
			else if(treeView is HierarchyPropertyTree propertyTree) {
				ActionPopupWindow.ShowWindow(Vector2.zero, () => {
					CustomInspector.ShowInspector(new GraphEditorData(graph as UnityEngine.Object, new[] { propertyTree.property }));
				}, 300, 300).ChangePosition(position);
			}
			else if(treeView is HierarchyVariableTree variableTree) {
				ActionPopupWindow.ShowWindow(Vector2.zero, () => {
					CustomInspector.ShowInspector(new GraphEditorData(graph as UnityEngine.Object, new[] { variableTree.variable }));
				}, 300, 300).ChangePosition(position);
			}
		}

		public bool AddNodeTree(TreeViewItem tree, TreeViewItem parentTree, IList<TreeViewItem> rows, bool isChildren = true) {
			if(tree == null || parentTree == null)
				return false;
			if(isChildren) {
				parentTree.AddChild(tree);
			}
			else {
				parentTree.parent.AddChild(tree);
			}
			rows.Add(tree);
			return true;
		}

		public bool CanAddTree(FlowPort flow) {
			if(flow.hasValidConnections) {
				if(flow is FlowOutput flowOutput) {
					return flowOutput.GetTargetNode() != null;
				}
				else if(flow is FlowInput flowInput) {
					return flowInput.node != null;
				}
			}
			return false;
		}

		public bool AddNodeTree(FlowPort flow, TreeViewItem parentTree, IList<TreeViewItem> rows, bool isChildren = true) {
			if(flow.hasValidConnections) {
				if(flow is FlowOutput flowOutput) {
					var n = flowOutput.GetTargetNode();
					if(n != null) {
						AddNodes(n, parentTree, rows, isChildren);
						return true;
					}
				}
				else if(flow is FlowInput flowInput) {
					var n = flowInput.node;
					bool flag = false;
					if(!flowPortsMap.TryGetValue(flowInput, out var flowTree)) {
						flag = true;
						flowTree = new HierarchyPortTree(
							n,
							flowInput,
							uNodeEditorUtility.GetUIDFromString($"{n.id}:FI={flow.id}"),
							-1,
							$"{n.GetTitle()} ( {ObjectNames.NicifyVariableName(flowInput.GetPrettyName())} )") {
							icon = uNodeEditorUtility.GetTypeIcon(n.GetNodeIcon()) as Texture2D
						};
						flowPortsMap[flowInput] = flowTree;
					}
					if(isChildren) {
						parentTree.AddChild(flowTree);
					}
					else {
						parentTree.parent.AddChild(flowTree);
					}
					rows.Add(flowTree);
					if(flag) {
						var drawer = HierarchyDrawer.FindDrawer(n.GetType());
						drawer.manager = this;
						drawer.AddChildNodes(n, flowTree, rows);
					}
					return true;
				}
			}
			return false;
		}

		public void AddNodes(NodeObject nodeComponent, TreeViewItem parentItem, IList<TreeViewItem> rows, bool isChildren = true) {
			if(nodeTreesMap.TryGetValue(nodeComponent, out var childItem)) {
				var tree = new HierarchyRefNodeTree(childItem, -1);
				if(isChildren) {
					parentItem.AddChild(tree);
				}
				else {
					parentItem.parent.AddChild(tree);
				}
				rows.Add(tree);
				return;
			}
			var drawer = HierarchyDrawer.FindDrawer(nodeComponent.node.GetType());
			drawer.manager = this;
			//Create Node Tree
			childItem = drawer.CreateNodeTree(nodeComponent);
			if(isChildren) {
				AddSummary(nodeComponent.comment, childItem, parentItem, rows);
				parentItem.AddChild(childItem);
			}
			else {
				AddSummary(nodeComponent.comment, childItem, parentItem.parent, rows);
				parentItem.parent.AddChild(childItem);
			}
			rows.Add(childItem);
			nodeTreesMap[nodeComponent] = childItem;
			drawer.AddChildNodes(nodeComponent, childItem, rows);
		}
		#endregion
	}
}