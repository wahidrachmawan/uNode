using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace MaxyGames.UNode.Editors.Drawer {
	class StringPropertyDrawer : UPropertyDrawer<string> {
		public override void Draw(Rect position, DrawerOption option) {
			EditorGUI.BeginChangeCheck();
			var fieldValue = GetValue(option.property, option.nullable);
			if(fieldValue != null) {
				if(option.nullable)
					position.width -= 16;
				ObjectTypeAttribute drawer = ReflectionUtils.GetAttribute<ObjectTypeAttribute>(option.attributes);
				FilterAttribute filter = ReflectionUtils.GetAttribute<FilterAttribute>(option.attributes);
				if(drawer != null) {
					if(filter != null) {
						if(drawer.type != null)
							filter.Types.Add(drawer.type);
						filter.ArrayManipulator = false;
						filter.OnlyGetType = true;
						filter.UnityReference = false;
					}
					uNodeGUIUtility.DrawTypeDrawer(position, TypeSerializer.Deserialize(fieldValue, false), option.label, delegate (Type t) {
						if(t != null) {
							fieldValue = t.FullName;
						} else {
							fieldValue = "";
						}
						option.value = fieldValue;
					}, filter, option.unityObject);
				} else if(filter != null) {
					filter.ArrayManipulator = false;
					filter.OnlyGetType = true;
					filter.UnityReference = false;
					uNodeGUIUtility.DrawTypeDrawer(position, TypeSerializer.Deserialize(fieldValue, false), option.label, delegate (Type t) {
						if(t != null) {
							fieldValue = t.FullName;
						} else {
							fieldValue = "";
						}
						option.value = fieldValue;
					}, filter, option.unityObject);
				} else {
					TextAreaAttribute textAtt = ReflectionUtils.GetAttribute<TextAreaAttribute>(option.attributes);
					if(textAtt != null) {
						position = EditorGUI.PrefixLabel(position, option.label);
						fieldValue = EditorGUI.TextArea(position, fieldValue);
					} else {
						fieldValue = EditorGUI.TextField(position, option.label, fieldValue);
					}
				}
				if(option.nullable) {
					position.x += position.width;
					position.width = 16;
					if(GUI.Button(position, GUIContent.none) && Event.current.button == 0) {
						fieldValue = null;
						GUI.changed = true;
					}
				}
			} else {
				uNodeGUIUtility.DrawNullValue(position, option.label, option.type, delegate (object o) {
					option.value = fieldValue = o as string;
				});
			}
			if(EditorGUI.EndChangeCheck()) {
				option.value = fieldValue;
			}
		}

		public override void DrawLayouted(DrawerOption option) {
			DrawDecorators(option);
			var attributes = option.attributes;
			EditorGUI.BeginChangeCheck();
			var fieldValue = GetValue(option.property, option.nullable);
			if(fieldValue != null) {
				if(option.nullable) {
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
					uNodeGUIUtility.DrawTypeDrawer(TypeSerializer.Deserialize(fieldValue, false), option.label, delegate (Type t) {
						if(t != null) {
							fieldValue = t.FullName;
						} else {
							fieldValue = "";
						}
						option.value = fieldValue;
					}, filter, option.unityObject);
				} else if(filter != null) {
					filter.ArrayManipulator = false;
					filter.OnlyGetType = true;
					filter.UnityReference = false;
					uNodeGUIUtility.DrawTypeDrawer(TypeSerializer.Deserialize(fieldValue, false), option.label, delegate (Type t) {
						if(t != null) {
							fieldValue = t.FullName;
						} else {
							fieldValue = "";
						}
						option.value = fieldValue;
					}, filter, option.unityObject);
				} else {
					TextAreaAttribute textAtt = ReflectionUtils.GetAttribute<TextAreaAttribute>(attributes);
					if(textAtt != null) {
						EditorGUILayout.BeginHorizontal();
						EditorGUILayout.PrefixLabel(option.label);
						var level = EditorGUI.indentLevel;
						EditorGUI.indentLevel = 0;
						fieldValue = EditorGUILayout.TextArea(fieldValue);
						EditorGUI.indentLevel = level;
						EditorGUILayout.EndHorizontal();
					} else {
						fieldValue = EditorGUILayout.TextField(option.label, fieldValue);
					}
				}
				if(option.nullable) {
					EditorGUILayout.EndVertical();
					if(GUILayout.Button(GUIContent.none, GUILayout.Width(16)) && Event.current.button == 0) {
						fieldValue = null;
						GUI.changed = true;
					}
					EditorGUILayout.EndHorizontal();
				}
			} else {
				uNodeGUIUtility.DrawNullValue(uNodeGUIUtility.GetRect(), option.label, option.type, delegate (object o) {
					option.value = fieldValue = o as string;
				});
			}
			if(EditorGUI.EndChangeCheck()) {
				option.value = fieldValue;
			}
		}
	}
}