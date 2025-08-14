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
			try {
				base.Initialize(owner, node);
				this.ExecuteAndScheduleAction(DoUpdate, 500);
			}
			catch(Exception ex) {
				if(ex is GraphException) {
					Debug.LogException(ex);
				}
				else {
					Debug.LogException(new GraphException(ex, nodeObject));
				}
			}
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
					uNodeThreadUtility.Queue(() => primaryOutputValue?.BringToFront());
				}
			}
		}

		/// <summary>
		/// Called inside ReloadView
		/// </summary>
		protected virtual void OnReloadView() {
			InitializeDefaultPorts();
		}

		/// <summary>
		/// Initialize the default ports
		/// </summary>
		protected virtual void InitializeDefaultPorts() {
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
		/// <summary>
		/// Called once on setup
		/// </summary>
		protected override void OnSetup() {
			base.OnSetup();
			
			if(nodeObject.node is ISuperNode) {
				titleContainer.RegisterCallback<MouseDownEvent>(e => {
					if(e.button == 0 && e.clickCount == 2) {
						owner.graphEditor.graphData.currentCanvas = targetNode.nodeObject;
						owner.graphEditor.Refresh();
						owner.graphEditor.UpdatePosition();
					}
				});
			}
		}

		/// <summary>
		/// Called every time when view is reloaded
		/// </summary>
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
				OnReloadView();
			}
			catch(Exception ex) {
				if(ex is GraphException) {
					Debug.LogException(ex);
				}
				else {
					Debug.LogException(new GraphException(ex, nodeObject));
				}
			}
			Teleport(nodeObject.position);

			#region Debug
			if(debugView == null && primaryInputFlow != null && (Application.isPlaying || GraphDebug.Breakpoint.HasBreakpoint(nodeObject))) {
				debugView = new VisualElement() {
					name = "debug-container"
				};
				debugView.pickingMode = PickingMode.Ignore;
				//titleButtonContainer.Add(debugView);
				
				this.Add(debugView);
			}
			if(Application.isPlaying && primaryInputFlow != null) {

				int lastState = -1;
				VisualElement debugElement = null;
				void UpdateState(int state) {
					if(lastState != state || debugElement == null) {
						if(debugElement == null) {
							debugElement = new VisualElement() {
								name = "node-running-status",
								pickingMode = PickingMode.Ignore,
							};
							this.Add(debugElement);
						}
						else {
							if(lastState == 0) {
								//Running
								this.RemoveFromClassList("node-debug-running");
								debugElement.RemoveFromClassList("highlight-debug-running");
							}
							else if(lastState == 1) {
								this.RemoveFromClassList("node-debug-success");
								debugElement.RemoveFromClassList("highlight-debug-success");
							}
							else if(lastState == 2) {
								this.RemoveFromClassList("node-debug-failure");
								debugElement.RemoveFromClassList("highlight-debug-failure");
							}
						}
						if(state == 0) {
							//Running
							this.AddToClassList("node-debug-running");
							debugElement.AddToClassList("highlight-debug-running");
						}
						else if(state == 1) {
							this.AddToClassList("node-debug-success");
							debugElement.AddToClassList("highlight-debug-success");
						}
						else if(state == 2) {
							this.AddToClassList("node-debug-failure");
							debugElement.AddToClassList("highlight-debug-failure");
						}
						lastState = state;
					}
				}

				this.ScheduleActionUntil(() => {
					if(isHidden || this.IsVisible() == false) return;
					var debugData = owner.graphEditor.GetDebugInfo();
					if(debugData != null) {
						var nodeDebug = debugData.GetDebugValue(primaryInputFlow.GetPortValue<FlowInput>());
						if(nodeDebug != null) {
							var layout = new Rect(5, -8, 8, 8);
							switch(nodeDebug.nodeState) {
								case StateType.Success:
									if(GraphDebug.debugTime - nodeDebug.time > 0.1f) {
										UpdateState(1);
									}
									else {
										UpdateState(0);
									}
									//GUI.DrawTexture(layout, Texture2D.whiteTexture, ScaleMode.ScaleAndCrop, true, 0, 
									//	Color.Lerp(
									//		UIElementUtility.Theme.nodeRunningColor,
									//		UIElementUtility.Theme.nodeSuccessColor,
									//		(GraphDebug.debugTime - nodeDebug.time) * GraphDebug.transitionSpeed * 4), 0, 0);
									break;
								case StateType.Failure:
									if(GraphDebug.debugTime - nodeDebug.time > 0.1f) {
										UpdateState(2);
									}
									else {
										UpdateState(0);
									}
									//GUI.DrawTexture(layout, Texture2D.whiteTexture, ScaleMode.ScaleAndCrop, true, 0,
									//	Color.Lerp(
									//		UIElementUtility.Theme.nodeRunningColor,
									//		UIElementUtility.Theme.nodeFailureColor,
									//		(GraphDebug.debugTime - nodeDebug.time) * GraphDebug.transitionSpeed * 4), 0, 0);
									break;
								case StateType.Running:
									UpdateState(0);
									//GUI.DrawTexture(layout, Texture2D.whiteTexture, ScaleMode.ScaleAndCrop, true, 0,
									//	UIElementUtility.Theme.nodeRunningColor, 0, 0);
									break;
							}
						}
						else {
							UpdateState(-1);
						}
					}
					else {
						UpdateState(-1);
					}
				}, static () => false);
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
				//border.SetToNoClipping();
				border.style.overflow = StyleKeyword.Null;
				if(!isBlock) {
					var anyFlowInput = inputPorts.Any((p) => p.isFlow);
					var anyFlowOutput = outputPorts.Any((p) => p.isFlow);
					if(anyFlowInput || anyFlowOutput) {
						bool flag = outputPorts.Count((p) => p.isFlow && !string.IsNullOrEmpty(p.GetName())) == 0;
						border.EnableInClassList(ussClassBorderFlowNode, true);
						if(anyFlowOutput == false || flag) {
							border.EnableInClassList(ussClassBorderOnlyInput, true);
							border.EnableInClassList(ussClassBorderOnlyOutput, false);
						} else if(anyFlowInput == false) {
							border.EnableInClassList(ussClassBorderOnlyInput, false);
							border.EnableInClassList(ussClassBorderOnlyOutput, true);
						} else {
							border.EnableInClassList(ussClassBorderOnlyInput, false);
							border.EnableInClassList(ussClassBorderOnlyOutput, false);
						}
						portInputContainer.EnableInClassList("flow", anyFlowInput);
					} else {
						border.EnableInClassList(ussClassBorderFlowNode, false);
					}
					border.EnableInClassList(ussClassBorderHasInputValue, inputPorts.Any(p => p.isValue));
					border.EnableInClassList(ussClassBorderHasOutputValue, outputPorts.Any(p => p.isValue));
				}
				else {
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
			UpdateUI();
		}

		/// <summary>
		/// Initialize default compact node style
		/// </summary>
		protected void ConstructCompactStyle(bool displayIcon = true, bool compactInput = true, bool compactOutput = true, bool minimalize = false, bool hidePortIcon = false) {
			if(isBlock) return;
			compactIcon?.RemoveFromHierarchy();
			EnableInClassList(ussClassCompactValue, true);
			var element = this.Q("top");
			if(element != null) {
				if(displayIcon) {
					compactIcon = new Image() {
						image = uNodeEditorUtility.GetTypeIcon(nodeObject.GetNodeIcon())
					};
					compactIcon.AddToClassList("compact-icon");
					if(minimalize) {
						compactIcon.AddToClassList("compact-icon-minimalize");
					}
					element.Insert(2, compactIcon);
				}
				if(compactOutput) {
					foreach(var p in outputPorts) {
						p.portName = "";
						if(hidePortIcon) {
							p.AddToClassList(ussClassHidePortIcon);
						}
					}
				}
				if(compactInput) {
					foreach(var p in inputPorts) {
						p.portName = "";
						if(hidePortIcon) {
							p.AddToClassList(ussClassHidePortIcon);
						}
					}
				}
			}
		}

		/// <summary>
		/// Construct compact title node, invoke it after initializing view
		/// </summary>
		/// <param name="inputValuePort"></param>
		/// <param name="outputValuePort"></param>
		protected void ConstructCompactTitle(ValueInput inputPort, ControlView control = null, bool minimalize = false) {
			var firstPort = inputPorts.FirstOrDefault(p => p != null && p.GetPortID() == inputPort?.id);
			ConstructCompactTitle(firstPort, control, minimalize);
		}


		/// <summary>
		/// Construct compact title node, invoke it after initializing view
		/// </summary>
		/// <param name="inputValuePort"></param>
		/// <param name="outputValuePort"></param>
		protected void ConstructCompactTitle(string inputValuePortID, ControlView control = null, bool minimalize = false) {
			var firstPort = inputPorts.FirstOrDefault(p => p != null && p.GetPortID() == inputValuePortID);
			ConstructCompactTitle(firstPort, control, minimalize);
		}

		/// <summary>
		/// Construct compact title node, invoke it after initializing view
		/// </summary>
		/// <param name="inputPort"></param>
		/// <param name="control"></param>
		protected void ConstructCompactTitle(PortView inputPort, ControlView control = null, bool minimalize = false) {
			if(inputPort != null) {
				inputPort.RemoveFromHierarchy();
				inputPort.AddToClassList(ussClassCompactInput);
				titleContainer.Insert(0, inputPort);
				EnableInClassList(ussClassCompactNode, true);
				if(minimalize) {
					EnableInClassList(ussClassCompactNodeMinimalize, true);
				}
			}
			if(control != null) {
				control.RemoveFromHierarchy();
				control.AddToClassList(ussClassCompactControl);
				titleContainer.Add(control);
				EnableInClassList(ussClassCompactNode, true);
				if(minimalize) {
					EnableInClassList(ussClassCompactNodeMinimalize, true);
				}
			}
			if(primaryOutputValue != null) {
				primaryOutputValue.RemoveFromHierarchy();
				primaryOutputValue.AddToClassList(ussClassCompactOutput);
				titleContainer.Add(primaryOutputValue);
				EnableInClassList(ussClassCompactNode, true);
				if(minimalize) {
					EnableInClassList(ussClassCompactNodeMinimalize, true);
				}
			}
		}
		#endregion

		#region Callbacks & Overrides
		/// <summary>
		/// Get nodes to carry
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<UNodeView> GetCarryNodes() {
			var preference = uNodePreference.GetPreference();
			bool carry;
			if(preference.carryNodes) {
				if(owner.currentEvent.modifiers.HasFlags(EventModifiers.Control | EventModifiers.Command)) {
					carry = false;
				}
				else {
					carry = true;
				}
			}
			else {
				if(!owner.currentEvent.modifiers.HasFlags(EventModifiers.Control | EventModifiers.Command)) {
					carry = false;
				}
				else {
					carry = true;
				}
			}
			if(carry) {
				return UIElementUtility.Nodes.FindNodeToCarry(this).Distinct();
			}
			else {
				if(preference.autoCarryInputValue) {
					return UIElementUtility.Nodes.FindNodeToCarryOnlyInputs(this).Distinct();
				}
			}
			return null;
		}

		public override void SetPosition(Rect newPos) {
			float xPos = newPos.x - nodeObject.position.x;
			float yPos = newPos.y - nodeObject.position.y;

			base.SetPosition(newPos);
			nodeObject.position = newPos;
			//Handle carry nodes
			if(owner.selection.Count == 1 && owner.selection.Contains(this) && owner.currentEvent != null && isBlock == false) {
				if((xPos != 0 || yPos != 0)) {
					var nodes = GetCarryNodes();
					if(nodes != null) {
						foreach(var n in nodes) {
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