using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Text;

namespace MaxyGames.UNode.Nodes {
	[NodeMenu("Data", "StringBuilder", typeof(string), inputs = new[] { typeof(string), typeof(object) })]
	public class StringBuilderNode : ValueNode {
		public class Data {
			public string id = uNodeUtility.GenerateUID();

			[System.NonSerialized]
			public ValueInput port;
		}

		[HideInInspector]
		public List<Data> stringValues = new List<Data>() { new Data(), new Data() };
		[Tooltip("If enabled, the output is a concatenation using System.Text.StringBuilder instead of concatenating strings.")]
		public bool useStringBuilder;

		protected override void OnRegister() {
			base.OnRegister();
			for(int i=0;i<stringValues.Count;i++) {
				stringValues[i].port = ValueInput(stringValues[i].id, typeof(string), MemberData.CreateFromValue("")).SetName(i.ToString());
				stringValues[i].port.filter = new(typeof(string), typeof(object));
			}
		}

		protected override System.Type ReturnType() {
			return typeof(string);
		}

		public override object GetValue(Flow flow) {
			if(useStringBuilder) {
				StringBuilder builder = new();
				for(int i = 0; i < stringValues.Count; i++) {
					builder.Append(stringValues[i].port.GetValue(flow));
				}
				return builder.ToString();
			}
			else {
				string builder = null;
				for(int i = 0; i < stringValues.Count; i++) {
					builder += stringValues[i].port.GetValue(flow);
				}
				return builder;
			}
		}

		protected override string GenerateValueCode() {
			if(stringValues.Count > 0) {
				if(useStringBuilder) {
					string builder = CG.New(typeof(StringBuilder), CG.Value(stringValues[0].port));
					for(int i = 1; i < stringValues.Count; i++) {
						builder = CG.Invoke(builder, nameof(StringBuilder.Append), CG.Value(stringValues[i].port));
					}
					return builder.CGInvoke(nameof(ToString));
				}
				else {
					if(stringValues.Any(s => s.port.ValueType != typeof(string))) {
						return CG.Invoke(typeof(string), nameof(string.Concat), stringValues.Select(s => CG.Value(s.port)).ToArray());
					}
					string builder = null;
					for(int i = 0; i < stringValues.Count; i++) {
						if(i != 0)
							builder += " + ";
						builder += CG.Value(stringValues[i].port);
					}
					return builder;
				}
			}
			return "null";
		}

		public override string GetTitle() {
			return "StringBuilder";
		}

		public override string GetRichName() {
			if(stringValues.Count > 0) {
				return string.Join(" + ", from s in stringValues select s.port.GetRichName());
			}
			return "null";
		}
	}
}