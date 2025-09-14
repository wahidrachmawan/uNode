using UnityEngine;
using System.Collections;
using System.Linq;

namespace MaxyGames.UNode.Nodes {
    [NodeMenu("Collections.List", "Insert Item", icon = typeof(IList), hasFlowInput = true, hasFlowOutput = true/*, inputs = new[] { typeof(IList) }*/)]
	public class InsertListItem : FlowAndValueNode {
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
			target.GetValue<IList>(flow).Insert(index.GetValue<int>(flow), value.GetValue(flow));
		}

		protected override string GenerateFlowCode() {
			return CG.Flow(
				CG.FlowInvoke(target.CGValue(), "Insert", index.CGValue(), value.CGValue()),
				CG.FlowFinish(enter, exit)
			);
		}

		public override string GetTitle() {
			return "Insert Item";
		}

		public override string GetRichName() {
			return target.GetRichName().Add($".Insert({index.GetRichName()}, {value.GetRichName()}");
		}

		public override void CheckError(ErrorAnalyzer analyzer) {
			base.CheckError(analyzer);
			if(target.isAssigned) {
				var type = target.ValueType;
				if(type.IsArray) {
					analyzer.RegisterError(this, "Cannot remove array element");
				}
			}
		}
	}
}