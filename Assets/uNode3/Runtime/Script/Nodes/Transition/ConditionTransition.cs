using System.Collections.Generic;

namespace MaxyGames.UNode.Transition {
	[TransitionMenu("Condition", "Condition")]
	public class ConditionTransition : TransitionEvent, IStackedNode {
		public BlockData data = new BlockData();

		public IEnumerable<NodeObject> StackedNodes => data.GetNodes();

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

		public override void OnGeneratorInitialize() {
			base.OnGeneratorInitialize();
		}

		public override string GenerateOnEnterCode() {
			if(!CG.HasInitialized(this)) {
				CG.SetInitialized(this);
				var mData = CG.generatorData.GetMethodData("Update");
				if(mData == null) {
					mData = CG.generatorData.AddMethod(
						"Update",
						typeof(void));
				}
				mData.AddCode(
					CG.Condition(
						"if",
						CG.And(CG.CompareNodeState(node.enter, null), data.GenerateConditionCode()),
						CG.FlowTransitionFinish(this)
					),
					this
				);
			}
			return null;
		}
	}
}
