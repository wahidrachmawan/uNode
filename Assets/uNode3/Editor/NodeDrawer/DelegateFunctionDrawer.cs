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
		public override void DrawLayouted(DrawerOption option) {
			DrawInputs(option);
			DrawOutputs(option);
			DrawErrors(option);
		}

		protected override void DrawInputs(DrawerOption option) {
			var node = GetNode(option);
			MultipurposeNodeDrawer.DrawMember(node, node.member, false, new FilterAttribute() { ValidTargetType = MemberData.TargetType.uNodeFunction | MemberData.TargetType.Method, VoidType = true, MaxMethodParam = int.MaxValue });
		}
	}
}