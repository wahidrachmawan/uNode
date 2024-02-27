using UnityEngine;
using System.Collections;

namespace MaxyGames.UNode.Nodes {
	[NodeMenu("Yield", "WaitWhile", IsCoroutine = true)]
	[Description("Waits until condition evaluate to false.")]
	public class NodeWaitWhile : CoroutineNode {
		public BlockData data = new BlockData();

		protected override bool AutoExit => false;

		protected override void OnRegister() {
			base.OnRegister();
			data.Register(this);
		}

		protected override IEnumerator OnExecutedCoroutine(Flow flow) {
			yield return new WaitWhile(() => data.Validate(flow));
		}

		public override void OnGeneratorInitialize() {
			if(CG.IsStateFlow(enter)) {
				CG.SetStateInitialization(enter, () => {
					return CG.Routine(
						CG.Routine(CG.SimplifiedLambda(CG.New(typeof(WaitWhile), CG.SimplifiedLambda(data.GenerateConditionCode())))),
						exit.isAssigned ? CG.Routine(CG.GetEvent(exit)) : null
					);
				});
				var finishFlow = exit.GetTargetFlow();
				if(finishFlow != null)
					CG.RegisterAsStateFlow(finishFlow);
			}
			CG.RegisterPort(enter, () => {
				return CG.Flow(
					CG.YieldReturn(CG.New(typeof(WaitWhile), CG.SimplifiedLambda(data.GenerateConditionCode()))),
					CG.FlowFinish(enter, true, exit)
				);
			});
		}

		public override void CheckError(ErrorAnalyzer analizer) {
			base.CheckError(analizer);
			data.CheckErrors(analizer, true);
		}
	}
}
