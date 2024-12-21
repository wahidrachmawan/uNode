using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MaxyGames.UNode.Editors.Control {
	class ListFieldControl : FieldControl {
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

		public override bool IsValidControl(Type type, bool layouted) {
			if(layouted) {
				if(type.IsGenericType && type.HasImplementInterface(typeof(IList)) || type.IsArray && type.GetArrayRank() == 1) {
					var elementType = type.ElementType();
					return allowedElementTypes.Contains(elementType) || elementType.IsCastableTo(typeof(UnityEngine.Object));
				}
			}
			return false;
		}

		public override void DrawLayouted(object value, GUIContent label, Type type, Action<object> onChanged, uNodeUtility.EditValueSettings settings) {
			if(ValidateValue(ref value, type)) {
				onChanged(value);
			}
			IList list = value as IList;
			var elementType = type.ElementType();
			int size = EditorGUILayout.DelayedIntField(label, list.Count);
			if(size != list.Count) {
				if(list is Array) {
					list = uNodeUtility.ResizeArray(list as Array, elementType, size);
				}
				else {
					uNodeUtility.ResizeList(list, elementType, size, true);
				}
				onChanged(list);
			}
			if(size != 0) {
				uNodeGUI.DrawCustomList(list, null,
					drawElement: (position, index, element) => {
						position.height -= 4;
						position.y += 2;
						uNodeGUIUtility.EditValue(position, new GUIContent("Element " + index), element, elementType, obj => {
							list[index] = obj;
							onChanged(list);
						}, new uNodeUtility.EditValueSettings(settings) { parentValue = list });
					},
					add: position => {
						if(list is Array) {
							var arr = list as Array;
							uNodeUtility.AddArray(ref arr, ReflectionUtils.CreateInstance(type.ElementType()));
							list = arr;
						}
						else {
							list.Add(ReflectionUtils.CreateInstance(type.ElementType()));
						}
						onChanged(list);
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
						onChanged(list);
					}, headerHeight: 0);
			}
		}
	}
}