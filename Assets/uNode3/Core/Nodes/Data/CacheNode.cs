using UnityEngine;
using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

namespace MaxyGames.UNode.Nodes {
	[NodeMenu("Data", "Cache", hasFlowInput = true, hasFlowOutput = true, icon = typeof(TypeIcons.DatabaseIcon))]
	[Description("Cache value for later use ( local variable )")]
	public class CacheNode : FlowAndValueNode {
		public SerializedType type = SerializedType.None;
		public bool compactView;

		public ValueInput target { get; set; }

		protected override void OnRegister() {
			base.OnRegister();
			target = ValueInput(nameof(target), () => type?.type ?? typeof(object)).SetName("");
			output.isVariable = true;
			output.SetTitle(() => name);
		}

		protected override Type ReturnType() {
			if(type.isAssigned) {
				return type.type ?? target.ValueType;
			}
			return target.ValueType;
		}

		protected override bool CanSetValue() {
			return true;
		}

		public override string GetTitle() => "Variable: " + name;

		public override string GetRichName() {
			return $"{uNodeUtility.WrapTextWithKeywordColor("var")} {name} = {target.GetRichName()}";
		}

		protected override void OnExecuted(Flow flow) {
			flow.SetPortData(output, ReflectionUtils.ValuePassing(target.GetValue(flow)));
		}

		public override void OnGeneratorInitialize() {
			var name = this.name;
			if(CG.CanDeclareLocal(output, exit)) {
				name = CG.RegisterLocalVariable(this.name, ReturnType());
				if(type.typeKind != SerializedTypeKind.None && target.ValueType == type) {
					CG.RegisterPort(enter, () => {
						var right = target.CGValue();
						if(right == CG.Null) {
							return CG.Flow(
								CG.Type(type.type ?? ReturnType()) + " " + name.CGSet(right),
								CG.FlowFinish(enter, exit)
							);
						}
						return CG.Flow(
							"var " + name.CGSet(target.CGValue()),
							CG.FlowFinish(enter, exit)
						);
					});
				}
				else {
					CG.RegisterPort(enter, () => {
						return CG.Flow(
							CG.Type(type.type ?? ReturnType()) + " " + name.CGSet(target.CGValue()),
							CG.FlowFinish(enter, exit)
						);
					});
				}
			}
			else {
				name = CG.RegisterPrivateVariable(this.name, ReturnType());
				CG.RegisterPort(enter, () => {
					return CG.Flow(
						name.CGSet(target.CGValue()),
						CG.FlowFinish(enter, exit)
					);
				});
			}
			CG.RegisterPort(output, () => name);
		}
	}
}