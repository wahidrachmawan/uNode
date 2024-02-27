using UnityEngine;

namespace MaxyGames.UNode.Nodes {
	[NodeMenu("Yield", "Yield Break", IsCoroutine = true, scope = NodeScope.Coroutine)]
	public class NodeYieldBreak : BaseFlowNode {
		protected override void OnExecuted(Flow flow) {
			flow.jumpStatement = new JumpStatement(this, JumpStatementType.Return);
		}

		protected override string GenerateFlowCode() {
			return CG.YieldBreak();
		}

		public override string GetTitle() {
			return "YieldBreak";
		}
	}
}