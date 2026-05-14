using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace MaxyGames.UNode.Nodes {
	public class EntryNode : BaseEntryNode {
		[NonSerialized]
		public ISuperNodeWithEntry container;
		[NonSerialized]
		public string title = "Entry";
		[NonSerialized]
		public Type nodeIcon = typeof(TypeIcons.FlowIcon);

		[System.Runtime.Serialization.OnDeserialized]
		void OnDeserialized() {
			title = "Entry";
			nodeIcon = typeof(TypeIcons.FlowIcon);
		}

		protected override void OnRegister() {
			container = nodeObject.GetNodeInParent<ISuperNodeWithEntry>();
			if(container != null && container.Entry == this) {
				container.RegisterEntry(this);
			}
		}

		public override void CheckError(ErrorAnalyzer analizer) {
			base.CheckError(analizer);
			if(container != null && container.Entry != this) {
				analizer.RegisterError(this, "Multiple entry node is not supported.");
			}
		}

		public override string GetTitle() => title;
		public override Type GetNodeIcon() => nodeIcon;
	}
}