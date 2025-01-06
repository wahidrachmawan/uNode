using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Xml;
using System.Reflection;
using System.Collections.Generic;
using UnityEditorInternal;
using Object = UnityEngine.Object;
using System.Collections;
using System.Runtime.CompilerServices;

namespace MaxyGames.UNode.Editors {
	public static class uNodeGUI {
		#region Reorderable
		static ConditionalWeakTable<object, ReorderableList> _reorderabeMap = new ConditionalWeakTable<object, ReorderableList>();
		static ConditionalWeakTable<object, ReorderableList> _reorderabeMap2 = new ConditionalWeakTable<object, ReorderableList>();

		static class CopyPasteValue {
			static Dictionary<Type, OdinSerializedData> map = new();
			static Dictionary<Type, List<OdinSerializedData>> element_Map = new();

			public static void Copy(object value, Type type) {
				map[type] = SerializerUtility.SerializeValue(value);
			}

			public static void CopyAsElement(object value, Type type) {
				element_Map[type] = new List<OdinSerializedData>() { SerializerUtility.SerializeValue(value) };
			}

			public static void CopyElement(IEnumerable value, Type elementType) {
				List<OdinSerializedData> list = new();
				foreach(var v in value) {
					list.Add(SerializerUtility.SerializeValue(v));
				}
				element_Map[elementType] = list;
			}

			public static object GetValue(Type type) {
				if(map.TryGetValue(type, out var result)) {
					return result;
				}
				return null;
			}

			public static object[] GetElementValue(Type elementType) {
				if(element_Map.TryGetValue(elementType, out var result)) {
					object[] objects = new object[result.Count];
					for(int i = 0; i < objects.Length; i++) {
						objects[i] = result[i].ToValue();
					}
					return objects;
				}
				return null;
			}

			public static T[] GetElementValue<T>() {
				if(element_Map.TryGetValue(typeof(T), out var result)) {
					T[] objects = new T[result.Count];
					for(int i = 0; i < objects.Length; i++) {
						var val = result[i].ToValue();
						if(val is T) {
							objects[i] = (T)val;
						}
					}
					return objects;
				}
				return null;
			}

			public static bool HasValue(Type type) {
				return map.ContainsKey(type);
			}

			public static bool HasElementValue(Type elementType) {
				return element_Map.ContainsKey(elementType);
			}
		}

		public static ReorderableList GetReorderableList<T>(
			List<T> values,
			string headerLabel,
			Action<Rect, int, T> drawElement,
			Action<Rect> add,
			Action<int> remove,
			ReorderableList.ReorderCallbackDelegateWithDetails reorder = null,
			ReorderableList.ElementHeightCallbackDelegate elementHeight = null) {
			if(values == null) {
				throw new ArgumentNullException(nameof(values));
			}
			ReorderableList reorderable;
			if(!_reorderabeMap.TryGetValue(values, out reorderable)) {
				reorderable = new ReorderableList(values as System.Collections.IList, typeof(T));
				reorderable.drawHeaderCallback = (pos) => {
					EditorGUI.LabelField(pos, headerLabel);
					if(Event.current.button == 1 && Event.current.clickCount == 1 && pos.Contains(Event.current.mousePosition)) {
						GenericMenu menu = new GenericMenu();
						menu.AddItem(new GUIContent("Copy"), false, () => {
							CopyPasteValue.CopyElement(values, typeof(T));
						});
						if(CopyPasteValue.HasElementValue(typeof(T))) {
							menu.AddItem(new GUIContent("Paste as new"), false, () => {
								//if(unityObject != null)
								//	uNodeEditorUtility.RegisterUndo(unityObject, "Paste");
								var pasteValues = CopyPasteValue.GetElementValue<T>();
								values.AddRange(pasteValues);
							});
							menu.AddItem(new GUIContent("Paste as overwrite"), false, () => {
								//if(unityObject != null)
								//	uNodeEditorUtility.RegisterUndo(unityObject, "Paste");
								var pasteValues = CopyPasteValue.GetElementValue<T>();
								values.Clear();
								values.AddRange(pasteValues);
							});
						}
						menu.ShowAsContext();
					}
				};
				reorderable.drawElementCallback = (pos, index, isActive, isFocused) => {
					try {
						drawElement(pos, index, values[index]);
					}
					catch(Exception ex) { Debug.LogException(ex); }
				};
				reorderable.drawElementBackgroundCallback = (pos, index, isActive, isFocused) => {
					if(Event.current.type == EventType.Repaint) {
						ReorderableList.defaultBehaviours.elementBackground.Draw(pos, false, isActive, isActive, isFocused);
					}
					if(pos.Contains(Event.current.mousePosition) && Event.current.button == 1 && Event.current.clickCount == 1 && Event.current.mousePosition.x > 20 && Event.current.mousePosition.x < 38) {
						GenericMenu menu = new GenericMenu();
						menu.AddItem(new GUIContent("Copy"), false, () => {
							CopyPasteValue.CopyAsElement(values[index], typeof(T));
						});
						if(CopyPasteValue.HasElementValue(typeof(T))) {
							menu.AddItem(new GUIContent("Paste as new"), false, () => {
								//if(unityObject != null)
								//	uNodeEditorUtility.RegisterUndo(unityObject, "Paste");
								var pasteValues = CopyPasteValue.GetElementValue<T>();
								for(int i = pasteValues.Length - 1; i >= 0; i--) {
									values.Insert(index, pasteValues[i]);
								}
							});
						}
						menu.ShowAsContext();
					}
				};
				if(elementHeight != null) {
					reorderable.elementHeightCallback = elementHeight;
				}
				reorderable.displayAdd = add != null;
				reorderable.displayRemove = remove != null;
				if(reorderable.displayAdd) {
					reorderable.onAddDropdownCallback = (pos, list) => {
						add(pos);
						reorderable.list = values as System.Collections.IList;
					};
				}
				if(reorderable.displayRemove) {
					reorderable.onRemoveCallback = (list) => {
						remove(reorderable.index);
						reorderable.list = values as System.Collections.IList;
					};
				}
				reorderable.onReorderCallbackWithDetails = reorder;
				_reorderabeMap.AddOrUpdate(values, reorderable);
			}
			return reorderable;
		}

		public static void DrawCustomList<T>(
			List<T> values,
			string headerLabel,
			Action<Rect, int, T> drawElement,
			Action<Rect> add,
			Action<int> remove,
			ReorderableList.ReorderCallbackDelegateWithDetails reorder = null,
			ReorderableList.ElementHeightCallbackDelegate elementHeight = null) {
			if(values == null) {
				throw new ArgumentNullException(nameof(values));
			}
			ReorderableList reorderable;
			if(!_reorderabeMap.TryGetValue(values, out reorderable)) {
				reorderable = GetReorderableList(values, headerLabel, drawElement, add, remove, reorder, elementHeight);
			}
			reorderable.DoLayoutList();
		}

		public static ReorderableList GetReorderableList(
			IList values,
			string headerLabel,
			Action<Rect, int, object> drawElement,
			Action<Rect> add,
			Action<int> remove,
			ReorderableList.ReorderCallbackDelegateWithDetails reorder = null,
			ReorderableList.ElementHeightCallbackDelegate elementHeight = null,
			float? headerHeight = null) {
			ReorderableList reorderable;
			if(!_reorderabeMap2.TryGetValue(values, out reorderable)) {
				reorderable = new ReorderableList(values, values.GetType().ElementType());
				reorderable.drawHeaderCallback = (pos) => {
					EditorGUI.LabelField(pos, headerLabel);
					if(Event.current.button == 1 && Event.current.clickCount == 1 && pos.Contains(Event.current.mousePosition)) {
						var elementType = values.GetType().ElementType();
						GenericMenu menu = new GenericMenu();
						menu.AddItem(new GUIContent("Copy"), false, () => {
							CopyPasteValue.CopyElement(values, elementType);
						});
						if(CopyPasteValue.HasElementValue(elementType)) {
							menu.AddItem(new GUIContent("Paste as new"), false, () => {
								//if(unityObject != null)
								//	uNodeEditorUtility.RegisterUndo(unityObject, "Paste");
								var pasteValues = CopyPasteValue.GetElementValue(elementType);
								foreach(var v in pasteValues) {
									values.Add(v);
								}
							});
							menu.AddItem(new GUIContent("Paste as overwrite"), false, () => {
								//if(unityObject != null)
								//	uNodeEditorUtility.RegisterUndo(unityObject, "Paste");
								var pasteValues = CopyPasteValue.GetElementValue(elementType);
								values.Clear();
								foreach(var v in pasteValues) {
									values.Add(v);
								}
							});
						}
						menu.ShowAsContext();
					}
				};
				reorderable.drawElementCallback = (pos, index, isActive, isFocused) => {
					try {
						drawElement(pos, index, values[index]);
					}
					catch(Exception ex) { Debug.LogException(ex); }
				};
				reorderable.drawElementBackgroundCallback = (pos, index, isActive, isFocused) => {
					if(Event.current.type == EventType.Repaint) {
						ReorderableList.defaultBehaviours.elementBackground.Draw(pos, false, isActive, isActive, isFocused);
					}
					if(pos.Contains(Event.current.mousePosition) && Event.current.button == 1 && Event.current.clickCount == 1 && Event.current.mousePosition.x > 20 && Event.current.mousePosition.x < 38) {
						var elementType = values.GetType().ElementType();
						GenericMenu menu = new GenericMenu();
						menu.AddItem(new GUIContent("Copy"), false, () => {
							CopyPasteValue.CopyAsElement(values[index], elementType);
						});
						if(CopyPasteValue.HasElementValue(elementType)) {
							menu.AddItem(new GUIContent("Paste as new"), false, () => {
								//if(unityObject != null)
								//	uNodeEditorUtility.RegisterUndo(unityObject, "Paste");
								var pasteValues = CopyPasteValue.GetElementValue(elementType);
								for(int i = pasteValues.Length - 1; i >= 0; i--) {
									values.Insert(index, pasteValues[i]);
								}
							});
						}
						menu.ShowAsContext();
					}
				};
				if(elementHeight != null) {
					reorderable.elementHeightCallback = elementHeight;
				}
				if(headerHeight != null) {
					reorderable.headerHeight = headerHeight.Value;
				}
				reorderable.displayAdd = add != null;
				reorderable.displayRemove = remove != null;
				if(reorderable.displayAdd) {
					reorderable.onAddDropdownCallback = (pos, list) => {
						add(pos);
						reorderable.list = values as System.Collections.IList;
					};
				}
				if(reorderable.displayRemove) {
					reorderable.onRemoveCallback = (list) => {
						remove(reorderable.index);
						reorderable.list = values as System.Collections.IList;
					};
				}
				reorderable.onReorderCallbackWithDetails = reorder;
				_reorderabeMap2.AddOrUpdate(values, reorderable);
			}
			return reorderable;
		}

		public static void DrawCustomList(
			IList values,
			string headerLabel,
			Action<Rect, int, object> drawElement,
			Action<Rect> add,
			Action<int> remove,
			ReorderableList.ReorderCallbackDelegateWithDetails reorder = null,
			ReorderableList.ElementHeightCallbackDelegate elementHeight = null,
			float? headerHeight = null) {
			if(values == null) {
				throw new ArgumentNullException(nameof(values));
			}
			ReorderableList reorderable;
			if(!_reorderabeMap2.TryGetValue(values, out reorderable)) {
				reorderable = GetReorderableList(values, headerLabel, drawElement, add, remove, reorder, elementHeight, headerHeight);
			}
			reorderable.DoLayoutList();
		}

		public static void DrawList(
			UBind property,
			string headerLabel,
			Action<Rect, int> drawElement,
			Action<Rect> add,
			Action<int> remove,
			ReorderableList.ReorderCallbackDelegateWithDetails reorder = null,
			ReorderableList.ElementHeightCallbackDelegate elementHeight = null) {
			if(property == null) {
				throw new ArgumentNullException(nameof(property));
			}
			ReorderableList reorderable;
			if(!_reorderabeMap.TryGetValue(property, out reorderable)) {
				reorderable = new ReorderableList(property.value as IList, property.type.ElementType());
				reorderable.drawHeaderCallback = (pos) => {
					EditorGUI.LabelField(pos, headerLabel);
					if(Event.current.button == 1 && Event.current.clickCount == 1 && pos.Contains(Event.current.mousePosition)) {
						var values = property.value as IList;
						var elementType = values.GetType().ElementType();
						GenericMenu menu = new GenericMenu();
						menu.AddItem(new GUIContent("Copy"), false, () => {
							CopyPasteValue.CopyElement(values, elementType);
						});
						if(CopyPasteValue.HasElementValue(elementType)) {
							menu.AddItem(new GUIContent("Paste as new"), false, () => {
								property.RegisterUndo("Paste");
								var pasteValues = CopyPasteValue.GetElementValue(elementType);
								foreach(var v in pasteValues) {
									values.Add(v);
								}
							});
							menu.AddItem(new GUIContent("Paste as overwrite"), false, () => {
								property.RegisterUndo("Paste");
								var pasteValues = CopyPasteValue.GetElementValue(elementType);
								values.Clear();
								foreach(var v in pasteValues) {
									values.Add(v);
								}
							});
						}
						menu.ShowAsContext();
					}
				};
				reorderable.drawElementCallback = (pos, index, isActive, isFocused) => {
					try {
						drawElement(pos, index);
					}
					catch(Exception ex) { Debug.LogException(ex); }
				};
				reorderable.drawElementBackgroundCallback = (pos, index, isActive, isFocused) => {
					if(Event.current.type == EventType.Repaint) {
						ReorderableList.defaultBehaviours.elementBackground.Draw(pos, false, isActive, isActive, isFocused);
					}
					if(pos.Contains(Event.current.mousePosition) && Event.current.button == 1 && Event.current.clickCount == 1 && Event.current.mousePosition.x > 20 && Event.current.mousePosition.x < 38) {
						var values = property.value as IList;
						var elementType = values.GetType().ElementType();
						GenericMenu menu = new GenericMenu();
						menu.AddItem(new GUIContent("Copy"), false, () => {
							CopyPasteValue.CopyAsElement(values[index], elementType);
						});
						if(CopyPasteValue.HasElementValue(elementType)) {
							menu.AddItem(new GUIContent("Paste as new"), false, () => {
								property.RegisterUndo("Paste");
								var pasteValues = CopyPasteValue.GetElementValue(elementType);
								for(int i = pasteValues.Length - 1; i >= 0; i--) {
									values.Insert(index, pasteValues[i]);
								}
							});
						}
						menu.ShowAsContext();
					}
				};
				if(elementHeight != null) {
					reorderable.elementHeightCallback = elementHeight;
				}
				reorderable.displayAdd = add != null;
				reorderable.displayRemove = remove != null;
				if(reorderable.displayAdd) {
					reorderable.onAddDropdownCallback = (pos, list) => {
						add(pos);
						reorderable.list = property.value as IList;
					};
				}
				if(reorderable.displayRemove) {
					reorderable.onRemoveCallback = (list) => {
						remove(reorderable.index);
						reorderable.list = property.value as IList;
					};
				}
				reorderable.onReorderCallbackWithDetails = reorder;
				_reorderabeMap.AddOrUpdate(property, reorderable);
			}
			reorderable.DoLayoutList();
		}

		public static void DrawTypeList(string header, List<SerializedType> types, FilterAttribute typeFilter = null, UnityEngine.Object unityObject = null) {
			if(typeFilter == null)
				typeFilter = FilterAttribute.DefaultTypeFilter;
			DrawCustomList(types, header,
				drawElement: (position, index, value) => {
					uNodeGUIUtility.DrawTypeDrawer(
						position,
						value,
						new GUIContent("Element " + index),
						(type) => {
							types[index] = type;
						},
						typeFilter,
						unityObject
					);
				},
				add: pos => {
					var selector = ItemSelector.ShowType(unityObject, typeFilter, member => {
						uNodeEditorUtility.RegisterUndo(unityObject);
						types.Add(member.startType);
					});
					Rect r = pos.ToScreenRect();
					Vector2 position = new Vector2(r.position.x - selector.position.width, r.position.y);
					r.position = position;
					selector.ChangePosition(r);
				},
				remove: (index) => {
					uNodeEditorUtility.RegisterUndo(unityObject);
					types.RemoveAt(index);
				});
		}

		public static void DrawAttribute(List<AttributeData> values,
			UnityEngine.Object unityObject,
			Action<List<AttributeData>> action,
			AttributeTargets attributeTargets = AttributeTargets.All,
			string header = "Attributes"
		) {
			if(values == null) {
				values = new();
				if(action != null) {
					action(values);
				}
			}
			DrawCustomList<AttributeData>(values, header,
				drawElement: (pos, index, value) => {
					if(pos.Contains(Event.current.mousePosition) && Event.current.button == 0 && Event.current.clickCount == 2) {
						FieldsEditorWindow.ShowWindow(values[index], unityObject, delegate (object obj) {
							return values[(int)obj];
						}, index);
					}
					var attName = values[index].attributeType != null ? values[index].attributeType.prettyName : "null";
					if(attName.EndsWith("Attribute")) {
						attName = attName.RemoveLast("Attribute".Length);
					}
					var ctor = values[index].constructor;
					if(ctor?.parameters?.Length > 0) {
						var parameters = ctor.parameters;
						string pInfo = null;
						if(parameters != null && parameters.Length > 0) {
							for(int i = 0; i < parameters.Length; i++) {
								if(i != 0) {
									pInfo += ", ";
								}
								pInfo += parameters[i] != null ? parameters[i].value : "null";
							}
						}
						attName += $"({pInfo})";
					}
					EditorGUI.LabelField(pos, new GUIContent(attName));
				},
				add: (pos) => {
					var selector = ItemSelector.ShowWindow(unityObject, new FilterAttribute(typeof(Attribute)) {
						DisplayAbstractType = false,
						DisplayInterfaceType = false,
						OnlyGetType = true,
						ArrayManipulator = false,
						UnityReference = false,
						attributeTargets = attributeTargets
					}, m => {
						var type = m.startType;
						var att = new AttributeData() { attributeType = type };
						if(type != null && !(type.IsAbstract || type.IsInterface)) {
							var ctor = type.GetConstructors().FirstOrDefault();
							if(ctor != null) {
								att.constructor = new ConstructorValueData(ctor);
							}
							else {
								att.constructor = null;
							}
						}
						values.Add(att);
						if(action != null) {
							action(values);
						}
					});
					Rect r = pos.ToScreenRect();
					Vector2 position = new Vector2(r.position.x - selector.position.width, r.position.y);
					r.position = position;
					selector.ChangePosition(r);
				},
				remove: index => {
					if(unityObject)
						uNodeEditorUtility.RegisterUndo(unityObject, "Remove Attribute");
					values.RemoveAt(index);
					if(action != null) {
						action(values);
					}
				},
				reorder: (list, oldIndex, newIndex) => {
					var val = values[newIndex];
					values.RemoveAt(newIndex);
					if(oldIndex >= values.Count) {
						values.Add(val);
					}
					else {
						values.Insert(oldIndex, val);
					}
					if(action != null) {
						action(values);
					}
					if(unityObject)
						uNodeEditorUtility.RegisterUndo(unityObject, "Reorder List");
					val = values[oldIndex];
					values.RemoveAt(oldIndex);
					if(newIndex >= values.Count) {
						values.Add(val);
					}
					else {
						values.Insert(newIndex, val);
					}
					if(action != null) {
						action(values);
					}
				});
		}

		public static void DrawNamespace(string header, IList<string> namespaces, UnityEngine.Object unityObject, Action<IList<string>> action = null) {
			if(namespaces == null) {
				namespaces = new List<string>();
				if(action != null) {
					action(namespaces);
				}
			}
			DrawCustomList(namespaces as IList, header,
				drawElement: (pos, index, value) => {
					namespaces[index] = EditorGUI.TextField(pos, namespaces[index]);
					if(GUI.changed) {
						if(unityObject) {
							uNodeEditorUtility.MarkDirty(unityObject);
						}
					}
				},
				add: pos => {
					List<ItemSelector.CustomItem> items = new List<ItemSelector.CustomItem>();
					var ns = EditorReflectionUtility.GetNamespaces();
					if(ns != null && ns.Count > 0) {
						foreach(var n in ns) {
							if(!string.IsNullOrEmpty(n) && !namespaces.Contains(n)) {
								items.Add(ItemSelector.CustomItem.Create(n, (obj) => {
									uNodeUtility.AddList(ref namespaces, n);
									if(action != null) {
										action(namespaces);
									}
									if(unityObject && uNodeEditorUtility.IsPrefab(unityObject)) {
										uNodeEditorUtility.MarkDirty(unityObject);
									}
								}, "Namespaces"));
							}
						}
						items.Sort((x, y) => string.CompareOrdinal(x.name, y.name));
					}
					var selector = ItemSelector.ShowWindow(null, null, null, items);
					Rect r = pos.ToScreenRect();
					Vector2 position = new Vector2(r.position.x - selector.position.width, r.position.y);
					r.position = position;
					selector.ChangePosition(r);
					selector.displayDefaultItem = false;
				},
				remove: index => {
					if(unityObject)
						uNodeEditorUtility.RegisterUndo(unityObject, "Remove Namespace: " + namespaces[index]);
					uNodeUtility.RemoveListAt(ref namespaces, index);
					if(action != null) {
						action(namespaces);
					}
					if(unityObject && uNodeEditorUtility.IsPrefab(unityObject)) {
						uNodeEditorUtility.MarkDirty(unityObject);
					}
				});
		}

		public static void DrawInterfaces(IInterfaceSystem system, string headerLabel = "Interfaces", Action onChanged = null) {
			ReorderableList reorderable;
			if(!_reorderabeMap.TryGetValue(system, out reorderable)) {
				var interfaces = system.Interfaces;
				reorderable = new ReorderableList(interfaces, typeof(SerializedType));
				reorderable.drawHeaderCallback = (pos) => {
					EditorGUI.LabelField(pos, headerLabel);
				};
				reorderable.drawElementCallback = (pos, index, isActive, isFocused) => {
					EditorGUI.LabelField(pos, new GUIContent(interfaces[index].prettyName, uNodeEditorUtility.GetTypeIcon(interfaces[index])));
				};
				reorderable.onReorderCallbackWithDetails = (list, oldIndex, newIndex) => {
					var val = interfaces[newIndex];
					interfaces.RemoveAt(newIndex);
					if(oldIndex >= interfaces.Count) {
						interfaces.Add(val);
					}
					else {
						interfaces.Insert(oldIndex, val);
					}
					if(system as UnityEngine.Object)
						uNodeEditorUtility.RegisterUndo(system as UnityEngine.Object, "Reorder List");
					val = interfaces[oldIndex];
					interfaces.RemoveAt(oldIndex);
					if(newIndex >= interfaces.Count) {
						interfaces.Add(val);
					}
					else {
						interfaces.Insert(newIndex, val);
					}
					reorderable.list = interfaces;
					onChanged?.Invoke();
				};
				reorderable.onAddDropdownCallback = (pos, list) => {
					bool isNativeType = system is IScriptGraphType;
					var filter = new FilterAttribute() {
						OnlyGetType = true,
						ArrayManipulator = false,
						//UnityReference = false,
						ValidateType = (type) => {
							if(isNativeType != ReflectionUtils.IsNativeType(type)) {
								return false;
							}
							return type.IsInterface;
						}
					};
					var selector = ItemSelector.ShowWindow(system as Object, filter, (member) => {
						if(system as UnityEngine.Object)
							uNodeEditorUtility.RegisterUndo(system as UnityEngine.Object, "Add Interface");
						interfaces.Add(member.startType);
						onChanged?.Invoke();
						if(system as UnityEngine.Object && uNodeEditorUtility.IsPrefab(system as UnityEngine.Object)) {
							uNodeEditorUtility.MarkDirty(system as UnityEngine.Object);
						}
					});
					Rect r = pos.ToScreenRect();
					Vector2 position = new Vector2(r.position.x - selector.position.width, r.position.y);
					r.position = position;
					selector.ChangePosition(r);
				};
				reorderable.onRemoveCallback = (list) => {
					if(system as UnityEngine.Object)
						uNodeEditorUtility.RegisterUndo(system as UnityEngine.Object, "Remove Interface: " + interfaces[list.index].prettyName);
					interfaces.RemoveAt(list.index);
					onChanged?.Invoke();
					if(system as UnityEngine.Object && uNodeEditorUtility.IsPrefab(system as UnityEngine.Object)) {
						uNodeEditorUtility.MarkDirty(system as UnityEngine.Object);
					}
				};
				_reorderabeMap.AddOrUpdate(system, reorderable);
			}
			reorderable.DoLayoutList();
		}
		#endregion

		#region Draw Fields
		public static void DrawBoolField(Rect position, ref bool variable, Object unityObject, GUIContent label) {
			EditorGUI.BeginChangeCheck();
			var newVar = EditorGUI.Toggle(position, label, variable);
			if(EditorGUI.EndChangeCheck()) {
				if(newVar != variable) {
					uNodeEditorUtility.RegisterUndo(unityObject, label.text);
					variable = newVar;
				}
			}
		}

		public static void DrawStringField(Rect position, ref string variable, Object unityObject, GUIContent label) {
			EditorGUI.BeginChangeCheck();
			var newVar = EditorGUI.TextField(position, label, variable);
			if(EditorGUI.EndChangeCheck()) {
				if(newVar != variable) {
					uNodeEditorUtility.RegisterUndo(unityObject, label.text);
					variable = newVar;
				}
			}
		}

		public static void DrawIntField(Rect position, ref int variable, Object unityObject, GUIContent label) {
			EditorGUI.BeginChangeCheck();
			var newVar = EditorGUI.DelayedIntField(position, label, variable);
			if(EditorGUI.EndChangeCheck()) {
				if(newVar != variable) {
					uNodeEditorUtility.RegisterUndo(unityObject, label.text);
					variable = newVar;
				}
			}
		}

		public static void DrawFloatField(Rect position, ref float variable, Object unityObject, GUIContent label) {
			EditorGUI.BeginChangeCheck();
			var newVar = EditorGUI.DelayedFloatField(position, label, variable);
			if(EditorGUI.EndChangeCheck()) {
				if(newVar != variable) {
					uNodeEditorUtility.RegisterUndo(unityObject, label.text);
					variable = newVar;
				}
			}
		}

		public static void DrawVector2Field(Rect position, ref Vector2 variable, Object unityObject, GUIContent label) {
			EditorGUI.BeginChangeCheck();
			var newVar = EditorGUI.Vector2Field(position, label, variable);
			if(EditorGUI.EndChangeCheck()) {
				if(newVar != variable) {
					uNodeEditorUtility.RegisterUndo(unityObject, label.text);
					variable = newVar;
				}
			}
		}

		public static void DrawVector2Field(ref Vector2 variable, Object unityObject, GUIContent label) {
			EditorGUI.BeginChangeCheck();
			var newVar = EditorGUILayout.Vector2Field(label, variable);
			if(EditorGUI.EndChangeCheck()) {
				if(newVar != variable) {
					uNodeEditorUtility.RegisterUndo(unityObject, label.text);
					variable = newVar;
				}
			}
		}

		public static void DrawVector3Field(Rect position, ref Vector3 variable, Object unityObject, GUIContent label) {
			EditorGUI.BeginChangeCheck();
			var newVar = EditorGUI.Vector3Field(position, label, variable);
			if(EditorGUI.EndChangeCheck()) {
				if(newVar != variable) {
					uNodeEditorUtility.RegisterUndo(unityObject, label.text);
					variable = newVar;
				}
			}
		}

		public static void DrawVector3Field(ref Vector3 variable, Object unityObject, GUIContent label) {
			EditorGUI.BeginChangeCheck();
			var newVar = EditorGUILayout.Vector3Field(label, variable);
			if(EditorGUI.EndChangeCheck()) {
				if(newVar != variable) {
					uNodeEditorUtility.RegisterUndo(unityObject, label.text);
					variable = newVar;
				}
			}
		}

		public static void DrawColorField(Rect position, ref Color variable, Object unityObject, GUIContent label) {
			EditorGUI.BeginChangeCheck();
			var newVar = EditorGUI.ColorField(position, label, variable);
			if(EditorGUI.EndChangeCheck()) {
				if(newVar != variable) {
					uNodeEditorUtility.RegisterUndo(unityObject, label.text);
					variable = newVar;
				}
			}
		}

		public static void DrawColorField(ref Color variable, Object unityObject, GUIContent label) {
			EditorGUI.BeginChangeCheck();
			var newVar = EditorGUILayout.ColorField(label, variable);
			if(EditorGUI.EndChangeCheck()) {
				if(newVar != variable) {
					uNodeEditorUtility.RegisterUndo(unityObject, label.text);
					variable = newVar;
				}
			}
		}

		public static void DrawObjectField(Rect position, ref Object variable, System.Type objectType, Object unityObject, GUIContent label) {
			EditorGUI.BeginChangeCheck();
			var newVar = EditorGUI.ObjectField(position, label, variable, objectType, uNodeEditorUtility.IsSceneObject(unityObject));
			if(EditorGUI.EndChangeCheck()) {
				if(newVar != variable) {
					uNodeEditorUtility.RegisterUndo(unityObject, label.text);
					variable = newVar;
				}
			}
		}

		public static void DrawReference(Rect position, object reference, Type objectType) {
			if(reference is Node) {
				DrawReference(position, (reference as Node).nodeObject, typeof(NodeObject));
			}
			else if(reference is UGraphElement) {
				var element = reference as UGraphElement;
				var text = element.name;
				if(element is IPrettyName) {
					text = (element as IPrettyName).GetPrettyName();
				}
				var icon = objectType;
				if(element is IIcon) {
					icon = (element as IIcon).GetIcon() ?? objectType;
				}
				if(Event.current.clickCount == 1 && Event.current.button == 0 && position.Contains(Event.current.mousePosition)) {
					if(element is NodeObject) {
						uNodeEditor.HighlightNode(element as NodeObject);
					}
					else {
						uNodeEditor.Open(element.graphContainer, element);
					}
				}
				EditorGUI.DropdownButton(position, new GUIContent(text, uNodeEditorUtility.GetTypeIcon(icon)), FocusType.Keyboard, EditorStyles.objectField);
			}
			else if(reference is Object) {
				EditorGUI.BeginDisabledGroup(true);
				EditorGUI.ObjectField(position, GUIContent.none, reference as Object, objectType, true);
				EditorGUI.EndDisabledGroup();
			}
			else if(reference == null) {
				EditorGUI.DropdownButton(position, new GUIContent("null", uNodeEditorUtility.GetTypeIcon(objectType)), FocusType.Keyboard, EditorStyles.objectField);
			}
			else {
				throw new InvalidOperationException();
			}
		}

		public static System.Enum DrawEnumField(Rect position, ref string variable, System.Type enumType, Object unityObject, GUIContent label) {
			EditorGUI.BeginChangeCheck();
			System.Enum newVar = null;
			{
				bool createNew = true;
				if(!string.IsNullOrEmpty(variable)) {
					string[] EnumNames = System.Enum.GetNames(enumType);
					foreach(string str in EnumNames) {
						if(str == variable) {
							newVar = (System.Enum)System.Enum.Parse(enumType, variable);
							createNew = false;
							break;
						}
					}
				}
				if(createNew) {
					newVar = (System.Enum)System.Activator.CreateInstance(enumType);
				}
			}
			newVar = EditorGUI.EnumPopup(position, label, newVar);
			if(EditorGUI.EndChangeCheck()) {
				if(newVar.ToString() != variable) {
					uNodeEditorUtility.RegisterUndo(unityObject, label.text);
					variable = newVar.ToString();
				}
			}
			return newVar;
		}
		#endregion

		#region Private
		private static readonly GUIContent s_TempContent = new GUIContent();

		private static GUIContent TempContent(string text) {
			s_TempContent.text = text;
			s_TempContent.image = null;
			s_TempContent.tooltip = null;
			return s_TempContent;
		}
		#endregion

		#region Original
		public static void Label(Rect position, GUIContent label, GUIStyle style = null) {
			if(style == null) {
				style = "Label";
			}
			EditorGUI.LabelField(position, label, style);
		}

		public static bool Button(Rect position, GUIContent label, GUIStyle style = null) {
			if(style == null) {
				style = GUI.skin.button;
			}
			return EditorGUI.DropdownButton(position, label, FocusType.Keyboard, style);
		}
		#endregion

		#region Helper
		public static void Label(Rect position, string label, GUIStyle style = null) {
			Label(position, TempContent(label), style);
		}

		public static bool Button(Rect position, string label, GUIStyle style = null) {
			return Button(position, TempContent(label), style);
		}

		public static void DrawHeader(string label, float space = 6) {
			EditorGUILayout.Space(space);
			EditorGUI.LabelField(uNodeGUIUtility.GetRect(), label, EditorStyles.boldLabel);
		}
		#endregion

		#region Layout Version
		public static string TextInput(string text, string placeholder, bool area = false, bool delayedField = true, params GUILayoutOption[] options) {
			var newText = area ? EditorGUILayout.TextArea(text, options) : delayedField ? EditorGUILayout.DelayedTextField(text, options) : EditorGUILayout.TextField(text, options);
			if(string.IsNullOrEmpty(text)) {
				const int textMargin = 2;
				var guiColor = GUI.color;
				GUI.color = Color.grey;
				var textRect = GUILayoutUtility.GetLastRect();
				var position = new Rect(textRect.x + textMargin, textRect.y, textRect.width, textRect.height);
				EditorGUI.LabelField(position, placeholder);
				GUI.color = guiColor;
			}
			return newText;
		}

		public static bool Button(GUIContent label, GUIStyle style, params GUILayoutOption[] options) {
			if(style == null) {
				style = GUI.skin.button;
			}
			Rect position = GUILayoutUtility.GetRect(label, style, options);
			return Button(position, label, style);
		}

		public static bool Button(string label, GUIStyle style, params GUILayoutOption[] options) {
			return Button(TempContent(label), style, options);
		}

		public static bool Button(string label, params GUILayoutOption[] options) {
			return Button(label, null, options);
		}
		#endregion

		#region Variables
		public static void DrawGraphVariables(Graph graph, UnityEngine.Object unityObject, bool publicOnly = true) {
			if(graph == null)
				return;
			var variables = graph.variableContainer.collections;
			if(variables.Count == 0)
				return;
			for(int x = 0; x < variables.Count; x++) {
				var variable = variables[x];
				if(publicOnly && !variable.showInInspector) {
					continue;
				}
				if(variable != null) {
					FieldDecorator.DrawDecorators(variable.GetAttributes());
				}
				string varName = ObjectNames.NicifyVariableName(variable.name);
				System.Type type = variable.type;
				uNodeGUIUtility.EditSerializedValue(variable.serializedValue, new GUIContent(varName), variable.type, unityObject);
			}
		}

		public static void DrawRuntimeGraphVariables(GraphInstance instance, bool publicOnly = true) {
			if(instance == null || instance.graph == null)
				return;
			var graph = instance.graph;
			if(graph is IReflectionType reflectionType) {
				var type = reflectionType.ReflectionType;
				var variables = type.GetRuntimeFields().Where(f => f is RuntimeField<VariableRef>).Select(f => (f as RuntimeField<VariableRef>).target.reference);
				DrawRuntimeGraphVariables(instance, variables, publicOnly);
			}
			else {
				DrawRuntimeGraphVariables(instance, graph.GetAllVariables(), publicOnly);
			}
		}

		private static void DrawRuntimeGraphVariables(GraphInstance instance, IEnumerable<Variable> variables, bool publicOnly = true) {
			if(variables == null) return;
			foreach(var variable in variables) {
				if(publicOnly && !variable.showInInspector) {
					continue;
				}
				if(variable != null) {
					FieldDecorator.DrawDecorators(variable.GetAttributes());
				}
				string varName = ObjectNames.NicifyVariableName(variable.name);
				System.Type type = variable.type;
				uNodeGUIUtility.EditValueLayouted(new GUIContent(varName), variable.Get(instance), type, (val) => {
					variable.Set(instance, val);
				});
			}
		}

		public static void DrawRuntimeGraphVariables(IRuntimeGraphWrapper graphWrapper, bool publicOnly = true, Object unityObject = null) {

			IEnumerable<Variable> GetVariables() {
				var originalGraph = graphWrapper.OriginalGraph;
				if(originalGraph != null) {
					foreach(var v in originalGraph.GetAllVariables()) {
						if(publicOnly) {
							if(v.modifier.isPublic) {
								yield return v;
							}
						}
						else {
							yield return v;
						}
					}
				}
			}

			if(Application.isPlaying && graphWrapper.RuntimeClass != null) {
				var instance = graphWrapper.RuntimeClass;
				if(instance is IInstancedGraph) {
					var graphInstance = (instance as IInstancedGraph).Instance;
					if(graphInstance != null) {
						foreach(var v in GetVariables()) {
							uNodeGUIUtility.EditValueLayouted(new GUIContent(v.name), v.Get(graphInstance), v.type, val => {
								v.Set(graphInstance, val);
							});
						}
					}
					else {
						DrawLinkedVariables(graphWrapper.WrappedVariables, graphWrapper.OriginalGraph, unityObject: unityObject);
					}
				}
				else {
					foreach(var v in GetVariables()) {
						uNodeGUIUtility.EditValueLayouted(new GUIContent(v.name), instance.GetVariable(v.name), v.type, val => {
							instance.SetVariable(v.name, val);
						});
					}
				}
			}
			else {
				DrawLinkedVariables(graphWrapper.WrappedVariables, graphWrapper.OriginalGraph, unityObject: unityObject);
			}
		}

		public static void DrawLinkedVariables(IList<VariableData> instanceVariables, IGraph linked, bool publicOnly = true, Object unityObject = null) {
			DoDrawLinkedVariables(instanceVariables, linked, publicOnly, unityObject);
		}

		private static void DoDrawLinkedVariables(IList<VariableData> instanceVariables, IEnumerable<Variable> linkedVariable, bool publicOnly = true, Object unityObject = null) {
			for(int i = 0; i < instanceVariables.Count; i++) {
				if(!linkedVariable.Any((v) => v?.name == instanceVariables[i].name)) {
					//Remove the variable when the variable doesn't exist in the linked variable.
					instanceVariables.RemoveAt(i);
				}
			}
			foreach(var linkedVar in linkedVariable) {
				if(publicOnly && !linkedVar.showInInspector) {
					continue;
				}
				VariableData ownerVar = null;
				for(int y = 0; y < instanceVariables.Count; y++) {
					if(linkedVar.name == instanceVariables[y].name) {
						ownerVar = instanceVariables[y];
						break;
					}
				}
				if(ownerVar != null) {
					ownerVar.attributes = linkedVar.attributes;
					if(ownerVar.type != linkedVar.type) {
						ownerVar.type = linkedVar.type;
					}
				}
				VariableData variable = ownerVar;
				if(variable == null) {
					variable = new VariableData() {
						name = linkedVar.name,
						attributes = linkedVar.attributes,
						serializedValue = new SerializedValue(SerializerUtility.Duplicate(linkedVar.defaultValue), linkedVar.type),
					};
				}
				if(variable != null) {
					FieldDecorator.DrawDecorators(linkedVar.GetAttributes());
				}
				using(new EditorGUILayout.HorizontalScope()) {
					var indent = EditorGUI.indentLevel;
					EditorGUI.indentLevel = 0;
					bool flag = EditorGUILayout.Toggle(ownerVar != null, GUILayout.Width(EditorGUIUtility.singleLineHeight));
					EditorGUI.indentLevel = indent;
					if(flag != (ownerVar != null)) {
						uNodeEditorUtility.RegisterUndo(unityObject);
						if(flag) {
							instanceVariables.Add(new VariableData() {
								name = linkedVar.name,
								serializedValue = new SerializedValue(SerializerUtility.Duplicate(linkedVar.defaultValue), linkedVar.type),
							});
						}
						else {
							instanceVariables.Remove(ownerVar);
						}
					}
					else if(ownerVar != null && ownerVar.type != null && !ownerVar.type.IsCastableTo(linkedVar.type)) {
						instanceVariables.Remove(ownerVar);
						instanceVariables.Add(new VariableData() {
							name = linkedVar.name,
							serializedValue = new SerializedValue(SerializerUtility.Duplicate(linkedVar.defaultValue), linkedVar.type),
						});
					}
					EditorGUI.BeginDisabledGroup(ownerVar == null);
					using(new EditorGUILayout.VerticalScope()) {
						uNodeGUIUtility.EditVariableValue(variable, unityObject, false);
					}
					EditorGUI.EndDisabledGroup();
				}
			}
		}

		private static void DoDrawLinkedVariables(IList<VariableData> instanceVariables, IGraph linked, bool publicOnly = true, Object unityObject = null) {
			if(instanceVariables == null || linked == null)
				return;
			//if(linked is IReflectionType reflectionType) {
			//	var type = reflectionType.ReflectionType;
			//	var linkedVariable = type.GetRuntimeFields().Where(f => f is RuntimeField<VariableRef>).Select(f => (f as RuntimeField<VariableRef>).target.reference);
			//	DoDrawLinkedVariables(instanceVariables, linkedVariable, publicOnly, unityObject);
			//}
			//else {
			DoDrawLinkedVariables(instanceVariables, linked.GetAllVariables(), publicOnly, unityObject);
			//}
		}
		#endregion

		#region Others
		public static void DrawClassDefinitionModel(ClassDefinitionModel model, Action<ClassDefinitionModel> onChange) {
			using(new GUILayout.HorizontalScope()) {
				EditorGUILayout.PrefixLabel("Model");
				if(uNodeGUI.Button(model.title)) {
					var models = EditorReflectionUtility.GetSubClassesOfType<ClassDefinitionModel>().ToList();
					models.Sort((x, y) => string.Compare(x.Name, y.Name));
					GenericMenu menu = new GenericMenu();
					foreach(var m in models) {
						var modelType = m;
						menu.AddItem(new GUIContent(m.Name), model.GetType() == modelType, () => {
							model = ReflectionUtils.CreateInstance(modelType) as ClassDefinitionModel;
							onChange(model);
						});
					}
					menu.ShowAsContext();
				}
			}
			using(new GUILayout.VerticalScope()) {
				EditorGUI.indentLevel++;
				uNodeGUIUtility.EditValueLayouted(GUIContent.none, model, model.GetType(), val => onChange(val as ClassDefinitionModel));
				EditorGUI.indentLevel--;

			}
		}
		#endregion
	}
}