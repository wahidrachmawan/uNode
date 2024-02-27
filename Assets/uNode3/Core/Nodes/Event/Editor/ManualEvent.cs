using System;
using UnityEngine;
using System.Collections.Generic;

namespace MaxyGames.UNode.Nodes {
    [EventMenu("Editor", "Manual Event")]
	[StateEvent]
	public class ManualEvent : BaseComponentEvent {
		public override void OnRuntimeInitialize(GraphInstance instance) {
			base.OnRuntimeInitialize(instance);
			instance.eventData.RegisterCustomEvent("@ManualEvent_" + id, instance => {
				Trigger(instance);
			});
		}

		public override void GenerateEventCode() {
			var mdata = DoGenerateCode("M_ManualEvent_" + id);
			mdata.modifier = new FunctionModifier();
		}

		public override string GetTitle() {
			return "Manual Event: " + name;
		}
	}
}