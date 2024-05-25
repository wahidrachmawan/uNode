using UnityEngine;
using System.Collections;

namespace MaxyGames.UNode.Nodes {
    [NodeMenu("Collections.List", "Add Item", icon = typeof(IList))]
	public class AddListItem : FlowNode {
		public ValueInput target { get; set; }
		public ValueInput value { get; set; }

		protected override void OnRegister() {
			base.OnRegister();
			target = ValueInput(nameof(target), typeof(IList)).SetName("List");
			value = ValueInput(nameof(value), () => target.ValueType?.ElementType());
		}

		protected override void OnExecuted(Flow flow) {
			target.GetValue<IList>(flow).Add(value.GetValue(flow));
		}

		protected override string GenerateFlowCode() {
			return CG.Flow(
				CG.FlowInvoke(target.CGValue(), "Add", value.CGValue()),
				CG.FlowFinish(enter, exit)
			);
		}

		public override string GetTitle() {
			return "Add Item";
		}

		public override string GetRichName() {
			return target.GetRichName().Add($".Add({value.GetRichName()})");
		}
	}
}