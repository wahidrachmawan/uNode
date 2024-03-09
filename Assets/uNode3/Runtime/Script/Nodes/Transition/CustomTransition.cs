using System;

namespace MaxyGames.UNode.Transition {
	[TransitionMenu("Custom", "Custom")]
	public class CustomTransition : TransitionEvent {
		public const string KEY_Activate_Transition = "_ActivateTransition";

		public void Execute(Flow flow) {
			Finish(flow);
		}

		public override string GenerateOnEnterCode() {
			if(!exit.hasValidConnections)
				return null;
			var node = GetStateNode();
			string contents = CG.If(CG.CompareNodeState(node.enter, null), CG.FlowTransitionFinish(this));
			CG.generatorData.InsertCustomUIDMethod(KEY_Activate_Transition, typeof(void), name + node.id, contents);
			return null;
		}

		public override string GetTitle() {
			return name;
		}
	}
}
