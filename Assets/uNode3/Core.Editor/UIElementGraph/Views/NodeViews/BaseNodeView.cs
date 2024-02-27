using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;

namespace MaxyGames.UNode.Editors {
	public class BaseNodeView : UNodeView {

		protected VisualElement debugView;
		protected VisualElement debugStateView;

		private Image compactIcon = null;

		#region  Initialization
		/// <summary>
		/// Initialize once on created.
		/// </summary>
		/// <param name="owner"></param>
		/// <param name="node"></param>
		public override void Initialize(UGraphView owner, NodeObject node) {
			this.AddStyleSheet("uNodeStyles/NativeNodeStyle");
			this.AddStyleSheet(UIElementUtility.Theme.nodeStyle);
			base.Initialize(owner, node);
			this.ExecuteAndScheduleAction(DoUpdate, 500);
		}

		/// <summary>
		/// Initialize the primary port.
		/// </summary>
		protected void InitializePrimaryPort(bool flowInput = true, bool flowOutput = true, bool valueOutput = true) {
			if(flowInput && nodeObject.primaryFlowInput != null) {
				AddPrimaryInputFlow();
			}
			if(flowOutput && nodeObject.primaryFlowOutput != null) {
				AddPrimaryOutputFlow();
			}
			if(valueOutput && nodeObject.primaryValueOutput != null) {
				AddPrimaryOutputValue();
				if(flowLayout == Orientation.Horizontal) {
					uNodeThreadUtility.Queue(() => primaryOutputValue.BringToFront());
				}
			}
		}

		/// <summary>
		/// Called inside ReloadView
		/// </summary>
		protected virtual void InitializeView() {
			if(nodeObject.node is ISuperNode) {
				titleContainer.RegisterCallback<MouseDownEvent>(e => {
					if(e.button == 0 && e.clickCount == 2) {
						owner.graph.graphData.currentCanvas = targetNode.nodeObject;
						owner.graph.Refresh();
						owner.graph.UpdatePosition();
					}
				});
			}
			foreach(var port in nodeObject.FlowInputs) {
				if(port == nodeObject.primaryFlowInput) {
					AddPrimaryInputFlow();
					continue;
				}
				AddInputFlowPort(new FlowInputData(port));
			}
			foreach(var port in nodeObject.FlowOutputs) {
				if(port == nodeObject.primaryFlowOutput) {
					AddPrimaryOutputFlow();
					continue;
				}
				AddOutputFlowPort(new FlowOutputData(port));
			}
			foreach(var port in nodeObject.ValueInputs) {
				AddInputValuePort(new ValueInputData(port));
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
		}
		#endregion

		#region Functions
		public override void ReloadView() {
			try {
				base.ReloadView();
				if(UIElementGraph.richText) {
					title = nodeObject.GetRichTitle();
				}
				else {
					title = nodeObject.GetTitle();
				}
				if(titleIcon != null)
					titleIcon.image = uNodeEditorUtility.GetTypeIcon(nodeObject.GetNodeIcon());
				InitializeView();
			}
			catch(Exception ex) {
				Debug.LogException(ex);
			}
			Teleport(nodeObject.position);

			#region Debug
			if(debugView == null && primaryInputFlow != null && (Application.isPlaying || GraphDebug.Breakpoint.HasBreakpoint(nodeObject))) {
				debugView = new VisualElement() {
					name = "debug-container"
				};
				//titleButtonContainer.Add(debugView);
				
				this.Add(debugView);
			}
			if(Application.isPlaying && primaryInputFlow != null) {
				this.RegisterRepaintAction(() => {
					var debugData = owner.graph.GetDebugInfo();
					if(debugData != null) {
						var nodeDebug = debugData.GetDebugValue(primaryInputFlow.GetPortValue<FlowInput>());
						if(nodeDebug != null) {
							var layout = new Rect(5, -8, 8, 8);
							switch(nodeDebug.nodeState) {
								case StateType.Success:
									GUI.DrawTexture(layout, uNodeEditorUtility.MakeTexture(1, 1,
										Color.Lerp(
											UIElementUtility.Theme.nodeRunningColor,
											UIElementUtility.Theme.nodeSuccessColor,
											(GraphDebug.debugTime - nodeDebug.calledTime) * GraphDebug.transitionSpeed * 4)
										)
									);
									break;
								case StateType.Failure:
									GUI.DrawTexture(layout, uNodeEditorUtility.MakeTexture(1, 1,
										Color.Lerp(
											UIElementUtility.Theme.nodeRunningColor,
											UIElementUtility.Theme.nodeFailureColor,
											(GraphDebug.debugTime - nodeDebug.calledTime) * GraphDebug.transitionSpeed * 4)
										)
									);
									break;
								case StateType.Running:
									GUI.DrawTexture(layout, uNodeEditorUtility.MakeTexture(1, 1, UIElementUtility.Theme.nodeRunningColor));
									break;
							}
						}
					}
				});
			}
			if(debugView != null) {
				this.ExecuteAndScheduleAction(() => {
					if(!this.IsVisible())
						return;
					bool hasBreakpoint = GraphDebug.Breakpoint.HasBreakpoint(nodeObject);
					if(hasBreakpoint) {
						//debugView.style.backgroundColor = UIElementUtility.Theme.breakpointColor;
						debugView.style.width = new StyleLength(StyleKeyword.Null);
						debugView.style.height = new StyleLength(StyleKeyword.Null);
						debugView.style.position = Position.Relative;
						debugView.StretchToParentSize();
						debugView.visible = true;
					}
					else {
						debugView.style.position = Position.Absolute;
						debugView.style.width = 0;
						debugView.style.height = 0;
						debugView.visible = false;
					}
				}, 50);
			}
			#endregion

			#region Node Styles
			if(border != null) {
				border.SetToNoClipping();
				if(!isBlock) {
					int flowInputCount = inputPorts.Count((p) => p.isFlow);
					int flowOutputCount = outputPorts.Count((p) => p.isFlow);
					if(flowInputCount + flowOutputCount > 0) {
						bool flag = outputPorts.Count((p) => p.isFlow && !string.IsNullOrEmpty(p.GetName())) == 0;
						border.EnableInClassList(ussClassBorderFlowNode, true);
						if(flowOutputCount == 0 || flag) {
							border.EnableInClassList(ussClassBorderOnlyInput, true);
							border.EnableInClassList(ussClassBorderOnlyOutput, false);
						} else if(flowInputCount == 0) {
							border.EnableInClassList(ussClassBorderOnlyInput, false);
							border.EnableInClassList(ussClassBorderOnlyOutput, true);
						} else {
							border.EnableInClassList(ussClassBorderOnlyInput, false);
							border.EnableInClassList(ussClassBorderOnlyOutput, false);
						}
						portInputContainer.EnableInClassList("flow", flowInputCount > 0);
					} else {
						border.EnableInClassList(ussClassBorderFlowNode, false);
					}
				} else {
					border.EnableInClassList(ussClassBorderFlowNode, false);
				}
			}
			#endregion

			if(titleIcon != null) {
				titleIcon.EnableInClassList("ui-hidden", titleIcon.image == null);
			}

			RefreshPorts();
			if(ShowExpandButton()) {
				expanded = nodeObject.nodeExpanded;
			}
		}

		/// <summary>
		/// Initialize default compact node style
		/// </summary>
		protected void ConstructCompactStyle(bool displayIcon = true, bool compactInput = true, bool compactOutput = true) {
			if(isBlock) return;
			compactIcon?.RemoveFromHierarchy();
			EnableInClassList("compact-value", true);
			var element = this.Q("top");
			if(element != null) {
				if(displayIcon) {
					compactIcon = new Image() {
						image = uNodeEditorUtility.GetTypeIcon(nodeObject.GetNodeIcon())
					};
					compactIcon.AddToClassList("compact-icon");
					element.Insert(2, compactIcon);
				}
				if(compactOutput) {
					foreach(var p in outputPorts) {
						p.portName = "";
					}
				}
				if(compactInput) {
					foreach(var p in inputPorts) {
						p.portName = "";
					}
				}
			}
		}

		/// <summary>
		/// Construct compact title node, invoke it after initializing view
		/// </summary>
		/// <param name="inputValuePort"></param>
		/// <param name="outputValuePort"></param>
		protected void ConstructCompactTitle(ValueInput inputPort, ControlView control = null) {
			var firstPort = inputPorts.FirstOrDefault(p => p != null && p.GetPortID() == inputPort?.id);
			ConstructCompactTitle(firstPort, control);
		}


		/// <summary>
		/// Construct compact title node, invoke it after initializing view
		/// </summary>
		/// <param name="inputValuePort"></param>
		/// <param name="outputValuePort"></param>
		protected void ConstructCompactTitle(string inputValuePortID, ControlView control = null) {
			var firstPort = inputPorts.FirstOrDefault(p => p != null && p.GetPortID() == inputValuePortID);
			ConstructCompactTitle(firstPort, control);
		}

		/// <summary>
		/// Construct compact title node, invoke it after initializing view
		/// </summary>
		/// <param name="inputPort"></param>
		/// <param name="control"></param>
		protected void ConstructCompactTitle(PortView inputPort, ControlView control = null) {
			if(inputPort != null) {
				inputPort.RemoveFromHierarchy();
				inputPort.AddToClassList("compact-input");
				titleContainer.Insert(0, inputPort);
				EnableInClassList("compact-node", true);
			}
			if(control != null) {
				control.RemoveFromHierarchy();
				control.AddToClassList("compact-control");
				titleContainer.Add(control);
				EnableInClassList("compact-node", true);
			}
			if(primaryOutputValue != null) {
				primaryOutputValue.RemoveFromHierarchy();
				primaryOutputValue.AddToClassList("compact-output");
				titleContainer.Add(primaryOutputValue);
				EnableInClassList("compact-node", true);
			}
		}
		#endregion

		#region Callbacks & Overrides
		public override void SetPosition(Rect newPos) {
			// if (newPos != targetNode.editorRect && preference.snapNode) {
			// 	float range = preference.snapRange;
			// 	newPos.x = NodeEditorUtility.SnapTo(newPos.x, range);
			// 	newPos.y = NodeEditorUtility.SnapTo(newPos.y, range);
			// 	if (preference.snapToPin && owner.selection.Count == 1) {
			// 		var connectedPort = inputPorts.Where((p) => p.connected).ToList();
			// 		bool hFlag = false;
			// 		bool vFalg = false;
			// 		var snapRange = preference.snapToPinRange / uNodePreference.nodeGraph.zoomScale;
			// 		for (int i = 0; i < connectedPort.Count; i++) {
			// 			var edges = connectedPort[i].GetEdges();
			// 			if (connectedPort[i].orientation == Orientation.Vertical) {
			// 				if (vFalg)
			// 					continue;
			// 				foreach (var e in edges) {
			// 					if (e != null) {
			// 						float distanceToPort = e.input.GetGlobalCenter().x - e.output.GetGlobalCenter().x;
			// 						if (Mathf.Abs(distanceToPort) <= snapRange && Mathf.Abs(newPos.x - layout.x) <= snapRange) {
			// 							newPos.x = layout.x - distanceToPort;
			// 							vFalg = true;
			// 							break;
			// 						}
			// 					}
			// 				}
			// 			} else {
			// 				//if(hFlag || vFalg)
			// 				//	continue;
			// 				//foreach(var e in edges) {
			// 				//	if(e != null) {
			// 				//		float distanceToPort = e.edgeControl.to.y - e.edgeControl.from.y;
			// 				//		if(Mathf.Abs(distanceToPort) <= preference.snapToPinRange &&
			// 				//			Mathf.Abs(newPos.y - layout.y) <= preference.snapToPinRange) {
			// 				//			newPos.y = layout.y - distanceToPort;
			// 				//			hFlag = true;
			// 				//			break;
			// 				//		}
			// 				//	}
			// 				//}
			// 			}
			// 		}
			// 		connectedPort = outputPorts.Where((p) => p.connected).ToList();
			// 		for (int i = 0; i < connectedPort.Count; i++) {
			// 			var edges = connectedPort[i].GetEdges();
			// 			if (connectedPort[i].orientation == Orientation.Vertical) {
			// 				//if(vFalg)
			// 				//	continue;
			// 				//foreach(var e in edges) {
			// 				//	if(e != null) {
			// 				//		float distanceToPort = e.edgeControl.to.x - e.edgeControl.from.x;
			// 				//		if(Mathf.Abs(distanceToPort) <= preference.snapToPinRange &&
			// 				//			Mathf.Abs(newPos.x - layout.x) <= preference.snapToPinRange) {
			// 				//			newPos.x = layout.x + distanceToPort;
			// 				//			break;
			// 				//		}
			// 				//	}
			// 				//}
			// 			} else {
			// 				if (hFlag || vFalg)
			// 					continue;
			// 				foreach (var e in edges) {
			// 					if (e != null) {
			// 						float distanceToPort = e.input.GetGlobalCenter().y - e.output.GetGlobalCenter().y;
			// 						if (Mathf.Abs(distanceToPort) <= snapRange && Mathf.Abs(newPos.y - layout.y) <= snapRange) {
			// 							newPos.y = layout.y + distanceToPort;
			// 							break;
			// 						}
			// 					}
			// 				}
			// 			}
			// 		}
			// 	}
			// }
			float xPos = newPos.x - nodeObject.position.x;
			float yPos = newPos.y - nodeObject.position.y;

			base.SetPosition(newPos);
			nodeObject.position = newPos;
			//Handle carry nodes
			if(owner.selection.Count == 1 && owner.selection.Contains(this) && owner.currentEvent != null && isBlock == false) {
				if((xPos != 0 || yPos != 0)) {
					var preference = uNodePreference.GetPreference();
					if(preference.carryNodes) {
						if(owner.currentEvent.modifiers.HasFlags(EventModifiers.Control | EventModifiers.Command)) {
							return;
						}
					} else {
						if(!owner.currentEvent.modifiers.HasFlags(EventModifiers.Control | EventModifiers.Command)) {
							return;
						}
					}
					List<UNodeView> nodes = UIElementUtility.Nodes.FindNodeToCarry(this);
					foreach(var n in nodes.Distinct()) {
						if(n != null) {
							Rect rect = n.nodeObject.position;
							rect.x += xPos;
							rect.y += yPos;
							n.Teleport(rect);
						}
					}
				}
			}
		}

		/// <summary>
		/// Do update every 0.5 second.
		/// </summary>
		protected virtual void DoUpdate() {
			if(!this.IsVisible())
				return;
			#region Errors
			var errors = GraphUtility.ErrorChecker.GetErrorMessages(nodeObject, InfoType.Error);
			if(errors != null && errors.Any()) {
				System.Text.StringBuilder sb = new System.Text.StringBuilder();
				foreach(var error in errors) { 
					if(sb.Length > 0) {
						sb.AppendLine();
						sb.AppendLine();
					}
					sb.Append("-" + uNodeEditorUtility.RemoveHTMLTag(error.message));
				}
				UpdateError(sb.ToString());
			} else {
				UpdateError(string.Empty);
			}
			#endregion
		}
		#endregion
	}

	public class BlockControl : VisualElement {
		public Label label;
		public ControlView control;

		public BlockControl(string label, ControlView control) {
			style.flexDirection = FlexDirection.Row;

			this.label = new Label(ObjectNames.NicifyVariableName(label));
			this.label.style.width = 100;
			Add(this.label);
			if(control != null) {
				this.control = control;
				Add(control);
			}
		}

		public BlockControl(Label label, ControlView control) {
			style.flexDirection = FlexDirection.Row;

			this.label = label;
			this.label.style.width = 100;
			Add(this.label);
			if(control != null) {
				this.control = control;
				Add(control);
			}
		}
	}

	public class ControlView : VisualElement {
		public ValueControl control;

		// public new bool visible {
		// 	get {
		// 		return !this.IsFaded();
		// 	}
		// 	set {
		// 		this.SetOpacity(value);
		// 	}
		// }

		public ControlView() { }

		public ControlView(VisualElement visualElement, bool autoLayout = false) {
			Add(visualElement);
			ToggleLayout(autoLayout);
		}

		public ControlView(ValueControl valueControl, bool autoLayout = false) {
			Add(valueControl);
			control = valueControl;
			ToggleLayout(autoLayout);
		}

		public void ToggleLayout(bool value) {
			EnableInClassList("Layout", value);
			if(control != null)
				control.EnableInClassList("Layout", value);
		}
	}
}