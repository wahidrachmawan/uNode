using UnityEngine;
using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

namespace MaxyGames.UNode.Nodes {
	[NodeMenu("Data", "AND {&&}", typeof(bool), inputs = new[] { typeof(bool) })]
	public class MultiANDNode : ValueNode {
		public class PortData {
			public string id = uNodeUtility.GenerateUID();
			[NonSerialized]
			public ValueInput port;
		}

		public List<PortData> inputs = new List<PortData>() { new PortData(), new PortData() };

		protected override void OnRegister() {
			base.OnRegister();
			while(inputs.Count < 2) {
				inputs.Add(new PortData());
			}
			for(int i = 0; i < inputs.Count; i++) {
				inputs[i].port = ValueInput(inputs[i].id, typeof(bool)).SetName("input " + (i + 1));
			}
		}

		public override System.Type ReturnType() {
			return typeof(bool);
		}

		public override object GetValue(Flow flow) {
			if(inputs.Count >= 2) {
				for(int i = 0; i < inputs.Count; i++) {
					if(!inputs[i].port.GetValue<bool>(flow)) {
						return false;
					}
				}
				return true;
			}
			return false;
		}

		protected override string GenerateValueCode() {
			if(inputs.Count >= 2) {
				string contents = inputs[0].port.CGValue();
				for(int i = 1; i < inputs.Count; i++) {
					contents = CG.And(contents, inputs[i].port.CGValue()).Wrap();
				}
				return contents;
			}
			throw new System.Exception("Target is unassigned");
		}

		public override string GetTitle() {
			return "AND";
		}

		public override string GetRichName() {
			return string.Join(" && ", from input in inputs select input.port.GetRichName());
		}

		public override System.Type GetNodeIcon() {
			return typeof(TypeIcons.AndIcon);
		}

		public override void CheckError(ErrorAnalyzer analizer) {
			analizer.CheckPort(inputs.Select(p => p.port));
			if(inputs.Count < 2) {
				analizer.RegisterError(this, "The minimal value input must be 2.");
			}
		}
	}
}