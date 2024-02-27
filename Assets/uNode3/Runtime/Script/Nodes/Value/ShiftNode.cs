using UnityEngine;

namespace MaxyGames.UNode.Nodes {
    [NodeMenu("Operator", "Shift {<<} {>>}")]
	public class ShiftNode : ValueNode {
		public ShiftType operatorType = ShiftType.LeftShift;
		[System.NonSerialized]
		public ValueInput targetA;
		[System.NonSerialized]
		public ValueInput targetB;

		protected override void OnRegister() {
			base.OnRegister();
			targetA = ValueInput(nameof(targetA), typeof(object));
			targetB = ValueInput(nameof(targetB), typeof(int));
		}

		public override System.Type ReturnType() {
			if(targetA.isAssigned && targetB.isAssigned) {
				try {
					object obj = uNodeHelper.ShiftOperator(
						ReflectionUtils.CreateInstance(targetA.ValueType),
						default(int), operatorType);
					if(!object.ReferenceEquals(obj, null)) {
						return obj.GetType();
					}
				}
				catch { }
			}
			return typeof(object);
		}

		public override object GetValue(Flow flow) {
			return uNodeHelper.ShiftOperator(targetA.GetValue(flow), targetB.GetValue<int>(flow), operatorType);
		}

		protected override string GenerateValueCode() {
			if(targetA.isAssigned && targetB.isAssigned) {
				return CG.Operator(CG.Value(targetA),
					CG.Value(targetB), operatorType).AddFirst("(").Add(")");
			}
			throw new System.Exception("Target is unassigned.");
		}

		public override string GetTitle() {
			return operatorType.ToString();
		}

		public override string GetRichName() {
			return CG.Operator(targetA.GetRichName(), targetB.GetRichName(), operatorType);
		}
	}
}