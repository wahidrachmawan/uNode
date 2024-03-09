using UnityEngine;
using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

namespace MaxyGames.UNode.Nodes {
	[NodeMenu("Yield", "WaitForSecond", IsCoroutine = true)]
	public class NodeWaitForSecond : CoroutineNode {
		public ValueInput waitTime { get; set; }

		protected override void OnRegister() {
			base.OnRegister();
			waitTime = ValueInput<float>(nameof(waitTime));
		}

		protected override IEnumerator OnExecutedCoroutine(Flow flow) {
			yield return new WaitForSeconds(waitTime.GetValue<float>(flow));
		}

		public override void OnGeneratorInitialize() {
			if(CG.IsStateFlow(enter)) {
				CG.SetStateInitialization(enter, () => {
					return CG.Routine(
						CG.Invoke(typeof(Runtime.Routine), nameof(Runtime.Routine.Wait), CG.SimplifiedLambda(CG.Value(waitTime))),
						exit.isAssigned ? CG.Routine(CG.GetEvent(exit)) : null
					);
				});
				var finishFlow = exit.GetTargetFlow();
				if(finishFlow != null)
					CG.RegisterAsStateFlow(finishFlow);
			}
			CG.RegisterPort(enter, () => {
				return "yield return new " + CG.Type(typeof(WaitForSeconds)) + "(" + CG.Value(waitTime) + ");" + CG.FlowFinish(enter, exit).AddLineInFirst();
			});
		}

		public override string GetRichName() {
			return "Wait For Second:" + waitTime.GetRichName();
		}
	}
}