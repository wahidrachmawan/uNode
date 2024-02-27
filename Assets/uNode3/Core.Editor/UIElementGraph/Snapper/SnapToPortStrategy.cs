using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;

namespace MaxyGames.UNode.Editors {
	class SnapToPortStrategy : SnapStrategy {
		class SnapToPortResult : SnapResult {
			public Orientation PortOrientation { get; set; }
		}

		public override void UpdateSnap() {
			var preference = uNodePreference.GetPreference();
            Enabled = preference.enableSnapping && preference.nodePortSnapping;
			SnapDistance = preference.portSnappingRange;
		}
        
		List<EdgeView> m_ConnectedEdges = new List<EdgeView>();
		Dictionary<PortView, Vector2> m_ConnectedPortsPos = new Dictionary<PortView, Vector2>();

		public override void BeginSnap(GraphElement selectedElement) {
			if (IsActive) {
				EndSnap();
				//throw new InvalidOperationException("SnapStrategy.BeginSnap: Snap to port already active. Call EndSnap() first.");
			}
			IsActive = true;

			if (selectedElement is UNodeView selectedNode) {
				m_GraphView = selectedNode.owner;

				m_ConnectedEdges = GetConnectedEdges(selectedNode);
				m_ConnectedPortsPos = GetConnectedPortPositions(m_ConnectedEdges);
			}
		}

		public override Rect GetSnappedRect(ref Vector2 snappingOffset, Rect sourceRect, GraphElement selectedElement, float scale, Vector2 mousePanningDelta = default) {
			if (!IsActive) {
				throw new InvalidOperationException("SnapStrategy.GetSnappedRect: Snap to port not active. Call BeginSnap() first.");
			}

			if (IsPaused) {
				// Snapping was paused, we do not return a snapped rect
				return sourceRect;
			}
			//if(selectedElement is RegionNodeView)
			//	return sourceRect;

			Rect snappedRect = sourceRect;

			if (selectedElement is UNodeView selectedNode) {
				m_CurrentScale = scale;
				SnapToPortResult chosenResult = GetClosestSnapToPortResult(selectedNode, mousePanningDelta);

				if (chosenResult != null) {
					var adjustedSourceRect = GetAdjustedSourceRect(chosenResult, sourceRect, mousePanningDelta);
					snappedRect = adjustedSourceRect;
					ApplySnapToPortResult(ref snappingOffset, adjustedSourceRect, ref snappedRect, chosenResult);
				}
			}

			return snappedRect;
		}

		public override void EndSnap() {
			if (!IsActive) {
				throw new InvalidOperationException("SnapStrategy.EndSnap: Snap to port already inactive. Call BeginSnap() first.");
			}
			IsActive = false;

			m_ConnectedEdges.Clear();
			m_ConnectedPortsPos.Clear();
		}

		Dictionary<PortView, Vector2> GetConnectedPortPositions(List<EdgeView> edges) {
			Dictionary<PortView, Vector2> connectedPortsOriginalPos = new Dictionary<PortView, Vector2>();
			if(m_GraphView == null) return connectedPortsOriginalPos;
			foreach (var edge in edges) {
				PortView inputPort = edge.Input;
				PortView outputPort = edge.Output;

				if (inputPort != null && inputPort.parent != null) {
					Vector2 inputPortPosInContentViewContainerSpace = inputPort.parent.ChangeCoordinatesTo(m_GraphView.contentViewContainer, inputPort.layout.center);
					if (!connectedPortsOriginalPos.ContainsKey(inputPort)) {
						connectedPortsOriginalPos.Add(inputPort, inputPortPosInContentViewContainerSpace);
					}
				}

				if (outputPort != null && outputPort.parent != null) {
					Vector2 outputPortPosInContentViewContainerSpace = outputPort.parent.ChangeCoordinatesTo(m_GraphView.contentViewContainer, outputPort.layout.center);
					if (!connectedPortsOriginalPos.ContainsKey(outputPort)) {
						connectedPortsOriginalPos.Add(outputPort, outputPortPosInContentViewContainerSpace);
					}
				}
			}
			return connectedPortsOriginalPos;
		}

		List<EdgeView> GetConnectedEdges(UNodeView selectedNode) {
			List<EdgeView> connectedEdges = new List<EdgeView>();

			foreach (EdgeView edge in m_GraphView.edges.ToList()) {
				if (edge.Output != null && edge.Output.owner == selectedNode || edge.Input != null && edge.Input.owner == selectedNode) {
					connectedEdges.Add(edge);
				}
			}

			return connectedEdges;
		}

		SnapToPortResult GetClosestSnapToPortResult(UNodeView selectedNode, Vector2 mousePanningDelta) {
			var results = GetSnapToPortResults(selectedNode);

			float smallestDraggedDistanceFromNode = float.MaxValue;
			SnapToPortResult closestResult = null;
			foreach (SnapToPortResult result in results) {
				// We have to consider the mouse and panning delta to estimate the distance when the node is being dragged
				float draggedDistanceFromNode = Math.Abs(result.Offset - (result.PortOrientation == Orientation.Horizontal ? mousePanningDelta.y : mousePanningDelta.x));
				bool isSnapping = IsSnappingToPort(draggedDistanceFromNode);

				if (isSnapping && smallestDraggedDistanceFromNode > draggedDistanceFromNode) {
					smallestDraggedDistanceFromNode = draggedDistanceFromNode;
					closestResult = result;
				}
			}

			return closestResult;
		}

		static Rect GetAdjustedSourceRect(SnapToPortResult result, Rect sourceRect, Vector2 mousePanningDelta) {
			Rect adjustedSourceRect = sourceRect;
			// We only want the mouse delta position and panning info on the axis that is not snapping
			if (result.PortOrientation == Orientation.Horizontal) {
				adjustedSourceRect.y += mousePanningDelta.y;
			} else {
				adjustedSourceRect.x += mousePanningDelta.x;
			}

			return adjustedSourceRect;
		}

		IEnumerable<SnapToPortResult> GetSnapToPortResults(UNodeView selectedNode) {
			return m_ConnectedEdges.Select(edge => GetSnapToPortResult(edge, selectedNode)).Where(result => result != null);
		}

		SnapToPortResult GetSnapToPortResult(EdgeView edge, UNodeView selectedNode) {
			PortView sourcePort = null;
			PortView snappablePort = null;

			if (edge.Output?.owner == selectedNode) {
				sourcePort = edge.Output;
				snappablePort = edge.Input;
			} else if (edge.Input?.owner == selectedNode) {
				sourcePort = edge.Input;
				snappablePort = edge.Output;
			}

			// We don't want to snap non existing ports and ports with different orientations (to be determined)
			if (sourcePort == null || snappablePort == null || sourcePort.orientation != snappablePort.orientation) {
				return null;
			}

			float offset;
			if (snappablePort.orientation == Orientation.Horizontal) {
				offset = m_ConnectedPortsPos[sourcePort].y - m_ConnectedPortsPos[snappablePort].y;
			} else {
				offset = m_ConnectedPortsPos[sourcePort].x - m_ConnectedPortsPos[snappablePort].x;
			}

			SnapToPortResult minResult = new SnapToPortResult {
				PortOrientation = snappablePort.orientation,
				Offset = offset
			};

			return minResult;
		}

		bool IsSnappingToPort(float draggedDistanceFromNode) => draggedDistanceFromNode <= SnapDistance * 1 / m_CurrentScale;

		static void ApplySnapToPortResult(ref Vector2 snappingOffset, Rect sourceRect, ref Rect r1, SnapToPortResult result) {
			if (result.PortOrientation == Orientation.Horizontal) {
				r1.y = sourceRect.y - result.Offset;
				snappingOffset.y = result.Offset;
			} else {
				r1.x = sourceRect.x - result.Offset;
				snappingOffset.x = result.Offset;
			}
		}
	}
}