using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MaxyGames.UNode.Editors.Control {
	class FieldModifierFieldControl : FieldControl<FieldModifier> {
		public override void Draw(Rect position, GUIContent label, object value, Type type, Action<object> onChanged, uNodeUtility.EditValueSettings settings) {
			EditorGUI.BeginChangeCheck();
			ValidateValue(ref value);
			var fieldValue = value as FieldModifier;
			if(value != null) {
				position = EditorGUI.PrefixLabel(position, label);
				if(EditorGUI.DropdownButton(position, new GUIContent(fieldValue.GenerateCode()), FocusType.Keyboard)) {
					GenericMenu menu = new GenericMenu();
					menu.AddItem(new GUIContent("Public"), fieldValue.isPublic, () => {
						uNodeEditorUtility.RegisterUndo(settings.unityObject, "");
						if(fieldValue.isPublic) {
							fieldValue.Public = false;
						}
						else {
							fieldValue.SetPublic();
						}
						onChanged(fieldValue);
						if(settings.unityObject is IGraph) {
							EditorReflectionUtility.UpdateRuntimeType(settings.unityObject as IGraph);
						}
					});
					menu.AddItem(new GUIContent("Private"), fieldValue.isPrivate && !fieldValue.Internal, () => {
						uNodeEditorUtility.RegisterUndo(settings.unityObject, "");
						fieldValue.SetPrivate();
						onChanged(fieldValue);
						if(settings.unityObject is IGraph) {
							EditorReflectionUtility.UpdateRuntimeType(settings.unityObject as IGraph);
						}
					});
					bool flag = false;
					bool isScriptGraph = false;
					if(settings.unityObject != null) {
						GraphSystemAttribute graphSystem = GraphUtility.GetGraphSystem(settings.unityObject);
						if(graphSystem != null) {
							flag = graphSystem.supportModifier;
							isScriptGraph = graphSystem.isScriptGraph || settings.unityObject is IScriptGraphType;
						}
					}
					if(flag) {
						menu.AddItem(new GUIContent("Protected"), fieldValue.isProtected, () => {
							uNodeEditorUtility.RegisterUndo(settings.unityObject, "");
							if(fieldValue.isProtected) {
								fieldValue.Protected = false;
							}
							else {
								fieldValue.SetProtected();
							}
							onChanged(fieldValue);
							if(settings.unityObject is IGraph) {
								EditorReflectionUtility.UpdateRuntimeType(settings.unityObject as IGraph);
							}
						});
						menu.AddItem(new GUIContent("Internal"), fieldValue.Internal, () => {
							uNodeEditorUtility.RegisterUndo(settings.unityObject, "");
							if(fieldValue.Internal) {
								fieldValue.Internal = false;
							}
							else {
								fieldValue.Public = false;
								fieldValue.Private = false;
								fieldValue.Internal = true;
							}
							onChanged(fieldValue);
							if(settings.unityObject is IGraph) {
								EditorReflectionUtility.UpdateRuntimeType(settings.unityObject as IGraph);
							}
						});
						if(isScriptGraph) {
							menu.AddSeparator("");
							menu.AddItem(new GUIContent("Static"), fieldValue.Static, () => {
								uNodeEditorUtility.RegisterUndo(settings.unityObject, "");
								fieldValue.Static = !fieldValue.Static;
								fieldValue.Const = false;
								onChanged(fieldValue);
								if(settings.unityObject is IGraph) {
									EditorReflectionUtility.UpdateRuntimeType(settings.unityObject as IGraph);
								}
							});
							menu.AddItem(new GUIContent("Event"), fieldValue.Event, () => {
								uNodeEditorUtility.RegisterUndo(settings.unityObject, "");
								fieldValue.Event = !fieldValue.Event;
								fieldValue.Const = false;
								onChanged(fieldValue);
								if(settings.unityObject is IGraph) {
									EditorReflectionUtility.UpdateRuntimeType(settings.unityObject as IGraph);
								}
							});
							menu.AddItem(new GUIContent("ReadOnly"), fieldValue.ReadOnly, () => {
								uNodeEditorUtility.RegisterUndo(settings.unityObject, "");
								fieldValue.ReadOnly = !fieldValue.ReadOnly;
								fieldValue.Const = false;
								onChanged(fieldValue);
								if(settings.unityObject is IGraph) {
									EditorReflectionUtility.UpdateRuntimeType(settings.unityObject as IGraph);
								}
							});
							menu.AddItem(new GUIContent("Const"), fieldValue.Const, () => {
								uNodeEditorUtility.RegisterUndo(settings.unityObject, "");
								fieldValue.Const = !fieldValue.Const;
								fieldValue.Static = false;
								fieldValue.ReadOnly = false;
								fieldValue.Event = false;
								onChanged(fieldValue);
								if(settings.unityObject is IGraph) {
									EditorReflectionUtility.UpdateRuntimeType(settings.unityObject as IGraph);
								}
							});
						}
					}
					menu.ShowAsContext();
				}
			}
			if(EditorGUI.EndChangeCheck()) {
				onChanged(fieldValue);
				if(settings.unityObject is IGraph) {
					EditorReflectionUtility.UpdateRuntimeType(settings.unityObject as IGraph);
				}
			}
		}
	}
}