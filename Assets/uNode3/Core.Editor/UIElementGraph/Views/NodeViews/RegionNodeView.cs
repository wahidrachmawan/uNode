using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace MaxyGames.UNode.Editors {
	[NodeCustomEditor(typeof(Nodes.NodeRegion))]
	public class RegionNodeView : BaseNodeView, IElementResizable {
		protected Label comment;
		protected VisualElement horizontalDivider;
		protected List<NodeObject> nodes;
		protected List<Vector2> nodePositions;
		private Vector2 oldNodePosition;

		public override void Initialize(UGraphView owner, NodeObject node) {
			this.owner = owner;
			nodeObject = node;
			title = targetNode.GetTitle();
			titleButtonContainer.RemoveFromHierarchy();
			this.AddStyleSheet("uNodeStyles/NativeRegionStyle");
			var border = this.Q("node-border");
			border.style.overflow = Overflow.Visible;
			horizontalDivider = border.Q("contents").Q("divider");

			comment = new Label(node.comment);
			inputContainer.Add(comment);

			titleContainer.RegisterCallback<MouseDownEvent>((e) => {
				if(e.clickCount == 2 && e.button == 0) {
					ActionPopupWindow.ShowWindow(Vector2.zero, node.name,
						(ref object obj) => {
							object str = EditorGUILayout.TextField(obj as string);
							if(obj != str) {
								obj = str;
								node.name = obj as string;
								uNodeGUIUtility.GUIChanged(node, UIChangeType.Average);
							}
						}).ChangePosition(owner.GetScreenMousePosition(e)).headerName = "Rename title";
					e.StopImmediatePropagation();
				}
			});
			RegisterCallback<MouseDownEvent>((e) => {
				if(e.button == 0) {
					nodes = new List<NodeObject>(owner.graph.nodes);
					nodes.RemoveAll((n) => {
						if(n == null || n == targetNode)
							return false;
						var targetRect = n.position;
						var regionRect = targetNode.position;
						if(regionRect.Overlaps(targetRect)) {
							if(targetRect.x < regionRect.x) {
								return true;
							}
							if(targetRect.y < regionRect.y) {
								return true;
							}
							if(targetRect.x + targetRect.width > regionRect.x + regionRect.width) {
								return true;
							}
							if(targetRect.y + targetRect.height > regionRect.y + regionRect.height) {
								return true;
							}
							return false;
						}
						return true;
					});
					oldNodePosition = GetPosition().position;
					nodePositions = new List<Vector2>();
					for(int i = 0; i < nodes.Count; i++) {
						nodePositions.Add(nodes[i].position.position);
					}
				}
			});

			Add(new ResizableElement());
			this.SetSize(new Vector2(node.position.width, node.position.height));
			Teleport(targetNode.position);
			ReloadView();
			RefreshPorts();
		}

		public void OnResized() {
			Teleport(layout);
			owner.MarkRepaint(this);
		}

		public void OnStartResize() {
			nodes = null;
		}

		public override void UpdateUI() {
			var region = targetNode as Nodes.NodeRegion;
			comment.text = targetNode.comment;
			elementTypeColor = region.nodeColor;
			mainContainer.style.SetBorderColor(new Color(region.nodeColor.r, region.nodeColor.g, region.nodeColor.b, 0.9f));
			horizontalDivider.style.backgroundColor = new Color(region.nodeColor.r, region.nodeColor.g, region.nodeColor.b, 0.9f);
			mainContainer.style.backgroundColor = new Color(region.nodeColor.r, region.nodeColor.g, region.nodeColor.b, 0.05f);
			titleContainer.style.backgroundColor = new Color(region.nodeColor.r, region.nodeColor.g, region.nodeColor.b, 0.3f);
		}

		public override void ReloadView() {
			UpdateUI();
			base.ReloadView();
		}

		public override void SetPosition(Rect newPos) {
			if(newPos != targetNode.position) {
				// if(uNodePreference.GetPreference().snapNode) {
				// 	float range = uNodePreference.GetPreference().snapRange;
				// 	newPos.x = NodeEditorUtility.SnapTo(newPos.x, range);
				// 	newPos.y = NodeEditorUtility.SnapTo(newPos.y, range);
				// }
				if(nodes != null && newPos.width == targetNode.position.width && newPos.height == targetNode.position.height) {
					float xPos = newPos.x - oldNodePosition.x;
					float yPos = newPos.y - oldNodePosition.y;
					if(xPos != 0 || yPos != 0) {
						for(int n = 0; n < nodes.Count; n++) {
							UNodeView node;
							if(owner.nodeViewsPerNode.TryGetValue(nodes[n], out node)) {
								nodes[n].position.x = nodePositions[n].x + xPos;
								nodes[n].position.y = nodePositions[n].y + yPos;
								node.Teleport(nodes[n].position);
							}
						}
						// if(uNodePreference.GetPreference().enableSnapping) {
						// 	float range = uNodePreference.GetPreference().snappingRange;
						// 	for(int n = 0; n < nodes.Count; n++) {
						// 		UNodeView node;
						// 		if(owner.nodeViewsPerNode.TryGetValue(nodes[n], out node)) {
						// 			nodes[n].editorRect.x = NodeEditorUtility.SnapTo(nodes[n].editorRect.x + xPos, range);
						// 			nodes[n].editorRect.y = NodeEditorUtility.SnapTo(nodes[n].editorRect.y + yPos, range);
						// 			node.SetPosition(nodes[n].editorRect);
						// 		}
						// 	}
						// } else {
						// }
					}
				}
			}
			base.SetPosition(newPos);
		}

		//public override bool HitTest(Vector2 localPoint) {
		//	return titleContainer.ContainsPoint(this.ChangeCoordinatesTo(titleContainer, localPoint));
		//}

		public override bool ContainsPoint(Vector2 localPoint) {
			return titleContainer.ContainsPoint(this.ChangeCoordinatesTo(titleContainer, localPoint));
		}

		public override bool Overlaps(Rect rectangle) {
			return titleContainer.Overlaps(this.ChangeCoordinatesTo(titleContainer, rectangle));
		}
	}
}