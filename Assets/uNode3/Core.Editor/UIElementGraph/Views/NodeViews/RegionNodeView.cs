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
			//base.OnCustomStyleResolved(style);
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