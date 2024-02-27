using System;

namespace MaxyGames.UNode {
	/// <summary>
	/// Used to show event for state.
	/// Note: need to combine with EventMenu attribute in order to show the menu.
	/// </summary>
	[System.AttributeUsage(AttributeTargets.Class)]
	public class StateEventAttribute : Attribute {
		public StateEventAttribute() { }
	}
}