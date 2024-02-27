using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MaxyGames.UNode.Editors.Control {
	class ClassModifierFieldControl : FieldControl<ClassModifier> {
		public override void Draw(Rect position, GUIContent label, object value, Type type, Action<object> onChanged, uNodeUtility.EditValueSettings settings) {
			EditorGUI.BeginChangeCheck();
			ValidateValue(ref value);
			var fieldValue = value as ClassModifier;
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
					});
					menu.AddItem(new GUIContent("Private"), fieldValue.isPrivate && !fieldValue.Internal, () => {
						uNodeEditorUtility.RegisterUndo(settings.unityObject, "");
						fieldValue.SetPrivate();
						onChanged(fieldValue);
					});
					bool flag = false;
					if(settings.unityObject != null) {
						GraphSystemAttribute graphSystem = GraphUtility.GetGraphSystem(settings.unityObject);
						if(graphSystem != null) {
							flag = graphSystem.supportModifier;
							flag &= graphSystem.isScriptGraph || settings.unityObject is IScriptGraphType;
						}
					}
					if(flag) {
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
						});
						menu.AddSeparator("");
						menu.AddItem(new GUIContent("Abstract"), fieldValue.Abstract, () => {
							uNodeEditorUtility.RegisterUndo(settings.unityObject, "");
							fieldValue.Abstract = !fieldValue.Abstract;
							fieldValue.Static = false;
							onChanged(fieldValue);
						});
						menu.AddItem(new GUIContent("Static"), fieldValue.Static, () => {
							uNodeEditorUtility.RegisterUndo(settings.unityObject, "");
							fieldValue.Abstract = false;
							fieldValue.Static = !fieldValue.Static;
							onChanged(fieldValue);
						});
						menu.AddItem(new GUIContent("Sealed"), fieldValue.Sealed, () => {
							uNodeEditorUtility.RegisterUndo(settings.unityObject, "");
							fieldValue.Abstract = false;
							fieldValue.Static = false;
							fieldValue.Sealed = !fieldValue.Sealed;
							onChanged(fieldValue);
						});
						menu.AddSeparator("");
						menu.AddItem(new GUIContent("Partial"), fieldValue.Partial, () => {
							uNodeEditorUtility.RegisterUndo(settings.unityObject, "");
							fieldValue.Partial = !fieldValue.Partial;
							onChanged(fieldValue);
						});
						if(settings.unityObject is IClassGraph classGraph && classGraph.IsStruct) {
							menu.AddItem(new GUIContent("Read Only"), fieldValue.ReadOnly, () => {
								uNodeEditorUtility.RegisterUndo(settings.unityObject, "");
								fieldValue.ReadOnly = !fieldValue.ReadOnly;
								onChanged(fieldValue);
							});
						}
					}
					menu.ShowAsContext();
				}
			}
			if(EditorGUI.EndChangeCheck()) {
				onChanged(fieldValue);
			}
		}
	}
}