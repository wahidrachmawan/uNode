using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace MaxyGames.UNode.Editors.Drawer {
    public class SwitchDrawer : NodeDrawer<Nodes.NodeSwitch> {
		public override void DrawLayouted(DrawerOption option) {
			var node = GetNode(option);
			uNodeGUI.DrawCustomList(node.datas, "List Cases",
				drawElement: (position, index, value) => {
					uNodeGUIUtility.EditValue(position, new GUIContent("Element " + index), value.value.Get(null), node.target.ValueType, val => {
						option.RegisterUndo();
						node.datas[index].value = MemberData.CreateFromValue(val, node.target.ValueType);
						uNodeGUIUtility.GUIChanged(node, UIChangeType.Average);
					});
				},
				add: (position) => {
					option.RegisterUndo();
					node.datas.Add(new Nodes.NodeSwitch.Data() {
						value = new MemberData(ReflectionUtils.CreateInstance(node.target.ValueType)),
					});
					node.Register();
					uNodeGUIUtility.GUIChanged(node, UIChangeType.Average);
				},
				remove: (index) => {
					if(node.datas.Count > 0) {
						option.RegisterUndo();
						node.datas.RemoveAt(index);
						//Re-register the node for fix errors on showing inputs port summaries.
						node.Register();
						uNodeGUIUtility.GUIChanged(node, UIChangeType.Average);
					}
				},
				reorder: (list, oldIndex, newIndex) => {
					uNodeUtility.ReorderList(node.datas, newIndex, oldIndex);
					option.RegisterUndo();
					uNodeUtility.ReorderList(node.datas, oldIndex, newIndex);
					uNodeGUIUtility.GUIChanged(node, UIChangeType.Average);
				});
			if(node.nodeObject.graph.graphLayout == GraphLayout.Vertical) {
				UInspector.Draw(option.property[nameof(node.useVerticalLayout)]);
			}
			
			DrawInputs(option);
			DrawOutputs(option);
			DrawErrors(option);
		}
	}
}