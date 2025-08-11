using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Linq;

namespace MaxyGames.UNode.Editors {
	[NodeCustomEditor(typeof(Nodes.NodeRegion))]
	public class RegionNodeView : BaseNodeView, IElementResizable {
		protected Label comment;
		protected VisualElement horizontalDivider;
		protected List<NodeObject> nodes;
		protected List<Vector2> nodePositions;
		private Vector2 oldNodePosition;

		private float bodyTransparent = 0.05f;
		private float titleTransparent = 0.3f;

		private static CustomStyleProperty<float> s_BodyProperty = new CustomStyleProperty<float>("--region-body-transparent");
		private static CustomStyleProperty<float> s_TitleProperty = new CustomStyleProperty<float>("--region-title-transparent");

		public override void Initialize(UGraphView owner, NodeObject node) {
			this.owner = owner;
			nodeObject = node;
			title = targetNode.GetTitle();
			titleButtonContainer.RemoveFromHierarchy();
			this.AddStyleSheet("uNodeStyles/NativeRegionStyle");
			border = this.Q("node-border");
			horizontalDivider = border.Q("contents").Q("divider");

			comment = new Label(node.comment);
			inputContainer.Add(comment);

			titleContainer.RegisterCallback<MouseDownEvent>((e) => {
				if(e.clickCount == 2 && e.button == 0) {
					ActionPopupWindow.Show(
						node.name,
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
					nodes = new List<NodeObject>();
					foreach(var n in owner.graphEditor.nodes) {
						if(n == null) continue;
						nodes.Add(n);
						if(n.node is Nodes.StateNode state) {
							foreach(var tr in state.GetTransitions()) {
								if(tr == null) continue;
								nodes.Add(tr);
							}
						}
					}
					nodes.RemoveAll((n) => {
						if(n == null || n == targetNode)
							return false;
						Rect targetRect;
						if(owner.nodeViewsPerNode.TryGetValue(n, out var view)) {
							targetRect = view.GetPosition();
						}
						else {
							targetRect = n.position;
						}
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
			mainContainer.style.backgroundColor = new Color(region.nodeColor.r, region.nodeColor.g, region.nodeColor.b, bodyTransparent);
			titleContainer.style.backgroundColor = new Color(region.nodeColor.r, region.nodeColor.g, region.nodeColor.b, titleTransparent);
		}

		private const float m_titleSize = 25;
		private Label m_titleLabel;
		public override void OnZoomUpdated(float zoom) {
			if(m_titleLabel == null) {
				m_titleLabel = titleContainer.Q<Label>("title-label");
			}
			m_titleLabel.style.fontSize = m_titleSize / (Mathf.Clamp(zoom + 0.1f, 0.1f, 1));
		}

		public override void ReloadView() {
			UpdateUI();
			base.ReloadView();
		}

		protected override void OnCustomStyleResolved(ICustomStyle style) {
			if(style.TryGetValue(s_BodyProperty, out var body)) {
				bodyTransparent = body;
			}
			if(style.TryGetValue(s_TitleProperty, out var title)) {
				titleTransparent = title;
			}
			UpdateUI();
			base.OnCustomStyleResolved(style);
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