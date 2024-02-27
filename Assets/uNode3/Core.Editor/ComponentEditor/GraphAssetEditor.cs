using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using MaxyGames.UNode.Nodes;

namespace MaxyGames.UNode.Editors {
    public class GraphAssetEditor : Editor {
		public override void OnInspectorGUI() {
			var monoScript = uNodeEditorUtility.GetMonoScript(target);
			if(monoScript != null) {
				EditorGUI.BeginDisabledGroup(true);
				EditorGUILayout.ObjectField("Script", monoScript, typeof(MonoScript), true);
				EditorGUI.EndDisabledGroup();
			}
			DrawGUI(true);
		}

		public virtual void DrawGUI(bool isInspector) {
			if(target is IAttributeSystem attributeSystem) {
				var att = AttributeTargets.Class;
				if(target is IClassGraph typeGraph) {
					if(typeGraph.IsStruct) {
						att = AttributeTargets.Struct;
					}
					else if(typeGraph.IsInterface) {
						att = AttributeTargets.Interface;
					}
				}
				else if(target is IScriptInterface) {
					att = AttributeTargets.Interface;
				}
				uNodeGUI.DrawAttribute(attributeSystem.Attributes, target, null, att);
			}
			if(target is IInterfaceSystem) {
				uNodeGUI.DrawInterfaces(target as IInterfaceSystem);
			}
			DrawGraphLayout();
		}

		protected void DrawGraphLayout() {
			var graph = target as IGraph;
			if(graph != null) {
				uNodeGUIUtility.EditValueLayouted(nameof(graph.GraphData.graphLayout), graph.GraphData, val => {
					graph.GraphData.graphLayout = (GraphLayout)val;
					UGraphView.ClearCache(graph);
					uNodeEditor.RefreshEditor(true);
					//uNodeGUIUtility.GUIChangedMajor(target);
				});
			}
		}
	}
}