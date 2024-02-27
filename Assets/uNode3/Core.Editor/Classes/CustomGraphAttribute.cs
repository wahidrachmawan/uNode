using System;

namespace MaxyGames.UNode.Editors {
	[System.AttributeUsage(System.AttributeTargets.Class)]
	public class CustomGraphAttribute : Attribute {
		public string name;

		public Type type;

		public CustomGraphAttribute(string name) {
			this.name = name;
		}
	}
}
