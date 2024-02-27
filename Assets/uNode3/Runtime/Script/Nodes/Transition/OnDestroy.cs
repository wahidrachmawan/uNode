namespace MaxyGames.UNode.Transition {
	[TransitionMenu("OnDestroy", "OnDestroy")]
	public class OnDestroy : TransitionEvent {
		public override void OnEnter(Flow flow) {
			flow.eventData.onDestroy += (_) => Execute(flow);
		}

		void Execute(Flow flow) {
			Finish(flow);
		}

		public override void OnExit(Flow flow) {
			flow.eventData.onDestroy -= (_) => Execute(flow);
		}

		public override string GenerateOnEnterCode() {
			if(!CG.HasInitialized(this)) {
				CG.SetInitialized(this);
				CG.InsertCodeToFunction(
					"OnDestroy",
					typeof(void),
					CG.Condition("if", CG.CompareNodeState(enter, null), CG.FlowTransitionFinish(this)));
			}
			return null;
		}
	}
}
