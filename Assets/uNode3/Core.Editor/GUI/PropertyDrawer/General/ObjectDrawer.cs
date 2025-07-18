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

		public override void Draw(Rect position, DrawerOption option) {
			uNodeGUIUtility.EditValue(position, option.label, option.value, option.property.valueType, val => {
				option.value = val;
			}, new() {
				acceptUnityObject = option.acceptUnityObject,
				attributes = option.attributes,
				nullable = option.nullable,
				unityObject = option.unityObject,
			});
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
			var type = option.property.valueType;
			if(value is IList && (!type.IsArray || type.GetArrayRank() == 1)) {
				DrawList(option);
				return;
			}
			EditorGUI.BeginChangeCheck();
			if(type.IsGenericType || type.IsInterface) {
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
						//EditorGUILayout.LabelField(option.label);
						Rect foldoutRect = EditorGUILayout.GetControlRect();
						option.isExpanded = EditorGUI.Foldout(new Rect(foldoutRect.x, foldoutRect.y, 100, foldoutRect.height), option.isExpanded, option.label, true);
						if(option.isExpanded) {
							EditorGUI.indentLevel++;
							DrawChilds(option);
							EditorGUI.indentLevel--;
						}
					}
					else {
						DrawChilds(option);
					}
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
				GUI.changed = true;
			}
		}
		private readonly HashSet<Type> allowedElementTypes = new HashSet<Type>() {
			typeof(int),
			typeof(float),
			typeof(bool),
			typeof(short),
			typeof(double),
			typeof(long),
			typeof(sbyte),
			typeof(byte),
			typeof(string),
			typeof(Vector2),
			typeof(Vector2Int),
			typeof(Vector3),
			typeof(Vector3Int),
			typeof(Vector4),
			typeof(AnimationCurve),
			typeof(Bounds),
			typeof(BoundsInt),
			typeof(Color),
			typeof(Color32),
			typeof(Gradient),
			typeof(LayerMask),
			typeof(Quaternion),
			typeof(Rect),
			typeof(RectInt),
		};

		private void DrawList(DrawerOption option) {
			Rect foldoutRect = EditorGUILayout.GetControlRect();
			option.isExpanded = EditorGUI.Foldout(new Rect(foldoutRect.x, foldoutRect.y, 100, foldoutRect.height), option.isExpanded, option.label, true);

			var label = option.label;
			var type = option.property.valueType;
			var list = GetValue(option.property, option.property.type, option.nullable) as IList;
			var elementType = type.ElementType();

			int size = EditorGUI.DelayedIntField(new Rect(foldoutRect.x + 120, foldoutRect.y, foldoutRect.width - 120, foldoutRect.height),
				list.Count);
			if(size != list.Count) {
				if(list is Array) {
					list = uNodeUtility.ResizeArray(list as Array, elementType, size);
				}
				else {
					uNodeUtility.ResizeList(list, elementType, size, true);
				}
				option.value = list;
			}

			if(option.isExpanded) {
				if(allowedElementTypes.Contains(elementType) || elementType.IsCastableTo(typeof(UnityEngine.Object))) {
					uNodeGUI.DrawCustomList(list, null,
						drawElement: (position, index, element) => {
							position.height -= 5;
							position.y += 2.5f;
							try {
								uNodeGUIUtility.EditValue(position, new GUIContent("Element " + index), element, elementType, obj => {
									list[index] = obj;
									option.value = list;
								}, new uNodeUtility.EditValueSettings() {
									parentValue = list,
									acceptUnityObject = option.acceptUnityObject,
									attributes = option.attributes,
									nullable = option.nullable,
									unityObject = option.unityObject,
								});
							}
							catch(ExitGUIException) {
								Event.current?.Use();
							}
						},
						add: position => {
							if(list is Array) {
								var arr = list as Array;
								uNodeUtility.AddArray(ref arr, ReflectionUtils.CreateInstance(elementType));
								list = arr;
							}
							else {
								list.Add(ReflectionUtils.CreateInstance(elementType));
							}
							option.value = list;
						},
						remove: index => {
							if(list is Array) {
								var arr = list as Array;
								uNodeUtility.RemoveArrayAt(ref arr, index);
								list = arr;
							}
							else {
								list.RemoveAt(index);
							}
							option.value = list;
						}, headerHeight: 0);
				}
				else {
					EditorGUI.indentLevel++;
					for(int i = 0; i < list.Count; i++) {
						//EditorGUILayout.BeginHorizontal();
						//EditorGUILayout.PrefixLabel($"Element {i}");
						//GUILayout.FlexibleSpace();
						//if(GUILayout.Button("−", GUILayout.Width(20))) {
						//	if(list is Array) {
						//		var arr = list as Array;
						//		uNodeUtility.RemoveArrayAt(ref arr, i);
						//		list = arr;
						//	}
						//	else {
						//		list.RemoveAt(i);
						//	}
						//	break;
						//}
						//EditorGUILayout.EndHorizontal();
						EditorGUI.indentLevel++;
						UInspector.Draw(option.property.Index(i), option.nullable, option.acceptUnityObject, new GUIContent($"Element {i}\t{elementType.PrettyName()}"), flags: option.flags);
						EditorGUI.indentLevel--;
					}
					EditorGUI.indentLevel--;
				}
			}
		}
	}
}