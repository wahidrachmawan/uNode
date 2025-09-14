using UnityEngine;
using System.Collections;

namespace MaxyGames.UNode.Nodes {
	[NodeMenu("Collections.List", "Add Item", icon = typeof(IList), hasFlowInput = true, hasFlowOutput = true/*, inputs = new[] { typeof(IList) }*/)]
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