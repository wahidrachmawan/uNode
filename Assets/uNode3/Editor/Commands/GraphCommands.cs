using System;
using System.Collections.Generic;
using UnityEngine;
using MaxyGames.UNode.Nodes;
using UnityEditor.Experimental.GraphView;
using System.Linq;
using Random = UnityEngine.Random;

namespace MaxyGames.UNode.Editors.Commands {
	public class FindInBrowserOutputPinCommand : PortMenuCommand {
		public override string name {
			get {
				return "Find in browser";
			}
		}

		public override bool onlyContextMenu => true;

		public override void OnClick(Node source, PortCommandData data, Vector2 mousePosition) {
			Type type = data.portType;
			uNodeEditorUtility.FindInBrowser(type);
		}

		public override bool IsValidPort(Node source, PortCommandData data) {
			return data.portKind == PortKind.ValueOutput || data.portKind == PortKind.ValueInput;
		}
	}

	//public class FindInBrowserNodeCommands : NodeMenuCommand {
	//	public override string name {
	//		get {
	//			return "Find in browser";
	//		}
	//	}

	//	public override void OnClick(Node source, Vector2 mousePosition) {
	//		if(source is MultipurposeNode) {
	//			var node = source as MultipurposeNode;
	//			if(node.target.isAssigned) {
	//				if(node.target.targetType == MemberData.TargetType.Type) {
	//					var win = NodeBrowserWindow.ShowWindow();
	//					win.browser.RevealItem(node.target.startType);
	//					win.Focus();
	//					return;
	//				}
	//				var members = node.target.GetMembers();
	//				if(members != null && members.Length > 0) {
	//					var win = NodeBrowserWindow.ShowWindow();
	//					win.browser.RevealItem(members.LastOrDefault());
	//					win.Focus();
	//					return;
	//				}
	//			}
	//		}
	//	}

	//	public override bool IsValidNode(Node source) {
	//		return source is MultipurposeNode;
	//	}
	//}

	public class AddActivateTransitionNode : GraphMenuCommand {
		public override string name {
			get {
				return "Activate Transition";
			}
		}

		public override void OnClick(Vector2 mousePosition) {
			NodeEditorUtility.AddNewNode<Nodes.ActivateTransition>(graph.graphData, null, null, mousePositionOnCanvas, null);
			graph.Refresh();
		}

		public override bool IsValid() {
			return graph.graphData.currentCanvas is NodeObject nod && nod.node is StateNode;
		}
	}

	#region Macro
	public class AddInputFlowMacroNode : GraphMenuCommand {
		public override string name {
			get {
				return "New Input Flow";
			}
		}

		public override void OnClick(Vector2 mousePosition) {
			NodeEditorUtility.AddNewNode<MacroPortNode>(graph.graphData, null, null, mousePositionOnCanvas, (node) => {
				node.nodeObject.name = "flow" + Random.Range(0, 255);
				node.kind = PortKind.FlowInput;
			});
			graph.Refresh();
		}

		public override bool IsValid() {
			return graph.graphData.currentCanvas is NodeObject nodeObject && nodeObject.node is IMacro || graph.graphData.graph is IMacroGraph;
		}
	}

	public class AddOutputFlowMacroNode : GraphMenuCommand {
		public override string name {
			get {
				return "New Output Flow";
			}
		}

		public override void OnClick(Vector2 mousePosition) {
			NodeEditorUtility.AddNewNode<MacroPortNode>(graph.graphData, null, null, mousePositionOnCanvas, (node) => {
				node.nodeObject.name = "flow" + Random.Range(0, 255);
				node.kind = PortKind.FlowOutput;
			});
			graph.Refresh();
		}

		public override bool IsValid() {
			return graph.graphData.currentCanvas is NodeObject nodeObject && nodeObject.node is IMacro || graph.graphData.graph is IMacroGraph;
		}
	}

	public class AddInputValueMacroNode : GraphMenuCommand {
		public override string name {
			get {
				return "New Input Value";
			}
		}

		public override void OnClick(Vector2 mousePosition) {
			NodeEditorUtility.AddNewNode<MacroPortNode>(graph.graphData, null, null, mousePositionOnCanvas, (node) => {
				node.nodeObject.name = "value" + Random.Range(0, 255);
				node.kind = PortKind.ValueInput;
			});
			graph.Refresh();
		}

		public override bool IsValid() {
			return graph.graphData.currentCanvas is NodeObject nodeObject && nodeObject.node is IMacro || graph.graphData.graph is IMacroGraph;
		}
	}

	public class AddOutputValueMacroNode : GraphMenuCommand {
		public override string name {
			get {
				return "New Output Value";
			}
		}

		public override void OnClick(Vector2 mousePosition) {
			NodeEditorUtility.AddNewNode<MacroPortNode>(graph.graphData, null, null, mousePositionOnCanvas, (node) => {
				node.nodeObject.name = "value" + Random.Range(0, 255);
				node.kind = PortKind.ValueOutput;
			});
			graph.Refresh();
		}

		public override bool IsValid() {
			return graph.graphData.currentCanvas is NodeObject nodeObject && nodeObject.node is IMacro || graph.graphData.graph is IMacroGraph;
		}
	}
	#endregion
}