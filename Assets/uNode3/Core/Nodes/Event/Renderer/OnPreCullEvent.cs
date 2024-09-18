using System;
using UnityEngine;
using System.Collections.Generic;

namespace MaxyGames.UNode.Nodes {
    [EventMenu("Renderer", "On Pre Cull")]
	[StateEvent]
	[Description("Event function that Unity calls before a Camera culls the scene.")]
	public class OnPreCullEvent : BaseComponentEvent {
		public override void OnRuntimeInitialize(GraphInstance instance) {
			base.OnRuntimeInitialize(instance);
			if(instance.target is Component comp) {
				UEvent.Register(UEventID.OnPreCull, comp, () => Trigger(instance));
			} else {
				throw new Exception("Invalid target: " + instance.target + "\nThe target type must inherit from `UnityEngine.Component`");
			}
		}

		public override void GenerateEventCode() {
			DoGenerateCode(UEventID.OnPreCull);
		}
	}
}