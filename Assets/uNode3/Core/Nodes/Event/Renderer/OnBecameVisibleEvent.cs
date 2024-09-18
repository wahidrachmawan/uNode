using System;
using UnityEngine;
using System.Collections.Generic;

namespace MaxyGames.UNode.Nodes {
    [EventMenu("Renderer", "On Became Visible")]
	[StateEvent]
	[Description("OnBecameVisible is called when the renderer became visible by any camera.")]
	public class OnBecameVisibleEvent : BaseComponentEvent {
		public override void OnRuntimeInitialize(GraphInstance instance) {
			base.OnRuntimeInitialize(instance);
			if(instance.target is Component comp) {
				UEvent.Register(UEventID.OnBecameVisible, comp, () => Trigger(instance));
			} else {
				throw new Exception("Invalid target: " + instance.target + "\nThe target type must inherit from `UnityEngine.Component`");
			}
		}

		public override void GenerateEventCode() {
			DoGenerateCode(UEventID.OnBecameVisible);
		}
	}
}