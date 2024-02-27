using System.Collections.Generic;
using UnityEngine;

namespace MaxyGames.UNode.Nodes {
	[NodeMenu("Data", "Condition", typeof(bool))]
	public class ConditionNode : ValueNode, IStackedNode {
		[Hide]
		public BlockData data = new BlockData();

		public IEnumerable<NodeObject> stackedNodes => data.GetNodes();

		protected override void OnRegister() {
			data.Register(this);
			base.OnRegister();
		}

		public override System.Type ReturnType() {
			return typeof(bool);
		}

		public override object GetValue(Flow flow) {
			return data.Validate(flow);
		}

		protected override string GenerateValueCode() {
			return data.GenerateConditionCode();
		}

		public override string GetTitle() {
			return "Condition";
		}

		public override void CheckError(ErrorAnalyzer analizer) {
			base.CheckError(analizer);
			data.CheckErrors(analizer, true);
		}
	}
}