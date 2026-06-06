using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace MaxyGames.UNode.Editors.Drawer {
    public class AnonymousFunctionDrawer : NodeDrawer<Nodes.NodeAnonymousFunction> {
		public override void DrawLayouted(ref DrawerOption opt) {
			var option = opt;
			var node = GetNode(ref option);

			UInspector.Draw(option.property[nameof(node.returnType)]);
			uNodeGUI.DrawCustomList(node.parameters, "Parameters",
				elementHeight: index => {
					return EditorGUIUtility.singleLineHeight * 2;
				},
				drawElement: (position, index, value) => {
					position.height /= 2;
					var str = EditorGUI.DelayedTextField(position, "Name", value.name);
					if(str != value.name) {
						option.RegisterUndo();
						value.name = str;
						node.Register();
						uNodeGUIUtility.GUIChanged(node, UIChangeType.Average);
					}
					position.y += position.height;
					uNodeGUIUtility.EditType(position, value.type, new GUIContent("Type"), type => {
						option.RegisterUndo();
						value.type = type;
						node.Register();
						uNodeGUIUtility.GUIChanged(node, UIChangeType.Average);
					}, targetObject: node);
				},
				add: (position) => {
					option.RegisterUndo();
					node.parameters.Add(new());
					node.Register();
					uNodeGUIUtility.GUIChanged(node, UIChangeType.Average);
				},
				remove: (index) => {
					if(node.parameters.Count > 2) {
						option.RegisterUndo();
						node.parameters.RemoveAt(index);
						uNodeGUIUtility.GUIChanged(node, UIChangeType.Average);
						node.Register();
					}
				},
				reorder: (list, oldIndex, newIndex) => {
					uNodeUtility.ReorderList(node.parameters, newIndex, oldIndex);
					option.RegisterUndo();
					uNodeUtility.ReorderList(node.parameters, oldIndex, newIndex);
					node.Register();
					uNodeGUIUtility.GUIChanged(node, UIChangeType.Average);
				});
			
			DrawInputs(ref option);
			DrawOutputs(ref option);
			DrawErrors(ref option);
		}
	}
}