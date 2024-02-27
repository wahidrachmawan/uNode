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
		static ConditionalWeakTable<object, ReorderableList> _reorderabeMemberMap = new ConditionalWeakTable<object, ReorderableList>();

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
				reorderable = new ReorderableList(values as System.Collections.IList, typeof(T));
				reorderable.drawHeaderCallback = (pos) => {
					EditorGUI.LabelField(pos, headerLabel);
				};
				reorderable.drawElementCallback = (pos, index, isActive, isFocused) => {
					drawElement(pos, index, values[index]);
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
			reorderable.DoLayoutList();
		}

		public static void DrawCustomList(
			IList values,
			string headerLabel,
			Action<Rect, int, object> drawElement,
			Action<Rect> add,
			Action<int> remove,
			ReorderableList.ReorderCallbackDelegateWithDetails reorder = null,
			ReorderableList.ElementHeightCallbackDelegate elementHeight = null) {
			if(values == null) {
				throw new ArgumentNullException(nameof(values));
			}
			ReorderableList reorderable;
			if(!_reorderabeMap2.TryGetValue(values, out reorderable)) {
				reorderable = new ReorderableList(values, values.GetType().ElementType());
				reorderable.drawHeaderCallback = (pos) => {
					EditorGUI.LabelField(pos, headerLabel);
				};
				reorderable.drawElementCallback = (pos, index, isActive, isFocused) => {
					drawElement(pos, index, values[index]);
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
				_reorderabeMap2.AddOrUpdate(values, reorderable);
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
				};
				reorderable.drawElementCallback = (pos, index, isActive, isFocused) => {
					drawElement(pos, index);
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
				add: position => {
					ItemSelector.ShowType(unityObject, typeFilter, member => {
						uNodeEditorUtility.RegisterUndo(unityObject);
						types.Add(member.startType);
					}).ChangePosition(GUIUtility.GUIToScreenRect(position));
				},
				remove: (index) => {
					uNodeEditorUtility.RegisterUndo(unityObject);
					types.RemoveAt(index);
				});
		}

		public static void DrawAttribute(List<AttributeData> attributes,
			UnityEngine.Object targetObject,
			Action<List<AttributeData>> action,
			AttributeTargets attributeTargets = AttributeTargets.All,
			string header = "Attributes"
		) {
			if(attributes == null) {
				attributes = new();
				if(action != null) {
					action(attributes);
				}
			}
			ReorderableList reorderable;
			if(!_reorderabeMap.TryGetValue(attributes, out reorderable)) {
				reorderable = new ReorderableList(attributes as System.Collections.IList, typeof(AttributeData));
				_reorderabeMap.AddOrUpdate(attributes, reorderable);
				reorderable.drawHeaderCallback = (pos) => {
					EditorGUI.LabelField(pos, header);
				};
				reorderable.drawElementCallback = (pos, index, isActive, isFocused) => {
					if(pos.Contains(Event.current.mousePosition) && Event.current.button == 0 && Event.current.clickCount == 2) {
						FieldsEditorWindow.ShowWindow(attributes[index], targetObject, delegate (object obj) {
							return attributes[(int)obj];
						}, index);
					}
					var attName = attributes[index].attributeType != null ? attributes[index].attributeType.prettyName : "null";
					if(attName.EndsWith("Attribute")) {
						attName = attName.RemoveLast("Attribute".Length);
					}
					var ctor = attributes[index].constructor;
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
				};
				reorderable.onReorderCallbackWithDetails = (list, oldIndex, newIndex) => {
					var val = attributes[newIndex];
					attributes.RemoveAt(newIndex);
					if(oldIndex >= attributes.Count) {
						attributes.Add(val);
					}
					else {
						attributes.Insert(oldIndex, val);
					}
					if(action != null) {
						action(attributes);
					}
					if(targetObject)
						uNodeEditorUtility.RegisterUndo(targetObject, "Reorder List");
					val = attributes[oldIndex];
					attributes.RemoveAt(oldIndex);
					if(newIndex >= attributes.Count) {
						attributes.Add(val);
					}
					else {
						attributes.Insert(newIndex, val);
					}
					reorderable.list = attributes as System.Collections.IList;
					if(action != null) {
						action(attributes);
					}
				};

				reorderable.onAddDropdownCallback = (pos, list) => {
					ItemSelector.ShowWindow(targetObject, new FilterAttribute(typeof(Attribute)) {
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
						attributes.Add(att);
						if(action != null) {
							action(attributes);
						}
						reorderable.list = attributes as System.Collections.IList;
					}).ChangePosition(pos.ToScreenRect());
				};
				reorderable.onRemoveCallback = (list) => {
					if(targetObject)
						uNodeEditorUtility.RegisterUndo(targetObject, "Remove Attribute: " + attributes[reorderable.index].attributeType);
					attributes.RemoveAt(reorderable.index);
					if(action != null) {
						action(attributes);
					}
					reorderable.list = attributes as System.Collections.IList;
				};
			}
			reorderable.DoLayoutList();
		}

		public static void DrawNamespace(string header, IList<string> namespaces, UnityEngine.Object targetObject, Action<IList<string>> action = null) {
			if(namespaces == null) {
				namespaces = new List<string>();
				if(action != null) {
					action(namespaces);
				}
			}
			ReorderableList reorderable;
			if(!_reorderabeMap.TryGetValue(namespaces, out reorderable)) {
				reorderable = new ReorderableList(namespaces as IList, typeof(string));
				reorderable.drawHeaderCallback = (pos) => {
					EditorGUI.LabelField(pos, header);
				};
				reorderable.drawElementCallback = (pos, index, isActive, isFocused) => {
					namespaces[index] = EditorGUI.TextField(pos, namespaces[index]);
					if(GUI.changed) {
						if(targetObject) {
							uNodeEditorUtility.MarkDirty(targetObject);
						}
					}
				};
				reorderable.onAddDropdownCallback = (pos, list) => {
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
									reorderable.list = namespaces as System.Collections.IList;
									if(targetObject && uNodeEditorUtility.IsPrefab(targetObject)) {
										uNodeEditorUtility.MarkDirty(targetObject);
									}
								}, "Namespaces"));
							}
						}
						items.Sort((x, y) => string.CompareOrdinal(x.name, y.name));
					}
					ItemSelector.ShowWindow(null, null, null, items).ChangePosition(pos.ToScreenRect()).displayDefaultItem = false;
				};
				reorderable.onRemoveCallback = (list) => {
					if(targetObject)
						uNodeEditorUtility.RegisterUndo(targetObject, "Remove Namespace: " + namespaces[list.index]);
					uNodeUtility.RemoveListAt(ref namespaces, list.index);
					if(action != null) {
						action(namespaces);
					}
					if(targetObject && uNodeEditorUtility.IsPrefab(targetObject)) {
						uNodeEditorUtility.MarkDirty(targetObject);
					}
				};
				_reorderabeMap.AddOrUpdate(namespaces, reorderable);
			}
			reorderable.DoLayoutList();
		}

		public static void DrawMembers(GUIContent label,
			List<MemberData> members,
			Object targetObject,
			FilterAttribute filter,
			Action<List<MemberData>> action,
			Action onDropDownClick = null,
			Action<int, int> onReorderCallback = null) {
			if(members == null) {
				members = new List<MemberData>();
				if(action != null) {
					action(members);
				}
			}
			ReorderableList reorderable;
			if(!_reorderabeMemberMap.TryGetValue(members, out reorderable)) {
				reorderable = new ReorderableList(members, typeof(AttributeData));
				reorderable.drawHeaderCallback = (pos) => {
					EditorGUI.LabelField(pos, label);
				};
				if(onReorderCallback != null) {
					reorderable.onReorderCallbackWithDetails = (list, oldIndex, newIndex) => {
						onReorderCallback(oldIndex, newIndex);
					};
				}
				reorderable.drawElementCallback = (pos, index, isActive, isFocused) => {
					pos = EditorGUI.PrefixLabel(pos, new GUIContent("Element " + index));
					pos.height = EditorGUIUtility.singleLineHeight;
					uNodeGUIUtility.DrawMember(pos, members[index], filter, targetObject, (obj) => {
						members[index] = obj;
						if(action != null) {
							action(members);
						}
					});
				};
				reorderable.onAddDropdownCallback = (pos, list) => {
					if(onDropDownClick == null) {
						if(members.Count > 0) {
							members.Add(new MemberData(members[members.Count - 1]));
						}
						else {
							members.Add(MemberData.None);
						}
					}
					else {
						onDropDownClick();
					}
				};
				reorderable.onRemoveCallback = (list) => {
					if(targetObject)
						uNodeEditorUtility.RegisterUndo(targetObject, "Remove Member: " + members[reorderable.index]);
					members.RemoveAt(reorderable.index);
					if(action != null) {
						action(members);
					}
				};
				reorderable.onChangedCallback = (list) => {
					uNodeGUIUtility.GUIChanged(targetObject);
				};
				_reorderabeMemberMap.AddOrUpdate(members, reorderable);
			}
			reorderable.DoLayoutList();
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
					ItemSelector.ShowWindow(system as Object, filter, (member) => {
						if(system as UnityEngine.Object)
							uNodeEditorUtility.RegisterUndo(system as UnityEngine.Object, "Add Interface");
						interfaces.Add(member.startType);
						onChanged?.Invoke();
						if(system as UnityEngine.Object && uNodeEditorUtility.IsPrefab(system as UnityEngine.Object)) {
							uNodeEditorUtility.MarkDirty(system as UnityEngine.Object);
						}
					}).ChangePosition(pos.ToScreenRect());
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
			var newVar = EditorGUI.IntField(position, label, variable);
			if(EditorGUI.EndChangeCheck()) {
				if(newVar != variable) {
					uNodeEditorUtility.RegisterUndo(unityObject, label.text);
					variable = newVar;
				}
			}
		}

		public static void DrawFloatField(Rect position, ref float variable, Object unityObject, GUIContent label) {
			EditorGUI.BeginChangeCheck();
			var newVar = EditorGUI.FloatField(position, label, variable);
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