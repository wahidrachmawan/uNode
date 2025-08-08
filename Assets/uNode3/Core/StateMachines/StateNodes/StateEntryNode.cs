using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MaxyGames.UNode.Nodes {
	public class StateEntryNode : BaseEntryNode {
		public FlowInput enter;

		public FlowOutput exit;

		[NonSerialized]
		private IElementWithEntry container;

		private static readonly string[] m_styles = new[] { "state-node", "state-entry" };
		public override string[] Styles => m_styles;

		protected override void OnRegister() {
			enter = new FlowInput(this, nameof(enter), flow => flow.Next(exit));
			exit = PrimaryFlowOutput(nameof(exit));
			if(container == null) {
				container = nodeObject.GetObjectOrNodeInParent<IElementWithEntry>();
				if(container != null && container.Entry == this) {
					container.RegisterEntry(this);
				}
			}
		}

		public override void CheckError(ErrorAnalyzer analizer) {
			base.CheckError(analizer);
			if(container != null && container.Entry != this) {
				analizer.RegisterError(this, "Multiple entry node is not supported.");
			}
		}

		public override Type GetNodeIcon() {
			return typeof(TypeIcons.FlowIcon);
		}
	}
}