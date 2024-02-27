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
	[NodeCustomEditor(typeof(Nodes.SelectNode))]
	public class SelectNodeView : BaseNodeView {
		protected override void InitializeView() {
			InitializePrimaryPort();
			Nodes.SelectNode node = targetNode as Nodes.SelectNode;
			AddInputValuePort(new ValueInputData(node.target));
			if(!node.target.isAssigned || node.target.type == null)
				return;
			AddInputValuePort(new ValueInputData(node.defaultTarget));
			FilterAttribute valueFilter = new FilterAttribute(node.target.ValueType) {
				ValidTargetType = MemberData.TargetType.Values
			};
			for(int i = 0; i < node.datas.Count; i++) {
				var data = node.datas[i];
				var port = AddInputValuePort(new ValueInputData(data.port));
				port.SetName("");
				if(data.value == null || !data.value.isAssigned) {
					data.value = MemberData.Default(node.target.ValueType);
				} else if(data.value.startType != node.target.ValueType) {
					if(data.value.startType.IsCastableTo(node.target.ValueType)) {
						data.value = MemberData.CreateFromValue(data.value.Get(null, node.target.ValueType), node.target.ValueType);
					} else {
						data.value = MemberData.Default(node.target.ValueType);
					}
				}
				ControlView controlType = new ControlView();
				controlType.Add(new UIControl.MemberControl(
					new ControlConfig() {
						owner = this,
						value = data.value,
						onValueChanged = (obj) => {
							data.value = obj as MemberData;
						},
						filter = valueFilter,
					},
					true
				));
				port.SetControl(controlType, true);
			}
			ControlView control = new ControlView();
			control.style.alignSelf = Align.Center;
			control.Add(new Button(() => {
				if(node.datas.Count > 0) {
					RegisterUndo();
					node.datas.RemoveAt(node.datas.Count - 1);
					node.Register();
					MarkRepaint();
				}
			}) { text = "-" });
			control.Add(new Button(() => {
				RegisterUndo();
				node.datas.Add(new Nodes.SelectNode.Data() {
					value = MemberData.Default(node.target.ValueType)
				});
				node.Register();
				MarkRepaint();
			}) { text = "+" });
			AddControl(Direction.Input, control);
		}
	}
}