using System;
using UnityEngine;

namespace MaxyGames.UNode.Nodes {
	[NodeMenu("Math.Time", "Per Second", typeof(float), inputs = new[] { typeof(float), typeof(Vector2), typeof(Vector3) })]
	public class PerSecond : ValueNode {
		public ValueInput input;
		public bool unscaledTime;

		protected override void OnRegister() {
			base.OnRegister();
			input = ValueInput(nameof(input), typeof(object), MemberData.CreateFromValue(1));
			input.filter = new FilterAttribute(typeof(float), typeof(Vector2), typeof(Vector3), typeof(Vector4));
		}

		public override System.Type ReturnType() {
			if(input.isAssigned) {
				return input.ValueType;
			}
			return typeof(float);
		}

		public override object GetValue(Flow flow) {
			if(unscaledTime) {
				if(input.isAssigned) {
					return Operator.Multiply(input.GetValue(flow), Time.unscaledDeltaTime);
				}
				return 1 * Time.unscaledDeltaTime;
			} else {
				if(input.isAssigned) {
					return Operator.Multiply(input.GetValue(flow), Time.deltaTime);
				}
				return 1 * Time.deltaTime;
			}
		}

		protected override string GenerateValueCode() {
			if(unscaledTime) {
				if(input.isAssigned) {
					return CG.Arithmetic(input.CGValue(), typeof(Time).CGAccess(nameof(Time.unscaledDeltaTime)), ArithmeticType.Multiply);
				}
				return CG.Arithmetic(1.CGValue(), typeof(Time).CGAccess(nameof(Time.unscaledDeltaTime)), ArithmeticType.Multiply);
			}
			else {
				if(input.isAssigned) {
					return CG.Arithmetic(input.CGValue(), typeof(Time).CGAccess(nameof(Time.deltaTime)), ArithmeticType.Multiply);
				}
				return CG.Arithmetic(1.CGValue(), typeof(Time).CGAccess(nameof(Time.deltaTime)), ArithmeticType.Multiply);
			}
		}

		public override Type GetNodeIcon() {
			return typeof(TypeIcons.ClockIcon);
		}

		public override string GetTitle() {
			return "Per Second";
		}
	}
}