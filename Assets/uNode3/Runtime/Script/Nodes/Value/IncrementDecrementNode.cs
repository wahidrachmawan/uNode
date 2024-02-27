using UnityEngine;

namespace MaxyGames.UNode.Nodes {
	[NodeMenu("Data", "Increment-Decrement", inputs = new[] { typeof(int), typeof(float), typeof(double), typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(long), typeof(ulong), typeof(uint) })]
	public class IncrementDecrementNode : FlowAndValueNode {
		public bool isDecrement;
		public bool isPrefix;

		[System.NonSerialized]
		public ValueInput target;

		protected override void OnRegister() {
			base.OnRegister();
			target = ValueInput(nameof(target), typeof(object), MemberData.None);
			target.filter = new FilterAttribute() { SetMember = true };
		}

		protected override void OnExecuted(Flow flow) {
			nodeObject.GetPrimaryValue(flow);
		}

		public override System.Type ReturnType() {
			if(target.isAssigned) {
				try {
					return target.ValueType;
				}
				catch { }
			}
			return typeof(object);
		}

		public override object GetValue(Flow flow) {
			object obj;
			if(isPrefix) {
				if(isDecrement) {
					obj = Operator.Decrement(target.GetValue(flow));
				} else {
					obj = Operator.Increment(target.GetValue(flow));
				}
				target.SetValue(flow, obj);
			} else {
				obj = target.GetValue(flow);
				if(isDecrement) {
					target.SetValue(flow, Operator.Decrement(obj));
				} else {
					target.SetValue(flow, Operator.Increment(obj));
				}
			}
			return obj;
		}

		protected override string GenerateFlowCode() {
			return CG.Flow(GenerateValueCode() + CG.FlowFinish(enter, exit));
		}

		protected override string GenerateValueCode() {
			if(target.isAssigned) {
				if(isPrefix) {
					if(isDecrement) {
						return "--(" + CG.Value(target) + ")";
					}
					return "++(" + CG.Value(target) + ")";
				}
				else {
					if(isDecrement) {
						return "(" + CG.Value(target) + ")--";
					}
					return "(" + CG.Value(target) + ")++";
				}
			}
			throw new System.Exception("Target is unassigned");
		}

		public override string GetTitle() {
			if(isPrefix) {
				if(isDecrement) {
					return "--$Decrement";
				}
				return "++$Increment";
			}
			if(isDecrement) {
				return "$Decrement--";
			}
			return "$Increment++";
		}

		public override string GetRichName() {
			if(isPrefix) {
				if(isDecrement) {
					return "--" + target.GetRichName();
				}
				return "++" + target.GetRichName();
			}
			if(isDecrement) {
				return target.GetRichName() + "--";
			}
			return target.GetRichName() + "++";
		}
	}
}