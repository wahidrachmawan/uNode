using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace MaxyGames.UNode.Editors.Drawer {
	public abstract class CustomVariableDrawer {
		public virtual int order => 0;
		public abstract bool DoDraw(ref DrawerOption option, Variable variable);
	}

	class VariableDrawer : UGraphElementDrawer<Variable> {
		protected override void DrawHeader(ref DrawerOption option) {
			var value = option.value as Variable;
			DrawNicelyHeader(ref option, value.type);
		}

		static readonly Lazy<List<CustomVariableDrawer>> customVariableDrawers = new(() => {
			var result = EditorReflectionUtility.GetListOfType<CustomVariableDrawer>();
			result.Sort((x, y) => CompareUtility.Compare(x.order, y.order));
			return result;
		});

		protected override void DoDraw(ref DrawerOption option) {
			var value = option.value as Variable;
			foreach(var drawer in customVariableDrawers.Value) {
				if(drawer.DoDraw(ref option, value)) {
					return;
				}
			}
			var container = value.graphContainer;
			if(container.GetGraphInheritType() != typeof(ValueType) || value.modifier.Const) {
				UInspector.Draw(new DrawerOption() {
					property = option.property[nameof(Variable.serializedValue)],
					label = new GUIContent("Default Value"),
					nullable = true,
					flags = option.flags,
				});
			}
			uNodeGUIUtility.EditType(value.type, new GUIContent("Type"), type => {
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

				if(container is not IScriptGraphType && container is not IInstancedGraph && container is not IRuntimeClass && value.showInInspector) {
					UInspector.Draw(new DrawerOption() {
						property = option.property[nameof(Variable.alwaysOverride)],
						nullable = false,
						flags = option.flags,
					});
				}
				else {
					value.alwaysOverride = false;
				}

				uNodeGUI.DrawAttribute(value.attributes, option.unityObject, (a) => {
					value.attributes = a;
				}, value.modifier.Event ? AttributeTargets.Event : AttributeTargets.Field);
			}
			else {
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