using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MaxyGames.UNode.Nodes;
using UnityEditor;

namespace MaxyGames.UNode.Editors.Commands {
	internal static class SelectionCommandUtility {
		public static float GetWidth(NodeObject node, UIElementGraph uiGraph) {
			if(uiGraph != null && uiGraph.graphView != null && uiGraph.graphView.nodeViewsPerNode.TryGetValue(node, out var view)) {
				if(!float.IsNaN(view.layout.width) && view.layout.width > 0) {
					return view.layout.width;
				}
			}
			return node.position.width;
		}

		public static float GetHeight(NodeObject node, UIElementGraph uiGraph) {
			if(uiGraph != null && uiGraph.graphView != null && uiGraph.graphView.nodeViewsPerNode.TryGetValue(node, out var view)) {
				if(!float.IsNaN(view.layout.height) && view.layout.height > 0) {
					return view.layout.height;
				}
			}
			return node.position.height;
		}

		public static void UpdateNodePosition(NodeObject node, Rect newPosition, UIElementGraph uiGraph) {
			node.position = newPosition;
			if(uiGraph != null && uiGraph.graphView != null && uiGraph.graphView.nodeViewsPerNode.TryGetValue(node, out var view)) {
				view.Teleport(newPosition);
			}
		}
	}

	public class AlignLeftCommand : NodeMenuCommand {
		public override string name => "Align / Align Left";

		public override void OnClick(Node source, Vector2 mousePosition) {
			var selectedNodes = graphData.selectedNodes.ToList();
			if(selectedNodes.Count < 2) return;

			uNodeEditorUtility.RegisterUndo(graphData.owner, "Align Left");
			var uiGraph = graphEditor as UIElementGraph;

			float minX = selectedNodes.Min(n => n.position.x);

			foreach(var node in selectedNodes) {
				var rect = node.position;
				rect.x = minX;
				SelectionCommandUtility.UpdateNodePosition(node, rect, uiGraph);
			}

			graphEditor.Refresh();
		}

		public override bool IsValidNode(Node source) {
			return graphData.selectedNodes.Count() > 1;
		}
	}

	public class AlignCenterHorizontalCommand : NodeMenuCommand {
		public override string name => "Align / Align Center (Horizontal)";

		public override void OnClick(Node source, Vector2 mousePosition) {
			var selectedNodes = graphData.selectedNodes.ToList();
			if(selectedNodes.Count < 2) return;

			uNodeEditorUtility.RegisterUndo(graphData.owner, "Align Center");
			var uiGraph = graphEditor as UIElementGraph;

			float minX = float.MaxValue;
			float maxX = float.MinValue;

			foreach(var node in selectedNodes) {
				float w = SelectionCommandUtility.GetWidth(node, uiGraph);
				if(node.position.x < minX) minX = node.position.x;
				if(node.position.x + w > maxX) maxX = node.position.x + w;
			}

			float centerX = minX + (maxX - minX) / 2.0f;

			foreach(var node in selectedNodes) {
				float w = SelectionCommandUtility.GetWidth(node, uiGraph);
				var rect = node.position;
				rect.x = centerX - w / 2.0f;
				SelectionCommandUtility.UpdateNodePosition(node, rect, uiGraph);
			}

			graphEditor.Refresh();
		}

		public override bool IsValidNode(Node source) {
			return graphData.selectedNodes.Count() > 1;
		}
	}

	public class AlignRightCommand : NodeMenuCommand {
		public override string name => "Align / Align Right";

		public override void OnClick(Node source, Vector2 mousePosition) {
			var selectedNodes = graphData.selectedNodes.ToList();
			if(selectedNodes.Count < 2) return;

			uNodeEditorUtility.RegisterUndo(graphData.owner, "Align Right");
			var uiGraph = graphEditor as UIElementGraph;

			float maxX = selectedNodes.Max(n => n.position.x + SelectionCommandUtility.GetWidth(n, uiGraph));

			foreach(var node in selectedNodes) {
				float w = SelectionCommandUtility.GetWidth(node, uiGraph);
				var rect = node.position;
				rect.x = maxX - w;
				SelectionCommandUtility.UpdateNodePosition(node, rect, uiGraph);
			}

			graphEditor.Refresh();
		}

		public override bool IsValidNode(Node source) {
			return graphData.selectedNodes.Count() > 1;
		}
	}

	public class AlignTopCommand : NodeMenuCommand {
		public override string name => "Align / Align Top";

		public override void OnClick(Node source, Vector2 mousePosition) {
			var selectedNodes = graphData.selectedNodes.ToList();
			if(selectedNodes.Count < 2) return;

			uNodeEditorUtility.RegisterUndo(graphData.owner, "Align Top");
			var uiGraph = graphEditor as UIElementGraph;

			float minY = selectedNodes.Min(n => n.position.y);

			foreach(var node in selectedNodes) {
				var rect = node.position;
				rect.y = minY;
				SelectionCommandUtility.UpdateNodePosition(node, rect, uiGraph);
			}

			graphEditor.Refresh();
		}

		public override bool IsValidNode(Node source) {
			return graphData.selectedNodes.Count() > 1;
		}
	}

	public class AlignMiddleVerticalCommand : NodeMenuCommand {
		public override string name => "Align / Align Middle (Vertical)";

		public override void OnClick(Node source, Vector2 mousePosition) {
			var selectedNodes = graphData.selectedNodes.ToList();
			if(selectedNodes.Count < 2) return;

			uNodeEditorUtility.RegisterUndo(graphData.owner, "Align Middle");
			var uiGraph = graphEditor as UIElementGraph;

			float minY = float.MaxValue;
			float maxY = float.MinValue;

			foreach(var node in selectedNodes) {
				float h = SelectionCommandUtility.GetHeight(node, uiGraph);
				if(node.position.y < minY) minY = node.position.y;
				if(node.position.y + h > maxY) maxY = node.position.y + h;
			}

			float centerY = minY + (maxY - minY) / 2.0f;

			foreach(var node in selectedNodes) {
				float h = SelectionCommandUtility.GetHeight(node, uiGraph);
				var rect = node.position;
				rect.y = centerY - h / 2.0f;
				SelectionCommandUtility.UpdateNodePosition(node, rect, uiGraph);
			}

			graphEditor.Refresh();
		}

		public override bool IsValidNode(Node source) {
			return graphData.selectedNodes.Count() > 1;
		}
	}

	public class AlignBottomCommand : NodeMenuCommand {
		public override string name => "Align / Align Bottom";

		public override void OnClick(Node source, Vector2 mousePosition) {
			var selectedNodes = graphData.selectedNodes.ToList();
			if(selectedNodes.Count < 2) return;

			uNodeEditorUtility.RegisterUndo(graphData.owner, "Align Bottom");
			var uiGraph = graphEditor as UIElementGraph;

			float maxY = selectedNodes.Max(n => n.position.y + SelectionCommandUtility.GetHeight(n, uiGraph));

			foreach(var node in selectedNodes) {
				float h = SelectionCommandUtility.GetHeight(node, uiGraph);
				var rect = node.position;
				rect.y = maxY - h;
				SelectionCommandUtility.UpdateNodePosition(node, rect, uiGraph);
			}

			graphEditor.Refresh();
		}

		public override bool IsValidNode(Node source) {
			return graphData.selectedNodes.Count() > 1;
		}
	}

	public class DistributeHorizontallyCommand : NodeMenuCommand {
		public override string name => "Align / Distribute Horizontally";

		public override void OnClick(Node source, Vector2 mousePosition) {
			var selectedNodes = graphData.selectedNodes.ToList();
			if(selectedNodes.Count < 3) return;

			uNodeEditorUtility.RegisterUndo(graphData.owner, "Distribute Horizontally");
			var uiGraph = graphEditor as UIElementGraph;

			var sorted = selectedNodes.Select(node => {
				float w = SelectionCommandUtility.GetWidth(node, uiGraph);
				return new {
					Node = node,
					CenterX = node.position.x + w / 2.0f,
					Width = w
				};
			}).OrderBy(item => item.CenterX).ToList();

			int count = sorted.Count;
			float firstCenter = sorted[0].CenterX;
			float lastCenter = sorted[count - 1].CenterX;
			float step = (lastCenter - firstCenter) / (count - 1);

			for(int i = 1; i < count - 1; i++) {
				var item = sorted[i];
				var rect = item.Node.position;
				rect.x = (firstCenter + i * step) - item.Width / 2.0f;
				SelectionCommandUtility.UpdateNodePosition(item.Node, rect, uiGraph);
			}

			graphEditor.Refresh();
		}

		public override bool IsValidNode(Node source) {
			return graphData.selectedNodes.Count() > 2;
		}
	}

	public class DistributeVerticallyCommand : NodeMenuCommand {
		public override string name => "Align / Distribute Vertically";

		public override void OnClick(Node source, Vector2 mousePosition) {
			var selectedNodes = graphData.selectedNodes.ToList();
			if(selectedNodes.Count < 3) return;

			uNodeEditorUtility.RegisterUndo(graphData.owner, "Distribute Vertically");
			var uiGraph = graphEditor as UIElementGraph;

			var sorted = selectedNodes.Select(node => {
				float h = SelectionCommandUtility.GetHeight(node, uiGraph);
				return new {
					Node = node,
					CenterY = node.position.y + h / 2.0f,
					Height = h
				};
			}).OrderBy(item => item.CenterY).ToList();

			int count = sorted.Count;
			float firstCenter = sorted[0].CenterY;
			float lastCenter = sorted[count - 1].CenterY;
			float step = (lastCenter - firstCenter) / (count - 1);

			for(int i = 1; i < count - 1; i++) {
				var item = sorted[i];
				var rect = item.Node.position;
				rect.y = (firstCenter + i * step) - item.Height / 2.0f;
				SelectionCommandUtility.UpdateNodePosition(item.Node, rect, uiGraph);
			}

			graphEditor.Refresh();
		}

		public override bool IsValidNode(Node source) {
			return graphData.selectedNodes.Count() > 2;
		}
	}
}
