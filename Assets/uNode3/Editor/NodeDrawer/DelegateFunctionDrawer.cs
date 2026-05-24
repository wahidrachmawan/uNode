using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;
using MaxyGames.UNode.Nodes;

namespace MaxyGames.UNode.Editors.Drawer {
    public class DelegateFunctionDrawer : NodeDrawer<NodeDelegateFunction> {
		public override void DrawLayouted(ref DrawerOption option) {
			DrawInputs(ref option);
			DrawOutputs(ref option);
			DrawErrors(ref option);
		}

		protected override void DrawInputs(ref DrawerOption option) {
			var node = GetNode(ref option);
			MultipurposeNodeDrawer.DrawMember(node, node.member, false, new FilterAttribute() { ValidTargetType = MemberData.TargetType.uNodeFunction | MemberData.TargetType.Method, VoidType = true, MaxMethodParam = int.MaxValue });
		}
	}
}