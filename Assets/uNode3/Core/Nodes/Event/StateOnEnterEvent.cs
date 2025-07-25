using System;
using UnityEngine;
using System.Collections.Generic;

namespace MaxyGames.UNode.Nodes {
    [EventMenu("", "On State Enter", scope = NodeScope.State)]
	[StateEvent]
	[Description("On Enter is called once when the state becomes active.")]
	public class StateOnEnterEvent : BaseGraphEvent {
		public override void OnRuntimeInitialize(GraphInstance instance) {
			if(nodeObject.parent is NodeObject parentNode && parentNode.node is INodeWithEnterExitEvent state) {
				state.OnEnterCallback += (flow) => {
					Trigger(flow);
				};
			}
		}

		public override string GetTitle() => "On State Enter";

		public override Type GetNodeIcon() {
			return typeof(TypeIcons.EventIcon);
		}

		public override void CheckError(ErrorAnalyzer analizer) {
			base.CheckError(analizer);
			if(nodeObject.parent is not NodeObject parentNode || parentNode.node is not INodeWithEnterExitEvent) {
				analizer.RegisterError(this, "On Enter event can only be placed inside Script State.");
			}
		}
	}
}