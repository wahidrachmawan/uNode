using UnityEngine;

namespace MaxyGames.UNode.Nodes {
    [NodeMenu("Operator", "Bitwise {|} {&} {^}")]
	public class BitwiseNode : ValueNode {
		public BitwiseType operatorType = BitwiseType.Or;

		public ValueInput targetA { get; set; }
		public ValueInput targetB { get; set; }

		protected override void OnRegister() {
			base.OnRegister();
			targetA = ValueInput(nameof(targetA), typeof(object)).SetName("");
			targetB = ValueInput(nameof(targetB), typeof(object)).SetName("");
			targetA.filter = new FilterAttribute(typeof(int), typeof(uint), typeof(long), typeof(short), typeof(byte), typeof(ulong), typeof(System.Enum));
			targetB.filter = new FilterAttribute(typeof(int), typeof(uint), typeof(long), typeof(short), typeof(byte), typeof(ulong), typeof(System.Enum));
		}

		public override System.Type ReturnType() {
			if(targetA.isAssigned && targetB.isAssigned) {
				try {
					var val = ReflectionUtils.CreateInstance(targetA.ValueType);
					object obj = uNodeHelper.BitwiseOperator(
						val,
						val, operatorType);
					if(!object.ReferenceEquals(obj, null)) {
						return obj.GetType();
					}
				}
				catch { }
			}
			return typeof(object);
		}

		public override object GetValue(Flow flow) {
			return uNodeHelper.BitwiseOperator(targetA.GetValue(flow), targetB.GetValue(flow), operatorType);
		}

		protected override string GenerateValueCode() {
			if(targetA.isAssigned && targetB.isAssigned) {
				return CG.Operator(
					CG.Value(targetA),
					CG.Value(targetB), 
					operatorType).Wrap();
			}
			throw new System.Exception("Target is unassigned.");
		}

		public override string GetTitle() {
			return operatorType.ToString();
		}

		public override string GetRichName() {
			return CG.Operator(targetA.GetRichName(), targetB.GetRichName(), operatorType);
		}

		public override System.Type GetNodeIcon() {
			switch(operatorType) {
				case BitwiseType.And:
					return typeof(TypeIcons.BitwiseAndIcon);
				case BitwiseType.Or:
					return typeof(TypeIcons.BitwiseOrIcon);
			}
			return base.GetNodeIcon();
		}
	}
}