using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace MaxyGames.UNode.Nodes {
	[NodeMenu("Yield", "WaitUntil", IsCoroutine = true)]
	[Description("Waits until condition evaluate to true.")]
	public class NodeWaitUntil : CoroutineNode, IStackedNode {
		public BlockData data = new BlockData();

		public IEnumerable<NodeObject> stackedNodes => data.GetNodes();

		protected override void OnRegister() {
			base.OnRegister();
			data.Register(this);
		}

		protected override IEnumerator OnExecutedCoroutine(Flow flow) {
			yield return new WaitUntil(() => data.Validate(flow));
		}

		public override void OnGeneratorInitialize() {
			if(CG.IsStateFlow(enter)) {
				CG.SetStateInitialization(enter, () => {
					return CG.Routine(
						CG.Routine(CG.SimplifiedLambda(CG.New(typeof(WaitUntil), CG.SimplifiedLambda(data.GenerateConditionCode())))),
						exit.isAssigned ? CG.Routine(CG.GetEvent(exit)) : null
					);
				});
				var finishFlow = exit.GetTargetFlow();
				if(finishFlow != null)
					CG.RegisterAsStateFlow(finishFlow);
			}
			CG.RegisterPort(enter, () => {
				return CG.Flow(
					CG.YieldReturn(CG.New(typeof(WaitUntil), CG.SimplifiedLambda(data.GenerateConditionCode()))),
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
