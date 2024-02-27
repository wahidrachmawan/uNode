using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace MaxyGames.UNode.Editors {
	public abstract class HierarchyDrawer {
		public GraphHierarchyTree manager;
		public virtual int order => 0;
		public abstract bool IsValid(Type type);

		#region Functions
		public virtual HierarchyNodeTree CreateNodeTree(NodeObject nodeComponent) {
			var tree = new HierarchyNodeTree(nodeComponent, -1);
			tree.icon = uNodeEditorUtility.GetTypeIcon(nodeComponent.GetNodeIcon()) as Texture2D;
			return tree;
		}

		public static HierarchyFlowTree CreateFlowTree(NodeObject owner, FlowPort flow, string displayName) {
			var tree = new HierarchyFlowTree(owner, flow, uNodeEditorUtility.GetUIDFromString($"Graph:{owner.graphContainer.GetGraphID()}-Node:{owner.id}:F={flow.id}"), -1, displayName) {
				icon = uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.FlowIcon)) as Texture2D
			};
			return tree;
		}

		public virtual void AddChildNodes(NodeObject nodeComponent, TreeViewItem parentTree, IList<TreeViewItem> rows) {
			foreach(var port in nodeComponent.FlowOutputs) {
				if(port.isNextFlow) {
					manager.AddNodeTree(port, parentTree, rows, false);
				}
				else {
					if(string.IsNullOrEmpty(port.name)) {
						manager.AddNodeTree(port, parentTree, rows, true);
					}
					else {
						var flowItem = CreateFlowTree(
							nodeComponent,
							port,
							port.GetPrettyName()
						);
						parentTree.AddChild(flowItem);
						rows.Add(flowItem);
						manager.AddNodeTree(port, flowItem, rows);
						//manager.AddNodeTree(port, parentTree, rows);
					}
				}
			}
		}
		#endregion

		#region Find Drawer
		private static List<HierarchyDrawer> _drawers;
		private static Dictionary<Type, HierarchyDrawer> drawerMaps = new Dictionary<Type, HierarchyDrawer>();
		public static List<HierarchyDrawer> FindHierarchyDrawer() {
			if(_drawers == null) {
				_drawers = EditorReflectionUtility.GetListOfType<HierarchyDrawer>();
				_drawers.Sort((x, y) => {
					return CompareUtility.Compare(x.order, y.order);
				});
			}
			return _drawers;
		}

		public static HierarchyDrawer FindDrawer(Type type) {
			if(type == null)
				return Default;
			if(drawerMaps.TryGetValue(type, out var drawer)) {
				return drawer;
			}
			var drawers = FindHierarchyDrawer();
			for(int i = 0; i < drawers.Count; i++) {
				if(drawers[i].IsValid(type)) {
					drawer = drawers[i];
					break;
				}
			}
			drawerMaps[type] = drawer;
			return drawer;
		}
		#endregion

		#region Default Drawer
		class DefaultDrawer : HierarchyDrawer {
			public override int order => int.MaxValue;
			public override bool IsValid(Type type) => true;
		}

		private static HierarchyDrawer _default;
		public static HierarchyDrawer Default {
			get {
				if(_default == null) {
					_default = new DefaultDrawer();
				}
				return _default;
			}
		}
		#endregion
	}

	#region Drawer
	class HierarchyStateDrawer : HierarchyDrawer {
		public override bool IsValid(Type type) {
			return type == typeof(Nodes.StateNode);
		}

		public override void AddChildNodes(NodeObject nodeComponent, TreeViewItem parentItem, IList<TreeViewItem> rows) {
			var node = nodeComponent.node as Nodes.StateNode;
			var flows = node.nestedFlowNodes;
			if(flows.Any()) {
				//var tree = new HierarchyFlowTree(nodeComponent, MemberData.none, uNodeEditorUtility.GetUIDFromString(nodeComponent.GetInstanceID() + "[EVENTS]"), -1, "Events");
				//parentItem.AddChild(tree);
				//rows.Add(tree);
				//for(int i = 0; i < flows.Count; i++) {
				//	manager.AddNodes(flows[i], tree, rows);
				//}
			}
			var transitions = node.GetTransitions();
			if(transitions.Any()) {
				//var tree = new HierarchyFlowTree(nodeComponent, MemberData.none, uNodeEditorUtility.GetUIDFromString(nodeComponent.GetInstanceID() + "[TRANSITIONS]"), -1, "Transitions");
				//parentItem.AddChild(tree);
				//rows.Add(tree);
				foreach(var tr in transitions) { 
					var transitionTree = new HierarchyTransitionTree(tr, -1);
					manager.AddNodeTree(transitionTree, parentItem, rows);
					manager.AddNodeTree(tr.exit, transitionTree, rows);
				}
			}
		}
	}

	class HierarchyMacroDrawer : HierarchyDrawer {
		public override bool IsValid(Type type) {
			return type == typeof(Nodes.MacroPortNode);
		}

		public override HierarchyNodeTree CreateNodeTree(NodeObject nodeComponent) {
			var target = nodeComponent.node as Nodes.MacroPortNode;
			var parentNode = nodeComponent.parent as NodeObject;
			if(parentNode != null) {
				var tree = new HierarchyNodeTree(nodeComponent, -1);
				tree.icon = uNodeEditorUtility.GetTypeIcon(nodeComponent.GetNodeIcon()) as Texture2D;
				if(target.kind == PortKind.FlowInput) {
					tree.displayName = string.IsNullOrEmpty(tree.displayName) ? parentNode.GetTitle() : $"{parentNode.GetTitle()} ( {tree.displayName} )";
				}
				return tree;
			}
			return null;
		}

		public override void AddChildNodes(NodeObject nodeComponent, TreeViewItem parentItem, IList<TreeViewItem> rows) {
			var macroPort = nodeComponent.node as Nodes.MacroPortNode;
			manager.AddNodeTree(macroPort.exit, macroPort.kind == PortKind.FlowInput ? parentItem : parentItem.parent, rows, macroPort.kind == PortKind.FlowInput);
		}
	}
	#endregion
}