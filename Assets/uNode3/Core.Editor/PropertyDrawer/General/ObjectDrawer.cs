using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace MaxyGames.UNode.Editors.Drawer {
	class ObjectDrawer : UPropertyDrawer {
		public override int order => int.MaxValue;

		public override bool IsValid(Type type, bool layouted) {
			return true;
		}

		public override void DrawLayouted(DrawerOption option) {
			object value = GetValue(option.property, option.property.type, option.nullable);
			
			if(option.property.type != typeof(object)) {
				var control = FieldControl.FindControl(option.type, true);
				if(control != null) {
					control.DrawLayouted(value, option.label, option.type, (val) => {
						uNodeEditorUtility.RegisterUndo(option.unityObject, "");
						option.value = val;
					}, new uNodeUtility.EditValueSettings() {
						attributes = option.attributes,
						unityObject = option.unityObject,
						nullable = option.nullable,
						parentValue = option.property.parent.value,
					});
					return;
				}
			}
			EditorGUI.BeginChangeCheck();
			var type = option.property.valueType;
			if(type.IsArray) {
				if(type.GetArrayRank() == 1) {
					Array array = value as Array;
					if(array == null) {
						if(option.nullable) {
							if(value != null)
								GUI.changed = true;
							array = null;
						} else {
							array = Array.CreateInstance(type.GetElementType(), 0);
							GUI.changed = true;
						}
					}
					if(array != null) {
						Rect position = uNodeGUIUtility.GetRect();
						if(option.nullable)
							position.width -= 16;
						int num = EditorGUI.IntField(position, option.label, array.Length);
						if(option.nullable) {
							position.x += position.width;
							position.width = 16;
							if(EditorGUI.DropdownButton(position, GUIContent.none, FocusType.Keyboard, EditorStyles.miniButton) && Event.current.button == 0) {
								array = null;
								option.value = null;
							}
						}
						Array newArray = array;
						if(newArray != null) {
							if(num != array.Length) {
								newArray = uNodeUtility.ResizeArray(array, type.GetElementType(), num);
								option.value = newArray;
							}
							if(newArray.Length > 0) {
								//Event currentEvent = Event.current;
								EditorGUI.indentLevel++;
								for(int i = 0; i < newArray.Length; i++) {
									//var elementToEdit = newArray.GetValue(i);
									UInspector.Draw(new DrawerOption() {
										property = option.property.Index(i),
										nullable = option.nullable,
										acceptUnityObject = option.acceptUnityObject,
									});
								}
								EditorGUI.indentLevel--;
							}
						}
						if(EditorGUI.EndChangeCheck()) {
							option.value = newArray;
						}
						return;
					}
				} else {

				}
			} else if(type.IsGenericType || type.IsInterface) {
				uNodeGUIUtility.EditValueLayouted(option.label, value, type,
					onChange: (val) => {
						option.value = value;
					}, new uNodeUtility.EditValueSettings() { unityObject = option.unityObject });
			} else {
				if(value == null && !option.nullable) {
					if(ReflectionUtils.CanCreateInstance(option.type)) {
						value = ReflectionUtils.CreateInstance(option.type);
						option.value = value;
					}
				}
				if(value != null) {
					if(value.GetType() != type) {
						var drawer = UPropertyDrawer.FindDrawer(type, true);
						if(drawer != this) {
							EditorGUI.EndChangeCheck();
							drawer.DrawLayouted(option);
							return;
						}
					}
					if(option.label != null) {
						EditorGUILayout.LabelField(option.label);
						EditorGUI.indentLevel++;
					}
					DrawChilds(option);
					if(option.label != null)
						EditorGUI.indentLevel--;
					EditorGUI.EndChangeCheck();
				} else {
					uNodeGUIUtility.DrawNullValue(option.label, option.type, (obj) => {
						option.value = obj;
					});
				}
				return;
			}
			if(EditorGUI.EndChangeCheck()) {
				option.value = value;
			}
		}
	}
}