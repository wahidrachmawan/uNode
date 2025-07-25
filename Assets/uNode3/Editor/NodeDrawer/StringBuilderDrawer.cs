using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace MaxyGames.UNode.Editors.Drawer {
    public class StringBuilderDrawer : NodeDrawer<Nodes.StringBuilderNode> {
		public override void DrawLayouted(DrawerOption option) {
			var node = GetNode(option);

			UInspector.Draw(option.property[nameof(node.useStringBuilder)]);

			uNodeGUI.DrawCustomList(node.stringValues, "Inputs",
				drawElement: (position, index, value) => {
					EditorGUI.LabelField(position, new GUIContent("Element " + index));
				},
				add: (position) => {
					option.RegisterUndo();
					node.stringValues.Add(new());
					node.Register();
					node.stringValues.Last().port.AssignToDefault(true);
					uNodeGUIUtility.GUIChanged(node, UIChangeType.Average);
				},
				remove: (index) => {
					if(node.stringValues.Count > 2) {
						option.RegisterUndo();
						node.stringValues.RemoveAt(index);
						uNodeGUIUtility.GUIChanged(node, UIChangeType.Average);
						//Re-register the node for fix errors on showing inputs port summaries.
						node.Register();
					}
				},
				reorder: (list, oldIndex, newIndex) => {
					uNodeUtility.ReorderList(node.stringValues, newIndex, oldIndex);
					option.RegisterUndo();
					uNodeUtility.ReorderList(node.stringValues, oldIndex, newIndex);
					uNodeGUIUtility.GUIChanged(node, UIChangeType.Average);
				});
			
			DrawInputs(option);
			DrawOutputs(option);
			DrawErrors(option);
		}
	}
}