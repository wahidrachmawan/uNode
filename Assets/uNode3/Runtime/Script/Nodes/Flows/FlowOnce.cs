using UnityEngine;

namespace MaxyGames.UNode.Nodes {
	[NodeMenu("Flow", "Once", hasFlowInput = true, hasFlowOutput = true)]
	public class FlowOnce : Node {
		[Tooltip("The flow to execute the node.")]
		public FlowInput input { get; set; }

		[Tooltip("Reset the once state.")]
		public FlowInput reset { get; set; }

		[Tooltip("The flow to execute only once at first time node get executed.")]
		public FlowOutput output { get; set; }

		[Tooltip("The flow to execute after once the node is executed twice or more.")]
		public FlowOutput after { get; set; }

		class RuntimeData {
			public bool hasEnter = false;
		}

		protected override void OnRegister() {
			input = FlowInput(
				nameof(input), 
				(flow) => {
					var data = flow.GetOrCreateElementData<RuntimeData>(this);
					if(!data.hasEnter) {
						data.hasEnter = true;
						flow.Next(output);
					} else {
						flow.Next(after);
					}
				}).SetName("In");
			reset = FlowInput(
				nameof(reset), 
				(flow) => {
					var data = flow.GetOrCreateElementData<RuntimeData>(this);
					data.hasEnter = false;
				}).SetName("Reset");
			output = FlowOutput(nameof(output)).SetName("Once");
			after = FlowOutput(nameof(after));

			input.isCoroutine = output.IsCoroutine;
		}

		public override void OnGeneratorInitialize() {
			string varName = CG.RegisterPrivateVariable(nameof(RuntimeData.hasEnter), typeof(bool), false);
			CG.RegisterPort(input, () => {
				return CG.If(
					varName.CGNot(),
					varName.CGSet(true.CGValue()).AddStatement(CG.FlowFinish(input, output)),
					CG.FlowFinish(input, after)
				);
			});
			CG.RegisterPort(reset, () => {
				return varName.CGSet(false.CGValue());
			});
		}
	}
}
