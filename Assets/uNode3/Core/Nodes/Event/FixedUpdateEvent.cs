using System;
using UnityEngine;
using System.Collections.Generic;

namespace MaxyGames.UNode.Nodes {
    [EventMenu("Gameloop", "FixedUpdate")]
	[StateEvent]
	public class FixedUpdateEvent : BaseComponentEvent {
		public override void OnRuntimeInitialize(GraphInstance instance) {
			base.OnRuntimeInitialize(instance);
			if(instance.target is Component comp) {
				UEvent.Register(UEventID.FixedUpdate, comp, () => Trigger(instance));
			} else {
				throw new Exception("Invalid target: " + instance + "\nThe target type must inherit from `UnityEngine.Component`");
			}
		}

		public override void GenerateEventCode() {
			DoGenerateCode(UEventID.FixedUpdate);
		}
	}
}