using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace MaxyGames.UNode.Nodes {
	[NodeMenu("Data", "StringBuilder", typeof(string), inputs = new[] { typeof(string) })]
	public class StringBuilderNode : ValueNode {
		public class Data {
			public string id = uNodeUtility.GenerateUID();

			[System.NonSerialized]
			public ValueInput port;
		}

		[HideInInspector]
		public List<Data> stringValues = new List<Data>() { new Data(), new Data() };

		protected override void OnRegister() {
			base.OnRegister();
			for(int i=0;i<stringValues.Count;i++) {
				stringValues[i].port = ValueInput(stringValues[i].id, typeof(string), MemberData.CreateFromValue("")).SetName(i.ToString());
			}
		}

		public override System.Type ReturnType() {
			return typeof(string);
		}

		public override object GetValue(Flow flow) {
			string builder = null;
			for(int i = 0; i < stringValues.Count; i++) {
				builder += stringValues[i].port.GetValue<string>(flow);
			}
			return builder;
		}

		protected override string GenerateValueCode() {
			if(stringValues.Count > 0) {
				string builder = null;
				for(int i = 0; i < stringValues.Count; i++) {
					if(i != 0)
						builder += " + ";
					builder += CG.Value(stringValues[i].port);
				}
				return builder;
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