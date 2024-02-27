using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace MaxyGames.UNode.Nodes {
	public class NestedEntryNode : BaseEntryNode {
		public FlowOutput output;

		[NonSerialized, HideInInspector]
		public ISuperNodeWithEntry container;

		protected override void OnRegister() {
			output = PrimaryFlowOutput(nameof(output));
			container = nodeObject.GetNodeInParent<ISuperNodeWithEntry>();
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