using System;

namespace MaxyGames.UNode {
	/// <summary>
	/// Used to show help box in inspector.
	/// </summary>
	[System.AttributeUsage(System.AttributeTargets.Class)]
	public class DescriptionAttribute : Attribute {
		public string description { get; set; }
		public Type type { get; set; }

		public DescriptionAttribute(string description = null) {
			this.description = description;
		}
	}
}
