using UnityEngine.UIElements;
using System;

namespace MaxyGames.UNode.Editors {
	public class ContextualManipulator : ContextualMenuManipulator {
		public ContextualManipulator(Action<ContextualMenuPopulateEvent> menuBuilder) : base(menuBuilder) {

		}

		public ContextualManipulator(bool onlyRightClick, Action<ContextualMenuPopulateEvent> menuBuilder) : base(menuBuilder) {
			if(!onlyRightClick) {
				// activators.Clear();
				activators.Add(new ManipulatorActivationFilter {
					button = MouseButton.LeftMouse
				});
			}
		}
	}
}