using System.Collections;
using UnityEngine;

namespace MaxyGames.UNode.Transition {
	[TransitionMenu("OnTimerElapsed", "OnTimerElapsed")]
	public class OnTimerElapsed : TransitionEvent {
		[Filter(typeof(float))]
		public ValueInput delay;
		public bool unscaled;

		protected override void OnRegister() {
			base.OnRegister();
			delay = ValueInput<float>(nameof(delay), 1);
		}

		public override void OnEnter(Flow flow) {
			flow.SetElementData(this, (flow.target as MonoBehaviour).StartCoroutine(Wait(flow)));
		}

		IEnumerator Wait(Flow flow) {
			if(unscaled) {
				yield return new WaitForSecondsRealtime(delay.GetValue<float>(flow));
			} else {
				yield return new WaitForSeconds(delay.GetValue<float>(flow));
			}
			Finish(flow);
		}

		public override void OnExit(Flow flow) {
			(flow.target as MonoBehaviour).StopCoroutine(flow.GetElementData<Coroutine>(this));
		}

		public override string GenerateOnEnterCode() {
			CG.SetStateInitialization(enter, () => {
				if(unscaled) {
					return CG.Routine(
						CG.Invoke(typeof(Runtime.Routine), nameof(Runtime.Routine.WaitRealtime), CG.SimplifiedLambda(CG.Value(delay))),
						CG.Routine(CG.Lambda(CG.StopEvent(GetStateNode().enter))),
						CG.Routine(CG.GetEvent(exit))
					);
				}
				else {
					return CG.Routine(
						CG.Invoke(typeof(Runtime.Routine), nameof(Runtime.Routine.Wait), CG.SimplifiedLambda(CG.Value(delay))),
						CG.Routine(CG.Lambda(CG.StopEvent(GetStateNode().enter))),
						CG.Routine(CG.GetEvent(exit))
					);
				}
			});
			//if(unscaled) {
			//	CG.generatorData.AddEventCoroutineData(this, CG.YieldReturn(CG.New(typeof(WaitForSecondsRealtime), CG.Value(delay))).AddLineInFirst() + CG.FlowFinish(this).AddLineInFirst());
			//} else {
			//	CG.generatorData.AddEventCoroutineData(this, CG.YieldReturn(CG.New(typeof(WaitForSeconds), CG.Value(delay))).AddLineInFirst() + CG.FlowFinish(this).AddLineInFirst());
			//}
			return CG.RunEvent(enter);
		}

		public override string GenerateOnExitCode() {
			return CG.StopEvent(enter);
		}
	}
}
