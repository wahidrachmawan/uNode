using UnityEngine;
using System.Collections;

namespace MaxyGames.UNode.Nodes {
	[NodeMenu("Behavior Tree.Decorators", "Inverter", scope = NodeScope.StateGraph, IsCoroutine = true)]
	[Description("Invert target event state." +
		"\nThis will return success if target node is Failure, and will return Failure when target node Success." +
		"\nThis state will running when target node running.")]
	public class Inverter : CoroutineNode {
		protected override bool AutoExit => false;

		protected override IEnumerator OnExecutedCoroutine(Flow flow) {
			if(!exit.isConnected) {
				yield break;
			}
			flow.TriggerCoroutine(exit, out var wait, out var jump);
			if(wait != null)
				yield return wait;
			var js = jump();
			if(js != null) {
				flow.jumpStatement = js;
				yield break;
			}
			if(exit.GetCurrentState(flow) == StateType.Success) {
				yield return false;
			}
			else if(exit.GetCurrentState(flow) == StateType.Failure) {
				yield return true;
			}
			throw new System.Exception("The exit is still run.");
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
				return CG.New(typeof(Runtime.Inverter), CG.GetEvent(exit));
			});
		}
	}
}
