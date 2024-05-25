using UnityEngine;
using System.Collections;
using System.Linq;

namespace MaxyGames.UNode.Nodes {
    [NodeMenu("Collections.List", "Get Item", icon = typeof(IList))]
	public class GetListItem : ValueNode {
		public ValueInput target { get; set; }
		public ValueInput index { get; set; }

		protected override void OnRegister() {
			base.OnRegister();
			target = ValueInput(nameof(target), typeof(IList)).SetName("List");
			index = ValueInput(nameof(index), typeof(int));
		}

		public override System.Type ReturnType() {
			if(target.isAssigned) {
				return target.ValueType.ElementType();
			}
			return typeof(object);
		}

		public override object GetValue(Flow flow) {
			var val = target.GetValue<IList>(flow);
			return val[index.GetValue<int>(flow)];
		}

		protected override string GenerateValueCode() {
			return CG.AccessElement(target, CG.Value(index));
		}

		public override string GetTitle() {
			return "Get Item";
		}

		public override string GetRichName() {
			return target.GetRichName().Add($".Get({index.GetRichName()})");
		}
	}
}