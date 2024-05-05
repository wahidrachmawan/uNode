using UnityEngine;
using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

namespace MaxyGames.UNode.Nodes {
	public class NodeRegion : Node {
		public Color nodeColor = Color.cyan;

		protected override void OnRegister() { }

		public override Type GetNodeIcon() {
			return typeof(TypeIcons.FolderIcon);
		}
	}
}