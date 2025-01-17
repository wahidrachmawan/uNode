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
				ObjectTypeAttribute drawer = ReflectionUtils.GetAttribute<ObjectTypeAttribute>(attributes);
				FilterAttribute filter = ReflectionUtils.GetAttribute<FilterAttribute>(attributes);
				if(drawer != null) {
					if(settings.nullable)
						position.width -= 16;
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
					if(settings.nullable)
						position.width -= 16;
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
					position.width -= 16;
					position.height = EditorGUIUtility.singleLineHeight;
					fieldValue = EditorGUI.TextField(position, label, fieldValue);
					position.x += position.width;
					position.width = 16;
					if(EditorGUI.DropdownButton(position, GUIContent.none, FocusType.Keyboard) && Event.current.button == 0) {
						GUI.changed = false;
						ActionPopupWindow.Show(position.ToScreenRect(), () => {
							if(settings.nullable) {
								EditorGUILayout.BeginHorizontal();
								EditorGUILayout.BeginVertical();
							}
							EditorGUI.BeginChangeCheck();
							fieldValue = EditorGUILayout.TextArea(fieldValue);
							if(EditorGUI.EndChangeCheck()) {
								onChanged(fieldValue);
							}
							if(settings.nullable) {
								EditorGUILayout.EndVertical();
								if(GUILayout.Button(GUIContent.none, GUILayout.Width(16)) && Event.current.button == 0) {
									if(fieldValue != null) {
										fieldValue = null;
									}
									else {
										fieldValue = "";
									}
									onChanged(fieldValue);
								}
								EditorGUILayout.EndHorizontal();
							}
						});
					}
					if(EditorGUI.EndChangeCheck()) {
						onChanged(fieldValue);
					}
					return;
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
					if(settings.nullable) {
						EditorGUILayout.BeginHorizontal();
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
					if(settings.nullable) {
						if(GUILayout.Button(GUIContent.none, GUILayout.Width(16)) && Event.current.button == 0) {
							fieldValue = null;
							GUI.changed = true;
						}
						EditorGUILayout.EndHorizontal();
					}
				}
				else if(filter != null) {
					filter.ArrayManipulator = false;
					filter.OnlyGetType = true;
					filter.UnityReference = false;
					if(settings.nullable) {
						EditorGUILayout.BeginHorizontal();
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
					if(settings.nullable) {
						if(GUILayout.Button(GUIContent.none, GUILayout.Width(16)) && Event.current.button == 0) {
							fieldValue = null;
							GUI.changed = true;
						}
						EditorGUILayout.EndHorizontal();
					}
				}
				else {
					TextAreaAttribute textAtt = ReflectionUtils.GetAttribute<TextAreaAttribute>(attributes);
					if(textAtt != null) {
						if(settings.nullable) {
							EditorGUILayout.BeginHorizontal();
							EditorGUILayout.BeginVertical();
						}
						EditorGUILayout.BeginHorizontal();
						EditorGUILayout.PrefixLabel(label);
						fieldValue = EditorGUILayout.TextArea(fieldValue);
						EditorGUILayout.EndHorizontal();
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
						EditorGUILayout.BeginHorizontal();
						fieldValue = EditorGUILayout.TextField(label, fieldValue);
						var pos = uNodeGUIUtility.GetRect(GUILayout.Width(18));
						if(EditorGUI.DropdownButton(pos, GUIContent.none, FocusType.Keyboard)) {
							GUI.changed = false;
							ActionPopupWindow.Show(pos.ToScreenRect(), () => {
								if(settings.nullable) {
									EditorGUILayout.BeginHorizontal();
									EditorGUILayout.BeginVertical();
								}
								EditorGUI.BeginChangeCheck();
								fieldValue = EditorGUILayout.TextArea(fieldValue);
								if(EditorGUI.EndChangeCheck()) {
									onChanged(fieldValue);
								}
								if(settings.nullable) {
									EditorGUILayout.EndVertical();
									if(GUILayout.Button(GUIContent.none, GUILayout.Width(16)) && Event.current.button == 0) {
										if(fieldValue != null) {
											fieldValue = null;
										}
										else {
											fieldValue = "";
										}
										onChanged(fieldValue);
									}
									EditorGUILayout.EndHorizontal();
								}
							});
						}
						EditorGUILayout.EndHorizontal();
						if(EditorGUI.EndChangeCheck()) {
							onChanged(fieldValue);
						}
						return;
					}
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