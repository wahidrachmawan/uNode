using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace MaxyGames.UNode.Nodes {
	//[NodeMenu("Data", "MakeDictionary", typeof(System.Collections.Generic.Dictionary<,>))]
	[Description("A node to create a new dictionary value")]
	public class MakeDictionaryNode : ValueNode {
		
		public SerializedType keyType = typeof(object);
		public SerializedType valueType = typeof(object);
		[HideInInspector]
		public SerializedType elementType {
			get {
				if(keyType.isFilled && valueType.isFilled) {
					return typeof(Dictionary<,>).MakeGenericType(keyType.type, valueType.type);
				}
				return typeof(Dictionary<,>);
			}
			set {
				if(value != null) {
					keyType = value.type.GetGenericArguments()[0];
					valueType = value.type.GetGenericArguments()[1];
				}
			}
		}

		public bool autoLength = true;

		public ValueInput length { get; set; }

		public class PortData {
			public string keyId = uNodeUtility.GenerateUID();
			public string valueId = uNodeUtility.GenerateUID();
			[NonSerialized]
			public ValueInput keyPort;
			public ValueInput valuePort;
		}
		[HideInInspector]
		public List<PortData> elements = new List<PortData>() { new PortData() };

		protected override void OnRegister() {
			base.OnRegister();
			if(autoLength == false) {
				length = ValueInput(nameof(length), typeof(int), MemberData.None).SetName("Length");
			}
			for(int i = 0; i < elements.Count; i++) {
				elements[i].keyPort = ValueInput(elements[i].keyId, () => keyType.type).SetName("element " + (i + 1) + " (key)");
				elements[i].valuePort = ValueInput(elements[i].valueId, () => valueType.type).SetName("element " + (i + 1) + " (value)");
			}
		}

		public override Type ReturnType() {
			return elementType.type;
		}

		public override Type GetNodeIcon() {
			return typeof(object[]);
		}

		public override object GetValue(Flow flow) {
			int dicLength;
			if(autoLength) {
				dicLength = elements.Count;
			}
			else {
				dicLength = length.isAssigned ? length.GetValue<int>(flow) : elements.Count;
			}
			var dic = Activator.CreateInstance(elementType.type);
			for(int i = 0; i < elements.Count; i++) {
				var key = elements[i].keyPort.GetValue(flow);
				var value = elements[i].valuePort.GetValue(flow);
				dic.GetType().GetMethod("SetValue").Invoke(dic, new object[] { key, value });
			}
			return dic;
		}

		protected override string GenerateValueCode() {
			if(keyType.isAssigned && valueType.isAssigned) {
				if(autoLength) {
					return CG.MakeDictionary(
						elementType.type, 
						elements.Select(item => CG.Value(item.keyPort)).ToArray(),
						elements.Select(item => CG.Value(item.valuePort)).ToArray());
				}
				else {
					return CG.MakeDictionary(
						elementType.type,
						length,
						elements.Select(item => CG.Value(item.keyPort)).ToArray(),
						elements.Select(item => CG.Value(item.valuePort)).ToArray());
				}
			}
			return null;
		}

		public override string GetTitle() {
			if(keyType != null && valueType != null) {
				return "Create: " + elementType.prettyName;
			}
			return "MakeDictionary";
		}

		public override string GetRichTitle() {
			if(keyType != null && valueType != null) {
				return "Create: " + elementType.GetRichName();
			}
			return "MakeDictionary";
		}

		public override string GetRichName() {
			string length = null;
			if(this.length != null && this.length.isAssigned) {
				length = this.length.GetRichName();
			}
			return $"{uNodeUtility.WrapTextWithKeywordColor("new")} {elementType.GetRichName()}[{(autoLength ? null : length)}] ( {string.Join(", ", from val in elements select val.keyPort.GetRichName()+"|"+val.valuePort.GetRichName())} )";
		}

		public override void CheckError(ErrorAnalyzer analizer) {
			if(!autoLength)
				analizer.CheckPort(length);
			analizer.CheckValue(elementType, nameof(elementType), this);
			foreach(var element in elements) {
				analizer.CheckPort(element.keyPort);
				analizer.CheckPort(element.valuePort);
			}
		}
	}
}