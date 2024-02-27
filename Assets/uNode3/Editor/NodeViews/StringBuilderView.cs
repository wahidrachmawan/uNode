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
	[NodeCustomEditor(typeof(Nodes.StringBuilderNode))]
	public class StringBuilderView : BaseNodeView {
		protected override void InitializeView() {
			base.InitializeView();
			Nodes.StringBuilderNode node = targetNode as Nodes.StringBuilderNode;
			ControlView control = new ControlView();
			control.Add(new Button(() => {
				if(node.stringValues.Count > 0) {
					RegisterUndo();
					node.stringValues.RemoveAt(node.stringValues.Count - 1);
					MarkRepaint();
				}
			}) { text = "-" });
			control.Add(new Button(() => {
				RegisterUndo();
				node.stringValues.Add(new Nodes.StringBuilderNode.Data());
				node.Register();
				MarkRepaint();
			}) { text = "+" });
			AddControl(Direction.Input, control);
		}
	}
}