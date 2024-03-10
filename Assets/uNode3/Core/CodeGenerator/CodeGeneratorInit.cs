using MaxyGames.UNode;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace MaxyGames {
	public static partial class CG {
		private static List<string> InitData = new List<string>();
		private static void Initialize(out int fieldCount, out int propCount, out int ctorCount) {
			fieldCount = 0;
			propCount = 0;
			ctorCount = 0;
			if(graph != null) {
				List<IGeneratorPrePostInitializer> initializersList = new List<IGeneratorPrePostInitializer>();
				if(graph is IGeneratorPrePostInitializer) {
					initializersList.Add(graph as IGeneratorPrePostInitializer);
				}

				graph.GraphData.ForeachInChildrens(element => {
					if(element is IGeneratorPrePostInitializer initializer) {
						initializersList.Add(initializer);
					} else if(element is NodeObject nodeObject) {
						nodeObject.Register();
						if(nodeObject.node is IGeneratorPrePostInitializer generatorInitializer) {
							initializersList.Add(generatorInitializer);
						}
					}
				}, true);
				for(int i = 0; i < initializersList.Count; i++) {
					initializersList[i].OnPreInitializer();
				}
				if(hasMainGraph) {
					generatorData.eventNodes.AddRange(graph.GraphData.mainGraphContainer.GetNodesInChildren<BaseGraphEvent>());
				}
				InitStartNode();
				generatorData.allNode.AddRange(generatorData.connectedNodes);

				var flowMaps = new List<KeyValuePair<FlowInput, bool>>();
				foreach(var node in generatorData.allNode) {
					if(node == null)
						continue;
					//Make sure the node is registered.
					node.EnsureRegistered();

					bool isInStateGraph = IsInStateGraph(node);
					var inputs = node.FlowInputs;
					foreach(var input in inputs) {
						if (!input.isPrimaryPort || !isInStateGraph || /*setting.enableOptimization &&*/ IsCanBeGrouped(input)) {
							flowMaps.Add(new KeyValuePair<FlowInput, bool>(input, true));
							continue;
						}
						flowMaps.Add(new KeyValuePair<FlowInput, bool>(input, false));
					}
				}
				foreach(var pair in flowMaps) {
					if(pair.Value) {
						generatorData.regularNodes.Add(pair.Key);
					} else {
						generatorData.stateNodes.Add(pair.Key);
					}
				}
				for(int i = 0; i < generatorData.allNode.Count; i++) {
					var node = generatorData.allNode[i];
					if(node == null)
						continue;
					try {
						//Register node pin for custom node.
						node.OnGeneratorInitialize();
					}
					catch(Exception ex) {
						if(ex is GraphException) {
							throw;
						}
						else {
							throw new GraphException(ex, node);
						}
					}
				}
				if(graph is IGraphWithVariables) {
					foreach(var var in graph.GetVariables()) {
						fieldCount++;
						List<AData> attribute = new List<AData>();
						if(var.attributes != null && var.attributes.Count > 0) {
							foreach(var a in var.attributes) {
								attribute.Add(TryParseAttributeData(a));
							}
						}
						generatorData.AddVariable(new VData(var.name, var.type) { modifier = var.modifier, attributes = attribute, reference = var, defaultValue = var.defaultValue });
					}
				}
				if(graph is IGraphWithProperties) {
					//generationState.state = State.Property;
					foreach(var var in graph.GetProperties()) {
						propCount++;
						List<AData> attribute = new List<AData>();
						if(var.attributes != null && var.attributes.Count > 0) {
							foreach(var a in var.attributes) {
								attribute.Add(TryParseAttributeData(a));
							}
						}
						generatorData.properties.Add(new PData(var, attribute) { modifier = var.modifier });
					}
				}
				if(graph is IGraphWithConstructors) {
					//generationState.state = State.Constructor;
					foreach(var var in graph.GetConstructors()) {
						ctorCount++;
						generatorData.constructors.Add(new CData(var) { modifier = var.modifier });
					}
				}

				for(int i=0;i< initializersList.Count;i++) {
					initializersList[i].OnPostInitializer();
				}
				generatorData.postInitialization?.Invoke();
			}
		}

		/// <summary>
		/// Call only on pre initialization phrase using <see cref="IGeneratorPrePostInitializer"/>
		/// </summary>
		/// <param name="entry"></param>
		public static void RegisterEntry(NodeObject entry) {
			InitConnect(entry);
		}

		/// <summary>
		/// Call only on pre initialization phrase using <see cref="IGeneratorPrePostInitializer"/>.
		/// </summary>
		/// <remarks>
		/// Register nested graph initializations
		/// </remarks>
		/// <param name="graph"></param>
		public static void RegisterNestedGraph(Graph graph) {
			List<IGeneratorPrePostInitializer> initializersList = new List<IGeneratorPrePostInitializer>();

			graph.ForeachInChildrens(element => {
				if(element is IGeneratorPrePostInitializer initializer) {
					initializersList.Add(initializer);
				}
				else if(element is NodeObject nodeObject) {
					nodeObject.Register();
					if(nodeObject.node is IGeneratorPrePostInitializer generatorInitializer) {
						initializersList.Add(generatorInitializer);
					}
				}
			}, true);
			for(int i = 0; i < initializersList.Count; i++) {
				initializersList[i].OnPreInitializer();
			}
			CG.RegisterPostInitialization(() => {
				for(int i = 0; i < initializersList.Count; i++) {
					initializersList[i].OnPostInitializer();
				}
			});
		}

		private static void InitStartNode() {
			foreach(var function in graph.GetFunctions()) {
				InitConnect(function.Entry);
			}
			foreach(var property in graph.GetProperties()) {
				if(property != null && !property.AutoProperty) {
					if(property.getRoot != null) {
						InitConnect(property.getRoot.Entry);
					}
					if(property.setRoot != null) {
						InitConnect(property.setRoot.Entry);
					}
				}
			}
			foreach(var ctor in graph.GetConstructors()) {
				InitConnect(ctor.Entry);
			}
			if(hasMainGraph) {
				foreach(var eventNode in graph.GraphData.mainGraphContainer.GetNodesInChildren<BaseGraphEvent>()) {
					try {
						InitConnect(eventNode);
						foreach(var flow in eventNode.outputs) {
							var tFlow = flow?.GetTargetFlow();
							if(tFlow != null) {
								if(Nodes.HasStateFlowOutput(tFlow) || Nodes.IsStackOverflow(tFlow.node)) {
									RegisterAsStateFlow(tFlow);
									break;
								}
							}
						}
					} catch(Exception ex) {
						var node = eventNode.nodeObject;
						UnityEngine.Debug.LogError(
							"Error from node:" + node.name + " |Type:" + node.node.GetType() +
							"\nRef: " + GraphException.GetMessage(node) +
							"\nFrom graph:" + node.graphContainer.GetGraphName() +
							"\nError:" + ex.ToString(), node.graphContainer as UnityEngine.Object);
						throw;
					}
				}
			}
		}

		public static class Nodes {
			/// <summary>
			/// Is the node is stack overflowed?
			/// </summary>
			/// <param name="node"></param>
			/// <returns></returns>
			public static bool IsStackOverflow(NodeObject node) {
				if(isGenerating) {
					if(!generatorData.stackOverflowMap.TryGetValue(node, out var result)) {
						result = IsRecusive(node);
						generatorData.stackOverflowMap[node] = result;
					}
					return result;
				}
				return IsRecusive(node);
			}

			public static HashSet<NodeObject> FindAllConnections(NodeObject node,
				bool includeFlowOutput = false,
				bool includeValueInput = false,
				bool includeFlowInput = false,
				bool includeValueOutput = false,
				bool includeProxyConnections = true) {
				var result = new HashSet<NodeObject>();
				FindAllConnections(node, ref result, includeFlowOutput, includeValueInput, includeFlowInput, includeValueOutput, includeProxyConnections);
				return result;
			}

			public static void FindAllConnections(NodeObject node,
				ref HashSet<NodeObject> connections,
				bool includeFlowOutput = true,
				bool includeValueInput = true,
				bool includeFlowInput = false,
				bool includeValueOutput = false,
				bool includeProxyConnections = true) {
				if(node != null && connections.Add(node)) {
					if(includeFlowOutput) {
						for(int i = 0; i < node.FlowOutputs.Count; i++) {
							var p = node.FlowOutputs[i];
							foreach(var c in p.connections) {
								if(c.input.isValid) {
									if(includeProxyConnections == false && c.isProxy) continue;
									FindAllConnections(c.input.node, ref connections, includeFlowOutput, includeValueInput, includeFlowInput, includeValueOutput, includeProxyConnections);
								}
							}
						}
					}
					if(includeValueInput) {
						for(int i = 0; i < node.ValueInputs.Count; i++) {
							var p = node.ValueInputs[i];
							foreach(var c in p.connections) {
								if(c.output.isValid) {
									if(includeProxyConnections == false && c.isProxy) continue;
									FindAllConnections(c.output.node, ref connections, includeFlowOutput, includeValueInput, includeFlowInput, includeValueOutput, includeProxyConnections);
								}
							}
						}
					}
					if(includeFlowInput) {
						for(int i = 0; i < node.FlowInputs.Count; i++) {
							var p = node.FlowInputs[i];
							foreach(var c in p.connections) {
								if(c.output.isValid) {
									if(includeProxyConnections == false && c.isProxy) continue;
									FindAllConnections(c.output.node, ref connections, includeFlowOutput, includeValueInput, includeFlowInput, includeValueOutput, includeProxyConnections);
								}
							}
						}
					}
					if(includeValueOutput) {
						for(int i = 0; i < node.ValueOutputs.Count; i++) {
							var p = node.ValueOutputs[i];
							foreach (var c in p.connections) {
								if (c.input.isValid) {
									if(includeProxyConnections == false && c.isProxy) continue;
									FindAllConnections(c.input.node, ref connections, includeFlowOutput, includeValueInput, includeFlowInput, includeValueOutput, includeProxyConnections);
								}
							}
						}
					}
				}
			}

			private static bool IsRecusive(NodeObject original, NodeObject current = null, HashSet<NodeObject> prevs = null) {
				if(current == null)
					current = original;
				if(prevs == null)
					prevs = new HashSet<NodeObject>();
				if(prevs.Contains(current)) {
					return false;
				}
				prevs.Add(current);
				for(int i = 0; i < current.FlowOutputs.Count; i++) {
					if (current.FlowOutputs[i].isConnected) {
						var flow = current.FlowOutputs[i].GetTargetNode();
						if (flow == original || IsRecusive(original, flow, prevs)) {
							return true;
						}
					}
				}
				return false;
			}

			/// <summary>
			/// Is the node has connected to a state flow in node output flow ports.
			/// </summary>
			/// <param name="node"></param>
			/// <param name="prevs"></param>
			/// <returns></returns>
			public static bool HasStateFlowOutput(FlowInput port, HashSet<NodeObject> prevs = null) {
				if(port == null)
					return false;
				if (IsStateFlow(port))
					return true;
				if (prevs == null)
					prevs = new HashSet<NodeObject>();
				var node = port.node;
				if (prevs.Contains(node)) {
					return false;
				}
				prevs.Add(node);
				for (int i = 0; i < node.FlowOutputs.Count; i++) {
					if (node.FlowOutputs[i].isConnected) {
						if (HasStateFlowOutput(node.FlowOutputs[i].GetTargetFlow(), prevs)) {
							return true;
						}
					}
				}
				return false;
			}

			/// <summary>
			/// Is the node has connected to a state flow in node input flow ports.
			/// </summary>
			/// <param name="node"></param>
			/// <param name="prevs"></param>
			/// <returns></returns>
			public static bool HasStateFlowInput(NodeObject node, HashSet<NodeObject> prevs = null) {
				if(IsStateFlow(node.primaryFlowInput))
					return true;
				if(prevs == null)
					prevs = new HashSet<NodeObject>();
				if(prevs.Contains(node)) {
					return false;
				}
				prevs.Add(node);
				for(int i = 0; i < node.FlowInputs.Count; i++) {
					if (node.FlowInputs[i].isConnected) {
						foreach(var port in node.FlowInputs[i].GetConnectedPorts()) {
							if (HasStateFlowInput(port.node, prevs)) {
								return true;
							}
						}
					}
				}
				return false;
			}

			#region GetFlowConnection
			/// <summary>
			/// Find all node connection include first node.
			/// </summary>
			/// <param name="node"></param>
			/// <param name="allNode"></param>
			/// <param name="includeSuperNode"></param>
			internal static void FindAllFlowConnection(NodeObject node, ref HashSet<NodeObject> allNode, bool includeSuperNode = true) {
				if(node != null && !allNode.Contains(node)) {
					allNode.Add(node);
					foreach (var port in node.FlowOutputs) {
						if (port.isConnected) {
							FindAllFlowConnection(port.GetTargetNode(), ref allNode, includeSuperNode);
						}
					}
					if (includeSuperNode && node.node is ISuperNode) {
						ISuperNode superNode = node.node as ISuperNode;
						foreach(var n in superNode.nestedFlowNodes) {
							FindAllFlowConnection(n, ref allNode, includeSuperNode);
						}
					}
				}
			}

			/// <summary>
			/// Find all node connection after coroutine node.
			/// </summary>
			/// <param name="node"></param>
			/// <param name="allNode"></param>
			/// <param name="includeSuperNode"></param>
			/// <param name="includeCoroutineEvent"></param>
			internal static void FindFlowConnectionAfterCoroutineNode(NodeObject node, ref HashSet<NodeObject> allNode,
				bool includeSuperNode = true,
				bool includeCoroutineEvent = true,
				bool passCoroutine = false) {
				if(node != null && !allNode.Contains(node)) {
					bool isCoroutineNode = node.primaryFlowInput.IsSelfCoroutine();
					if(!passCoroutine && isCoroutineNode) {
						passCoroutine = true;
					}
					if(passCoroutine && (!isCoroutineNode || includeCoroutineEvent)) {
						allNode.Add(node);
					}
					foreach (var port in node.FlowOutputs) {
						if (port.isConnected) {
							FindFlowConnectionAfterCoroutineNode(port.GetTargetNode(), ref allNode, includeSuperNode, includeCoroutineEvent, passCoroutine);
						}
					}
					if (includeSuperNode && node.node is ISuperNode) {
						ISuperNode superNode = node.node as ISuperNode;
						foreach(var n in superNode.nestedFlowNodes) {
							FindFlowConnectionAfterCoroutineNode(n, ref allNode, includeSuperNode, includeCoroutineEvent, passCoroutine);
						}
					}
				}
			}
			#endregion
		}

		private static void InitConnect(NodeObject node) {
			if(node != null && generatorData.connectedNodes.Add(node)) {
				//Ensure the node is registered.
				node.EnsureRegistered();

				if(IsInStateGraph(node)) {
					foreach(var input in node.FlowInputs) {
						if(input.IsSelfCoroutine()) {
							RegisterAsStateFlow(input);
						}
					}
				}
				foreach (var port in node.FlowOutputs) {
					if (port.isAssigned) {
						InitConnect(port.GetTargetNode());
					}
				}
				foreach (var port in node.ValueInputs) {
					if (port.isAssigned) {
						InitConnect(port.GetTargetNode());
					}
				}
				if (node.node is ISuperNode) {
					ISuperNode superNode = node.node as ISuperNode;
					foreach(var n in superNode.nestedFlowNodes) {
						if(n != null) {
							InitConnect(n);
						}
					}
				}
				if(node.node is UNode.Nodes.StateNode) {
					UNode.Nodes.StateNode stateNode = node.node as UNode.Nodes.StateNode;
					foreach(var tr in stateNode.GetTransitions()) {
						if(tr != null) {
							InitConnect(tr);
						}
					}
				}
				if(node.node is IStackedNode) {
					IStackedNode stackedNode = node.node as IStackedNode;
					foreach(var n in stackedNode.stackedNodes) {
						if(n != null) {
							InitConnect(n);
						}
					}
				}
				if(node.node is IMacro) {
					var macro = node.node as IMacro;
					{
						var ports = macro.InputFlows;
						if(ports != null) {
							foreach(var port in ports) {
								if(port != null && port.enter.isConnected) {
									InitConnect(port.exit.GetTargetNode());
								}
							}
						}
					}
					{
						var ports = macro.OutputFlows;
						if (ports != null) {
							foreach (var port in ports) {
								if (port != null && port.exit.isConnected) {
									foreach(var c in port.enter.GetConnectedPorts()) {
										if(c.isValid) {
											InitConnect(c.node);
										}
									}
								}
							}
						}
					}
					{
						var ports = macro.OutputValues;
						if (ports != null) {
							foreach (var port in ports) {
								if (port != null && port.output.isConnected) {
									InitConnect(port.input.GetTargetNode());
								}
							}
						}
					}
					{
						var ports = macro.InputValues;
						if (ports != null) {
							foreach (var port in ports) {
								if (port != null && port.input.isConnected) {
									foreach (var c in port.output.GetConnectedPorts()) {
										if (c.isValid) {
											InitConnect(c.node);
										}
									}
								}
							}
						}
					}
				}
			}
		}

		private static HashSet<NodeObject> InitConnections(NodeObject node) {
			var allNodes = new HashSet<NodeObject>();
			if(node.node is UNode.Nodes.StateNode) {
				var eventNode = node.node as UNode.Nodes.StateNode;
				var TE = eventNode.GetTransitions();
				foreach(var transition in TE) {
					var tNode = transition.exit.GetTargetNode();
					if(tNode != null) {
						allNodes.Add(tNode);
					}
				}
			} else if(node.node is BaseEventNode) {
				var stateEvent = node.node as BaseEventNode;
				foreach(var port in stateEvent.outputs) {
					var tNode = port.GetTargetNode();
					if(tNode != null) {
						allNodes.Add(tNode);
					}
				}
			}

			return allNodes;
		}
	}
}