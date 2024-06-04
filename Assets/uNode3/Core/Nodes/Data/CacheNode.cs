using UnityEngine;
using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

namespace MaxyGames.UNode.Nodes {
	[NodeMenu("Data", "Cache", hasFlowInput = true, hasFlowOutput = true)]
	public class CacheNode : FlowAndValueNode {
		public SerializedType type = SerializedType.None;
		public bool compactView;

		public ValueInput target { get; set; }

		protected override void OnRegister() {
			base.OnRegister();
			target = ValueInput(nameof(target), () => type?.type ?? typeof(object)).SetName("");
			output.isVariable = true;
		}

		public override Type ReturnType() {
			return target.ValueType;
		}

		public override bool CanSetValue() {
			return true;
		}

		public override Type GetNodeIcon() {
			return typeof(TypeIcons.DatabaseIcon);
		}

		public override string GetTitle() => "Variable: " + name;

		public override string GetRichName() {
			return $"Cache Value: {target.GetRichName()}";
		}

		protected override void OnExecuted(Flow flow) {
			flow.SetPortData(output, ReflectionUtils.ValuePassing(target.GetValue(flow)));
		}

		public override void OnGeneratorInitialize() {
			var name = this.name;
			if(CG.CanDeclareLocal(output, exit)) {
				name = CG.RegisterLocalVariable(this.name, ReturnType());
				if(ReturnType() == target.ValueType) {
					CG.RegisterPort(enter, () => {
						return CG.Flow(
							"var " + name.CGSet(target.CGValue()),
							CG.FlowFinish(enter, exit)
						);
					});
				}
				else {
					var type = CG.Type(ReturnType());
					CG.RegisterPort(enter, () => {
						return CG.Flow(
							type + " " + name.CGSet(target.CGValue()),
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