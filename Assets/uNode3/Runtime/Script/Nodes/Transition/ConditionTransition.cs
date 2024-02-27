namespace MaxyGames.UNode.Transition {
	[TransitionMenu("Condition", "Condition")]
	public class ConditionTransition : TransitionEvent {
		public BlockData data = new BlockData();

		protected override void OnRegister() {
			data.Register(this);
			base.OnRegister();
		}

		public override void OnUpdate(Flow flow) {
			if(data.Validate(flow)) {
				Finish(flow);
			}
		}

		public override void CheckError(ErrorAnalyzer analizer) {
			base.CheckError(analizer);
			data.CheckErrors(analizer, true);
		}
	}
}
