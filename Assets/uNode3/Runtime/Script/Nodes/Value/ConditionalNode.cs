using UnityEngine;

namespace MaxyGames.UNode.Nodes {
    [NodeMenu("Operator", "Conditional {?:}", typeof(object), inputs = new[] { typeof(bool) })]
	public class ConditionalNode : ValueNode {
		public ValueInput condition { get; set; }
		public ValueInput onTrue { get; set; }
		public ValueInput onFalse { get; set; }

		protected override void OnRegister() {
			base.OnRegister();
			condition = ValueInput(nameof(condition), typeof(bool));
			onTrue = ValueInput(nameof(onTrue), typeof(object), MemberData.Null);
			onFalse = ValueInput(nameof(onFalse), typeof(object), MemberData.Null);
		}

		public override System.Type ReturnType() {
			if(onTrue.isAssigned || onFalse.isAssigned) {
				try {
					if(onTrue.isAssigned) {
						return onTrue.ValueType;
					} else {
						return onFalse.ValueType;
					}
				}
				catch { }
			}
			return typeof(object);
		}

		public override object GetValue(Flow flow) {
			if(condition.isAssigned) {
				return condition.GetValue<bool>(flow) ? onTrue.GetValue(flow) : onFalse.GetValue(flow);
			}
			throw new System.Exception();
		}

		protected override string GenerateValueCode() {
			if(condition.isAssigned) {
				return "(" + CG.Value(condition) + " ? " + CG.Value(onTrue) + " : " + CG.Value(onFalse) + ")";
			}
			throw new System.Exception("Condition is unassigned.");
		}

		public override string GetTitle() {
			return "?:";
		}

		public override string GetRichName() {
			return $"? {condition.GetRichName()} {onTrue.GetRichName()} : {onFalse.GetRichName()}";
		}
	}
}