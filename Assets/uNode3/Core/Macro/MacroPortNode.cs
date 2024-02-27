using System;
using UnityEngine;

namespace MaxyGames.UNode.Nodes {
	public class MacroPortNode : Node {
		[Hide]
		public PortKind kind;
		[Filter(OnlyGetType =true)]
		public SerializedType type = typeof(object);

		[NonSerialized]
		public FlowInput enter;
		[NonSerialized]
		public FlowOutput exit;
		[NonSerialized]
		public ValueInput input;
		[NonSerialized]
		public ValueOutput output;

		protected override void OnRegister() {
			switch(kind) {
				case PortKind.FlowInput: {
					exit = PrimaryFlowOutput(nameof(exit));
					break;
				}
				case PortKind.FlowOutput: {
					enter = PrimaryFlowInput(nameof(enter), (flow) => {
						var exit = flow.GetElementData<FlowOutput>(this);
						if(exit == null)
							throw new Exception("The exit port is still un-initialized, make sure that the parent is a macro node.");
						flow.Next(exit);
					});
					enter.isCoroutine = IsCoroutine;
					break;
				}
				case PortKind.ValueInput: {
					output = PrimaryValueOutput(nameof(output));
					break;
				}
				case PortKind.ValueOutput: {
					input = ValueInput(nameof(input), ReturnType);
					break;
				}
			}
		}

		public override object GetValue(Flow flow) {
			return flow.GetElementData<ValueInput>(this).GetValue(flow);
		}

		public override void SetValue(Flow flow, object value) {
			flow.GetElementData<ValueInput>(this).SetValue(flow, value);
		}

		public override bool CanGetValue() {
			return kind == PortKind.ValueInput;
		}

		public override bool CanSetValue() {
			return kind == PortKind.ValueInput;
		}

		public override string GetTitle() {
			return name;
		}

		public override Type GetNodeIcon() {
			if(kind == PortKind.FlowInput || kind == PortKind.ValueInput) {
				return typeof(TypeIcons.InputIcon);
			}
			return typeof(TypeIcons.OutputIcon);
		}

		public override Type ReturnType() {
			if(kind == PortKind.ValueInput || kind == PortKind.ValueOutput) {
				if(type.isAssigned) {
					return type.type;
				} else {
					return typeof(object);
				}
			}
			return base.ReturnType();
		}

		public bool IsCoroutine() {
			if(kind == PortKind.FlowInput && exit.isConnected) {
				return exit.IsCoroutine();
			}
			return false;
		}

		public override void CheckError(ErrorAnalyzer analizer) {
			base.CheckError(analizer);
			var parent = nodeObject.parent;
			if((parent as NodeObject)?.node is not IMacro && nodeObject.graphContainer is not IMacroGraph) {
				analizer.RegisterError(this, "Invalid node context, this node are valid only for macro graph or macro node.");
			}
		}
	}
}