using UnityEngine;
using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

namespace MaxyGames.UNode.Nodes {
	[NodeMenu("Statement", "Lock")]
	public class NodeLock : FlowNode {
		public FlowOutput body { get; private set; }
		public ValueInput target { get; set; }

		protected override void OnRegister() {
			body = FlowOutput(nameof(body));
			target = ValueInput<object>(nameof(target));
			base.OnRegister();
			exit.SetName("Exit");
		}

		protected override bool IsCoroutine() {
			return HasCoroutineInFlows(body, exit);
		}

		protected override void OnExecuted(Flow flow) {
			if(!body.isConnected) {
				throw new Exception("body is unassigned");
			}
			lock(target.GetValue(flow)) {
				flow.Trigger(body, out var js);
				if(js != null) {
					flow.jumpStatement = js;
				}
			}
		}

		protected override IEnumerator OnExecutedCoroutine(Flow flow) {
			if(!body.isConnected) {
				throw new Exception("body is unassigned");
			}
			lock(target.GetValue(flow)) {
				flow.TriggerCoroutine(body, out var wait, out var jump);
				if(wait != null)
					yield return wait;
				var js = jump();
				if(js != null) {
					flow.jumpStatement = js;
				}
			}
		}

		protected override string GenerateFlowCode() {
			if(!body.isAssigned) {
				throw new System.Exception("body is unassigned");
			}
			string data = CG.Condition("lock", CG.Value(target), CG.Flow(body));
			return data + CG.FlowFinish(enter, exit);
		}

		public override string GetRichName() {
			return uNodeUtility.WrapTextWithKeywordColor("lock: ") + target.GetRichName();
		}
	}
}