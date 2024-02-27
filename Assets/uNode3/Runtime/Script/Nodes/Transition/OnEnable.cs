namespace MaxyGames.UNode.Transition {
	[TransitionMenu("OnEnable", "OnEnable")]
	public class OnEnable : TransitionEvent {
		public override void OnEnter(Flow flow) {
			flow.eventData.onEnable += (_) => Execute(flow);
		}

		void Execute(Flow flow) {
			Finish(flow);
		}

		public override void OnExit(Flow flow) {
			flow.eventData.onEnable -= (_) => Execute(flow);
		}

		public override string GenerateOnEnterCode() {
			if(!CG.HasInitialized(this)) {
				CG.SetInitialized(this);
				CG.InsertCodeToFunction(
					"OnEnable",
					typeof(void),
					CG.Condition("if", CG.CompareNodeState(enter, null), CG.FlowTransitionFinish(this)));
			}
			return null;
		}
	}
}
