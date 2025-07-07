using UnityEngine;
using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

namespace MaxyGames.UNode {
	/// <summary>
	/// Base class for all entry node.
	/// </summary>
	public abstract class BaseEntryNode : Node {
		public override string GetTitle() {
			return "Entry";
		}

		public override Type GetNodeIcon() {
			return typeof(TypeIcons.FlowIcon);
		}

		public override void CheckError(ErrorAnalyzer analyzer) {
			base.CheckError(analyzer);
			if(nodeObject.parent is NodeContainerWithEntry containerWithEntry) {
				if(containerWithEntry.Entry != this) {
					analyzer.RegisterError(this, "Multiple entry node is not supported.");
				}
			}
			else if(nodeObject.parent is NodeObject parentNode) {
				if(parentNode.node is ISuperNodeWithEntry container) {
					if(container.Entry != this) {
						analyzer.RegisterError(this, "Multiple entry node is not supported.");
					}
				}
			}
		}
	}
}