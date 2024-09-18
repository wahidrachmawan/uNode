using System;
using UnityEngine;
using System.Collections.Generic;

namespace MaxyGames.UNode.Nodes {
    [EventMenu("Mouse", "On Mouse Up As Button")]
	[StateEvent]
	[Description("OnMouseUpAsButton is only called when the mouse is released over the same Collider as it was pressed.")]
	public class OnMouseUpAsButtonEvent : BaseComponentEvent {
		public override void OnRuntimeInitialize(GraphInstance instance) {
			base.OnRuntimeInitialize(instance);
			if(instance.target is Component comp) {
				UEvent.Register(UEventID.OnMouseUpAsButton, comp, () => Trigger(instance));
			} else {
				throw new Exception("Invalid target: " + instance.target + "\nThe target type must inherit from `UnityEngine.Component`");
			}
		}

		public override void GenerateEventCode() {
			DoGenerateCode(UEventID.OnMouseUpAsButton);
		}
	}
}