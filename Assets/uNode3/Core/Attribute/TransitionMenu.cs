using System;

namespace MaxyGames.UNode {
	/// <summary>
	/// Used to show menu item for transition.
	/// </summary>
	[System.AttributeUsage(AttributeTargets.Class)]
	public class TransitionMenu : Attribute {
		public string path { get; set; }
		public string name { get; set; }
		public Type type { get; set; }

		//public TransitionEvent TransitionEvent;

		public TransitionMenu(string path, string name) {
			this.path = path;
			this.name = name;
		}
	}
}