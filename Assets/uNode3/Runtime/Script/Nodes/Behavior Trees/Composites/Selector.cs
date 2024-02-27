using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace MaxyGames.UNode.Nodes {
	[NodeMenu("Behavior Tree.Composites", "Selector", IsCoroutine = true)]
	[Description("Execute node until one of node Success or all node Failure."
		+ "\nit similar to an \"Or\" operator."
		+ "\nSuccess if one of the node success\nFailure if no node Success.")]
	public class Selector : BaseCoroutineNode {
		[Range(1, 10)]
		public int flowCount = 2;
		[System.NonSerialized]
		public FlowOutput[] flows;

		protected override void OnRegister() {
			base.OnRegister();
			flows = new FlowOutput[flowCount];
			for(int i = 0; i < flowCount; i++) {
				flows[i] = FlowOutput("Flow-" + i).SetName("");
			}
		}

		protected override IEnumerator OnExecutedCoroutine(Flow flow) {
			for(int i = 0; i < flows.Length; i++) {
				var t = flows[i];
				if(!t.isConnected)
					continue;
				flow.TriggerCoroutine(t, out var wait, out var jump);
				if(wait != null) {
					//wait if there's any coroutine
					yield return wait;
				}
				var js = jump();
				if(js != null) {
					flow.jumpStatement = js;
					yield break;
				}
				if(t.GetCurrentState(flow) == StateType.Success) {
					//stop and return success state
					yield return true;
				}
			}
			//stop and return failure state
			yield return false;
		}

		public override void OnGeneratorInitialize() {
			//Register this node as state node, because this is coroutine node with state.
			CG.RegisterAsStateFlow(enter);
			CG.SetStateInitialization(enter, () => CG.GeneratePort(enter));
			for(int i = 0; i < flows.Length; i++) {
				if(flows[i] != null && flows[i].isAssigned) {
					CG.RegisterAsStateFlow(flows[i].GetTargetFlow());
				}
			}
			CG.RegisterPort(enter, () => {
				string data = null;
				for(int i = 0; i < flows.Length; i++) {
					if(flows[i] != null && flows[i].isAssigned) {
						if(!string.IsNullOrEmpty(data)) {
							data += ", ";
						}
						data += CG.GetEvent(flows[i]);
					}
				}
				return CG.New(typeof(Runtime.Selector), data);
			});
		}

		public override System.Type GetNodeIcon() {
			return typeof(TypeIcons.BranchIcon);
		}
	}
}