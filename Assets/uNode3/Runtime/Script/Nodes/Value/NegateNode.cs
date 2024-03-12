using System;
using UnityEngine;

namespace MaxyGames.UNode.Nodes {
    [NodeMenu("Data", "Negate {-}", inputs = new[] { typeof(int), typeof(float), typeof(double), typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(long), typeof(ulong), typeof(uint) })]
	public class NegateNode : ValueNode {
		[System.NonSerialized]
		public ValueInput target;

		protected override void OnRegister() {
			base.OnRegister();
			target = ValueInput(nameof(target), typeof(object), MemberData.None);
		}

		public override System.Type ReturnType() {
			if(target.isAssigned) {
				try {
					object obj = Operator.Negate(ReflectionUtils.CreateInstance(target.ValueType));
					if(!object.ReferenceEquals(obj, null)) {
						return obj.GetType();
					}
				}
				catch { }
			}
			return typeof(object);
		}

		public override object GetValue(Flow flow) {
			return Operator.Negate(target.GetValue(flow));
		}

		protected override string GenerateValueCode() {
			if(target.isAssigned) {
				return CG.Value(target).CGNegate();
			}
			throw new System.Exception("Target is unassigned.");
		}

		public override string GetTitle() {
			return "Negate";
		}

		public override string GetRichName() {
			return "-" + target.GetRichName();
		}

		public override Type GetNodeIcon() {
			return typeof(TypeIcons.SubtractIcon);
		}
	}
}