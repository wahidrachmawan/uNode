using System;
using UnityEngine;

namespace MaxyGames.UNode.Nodes {
	[NodeMenu("Operator", "Coalescing {??}")]
	public class CoalescingNode : ValueNode {
		public ValueInput input { get; set; }
		public ValueInput fallback { get; set; }

		protected override void OnRegister() {
			base.OnRegister();
			input = ValueInput(nameof(input), typeof(object));
			fallback = ValueInput(nameof(fallback), () => input.ValueType);
		}

		public override System.Type ReturnType() {
			if(input.isAssigned || fallback.isAssigned) {
				try {
					if(input.isAssigned) {
						return input.ValueType;
					} else {
						return fallback.ValueType;
					}
				}
				catch { }
			}
			return typeof(object);
		}

		public override object GetValue(Flow flow) {
			return input.GetValue(flow) ?? fallback.GetValue(flow);
		}

		protected override string GenerateValueCode() {
			return CG.Value(input) + " ?? " + CG.Value(fallback);
		}

		public override Type GetNodeIcon() {
			return typeof(TypeIcons.NullTypeIcon);
		}

		public override string GetTitle() {
			return "Null Coalesce";
		}

		public override string GetRichName() {
			return input.GetRichName() + " ?? " + fallback.GetRichName();
		}
	}
}