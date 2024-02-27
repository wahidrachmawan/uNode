using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using NodeView = UnityEditor.Experimental.GraphView.Node;

namespace MaxyGames.UNode.Editors {
	/// <summary>
	/// The class for auto hiding the graph elements from the current view to boost the performance.
	/// </summary>
	public static class AutoHideGraphElement {
		static readonly HashSet<NodeView> ignoredNodes = new HashSet<NodeView>();

		/// <summary>
		/// Register a node to ignore from auto hide
		/// </summary>
		/// <param name="node"></param>
		public static void RegisterNodeToIgnore(NodeView node) {
			if(node != null) {
				ignoredNodes.Add(node);
			}
		}

		/// <summary>
		/// Unregister a node to ignore from auto hide
		/// </summary>
		/// <param name="node"></param>
		public static void UnregisterNodeToIgnore(NodeView node) {
			if(node != null) {
				ignoredNodes.Remove(node);
			}
		}

		/// <summary>
		/// Clear the ignored nodes from auto hide
		/// </summary>
		public static void ClearIgnoredNodes() {
			ignoredNodes.Clear();
		}

		/// <summary>
		/// Mark update the visibility for graph so that it will update the visibility in the next frame and it's guarantee to only be executed once.
		/// </summary>
		/// <param name="graphView"></param>
		public static void MarkUpdateVisibility(UGraphView graphView) {
			uNodeThreadUtility.ExecuteOnce(() => UpdateVisibility(graphView), "UpdateGraphVisibility:" + graphView.GetHashCode());
		}

		/// <summary>
		/// Update the graph visibility
		/// </summary>
		/// <param name="graphView"></param>
		public static void UpdateVisibility(UGraphView graphView) {
			UpdateVisibility(graphView, graphView.nodeViews, graphView.edgeViews);
		}

		/// <summary>
		/// Update the graph visibility
		/// </summary>
		/// <param name="graphView"></param>
		/// <param name="nodeViews"></param>
		/// <param name="edgeViews"></param>
		public static void UpdateVisibility(UGraphView graphView, IEnumerable<UNodeView> nodeViews, IEnumerable<EdgeView> edgeViews) {
			UnityEngine.Profiling.Profiler.BeginSample("Update Element Visibility");
			Rect contentRect = graphView.layout;
			HashSet<NodeView> visibleNodes = new HashSet<NodeView>();
			System.Action postAction = null;
			//Auto hide edges
			if(edgeViews != null) {
				foreach(var edge in edgeViews) {
					if(edge.isProxy || edge.isGhostEdge)
						continue;//skip if the edge is proxy because it is hide by default
					var edgeControl = edge.edgeControl;
					Rect edgeRect;
					var input = edge.Input?.owner;
					while(input != null && input.isBlock) {
						input = input.ownerBlock as UNodeView;
					}
					var output = edge.Output?.owner;
					while(output != null && output.isBlock) {
						output = output.ownerBlock as UNodeView;
					}
					if(input != null && input != null && output != null && output != null) {
						edgeRect = RectUtils.Encompass(
							graphView.contentViewContainer.ChangeCoordinatesTo(graphView, input.isHidden ? input.hidingRect : input.layout), 
							graphView.contentViewContainer.ChangeCoordinatesTo(graphView, output.isHidden ? output.hidingRect : output.layout)
						);
					} else {
						edgeRect = edgeControl.ChangeCoordinatesTo(graphView, edgeControl.GetRect());
					}
					if(edgeRect.Overlaps(contentRect)) {
						if(edge.parent == null && edge.isHidding) {
							graphView.AddElement(edge);
							postAction += edge.UpdateEndPoints;
							edge.isHidding = false;
						}
						if(input != null) {
							visibleNodes.Add(input);
						}
						if(output != null) {
							visibleNodes.Add(output);
						}
					} else if(edge.parent != null) {
						edge.isHidding = true;
						graphView.RemoveElement(edge);
					}
				}
			}
			//Auto hide nodes
			if(nodeViews != null) {
				foreach(var node in nodeViews) {
					if(node.isBlock)
						continue;
					if(visibleNodes.Contains(node) || ignoredNodes.Contains(node)) {
						if(node.parent == null) {
							graphView.AddElement(node);
						}
						continue;
					}
					Rect nodeRect = graphView.contentViewContainer.ChangeCoordinatesTo(graphView, node.nodeObject.position);
					nodeRect.x -= 200;
					nodeRect.y -= 100;
					nodeRect.width += 400;
					nodeRect.height += 200;
					if(nodeRect.Overlaps(contentRect)) {
						if(node.parent == null) {
							graphView.AddElement(node);
						}
					} else if(node.parent != null) {
						node.hidingRect = node.ChangeCoordinatesTo(graphView.contentViewContainer, node.GetRect());
						graphView.RemoveElement(node);
					}
				}
			}
			postAction?.Invoke();
			UnityEngine.Profiling.Profiler.EndSample();
		}

		/// <summary>
		/// Reset the graph visibility states
		/// </summary>
		/// <param name="graphView"></param>
		public static void ResetVisibility(UGraphView graphView) {
			graphView.edgeViews.ForEach(edge => {
				if(edge.isProxy)
					return;//skip if the edge is proxy because it is hide by default
				if(edge.parent == null) {
					graphView.AddElement(edge);
				}
			});
			//Auto hide nodes
			graphView.nodeViews.ForEach((node) => {
				if(node.parent == null) {
					graphView.AddElement(node);
				}
			});
		}
	}
}