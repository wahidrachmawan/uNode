using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;

namespace MaxyGames.UNode.Editors {
	public class EdgeDragHelper<TEdge> : EdgeDragHelper where TEdge : Edge, new() {
		protected List<PortView> m_CompatiblePorts;
		private Edge m_GhostEdge;
		protected UGraphView m_GraphView;
		protected static NodeAdapter s_nodeAdapter = new NodeAdapter();
		protected readonly IEdgeConnectorListener m_Listener;

		private IVisualElementScheduledItem m_PanSchedule;
		private Vector3 m_PanDiff = Vector3.zero;
		private bool m_WasPanned;

		internal const int k_PanAreaWidth = 100;
		internal const int k_PanSpeed = 4;
		internal const int k_PanInterval = 10;
		internal const float k_MinSpeedFactor = 0.5f;
		internal const float k_MaxSpeedFactor = 2.5f;
		internal const float k_MaxPanSpeed = k_MaxSpeedFactor * k_PanSpeed;

		public bool resetPositionOnPan { get; set; }

		public EdgeDragHelper(IEdgeConnectorListener listener) {
			m_Listener = listener;
			//resetPositionOnPan = true;
			Reset();
		}

		public override Edge edgeCandidate { get; set; }

		public override UnityEditor.Experimental.GraphView.Port draggedPort { get; set; }

		public override void Reset(bool didConnect = false) {
			if(m_CompatiblePorts != null) {
				// Reset the highlights.
				m_GraphView.ports.ForEach((p) => {
					p.OnStopEdgeDragging();
				});
				m_CompatiblePorts = null;
			}

			// Clean up ghost edge.
			if((m_GhostEdge != null) && (m_GraphView != null)) {
				m_GraphView.RemoveElement(m_GhostEdge);
			}

			if(m_WasPanned) {
				if(!resetPositionOnPan || didConnect) {
					Vector3 p = m_GraphView.contentViewContainer.transform.position;
					Vector3 s = m_GraphView.contentViewContainer.transform.scale;
					m_GraphView.UpdateViewTransform(p, s);
				}
			}

			if(m_PanSchedule != null)
				m_PanSchedule.Pause();

			if(m_GhostEdge != null) {
				m_GhostEdge.input = null;
				m_GhostEdge.output = null;
			}

			if(draggedPort != null && !didConnect) {
				draggedPort.portCapLit = false;
				draggedPort = null;
			}

			m_GhostEdge = null;
			edgeCandidate = null;

			m_GraphView = null;
		}

		public override bool HandleMouseDown(MouseDownEvent evt) {
			Vector2 mousePosition = evt.mousePosition;

			if((draggedPort == null) || (edgeCandidate == null)) {
				return false;
			}

			m_GraphView = draggedPort.GetFirstAncestorOfType<UGraphView>();

			if(m_GraphView == null) {
				return false;
			}

			if(edgeCandidate.parent == null) {
				m_GraphView.AddElement(edgeCandidate);
			}

			bool startFromOutput = (draggedPort.direction == Direction.Output);

			edgeCandidate.candidatePosition = mousePosition;
			edgeCandidate.SetEnabled(false);

			if(startFromOutput) {
				edgeCandidate.output = draggedPort;
				edgeCandidate.input = null;
			} else {
				edgeCandidate.output = null;
				edgeCandidate.input = draggedPort;
			}

			draggedPort.portCapLit = true;


			m_CompatiblePorts = m_GraphView.GetCompatiblePorts(draggedPort as PortView, s_nodeAdapter);

			// Only light compatible anchors when dragging an edge.
			m_GraphView.ports.ForEach((p) => {
				p.OnStartEdgeDragging();
			});

			foreach(var compatiblePort in m_CompatiblePorts) {
				compatiblePort.highlight = true;
			}

			edgeCandidate.UpdateEdgeControl();

			if(m_PanSchedule == null) {
				m_PanSchedule = m_GraphView.schedule.Execute(Pan).Every(k_PanInterval).StartingIn(k_PanInterval);
				m_PanSchedule.Pause();
			}
			m_WasPanned = false;

			edgeCandidate.layer = Int32.MaxValue;

			return true;
		}

		internal Vector2 GetEffectivePanSpeed(Vector2 mousePos) {
			Vector2 effectiveSpeed = Vector2.zero;

			if(mousePos.x <= k_PanAreaWidth)
				effectiveSpeed.x = -(((k_PanAreaWidth - mousePos.x) / k_PanAreaWidth) + 0.5f) * k_PanSpeed;
			else if(mousePos.x >= m_GraphView.contentContainer.layout.width - k_PanAreaWidth)
				effectiveSpeed.x = (((mousePos.x - (m_GraphView.contentContainer.layout.width - k_PanAreaWidth)) / k_PanAreaWidth) + 0.5f) * k_PanSpeed;

			if(mousePos.y <= k_PanAreaWidth)
				effectiveSpeed.y = -(((k_PanAreaWidth - mousePos.y) / k_PanAreaWidth) + 0.5f) * k_PanSpeed;
			else if(mousePos.y >= m_GraphView.contentContainer.layout.height - k_PanAreaWidth)
				effectiveSpeed.y = (((mousePos.y - (m_GraphView.contentContainer.layout.height - k_PanAreaWidth)) / k_PanAreaWidth) + 0.5f) * k_PanSpeed;

			effectiveSpeed = Vector2.ClampMagnitude(effectiveSpeed, k_MaxPanSpeed);

			return effectiveSpeed;
		}

		public override void HandleMouseMove(MouseMoveEvent evt) {
			var ve = (VisualElement)evt.target;
			Vector2 gvMousePos = ve.ChangeCoordinatesTo(m_GraphView.contentContainer, evt.localMousePosition);
			m_PanDiff = GetEffectivePanSpeed(gvMousePos);

			if(m_PanDiff != Vector3.zero) {
				m_PanSchedule.Resume();
			} else {
				m_PanSchedule.Pause();
			}

			Vector2 mousePosition = evt.mousePosition;

			edgeCandidate.candidatePosition = mousePosition;

			// Draw ghost edge if possible port exists.
			var endPort = GetEndPort(evt);

			if(endPort != null) {
				if(m_GhostEdge == null) {
					m_GhostEdge = new TEdge();
					m_GhostEdge.isGhostEdge = true;
					m_GhostEdge.pickingMode = PickingMode.Ignore;
					m_GraphView.AddElement(m_GhostEdge);
				}

				if(edgeCandidate.output == null) {
					m_GhostEdge.input = edgeCandidate.input;
					if(m_GhostEdge.output != null)
						m_GhostEdge.output.portCapLit = false;
					m_GhostEdge.output = endPort;
					m_GhostEdge.output.portCapLit = true;
				} else {
					if(m_GhostEdge.input != null)
						m_GhostEdge.input.portCapLit = false;
					m_GhostEdge.input = endPort;
					m_GhostEdge.input.portCapLit = true;
					m_GhostEdge.output = edgeCandidate.output;
				}
			} else if(m_GhostEdge != null) {
				if(edgeCandidate.input == null) {
					if(m_GhostEdge.input != null)
						m_GhostEdge.input.portCapLit = false;
				} else {
					if(m_GhostEdge.output != null)
						m_GhostEdge.output.portCapLit = false;
				}
				m_GraphView.RemoveElement(m_GhostEdge);
				m_GhostEdge.input = null;
				m_GhostEdge.output = null;
				m_GhostEdge = null;
			}
			if(endPort == null) {
				IDropTarget dropTarget = GetDropTargetAt(evt.mousePosition, null);
				if(m_PrevDropTarget != dropTarget) {
					if(m_PrevDropTarget != null) {
						using(DragLeaveEvent eexit = DragLeaveEvent.GetPooled(evt)) {
							SendDragAndDropEvent(eexit, new List<ISelectable>() { draggedPort }, m_PrevDropTarget, m_GraphView);
						}
					}

					using(DragEnterEvent eenter = DragEnterEvent.GetPooled(evt)) {
						SendDragAndDropEvent(eenter, new List<ISelectable>() { draggedPort }, dropTarget, m_GraphView);
					}
				}

				using(DragUpdatedEvent eupdated = DragUpdatedEvent.GetPooled(evt)) {
					SendDragAndDropEvent(eupdated, new List<ISelectable>() { draggedPort }, dropTarget, m_GraphView);
				}
				m_PrevDropTarget = dropTarget;
			} else {
				if(m_PrevDropTarget != null) {
					using(DragLeaveEvent eexit = DragLeaveEvent.GetPooled(evt)) {
						SendDragAndDropEvent(eexit, new List<ISelectable>() { draggedPort }, m_PrevDropTarget, m_GraphView);
					}
				}
				m_PrevDropTarget = null;
			}
		}

		private void Pan(TimerState ts) {
			m_GraphView.viewTransform.position -= m_PanDiff;
			edgeCandidate.output = edgeCandidate.output;
			edgeCandidate.input = edgeCandidate.input;

			edgeCandidate.UpdateEdgeControl();
			m_WasPanned = true;
		}

		public override void HandleMouseUp(MouseUpEvent evt) {
			bool didConnect = false;

			Vector2 mousePosition = evt.mousePosition;

			// Reset the highlights.
			m_GraphView.ports.ForEach((p) => {
				p.OnStopEdgeDragging();
			});

			// Clean up ghost edges.
			if(m_GhostEdge != null) {
				if(m_GhostEdge.input != null)
					m_GhostEdge.input.portCapLit = false;
				if(m_GhostEdge.output != null)
					m_GhostEdge.output.portCapLit = false;

				m_GraphView.RemoveElement(m_GhostEdge);
				m_GhostEdge.input = null;
				m_GhostEdge.output = null;
				m_GhostEdge = null;
			}

			var endPort = GetEndPort(evt);
			if(endPort == null) {
				bool flag = true;
				if(m_PrevDropTarget != null) {
					if(m_PrevDropTarget.CanAcceptDrop(new List<ISelectable>() { draggedPort })) {
						using(DragPerformEvent drop = DragPerformEvent.GetPooled(evt)) {
							SendDragAndDropEvent(drop, new List<ISelectable>() { draggedPort }, m_PrevDropTarget, m_GraphView);
						}
						flag = false;
					} else {
						using(DragExitedEvent dexit = DragExitedEvent.GetPooled(evt)) {
							SendDragAndDropEvent(dexit, new List<ISelectable>() { draggedPort }, m_PrevDropTarget, m_GraphView);
						}
					}
				}
				if(flag && m_Listener != null) {
					if(evt.modifiers == EventModifiers.Shift) {
						var edge = edgeCandidate as EdgeView;
						if(edge != null) {
							if(edge.Input != null) {
								var port = edge.Input;

								var screenRect = port.owner.graph.window.GetMousePositionForMenu(mousePosition);
								Vector2 pos = port.owner.graph.window.rootVisualElement.ChangeCoordinatesTo(
									port.owner.graph.window.rootVisualElement.parent,
									screenRect - port.owner.graph.window.position.position);
								var position = port.owner.owner.contentViewContainer.WorldToLocal(pos);
								if(port.isFlow) {
									NodeEditorUtility.AddNewNode(port.owner.graphData, position, (Nodes.NodeReroute node) => {
										node.kind = Nodes.NodeReroute.RerouteKind.Flow;
										node.Register();
										var con = Connection.CreateAndConnect(port.GetPortValue(), node.exit);
										NodeEditorUtility.AutoRerouteAndProxy(con, port.owner.graphData.currentCanvas);
										port.owner.owner.MarkRepaint();
									});
								}
								else {
									NodeEditorUtility.AddNewNode(port.owner.graphData, position, (Nodes.NodeReroute node) => {
										node.kind = Nodes.NodeReroute.RerouteKind.Value;
										node.Register();
										var con = Connection.CreateAndConnect(port.GetPortValue(), node.output);
										NodeEditorUtility.AutoRerouteAndProxy(con, port.owner.graphData.currentCanvas);
										port.owner.owner.MarkRepaint();
									});
								}
							}
							else if(edge.Output != null) {
								var port = edge.Output;

								var screenRect = port.owner.graph.window.GetMousePositionForMenu(mousePosition);
								Vector2 pos = port.owner.graph.window.rootVisualElement.ChangeCoordinatesTo(
									port.owner.graph.window.rootVisualElement.parent,
									screenRect - port.owner.graph.window.position.position);
								var position = port.owner.owner.contentViewContainer.WorldToLocal(pos);
								if(port.isFlow) {
									NodeEditorUtility.AddNewNode(port.owner.graphData, position, (Nodes.NodeReroute node) => {
										node.kind = Nodes.NodeReroute.RerouteKind.Flow;
										node.Register();
										var con = Connection.CreateAndConnect(node.enter, port.GetPortValue());
										NodeEditorUtility.AutoRerouteAndProxy(con, port.owner.graphData.currentCanvas);
										port.owner.owner.MarkRepaint();
									});
								}
								else {
									NodeEditorUtility.AddNewNode(port.owner.graphData, position, (Nodes.NodeReroute node) => {
										node.kind = Nodes.NodeReroute.RerouteKind.Value;
										node.Register();
										var con = Connection.CreateAndConnect(node.input, port.GetPortValue());
										NodeEditorUtility.AutoRerouteAndProxy(con, port.owner.graphData.currentCanvas);
										port.owner.owner.MarkRepaint();
									});
								}
							}
						}
					}
					else if(evt.modifiers == EventModifiers.Alt) {
						var edge = edgeCandidate as EdgeView;
						if(edge != null) {
							if(edge.Input != null) {
								var port = edge.Input;

								if(port.owner.graphData.isInMacro) {
									var screenRect = port.owner.graph.window.GetMousePositionForMenu(mousePosition);
									Vector2 pos = port.owner.graph.window.rootVisualElement.ChangeCoordinatesTo(
										port.owner.graph.window.rootVisualElement.parent,
										screenRect - port.owner.graph.window.position.position);
									var position = port.owner.owner.contentViewContainer.WorldToLocal(pos);
									if(port.isFlow) {
										NodeEditorUtility.AddNewNode(port.owner.graphData, position, (Nodes.MacroPortNode node) => {
											node.kind = PortKind.FlowInput;
											node.Register();
											var con = Connection.CreateAndConnect(node.exit, port.GetPortValue());
											NodeEditorUtility.AutoRerouteAndProxy(con, port.owner.graphData.currentCanvas);
											port.owner.owner.MarkRepaint();
										});
									} else {
										NodeEditorUtility.AddNewNode(port.owner.graphData, position, (Nodes.MacroPortNode node) => {
											if(string.IsNullOrWhiteSpace(port.GetName()) == false) {
												node.nodeObject.name = port.GetName();
											}
											node.kind = PortKind.ValueInput;
											node.Register();
											var con = Connection.CreateAndConnect(port.GetPortValue(), node.output);
											NodeEditorUtility.AutoRerouteAndProxy(con, port.owner.graphData.currentCanvas);
											port.owner.owner.MarkRepaint();
										});
									}
								}
							}
							else if(edge.Output != null) {
								var port = edge.Output;

								if(port.owner.graphData.isInMacro) {
									var screenRect = port.owner.graph.window.GetMousePositionForMenu(mousePosition);
									Vector2 pos = port.owner.graph.window.rootVisualElement.ChangeCoordinatesTo(
										port.owner.graph.window.rootVisualElement.parent,
										screenRect - port.owner.graph.window.position.position);
									var position = port.owner.owner.contentViewContainer.WorldToLocal(pos);
									if(port.isFlow) {
										NodeEditorUtility.AddNewNode(port.owner.graphData, position, (Nodes.MacroPortNode node) => {
											node.kind = PortKind.FlowOutput;
											node.Register();
											var con = Connection.CreateAndConnect(port.GetPortValue(), node.enter);
											NodeEditorUtility.AutoRerouteAndProxy(con, port.owner.graphData.currentCanvas);
											port.owner.owner.MarkRepaint();
										});
									} else {
										NodeEditorUtility.AddNewNode(port.owner.graphData, position, (Nodes.MacroPortNode node) => {
											if(string.IsNullOrWhiteSpace(port.GetName()) == false) {
												node.nodeObject.name = port.GetName();
											}
											node.kind = PortKind.ValueOutput;
											node.Register();
											var con = Connection.CreateAndConnect(node.input, port.GetPortValue());
											NodeEditorUtility.AutoRerouteAndProxy(con, port.owner.graphData.currentCanvas);
											port.owner.owner.MarkRepaint();
										});
									}
								}
							}
						}
					}
					else {
						m_Listener.OnDropOutsidePort(edgeCandidate, mousePosition);
					}
				}
			}

			edgeCandidate.SetEnabled(true);

			if(edgeCandidate.input != null)
				edgeCandidate.input.portCapLit = false;

			if(edgeCandidate.output != null)
				edgeCandidate.output.portCapLit = false;

			// If it is an existing valid edge then delete and notify the model (using DeleteElements()).
			if(edgeCandidate.input != null && edgeCandidate.output != null) {
				// Save the current input and output before deleting the edge as they will be reset
				var oldInput = edgeCandidate.input;
				var oldOutput = edgeCandidate.output;

				m_GraphView.DeleteElements(new[] { edgeCandidate });

				// Restore the previous input and output
				edgeCandidate.input = oldInput;
				edgeCandidate.output = oldOutput;
			}
			// otherwise, if it is an temporary edge then just remove it as it is not already known my the model
			else {
				m_GraphView.RemoveElement(edgeCandidate);
			}

			if(endPort != null) {
				if(endPort.direction == Direction.Output) {
					if(edgeCandidate.output is PortView port) {
						port.ResetPortValue();
					}
					edgeCandidate.output = endPort;
				} else {
					if(edgeCandidate.input is PortView port) {
						port.ResetPortValue();
					}
					edgeCandidate.input = endPort;
				}
				m_Listener.OnDrop(m_GraphView, edgeCandidate);
				if(m_PrevDropTarget != null) {
					using(DragExitedEvent dexit = DragExitedEvent.GetPooled(evt)) {
						SendDragAndDropEvent(dexit, new List<ISelectable>() { draggedPort }, m_PrevDropTarget, m_GraphView);
					}
				}
				didConnect = true;
			} else {
				edgeCandidate.output = null;
				edgeCandidate.input = null;
			}

			edgeCandidate.ResetLayer();

			m_PrevDropTarget = null;
			edgeCandidate = null;
			m_CompatiblePorts = null;
			Reset(didConnect);
		}

		public void HandleMouseCaptureOut(MouseCaptureOutEvent evt) {
			if(m_PrevDropTarget != null && m_GraphView != null) {
				if(m_PrevDropTarget.CanAcceptDrop(m_GraphView.selection)) {
					m_PrevDropTarget.DragExited();
				}
			}

			// Stop processing the event sequence if the target has lost focus, then.
			m_PrevDropTarget = null;
		}

		private UnityEditor.Experimental.GraphView.Port GetEndPort(IMouseEvent evt) {
			if(m_GraphView == null)
				return null;
			UnityEditor.Experimental.GraphView.Port endPort = null;
			Vector2 mousePosition = evt.mousePosition;

			foreach(var compatiblePort in m_CompatiblePorts) {
				Rect bounds = compatiblePort.worldBound;
				float hitboxExtraPadding = bounds.height;

				if(compatiblePort.isValue) {
					// Add extra padding for mouse check to the left of input port or right of output port.
					if(compatiblePort.direction == Direction.Input) {
						// Move bounds to the left by hitboxExtraPadding and increase width
						// by hitboxExtraPadding.
						bounds.x -= hitboxExtraPadding;
						bounds.width += hitboxExtraPadding;
					} else if(compatiblePort.direction == Direction.Output) {
						// Just add hitboxExtraPadding to the width.
						bounds.width += hitboxExtraPadding;
					}
				}

				// Check if mouse is over port.
				if(bounds.Contains(mousePosition)) {
					endPort = compatiblePort;
					break;
				}
			}
			if(endPort == null) {
				var dragPort = draggedPort as PortView;
				if(dragPort.isFlow) {
					var target = (evt as EventBase).target as VisualElement;
					List<VisualElement> picked = new List<VisualElement>();
					target.panel.PickAll(mousePosition, picked);
					UNodeView node = null;
					if(picked.Count > 0) {
						node = picked.FirstOrDefault(p => p is UNodeView) as UNodeView;
					}
					if(node != null && node != draggedPort.node) {
						//Find the closest compatible port.
						float closestDistance = float.MaxValue;
						foreach(var compatiblePort in m_CompatiblePorts) {
							if(compatiblePort.owner == node) {
								var distance = Vector2.Distance(compatiblePort.worldBound.position, mousePosition);
								if(distance < closestDistance) {
									endPort = compatiblePort;
									closestDistance = distance;
								}
							}
						}
					}
				} else {
					if(GetDropTargetAt(evt.mousePosition, null) == null) {
						var target = (evt as EventBase).target as VisualElement;
						List<VisualElement> picked = new List<VisualElement>();
						target.panel.PickAll(mousePosition, picked);
						UNodeView node = null;
						if(picked.Count > 0) {
							node = picked.FirstOrDefault(p => p is UNodeView) as UNodeView;
						}
						if(node != null && node != draggedPort.node) {
							var type = dragPort?.GetPortType();
							if(type != null) {
								//Find the closest compatible port.
								float closestDistance = float.MaxValue;
								foreach(var port in m_CompatiblePorts) {
									if(port.owner != node)
										continue;
									if(draggedPort.direction == Direction.Input ? port.GetPortType().IsCastableTo(type) : type.IsCastableTo(port.GetPortType())) {
										var distance = Vector2.Distance(port.worldBound.position, mousePosition);
										if(distance < closestDistance) {
											endPort = port;
											closestDistance = distance;
										}
									}
								}
							}
							if(endPort == null) {
								//Find the closest compatible port that's can be auto converted.
								float closestDistance = float.MaxValue;
								foreach(var port in m_CompatiblePorts) {
									if(port.owner != node)
										continue;
									if(dragPort.CanConnect(port)) {
										var distance = Vector2.Distance(port.worldBound.position, mousePosition);
										if(distance < closestDistance) {
											endPort = port;
											closestDistance = distance;
										}
									}
								}
							}
						}
					}
				}
			}
			return endPort;
		}

		#region Drag & Drop
		IDropTarget m_PrevDropTarget;
		static void SendDragAndDropEvent(IDragAndDropEvent evt, List<ISelectable> selection, IDropTarget dropTarget, ISelection dragSource) {
			if(dropTarget == null) {
				return;
			}

			EventBase e = evt as EventBase;
			if(e.eventTypeId == DragExitedEvent.TypeId()) {
				dropTarget.DragExited();
			} else if(e.eventTypeId == DragEnterEvent.TypeId()) {
				dropTarget.DragEnter(evt as DragEnterEvent, selection, dropTarget, dragSource);
			} else if(e.eventTypeId == DragLeaveEvent.TypeId()) {
				dropTarget.DragLeave(evt as DragLeaveEvent, selection, dropTarget, dragSource);
			}

			if(!dropTarget.CanAcceptDrop(selection)) {
				return;
			}

			if(e.eventTypeId == DragPerformEvent.TypeId()) {
				dropTarget.DragPerform(evt as DragPerformEvent, selection, dropTarget, dragSource);
			} else if(e.eventTypeId == DragUpdatedEvent.TypeId()) {
				dropTarget.DragUpdated(evt as DragUpdatedEvent, selection, dropTarget, dragSource);
			}
		}

		private List<VisualElement> m_DropTargetPickList = new List<VisualElement>();
		IDropTarget GetDropTargetAt(Vector2 mousePosition, IEnumerable<VisualElement> exclusionList) {
			Vector2 pickPoint = mousePosition;
			var pickList = m_DropTargetPickList;
			pickList.Clear();
			draggedPort.panel.PickAll(pickPoint, pickList);

			IDropTarget dropTarget = null;

			for(int i = 0; i < pickList.Count; i++) {
				if(pickList[i] == draggedPort)
					continue;

				var picked = pickList[i];

				dropTarget = picked as IDropTarget;

				if(dropTarget != null) {
					if(exclusionList != null && exclusionList.Contains(picked)) {
						dropTarget = null;
					} else
						break;
				}
			}

			return dropTarget;
		}
		#endregion
	}
}