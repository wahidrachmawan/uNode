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
	[NodeCustomEditor(typeof(Nodes.NodeSwitch))]
	public class NodeSwitchView : BaseNodeView {
		FilterAttribute filter = new FilterAttribute() {
			ValidTargetType = MemberData.TargetType.Values,
			InvalidTargetType=MemberData.TargetType.Null,
			UnityReference=false,
		};
		PortView defaultPort;

		public override bool ShowExpandButton() {
			return true;
		}

		protected override void InitializeView() {
			InitializePrimaryPort(flowOutput: false);
			Nodes.NodeSwitch node = targetNode as Nodes.NodeSwitch;
			AddInputValuePort(new ValueInputData(node.target));
			var targetType = node.target.ValueType;
			if(node.target != null && node.target.isAssigned && targetType != null) {
				filter.SetType(targetType);
			}
			bool isVertical = node.useVerticalLayout && flowLayout == Orientation.Vertical;
			for(int i = 0; i < node.datas.Count; i++) {
				int x = i;
				if(targetType != null) {
					var data = node.datas[i];
					if(data.value.isAssigned && data.value.type != targetType) {
						if(data.value.type.IsCastableTo(targetType) && data.value.Get(null) != null) {
							data.value = MemberData.CreateFromValue(Operator.Convert(data.value.Get(null), targetType), targetType);
						}
						else {
							data.value = MemberData.CreateValueFromType(targetType);
						}
					}
					ControlView controlType = new ControlView();
					if(isVertical) {
						controlType.Add(new Label(x.ToString()));
					}
					controlType.Add(new UIControl.MemberControl(
							new ControlConfig() {
								owner = this,
								type = typeof(MemberData),
								value = data.value,
								onValueChanged = (obj) => {
									data.value = obj as MemberData;
									nodeObject.Register();
									UpdateEdgeLabels();
								},
								filter = filter,
							}
						));
					var port = AddOutputFlowPort(new FlowOutputData(data.flow), isVertical ? Orientation.Vertical : Orientation.Horizontal);
					if(isVertical) {
						port.SetName(x.ToString());
						AddControl(Direction.Input, controlType);
					}
					else {
						port.SetName("");
						port.SetControl(controlType);
					}
					port.userData = data;
				}
			}
			if(targetType != null) {
				ControlView control = new ControlView();
				control.style.alignSelf = Align.Center;
				control.Add(new Button(() => {
					if(node.datas.Count > 0) {
						RegisterUndo();
						//Re-register the node to apply the changes
						node.Register();
						node.datas.RemoveAt(node.datas.Count - 1);
						MarkRepaint();
					}
				}) { text = "-" });
				control.Add(new Button(() => {
					RegisterUndo();
					node.datas.Add(new Nodes.NodeSwitch.Data() {
						value = new MemberData(ReflectionUtils.CreateInstance(node.target.ValueType)),
					});
					//Re-register the node to apply the changes
					node.Register();
					MarkRepaint();
				}) { text = "+" });
				AddControl(Direction.Input, control);
			}

			{//Default
				var member = node.defaultTarget;
				defaultPort = AddOutputFlowPort(new FlowOutputData(node.defaultTarget), isVertical ? Orientation.Vertical : Orientation.Horizontal);
			}
			AddOutputFlowPort(new FlowOutputData(node.exit));
		}

		public override void OnValueChanged() {
			//Ensure to repaint every value changed.
			MarkRepaint();
			UpdateEdgeLabels();
		}

		public override void InitializeEdge() {
			base.InitializeEdge();
			UpdateEdgeLabels();
		}

		void UpdateEdgeLabels() {
			foreach(var port in outputPorts) {
				if(port != defaultPort && port.userData is Nodes.NodeSwitch.Data data) {
					var label = data.value.GetDisplayName(DisplayKind.Default);
					foreach(var edge in port.GetValidEdges()) {
						edge.SetEdgeLabel(label);
					}
				}
			}
		}
	}
}