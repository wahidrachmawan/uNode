using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;

namespace MaxyGames.UNode.Editors.Drawer {
    public class NodeBaseCallerDrawer : MultipurposeNodeDrawer {
		FilterAttribute filter = new FilterAttribute() {
			MaxMethodParam = int.MaxValue,
			Static = false,
			VoidType = true,
			DisplayDefaultStaticType = false,
			ValidNextMemberTypes = MemberTypes.Constructor,
		};

		protected override void DrawInputs(DrawerOption option) {
			var node = GetNode(option);
			if(GUILayout.Button(new GUIContent("base"), EditorStyles.popup)) {
				GUI.changed = false;
				ChangeMember(node);
			}
			DrawInputs(option, node, showAddButton: false, filter: filter, customChangeAction: () => {
				ChangeMember(node);
			});
		}

		private void ChangeMember(MultipurposeNode node) {
			var member = node.member;
			ItemSelector.ShowCustomItem(
				customItems: ItemSelector.MakeCustomItems(node.nodeObject.graphContainer.GetGraphInheritType(), filter, inheritCategory: ItemSelector.CategoryInherited),
				selectCallback: item => {
					uNodeEditorUtility.RegisterUndo(node.nodeObject.graphContainer as UnityEngine.Object);
					member.target = item;
					uNodeGUIUtility.GUIChanged(node, UIChangeType.Average);
				}, filter: filter).ChangePosition(Event.current.mousePosition.ToScreenPoint());
		}

		public override bool IsValid(Type type, bool layouted) {
			return type == typeof(NodeBaseCaller);
		}
	}
}