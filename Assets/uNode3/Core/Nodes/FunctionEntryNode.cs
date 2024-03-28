using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace MaxyGames.UNode.Nodes {
	public class FunctionEntryNode : BaseEntryNode {
		public FlowInput enter;

		public FlowOutput exit;

		[NonSerialized]
		public NodeContainerWithEntry container;

		protected override void OnRegister() {
			enter = new FlowInput(this, nameof(enter), flow => flow.Next(exit));
			exit = PrimaryFlowOutput(nameof(exit));
			container = nodeObject.GetObjectInParent<NodeContainerWithEntry>();
			if (container != null && container.Entry == this) {
				container.RegisterEntry(this);
			}
		}

		public override void CheckError(ErrorAnalyzer analizer) {
			base.CheckError(analizer);
			if (container != null && container.Entry != this) {
				analizer.RegisterError(this, "Multiple entry node is not supported.");
			}
		}

		public override string GetTitle() {
			return "Entry";
		}

		public override Type GetNodeIcon() {
			return typeof(TypeIcons.FlowIcon);
		}
	}
}