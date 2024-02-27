using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace MaxyGames.UNode.Nodes {
	//[NodeMenu("Data", "MakeArray", typeof(System.Array))]
	public class MakeArrayNode : ValueNode {
		public SerializedType elementType = typeof(object);

		public bool autoLength = true;

		public ValueInput length { get; set; }

		public class PortData {
			public string id = uNodeUtility.GenerateUID();
			[System.NonSerialized]
			public ValueInput port;
		}
		[HideInInspector]
		public List<PortData> elements = new List<PortData>() { new PortData() };

		protected override void OnRegister() {
			base.OnRegister();
			if(autoLength == false) {
				length = ValueInput(nameof(length), typeof(int), MemberData.None).SetName("Length");
			}
			for(int i = 0; i < elements.Count; i++) {
				elements[i].port = ValueInput(elements[i].id, () => elementType.type).SetName("elements " + (i + 1));
			}
		}

		public override System.Type ReturnType() {
			if(elementType.isFilled) {
				System.Type type = elementType.type;
				if(type != null) {
					return type.MakeArrayType();
				}
			}
			return typeof(System.Array);
		}

		public override Type GetNodeIcon() {
			return typeof(object[]);
		}

		public override object GetValue(Flow flow) {
			int arrayLength;
			if(autoLength) {
				arrayLength = elements.Count;
			}
			else {
				arrayLength = length.isAssigned ? length.GetValue<int>(flow) : elements.Count;
			}
			System.Array array = System.Array.CreateInstance(elementType.type, arrayLength);
			for(int i = 0; i < elements.Count; i++) {
				array.SetValue(elements[i].port.GetValue(flow), i);
			}
			return array;
		}

		protected override string GenerateValueCode() {
			if(elementType.isAssigned) {
				if(autoLength) {
					return CG.MakeArray(elementType.type, elements.Select(item => CG.Value(item.port)).ToArray());
				}
				else {
					return CG.MakeArray(
						elementType.type,
						length,
						elements.Select(item => CG.Value(item.port)).ToArray());
				}
			}
			return null;
		}

		public override string GetTitle() {
			if(elementType != null) {
				return "Create: " + elementType.prettyName + "[]";
			}
			return "MakeArray";
		}

		public override string GetRichTitle() {
			if(elementType != null) {
				return "Create: " + elementType.GetRichName() + "[]";
			}
			return "MakeArray";
		}

		public override string GetRichName() {
			string length = null;
			if(this.length != null && this.length.isAssigned) {
				length = this.length.GetRichName();
			}
			return $"{uNodeUtility.WrapTextWithKeywordColor("new")} {elementType.GetRichName()}[{(autoLength ? null : length)}] ( {string.Join(", ", from val in elements select val.port.GetRichName())} )";
		}

		public override void CheckError(ErrorAnalyzer analizer) {
			if(!autoLength)
				analizer.CheckPort(length);
			analizer.CheckValue(elementType, nameof(elementType), this);
			foreach(var element in elements) {
				analizer.CheckPort(element.port);
			}
		}
	}
}