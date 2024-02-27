using UnityEngine;
using System.Collections;

namespace MaxyGames.UNode.Nodes {
	[NodeMenu("Behavior Tree.Decorators", "Repeater", IsCoroutine = true)]
	[Description("Repeat target node until target node is run in specified number of time." +
		"\nThis will always return success.")]
	public class Repeater : CoroutineNode {
		[Tooltip("The number repeat time.")]
		public int RepeatCount = 1;
		[Tooltip("If true, this will repeat forever except StopEventOnFailure is true and called event is failure.")]
		public bool RepeatForever = false;
		[Tooltip("If called event Failure, this will stop to repeat event\n" +
		"This will always return success.")]
		public bool StopEventOnFailure = false;

		private int repeatNumber;
		private bool canExecuteEvent;

		protected override bool AutoExit => false;

		protected override IEnumerator OnExecutedCoroutine(Flow flow) {
			canExecuteEvent = true;
			repeatNumber = 0;
			while(flow.currentState == StateType.Running) {
				if(!enter.isConnected) {
					yield break;
				}
				if(canExecuteEvent && (RepeatForever || RepeatCount > repeatNumber)) {
					flow.TriggerCoroutine(exit, out var wait);
					if(wait != null)
						yield return wait;
					repeatNumber++;
					canExecuteEvent = false;
				}
				//if(exit.IsFinished(flow)) 
				{
					//JumpStatement js = exit.GetJumpStatement(flow);
					//if(js != null) {
					//	if(js.jumpType == JumpStatementType.Continue) {
					//		continue;
					//	} else if(js.jumpType == JumpStatementType.Break) {
					//		yield break;
					//	}
					//	flow.jumpStatement = js;
					//	yield break;
					//}
					if(StopEventOnFailure && exit.GetCurrentState(flow) == StateType.Failure) {
						yield break;
					}
					if(!RepeatForever && RepeatCount <= repeatNumber) {
						yield break;
					}
					canExecuteEvent = true;
				}
				yield return null;
			}
		}

		public override void OnGeneratorInitialize() {
			//Register this node as state node, because this is coroutine node with state.
			CG.RegisterAsStateFlow(enter);
			CG.SetStateInitialization(enter, () => CG.GeneratePort(enter));
			if(exit.isAssigned) {
				CG.RegisterAsStateFlow(exit.GetTargetFlow());
			}
			CG.RegisterPort(enter, () => {
				if(!exit.isAssigned)
					throw new System.Exception("Exit is not assigned");
				return CG.New(typeof(Runtime.Repeater), CG.GetEvent(exit), RepeatForever ? CG.Value(-1) : CG.Value(RepeatCount), CG.Value(StopEventOnFailure));
			});
		}
	}
}
