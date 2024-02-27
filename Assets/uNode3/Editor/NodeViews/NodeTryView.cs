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
	[NodeCustomEditor(typeof(Nodes.NodeTry))]
	public class NodeTryView : BaseNodeView {
		FilterAttribute filter = new FilterAttribute(typeof(Exception)) {
			OnlyGetType = true,
			ArrayManipulator = false
		};

		protected override void InitializeView() {
			InitializePrimaryPort(flowOutput: false);
			Nodes.NodeTry node = targetNode as Nodes.NodeTry;
			AddOutputFlowPort(new FlowOutputData(node.Try));
			for(int i = 0; i < node.exceptions.Count; i++) {
				var data = node.exceptions[i];
				AddOutputValuePort(new ValueOutputData(data.value));
				ControlView controlType = new ControlView();
				//controlType.Add(new Label(x.ToString()));
				controlType.Add(new UIControl.DefaultControl(
						new ControlConfig() {
							owner = this,
							type = typeof(SerializedType),
							value = data.type,
							onValueChanged = (obj) => {
								data.type = obj as SerializedType;
							},
							filter = filter,
						},
						true
					));

				AddControl(Direction.Input, controlType);
				AddOutputFlowPort(new FlowOutputData(data.flow));
			}
			ControlView control = new ControlView();
			control.style.alignSelf = Align.Center;
			control.Add(new Button(() => {
				if(node.exceptions.Count > 0) {
					RegisterUndo();
					node.exceptions.RemoveAt(node.exceptions.Count - 1);
					node.Register();
					MarkRepaint();
				}
			}) { text = "-" });
			control.Add(new Button(() => {
				RegisterUndo();
				node.exceptions.Add(new Nodes.NodeTry.Data());
				node.Register();
				MarkRepaint();
			}) { text = "+" });
			AddControl(Direction.Input, control);
			AddOutputFlowPort(new FlowOutputData(node.Finally));
			AddOutputFlowPort(new FlowOutputData(node.exit));
		}

		public override void OnValueChanged() {
			//Ensure to repaint every value changed.
			MarkRepaint();
		}
	}
}