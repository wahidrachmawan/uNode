using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;

namespace MaxyGames.UNode.Editors {
	[NodeCustomEditor(typeof(MultipurposeNode))]
	public class MultipurposeNodeView : BaseNodeView {
		bool isCompact;

		void OpenFunction() {
			var node = targetNode as MultipurposeNode;
			if(node.target.targetType == MemberData.TargetType.uNodeFunction) {
				var obj = node.target.startItem.GetReferenceValue() as Function;
				if(obj != null) {
					uNodeEditor.Open(obj.graphContainer, obj);
				}
			}
		}

		protected override void InitializeView() {
			MultipurposeNode node = nodeObject.node as MultipurposeNode;
			titleContainer.RegisterCallback<MouseDownEvent>(e => {
				if(e.button == 0 && e.clickCount == 2) {
					OpenFunction();
				}
			});
			{//Initialize ports
				if(node.target.targetType != MemberData.TargetType.Constructor) {
					foreach(var port in nodeObject.FlowInputs) {
						if(port == nodeObject.primaryFlowInput) {
							if(nodeObject.primaryValueOutput == null || nodeObject.primaryValueOutput.isConnected == false || port.isConnected || nodeObject.primaryFlowOutput.isConnected) {
								AddPrimaryInputFlow();
							}
							continue;
						}
						AddInputFlowPort(new FlowInputData(port));
					}
					foreach(var port in nodeObject.FlowOutputs) {
						if(port == nodeObject.primaryFlowOutput) {
							if(nodeObject.primaryValueOutput == null || nodeObject.primaryValueOutput.isConnected == false || port.isConnected || nodeObject.primaryFlowInput.isConnected) {
								AddPrimaryOutputFlow();
							}
							continue;
						}
						AddOutputFlowPort(new FlowOutputData(port));
					}
				}
				foreach(var port in nodeObject.ValueOutputs) {
					if(port == nodeObject.primaryValueOutput) {
						AddPrimaryOutputValue();
						if(flowLayout == Orientation.Horizontal) {
							uNodeThreadUtility.Queue(() => primaryOutputValue?.BringToFront());
						}
						continue;
					}
					AddOutputValuePort(new ValueOutputData(port));
				}
				if(node.instance != null) {
					AddInputValuePort(new ValueInputData(node.instance));
				}
				if(node.parameters != null) {
					foreach(var param in node.parameters) {
						if(param.input != null) {
							AddInputValuePort(new ValueInputData(param.input));
						}
					}
				}
				if(node.member.initializers?.Count > 0) {
					AddControl(Direction.Input, new Label("Initializers"));
					foreach(var init in node.member.initializers) {
						if(init.isComplexInitializer) {
							for(int i = 0; i < init.elementInitializers.Length; i++) {
								AddInputValuePort(new ValueInputData(init.elementInitializers[i].port));
							}
						}
						else {
							AddInputValuePort(new ValueInputData(init.port));
						}
					}
				}
			}
			isCompact = false;
			EnableInClassList(ussClassCompact, false);
			EnableInClassList(ussClassCompactValue, false);
			EnableInClassList(ussClassCompactValueMinimalize, false);
			if(primaryOutputFlow != null || outputControls.Count != 0) {
				if(uNodeUtility.preferredDisplay == DisplayKind.Partial) {
					ConstructCompactTitle(node.instance);
				}
				return;
			}
			if(node.target.targetType == MemberData.TargetType.Values && node.target.type != null) {
				var control = new UIControl.MemberControl(new ControlConfig() {
					value = node.target,
					owner = this,
					type = node.target.type,
					onValueChanged = (o) => {
						RegisterUndo();
						node.target = o as MemberData;
					},
				});
				AddControl(Direction.Input, control);
				{
					isCompact = true;
					var config = control.config;
					if(config.filter != null) {
						config.filter.ValidTargetType = MemberData.TargetType.Values;
					}
					if(config.type == typeof(string)) {
						control.AddToClassList("multiline");
						control.style.height = new StyleLength(StyleKeyword.Auto);
						control.style.flexGrow = 1;
					}
					control.UpdateControl();
					if(inputControls.Count == 1) {
						EnableInClassList(ussClassCompactValue, true);
						if(primaryOutputValue != null) {
							primaryOutputValue.SetName("");
						}
					}
				}
			}
			else if(inputControls.Count == 0) {
				int valueOutputCount = outputPorts.Count(x => x.orientation == Orientation.Horizontal);
				int valueInputCount = inputPorts.Count(x => x.orientation == Orientation.Horizontal);
				if(valueOutputCount == 1 && valueInputCount == 0) {
					isCompact = true;
					EnableInClassList(ussClassCompact, true);
					if(primaryOutputValue != null) {
						primaryOutputValue.SetName("");
					}
				}
			}
			if(isCompact && primaryOutputValue != null) {
				EnableInClassList(ussClassCompactValueMinimalize, true);
				Color c = uNodePreference.GetColorForType(primaryOutputValue.GetPortType());
				c.a = 0.8f;
				elementTypeColor = c;
				if(UIElementUtility.Theme.coloredNodeBorder) {
					border.style.SetBorderColor(c);
				}
			}
			else if(uNodeUtility.preferredDisplay == DisplayKind.Partial) {
				ConstructCompactTitle(node.instance);
				if(primaryOutputValue != null) {
					EnableInClassList(ussClassCompactTitle, true);
					Color c = uNodePreference.GetColorForType(primaryOutputValue.GetPortType());
					c.a = 0.8f;
					elementTypeColor = c;
					if(UIElementUtility.Theme.coloredNodeBorder) {
						border.style.SetBorderColor(c);
					}
				}
			}
		}

		protected override void OnCustomStyleResolved(ICustomStyle style) {
			base.OnCustomStyleResolved(style);
			if(isCompact && primaryOutputValue != null) {
				Color c = uNodePreference.GetColorForType(primaryOutputValue.GetPortType());
				c.a = 0.8f;
				elementTypeColor = c;
				if(UIElementUtility.Theme.coloredNodeBorder) {
					border.style.SetBorderColor(c);
				}
			}
		}
	}
}
