using UnityEngine;
using System.Collections;
using System.Linq;

namespace MaxyGames.UNode.Nodes {
    [NodeMenu("Collections", "Count Items", typeof(int), icon = typeof(IList))]
	public class CountItems : ValueNode {
		public ValueInput target { get; set; }

		protected override void OnRegister() {
			base.OnRegister();
			target = ValueInput(nameof(target), typeof(IEnumerable));
		}

		public override System.Type ReturnType() {
			return typeof(int);
		}

		public override object GetValue(Flow flow) {
			var val = target.GetValue<IEnumerable>(flow);
			if(val is ICollection col) {
				return col.Count;
			} else {
				return val.Cast<object>().Count();
			}
		}

		protected override string GenerateValueCode() {
			var type = target.ValueType;
			if(type.IsArray) {
				return CG.Access(target, "Length");
			} 
			else if(type.IsCastableTo(typeof(ICollection))) {
				return CG.Access(target, "Count");
			}
			//Because the function is using Linq we need to make sure that System.Linq namespaces is registered.
			CG.RegisterUsingNamespace("System.Linq");
			return CG.GenericInvoke<object>(target, "Cast").CGInvoke("Count");
		}

		public override string GetTitle() {
			return "Count Items";
		}

		public override string GetRichName() {
			return target.GetRichName().Add(".Count");
		}
	}
}