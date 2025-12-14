using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace MaxyGames.UNode.Editors.Drawer {
    public class NodePreprocessorDrawer : NodeDrawer<Nodes.NodePreprocessor> {
		public override void DrawLayouted(DrawerOption option) {
			var node = GetNode(option);

			uNodeGUI.DrawCustomList(node.symbols, "Symbols",
				drawElement: (position, index, value) => {
					EditorGUI.BeginChangeCheck();
					value.symbol = EditorGUI.TextField(position, "Element " + index, value.symbol);
					if(EditorGUI.EndChangeCheck()) {
						option.RegisterUndo();
						node.symbols[index].symbol = value.symbol;
						uNodeGUIUtility.GUIChanged(node, UIChangeType.Average);
					}
				},
				add: (position) => {
					option.RegisterUndo();
					node.symbols.Add(new());
					uNodeGUIUtility.GUIChanged(node, UIChangeType.Average);
				},
				remove: (index) => {
					if(node.symbols.Count > 2) {
						option.RegisterUndo();
						node.symbols.RemoveAt(index);
						uNodeGUIUtility.GUIChanged(node, UIChangeType.Average);
						//Re-register the node for fix errors on showing inputs port summaries.
						node.Register();
					}
				},
				reorder: (list, oldIndex, newIndex) => {
					uNodeUtility.ReorderList(node.symbols, newIndex, oldIndex);
					option.RegisterUndo();
					uNodeUtility.ReorderList(node.symbols, oldIndex, newIndex);
					uNodeGUIUtility.GUIChanged(node, UIChangeType.Average);
				});
			DrawInputs(option);
			DrawOutputs(option);
			DrawErrors(option);
		}
	}
}