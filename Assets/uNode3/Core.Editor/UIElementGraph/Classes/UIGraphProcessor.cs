using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;

namespace MaxyGames.UNode.Editors {
	public abstract class UIGraphProcessor {
		public virtual int order => 0;

		public virtual bool Delete(List<ISelectable> selected) {
			return false;
		}

		public virtual bool PostDelete(List<ISelectable> selected) => false;

		public virtual bool Connect(UGraphView graph, PortView input, PortView output) {
			return false;
		}

		public virtual bool RepaintNode(UGraphView graph, UNodeView view, NodeObject node, bool fullReload) => false;

		public virtual UNodeView InitializeView(UGraphView graph, NodeObject node) => null;

		public virtual EdgeView InitializeEdge(UGraphView graph, EdgeData edgeData) => null;

		public virtual IEnumerable<DropdownMenuItem> ContextMenuForNode(Vector2 mousePosition, UNodeView view) => null;

		/// <summary>
		/// Handle the default port on drop event
		/// </summary>
		/// <param name="graphView"></param>
		/// <param name="edge"></param>
		/// <returns></returns>
		public virtual bool HandlePortOnDrop(UGraphView graphView, EdgeView edge) => false;

		/// <summary>
		/// Handle the default port on drop event
		/// </summary>
		/// <param name="edge"></param>
		/// <param name="position"></param>
		/// <returns></returns>
		public virtual bool HandlePortOnDropOutsidePort(UGraphView graphView, EdgeView edge, Vector2 position) => false;
	}

	class DefaultUIGraphProcessor : UIGraphProcessor {
		public override int order => int.MaxValue;

		public override bool RepaintNode(UGraphView graph, UNodeView view, NodeObject node, bool fullReload) {
			if(node.node is Nodes.NodeValueConverter) {
				var n = node.node as Nodes.NodeValueConverter;
				n.EnsureRegistered();
				if(n.input.isAssigned && !n.input.UseDefaultValue && n.output.hasValidConnections) {
					return true;
				} else {
					node.Destroy();
				}
				return true;
			}
			return false;
		}

		public override EdgeView InitializeEdge(UGraphView graph, EdgeData edgeData) {
			var outNode = edgeData.output.GetNode();
			if(outNode != null) {
				if(outNode is Nodes.NodeValueConverter valueConverter) {
					var type = edgeData.input.GetPortType();
					if(type != typeof(object)) {
						valueConverter.type = type;
					}
					var nView = graph.GetNodeView(valueConverter);
					if(nView != null) {
						valueConverter.position = edgeData.input.owner.GetPosition();
						valueConverter.nodeObject.position.x -= 50;
						nView.HideElement();
						nView.SetDisplay(false);
					}
					if(valueConverter.input.hasValidConnections) {
						return new ConversionEdgeView(valueConverter, new EdgeData(null, edgeData.input, PortUtility.GetPort(valueConverter.input.connections[0].output, graph)));
					}
				}
			}
			if(edgeData.input.GetNode() is Nodes.NodeValueConverter vc && vc.output.isConnected) {
				return null;
			}
			if(graph.graphData.scopes.Contains(StateGraphContainer.Scope)) {
				return new TransitionEdgeView(edgeData);
			}
			return new EdgeView(edgeData);
		}

		public override bool Connect(UGraphView graph, PortView input, PortView output) {
			if(output.isValue) {
				var port = output.GetPortValue<ValueOutput>();
				var outputNode = output.GetNode();
				if(outputNode is MultipurposeNode mNode && port.IsPrimaryPort()) {
					var connecteds = UIElementUtility.Nodes.FindConnectedFlowNodes(output.owner);
					if(connecteds.Contains(input.owner)) {
						if(EditorUtility.DisplayDialog("", "Value can be cached for get better performance\nDo you want to cache the value?", "Yes", "No")) {
							NodeEditorUtility.AddNewNode(graph.graphData, output.owner.GetPosition().position, (Nodes.CacheNode node) => {
								node.Register();
								input.GetPortValue().ConnectTo(node.output);
								node.target.ConnectTo(output.GetPortValue());
								node.exit.ConnectTo(input.GetNodeObject().primaryFlowInput);
								mNode.exit.ClearConnections();
								mNode.nodeObject.position.x = node.position.x - node.position.width - 100;
								foreach(var port in output.owner.inputPorts) {
									if(port.connected && port.isFlow) {
										var edges = port.GetValidEdges();
										foreach(var e in edges) {
											e.Output.GetPortValue().ConnectTo(node.enter);
										}
									}
								}
							});
							return true;
						}
					}
				}

			}
			return false;
		}

		public override bool PostDelete(List<ISelectable> selected) {
			HashSet<UNodeView> nodes = new HashSet<UNodeView>();
			for(int i = 0; i < selected.Count; i++) {
				if(selected[i] is UNodeView) {
					nodes.Add(selected[i] as UNodeView);
				}
			}
			if(nodes.Count > 0) {
				Action action = null;
				foreach(var node in nodes) {
					var inputPort = UIElementUtility.GetPrimaryFlowInput(node);
					var outputPort = UIElementUtility.GetPrimaryFlowOutput(node);
					if(inputPort != null && outputPort != null) {
						//Flow auto re-connection.
						var targetInput = inputPort.GetConnectedPorts();
						var targetOutput = outputPort.GetConnectedPorts().FirstOrDefault();
						if(targetInput.Count > 0 && targetOutput != null) {
							foreach(var p in targetInput) {
								var port = p;
								if(port.GetNodeObject() != targetOutput.GetNodeObject()) {
									action += () => {
										port.owner.owner.Connect(port, targetOutput, true);
									};
								}
							}
						}
					}
					if(node.targetNode is IRerouteNode) {
						//Reroute auto re-connection
						inputPort = node.inputPorts.FirstOrDefault();
						outputPort = node.outputPorts.FirstOrDefault();
						if(inputPort != null && outputPort != null) {
							if(inputPort.isValue && inputPort.IsProxy() == false) {
								var inputEdge = inputPort.GetValidEdges().FirstOrDefault();
								var targetOutput = outputPort.GetConnectedPorts();
								if(targetOutput.Count > 0 && inputEdge != null) {
									foreach(var p in targetOutput) {
										var port = p;
										if(nodes.Contains(port.owner) == false) {
											action += () => {
												if(inputEdge.isProxy) {
													if(inputEdge is ConversionEdgeView) {
														port.GetPortValue().ConnectToAsProxy(((inputEdge as ConversionEdgeView).node).output);
													}
													else {
														port.GetPortValue().ConnectToAsProxy(inputEdge.GetReceiverPort().GetPortValue());
													}
												}
												else {
													if(inputEdge is ConversionEdgeView) {
														port.GetPortValue().ConnectTo(((inputEdge as ConversionEdgeView).node).output);
													}
													else {
														port.GetPortValue().ConnectTo(inputEdge.GetReceiverPort().GetPortValue());
													}
												}
											};
										}
									}
								}
							}
						}
					}
				}
				action?.Invoke();
			}
			return false;
		}

		public override IEnumerable<DropdownMenuItem> ContextMenuForNode(Vector2 mousePosition, UNodeView view) {
			if(view.targetNode is Nodes.StateEntryNode || view.targetNode is Nodes.StateTransition) {
				yield return new DropdownMenuAction("Make Connection", (e) => {
					view.outputPorts.First().SendMakeConnectionEvent();
				}, DropdownMenuAction.AlwaysEnabled);
			}
			yield break;
		}

		public override bool HandlePortOnDropOutsidePort(UGraphView graphView, EdgeView edge, Vector2 position) {
			if(graphView.graphData.scopes.Contains(StateGraphContainer.Scope)) {
				if(edge.isFlow) {
					if(edge.Output != null) {
						NodeEditorUtility.AddNewNode<Nodes.ScriptState>(graphView.graphData, position, node => {
							node.enter.ConnectTo(edge.Output.GetPortValue());
							graphView.graphEditor.Refresh();
						});
					}
				}
				return true;
			}
			return false;
		}
	}
}