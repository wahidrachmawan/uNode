using System;
using UnityEngine;
using System.Collections.Generic;

namespace MaxyGames.UNode.Nodes {
    [EventMenu("", "On State Update", scope = NodeScope.State)]
	[StateEvent]
	[Description("On Update is called every frame when the state becomes active.")]
	public class StateOnUpdateEvent : BaseGraphEvent {
		public override void OnRuntimeInitialize(GraphInstance instance) {
			if(nodeObject.parent is NodeObject parentNode && parentNode.node is INodeWithUpdateEvent state) {
				state.OnUpdateCallback += (flow) => {
					Trigger(flow);
				};
			}
		}

		public override string GetTitle() => "On State Update";

		public override Type GetNodeIcon() {
			return typeof(TypeIcons.EventIcon);
		}

		public override void CheckError(ErrorAnalyzer analizer) {
			base.CheckError(analizer);
			if(nodeObject.parent is not NodeObject parentNode || parentNode.node is not INodeWithUpdateEvent) {
				analizer.RegisterError(this, "On Update event can only be placed inside Script State or Any State.");
			}
		}
	}
}