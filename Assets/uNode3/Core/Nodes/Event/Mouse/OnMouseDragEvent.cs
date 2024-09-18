using System;
using UnityEngine;
using System.Collections.Generic;

namespace MaxyGames.UNode.Nodes {
    [EventMenu("Mouse", "On Mouse Drag")]
	[StateEvent]
	[Description("OnMouseDrag is called when the user has clicked on a Collider and is still holding down the mouse.")]
	public class OnMouseDragEvent : BaseComponentEvent {
		public override void OnRuntimeInitialize(GraphInstance instance) {
			base.OnRuntimeInitialize(instance);
			if(instance.target is Component comp) {
				UEvent.Register(UEventID.OnMouseDrag, comp, () => Trigger(instance));
			} else {
				throw new Exception("Invalid target: " + instance.target + "\nThe target type must inherit from `UnityEngine.Component`");
			}
		}

		public override void GenerateEventCode() {
			DoGenerateCode(UEventID.OnMouseDrag);
		}
	}
}