using UnityEngine;
using System.Collections;
using System.Linq;

namespace MaxyGames.UNode.Nodes {
    [NodeMenu("Collections.List", "Remove Item", icon = typeof(IList))]
	public class RemoveListItem : FlowNode {
		public ValueInput target { get; set; }
		public ValueInput value { get; set; }

		protected override void OnRegister() {
			base.OnRegister();
			target = ValueInput(nameof(target), typeof(IList)).SetName("List");
			value = ValueInput(nameof(value), () => target.ValueType?.GetElementType());
		}

		protected override void OnExecuted(Flow flow) {
			target.GetValue<IList>(flow).Remove(value.GetValue(flow));
		}

		protected override string GenerateFlowCode() {
			return CG.Flow(
				CG.FlowInvoke(target.CGValue(), "Remove", value.CGValue()),
				CG.FlowFinish(enter, exit)
			);
		}

		public override string GetTitle() {
			return "Remove Item";
		}

		public override string GetRichName() {
			return target.GetRichName().Add($".Remove({value.GetRichName()})");
		}
	}
}