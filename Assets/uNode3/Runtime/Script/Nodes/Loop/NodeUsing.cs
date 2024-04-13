using UnityEngine;
using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

namespace MaxyGames.UNode.Nodes {
	[NodeMenu("Statement", "Using")]
	[Description("Provides a convenient node that ensures the correct use of IDisposable objects.")]
	public class NodeUsing : FlowNode {
		public FlowOutput body { get; private set; }
		public ValueInput target { get; set; }
		public ValueOutput output { get; set; }

		protected override void OnRegister() {
			body = FlowOutput(nameof(body));
			target = ValueInput<IDisposable>(nameof(target));
			output = ValueOutput(nameof(output), target.ValueType);
			output.isVariable = true;
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
			using(var val = target.GetValue<System.IDisposable>(flow)) {
				flow.SetPortData(output, val);
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
			using(var val = target.GetValue<System.IDisposable>(flow)) {
				flow.SetPortData(output, val);
				flow.TriggerCoroutine(body, out var wait, out var jump);
				if(wait != null)
					yield return wait;
				var js = jump();
				if(js != null) {
					flow.jumpStatement = js;
				}
			}
		}

		public override void OnGeneratorInitialize() {
			base.OnGeneratorInitialize();
			var vName = CG.RegisterVariable(output, "disposableVal");
			CG.RegisterPort(output, () => vName);
		}

		protected override string GenerateFlowCode() {
			if(!body.isAssigned) {
				throw new System.Exception("body is unassigned");
			}
			string data = CG.Condition("using", CG.DeclareVariable(output, CG.Value(target), body).RemoveSemicolon(), CG.Flow(body));
			return data + CG.FlowFinish(enter, exit);
		}

		public override string GetRichName() {
			return uNodeUtility.WrapTextWithKeywordColor("using: ") + target.GetRichName();
		}
	}
}