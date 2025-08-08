using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;
using NodeView = UnityEditor.Experimental.GraphView.Node;

namespace MaxyGames.UNode.Editors {
	[NodeCustomEditor(typeof(Nodes.StickyNote))]
	public class StickyNoteView : BaseNodeView, IElementResizable {
		protected Label comment;

		public override void Initialize(UGraphView owner, NodeObject node) {
			this.owner = owner;
			this.nodeObject = node;
			titleButtonContainer.RemoveFromHierarchy();

			this.AddStyleSheet("uNodeStyles/NativeStickyNote");
			AddToClassList("sticky-note");

			comment = new Label(node.comment);
			comment.style.whiteSpace = WhiteSpace.Normal;
			inputContainer.Add(comment);
			elementTypeColor = Color.yellow;

			titleContainer.RegisterCallback<MouseDownEvent>((e) => {
				if(e.clickCount == 2) {
					ActionPopupWindow.Show(node.name,
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
			comment.RegisterCallback<MouseDownEvent>((e) => {
				if(e.clickCount == 2) {
					ActionPopupWindow.Show(node.comment,
						(ref object obj) => {
							object str = EditorGUILayout.TextArea(obj as string);
							if(obj != str) {
								obj = str;
								node.comment = obj as string;
								uNodeGUIUtility.GUIChanged(node, UIChangeType.Average);
							}
						}).ChangePosition(owner.GetScreenMousePosition(e)).headerName = "Edit description";
					e.StopImmediatePropagation();
				}
			});

			Add(new ResizableElement());
			//this.SetSize(new Vector2(node.editorRect.width, node.editorRect.height));
			SetPosition(targetNode.position);
			RefreshPorts();
			UpdateUI();
		}

		public void OnResized() {
			Teleport(layout);
			(nodeObject.node as Nodes.StickyNote).customNodeSize = true;
		}

		void IElementResizable.OnResizeUpdate() {
			Teleport(layout);
			(nodeObject.node as Nodes.StickyNote).customNodeSize = true;
		}

		public override void UpdateUI() {
			title = targetNode.GetTitle();
			comment.text = targetNode.comment;
			if(nodeObject.node is Nodes.StickyNote note) {

				if(note.customNodeSize) {
					this.SetSize(new Vector2(nodeObject.position.width, nodeObject.position.height));
				}
				else {
					style.width = StyleKeyword.Null;
					style.height = StyleKeyword.Null;
				}

				titleContainer.EnableInClassList("ui-hidden", note.hideTitle);
				if(note.backgroundColor != Color.clear) {
					this.style.backgroundColor = note.backgroundColor;
				}
				else {
					this.style.backgroundColor = StyleKeyword.Null;
				}
				if(note.textColor != Color.clear) {
					titleLabel.style.color = note.textColor;
					comment.style.color = note.textColor;
				}
				else {
					titleLabel.style.color = StyleKeyword.Null;
					comment.style.color = StyleKeyword.Null;
				}
				if(note.titleSize > 0) {
					titleLabel.style.fontSize = note.titleSize;
					titleContainer.style.height = StyleKeyword.Auto;
				}
				else {
					titleLabel.style.fontSize = StyleKeyword.Null;
				}
				if(note.descriptionSize > 0) {
					comment.style.fontSize = note.descriptionSize;
				}
				else {
					comment.style.fontSize = StyleKeyword.Null;
				}
				//if(note.titleFont != null) {
				//	titleLabel.style.unityFontDefinition = FontDefinition.FromFont(note.titleFont);
				//}
				//if(note.descriptionFont != null) {
				//	comment.style.unityFontDefinition = FontDefinition.FromFont(note.descriptionFont);
				//}
			}
		}
	}
}