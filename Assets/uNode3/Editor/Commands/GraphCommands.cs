using System;
using System.Collections.Generic;
using UnityEngine;
using MaxyGames.UNode.Nodes;
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

	public class SurroundWith : GraphMenuCommand {
		public override string name {
			get {
				return "Surround Selection With...";
			}
		}

		public override void OnClick(Vector2 mousePosition) {
			SurroundWithWindow.ShowWindow((command) => {
				// Get first node in selection where the selection does not contain any node from all inputs connections
				var sourceOutputs = new List<FlowOutput>();
				var sourceNodes = graph.nodes.Where(node => !graph.graphData.selectedNodes.Contains(node) && node.FlowOutputs.Any(output => {
					if(output.hasValidConnections && graph.graphData.selectedNodes.Contains(output.connections[0].input.node)) {
						sourceOutputs.Add(output);
						return true;
					}
					return false;
				})).ToList();

				// Get the destination node that all sources connect to
				var firstDestination = sourceOutputs.FirstOrDefault()?.connections[0].input.node;
				// Check if all sources connect to the same destination node
				bool allSourcesConnectToSame = sourceOutputs.All(output =>
						output.hasValidConnections &&
						output.connections[0].input.node.node == firstDestination.node);

				if(sourceNodes.Count > 1 && !allSourcesConnectToSame) {
					Debug.LogWarning("Cannot surround different flows!");
					return;
				}
				var sourceNode = firstDestination;
				if(sourceNode == null) return;
				NodeEditorUtility.AddNewNode<Node>(graph.graphData, command.SurroundUnit.GetType(), mousePositionOnCanvas, (node) => {
					node.nodeObject.node = command.SurroundUnit;
					command.SurroundUnit.Register();
					command.unitEnterPort.ConnectTo(sourceNode.FlowInputs[0].connections[0].output);
					command.surroundSource.ConnectTo(sourceNode.FlowInputs[0]);
					command.SurroundUnit.position = new Rect(sourceNode.position.position.x, sourceNode.position.position.y, command.SurroundUnit.position.width, command.SurroundUnit.position.height);
					var lastUnit = graph.nodes.FirstOrDefault(node => graph.graphData.selectedNodes.Contains(node) && node.FlowOutputs.Any(output => output.hasValidConnections && !graph.graphData.selectedNodes.Contains(output.connections[0].input.node))) ?? graph.nodes.FirstOrDefault(node => graph.graphData.selectedNodes.Contains(node) && !node.FlowOutputs.Any(output => output.hasValidConnections));
					if(lastUnit != null && lastUnit.FlowOutputs.Any(output => output.isConnected)) {
						command.surroundExit.ConnectTo(lastUnit.FlowOutputs.First(output => output.hasValidConnections && !graph.graphData.selectedNodes.Contains(output.connections[0].input.node)).connections[0].input);
						lastUnit.FlowOutputs.First(output => output.hasValidConnections && !graph.graphData.selectedNodes.Contains(output.connections[0].input.node)).ClearConnections();
					}
				});
				NodeEditorUtility.PlaceFit.PlaceFitNodes(command.SurroundUnit);
				graph.ReloadView(true);
			}, graph.graphData, mousePosition);
		}

		public override bool IsValid() {
			return graph.graphData.selectedNodes.Count() > 0;
		}
	}
}