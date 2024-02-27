using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MaxyGames.UNode.Editors.Control {
	class StringFieldControl : FieldControl<string> {
		public override void Draw(Rect position, GUIContent label, object value, Type type, Action<object> onChanged, uNodeUtility.EditValueSettings settings) {
			var attributes = settings.attributes;
			EditorGUI.BeginChangeCheck();
			ValidateValue(ref value, settings != null ? settings.nullable : false);
			var fieldValue = value as string;
			if(value != null) {
				if(settings.nullable)
					position.width -= 16;
				ObjectTypeAttribute drawer = ReflectionUtils.GetAttribute<ObjectTypeAttribute>(attributes);
				FilterAttribute filter = ReflectionUtils.GetAttribute<FilterAttribute>(attributes);
				if(drawer != null) {
					if(filter != null) {
						if(drawer.type != null)
							filter.Types.Add(drawer.type);
						filter.ArrayManipulator = false;
						filter.OnlyGetType = true;
						filter.UnityReference = false;
					}
					uNodeGUIUtility.DrawTypeDrawer(position, TypeSerializer.Deserialize(fieldValue, false), label, delegate (Type t) {
						if(t != null) {
							fieldValue = t.FullName;
						}
						else {
							fieldValue = "";
						}
						onChanged(fieldValue);
					}, filter, settings.unityObject);
				}
				else if(filter != null) {
					filter.ArrayManipulator = false;
					filter.OnlyGetType = true;
					filter.UnityReference = false;
					uNodeGUIUtility.DrawTypeDrawer(position, TypeSerializer.Deserialize(fieldValue, false), label, delegate (Type t) {
						if(t != null) {
							fieldValue = t.FullName;
						}
						else {
							fieldValue = "";
						}
						onChanged(fieldValue);
					}, filter, settings.unityObject);
				}
				else {
					TextAreaAttribute textAtt = ReflectionUtils.GetAttribute<TextAreaAttribute>(attributes);
					if(textAtt != null) {
						position = EditorGUI.PrefixLabel(position, label);
						fieldValue = EditorGUI.TextArea(position, fieldValue);
					}
					else {
						fieldValue = EditorGUI.TextField(position, label, fieldValue);
					}
				}
				if(settings.nullable) {
					position.x += position.width;
					position.width = 16;
					if(GUI.Button(position, GUIContent.none) && Event.current.button == 0) {
						fieldValue = null;
						GUI.changed = true;
					}
				}
			}
			else {
				uNodeGUIUtility.DrawNullValue(position, label, type, delegate (object o) {
					onChanged(fieldValue = o as string);
				});
			}
			if(EditorGUI.EndChangeCheck()) {
				onChanged(fieldValue);
			}
		}

		public override void DrawLayouted(object value, GUIContent label, Type type, Action<object> onChanged, uNodeUtility.EditValueSettings settings) {
			DrawDecorators(settings);
			var attributes = settings.attributes;
			EditorGUI.BeginChangeCheck();
			ValidateValue(ref value, settings != null ? settings.nullable : false);
			var fieldValue = value as string;
			if(value != null) {
				if(settings.nullable) {
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.BeginVertical();
				}
				ObjectTypeAttribute drawer = ReflectionUtils.GetAttribute<ObjectTypeAttribute>(attributes);
				FilterAttribute filter = ReflectionUtils.GetAttribute<FilterAttribute>(attributes);
				if(drawer != null) {
					if(filter != null) {
						if(drawer.type != null)
							filter.Types.Add(drawer.type);
						filter.ArrayManipulator = false;
						filter.OnlyGetType = true;
						filter.UnityReference = false;
					}
					uNodeGUIUtility.DrawTypeDrawer(TypeSerializer.Deserialize(fieldValue, false), label, delegate (Type t) {
						if(t != null) {
							fieldValue = t.FullName;
						}
						else {
							fieldValue = "";
						}
						onChanged(fieldValue);
					}, filter, settings.unityObject);
				}
				else if(filter != null) {
					filter.ArrayManipulator = false;
					filter.OnlyGetType = true;
					filter.UnityReference = false;
					uNodeGUIUtility.DrawTypeDrawer(TypeSerializer.Deserialize(fieldValue, false), label, delegate (Type t) {
						if(t != null) {
							fieldValue = t.FullName;
						}
						else {
							fieldValue = "";
						}
						onChanged(fieldValue);
					}, filter, settings.unityObject);
				}
				else {
					TextAreaAttribute textAtt = ReflectionUtils.GetAttribute<TextAreaAttribute>(attributes);
					if(textAtt != null) {
						EditorGUILayout.BeginHorizontal();
						EditorGUILayout.PrefixLabel(label);
						fieldValue = EditorGUILayout.TextArea(fieldValue);
						EditorGUILayout.EndHorizontal();
					}
					else {
						fieldValue = EditorGUILayout.TextField(label, fieldValue);
					}
				}
				if(settings.nullable) {
					EditorGUILayout.EndVertical();
					if(GUILayout.Button(GUIContent.none, GUILayout.Width(16)) && Event.current.button == 0) {
						fieldValue = null;
						GUI.changed = true;
					}
					EditorGUILayout.EndHorizontal();
				}
			}
			else {
				uNodeGUIUtility.DrawNullValue(uNodeGUIUtility.GetRect(), label, type, delegate (object o) {
					onChanged(fieldValue = o as string);
				});
			}
			if(EditorGUI.EndChangeCheck()) {
				onChanged(fieldValue);
			}
		}
	}
}