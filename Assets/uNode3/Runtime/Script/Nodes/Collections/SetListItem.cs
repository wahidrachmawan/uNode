using UnityEngine;
using System.Collections;
using System.Linq;

namespace MaxyGames.UNode.Nodes {
    [NodeMenu("Collections.List", "Set Item", icon = typeof(IList))]
	public class SetListItem : FlowNode {
		public ValueInput target { get; set; }
		public ValueInput index { get; set; }
		public ValueInput value { get; set; }

		protected override void OnRegister() {
			base.OnRegister();
			target = ValueInput(nameof(target), typeof(IList)).SetName("List");
			index = ValueInput(nameof(index), typeof(int));
			value = ValueInput(nameof(value), () => target.ValueType?.ElementType());
		}

		protected override void OnExecuted(Flow flow) {
			var val = target.GetValue<IList>(flow);
			val[index.GetValue<int>(flow)] = value.GetValue(flow);
		}

		protected override string GenerateFlowCode() {
			return CG.Flow(
				CG.Set(CG.AccessElement(target, CG.Value(index), true), CG.Value(value)),
				CG.FlowFinish(enter, exit)
			);
		}

		public override string GetTitle() {
			return "Set Item";
		}

		public override string GetRichName() {
			return target.GetRichName().Add($".Set({index.GetRichName()}, {value.GetRichName()}");
		}
	}
}