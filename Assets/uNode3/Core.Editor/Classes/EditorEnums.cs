using System;

namespace MaxyGames.UNode.Editors {
	/// <summary>
	/// The UI changed type, which is impact how to act after value changed.
	/// </summary>
	public enum UIChangeType {
		//The value changed do not have effect to another values
		None,
		//The value changed is effected to small portion of object
		Small,
		//The value changed is effected to average portion of object
		Average,
		//The value changed is big and do effects most/important value of the objects.
		Important,
	}

	[Flags]
	public enum NodeFilter {
		None = 0,
		FlowInput = 1 << 0,
		FlowOutput = 1 << 1,
		ValueInput = 1 << 2,
		ValueOutput = 1 << 3,
	}
}