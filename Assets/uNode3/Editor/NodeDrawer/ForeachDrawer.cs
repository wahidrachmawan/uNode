using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace MaxyGames.UNode.Editors.Drawer {
    public class ForeachDrawer : NodeDrawer<Nodes.ForeachLoop> {
		public override void DrawLayouted(DrawerOption option) {
			var node = GetNode(option);

			UInspector.Draw(option.property[nameof(node.deconstructValue)]);

			if(node.output == null) {
				using(new EditorGUILayout.VerticalScope()) {
					foreach(var data in node.deconstructDatas) {
						using(new EditorGUILayout.HorizontalScope()) {
							var str = EditorGUILayout.DelayedTextField(data.originalName, data.name);
							if(str != data.name) {
								option.RegisterUndo();
								data.name = str;
								uNodeGUIUtility.GUIChanged(node, UIChangeType.Average);
							}
						}
					}
				}
			}
			
			DrawInputs(option);
			DrawOutputs(option);
			DrawErrors(option);
		}
	}
}