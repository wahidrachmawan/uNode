namespace MaxyGames.UNode.Transition {
	[TransitionMenu("OnDisable", "OnDisable")]
	public class OnDisable : TransitionEvent {
		public override void OnEnter(Flow flow) {
			flow.eventData.onDisable += (_) => Execute(flow);
		}

		void Execute(Flow flow) {
			Finish(flow);
		}

		public override void OnExit(Flow flow) {
			flow.eventData.onDisable -= (_) => Execute(flow);
		}

		public override string GenerateOnEnterCode() {
			if(!CG.HasInitialized(this)) {
				CG.SetInitialized(this);
				CG.InsertCodeToFunction(
					"OnDisable",
					typeof(void),
					CG.Condition("if", CG.CompareNodeState(enter, null), CG.FlowTransitionFinish(this)));
			}
			return null;
		}
	}
}
