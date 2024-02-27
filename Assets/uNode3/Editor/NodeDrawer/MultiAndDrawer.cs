using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace MaxyGames.UNode.Editors.Drawer {
    public class MultiAndDrawer : NodeDrawer<Nodes.MultiANDNode> {
		public override void DrawLayouted(DrawerOption option) {
			var node = GetNode(option);

			uNodeGUI.DrawCustomList(node.inputs, "Inputs",
				drawElement: (position, index, value) => {
					EditorGUI.LabelField(position, new GUIContent("Element " + index));
				},
				add: (position) => {
					option.RegisterUndo();
					node.inputs.Add(new());
					node.Register();
					node.inputs.Last().port.AssignToDefault(true);
					uNodeGUIUtility.GUIChanged(node, UIChangeType.Average);
				},
				remove: (index) => {
					if(node.inputs.Count > 2) {
						option.RegisterUndo();
						node.inputs.RemoveAt(index);
						uNodeGUIUtility.GUIChanged(node, UIChangeType.Average);
						//Re-register the node for fix errors on showing inputs port summaries.
						node.Register();
					}
				},
				reorder: (list, oldIndex, newIndex) => {
					uNodeUtility.ReorderList(node.inputs, newIndex, oldIndex);
					option.RegisterUndo();
					uNodeUtility.ReorderList(node.inputs, oldIndex, newIndex);
					uNodeGUIUtility.GUIChanged(node, UIChangeType.Average);
				});
			
			DrawInputs(option);
			DrawOutputs(option);
			DrawErrors(option);
		}
	}
}