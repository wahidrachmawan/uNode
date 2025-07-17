using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace MaxyGames.UNode.Editors.Drawer {
	class VariableDrawer : UGraphElementDrawer<Variable> {
		protected override void DrawHeader(DrawerOption option) {
			var value = option.value as Variable;
			DrawNicelyHeader(option, value.type);
		}

		protected override void DoDraw(DrawerOption option) {
			var value = option.value as Variable;
			if(value.graphContainer.GetGraphInheritType() != typeof(ValueType)) {
				UInspector.Draw(new DrawerOption() {
					property = option.property[nameof(Variable.serializedValue)],
					label = new GUIContent("Default Value"),
					nullable = true,
					flags = option.flags,
				});
			}
			uNodeGUIUtility.DrawTypeDrawer(value.type, new GUIContent("Type"), type => {
				value.type = type;
				uNodeGUIUtility.GUIChangedMajor(value);
			}, targetObject: option.unityObject);
			if(value.GetObjectInParent<NodeContainer>() == null) {
				UInspector.Draw(new DrawerOption() {
					property = option.property[nameof(Variable.modifier)],
					nullable = false,
					flags = option.flags,
					onChanged = _ => {
						uNodeGUIUtility.GUIChangedMajor(value);
					}
				});
				uNodeGUI.DrawAttribute(value.attributes, option.unityObject, (a) => {
					value.attributes = a;
				}, value.modifier.Event ? AttributeTargets.Event : AttributeTargets.Field);
			} else {
				UInspector.Draw(new DrawerOption() {
					property = option.property[nameof(Variable.resetOnEnter)],
					nullable = false,
					flags = option.flags,
				});
			}
			if(uNodeUtility.isPlaying && value.resetOnEnter == false) {//Debug
				uNodeEditor.GetDebugData(out var debugTarget);
				if(debugTarget is IInstancedGraph instancedGraph) {
					var instance = instancedGraph.Instance;
					if(instance != null) {
						var instanceValue = instance.GetElementDataByRef(value);
						uNodeGUI.DrawHeader("Debug");
						uNodeGUIUtility.EditValueLayouted(new GUIContent("Current Value"), instanceValue, value.type, val => {
							instance.SetElementData(value, val);
						});
						if(GUILayout.Button(new GUIContent("Apply As Default Value"), EditorStyles.miniButton)) {
							value.defaultValue = SerializerUtility.Duplicate(instanceValue);
						}
					}
				}
				else if(debugTarget is ISingletonGraph) {
					var instance = (debugTarget as ISingletonGraph).Instance;
					if(instance != null) {
						var instanceValue = instance.GetVariable(value.name);
						uNodeGUI.DrawHeader("Debug");
						uNodeGUIUtility.EditValueLayouted(new GUIContent("Current Value"), instanceValue, value.type, val => {
							instance.SetVariable(value.name, val);
						});
						if(GUILayout.Button(new GUIContent("Apply As Default Value"), EditorStyles.miniButton)) {
							value.defaultValue = SerializerUtility.Duplicate(instanceValue);
						}
					}
				}
			}
		}
	}
}