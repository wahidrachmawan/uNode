using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;
using Object = UnityEngine.Object;

namespace MaxyGames.UNode.Editors {
	public partial class UGraphView {
		[InitializeOnLoadMethod()]
		static void Init() {
			uNodeEditor.onClearCache -= ClearCache;
			uNodeEditor.onClearCache += ClearCache;
		}

		public Event currentEvent;
		public void IMGUIEvent(Event @event) {
			currentEvent = @event;
			switch(@event.type) {
				case EventType.DragUpdated:
					OnDragUpdatedEvent(MouseEventBase<DragUpdatedEvent>.GetPooled(@event));
					break;
				case EventType.DragPerform:
					OnDragPerformEvent(MouseEventBase<DragPerformEvent>.GetPooled(@event));
					break;
			}
		}

		public void UpdatePosition() {
			//UpdateViewTransform(graphData.position * -graph.zoomScale, new Vector3(graph.zoomScale, graph.zoomScale, 1));
			UpdateViewTransform(graphData.position * -1, new Vector3(1, 1, 1));
			SetZoomScale(graph.zoomScale);
		}

		public void UpdatePosition(Vector2 position) {
			UpdatePosition(position, scale);
			//contentViewContainer.MarkDirtyRepaint();
		}

		public void UpdatePosition(Vector3 position, float zoomScale) {
			UpdateViewTransform(-(position * zoomScale), new Vector3(zoomScale, zoomScale, 1));
			//contentViewContainer.MarkDirtyRepaint();
		}

		#region Graph Functions
		public Dictionary<NodeObject, PortView> portFlowNodeAliases = new Dictionary<NodeObject, PortView>(EqualityComparer<NodeObject>.Default);
		public void RegisterFlowPortAliases(PortView port, NodeObject targetAlias) {
			portFlowNodeAliases[targetAlias] = port;
		}

		public Dictionary<NodeObject, PortView> portValueNodeAliases = new Dictionary<NodeObject, PortView>(EqualityComparer<NodeObject>.Default);
		public void RegisterValuePortAliases(PortView port, NodeObject targetAlias) {
			portValueNodeAliases[targetAlias] = port;
		}


		/// <summary>
		/// Set graph zoom scales
		/// </summary>
		/// <param name="zoomScale"></param>
		public void SetZoomScale(float zoomScale) {
			if(scale != zoomScale && graph != null) {
				UpdatePosition(graphData.position, zoomScale);
			}
		}

		/// <summary>
		/// Set graph zoom scales
		/// </summary>
		/// <param name="zoomScale"></param>
		/// <param name="cachedPixel"></param>
		public void SetZoomScale(float zoomScale, bool cachedPixel) {
			if(scale != zoomScale && graph != null) {
				UpdatePosition(graphData.position, zoomScale);
			}
			SetPixelCachedOnBoundChanged(cachedPixel);
		}

		/// <summary>
		/// Toogle minimap on / off
		/// </summary>
		/// <param name="value"></param>
		public void ToggleMinimap(bool value) {
			if(value) {
				if(miniMap == null) {
					miniMap = new MinimapView();
				}
				if(miniMap.parent == null) {
					Add(miniMap);
				}
			} else {
				if(miniMap != null && miniMap.parent != null) {
					miniMap.RemoveFromHierarchy();
				}
			}
		}

		private void ToogleGrid(bool enable) {
			if(enable) {
				if(gridBackground != null) {
					gridBackground.RemoveFromHierarchy();
					gridBackground = null;
				}
				var grid = new GridBackground();
				Insert(0, grid);
				grid.StretchToParentSize();
				grid.style.left = 1;
				gridBackground = grid;
			} else if(gridBackground != null) {
				gridBackground.RemoveFromHierarchy();
				gridBackground = null;
			}
		}

		public Vector2 GetTopMousePosition(Vector2 mPos) {
			var screenRect = graph.window.GetMousePositionForMenu(mPos);
			Vector2 position = graph.window.rootVisualElement.ChangeCoordinatesTo(
				graph.window.rootVisualElement.parent,
				screenRect - graph.window.position.position);
			return position;
		}

		public Vector2 GetMousePosition(Vector2 mPos) {
			var position = GetTopMousePosition(mPos);
			var result = contentViewContainer.WorldToLocal(position);
			//result.x -= contentContainer.layout.width;//for right node aligment
			return result;
		}

		public Vector2 GetMousePosition(Vector2 mPos, out Vector2 topMousePosition) {
			topMousePosition = GetTopMousePosition(mPos);
			var result = contentViewContainer.WorldToLocal(topMousePosition);
			//result.x -= contentContainer.layout.width;//for right node aligment
			return result;
		}

		public Vector2 GetTopMousePosition(IMouseEvent evt) {
			var screenRect = graph.window.GetMousePositionForMenu(evt.mousePosition);
			Vector2 position = graph.window.rootVisualElement.ChangeCoordinatesTo(
				graph.window.rootVisualElement.parent,
				screenRect - graph.window.position.position);
			return position;
		}

		public Vector2 GetScreenMousePosition(IMouseEvent evt) {
			return graph.window.GetMousePositionForMenu(evt.mousePosition);
		}

		public Vector2 GetScreenMousePosition(Vector2 mousePosition) {
			return graph.window.GetMousePositionForMenu(mousePosition);
		}

		public Vector2 GetMousePosition(IMouseEvent evt) {
			var position = GetTopMousePosition(evt);
			var result = contentViewContainer.WorldToLocal(position);
			//result.x -= contentContainer.layout.width;//for right node aligment
			return result;
		}

		public Vector2 GetMousePosition(IMouseEvent evt, out Vector2 topMousePosition) {
			topMousePosition = GetTopMousePosition(evt);
			var result = contentViewContainer.WorldToLocal(topMousePosition);
			//result.x -= contentContainer.layout.width;//for right node aligment
			return result;
		}

		public UNodeView AddNodeView(NodeObject node, Type defaultView = null) {
			if(node is null) {
				throw new ArgumentNullException(nameof(node));
			}

			var viewType = node.node != null ? UIElementUtility.GetNodeViewTypeFromType(node.node.GetType()) : null;

			if(viewType == null) {
				if(defaultView == null) {
					viewType = typeof(BaseNodeView);
				} else {
					viewType = defaultView;
				}
			}

			var nodeView = Activator.CreateInstance(viewType) as UNodeView;
			nodeView.Initialize(this, node);
			AddElement(nodeView);

			nodeViews.Add(nodeView);
			nodeViewsPerNode[node] = nodeView;
			cachedNodeMap[node] = nodeView;

			return nodeView;
		}

		public UNodeView GetNodeView(NodeObject node) {
			if(node == null)
				return null;
			UNodeView n;
			if(nodeViewsPerNode.TryGetValue(node, out n)) {
				return n;
			}
			return n;
		}

		public List<UNodeView> GetNodeViews(IEnumerable<NodeObject> nodes) {
			List<UNodeView> views = new List<UNodeView>();
			if(nodes == null)
				return views;
			foreach(var node in nodes) {
				if(node == null)
					continue;
				if(nodeViewsPerNode.TryGetValue(node, out var view)) {
					views.Add(view);
				}
			}
			return views;
		}


		private static void OnPortConnected(PortView input, PortView output) {
			if(input.isFlow == false) {
				var node = input.GetNodeObject();
				node.ValidatePorts();
			}
		}

		public void Connect(PortView input, PortView output, bool serialize) {
			var data = new EdgeData(Connection.Create(input.GetPortValue(), output.GetPortValue()), input, output);
			Connect(new EdgeView(data), serialize);
		}

		public void Connect(EdgeView e, bool serialize) {
			if(e.input == null || e.output == null)
				return;
			AddElement(e);

			e.input.Connect(e);
			e.output.Connect(e);

			var inputNodeView = e.input.node;
			var outputNodeView = e.output.node;

			if(inputNodeView == null || outputNodeView == null) {
				edgeViews.Add(e);//Register edge so it will be removed in the next frame
				Debug.LogError("Connect aborted !");
				return;
			}

			if(serialize) {
				var inPort = e.input as PortView;
				var outPort = e.output as PortView;
				if(outPort.isFlow) {
					//TODO: fix this
					//var inNode = inPort.GetNode();
					//var outNode = outPort.GetNode();
					//if(!graphData.supportCoroutine && inNode != null && outNode != null && uNodeUtility.IsStackOverflow(inNode, outNode)) {
					//	uNodeEditorUtility.DisplayErrorMessage("Cannot connect because of Stackoverflow / Recursive connections");
					//	MarkRepaint(true);
					//	edgeViews.Add(e);
					//	return;
					//}
				}
				if(e.isFlow) {
					foreach(var edge in outPort.GetValidEdges()) {
						edge.Disconnect();
					}
				}
				else {
					foreach(var edge in inPort.GetValidEdges()) {
						edge.Disconnect();
					}
				}
				var processor = GraphProcessor;
				foreach(var p in processor) {
					if(p.Connect(this, inPort, outPort)) {
						MarkRepaint(true);
						//This for ensure the created edge is removed
						edgeViews.Add(e);

						OnPortConnected(inPort, outPort);
						return;
					}
				}
				e.connection = Connection.CreateAndConnect(inPort.GetPortValue(), outPort.GetPortValue());
				//if(e.connection != null && e.connection.isValid) {
				//	e.connection.Connect();
				//} else {
				//}
				if(uNodePreference.preferenceData.autoCreateReroute && inPort.GetNode() is not Nodes.NodeReroute) {
					if(graphLayout == GraphLayout.Vertical) {
						var con = e.connection;
						if(!con.isProxy && con is ValueConnection valueConnection) {
							if(outPort.GetGlobalCenter().x > inPort.GetGlobalCenter().x) {
								var inPortPosition = inPort.ChangeCoordinatesTo(this.contentViewContainer, Vector2.zero);
								NodeEditorUtility.AddNewNode(graph.graphData,
									new Vector2(
										inPortPosition.x - 100,
										inPortPosition.y),
									(Nodes.NodeReroute n) => {
										n.kind = Nodes.NodeReroute.RerouteKind.Value;
										n.Register();
										n.input.ConnectTo(con.Output);
										con.Input.ConnectTo(n.output);
										if(uNodePreference.preferenceData.autoProxyConnection) {
											n.input.connections[0].isProxy = true;
										}
									}
								);
								MarkRepaint(true);
								//This for ensure the created edge is removed
								edgeViews.Add(e);

								OnPortConnected(inPort, outPort);
								return;
							}
						}
					}
				}
				else if(uNodePreference.preferenceData.autoProxyConnection) {
					if(graphLayout == GraphLayout.Vertical) {
						var con = e.connection;
						if(!con.isProxy && con is ValueConnection valueConnection) {
							if(outPort.GetGlobalCenter().x > inPort.GetGlobalCenter().x) {
								var inPortPosition = inPort.ChangeCoordinatesTo(this.contentViewContainer, Vector2.zero);
								valueConnection.isProxy = true;
							}
						}
					}
				}
				uNodeGUIUtility.GUIChanged(graphData.owner);
				//Code below will ensure that the port are up to date
				if(inputNodeView is UNodeView inputNode) {
					inputNode.MarkRepaint();
				}
				if(outputNodeView is UNodeView outputNode) {
					outputNode.MarkRepaint();
				}
				MarkRepaint(inPort.GetEdgeOwners());
				MarkRepaint(outPort.GetEdgeOwners());
				//MarkRepaint();
				OnPortConnected(inPort, outPort);
			}
			edgeViews.Add(e);
			inputNodeView.RefreshPorts();
			outputNodeView.RefreshPorts();
		}

		public void Disconnect(EdgeView e, bool serialize = true) {
			RemoveElement(e);

			if(serialize) {
				var inPort = e.input as PortView;
				var outPort = e.output as PortView;
				if(inPort.isValue) {
					inPort.ResetPortValue();
				} else if(outPort.isValue) {
					outPort.ResetPortValue();
				}
				//Code below will ensure that the port are up to date
				MarkRepaint(inPort.GetEdgeOwners());
				MarkRepaint(outPort.GetEdgeOwners());
				// if(inPort.owner != null) {
				// 	inPort.owner.MarkRepaint();
				// }
				// if(outPort.owner != null) {
				// 	outPort.owner.MarkRepaint();
				// }
				inPort.owner?.OnPortDisconnected(inPort);
				outPort.owner?.OnPortDisconnected(outPort);
			}

			if(e?.input?.node != null) {
				e.input.Disconnect(e);
				var inputNodeView = e.input.node as UNodeView;
				if(inputNodeView != null) {
					inputNodeView.RefreshPorts();
				}
			}
			if(e?.output?.node != null) {
				e.output.Disconnect(e);
				var outputNodeView = e.output.node as UNodeView;
				if(outputNodeView != null) {
					outputNodeView.RefreshPorts();
				}
			}
		}
		#endregion

		#region Statics
		/// <summary>
		/// Clear the graph cached data so the graph will have fresh datas
		/// </summary>
		public static void ClearCache() {
			if(uNodeEditor.window != null && uNodeEditor.window.graphEditor is UIElementGraph graph) {
				if(graph != null && graph.graphView != null) {
					graph.graphView.cachedNodeMap.Clear();
					//uNodeEditor.window?.Refresh(true);
				}
			}
		}

		/// <summary>
		/// Clear the specific graph cached data so the graph will have fresh datas
		/// </summary>
		/// <param name="graphRef"></param>
		public static void ClearCache(IGraph graphRef) {
			if(uNodeEditor.window != null && uNodeEditor.window.graphEditor is UIElementGraph graph) {
				if(graph != null && graph.graphView != null) {
					List<NodeObject> keys = new List<NodeObject>();
					foreach(var pair in graph.graphView.cachedNodeMap) {
						if(pair.Key != null && pair.Key.graphContainer == graphRef) {
							keys.Add(pair.Key);
						}
					}
					foreach(var key in keys) {
						graph.graphView.cachedNodeMap.Remove(key);
					}
					if(graph.graphView.graphData?.graph == graphRef) {
						graph.graphView.FullReload();
					}
				}
			}
		}

		public static void ClearCache(NodeObject nodeRef) {
			if(uNodeEditor.window != null && uNodeEditor.window.graphEditor is UIElementGraph graph) {
				if(graph != null && graph.graphView != null) {
					graph.graphView.cachedNodeMap.Remove(nodeRef);
					if(graph.graphView.graphData?.graphData == nodeRef.graph) {
						graph.graphView.FullReload();
					}
				}
			}
		}
		#endregion

		#region Highlight
		protected bool animateFrame = true;
		[NonSerialized]
		public List<UNodeView> searchedNodes = new List<UNodeView>();
		[NonSerialized]
		public UNodeView[] highlightedNodes = new UNodeView[0];
		private int frameSearchIndex = 0;
		private int currentAnimateFrame = 0;

		public void OnSearchNext() {
			frameSearchIndex++;
			if(frameSearchIndex >= searchedNodes.Count) {
				frameSearchIndex = 0;
			}
			if(searchedNodes.Count > 0) {
				Frame(new UNodeView[] { searchedNodes[frameSearchIndex] });
			}
		}

		public void OnSearchPrev() {
			frameSearchIndex--;
			if(frameSearchIndex < 0) {
				frameSearchIndex = searchedNodes.Count - 1;
			}
			if(searchedNodes.Count > 0) {
				Frame(new UNodeView[] { searchedNodes[frameSearchIndex] });
			}
		}

		public void OnSearchChanged(GraphSearchQuery query) {
			List<UNodeView> matched = new List<UNodeView>();
			searchedNodes = matched;
			frameSearchIndex = 0;
			switch(query.type) {
				case GraphSearchQuery.SearchType.Node: {
					foreach(var n in nodeViews) {
						string title = n.GetTitle();
						List<string> strs = new List<string>();
						string str = "";
						for(int i = 0; i < title.Length; i++) {
							char c = title[i];
							if(!string.IsNullOrEmpty(str)) {
								if(char.IsUpper(c) || !char.IsLetter(c) && c != '$') {
									strs.Add(str.ToLower());
									str = "";
								}
							}
							str += c;
							if(i + 1 == title.Length && !string.IsNullOrEmpty(str)) {
								strs.Add(str.ToLower());
							}
						}
						if(strs.Count > 0) {
							foreach(var q in query.query) {
								foreach(var s in strs) {
									if(s.StartsWith(q, StringComparison.OrdinalIgnoreCase)) {
										matched.Add(n);
										goto VALID;
									}
								}
							}
						}
					VALID:
						continue;
					}
					break;
				}
				case GraphSearchQuery.SearchType.NodeType: {
					List<string> types = new List<string>();
					foreach(var q in query.query) {
						string str = q.ToLower();
						if("event".StartsWith(str, StringComparison.Ordinal)) {
							types.Add("event");
						}
						if("variable".StartsWith(str, StringComparison.Ordinal)) {
							types.Add("variable");
						}
						if("field".StartsWith(str, StringComparison.Ordinal)) {
							types.Add("field");
						}
						if("property".StartsWith(str, StringComparison.Ordinal)) {
							types.Add("property");
						}
						if("function".StartsWith(str, StringComparison.Ordinal) || "method".StartsWith(str, StringComparison.Ordinal)) {
							types.Add("function");
						}
						if("constructor".StartsWith(str, StringComparison.Ordinal)) {
							types.Add("constructor");
						}
						if("literal".StartsWith(str, StringComparison.Ordinal)) {
							types.Add("literal");
						}
						if("value".StartsWith(str, StringComparison.Ordinal)) {
							types.Add("value");
						}
						if("proxy".StartsWith(str, StringComparison.Ordinal)) {
							types.Add("proxy");
						}
						if("block".StartsWith(str, StringComparison.Ordinal)) {
							types.Add("block");
						}
					}
					types = types.Distinct().ToList();
					foreach(var n in nodeViews) {
						foreach(var t in types) {
							switch(t) {
								case "event":
									//if(n.targetNode is BaseGraphEvent) {
									//	matched.Add(n);
									//	goto VALID;
									//}
									break;
								case "variable":
									//if(n.targetNode is MultipurposeNode) {
									//	var node = n.targetNode as MultipurposeNode;
									//	var tType = node.target.target.targetType;
									//	if(tType == MemberData.TargetType.uNodeVariable ||
									//		tType == MemberData.TargetType.uNodeGroupVariable ||
									//		tType == MemberData.TargetType.uNodeLocalVariable ||
									//		tType == MemberData.TargetType.Field) {
									//		matched.Add(n);
									//		goto VALID;
									//	}
									//}
									break;
								case "property":
									//if(n.targetNode is MultipurposeNode) {
									//	var node = n.targetNode as MultipurposeNode;
									//	var tType = node.target.target.targetType;
									//	if(tType == MemberData.TargetType.Property ||
									//		tType == MemberData.TargetType.uNodeProperty) {
									//		matched.Add(n);
									//		goto VALID;
									//	}
									//}
									break;
								case "function":
									//if(n.targetNode is MultipurposeNode) {
									//	var node = n.targetNode as MultipurposeNode;
									//	var tType = node.target.target.targetType;
									//	if(tType == MemberData.TargetType.uNodeFunction ||
									//		tType == MemberData.TargetType.Method) {
									//		matched.Add(n);
									//		goto VALID;
									//	}
									//}
									break;
								case "constructor":
									//if(n.targetNode is MultipurposeNode) {
									//	var node = n.targetNode as MultipurposeNode;
									//	var tType = node.target.target.targetType;
									//	if(tType == MemberData.TargetType.Constructor) {
									//		matched.Add(n);
									//		goto VALID;
									//	}
									//}
									break;
								case "literal":
									//if(n.targetNode is MultipurposeNode) {
									//	var node = n.targetNode as MultipurposeNode;
									//	var tType = node.target.target.targetType;
									//	if(tType == MemberData.TargetType.Values) {
									//		Type type = node.target.target.type;
									//		if(type != null && type.IsPrimitive || type == typeof(string)) {
									//			matched.Add(n);
									//			goto VALID;
									//		}
									//	}
									//}
									break;
								case "value":
									//if(n.targetNode is MultipurposeNode) {
									//	var node = n.targetNode as MultipurposeNode;
									//	var tType = node.target.target.targetType;
									//	if(tType == MemberData.TargetType.Values) {
									//		matched.Add(n);
									//		goto VALID;
									//	}
									//}
									break;
								case "proxy":
									var ports = n.inputPorts;
									ports.AddRange(n.outputPorts);
									foreach(var p in ports) {
										if(p.IsProxy()) {
											matched.Add(n);
											goto VALID;
										}
									}
									break;
							}
						}
					VALID:
						continue;
					}
					break;
				}
				case GraphSearchQuery.SearchType.Port: {
					foreach(var n in nodeViews) {
						var ports = n.inputPorts;
						ports.AddRange(n.outputPorts);
						foreach(var p in ports) {
							string str = p.portName.ToLower();
							foreach(var t in query.query) {
								if(str.StartsWith(t, StringComparison.OrdinalIgnoreCase)) {
									matched.Add(n);
									goto VALID;
								} else if(p.isValue && p.direction == Direction.Input && GraphSearchQuery.csharpKeyword.Contains(t)) {
									switch(t.ToLower()) {
										case "false": {
											object val = p.GetDefaultValue();
											if(val is bool && (bool)val == false || val is IGetValue) {
												matched.Add(n);
												goto VALID;
											} else if(val is IGetValue) {
												var obj = (val as IGetValue).Get();
												if(obj is bool && (bool)obj == false) {
													matched.Add(n);
													goto VALID;
												}
											}
											break;
										}
										case "true": {
											object val = p.GetDefaultValue();
											if(val is bool && (bool)val == true || val is IGetValue) {
												matched.Add(n);
												goto VALID;
											} else if(val is IGetValue) {
												var obj = (val as IGetValue).Get();
												if(obj is bool && (bool)obj == true) {
													matched.Add(n);
													goto VALID;
												}
											}
											break;
										}
										case "null": {
											object val = p.GetDefaultValue();
											if(val == null) {
												matched.Add(n);
												goto VALID;
											} else if(val is IGetValue) {
												var obj = (val as IGetValue).Get();
												if(obj == null) {
													matched.Add(n);
													goto VALID;
												}
											}
											break;
										}
										case "bool":
											if(p.GetPortType() == typeof(bool)) {
												matched.Add(n);
												goto VALID;
											}
											break;
										case "byte":
											if(p.GetPortType() == typeof(byte)) {
												matched.Add(n);
												goto VALID;
											}
											break;
										case "char":
											if(p.GetPortType() == typeof(bool)) {
												matched.Add(n);
												goto VALID;
											}
											break;
										case "decimal":
											if(p.GetPortType() == typeof(decimal)) {
												matched.Add(n);
												goto VALID;
											}
											break;
										case "double":
											if(p.GetPortType() == typeof(double)) {
												matched.Add(n);
												goto VALID;
											}
											break;
										case "float":
											if(p.GetPortType() == typeof(float)) {
												matched.Add(n);
												goto VALID;
											}
											break;
										case "int":
											if(p.GetPortType() == typeof(int)) {
												matched.Add(n);
												goto VALID;
											}
											break;
										case "long":
											if(p.GetPortType() == typeof(long)) {
												matched.Add(n);
												goto VALID;
											}
											break;
										case "object":
											if(p.GetPortType() == typeof(object)) {
												matched.Add(n);
												goto VALID;
											}
											break;
										case "sbyte":
											if(p.GetPortType() == typeof(sbyte)) {
												matched.Add(n);
												goto VALID;
											}
											break;
										case "short":
											if(p.GetPortType() == typeof(short)) {
												matched.Add(n);
												goto VALID;
											}
											break;
										case "string":
											if(p.GetPortType() == typeof(string)) {
												matched.Add(n);
												goto VALID;
											}
											break;
										case "uint":
											if(p.GetPortType() == typeof(uint)) {
												matched.Add(n);
												goto VALID;
											}
											break;
										case "ulong":
											if(p.GetPortType() == typeof(ulong)) {
												matched.Add(n);
												goto VALID;
											}
											break;
									}
								} else {
									Type type = t.ToType(false);
									if(type != null) {
										if(p.GetPortType() == type) {
											matched.Add(n);
											goto VALID;
										}
									}
								}
								//else if(t.Length > 0 && (char.IsNumber(t[0]) || t[0] == '"')) {

								//}
							}
						}
					VALID:
						continue;
					}
					break;
				}
			}

			HighlightNodes(matched);
			if(matched.Count > 0) {
				Frame(matched);
			}
		}

		public void HighlightNodes(IEnumerable<NodeObject> nodes, float duration = 1) {
			if(nodes == null)
				return;
			if(requiredReload) {
				executeAfterReload += () => {
					Frame(nodes);
					HighlightNodes(GetNodeViews(nodes), duration);
				};
			} else {
				Frame(nodes);
				HighlightNodes(GetNodeViews(nodes), duration);
			}
		}

		protected void HighlightNodes(IList<UNodeView> nodeViews, float duration = 0) {
			if(nodeViews is not Array) {
				foreach(var edge in edgeViews) {
					if(edge is ConversionEdgeView conversionEdge) {
						if(nodeViewsPerNode.TryGetValue(conversionEdge.node, out var view)) {
							if(nodeViews.Contains(view)) {
								if(conversionEdge.Input != null)
									nodeViews.Add(conversionEdge.Input.owner);
								if(conversionEdge.Output != null)
									nodeViews.Add(conversionEdge.Output.owner);
								nodeViews.Remove(view);
							}
						}
					}
				}
			}

			var nodes = nodeViews.ToArray();
			if(highlightedNodes?.Length > 0) {
				//Ensure remove previously highligted nodes
				RemoveHighlightedNodes(highlightedNodes);
			}
			highlightedNodes = nodes;
			foreach(var n in highlightedNodes) {
				if(n != null) {
					var visualElement = n.Q("highlighted-border");
					if(visualElement == null) {
						visualElement = new VisualElement() {
							name = "highlighted-border",
							pickingMode = PickingMode.Ignore,
						};
						n.Add(visualElement);
						visualElement.EnableInClassList("highlighted", true);
					}
					visualElement.SetLayout(new Rect(-10, -10, n.layout.width + 20, n.layout.height + 20));
				}
			}
			if(duration > 0) {
				var time = Time.realtimeSinceStartup;
				uNodeThreadUtility.ExecuteAfterDuration(duration, () => {
					uNodeThreadUtility.ExecuteWhileDuration(1, () => {
						if(highlightedNodes == nodes) {
							foreach(var n in nodes) {
								var visualElement = n.Q("highlighted-border");
								if(visualElement != null) {
									visualElement.SetOpacity(visualElement.resolvedStyle.opacity - uNodeThreadUtility.deltaTime);
								}
							}
						}
					});
					uNodeThreadUtility.ExecuteAfterDuration(1, () => {
						if(highlightedNodes == nodes) {
							RemoveHighlightedNodes(nodes);
						} else {
							RemoveHighlightedNodes(nodes.Where(node => !highlightedNodes.Contains(node)));
						}
					});
				});
			}
		}

		protected void RemoveHighlightedNodes(IEnumerable<UNodeView> nodes) {
			foreach(var n in nodes) {
				if(n != null) {
					var visualElement = n.Q("highlighted-border");
					if(visualElement != null) {
						visualElement.RemoveFromHierarchy();
					}
				}
			}
		}

		public void Frame(IEnumerable<NodeObject> nodes) {
			if(nodes == null)
				return;
			List<UNodeView> nodeViews = new List<UNodeView>();

			foreach(var node in nodes) {
				if(node == null)
					continue;
				if(nodeViewsPerNode.TryGetValue(node, out var view)) {
					nodeViews.Add(view);
				}
			}

			foreach(var edge in edgeViews) {
				if(edge is ConversionEdgeView conversionEdge) {
					if(nodeViewsPerNode.TryGetValue(conversionEdge.node, out var view)) {
						if(nodeViews.Contains(view)) {
							if(conversionEdge.Input != null)
								nodeViews.Add(conversionEdge.Input.owner);
							if(conversionEdge.Output != null)
								nodeViews.Add(conversionEdge.Output.owner);
							nodeViews.Remove(view);
						}
					}
				}
			}

			Frame(nodeViews);
		}

		public void Frame(IList<UNodeView> nodeViews) {
			if(nodeViews == null || nodeViews.Count == 0)
				return;
			Rect rect = contentViewContainer.layout;
			Vector3 frameTranslation = Vector3.zero;
			Vector3 frameScaling = Vector3.one;
			if(nodeViews[0] != null) {
				rect = nodeViews[0].ChangeCoordinatesTo(contentViewContainer, new Rect(0f, 0f, nodeViews[0].layout.width, nodeViews[0].layout.height));
			}
			rect = nodeViews.Aggregate(rect, (current, currentGraphElement) => {
				VisualElement currentElement = currentGraphElement;
				return RectUtils.Encompass(current, currentElement.ChangeCoordinatesTo(contentViewContainer, new Rect(0f, 0f, currentElement.layout.width, currentElement.layout.height)));
			});
			CalculateFrameTransform(rect, layout, 30, out frameTranslation, out frameScaling);
			if(animateFrame) {
				if(nodeViews.Count == 1)
					SetPixelCachedOnBoundChanged(true);
				currentAnimateFrame++;
				int frame = currentAnimateFrame;
				float time = 0;
				this.ScheduleActionUntil(t => {
					time += t.deltaTime / 1000f;
					Vector3 position = Vector3.Lerp(contentViewContainer.transform.position, frameTranslation, time);
					Vector3 scale = Vector3.Lerp(contentViewContainer.transform.scale, frameScaling, time);
					UpdateViewTransform(position, scale);
					contentViewContainer.MarkDirtyRepaint();
				}, () => {
					bool result = currentAnimateFrame != frame ||
						Vector3.Distance(contentViewContainer.transform.position, frameTranslation) < 5 &&
						Vector3.Distance(contentViewContainer.transform.scale, frameScaling) < 5;
					if(result) {
						SetPixelCachedOnBoundChanged(false);
					}
					return result;
				});
			} else {
				Matrix4x4.TRS(frameTranslation, Quaternion.identity, frameScaling);
				UpdateViewTransform(frameTranslation, frameScaling);
				contentViewContainer.MarkDirtyRepaint();
			}
		}
		#endregion

		#region Utility
		private bool isCapturing = false;
		public void CaptureGraphScreenshot(float zoomScale = 2) {
			if(isCapturing)
				return;
			isCapturing = true;
			AutoHideGraphElement.ResetVisibility(this);
			var blockElement = new VisualElement();
			blockElement.StretchToParentSize();
			blockElement.pickingMode = PickingMode.Position;
			window.rootVisualElement.Add(blockElement);

			Vector2 offset = new Vector2(2, 2);//The offset from position to capture
			Vector2 position = graph.window.rootVisualElement.ChangeCoordinatesTo(
				graph.window.rootVisualElement.parent,
				graph.window.position.position);
			position.x += worldBound.x;
			position.y += worldBound.y - graph.window.rootVisualElement.layout.y;
			position += offset;
			SetZoomScale(zoomScale, true);
			int layoutWidth = (int)layout.width - 10;
			int layoutHeight = (int)layout.height - 10;
			int width = (int)(layoutWidth / zoomScale);
			int height = (int)(layoutHeight / zoomScale);

			//EditorUtility.DisplayProgressBar("Capturing", "", 0);
			Rect graphRect = CalculateVisibleGraphRect();
			UpdatePosition(new Vector2(graphRect.x, graphRect.y));
			float xCount = 1;
			float yCount = 1;
			while((xCount * width) + graphRect.x < graphRect.x + graphRect.width) {
				xCount++;
			}
			while((yCount * height) + graphRect.y < graphRect.y + graphRect.height) {
				yCount++;
			}
			if(miniMap != null)
				miniMap.SetDisplay(false);
			uNodeThreadUtility.CreateThread(() => {
				graph.loadingProgress = 1;
				autoHideNodes = false;
				uNodeThreadUtility.WaitFrame(100);
				SetPixelCachedOnBoundChanged(false);
				float progress = 0;
				Texture2D[,] textures = new Texture2D[(int)yCount, (int)xCount];
				int currentCount = 0;
				for(int y = 0; y < textures.GetLength(0); y++) {
					for(int x = 0; x < textures.GetLength(1); x++) {
						uNodeThreadUtility.QueueAndWait(() => {
							UpdatePosition(new Vector2(graphRect.x + (x * width) - offset.x, graphRect.y + (y * height) - offset.x));
						});
						uNodeThreadUtility.WaitFrame(3);
						uNodeThreadUtility.QueueAndWait(/*6,*/() => {
							var pixels = UnityEditorInternal.InternalEditorUtility.ReadScreenPixel(position, layoutWidth, layoutHeight);
							var texture = new Texture2D(layoutWidth, layoutHeight, TextureFormat.RGB24, false);
							texture.SetPixels(pixels);
							textures[y, x] = texture;
							//EditorUtility.DisplayProgressBar("Capturing", "", progress);
						});
						//uNodeThreadUtility.WaitFrame(6);
						currentCount++;
						progress = currentCount / (yCount * xCount);
						graph.loadingProgress = progress;
						//GC.KeepAlive(textures);
					}
				}
				uNodeThreadUtility.QueueAndWait(() => {
					var canvasTexture = new Texture2D((int)xCount * layoutWidth, (int)yCount * layoutHeight, TextureFormat.RGB24, false);
					int realY = (int)yCount;
					for(int y = 0; y < yCount; y++) {
						realY--;
						for(int x = 0; x < xCount; x++) {
							Vector2 offet = new Vector2(x * layoutWidth, y * layoutHeight);
							canvasTexture.SetPixels((int)offet.x, (int)offet.y, layoutWidth, layoutHeight, textures[realY, x].GetPixels());
						}
					}
					//Color[] colors = canvasTexture.GetPixels(0, 0, (int)(graphRect.width * zoomScale), (int)(graphRect.height * zoomScale));
					//canvasTexture = new Texture2D((int)(graphRect.width * zoomScale), (int)(graphRect.height * zoomScale));
					//canvasTexture.SetPixels(colors);
					//canvasTexture.Apply();
					var bytes = canvasTexture.EncodeToPNG();
					EditorUtility.ClearProgressBar();
					isCapturing = false;
					if(miniMap != null)
						miniMap.SetDisplay(true);
					SetZoomScale(1, false);
					blockElement.RemoveFromHierarchy();
					graph.loadingProgress = 0;

					string path = EditorUtility.SaveFilePanel("Save Screenshot", "", "Capture", "png");
					File.WriteAllBytes(path, bytes);
					autoHideNodes = true;
				});
			}).Start();
		}
		#endregion

		#region ProgressBar
		struct ProgressData {
			public string title;
			public string info;
			public float progress;
		}
		private ProgressData _progressData;
		private IMGUIContainer _progressGUI;

		private void DisplayProgressBar(string title, string info, float progress) {
			if(_progressGUI == null) {
				_progressGUI = new IMGUIContainer(OnProgressBarGUI);
				Add(_progressGUI);
				_progressGUI.style.overflow = Overflow.Visible;
				_progressGUI.StretchToParentSize();
			}
			_progressData = new ProgressData() {
				title = title,
				info = info,
				progress = progress,
			};
			//EditorUtility.DisplayProgressBar(title, info, progress);
		}

		private void ClearProgressBar() {
			if(_progressGUI != null) {
				_progressGUI.RemoveFromHierarchy();
				_progressGUI = null;
			}
		}

		private void OnProgressBarGUI() {
			if(Event.current.type == EventType.Repaint) {
				GUI.DrawTexture(new Rect(0, 0, layout.width, layout.height), Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, resolvedStyle.backgroundColor, 0, 0);
				EditorGUI.DropShadowLabel(new Rect(0, (layout.height / 2) -20, layout.width, 20), _progressData.title);
				EditorGUI.ProgressBar(new Rect(0, layout.height / 2, layout.width, 20), _progressData.progress, _progressData.info);
			}
		}
		#endregion

		#region IMGUI
		//Dictionary<GraphElement, Action> graphIMGUIHandler = new Dictionary<GraphElement, Action>();
		//private IMGUIContainer _iMGUIContainer;

		//internal IMGUIContainer IMGUIContainer {
		//	get {
		//		if(_iMGUIContainer == null) {
		//			_iMGUIContainer = new IMGUIContainer(OnGUI);
		//			contentViewContainer.Add(_iMGUIContainer);
		//			_iMGUIContainer.style.overflow = Overflow.Visible;
		//			_iMGUIContainer.StretchToParentSize();
		//		}
		//		return _iMGUIContainer;
		//	}
		//}

		//internal void RegisterIMGUI(GraphElement element, Action onGUI) {
		//	if(IMGUIContainer != null) {
		//		graphIMGUIHandler[element] = onGUI;
		//	}
		//}

		//internal void UnRegisterIMGUI(GraphElement element) {
		//	if(_iMGUIContainer != null) {
		//		graphIMGUIHandler.Remove(element);
		//	}
		//}

		//private void OnGUI() {
		//	foreach(var pair in graphIMGUIHandler) {
		//		if(pair.Key != null && pair.Key.parent != null && pair.Key.IsVisible() && pair.Value != null) {
		//			pair.Value();
		//		}
		//	}
		//}
		#endregion

		#region Private Functions
		private void CreateLinkedMacro(MacroGraph macro, Vector2 position) {
			NodeEditorUtility.AddNewNode<Nodes.LinkedMacroNode>(graphData, null, null, position, (node) => {
				node.macroAsset = macro;
				node.Refresh();
				node.Register();
				NodeEditorUtility.AutoAssignNodePorts(node);
			});
			graph.Refresh();
		}

		private void SelectionAddRegion(Vector2 position) {
			var selectedNodes = graphData.selectedNodes.ToArray();
			Rect rect;
			if(selectedNodes.Length > 0) {
				rect = NodeEditorUtility.GetNodeRect(selectedNodes);
			}
			else {
				Vector2 point;
				if(ContainsPoint(window.rootVisualElement.ChangeCoordinatesTo(this, position))) {
					point = graph.window.rootVisualElement.ChangeCoordinatesTo(
						contentViewContainer,
						position);
				}
				else {
					point = graph.window.rootVisualElement.ChangeCoordinatesTo(
						contentViewContainer,
						new Vector2(300, 200));
				}
				rect = new Rect(point.x, point.y, 200, 130);
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
			graph.Refresh();
		}

		private void SelectionToMacro(Vector2 mousePosition) {
			uNodeEditorUtility.RegisterUndo(graph.graphData.owner, "Selection to Macro");
			NodeEditorUtility.AddNewNode<Nodes.MacroNode>(graph.graphData, "Macro", null, mousePosition, (node) => {
				HashSet<UNodeView> nodeViews = new HashSet<UNodeView>();
				foreach(var n in graph.graphData.selectedNodes) {
					var view = GetNodeView(n);
					if(view.isBlock)
						continue;
					n.SetParent(node.nodeObject);
					nodeViews.Add(view);
				}
				//foreach(var view in nodeViews.ToArray()) {
				//	foreach(var port in view.inputPorts) {
				//		if(port.isValue) {
				//			foreach(var e in port.GetValidEdges()) {
				//				if(e is EdgeViewWithNode edge) {
				//					nodeViews.Add(edge.Output.owner);
				//				}
				//			}
				//		}
				//	}
				//}
				HashSet<PortView> portHash = new HashSet<PortView>();
				Dictionary<PortView, Nodes.MacroPortNode> portMacros = new Dictionary<PortView, Nodes.MacroPortNode>();
				foreach(var view in nodeViews) {
					if(view != null) {
						var ports = view.inputPorts;
						ports.AddRange(view.outputPorts);
						foreach(var port in ports) {
							if(!port.connected || !port.IsVisible() || !portHash.Add(port))
								continue;
							var edges = port.GetEdges();
							foreach(var e in edges) {
								if(!e.isValid)
									continue;
								if(e is EdgeViewWithNode) {
									(e as EdgeViewWithNode).nodeObject.SetParent(node.nodeObject);
								}
								if(port.direction == Direction.Output) {
									var ePort = e.input as PortView;
									if(ePort != null && !nodeViews.Contains(ePort.owner)) {
										if(port.isFlow) {
											if(!portMacros.TryGetValue(port, out var mNode)) {
												NodeEditorUtility.AddNewNode<Nodes.MacroPortNode>(
													graph.graphData, port.GetName(),
													null,
													new Vector2(
														view.targetNode.position.x, view.targetNode.position.y +
														view.targetNode.position.height + 150),
													(macro) => {
														macro.nodeObject.name = port.GetName();
														macro.kind = PortKind.FlowOutput;
														macro.nodeObject.SetParent(node.nodeObject);
														node.Register();
														macro.Register();
														ePort.GetPortValue<FlowInput>().ConnectTo(macro.exit);
														port.GetPortValue<FlowOutput>().ConnectTo(macro.enter);
														portMacros[port] = macro;
														mNode = macro;
													});
											}
											else {
												//ePort.GetPortValue<FlowInput>().ConnectTo(mNode.exit);
												port.GetPortValue<FlowOutput>().ConnectTo(mNode.enter);
											}
										} else {
											if(!portMacros.TryGetValue(port, out var mNode)) {
												NodeEditorUtility.AddNewNode<Nodes.MacroPortNode>(
												graph.graphData,
												null,
												null,
												new Vector2(
													view.targetNode.position.x + view.targetNode.position.width + 150,
													view.targetNode.position.y),
												(macro) => {
													macro.nodeObject.name = port.GetName();
													macro.kind = PortKind.ValueOutput;
													macro.type = port.GetPortType();
													macro.nodeObject.SetParent(node.nodeObject);
													node.Register();
													macro.Register();
													port.GetPortValue<ValueOutput>().ConnectTo(macro.input);
													ePort.GetPortValue<ValueInput>().ConnectTo(macro.output);
													portMacros[port] = macro;
													mNode = macro;
												});
											}
											else {
												//port.GetPortValue<ValueOutput>().ConnectTo(mNode.input);
												ePort.GetPortValue<ValueInput>().ConnectTo(mNode.output);
											}
										}
									}
								} else {
									var ePort = e.output as PortView;
									if(ePort != null && !nodeViews.Contains(ePort.owner)) {
										if(port.isFlow) {
											if(!portMacros.TryGetValue(port, out var mNode)) {
												NodeEditorUtility.AddNewNode<Nodes.MacroPortNode>(graph.graphData, null, null,
												new Vector2(view.targetNode.position.x, view.targetNode.position.y - 150),
												(macro) => {
													macro.nodeObject.name = port.GetName();
													macro.kind = PortKind.FlowInput;
													macro.nodeObject.SetParent(node.nodeObject);
													node.Register();
													macro.Register();
													port.GetPortValue<FlowInput>().ConnectTo(macro.exit);
													ePort.GetPortValue<FlowOutput>().ConnectTo(macro.enter);
													portMacros[port] = macro;
													mNode = macro;
												});
											}
											else {
												//port.GetPortValue<FlowInput>().ConnectTo(mNode.exit);
												ePort.GetPortValue<FlowOutput>().ConnectTo(mNode.enter);
											}
										} else {
											if(!portMacros.TryGetValue(ePort, out var mNode)) {
												NodeEditorUtility.AddNewNode<Nodes.MacroPortNode>(graph.graphData, null, null,
												new Vector2(view.targetNode.position.x - 150, view.targetNode.position.y),
												(macro) => {
													macro.nodeObject.name = port.GetName();
													macro.kind = PortKind.ValueInput;
													macro.type = ePort.GetPortType();
													macro.nodeObject.SetParent(node.nodeObject);
													node.Register();
													macro.Register();
													ePort.GetPortValue<ValueOutput>().ConnectTo(macro.input);
													port.GetPortValue<ValueInput>().ConnectTo(macro.output);
													portMacros[ePort] = macro;
													mNode = macro;

													if(e is EdgeViewWithNode edgeViewWithNode) {
														edgeViewWithNode.inputPortForNode.ConvertTo<ValueInput>().ConnectTo(macro.output);
														edgeViewWithNode.outputPortForNode.ConvertTo<ValueOutput>().ConnectTo(port.GetPortValue<ValueInput>());
													}
												});
											}
											else {
												//ePort.GetPortValue<ValueOutput>().ConnectTo(mNode.input);
												port.GetPortValue<ValueInput>().ConnectTo(mNode.output);
											}
										}
									}
								}
							}
						}
					}
				}
			});
			MarkRepaint(true);
		}
		#endregion
	}
}
