using System;
using UnityEngine;
using System.Collections.Generic;

namespace MaxyGames.UNode.Nodes {
    [EventMenu("Gameloop", "LateUpdate")]
	[StateEvent]
	[Description("LateUpdate is called every frame, if the Behaviour is enabled.")]
	public class LateUpdateEvent : BaseComponentEvent {
		public override void OnRuntimeInitialize(GraphInstance instance) {
			base.OnRuntimeInitialize(instance);
			if(instance.target is Component comp) {
				UEvent.Register(UEventID.LateUpdate, comp, () => Trigger(instance));
			} else {
				throw new Exception("Invalid target: " + instance.target + "\nThe target type must inherit from `UnityEngine.Component`");
			}
		}

		public override void GenerateEventCode() {
			DoGenerateCode(UEventID.LateUpdate);
		}
	}
}