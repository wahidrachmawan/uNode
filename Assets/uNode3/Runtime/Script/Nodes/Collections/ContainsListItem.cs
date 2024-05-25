using UnityEngine;
using System.Collections;
using System.Linq;

namespace MaxyGames.UNode.Nodes {
    [NodeMenu("Collections.List", "Contains Item", typeof(bool), icon = typeof(IList))]
	public class ContainsListItem : ValueNode {
		public ValueInput target { get; set; }
		public ValueInput value { get; set; }

		protected override void OnRegister() {
			base.OnRegister();
			target = ValueInput(nameof(target), typeof(IList)).SetName("List");
			value = ValueInput(nameof(value), () => target.ValueType?.ElementType());
		}

		public override System.Type ReturnType() {
			return typeof(bool);
		}

		public override object GetValue(Flow flow) {
			return target.GetValue<IList>(flow).Contains(value.GetValue(flow));
		}

		protected override string GenerateValueCode() {
			return CG.Invoke(target.CGValue(), "Contains", value.CGValue());
		}

		public override string GetTitle() {
			return "Contains Item";
		}

		public override string GetRichName() {
			return target.GetRichName().Add($".Contains({value.GetRichName()})");
		}
	}
}