using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MaxyGames.UNode.Editors.Control {
	class PropertyModifierFieldControl : FieldControl<PropertyModifier> {
		public override void Draw(Rect position, GUIContent label, object value, Type type, Action<object> onChanged, uNodeUtility.EditValueSettings settings) {
			EditorGUI.BeginChangeCheck();
			ValidateValue(ref value);
			var fieldValue = value as PropertyModifier;
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
						});
						if(isScriptGraph) {
							menu.AddSeparator("");
							menu.AddItem(new GUIContent("Abstract"), fieldValue.Abstract, () => {
								uNodeEditorUtility.RegisterUndo(settings.unityObject, "");
								fieldValue.Abstract = !fieldValue.Abstract;
								fieldValue.Override = false;
								fieldValue.Static = false;
								fieldValue.Virtual = false;
								onChanged(fieldValue);
							});
							menu.AddItem(new GUIContent("Static"), fieldValue.Static, () => {
								uNodeEditorUtility.RegisterUndo(settings.unityObject, "");
								fieldValue.Static = !fieldValue.Static;
								fieldValue.Override = false;
								fieldValue.Abstract = false;
								fieldValue.Virtual = false;
								onChanged(fieldValue);
							});
							menu.AddItem(new GUIContent("Virtual"), fieldValue.Virtual, () => {
								uNodeEditorUtility.RegisterUndo(settings.unityObject, "");
								fieldValue.Virtual = !fieldValue.Virtual;
								fieldValue.Override = false;
								fieldValue.Static = false;
								fieldValue.Abstract = false;
								onChanged(fieldValue);
							});
							menu.AddItem(new GUIContent("Override"), fieldValue.Override, () => {
								uNodeEditorUtility.RegisterUndo(settings.unityObject, "");
								fieldValue.Virtual = false;
								fieldValue.Override = !fieldValue.Override;
								fieldValue.Static = false;
								fieldValue.Abstract = false;
								onChanged(fieldValue);
							});
						}
						//TODO: add support for virtual/override properties
						//else if(settings.unityObject is IClassGraph) {
						//	menu.AddSeparator("");
						//	menu.AddItem(new GUIContent("Override"), fieldValue.Override, () => {
						//		uNodeEditorUtility.RegisterUndo(settings.unityObject, "");
						//		fieldValue.Virtual = false;
						//		fieldValue.Override = !fieldValue.Override;
						//		fieldValue.Static = false;
						//		fieldValue.Abstract = false;
						//		onChanged(fieldValue);
						//	});
						//	menu.AddItem(new GUIContent("Virtual"), fieldValue.Virtual, () => {
						//		uNodeEditorUtility.RegisterUndo(settings.unityObject, "");
						//		fieldValue.Virtual = !fieldValue.Virtual;
						//		fieldValue.Override = false;
						//		fieldValue.Static = false;
						//		fieldValue.Abstract = false;
						//		onChanged(fieldValue);
						//	});
						//}
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