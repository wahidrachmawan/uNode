using UnityEngine;
using System.Collections;
using System.Linq;

namespace MaxyGames.UNode.Nodes {
    [NodeMenu("Collections", "First Item", icon = typeof(IList))]
	public class FirstItem : ValueNode {
		public ValueInput target { get; set; }

		protected override void OnRegister() {
			base.OnRegister();
			target = ValueInput(nameof(target), typeof(IEnumerable));
		}

		public override System.Type ReturnType() {
			if(target.isAssigned) {
				return target.ValueType.ElementType();
			}
			return typeof(object);
		}

		public override object GetValue(Flow flow) {
			var val = target.GetValue<IEnumerable>(flow);
			if(val is IList list) {
				return list[0];
			} else {
				return val.Cast<object>().First();
			}
		}

		protected override string GenerateValueCode() {
			var type = target.ValueType;
			if(type.IsCastableTo(typeof(IList))) {
				return CG.AccessElement(target, CG.Value(0));
			}
			//Because the function is using Linq we need to make sure that System.Linq namespaces is registered.
			CG.RegisterUsingNamespace("System.Linq");
			return CG.GenericInvoke<object>(target, "Cast").CGInvoke("First");
		}

		public override string GetTitle() {
			return "First Item";
		}

		public override string GetRichName() {
			return target.GetRichName().Add(".First");
		}
	}
}