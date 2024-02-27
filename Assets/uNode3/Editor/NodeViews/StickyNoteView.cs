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
	public class StickyNoteView : BaseNodeView {
		protected Label comment;

		public override void Initialize(UGraphView owner, NodeObject node) {
			this.owner = owner;
			this.nodeObject = node;
			title = targetNode.GetTitle();
			titleButtonContainer.RemoveFromHierarchy();

			this.AddStyleSheet("uNodeStyles/NativeStickyNote");
			AddToClassList("sticky-note");

			comment = new Label(node.comment);
			inputContainer.Add(comment);
			elementTypeColor = Color.yellow;

			titleContainer.RegisterCallback<MouseDownEvent>((e) => {
				if(e.clickCount == 2) {
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
			comment.RegisterCallback<MouseDownEvent>((e) => {
				if(e.clickCount == 2) {
					ActionPopupWindow.ShowWindow(Vector2.zero, node.comment,
						(ref object obj) => {
							object str = EditorGUILayout.TextArea(obj as string);
							if(obj != str) {
								obj = str;
								node.comment = obj as string;
								uNodeGUIUtility.GUIChanged(node, UIChangeType.Average);
							}
						}, 300, 200).ChangePosition(owner.GetScreenMousePosition(e)).headerName = "Edit description";
					e.StopImmediatePropagation();
				}
			});

			//this.SetSize(new Vector2(node.editorRect.width, node.editorRect.height));
			SetPosition(targetNode.position);
			RefreshPorts();
		}

		public override void ReloadView() {
			comment.text = targetNode.comment;
			base.ReloadView();
		}
	}
}