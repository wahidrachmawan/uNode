using UnityEngine;
using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

namespace MaxyGames.UNode.Nodes {
	[NodeMenu("Data", "Not {!}", typeof(bool), inputs = new[] { typeof(bool) })]
	public class NotNode : ValueNode {
		public ValueInput target { get; set; }

		protected override void OnRegister() {
			base.OnRegister();
			target = ValueInput(nameof(target), typeof(object));
		}

		public override System.Type ReturnType() {
			return typeof(bool);
		}

		public override object GetValue(Flow flow) {
			return !target.GetValue<bool>(flow);
		}

		protected override string GenerateValueCode() {
			if(target.isAssigned) {
				return CG.Value(target).CGNot(true);
			}
			throw new System.Exception("Target is unassigned");
		}

		public override string GetTitle() {
			return "Not";
		}

		public override string GetRichName() {
			return $"!({target.GetRichName()}";
		}

		public override System.Type GetNodeIcon() {
			return typeof(TypeIcons.NotIcon);
		}
	}
}