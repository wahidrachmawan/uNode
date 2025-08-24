using System;

namespace MaxyGames.UNode {
	[System.AttributeUsage(AttributeTargets.Field)]
	public class ObjectTypeAttribute : Attribute {
		public Type type { get; set; }
		public string targetFieldPath { get; set; }
		public bool isElementType { get; set; }

		public ObjectTypeAttribute(Type type) {
			this.type = type;
		}

		public ObjectTypeAttribute(string targetFieldType, bool isElementType = false) {
			this.targetFieldPath = targetFieldType;
			this.isElementType = isElementType;
		}
	}

	[System.AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = true)]
	public class GraphGuidAttribute : UnityEngine.PropertyAttribute {
		public string guid;

		public GraphGuidAttribute(string guid) {
			this.guid = guid;
		}
	}
}