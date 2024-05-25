using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;
using UnityEditor.Experimental.GraphView;

namespace MaxyGames.UNode.Editors {
	/// <summary>
	/// Provides useful Utility for GUI
	/// </summary>
	public static class uNodeGUIUtility {
		/// <summary>
		/// An event to be called when GUI has been changed.
		/// Note: this will be executed in the next frame after gui changed
		/// </summary>
		public static event Action<object, UIChangeType> onGUIChanged;

		public static void GUIChangedMajor(object owner) {
			uNodeEditor.ClearCache();
			EditorReflectionUtility.UpdateRuntimeTypes();
			//GUIChanged(owner, UIChangeType.Important);
		}

		public static void GUIChanged(object owner, UIChangeType changeType = UIChangeType.Small) {
			if(owner != null) {
				if(owner is Node) {
					owner = (owner as Node).nodeObject;
				}
				uNodeThreadUtility.ExecuteOnce(() => {
					onGUIChanged?.Invoke(owner, changeType);
				}, "UNODE_GUI_CHANGED_CALLBACK:" + owner.GetHashCode() + (int)changeType);
			}
		}

		#region Runtime Types
		public static void EditRuntimeTypeValueLayouted(GUIContent label, object value, RuntimeType type, Action<object> onChange, bool allowSceneObject, UnityEngine.Object unityObject = null, bool acceptUnityObject = true) {
			if(type is INativeMember) {
				var nativeType = (type as INativeMember).GetNativeMember() as Type;
				if(nativeType != null) {
					EditValueLayouted(label, value, nativeType, (val) => {
						onChange(val);
					}, new uNodeUtility.EditValueSettings() {
						acceptUnityObject = acceptUnityObject,
						unityObject = unityObject,
					});
				}
				else {
					EditRuntimeTypeValue(GetRect(), label, value, type, onChange, allowSceneObject, acceptUnityObject);
				}
				return;
			}
			else if(ReflectionUtils.IsNativeType(type)) {
				var nativeType = ReflectionUtils.GetNativeType(type);
				if(nativeType != null) {
					EditValueLayouted(label, value, nativeType, (val) => {
						onChange(val);
					}, new uNodeUtility.EditValueSettings() {
						acceptUnityObject = acceptUnityObject,
						unityObject = unityObject,
					});
				}
				else {
					DrawRuntimeTypeIsNotCompiled(GetRect(), label, type);
				}
				return;
			}
			var position = GetRect();
			if(type is IFakeType) {
				if(type.IsArray) {
					var nativeElementType = ReflectionUtils.GetNativeType(type.GetElementType());
					if(nativeElementType == null) {
						DrawRuntimeTypeIsNotCompiled(position, label, type);
						return;
					}
					Type elementType = type.GetElementType();
					Array array = value as Array;
					if(array == null) {
						array = Array.CreateInstance(nativeElementType, 0);
						GUI.changed = true;
					}
					if(array != null) {
						EditorGUI.BeginChangeCheck();
						int num = EditorGUI.DelayedIntField(position, label, array.Length);
						if(EditorGUI.EndChangeCheck()) {
							if(num != array.Length) {
								array = uNodeUtility.ResizeArray(array, nativeElementType, num);
								GUI.changed = true;
							}
							uNodeEditorUtility.RegisterUndo(unityObject, "");
							value = array;
							if(onChange != null) {
								onChange(value);
							}
							GUIChanged(unityObject);
						}
						if(array.Length > 0) {
							Event currentEvent = Event.current;
							EditorGUI.indentLevel++;
							for(int i = 0; i < array.Length; i++) {
								var elementToEdit = array.GetValue(i);
								int a = i;
								EditValueLayouted(new GUIContent("Element " + i), elementToEdit, elementType, delegate (object val) {
									uNodeEditorUtility.RegisterUndo(unityObject, "");
									elementToEdit = val;
									array.SetValue(elementToEdit, a);
									if(onChange != null)
										onChange(array);
									GUIChanged(unityObject);
								});
							}
							EditorGUI.indentLevel--;
						}
					}
					else {
						DrawNullValue(label, type, delegate (object o) {
							uNodeEditorUtility.RegisterUndo(unityObject, "Create Field Instance");
							if(onChange != null) {
								onChange(o);
							}
							GUIChanged(unityObject);
						});
						EditorGUI.EndChangeCheck();
					}
				}
				else if(type.IsGenericType) {
					if(type.IsCastableTo(typeof(IList))) {
						EditorGUI.BeginChangeCheck();
						Type elementType = type.GetGenericArguments()[0];
						IList array = value as IList;
						if(array == null) {
							array = ReflectionUtils.CreateInstance(type) as IList;
							GUI.changed = true;
						}
						if(array != null) {
							int num = EditorGUI.IntField(position, label, array.Count);
							if(num != array.Count) {
								uNodeUtility.ResizeList(array, elementType, num, true);
							}
							if(array.Count > 0) {
								EditorGUI.indentLevel++;
								for(int i = 0; i < array.Count; i++) {
									var elementToEdit = array[i];
									int a = i;
									EditValueLayouted(new GUIContent("Element " + i), elementToEdit, elementType, delegate (object val) {
										uNodeEditorUtility.RegisterUndo(unityObject, "");
										elementToEdit = val;
										array[a] = elementToEdit;
										if(onChange != null)
											onChange(array);
										GUIChanged(unityObject);
									}, new uNodeUtility.EditValueSettings() { acceptUnityObject = acceptUnityObject });
								}
								EditorGUI.indentLevel--;
							}
						}
						else {
							EditorGUI.PrefixLabel(position, label);
						}
						if(EditorGUI.EndChangeCheck()) {
							uNodeEditorUtility.RegisterUndo(unityObject, "");
							value = array;
							if(onChange != null) {
								onChange(value);
							}
							GUIChanged(unityObject);
						}
					}
					else if(type.IsCastableTo(typeof(IDictionary))) {
						EditorGUI.BeginChangeCheck();
						Type keyType = type.GetGenericArguments()[0];
						Type valType = type.GetGenericArguments()[1];
						IDictionary map = value as IDictionary;
						if(map == null) {
							map = ReflectionUtils.CreateInstance(type) as IDictionary;
							GUI.changed = true;
						}
						if(map != null) {
							if(EditorGUI.DropdownButton(position, new GUIContent("add new (" + keyType.PrettyName() + ", " + valType.PrettyName() + ")"), FocusType.Keyboard) && Event.current.button == 0) {
								GUI.changed = false;
								ActionPopupWindow.ShowWindow(position,
									new object[] { ReflectionUtils.CreateInstance(keyType), ReflectionUtils.CreateInstance(valType), map },
									delegate (ref object val) {
										object[] o = val as object[];
										EditValueLayouted(new GUIContent("Key"), o[0], keyType, delegate (object v) {
											o[0] = v;
										}, new uNodeUtility.EditValueSettings() { nullable = false });
										EditValueLayouted(new GUIContent("Value"), o[1], valType, delegate (object v) {
											o[1] = v;
										});
										if(GUILayout.Button(new GUIContent("Add"))) {
											if(!map.Contains(o[0])) {
												uNodeEditorUtility.RegisterUndo(unityObject, "" + "Add Dictonary Value");
												(o[2] as IDictionary).Add(o[0], o[1]);
												value = o[2];
												if(onChange != null) {
													onChange(value);
												}
												ActionPopupWindow.CloseLast();
												GUIChanged(unityObject);
											}
										}
									}).headerName = "Add New Dictonary Value";
							}
							IDictionary newMap = map;
							if(newMap != null) {
								if(newMap.Count > 0) {
									List<object> keys = uNodeEditorUtility.GetKeys(newMap);
									List<object> values = uNodeEditorUtility.GetValues(newMap);
									if(keys.Count == values.Count) {
										EditorGUI.indentLevel++;
										for(int i = 0; i < keys.Count; i++) {
											Rect rect = GetRect();
											EditorGUI.LabelField(rect, new GUIContent("Element " + i));
											if(Event.current.button == 1 && rect.Contains(Event.current.mousePosition)) {
												GenericMenu menu = new GenericMenu();
												menu.AddItem(new GUIContent("Remove"), false, (obj) => {
													int index = (int)obj;
													newMap.Remove(keys[index]);
												}, i);
												menu.AddSeparator("");
												menu.AddItem(new GUIContent("Move To Top"), false, (obj) => {
													int index = (int)obj;
													if(index != 0) {
														uNodeEditorUtility.RegisterUndo(unityObject, "");
														uNodeEditorUtility.ListMoveToTop(keys, (int)obj);
														uNodeEditorUtility.ListMoveToTop(values, (int)obj);
														newMap = ReflectionUtils.CreateInstance(type) as IDictionary;
														for(int x = 0; x < keys.Count; x++) {
															newMap.Add(keys[x], values[x]);
														}
														value = newMap;
														if(onChange != null) {
															onChange(value);
														}
														GUIChanged(unityObject);
													}
												}, i);
												menu.AddItem(new GUIContent("Move Up"), false, (obj) => {
													int index = (int)obj;
													if(index != 0) {
														uNodeEditorUtility.RegisterUndo(unityObject, "");
														uNodeEditorUtility.ListMoveUp(keys, (int)obj);
														uNodeEditorUtility.ListMoveUp(values, (int)obj);
														newMap = ReflectionUtils.CreateInstance(type) as IDictionary;
														for(int x = 0; x < keys.Count; x++) {
															newMap.Add(keys[x], values[x]);
														}
														value = newMap;
														if(onChange != null) {
															onChange(value);
														}
														GUIChanged(unityObject);
													}
												}, i);
												menu.AddItem(new GUIContent("Move Down"), false, (obj) => {
													int index = (int)obj;
													if(index != keys.Count - 1) {
														uNodeEditorUtility.RegisterUndo(unityObject, "");
														uNodeEditorUtility.ListMoveDown(keys, (int)obj);
														uNodeEditorUtility.ListMoveDown(values, (int)obj);
														newMap = ReflectionUtils.CreateInstance(type) as IDictionary;
														for(int x = 0; x < keys.Count; x++) {
															newMap.Add(keys[x], values[x]);
														}
														value = newMap;
														if(onChange != null) {
															onChange(value);
														}
														GUIChanged(unityObject);
													}
												}, i);
												menu.AddItem(new GUIContent("Move To Bottom"), false, (obj) => {
													int index = (int)obj;
													if(index != keys.Count - 1) {
														uNodeEditorUtility.RegisterUndo(unityObject, "");
														uNodeEditorUtility.ListMoveToBottom(keys, (int)obj);
														uNodeEditorUtility.ListMoveToBottom(values, (int)obj);
														newMap = ReflectionUtils.CreateInstance(type) as IDictionary;
														for(int x = 0; x < keys.Count; x++) {
															newMap.Add(keys[x], values[x]);
														}
														value = newMap;
														if(onChange != null) {
															onChange(value);
														}
														GUIChanged(unityObject);
													}
												}, i);
												menu.ShowAsContext();
											}
											EditorGUI.indentLevel++;
											EditValueLayouted(new GUIContent("Key"), keys[i], keyType, delegate (object val) {
												if(!newMap.Contains(val)) {
													uNodeEditorUtility.RegisterUndo(unityObject, "");
													keys[i] = val;
													newMap = ReflectionUtils.CreateInstance(type) as IDictionary;
													for(int x = 0; x < keys.Count; x++) {
														newMap.Add(keys[x], values[x]);
													}
													value = newMap;
													if(onChange != null) {
														onChange(value);
													}
													GUIChanged(unityObject);
												}
											}, new uNodeUtility.EditValueSettings() { nullable = false, acceptUnityObject = acceptUnityObject });
											EditValueLayouted(new GUIContent("Value"), values[i], valType, delegate (object val) {
												uNodeEditorUtility.RegisterUndo(unityObject, "");
												values[i] = val;
												newMap = ReflectionUtils.CreateInstance(type) as IDictionary;
												for(int x = 0; x < values.Count; x++) {
													newMap.Add(keys[x], values[x]);
												}
												value = newMap;
												if(onChange != null) {
													onChange(value);
												}
												GUIChanged(unityObject);
											});
											EditorGUI.indentLevel--;
										}
										EditorGUI.indentLevel--;
									}
								}
							}
							if(EditorGUI.EndChangeCheck()) {
								uNodeEditorUtility.RegisterUndo(unityObject, "");
								value = newMap;
								if(onChange != null) {
									onChange(value);
								}
								GUIChanged(unityObject);
							}
						}
						else {
							DrawNullValue(label, type, delegate (object o) {
								uNodeEditorUtility.RegisterUndo(unityObject, "Create Field Instance");
								if(onChange != null) {
									onChange(o);
								}
								GUIChanged(unityObject);
							});
							EditorGUI.EndChangeCheck();
						}
					}
					else {
						EditorGUI.HelpBox(position, "Unsupported Values", MessageType.None);
					}
				}
				else {
					EditorGUI.HelpBox(position, "Unsupported Values", MessageType.None);
				}
			}
			else if(type.IsSubclassOf(typeof(UnityEngine.Object)) || type.IsInterface) {
				EditRuntimeTypeValue(position, label, value, type, onChange, allowSceneObject, acceptUnityObject);
			}
			else {
				if(label != GUIContent.none) {
					position = EditorGUI.PrefixLabel(position, label);
				}
				if(value != null) {
					if(EditorGUI.DropdownButton(position, new GUIContent($"new {type.PrettyName()}()"), FocusType.Keyboard, EditorStyles.miniButton)) {
						onChange?.Invoke(null);
					}
					if(Application.isPlaying == false && value is IRuntimeClassContainer) {
						var container = value as IRuntimeClassContainer;
						if(container.IsInitialized) {
							//This will make sure the instanced value is up to date.
							container.ResetInitialization();
						}
					}
					//TODO: in playmode show actual value instead of default value
					if(value is IRuntimeGraphWrapper graphWrapper) {
							uNodeGUI.DrawLinkedVariables(graphWrapper.WrappedVariables, graphWrapper.OriginalGraph, unityObject: unityObject);
					}
				}
				else {
					if(EditorGUI.DropdownButton(position, new GUIContent("null"), FocusType.Keyboard, EditorStyles.miniButton)) {
						if(type is RuntimeGraphType graphType && graphType.target is IClassDefinition classDefinition) {
							var model = classDefinition.GetModel();
							var instance = model.CreateWrapperInstance(graphType.FullName);
							if(instance != null) {
								onChange?.Invoke(instance);
							}
						}
					}
				}
			}
		}

		private static void DrawRuntimeTypeIsNotCompiled(Rect position, GUIContent label, RuntimeType type) {
			if(label != GUIContent.none) {
				position = EditorGUI.PrefixLabel(position, label);
			}
			if(EditorGUI.DropdownButton(position, new GUIContent("Type: " + type.PrettyName() + " is not compiled."), FocusType.Keyboard, EditorStyles.helpBox)) {
				if(type is IRuntimeMemberWithRef withRef) {
					var reference = withRef.GetReference();
					if(reference is BaseGraphReference graphReference && graphReference.UnityObject != null) {
						EditorGUIUtility.PingObject(graphReference.UnityObject);
					}
				}
			}
		}

		public static void EditRuntimeTypeValue(Rect position, GUIContent label, object value, RuntimeType type, Action<object> onChange, bool allowSceneObject, bool acceptUnityObject = true) {
			if(type is INativeMember) {
				var nativeType = (type as INativeMember).GetNativeMember() as Type;
				if(nativeType != null) {
					EditValue(position, label, value, nativeType, (val) => {
						onChange(val);
					}, new uNodeUtility.EditValueSettings() {
						acceptUnityObject = acceptUnityObject,
					});
				}
				else {
					DrawRuntimeTypeIsNotCompiled(position, label, type);
				}
			}
			else if(type is IFakeType) {
				if(label != GUIContent.none) {
					position = EditorGUI.PrefixLabel(position, label);
				}
				if(EditorGUI.DropdownButton(position, new GUIContent(value != null ? type.PrettyName(true) : "null", type.PrettyName(true)), FocusType.Keyboard) && Event.current.button == 0) {
					//Make sure don't mark if value changed.
					GUI.changed = false;
					var w = ActionPopupWindow.ShowWindow(position,
						value,
						delegate (ref object obj) {
							EditValueLayouted(new GUIContent("Values", type.PrettyName(true)), obj, type, delegate (object val) {
								ActionPopupWindow.GetLast().variable = val;
								if(onChange != null)
									onChange(val);
							}, new uNodeUtility.EditValueSettings() { acceptUnityObject = acceptUnityObject });
						},
						onGUIBottom: delegate (ref object obj) {
							if(GUILayout.Button("Close")) {
								GUI.changed = false;
								ActionPopupWindow.CloseLast();
							}
						}, width: 300, height: 250);
					w.headerName = "Edit Values";
					if(type.IsValueType) {
						//Close Action when editing value type and performing redo or undo to avoid wrong value.
						w.onUndoOrRedo = delegate () {
							w.Close();
						};
					}
				}
			}
			else if(type.IsSubclassOf(typeof(UnityEngine.Object)) || type.IsInterface) {
				if(label != GUIContent.none) {
					position = EditorGUI.PrefixLabel(position, label);
				}
				if(acceptUnityObject) {
					position.width -= 20;
					bool flag = value != null && !type.IsInstanceOfType(value);
					if(flag) {
						GUI.backgroundColor = Color.red;
					}
					string name;
					if(value != null) {
						if(value as Component) {
							name = (value as Component).gameObject.name + $" ({type.Name})";
						}
						else if(value as ScriptableObject) {
							name = (value as ScriptableObject).name + $"({type.Name})";
						}
						else {
							name = value.ToString();
						}
					}
					else {
						name = $"None ({type.Name})";
					}
					if(EditorGUI.DropdownButton(position, new GUIContent(name, uNodeEditorUtility.GetTypeIcon(type)), FocusType.Keyboard, EditorStyles.objectField)) {
						if(value is UnityEngine.Object uobj) {
							if(Event.current.clickCount == 2) {
								Selection.activeObject = uobj;
							}
							else {
								if(uobj is Component comp) {
									EditorGUIUtility.PingObject(comp.gameObject);
								}
								else {
									EditorGUIUtility.PingObject(uobj);
								}
							}
						}
					}
					if(flag) {
						GUI.backgroundColor = Color.white;
					}
					if(GUI.Button(new Rect(position.x + position.width, position.y, 20, position.height), GUIContent.none, uNodeGUIStyle.objectField)) {
						var items = new List<ItemSelector.CustomItem>();
						items.Add(ItemSelector.CustomItem.Create("None", () => {
							if(onChange != null) {
								onChange(null);
							}
						}, "#"));
						items.AddRange(ItemSelector.MakeCustomItemsForInstancedType(type, (val) => {
							if(onChange != null) {
								onChange(val);
							}
						}, allowSceneObject));
						ItemSelector.ShowCustomItem(items).ChangePosition(position.ToScreenRect());
					}
					uNodeEditorUtility.GUIDropArea(position,
						onDragPerform: () => {
							if(DragAndDrop.objectReferences.Length != 1) {
								return;
							}
							var dragObj = DragAndDrop.objectReferences[0];
							if(dragObj is GameObject) {
								foreach(var c in (dragObj as GameObject).GetComponents<MonoBehaviour>()) {
									if(ReflectionUtils.IsValidRuntimeInstance(c, type)) {
										if(onChange != null) {
											onChange(c);
										}
										break;
									}
								}
							} else if(ReflectionUtils.IsValidRuntimeInstance(dragObj, type)) {
								if(onChange != null) {
									onChange(dragObj);
								}
							}
							else {
								uNodeEditorUtility.DisplayErrorMessage("Invalid dragged object.");
								DragAndDrop.objectReferences = new UnityEngine.Object[0];
							}
						},
						repaintAction: () => {
							//GUI.DrawTexture(position, uNodeEditorUtility.MakeTexture(1, 1, new Color(0, 0.5f, 1, 0.5f)));
							EditorGUI.DrawRect(position, new Color(0, 0.5f, 1, 0.5f));
						});
				}
				else {
					if(EditorGUI.DropdownButton(position, new GUIContent("null"), FocusType.Keyboard, EditorStyles.miniButton)) {

					}
				}
			}
			else {
				if(label != GUIContent.none) {
					position = EditorGUI.PrefixLabel(position, label);
				}
				if(value != null) {
					if(EditorGUI.DropdownButton(position, new GUIContent($"new {type.PrettyName()}()"), FocusType.Keyboard, EditorStyles.miniButton)) {
						onChange?.Invoke(null);
					}
				}
				else {
					if(EditorGUI.DropdownButton(position, new GUIContent("null"), FocusType.Keyboard, EditorStyles.miniButton)) {
						if(type is RuntimeGraphType graphType && graphType.target is IClassDefinition classDefinition) {
							var model = classDefinition.GetModel();
							var instance = model.CreateWrapperInstance(graphType.FullName);
							if(instance != null) {
								onChange?.Invoke(instance);
							}
						}
					}
				}
			}
		}
		#endregion

		#region MemberData
		public static void DrawMember(Rect position, MemberData member, FilterAttribute filter = null, UnityEngine.Object unityObject = null, Action<MemberData> onChange = null) {
			if(filter == null) {
				filter = new FilterAttribute();
			}
			Type t = filter.Types.Count == 1 && (member.type == null || !member.type.IsCastableTo(filter.Types[0])) ? filter.Types[0] : member.type;
			if(!filter.OnlyGetType &&
				filter.IsValidTarget(MemberData.TargetType.Values) &&
				!filter.SetMember &&
				(member.targetType == MemberData.TargetType.Values || t != null && member.targetType != MemberData.TargetType.Type)) {
				DrawMemberValues(position, member, t, filter, unityObject, onChange);
			}
			else {
				DrawMemberReference(position, member, filter, unityObject, onChange);
			}
		}

		public static void RenderVariable(Rect position, MemberData member, GUIContent label, FilterAttribute filter = null, UnityEngine.Object unityObject = null, Action<MemberData> onChange = null) {
			if(member == null)
				return;
			if(filter == null) {
				filter = new FilterAttribute();
			}
			position = EditorGUI.PrefixLabel(position, label);
			int oldIndent = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;
			if(filter.UnityReference &&
				!filter.OnlyGetType &&
				!member.isStatic &&
				!member.targetType.HasFlags(MemberData.TargetType.Values |
					MemberData.TargetType.Type |
					MemberData.TargetType.Null |
					MemberData.TargetType.uNodeGenericParameter)) {
				Rect rect = position;
				DrawInstanceValue(ref rect, GUIContent.none, member, unityObject, onChange);
				DrawMember(rect, member, filter, unityObject, onChange);
			}
			else {
				DrawMember(position, member, filter, unityObject, onChange);
			}

			EditorGUI.indentLevel = oldIndent;
		}

		private static void DrawInstanceValue(ref Rect position, GUIContent label, MemberData member, UnityEngine.Object unityObject = null, Action<MemberData> onChange = null) {
			object target = member.instance;
			if(target != null) {
				if(target is MemberData) {
					var targetType = (target as MemberData).targetType;
					if(targetType != MemberData.TargetType.Values &&
						targetType != MemberData.TargetType.None &&
						targetType != MemberData.TargetType.Self &&
						targetType != MemberData.TargetType.Type) {
						position.width = position.width / 2;
						EditorGUI.HelpBox(position, uNodeUtility.GetDisplayName(target), MessageType.None);
						position.x += position.width;
						return;
					}
					else {
						target = (target as MemberData).Get(null);
					}
				}
				else if(target is IGetValue) {
					target = (target as IGetValue).Get();
				}
			}
			Type t = member.IsTargetingUNode ? typeof(Graph) : member.startType;
			if(t == null) {
				if(target != null) {
					t = target.GetType();
				}
				else {
					t = typeof(object);
				}
			}
			if(t.IsCastableTo(typeof(UGraphElement))) {
				return;
			}
			bool flag = (target == null || !target.GetType().IsCastableTo(t)) &&
				!member.isStatic && member.targetType != MemberData.TargetType.None;
			if(flag && target != null && member.IsTargetingUNode)
				flag = false;
			if(flag)
				GUI.backgroundColor = Color.red;
			position.width = position.width / 2;
			if(!t.IsCastableTo(typeof(UnityEngine.Object)) || target is MemberData) {
				if(target == null && ReflectionUtils.CanCreateInstance(t)) {
					target = ReflectionUtils.CreateInstance(t) ?? MemberData.CreateFromValue(null, t);
					member.instance = target;
					if(onChange != null) {
						onChange(member);
					}
					uNodeGUIUtility.GUIChanged(unityObject);
				}
				uNodeGUIUtility.EditValue(position, label, target, t, delegate (object val) {
					target = val;
					member.instance = target;
					if(onChange != null) {
						onChange(member);
					}
				}, new uNodeUtility.EditValueSettings() {
					acceptUnityObject = true,
					nullable = true,
					unityObject = unityObject,
				});
			}
			else {
				if(position.Contains(Event.current.mousePosition) && Event.current.button == 1 && Event.current.type == EventType.MouseUp) {
					GUI.changed = false;
					GenericMenu menu = new GenericMenu();
					if(target != null) {
						bool isGo = true;
						GameObject go = null;
						Component comp = null;
						if(target is GameObject) {
							go = target as GameObject;
						}
						if(target is Component || target.GetType().IsSubclassOf(typeof(Component))) {
							comp = target as Component;
							isGo = false;
						}
						if(go != null || comp != null) {
							if(isGo) {
								Component[] comps = go.GetComponents<Component>();
								menu.AddItem(new GUIContent("0-" + go.GetType().Name), true, delegate (object obj) {
									member.instance = obj;
									if(onChange != null) {
										onChange(member);
									}
									uNodeGUIUtility.GUIChanged(unityObject);
								}, go);
								int index = 1;
								foreach(Component com in comps) {
									menu.AddItem(new GUIContent(index + "-" + com.GetType().Name), false, delegate (object obj) {
										member.instance = obj;
										if(onChange != null) {
											onChange(member);
										}
										uNodeGUIUtility.GUIChanged(unityObject);
									}, com);
									index++;
								}
							}
							else {
								GameObject g = comp.gameObject;
								menu.AddItem(new GUIContent("0-" + g.GetType().Name), false, delegate (object obj) {
									member.instance = obj;
									if(onChange != null) {
										onChange(member);
									}
									uNodeGUIUtility.GUIChanged(unityObject);
								}, g);
								Component[] comps = comp.GetComponents<Component>();
								int index = 1;
								foreach(Component com in comps) {
									menu.AddItem(new GUIContent(index + "-" + com.GetType().Name), com.Equals(target), delegate (object obj) {
										member.instance = obj;
										if(onChange != null) {
											onChange(member);
										}
										uNodeGUIUtility.GUIChanged(unityObject);
									}, com);
									index++;
								}
							}
						}
					}
					menu.ShowAsContext();
				}
				EditorGUI.BeginChangeCheck();
				var newVar = target as UnityEngine.Object;
				newVar = EditorGUI.ObjectField(position, newVar, typeof(UnityEngine.Object), uNodeEditorUtility.IsSceneObject(unityObject));
				if(EditorGUI.EndChangeCheck()) {
					if(unityObject)
						uNodeEditorUtility.RegisterUndo(unityObject, label.text);
					member.instance = newVar;
					if(onChange != null) {
						onChange(member);
					}
					uNodeGUIUtility.GUIChanged(unityObject);
				}
			}
			position.x += position.width;
			if(flag)
				GUI.backgroundColor = Color.white;
		}

		public static void DrawMemberValues(Rect position, MemberData member, Type type, FilterAttribute filter,
			UnityEngine.Object unityObject = null, Action<MemberData> onChange = null) {
			Type t = type;
			if(t == null) {
				t = member.type;
			}
			if(t != null) {
				if(filter == null)
					filter = new FilterAttribute();
				bool flag = type != null && !filter.SetMember && filter.IsValidTarget(MemberData.TargetType.Values) &&
					(filter.IsValidTarget(MemberData.TargetType.Constructor | MemberData.TargetType.Event | MemberData.TargetType.Field | MemberData.TargetType.Method | MemberData.TargetType.Type | MemberData.TargetType.Property) || !filter.IsValueTypes());
				if(flag)
					position.width -= 16;
				if(type != null && member.targetType != MemberData.TargetType.Values) {
					DrawMemberReference(position, member, filter, unityObject, onChange);
					if(filter.ValidTargetType == MemberData.TargetType.Values && !filter.InvalidTargetType.HasFlags(MemberData.TargetType.Values) && filter.IsValueTypes()) {
						member.targetType = MemberData.TargetType.Values;
						member.type = t;
						if(onChange != null) {
							onChange(member);
						}
						uNodeGUIUtility.GUIChanged(unityObject);
					}
				}
				else {
					object obj = member.GetInstance();
					if(ReflectionUtils.CanCreateInstance(t) || t.IsCastableTo(typeof(UnityEngine.Object))) {
						if(!member.TargetSerializedType.isFilled) {
							member.type = t;
						}
						if(obj != null && t.IsCastableTo(typeof(UnityEngine.Object)) && !obj.GetType().IsCastableTo(typeof(UnityEngine.Object))) {
							obj = null;
							member.CopyFrom(MemberData.CreateFromValue(obj));
							if(onChange != null) {
								onChange(member);
							}
							uNodeGUIUtility.GUIChanged(unityObject);
						}
						if(t.IsValueType && obj == null || !t.IsCastableTo(typeof(UnityEngine.Object)) && obj != null && !obj.GetType().IsCastableTo(t)) {
							obj = ReflectionUtils.CreateInstance(t);
							member.CopyFrom(MemberData.CreateFromValue(obj));
							if(onChange != null) {
								onChange(member);
							}
							uNodeGUIUtility.GUIChanged(unityObject);
						}
						uNodeGUIUtility.EditValue(position, GUIContent.none, obj, t, delegate (object val) {
							member.CopyFrom(MemberData.CreateFromValue(val));
							if(onChange != null) {
								onChange(member);
							}
						}, new uNodeUtility.EditValueSettings() {
							acceptUnityObject = true,
							nullable = filter.IsValidTarget(MemberData.TargetType.Null),
							unityObject = unityObject,
						});
					}
					else {
						uNodeGUI.Label(position, "null", (GUIStyle)"HelpBox");
					}
				}
				if(flag) {
					position.x += position.width;
					position.width = 16;
					bool check = EditorGUI.Toggle(position, member.targetType != MemberData.TargetType.Values, EditorStyles.radioButton);
					if(check && member.targetType == MemberData.TargetType.Values) {
						//string tName = variable.targetTypeName;
						if(unityObject)
							uNodeEditorUtility.RegisterUndo(unityObject, "");
						//variable.Reset();
						//variable.targetTypeName = tName;
						member.targetType = t.IsValueType ? MemberData.TargetType.None : MemberData.TargetType.Null;
						member.ResetCache();
						if(onChange != null) {
							onChange(member);
						}
						uNodeGUIUtility.GUIChanged(unityObject);
					}
					else if(!check && member.targetType != MemberData.TargetType.Values) {
						if(unityObject)
							uNodeEditorUtility.RegisterUndo(unityObject, "");
						member.targetType = MemberData.TargetType.Values;
						member.type = t;
						member.ResetCache();
						if(onChange != null) {
							onChange(member);
						}
						uNodeGUIUtility.GUIChanged(unityObject);
					}
				}
			}
		}

		public static void DrawMemberValues(GUIContent label, MemberData member, Type type, FilterAttribute filter,
			UnityEngine.Object unityObject = null, Action<MemberData> onChange = null) {
			Type t = type;
			if(t == null) {
				t = member.type;
			}
			if(t != null) {
				if(filter == null)
					filter = new FilterAttribute();
				object obj = member.GetInstance();
				if(ReflectionUtils.CanCreateInstance(t) || t.IsCastableTo(typeof(UnityEngine.Object))) {
					if(!member.TargetSerializedType.isFilled) {
						member.type = t;
					}
					if(obj != null && t.IsCastableTo(typeof(UnityEngine.Object)) && !obj.GetType().IsCastableTo(typeof(UnityEngine.Object))) {
						obj = null;
						member.CopyFrom(MemberData.CreateFromValue(obj));
						if(onChange != null) {
							onChange(member);
						}
						uNodeGUIUtility.GUIChanged(unityObject);
					}
					if(t.IsValueType && obj == null || !t.IsCastableTo(typeof(UnityEngine.Object)) &&
						obj != null && !obj.GetType().IsCastableTo(t)) {
						obj = ReflectionUtils.CreateInstance(t);
						member.CopyFrom(MemberData.CreateFromValue(obj));
						if(onChange != null) {
							onChange(member);
						}
						uNodeGUIUtility.GUIChanged(unityObject);
					}
					uNodeGUIUtility.EditValueLayouted(label, obj, t, delegate (object val) {
						obj = val;
						member.CopyFrom(MemberData.CreateFromValue(obj));
						if(onChange != null) {
							onChange(member);
						}
						uNodeGUIUtility.GUIChanged(unityObject);
					}, new uNodeUtility.EditValueSettings() {
						acceptUnityObject = true,
						unityObject = unityObject,
						nullable = filter.IsValidTarget(MemberData.TargetType.Null)
					});
				}
				else {
					uNodeGUI.Label(uNodeGUIUtility.GetRect(), "null", (GUIStyle)"HelpBox");
				}
			}
		}

		public static void DrawMemberReference(Rect position, MemberData variable, FilterAttribute filter = null, UnityEngine.Object unityObject = null, Action<MemberData> onChange = null) {
			DrawVariableReference(position, new GUIContent(variable.DisplayName(), variable.Tooltip), variable, filter, unityObject, onChange);
		}

		public static void DrawVariableReference(Rect position, GUIContent label, MemberData variable, FilterAttribute filter = null, UnityEngine.Object unityObject = null, Action<MemberData> onChange = null) {
			if(filter == null) {
				filter = new FilterAttribute();
			}
			bool enabled = filter.DisplayDefaultStaticType;
			if(!enabled)
				EditorGUI.BeginDisabledGroup(true);
			if(EditorGUI.DropdownButton(position, label, FocusType.Keyboard)) {
				if(Event.current.button == 0) {
					GUI.changed = false;
					if(filter.OnlyGetType && filter.CanManipulateArray()) {
						TypeBuilderWindow.Show(position, unityObject, filter, delegate (MemberData[] types) {
							uNodeEditorUtility.RegisterUndo(unityObject);
							variable.CopyFrom(types[0]);
							if(onChange != null) {
								onChange(types[0]);
							}
							uNodeGUIUtility.GUIChanged(unityObject);
						}, new TypeItem[1] { variable });
					}
					else {
						ItemSelector.ShowWindow(unityObject, filter, (m) => {
							m.ResetCache();
							if(onChange != null) {
								onChange(m);
							}
							uNodeGUIUtility.GUIChanged(unityObject);
						}).ChangePosition(position.ToScreenRect());
					}
				}
				else if(Event.current.button == 1 && (variable.targetType == MemberData.TargetType.Method || variable.targetType == MemberData.TargetType.Constructor)) {
					if(filter.ValidMemberType.HasFlags(MemberTypes.Constructor | MemberTypes.Method)) {
						var mPos = Event.current.mousePosition;
						var members = variable.GetMembers(false);
						if(members != null && members.Length == 1) {
							var member = members[members.Length - 1];
							if(variable.targetType == MemberData.TargetType.Method) {
								BindingFlags flag = BindingFlags.Public;
								if(variable.isStatic) {
									flag |= BindingFlags.Static;
								}
								else {
									flag |= BindingFlags.Instance;
								}
								var memberName = member.Name;
								var mets = member.ReflectedType.GetMember(memberName, flag);
								List<MethodInfo> methods = new List<MethodInfo>();
								foreach(var m in mets) {
									if(m is MethodInfo) {
										methods.Add(m as MethodInfo);
									}
								}
								GenericMenu menu = new GenericMenu();
								foreach(var m in methods) {
									menu.AddItem(new GUIContent("Change Methods/" + EditorReflectionUtility.GetPrettyMethodName(m)), member == m, delegate (object obj) {
										object[] objs = obj as object[];
										MemberData mem = objs[0] as MemberData;
										MethodInfo method = objs[1] as MethodInfo;
										UnityEngine.Object UO = objs[2] as UnityEngine.Object;
										if(member != m) {
											if(method.IsGenericMethodDefinition) {
												TypeBuilderWindow.Show(mPos, UO, new FilterAttribute() { UnityReference = false },
												delegate (MemberData[] types) {
													uNodeEditorUtility.RegisterUndo(UO);
													method = ReflectionUtils.MakeGenericMethod(method, types.Select(i => i.startType).ToArray());
													MemberData d = new MemberData(method);
													mem.CopyFrom(d);
													mem.instance = null;
												}, new TypeItem[method.GetGenericArguments().Length]);
											}
											else {
												uNodeEditorUtility.RegisterUndo(UO);
												MemberData d = new MemberData(method);
												mem.CopyFrom(d);
												mem.instance = null;
											}
										}
									}, new object[] { variable, m, unityObject });
								}
								menu.ShowAsContext();
							}
							else if(variable.targetType == MemberData.TargetType.Constructor) {
								GenericMenu menu = new GenericMenu();
								BindingFlags flag = BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic;
								var ctors = member.ReflectedType.GetConstructors(flag);
								foreach(var m in ctors) {
									menu.AddItem(new GUIContent("Change Constructors/" + EditorReflectionUtility.GetPrettyConstructorName(m)), member == m, delegate (object obj) {
										object[] objs = obj as object[];
										MemberData mem = objs[0] as MemberData;
										ConstructorInfo ctor = objs[1] as ConstructorInfo;
										UnityEngine.Object UO = objs[2] as UnityEngine.Object;
										if(member != m) {
											MemberData d = new MemberData(ctor);
											mem.CopyFrom(d);
											mem.instance = null;
										}
									}, new object[] { variable, m, unityObject });
								}
							}
						}
					}
				}
			}

			if(!enabled)
				EditorGUI.EndDisabledGroup();
		}
		#endregion

		/// <summary>
		/// True if type can be edited.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static bool IsSupportedType(Type type) {
			if(type == typeof(UnityEngine.Object) || type.IsSubclassOf(typeof(UnityEngine.Object))) {
				return true;
			}
			if(!type.IsInterface && !type.IsAbstract) {
				return type == typeof(string) || type == typeof(Enum) || type.IsSubclassOf(typeof(Enum)) || type == typeof(MemberData) || type == typeof(AnimationCurve) || type.IsValueType;
			}
			return false;
		}

		public static void DrawList<T>(GUIContent label, List<T> list, UnityEngine.Object targetObject, Action<List<T>> action, Action<List<T>> onAddClick = null, Func<T, int, GUIContent> funcGetElementLabel = null, bool enableMoveElement = true) {
			if(list == null) {
				list = new List<T>();
				if(action != null) {
					action(list);
				}
			}
			Rect rect = GetRect("PreButton");
			Rect addRect = new Rect(rect.x + rect.width - 20, rect.y, 20, rect.height);
			if(!addRect.Contains(Event.current.mousePosition) || onAddClick == null) {
				if(GUI.Button(rect, label, (GUIStyle)"PreButton")) {

				}
			}
			else {
				GUI.Box(rect, label, (GUIStyle)"PreButton");
			}
			if(onAddClick != null) {
				if(GUI.Button(addRect, new GUIContent(""), (GUIStyle)"OL Plus")) {
					onAddClick(list);
				}
			}
			for(int i = 0; i < list.Count; i++) {
				T element = list[i];
				Rect aRect = GetRect("minibutton", GUILayout.Height(15));
				if(aRect.Contains(Event.current.mousePosition) && Event.current.button == 0 && Event.current.clickCount == 2) {
					FieldsEditorWindow.ShowWindow(list[i], targetObject, delegate (object obj) {
						return list[(int)obj];
					}, i);
				}
				GUIContent elementLabel = null;
				if(funcGetElementLabel != null) {
					GUIContent content = funcGetElementLabel(element, i);
					if(content != null) {
						elementLabel = content;
					}
				}
				if(elementLabel == null) {
					elementLabel = new GUIContent("Element");
				}
				if(GUI.Button(aRect, elementLabel, "minibutton")) {
					if(Event.current.button == 1) {
						GenericMenu menu = new GenericMenu();
						menu.AddItem(new GUIContent("Remove"), false, delegate (object obj) {
							uNodeEditorUtility.RegisterUndo(targetObject, "Remove List Element");
							if(action != null) {
								action(list);
							}
						}, element);
						menu.AddSeparator("");
						if(enableMoveElement) {
							menu.AddItem(new GUIContent("Move To Top"), false, delegate (object obj) {
								if((int)obj != 0) {
									uNodeEditorUtility.RegisterUndo(targetObject, "Move List To Top");
									uNodeEditorUtility.ListMoveToTop(list, (int)obj);
									if(action != null) {
										action(list);
									}
								}
							}, i);
							menu.AddItem(new GUIContent("Move Up"), false, delegate (object obj) {
								if((int)obj != 0) {
									uNodeEditorUtility.RegisterUndo(targetObject, "Move List Up");
									uNodeEditorUtility.ListMoveUp(list, (int)obj);
									if(action != null) {
										action(list);
									}
								}
							}, i);
							menu.AddItem(new GUIContent("Move Down"), false, delegate (object obj) {
								if((int)obj != list.Count - 1) {
									uNodeEditorUtility.RegisterUndo(targetObject, "Move List Down");
									uNodeEditorUtility.ListMoveDown(list, (int)obj);
									if(action != null) {
										action(list);
									}
								}
							}, i);
							menu.AddItem(new GUIContent("Move To Bottom"), false, delegate (object obj) {
								if((int)obj != list.Count - 1) {
									uNodeEditorUtility.RegisterUndo(targetObject, "Move List To Bottom Attribute");
									uNodeEditorUtility.ListMoveToBottom(list, (int)obj);
									if(action != null) {
										action(list);
									}
								}
							}, i);
						}
						menu.ShowAsContext();
					}
				}
			}
		}

		#region GUIUtility
		public static void DrawEnumData(EnumData data, UnityEngine.Object owner = null) {
			if(data == null) {
				throw new ArgumentNullException("data");
			}
			ShowField("name", data, owner);
			ShowField("inheritFrom", data, owner);
			ShowField("modifiers", data, owner);
			ShowField("enumeratorList", data, owner);
		}

		public static void DrawConstructorInitializer(ConstructorValueData cVal, Action<ConstructorValueData> onChanged, UnityEngine.Object unityObject = null) {
			Type type = cVal.type;
			if(type == null || type.IsPrimitive || type == typeof(decimal) || type == typeof(string) || type is RuntimeType)
				return;
			EditorGUILayout.BeginVertical("Box");
			EditorGUILayout.LabelField("Initializer", EditorStyles.boldLabel);
			if(cVal.initializer.Length > 0) {
				for(int i = 0; i < cVal.initializer.Length; i++) {
					var val = cVal.initializer[i];
					var index = i;
					if(val == null)
						continue;
					var vType = val.type;
					string vName = ObjectNames.NicifyVariableName(val.name);
					if(vType != null) {
						if(vType == typeof(object) && !object.ReferenceEquals(val.value, null)) {
							vType = val.value.GetType();
						}
						EditValueLayouted(new GUIContent(vName), val.value, vType, v => {
							uNodeEditorUtility.RegisterUndo(unityObject);
							val.value = v;
							cVal.initializer[index] = val;
							onChanged(cVal);
							GUIChanged(unityObject);
						});
					}
					else {
						var position = EditorGUI.PrefixLabel(GetRect(), new GUIContent(vName));
						EditorGUI.HelpBox(position, "Type not found", MessageType.Error);
					}
				}
			}
			else {
				EditorGUILayout.LabelField("No initializer");
			}
			EditorGUILayout.EndVertical();
			EditorGUILayout.BeginHorizontal();
			if(GUILayout.Button(new GUIContent("Refresh", ""), EditorStyles.miniButtonLeft)) {
				var init = cVal.initializer;
				if(cVal.type.IsArray || cVal.type.IsCastableTo(typeof(IList))) {
					for(int i = 0; i < init.Length; i++) {
						init[i].name = "Element" + i;
					}
				}
				else {
					var fields = cVal.type.GetMembers();
					for(int x = 0; x < fields.Length; x++) {
						var field = fields[x];
						var t = ReflectionUtils.GetMemberType(field);
						for(int y = 0; y < init.Length; y++) {
							if(field.Name == init[y].name) {
								if(t != init[y].type) {
									init[y] = new ParameterValueData(field.Name, t);
								}
								break;
							}
						}
					}
				}
				onChanged(cVal);
				GUIChanged(unityObject);
			}
			if(GUILayout.Button(new GUIContent("Fields", ""), EditorStyles.miniButtonMid)) {
				var init = cVal.initializer;
				bool hasAddMenu = false;
				GenericMenu menu = new GenericMenu();
				if(cVal.type.IsArray || cVal.type.IsCastableTo(typeof(IList))) {
					menu.AddItem(new GUIContent("Add Field"), false, delegate (object obj) {
						uNodeEditorUtility.RegisterUndo(unityObject, "Add Field");
						var ctor = obj as ConstructorValueData;
						uNodeUtility.AddArray(ref ctor.initializer, new ParameterValueData("Element", cVal.type.ElementType()));
						for(int i = 0; i < ctor.initializer.Length; i++) {
							ctor.initializer[i].name = "Element" + i;
						}
						onChanged(ctor);
						GUIChanged(unityObject);
					}, cVal);
					foreach(var v in init) {
						menu.AddItem(new GUIContent("Remove Field/" + v.name), false, delegate (object obj) {
							uNodeEditorUtility.RegisterUndo(unityObject, "Remove Field:" + v.name);
							var ctor = (obj as object[])[0] as ConstructorValueData;
							uNodeUtility.RemoveArray(ref ctor.initializer, (obj as object[])[1] as ParameterValueData);
							for(int i = 0; i < init.Length; i++) {
								init[i].name = "Element" + i;
							}
							onChanged(ctor);
							GUIChanged(unityObject);
						}, new object[] { cVal, v });
					}
				}
				else {
					var fields = cVal.type.GetMembers(BindingFlags.Public | BindingFlags.Instance);
					foreach(var vv in fields) {
						if(vv is FieldInfo || vv is PropertyInfo && (vv as PropertyInfo).CanWrite && (vv as PropertyInfo).GetIndexParameters().Length == 0) {
							bool valid = true;
							foreach(var v in init) {
								if(v.name == vv.Name) {
									valid = false;
									break;
								}
							}
							if(valid) {
								hasAddMenu = true;
								break;
							}
						}
					}
					if(hasAddMenu) {
						menu.AddItem(new GUIContent("Add All Fields"), false, delegate (object obj) {
							var ctor = obj as ConstructorValueData;
							foreach(var v in fields) {
								if(v is FieldInfo field) {
									if(field.Attributes.HasFlags(FieldAttributes.InitOnly))
										continue;
								}
								else if(v is PropertyInfo property) {
									if(!property.CanWrite || property.GetIndexParameters().Length > 0) {
										continue;
									}
								}
								else {
									continue;
								}
								var t = ReflectionUtils.GetMemberType(v);
								bool valid = true;
								foreach(var vv in ctor.initializer) {
									if(v.Name == vv.name) {
										valid = false;
										break;
									}
								}
								if(valid) {
									uNodeEditorUtility.RegisterUndo(unityObject, "");
									uNodeUtility.AddArray(ref ctor.initializer, new ParameterValueData(v.Name, t));
								}
							}
							onChanged(ctor);
							GUIChanged(unityObject);
						}, cVal);
					}
					foreach(var v in fields) {
						if(v is FieldInfo field) {
							if(field.Attributes.HasFlags(FieldAttributes.InitOnly))
								continue;
						}
						else if(v is PropertyInfo property) {
							if(!property.CanWrite || property.GetIndexParameters().Length > 0) {
								continue;
							}
						}
						else {
							continue;
						}
						var t = ReflectionUtils.GetMemberType(v);
						bool valid = true;
						foreach(var vv in init) {
							if(v.Name == vv.name) {
								valid = false;
								break;
							}
						}
						if(valid) {
							menu.AddItem(new GUIContent("Add Field/" + v.Name), false, delegate (object obj) {
								uNodeEditorUtility.RegisterUndo(unityObject, "Add Field:" + v.Name);
								var ctor = obj as ConstructorValueData;
								uNodeUtility.AddArray(ref ctor.initializer, new ParameterValueData(v.Name, t));
								onChanged(ctor);
								GUIChanged(unityObject);
							}, cVal);
						}
					}
					foreach(var v in init) {
						menu.AddItem(new GUIContent("Remove Field/" + v.name), false, delegate (object obj) {
							uNodeEditorUtility.RegisterUndo(unityObject, "Remove Field:" + v.name);
							var ctor = (obj as object[])[0] as ConstructorValueData;
							uNodeUtility.RemoveArray(ref ctor.initializer, (obj as object[])[1] as ParameterValueData);
							onChanged(ctor);
							GUIChanged(unityObject);
						}, new object[] { cVal, v });
					}
				}
				menu.ShowAsContext();
			}
			if(GUILayout.Button(new GUIContent("Reset", ""), EditorStyles.miniButtonRight)) {
				uNodeEditorUtility.RegisterUndo(unityObject, "Reset Initializer");
				cVal.initializer = new ParameterValueData[0];
				onChanged(cVal);
				GUIChanged(unityObject);
			}
			EditorGUILayout.EndHorizontal();
		}

		public static bool IsDoubleClick(Rect rect, int button = 0) {
			return Event.current.clickCount == 2 && rect.Contains(Event.current.mousePosition) && Event.current.button == button;
		}

		public static bool IsClicked(Rect rect, int button = 0) {
			return Event.current.clickCount >= 1 && rect.Contains(Event.current.mousePosition) && Event.current.button == button;
		}

		public static void ShowFields(object obj, UnityEngine.Object unityObject = null, BindingFlags flags = BindingFlags.Public | BindingFlags.Instance,
			uNodeUtility.EditValueSettings setting = null) {
			FieldInfo[] fieldInfo = ReflectionUtils.GetFields(obj, flags);
			Array.Sort(fieldInfo, (x, y) => {
				if(x.DeclaringType != y.DeclaringType) {
					return string.Compare(x.DeclaringType.IsSubclassOf(y.DeclaringType).ToString(), y.DeclaringType.IsSubclassOf(x.DeclaringType).ToString(), StringComparison.OrdinalIgnoreCase);
				}
				return string.Compare(x.MetadataToken.ToString(), y.MetadataToken.ToString(), StringComparison.OrdinalIgnoreCase);
			});
			ShowFields(fieldInfo, obj, unityObject);
		}

		public static void ShowFields(FieldInfo[] fields, object targetField, UnityEngine.Object unityObject = null, uNodeUtility.EditValueSettings setting = null) {
			foreach(FieldInfo field in fields) {
				if(IsHide(field, targetField)) {
					continue;
				}
				var control = FieldControl.FindControl(field.FieldType, false);
				if(control != null) {
					control.DrawLayouted(field.GetValueOptimized(targetField), new GUIContent(ObjectNames.NicifyVariableName(field.Name)), field.FieldType, (val) => {
						uNodeEditorUtility.RegisterUndo(unityObject, "");
						field.SetValueOptimized(targetField, val);
						GUIChanged(unityObject);
					}, new uNodeUtility.EditValueSettings() {
						attributes = field.GetCustomAttributes(true),
						unityObject = unityObject
					});
					continue;
				}
				ShowField(field, targetField, unityObject, setting);
			}
		}

		public static FieldInfo GetField(object parent, string targetField) {
			return parent.GetType().GetField(targetField, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic);
		}

		public static Type GetFieldValueType(object parent, string targetField) {
			FieldInfo field = parent.GetType().GetField(targetField);
			object obj = field.GetValueOptimized(parent);
			if(obj is MemberData) {
				return (obj as MemberData).startType;
			}
			else if(obj is string) {
				return TypeSerializer.Deserialize(obj as string, false);
			}
			else if(obj is BaseValueData) {
				return (obj as BaseValueData).Get() as Type;
			}
			else if(obj is ValueData) {
				return (obj as ValueData).Get() as Type;
			}
			if(obj is Type) {
				return obj as Type;
			}
			return null;
		}

		public static bool IsHide(FieldInfo field, object targetField) {
			if(field.IsDefined(typeof(NonSerializedAttribute), true) || field.IsDefined(typeof(HideInInspector), true))
				return true;
			if(targetField == null || !field.IsDefined(typeof(HideAttribute), true))
				return false;
			foreach(HideAttribute hideA in ((HideAttribute[])field.GetCustomAttributes(typeof(HideAttribute), true))) {
				if(string.IsNullOrEmpty(hideA.targetField)) {
					if(hideA.defaultOnHide && field.GetValueOptimized(targetField) != hideA.defaultValue) {
						field.SetValueOptimized(targetField, hideA.defaultValue);
					}
					return true;
				}
				object targetRef = ReflectionUtils.GetFieldValue(targetField, hideA.targetField);
				if(targetRef != null) {
					bool isHide = false;
					bool same = true;
					Type targetRefType = targetRef.GetType();
					if(targetRefType == typeof(MemberData)) {
						var fieldVal = targetRef as MemberData;
						if(fieldVal != null) {
							if(hideA.hideValue == null) {
								same = (!fieldVal.isAssigned || !fieldVal.TargetSerializedType.isFilled);
								if(hideA.hideOnSame && same) {
									isHide = true;
								}
								else if(!hideA.hideOnSame && !same) {
									isHide = true;
								}
							}
							else if(hideA.hideValue != null && (!hideA.hideOnSame || fieldVal.isAssigned && fieldVal.TargetSerializedType.isFilled)) {
								Type validType = fieldVal.type;
								if(validType != null) {
									if(hideA.elementType && (validType.IsArray || validType.IsGenericType)) {
										if(validType.IsArray) {
											validType = validType.GetElementType();
										}
										else {
											validType = validType.GetGenericArguments()[0];
										}
									}
								}
								if(hideA.hideValue is Type) {
									same = ((Type)hideA.hideValue) == validType || validType.IsCastableTo((Type)hideA.hideValue);
									if(hideA.hideOnSame && same) {
										isHide = true;
									}
									else if(!hideA.hideOnSame && !same) {
										isHide = true;
									}
								}
								else if(hideA.hideValue is Type[]) {
									Type[] hT = hideA.hideValue as Type[];
									for(int i = 0; i < hT.Length; i++) {
										same = hT[i] == validType || validType.IsCastableTo(hT[i]);
										if(hideA.hideOnSame && same) {
											isHide = true;
											break;
										}
										else if(!hideA.hideOnSame) {
											if(!same) {
												isHide = true;
												continue;
											}
											else {
												isHide = false;
												break;
											}
										}
									}
								}
							}
						}
					}
					else {
						same = targetRef.Equals(hideA.hideValue);
						if(hideA.hideOnSame && same) {
							isHide = true;
						}
						else if(!hideA.hideOnSame && !same) {
							isHide = true;
						}
					}
					if(isHide) {
						if(hideA.defaultOnHide && field.GetValueOptimized(targetField) != hideA.defaultValue) {
							field.SetValueOptimized(targetField, hideA.defaultValue);
						}
						return true;
					}
				}
			}
			return false;
		}

		public static void DrawTypeDrawer(Rect position, SerializedType type, GUIContent label, Action<Type> onClick, FilterAttribute filter = null, object targetObject = null) {
			if(filter == null) {
				filter = FilterAttribute.DefaultTypeFilter;
			}
			else if(filter.OnlyGetType == false) {
				filter = new FilterAttribute(filter);
				filter.OnlyGetType = true;
			}
			GUIContent buttonLabel = new GUIContent();
			if(type == null) {
				buttonLabel.text = "Unassigned";
			}
			else {
				buttonLabel.text = type.prettyName;
				buttonLabel.tooltip = type.typeName;
			}
			position = EditorGUI.PrefixLabel(position, label);
			position.width -= 20;
			if(EditorGUI.DropdownButton(position, buttonLabel, FocusType.Keyboard) && Event.current.button == 0) {
				GUI.changed = false;
				if(Event.current.shift || Event.current.control || type?.type != null && type.type.IsGenericType) {
					TypeBuilderWindow.Show(position, targetObject, filter, (types) => {
						onClick(types[0].startType);
					}, new TypeItem[] { new TypeItem(type, filter) });
				}
				else {
					ItemSelector.ShowWindow(targetObject, filter, delegate (MemberData member) {
						onClick(member.startType);
					}).ChangePosition(position.ToScreenRect());
				}
			}
			position.x += position.width;
			position.width = 20;
			if(EditorGUI.DropdownButton(position, GUIContent.none, FocusType.Keyboard) && Event.current.button == 0) {
				GUI.changed = false;
				TypeBuilderWindow.Show(position, targetObject, filter, (types) => {
					onClick(types[0].startType);
				}, new TypeItem[] { new TypeItem(type, filter) });
			}
		}

		public static void DrawTypeDrawer(Rect position, Type type, GUIContent label, Action<Type> onClick, FilterAttribute filter = null, object targetObject = null) {
			if(filter == null) {
				filter = new FilterAttribute(typeof(object)) { OnlyGetType = true, UnityReference = false };
			}
			else if(filter.OnlyGetType == false) {
				filter = new FilterAttribute(filter);
				filter.OnlyGetType = true;
			}
			GUIContent buttonLabel = new GUIContent();
			if(type == null) {
				buttonLabel.text = "Unassigned";
			}
			else {
				buttonLabel.text = type.PrettyName();
			}
			position = EditorGUI.PrefixLabel(position, label);
			position.width -= 20;
			if(EditorGUI.DropdownButton(position, buttonLabel, FocusType.Keyboard) && Event.current.button == 0) {
				GUI.changed = false;
				if(Event.current.shift || Event.current.control || type != null && type.IsGenericType) {
					TypeBuilderWindow.Show(position, targetObject, filter, (types) => {
						onClick(types[0].startType);
					}, new TypeItem[] { new TypeItem(type, filter) });
				}
				else {
					ItemSelector.ShowWindow(targetObject, filter, delegate (MemberData member) {
						onClick(member.startType);
					}).ChangePosition(position.ToScreenRect());
				}
			}
			position.x += position.width;
			position.width = 20;
			if(EditorGUI.DropdownButton(position, GUIContent.none, FocusType.Keyboard) && Event.current.button == 0) {
				GUI.changed = false;
				TypeBuilderWindow.Show(position, targetObject, filter, (types) => {
					onClick(types[0].startType);
				}, new TypeItem[] { new TypeItem(type, filter) });
			}
		}

		public static void DrawTypeDrawer(SerializedType type, GUIContent label, Action<Type> onClick, FilterAttribute filter = null, object targetObject = null) {
			var position = GetRect();
			if(filter == null) {
				filter = new FilterAttribute(typeof(object)) { OnlyGetType = true, UnityReference = false };
			}
			else if(filter.OnlyGetType == false) {
				filter = new FilterAttribute(filter);
				filter.OnlyGetType = true;
			}
			GUIContent buttonLabel = new GUIContent();
			if(type == null) {
				buttonLabel.text = "Unassigned";
			}
			else {
				buttonLabel.text = type.prettyName;
				buttonLabel.tooltip = type.typeName;
			}
			position = EditorGUI.PrefixLabel(position, label);
			position.width -= 20;
			if(EditorGUI.DropdownButton(position, buttonLabel, FocusType.Keyboard) && Event.current.button == 0) {
				GUI.changed = false;
				if(Event.current.shift || Event.current.control) {
					TypeBuilderWindow.Show(position, targetObject, filter, (types) => {
						onClick(types[0].startType);
					}, new TypeItem[] { new TypeItem(type, filter) });
				}
				else {
					ItemSelector.ShowWindow(targetObject, filter, delegate (MemberData member) {
						onClick(member.startType);
					}).ChangePosition(position.ToScreenRect());
				}
			}
			position.x += position.width;
			position.width = 20;
			if(EditorGUI.DropdownButton(position, GUIContent.none, FocusType.Keyboard) && Event.current.button == 0) {
				GUI.changed = false;
				TypeBuilderWindow.Show(position, targetObject, filter, (types) => {
					onClick(types[0].startType);
				}, new TypeItem[] { new TypeItem(type, filter) });
			}
			if(type?.type != null && type.type.IsGenericType) {
				EditorGUI.indentLevel++;
				DrawGenericTypeArguments(type.type, targetObject, onClick);
				EditorGUI.indentLevel--;
			}
		}

		public static void DrawTypeDrawer(Type type, GUIContent label, Action<Type> onClick, FilterAttribute filter = null, object targetObject = null) {
			var position = GetRect();
			if(filter == null) {
				filter = FilterAttribute.DefaultTypeFilter;
			}
			else if(filter.OnlyGetType == false) {
				filter = new FilterAttribute(filter);
				filter.OnlyGetType = true;
			}
			GUIContent buttonLabel = new GUIContent();
			if(type == null) {
				buttonLabel.text = "Unassigned";
			}
			else {
				buttonLabel.text = type.PrettyName();
			}
			position = EditorGUI.PrefixLabel(position, label);
			position.width -= 20;
			if(EditorGUI.DropdownButton(position, buttonLabel, FocusType.Keyboard) && Event.current.button == 0) {
				GUI.changed = false;
				if(Event.current.shift || Event.current.control) {
					TypeBuilderWindow.Show(position, targetObject, filter, (types) => {
						onClick(types[0].startType);
					}, new TypeItem[] { new TypeItem(type, filter) });
				}
				else {
					ItemSelector.ShowWindow(targetObject, filter, delegate (MemberData member) {
						onClick(member.startType);
					}).ChangePosition(position.ToScreenRect());
				}
			}
			position.x += position.width;
			position.width = 20;
			if(EditorGUI.DropdownButton(position, GUIContent.none, FocusType.Keyboard) && Event.current.button == 0) {
				GUI.changed = false;
				TypeBuilderWindow.Show(position, targetObject, filter, (types) => {
					onClick(types[0].startType);
				}, new TypeItem[] { new TypeItem(type, filter) });
			}
			if(type != null && type.IsGenericType) {
				EditorGUI.indentLevel++;
				DrawGenericTypeArguments(type, targetObject, onClick);
				EditorGUI.indentLevel--;
			}
		}

		private static void DrawGenericTypeArguments(Type type, object targetObject, Action<Type> onChanged) {
			var typeDefinition = type.GetGenericTypeDefinition();
			var rawGenericArguments = typeDefinition.GetGenericArguments();
			var genericArguments = type.GetGenericArguments();
			for(int x = 0; x < genericArguments.Length; x++) {
				var index = x;
				var arg = genericArguments[index];
				using(new EditorGUILayout.HorizontalScope()) {
					EditorGUILayout.PrefixLabel(rawGenericArguments[index].Name);
					var rect = uNodeGUIUtility.GetRect();
					rect.width -= 20;
					if(EditorGUI.DropdownButton(rect, new GUIContent(arg.PrettyName(), arg.FullName), FocusType.Keyboard)) {
						var filter = new FilterAttribute();
						filter.ToFilterGenericConstraints(rawGenericArguments[index]);
						if(Event.current.shift || Event.current.control) {
							TypeBuilderWindow.Show(rect, targetObject, filter, (types) => {
								genericArguments[index] = types[0].startType;
								var changedType = ReflectionUtils.MakeGenericType(typeDefinition, genericArguments);
								onChanged(changedType);
							}, new TypeItem[] { new TypeItem(genericArguments[index], filter) });
						}
						else {
							ItemSelector.ShowType(targetObject, filter, member => {
								genericArguments[index] = member.startType;
								var changedType = ReflectionUtils.MakeGenericType(typeDefinition, genericArguments);
								onChanged(changedType);
							}).ChangePosition(rect.ToScreenRect());
						}
					}
					rect.x += rect.width;
					rect.width = 20;
					if(EditorGUI.DropdownButton(rect, GUIContent.none, FocusType.Keyboard) && Event.current.button == 0) {
						var filter = new FilterAttribute();
						filter.ToFilterGenericConstraints(rawGenericArguments[index]);
						TypeBuilderWindow.Show(rect, targetObject, filter, (types) => {
							genericArguments[index] = types[0].startType;
							var changedType = ReflectionUtils.MakeGenericType(typeDefinition, genericArguments);
							onChanged(changedType);
						}, new TypeItem[] { new TypeItem(genericArguments[index], filter) });
					}
				}
				if(arg.IsGenericType) {
					EditorGUI.indentLevel++;
					DrawGenericTypeArguments(arg, targetObject, type => {
						genericArguments[index] = type;
						var changedType = ReflectionUtils.MakeGenericType(typeDefinition, genericArguments);
						onChanged(changedType);
					});
					EditorGUI.indentLevel--;
				}
			}
		}

		public static void DrawNullValue(Type instanceType = null, Action onClick = null) {
			Rect position = GetRect();
			DrawNullValue(position, instanceType, onClick);
		}

		public static void DrawNullValue(Rect position, Type instanceType = null, Action onClick = null) {
			if(instanceType != null && onClick != null)
				position.width -= 16;
			uNodeGUI.Label(position, "null", (GUIStyle)"HelpBox");
			if(instanceType != null && onClick != null) {
				position.x += position.width;
				position.width = 16;
				if(EditorGUI.DropdownButton(position, GUIContent.none, FocusType.Keyboard, EditorStyles.miniButton) && Event.current.button == 0) {
					if(onClick != null) {
						onClick();
					}
				}
			}
		}

		public static void DrawNullValue(GUIContent label, Type instanceType = null, Action<object> onCreateInstance = null) {
			Rect position = GetRect();
			DrawNullValue(position, label, instanceType, onCreateInstance);
		}

		public static void DrawNullValue(Rect position, GUIContent label, Type instanceType = null, Action<object> onCreateInstance = null) {
			position = EditorGUI.PrefixLabel(position, label);
			if(instanceType != null && onCreateInstance != null)
				position.width -= 16;
			uNodeGUI.Label(position, "null", (GUIStyle)"HelpBox");
			if(instanceType != null && onCreateInstance != null) {
				position.x += position.width;
				position.width = 16;
				if(EditorGUI.DropdownButton(position, GUIContent.none, FocusType.Keyboard, EditorStyles.miniButton) && Event.current.button == 0 &&
					ReflectionUtils.CanCreateInstance(instanceType)) {
					if(onCreateInstance != null) {
						onCreateInstance(ReflectionUtils.CreateInstance(instanceType));
					}
				}
			}
		}

		public static Rect GetRect(params GUILayoutOption[] options) {
			return GUILayoutUtility.GetRect(EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight, uNodeGUIStyle.labelStyle, options);
		}

		public static Rect GetRect(float width, float height, params GUILayoutOption[] options) {
			return GUILayoutUtility.GetRect(width, height, uNodeGUIStyle.labelStyle, options);
		}

		public static Rect GetRect(GUIStyle style, params GUILayoutOption[] options) {
			return GUILayoutUtility.GetRect(EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight, style, options);
		}

		public static Rect GetRectCustomHeight(float height, params GUILayoutOption[] options) {
			return GUILayoutUtility.GetRect(EditorGUIUtility.labelWidth, height, uNodeGUIStyle.labelStyle, options);
		}

		public static Rect GetRectCustomHeight(float height, GUIStyle style, params GUILayoutOption[] options) {
			return GUILayoutUtility.GetRect(EditorGUIUtility.labelWidth, height, style, options);
		}

		public static Rect GetRect(float heightMultiply, params GUILayoutOption[] options) {
			return GUILayoutUtility.GetRect(EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight * heightMultiply, uNodeGUIStyle.labelStyle, options);
		}

		public static Rect GetRect(GUIStyle style, float heightMultiply, params GUILayoutOption[] options) {
			return GUILayoutUtility.GetRect(EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight * heightMultiply, style, options);
		}

		public static Rect GetRect(GUIContent label, GUIStyle style, params GUILayoutOption[] options) {
			return GUILayoutUtility.GetRect(label, style, options);
		}

		public static bool CanDrawOneLine(Type type) {
			if(type == typeof(int)) {
				return true;
			}
			else if(type == typeof(uint)) {
				return true;
			}
			else if(type == typeof(char)) {
				return true;
			}
			else if(type == typeof(byte)) {
				return true;
			}
			else if(type == typeof(sbyte)) {
				return true;
			}
			else if(type == typeof(float)) {
				return true;
			}
			else if(type == typeof(short)) {
				return true;
			}
			else if(type == typeof(ushort)) {
				return true;
			}
			else if(type == typeof(bool)) {
				return true;
			}
			else if(type == typeof(double)) {
				return true;
			}
			else if(type == typeof(decimal)) {
				return true;
			}
			else if(type == typeof(long)) {
				return true;
			}
			else if(type == typeof(ulong)) {
				return true;
			}
			else if(type == typeof(Color)) {
				return true;
			}
			else if(type == typeof(Color32)) {
				return true;
			}
			else if(type == typeof(Vector2)) {
				return true;
			}
			else if(type == typeof(Vector3)) {
				return true;
			}
			else if(type == typeof(Vector4)) {
				return true;
			}
			else if(type == typeof(Quaternion)) {
				return true;
			}
			else if(type.IsSubclassOf(typeof(Enum))) {
				return true;
			}
			else if(type == typeof(AnimationCurve)) {
				return true;
			}
			else if(type == typeof(string)) {
				return true;
			}
			else if(type == typeof(MemberData)) {
				return true;
			}
			else if(type.IsCastableTo(typeof(UnityEngine.Object))) {
				return true;
			}
			else if(type == typeof(Type)) {
				return true;
			}
			return false;
		}

		public static void ShowField(GUIContent label,
			FieldInfo field,
			object targetField,
			UnityEngine.Object unityObject,
			ObjectTypeAttribute objectType = null,
			uNodeUtility.EditValueSettings settings = null) {
			if(settings == null) {
				settings = new uNodeUtility.EditValueSettings();
			}
			settings.parentValue = targetField;
			settings.unityObject = unityObject;
			settings.attributes = field.GetCustomAttributes(true);
			ShowField(label, field, targetField, objectType, settings);
		}

		public static void ShowField(GUIContent label,
			FieldInfo field,
			object targetField,
			ObjectTypeAttribute objectType = null,
			uNodeUtility.EditValueSettings settings = null) {
			Type type = field.FieldType;
			object fieldValue = field.GetValueOptimized(targetField);
			if(label == null) {
				string ToolTip = "";
				string fieldName = ObjectNames.NicifyVariableName(field.Name);
				if(field.IsDefined(typeof(TooltipAttribute), true)) {
					ToolTip = ((TooltipAttribute)field.GetCustomAttributes(typeof(TooltipAttribute), true)[0]).tooltip;
				}
				label = new GUIContent(fieldName, ToolTip);
			}
			if(settings == null) {
				settings = new uNodeUtility.EditValueSettings();
			}
			EditorGUI.BeginChangeCheck();
			if(type.IsGenericType ||
				type.IsArray ||
				IsSupportedType(type) ||
				type == typeof(Type) ||
				type == typeof(BaseValueData) ||
				type.IsSubclassOf(typeof(BaseValueData)) ||
				type == typeof(ValueData) ||
				type == typeof(object) ||
				FieldControl.FindControl(type, true) != null) {

				var oldValue = fieldValue;
				EditValueLayouted(label, oldValue, type, delegate (object val) {
					uNodeEditorUtility.RegisterUndo(settings.unityObject, field.Name);
					oldValue = val;
					field.SetValueOptimized(targetField, oldValue);
					GUIChanged(settings.unityObject);
				}, new uNodeUtility.EditValueSettings(settings) { parentValue = targetField });
				if(EditorGUI.EndChangeCheck()) {
					uNodeEditorUtility.RegisterUndo(settings.unityObject, field.Name);
					field.SetValueOptimized(targetField, oldValue);
					GUIChanged(settings.unityObject);
				}
			}
			else if(ReflectionUtils.CanCreateInstance(type)) {
				object obj = fieldValue;
				if(obj == null) {
					if(settings.nullable) {
						if(fieldValue != null)
							GUI.changed = true;
						obj = null;
					}
					else {
						obj = ReflectionUtils.CreateInstance(type);
						GUI.changed = true;
					}
				}
				if(obj != null) {
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.LabelField(label);
					if(settings.nullable) {
						if(EditorGUILayout.DropdownButton(GUIContent.none, FocusType.Keyboard, EditorStyles.miniButton, GUILayout.Width(16)) && Event.current.button == 0) {
							obj = null;
							GUI.changed = true;
						}
					}
					EditorGUILayout.EndHorizontal();
					if(obj != null) {
						FieldInfo[] fieldInfo = ReflectionUtils.GetFieldsFromType(type);
						if(fieldInfo != null && fieldInfo.Length > 0) {
							Array.Sort(fieldInfo, (x, y) => {
								if(x.DeclaringType != y.DeclaringType) {
									return string.Compare(x.DeclaringType.IsSubclassOf(y.DeclaringType).ToString(), y.DeclaringType.IsSubclassOf(x.DeclaringType).ToString(), StringComparison.OrdinalIgnoreCase);
								}
								return string.Compare(x.MetadataToken.ToString(), y.MetadataToken.ToString(), StringComparison.OrdinalIgnoreCase);
							});
							EditorGUI.indentLevel++;
							ShowFields(fieldInfo, obj, settings.unityObject);
							EditorGUI.indentLevel--;
						}
					}
					if(EditorGUI.EndChangeCheck()) {
						uNodeEditorUtility.RegisterUndo(settings.unityObject, field.Name);
						field.SetValueOptimized(targetField, obj);
						GUIChanged(settings.unityObject);
					}
				}
				else {
					DrawNullValue(label, type, delegate (object o) {
						uNodeEditorUtility.RegisterUndo(settings.unityObject, "Create Field Instance");
						field.SetValueOptimized(targetField, obj);
						GUIChanged(settings.unityObject);
					});
					EditorGUI.EndChangeCheck();
				}
			}
			else {
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.PrefixLabel(label);
				uNodeGUI.Button(fieldValue == null ? "null" : type.PrettyName());
				EditorGUILayout.EndHorizontal();
				EditorGUI.EndChangeCheck();
			}
		}

		public static void ShowField(string fieldName,
			object parentField,
			UnityEngine.Object unityObject,
			BindingFlags flags,
			uNodeUtility.EditValueSettings setting = null) {
			if(object.ReferenceEquals(parentField, null))
				return;
			FieldInfo field = parentField.GetType().GetField(fieldName, flags);
			ShowField(null, field, parentField, unityObject, null, setting);
		}

		public static void ShowField(string fieldName, object parentField, UnityEngine.Object unityObject = null,
			uNodeUtility.EditValueSettings setting = null) {
			if(object.ReferenceEquals(parentField, null))
				return;
			FieldInfo field = parentField.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic);
			if(field == null) {
				throw new System.Exception("The field name : " + fieldName + " does't exists");
			}
			ShowField(null, field, parentField, unityObject, null, setting);
		}

		public static void ShowField(GUIContent label, string fieldName, object parentField, UnityEngine.Object unityObject = null) {
			if(object.ReferenceEquals(parentField, null))
				return;
			FieldInfo field = parentField.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic);
			if(field == null) {
				throw new System.Exception("The field name : " + fieldName + " does't exists");
			}
			ShowField(label, field, parentField, unityObject, null, null);
		}

		public static void ShowField(GUIContent label, string fieldName, object parentField, object[] attributes, UnityEngine.Object unityObject = null) {
			if(object.ReferenceEquals(parentField, null))
				return;
			FieldInfo field = parentField.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic);
			if(field == null) {
				throw new System.Exception("The field name : " + fieldName + " does't exists");
			}
			ShowField(label, field, parentField, null, new uNodeUtility.EditValueSettings() {
				parentValue = parentField,
				unityObject = unityObject,
				attributes = attributes,
			});
		}

		public static void ShowField(string fieldName, object parentField, BindingFlags flags, UnityEngine.Object unityObject = null,
			uNodeUtility.EditValueSettings setting = null) {
			if(object.ReferenceEquals(parentField, null))
				return;
			FieldInfo field = parentField.GetType().GetField(fieldName, flags);
			if(field == null) {
				throw new System.Exception("The field name : " + fieldName + " does't exists");
			}
			ShowField(null, field, parentField, unityObject, null, setting);
		}

		public static void ShowField(FieldInfo field, object targetField,
			UnityEngine.Object unityObject = null,
			uNodeUtility.EditValueSettings setting = null) {
			ShowField(null, field, targetField, unityObject, null, setting);
		}
		#endregion

		#region EditValue
		public static void EditUnityObject(UnityEngine.Object target) {
			Editor editor = CustomInspector.GetEditor(target);
			if(editor != null) {
				editor.OnInspectorGUI();
			}
		}

		public static void EditValue<T>(Rect position,
			GUIContent label,
			T fieldValue,
			Action<T> onChange,
			uNodeUtility.EditValueSettings settings = null) {
			EditValue(position, label, fieldValue, typeof(T), (obj) => onChange?.Invoke((T)obj), settings);
		}

		public static void EditValue(Rect position,
			GUIContent label,
			object fieldValue,
			Type type,
			Action<object> onChange,
			UnityEngine.Object unityObject,
			object[] fieldAttribute = null) {
			EditValue(position, label, fieldValue, type, onChange, new uNodeUtility.EditValueSettings() {
				attributes = fieldAttribute,
				unityObject = unityObject,
			});
		}

		public static void EditValue(Rect position,
			GUIContent label,
			object fieldValue,
			Type type,
			Action<object> onChange = null,
			uNodeUtility.EditValueSettings settings = null) {
			if(settings == null) {
				settings = new uNodeUtility.EditValueSettings();
			}
			var unityObject = settings.unityObject;
			var fieldAttribute = settings.attributes;
			var control = FieldControl.FindControl(type, false);
			if(control != null) {
				if(string.IsNullOrEmpty(label.tooltip)) {
					label.tooltip = settings.Tooltip;
				}
				control.Draw(position, label, fieldValue, type, (val) => {
					uNodeEditorUtility.RegisterUndo(unityObject, "");
					fieldValue = val;
					if(onChange != null) {
						onChange(fieldValue);
					}
					GUIChanged(unityObject);
				}, settings);
				return;
			}
			EditorGUI.BeginChangeCheck();
			if(type is RuntimeType) {
				EditRuntimeTypeValue(position, label, fieldValue, type as RuntimeType, (val) => {
					uNodeEditorUtility.RegisterUndo(unityObject, "");
					fieldValue = val;
					if(onChange != null) {
						onChange(fieldValue);
					}
					GUIChanged(unityObject);
				}, uNodeEditorUtility.IsSceneObject(unityObject), settings.acceptUnityObject);
			}
			else if(type.IsCastableTo(typeof(UnityEngine.Object))) {
				if(settings.acceptUnityObject) {
					if(fieldValue != null && !(fieldValue is UnityEngine.Object)) {
						fieldValue = null;
					}
					UnityEngine.Object oldValue = fieldValue as UnityEngine.Object;
					oldValue = EditorGUI.ObjectField(position, label, oldValue, type, uNodeEditorUtility.IsSceneObject(unityObject));
					if(EditorGUI.EndChangeCheck()) {
						uNodeEditorUtility.RegisterUndo(unityObject, "");
						fieldValue = oldValue;
						if(onChange != null) {
							onChange(fieldValue);
						}
						GUIChanged(unityObject);
					}
				}
				else {
					position = EditorGUI.PrefixLabel(position, label);
					uNodeGUI.Label(position, "null", EditorStyles.helpBox);
					EditorGUI.EndChangeCheck();
				}
			}
			else if(type == typeof(Type)) {
				var oldValue = fieldValue;
				Type t = oldValue as Type;
				if(oldValue is string) {
					t = TypeSerializer.Deserialize(oldValue as string, false);
				}
				if(EditorGUI.DropdownButton(position, new GUIContent(t != null ?
					t.PrettyName(true) : string.IsNullOrEmpty(oldValue as string) ?
					"null" : "Missing Type", t != null ? t.PrettyName(true) : null), FocusType.Keyboard) && Event.current.button == 0) {
					GUI.changed = false;
					if(Event.current.button == 0) {
						FilterAttribute filter = ReflectionUtils.GetAttribute<FilterAttribute>(fieldAttribute);
						if(filter == null)
							filter = new FilterAttribute();
						filter.OnlyGetType = true;
						filter.UnityReference = false;
						TypeBuilderWindow.Show(position, unityObject, filter, delegate (MemberData[] members) {
							uNodeEditorUtility.RegisterUndo(unityObject, "");
							oldValue = members[0].startType;
							if(onChange != null) {
								onChange(oldValue);
							}
							GUIChanged(unityObject);
						}, t);
					}
					else {
						AutoCompleteWindow.CreateWindow(GUIToScreenRect(position), (items) => {
							var member = CompletionEvaluator.CompletionsToMemberData(items);
							if(member != null) {
								uNodeEditorUtility.RegisterUndo(unityObject, "");
								oldValue = member.startType;
								if(onChange != null) {
									onChange(oldValue);
								}
								GUIChanged(unityObject);
								return true;
							}
							return false;
						}, new CompletionEvaluator.CompletionSetting() {
							validCompletionKind = CompletionKind.Type | CompletionKind.Namespace | CompletionKind.Keyword,
						});
					}
				}
				if(EditorGUI.EndChangeCheck()) {
					uNodeEditorUtility.RegisterUndo(unityObject, "");
					fieldValue = oldValue;
					if(onChange != null) {
						onChange(fieldValue);
					}
					GUIChanged(unityObject);
				}
			}
			else if(type == typeof(object)) {
				position = EditorGUI.PrefixLabel(position, label);
				position.width -= 20;
				if(fieldValue == null) {
					if(settings.acceptUnityObject) {
						EditValue(position, GUIContent.none, fieldValue, typeof(UnityEngine.Object), onChange, settings);
					}
					else {
						uNodeGUI.Label(position, new GUIContent("null", type.PrettyName(true)), EditorStyles.helpBox);
					}
				}
				else if(fieldValue.GetType() == typeof(object)) {
					uNodeGUI.Label(position, new GUIContent("new object()", type.PrettyName(true)), EditorStyles.helpBox);
				}
				else if(fieldValue.GetType().IsCastableTo(typeof(UnityEngine.Object))) {
					EditValue(position, GUIContent.none, fieldValue, typeof(UnityEngine.Object), onChange, settings);
				}
				else {
					EditValue(position, GUIContent.none, fieldValue, fieldValue.GetType(), onChange, settings);
				}
				position.x += position.width;
				position.width = 20;
				if(EditorGUI.DropdownButton(position, GUIContent.none, FocusType.Keyboard, EditorStyles.popup)) {
					GUI.changed = false;
					if(Event.current.button == 0) {
						ItemSelector.ShowWindow(null, new FilterAttribute() { DisplayAbstractType = false, DisplayInterfaceType = false, OnlyGetType = true },
							delegate (MemberData member) {
								Type t = member.startType;
								if(ReflectionUtils.CanCreateInstance(t)) {
									uNodeEditorUtility.RegisterUndo(unityObject, "");
									fieldValue = ReflectionUtils.CreateInstance(t);
									if(onChange != null) {
										onChange(fieldValue);
									}
									GUIChanged(unityObject);
								}
								else if(settings.nullable) {
									uNodeEditorUtility.RegisterUndo(unityObject, "");
									fieldValue = null;
									if(onChange != null) {
										onChange(fieldValue);
									}
									GUIChanged(unityObject);
								}
							}).ChangePosition(position.ToScreenRect());
					}
					else if(Event.current.button == 1) {
						GenericMenu menu = new GenericMenu();
						menu.AddItem(new GUIContent("Make Null"), false, () => {
							uNodeEditorUtility.RegisterUndo(unityObject, "");
							fieldValue = null;
							if(onChange != null) {
								onChange(fieldValue);
							}
							GUIChanged(unityObject);
						});
						var mPos = GUIToScreenRect(position);
						menu.AddItem(new GUIContent("Change Type"), false, () => {
							AutoCompleteWindow.CreateWindow(mPos, (items) => {
								var member = CompletionEvaluator.CompletionsToMemberData(items);
								if(member != null) {
									Type t = member.startType;
									if(ReflectionUtils.CanCreateInstance(t)) {
										uNodeEditorUtility.RegisterUndo(unityObject, "");
										fieldValue = ReflectionUtils.CreateInstance(t);
										if(onChange != null) {
											onChange(fieldValue);
										}
										GUIChanged(unityObject);
									}
									else if(settings.nullable) {
										uNodeEditorUtility.RegisterUndo(unityObject, "");
										fieldValue = null;
										if(onChange != null) {
											onChange(fieldValue);
										}
										GUIChanged(unityObject);
									}
									return true;
								}
								return false;
							}, new CompletionEvaluator.CompletionSetting() {
								validCompletionKind = CompletionKind.Type | CompletionKind.Namespace | CompletionKind.Keyword,
							});
						});
						menu.ShowAsContext();
					}
				}
				EditorGUI.EndChangeCheck();
			}
			else if(type.IsValueType && type != typeof(void) || type.IsClass && !type.IsAbstract ||
			   type.IsGenericType && !type.IsGenericTypeDefinition ||
			   type.IsArray) {
				if((type.IsValueType && type != typeof(void) || !settings.nullable && (type.IsClass && !type.IsAbstract ||
					type.IsGenericType && !type.IsGenericTypeDefinition || type.IsArray)) &&
					(fieldValue == null || !type.IsCastableTo(fieldValue.GetType()))) {

					fieldValue = ReflectionUtils.CreateInstance(type);
					if(onChange != null) {
						onChange(fieldValue);
					}
				}
				position = EditorGUI.PrefixLabel(position, label);
				if(EditorGUI.DropdownButton(position, new GUIContent(fieldValue != null ? type.PrettyName(true) : "null", type.PrettyName(true)), FocusType.Keyboard) && Event.current.button == 0) {
					//Make sure don't mark if value changed.
					GUI.changed = false;
					var w = ActionPopupWindow.ShowWindow(position,
						fieldValue,
						delegate (ref object obj) {
							EditValueLayouted(new GUIContent("Values", type.PrettyName(true)), obj, type, delegate (object val) {
								ActionPopupWindow.GetLast().variable = val;
								if(onChange != null)
									onChange(val);
							}, settings);
						},
						onGUIBottom: delegate (ref object obj) {
							if(GUILayout.Button("Close")) {
								GUI.changed = false;
								ActionPopupWindow.CloseLast();
							}
						}, width: 300, height: 250);
					w.headerName = "Edit Values";
					if(type.IsValueType) {
						//Close Action when editing value type and performing redo or undo to avoid wrong value.
						w.onUndoOrRedo = delegate () {
							w.Close();
						};
					}
				}
				EditorGUI.EndChangeCheck();
			}
			else {
				position = EditorGUI.PrefixLabel(position, label);
				uNodeGUI.Label(position, new GUIContent("unsupported type", type.PrettyName(true)), EditorStyles.helpBox);
				EditorGUI.EndChangeCheck();
			}
		}

		public static void EditValue(Rect position, GUIContent label, string fieldName, object parentField, UnityEngine.Object unityObject = null) {
			if(object.ReferenceEquals(parentField, null))
				return;
			FieldInfo field = parentField.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic);
			if(field == null) {
				throw new System.Exception("The field name : " + fieldName + " does't exists");
			}
			EditValue(position, label, field.GetValueOptimized(parentField), field.FieldType, (obj) => {
				field.SetValueOptimized(parentField, obj);
			}, unityObject, field.GetCustomAttributes(true));
		}

		public static void EditValue(Rect position, GUIContent label, string fieldName, int elementIndex, object parentField, UnityEngine.Object unityObject = null) {
			if(object.ReferenceEquals(parentField, null))
				return;
			FieldInfo field = parentField.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic);
			if(field == null) {
				throw new Exception($"Cannot find field with name:{fieldName} on type: {parentField.GetType().FullName}");
			}
			var arr = field.GetValueOptimized(parentField) as IList;
			EditValue(position, label, arr[elementIndex], field.FieldType.ElementType(), (obj) => {
				field.SetValueOptimized(parentField, obj);
			}, unityObject, field.GetCustomAttributes(true));
		}

		public static void EditValueLayouted(string fieldName, object owner, Action<object> onChange = null, uNodeUtility.EditValueSettings settings = null) {
			var field = GetField(owner, fieldName);
			if(field == null) {
				throw new Exception($"Cannot find field with name:{fieldName} on type: {owner.GetType().FullName}");
			}
			if(settings != null && settings.attributes == null) {
				settings.attributes = field.GetCustomAttributes(true);
			}
			EditValueLayouted(field, owner, onChange, settings);
		}

		public static void EditValueLayouted(FieldInfo field, object owner, Action<object> onChange = null, uNodeUtility.EditValueSettings settings = null) {
			EditValueLayouted(new GUIContent(ObjectNames.NicifyVariableName(field.Name)), field.GetValueOptimized(owner), field.FieldType, (val) => {
				onChange?.Invoke(val);
			}, settings ?? new uNodeUtility.EditValueSettings() {
				attributes = field.GetCustomAttributes(true)
			});
		}

		public static void EditValueLayouted<T>(GUIContent label,
			T fieldValue,
			Action<T> onChange,
			uNodeUtility.EditValueSettings settings = null) {

			EditValueLayouted(
				label,
				fieldValue, typeof(T),
				(val) => {
					onChange((T)val);
				},
				settings
			);
		}

		public static void EditValueLayouted(GUIContent label,
			object fieldValue,
			Type type,
			Action<object> onChange = null,
			uNodeUtility.EditValueSettings settings = null) {
			if(settings == null) {
				settings = new uNodeUtility.EditValueSettings();
			}
			var control = FieldControl.FindControl(type, true);
			if(control != null) {
				control.DrawLayouted(fieldValue, label, type, (val) => {
					uNodeEditorUtility.RegisterUndo(settings.unityObject, "");
					fieldValue = val;
					if(onChange != null) {
						onChange(fieldValue);
					}
					GUIChanged(settings.unityObject);
				}, settings);
				return;
			}
			var fieldAttribute = settings.attributes;
			var unityObject = settings.unityObject;
			EditorGUI.BeginChangeCheck();
			if(type.IsValueType && type != typeof(void)) {
				var oldValue = fieldValue;
				if(oldValue == null) {
					oldValue = ReflectionUtils.CreateInstance(type);
					if(oldValue != null) {
						GUI.changed = true;
					}
				}
				else if(!type.IsCastableTo(oldValue.GetType())) {
					oldValue = ReflectionUtils.CreateInstance(type);
					GUI.changed = true;
				}
				FieldInfo[] fieldInfo = ReflectionUtils.GetFieldsFromType(type);
				if(fieldInfo != null && fieldInfo.Length > 0) {
					Array.Sort(fieldInfo, (x, y) => {
						if(x.DeclaringType != y.DeclaringType) {
							return string.Compare(x.DeclaringType.IsSubclassOf(y.DeclaringType).ToString(), y.DeclaringType.IsSubclassOf(x.DeclaringType).ToString(), StringComparison.OrdinalIgnoreCase);
						}
						return string.Compare(x.MetadataToken.ToString(), y.MetadataToken.ToString(), StringComparison.OrdinalIgnoreCase);
					});
					EditorGUI.LabelField(GetRect(), label);
					EditorGUI.indentLevel++;
					for(int i = 0; i < fieldInfo.Length; i++) {
						int index = i;
						object elementValue = fieldInfo[i].GetValueOptimized(oldValue);
						EditValueLayouted(new GUIContent(fieldInfo[index].Name), elementValue, fieldInfo[index].FieldType, o => {
							elementValue = o;
							fieldInfo[index].SetValueOptimized(oldValue, elementValue);
							GUIChanged(unityObject);
						}, new uNodeUtility.EditValueSettings(settings) {
							attributes = fieldInfo[index].GetCustomAttributes(true)
						});
					}
					EditorGUI.indentLevel--;
				}
				if(EditorGUI.EndChangeCheck()) {
					uNodeEditorUtility.RegisterUndo(unityObject, "");
					fieldValue = oldValue;
					if(onChange != null) {
						onChange(fieldValue);
					}
					GUIChanged(unityObject);
				}
			}
			else if(type is RuntimeType) {
				EditRuntimeTypeValueLayouted(label, fieldValue, type as RuntimeType, (val) => {
					uNodeEditorUtility.RegisterUndo(unityObject, "");
					fieldValue = val;
					if(onChange != null) {
						onChange(fieldValue);
					}
					GUIChanged(unityObject);
				}, uNodeEditorUtility.IsSceneObject(unityObject), acceptUnityObject: settings.acceptUnityObject);
			}
			else if((type == typeof(UnityEngine.Object) || type.IsSubclassOf(typeof(UnityEngine.Object)))) {
				if(fieldValue != null && !(fieldValue is UnityEngine.Object)) {
					fieldValue = null;
				}
				if(settings.acceptUnityObject) {
					UnityEngine.Object oldValue = fieldValue as UnityEngine.Object;
					oldValue = EditorGUI.ObjectField(GetRect(), label, oldValue, type, uNodeEditorUtility.IsSceneObject(unityObject));
					if(EditorGUI.EndChangeCheck()) {
						uNodeEditorUtility.RegisterUndo(unityObject, "");
						fieldValue = oldValue;
						if(onChange != null) {
							onChange(fieldValue);
						}
						GUIChanged(unityObject);
					}
				}
				else {
					Rect rect = GetRect();
					rect = EditorGUI.PrefixLabel(rect, label);
					if(EditorGUI.DropdownButton(rect, new GUIContent("null"), FocusType.Keyboard, EditorStyles.miniButton)) {

					}
					EditorGUI.EndChangeCheck();
				}
			}
			else if(type == typeof(Type)) {
				var oldValue = fieldValue;
				Type t = oldValue as Type;
				if(oldValue is string) {
					t = TypeSerializer.Deserialize(oldValue as string, false);
				}
				Rect rect = GetRect();
				rect = EditorGUI.PrefixLabel(rect, label);
				if(EditorGUI.DropdownButton(rect, new GUIContent(t != null ?
					t.PrettyName(true) : string.IsNullOrEmpty(oldValue as string) ?
					"null" : "Missing Type", t != null ? t.PrettyName(true) : null), FocusType.Keyboard) && Event.current.button == 0) {
					GUI.changed = false;
					if(Event.current.button == 0) {
						FilterAttribute filter = ReflectionUtils.GetAttribute<FilterAttribute>(fieldAttribute);
						if(filter == null)
							filter = new FilterAttribute();
						filter.OnlyGetType = true;
						filter.UnityReference = false;
						TypeBuilderWindow.Show(rect, unityObject, filter, delegate (MemberData[] members) {
							uNodeEditorUtility.RegisterUndo(unityObject, "");
							oldValue = members[0].startType;
							fieldValue = oldValue;
							if(onChange != null) {
								onChange(fieldValue);
							}
							GUIChanged(unityObject);
						}, t);
					}
					else {
						AutoCompleteWindow.CreateWindow(GUIToScreenRect(rect), (items) => {
							var member = CompletionEvaluator.CompletionsToMemberData(items);
							if(member != null) {
								uNodeEditorUtility.RegisterUndo(unityObject, "");
								oldValue = member.startType;
								fieldValue = oldValue;
								if(onChange != null) {
									onChange(fieldValue);
								}
								GUIChanged(unityObject);
								return true;
							}
							return false;
						}, new CompletionEvaluator.CompletionSetting() {
							validCompletionKind = CompletionKind.Type | CompletionKind.Namespace | CompletionKind.Keyword,
						});
					}
				}
				EditorGUI.EndChangeCheck();
			}
			else if(type.IsArray) {
				Type elementType = type.GetElementType();
				if(!IsSupportedType(elementType) && !elementType.IsClass) {
					EditorGUI.EndChangeCheck();
					return;
				}
				Array array = fieldValue as Array;
				if(array == null) {
					if(settings.nullable) {
						if(fieldValue != null)
							GUI.changed = true;
						array = null;
					}
					else {
						array = Array.CreateInstance(type.GetElementType(), 0);
						GUI.changed = true;
					}
				}
				if(array != null) {
					Rect position = GetRect();
					if(settings.nullable)
						position.width -= 16;
					int num = EditorGUI.IntField(position, label, array.Length);
					if(settings.nullable) {
						position.x += position.width;
						position.width = 16;
						if(EditorGUI.DropdownButton(position, GUIContent.none, FocusType.Keyboard, EditorStyles.miniButton) && Event.current.button == 0) {
							array = null;
							GUI.changed = true;
						}
					}
					Array newArray = array;
					if(newArray != null) {
						if(num != array.Length) {
							newArray = uNodeUtility.ResizeArray(array, type.GetElementType(), num);
						}
						if(newArray.Length > 0) {
							Event currentEvent = Event.current;
							EditorGUI.indentLevel++;
							for(int i = 0; i < newArray.Length; i++) {
								var elementToEdit = newArray.GetValue(i);
								if(IsSupportedType(elementType) || elementType.IsClass && !elementType.IsAbstract ||
									elementType.IsArray || elementType.IsGenericType) {
									int a = i;
									EditValueLayouted(new GUIContent("Element " + i), elementToEdit, elementType, delegate (object val) {
										uNodeEditorUtility.RegisterUndo(unityObject, "");
										elementToEdit = val;
										newArray.SetValue(elementToEdit, a);
										if(onChange != null)
											onChange(newArray);
										GUIChanged(unityObject);
									}, settings);
								}
							}
							EditorGUI.indentLevel--;
						}
					}
					if(EditorGUI.EndChangeCheck()) {
						uNodeEditorUtility.RegisterUndo(unityObject, "");
						fieldValue = newArray;
						if(onChange != null) {
							onChange(fieldValue);
						}
						GUIChanged(unityObject);
					}
				}
				else {
					DrawNullValue(label, type, delegate (object o) {
						uNodeEditorUtility.RegisterUndo(unityObject, "Create Field Instance");
						if(onChange != null) {
							onChange(o);
						}
						GUIChanged(unityObject);
					});
					EditorGUI.EndChangeCheck();
				}
			}
			else if(type.IsGenericType && type.GetGenericArguments().Length == 1 && type.IsCastableTo(typeof(IList))) {
				Type elementType = type.GetGenericArguments()[0];
				if(!IsSupportedType(elementType) && !elementType.IsClass) {
					EditorGUI.EndChangeCheck();
					return;
				}
				IList array = fieldValue as IList;
				if(array == null) {
					if(settings.nullable) {
						if(fieldValue != null)
							GUI.changed = true;
						array = null;
					}
					else {
						array = ReflectionUtils.CreateInstance(type) as IList;
						GUI.changed = true;
					}
				}
				if(array != null) {
					Rect position = GetRect();
					if(settings.nullable)
						position.width -= 16;
					int num = EditorGUI.IntField(position, label, array.Count);
					if(settings.nullable) {
						position.x += position.width;
						position.width = 16;
						if(EditorGUI.DropdownButton(position, GUIContent.none, FocusType.Keyboard, EditorStyles.miniButton) && Event.current.button == 0) {
							array = null;
							GUI.changed = true;
						}
					}
					if(array != null) {
						if(num != array.Count) {
							uNodeEditorUtility.RegisterUndo(unityObject);
							uNodeUtility.ResizeList(array, elementType, num, true);
						}
						if(array.Count > 0) {
							EditorGUI.indentLevel++;
							for(int i = 0; i < array.Count; i++) {
								var elementToEdit = array[i];
								if(IsSupportedType(elementType) || elementType.IsClass && !elementType.IsAbstract ||
									elementType.IsArray || elementType.IsGenericType) {
									int a = i;
									EditValueLayouted(new GUIContent("Element " + i), elementToEdit, elementType, delegate (object val) {
										uNodeEditorUtility.RegisterUndo(unityObject, "");
										elementToEdit = val;
										array[a] = elementToEdit;
										if(onChange != null)
											onChange(array);
										GUIChanged(unityObject);
									}, settings);
								}
							}
							EditorGUI.indentLevel--;
						}
					}
					if(EditorGUI.EndChangeCheck()) {
						uNodeEditorUtility.RegisterUndo(unityObject, "");
						fieldValue = array;
						if(onChange != null) {
							onChange(fieldValue);
						}
						GUIChanged(unityObject);
					}
				}
				else {
					DrawNullValue(label, type, delegate (object o) {
						uNodeEditorUtility.RegisterUndo(unityObject, "Create Field Instance");
						if(onChange != null) {
							onChange(o);
						}
						GUIChanged(unityObject);
					});
					EditorGUI.EndChangeCheck();
				}
			}
			else if(type.IsGenericType && type.IsCastableTo(typeof(IDictionary))) {
				Type keyType = type.GetGenericArguments()[0];
				Type valType = type.GetGenericArguments()[1];
				if(!IsSupportedType(keyType) && !keyType.IsClass || !IsSupportedType(valType) && !valType.IsClass || valType.IsAbstract) {
					EditorGUI.EndChangeCheck();
					return;
				}
				IDictionary map = fieldValue as IDictionary;
				if(map == null) {
					if(settings.nullable) {
						if(fieldValue != null)
							GUI.changed = true;
						map = null;
					}
					else {
						map = ReflectionUtils.CreateInstance(type) as IDictionary;
						GUI.changed = true;
					}
				}
				if(map != null) {
					Rect position = GetRect();
					if(settings.nullable)
						position.width -= 16;
					position = EditorGUI.PrefixLabel(position, label);
					if(EditorGUI.DropdownButton(position, new GUIContent("add new (" + keyType.PrettyName() + ", " + valType.PrettyName() + ")"), FocusType.Keyboard) && Event.current.button == 0) {
						GUI.changed = false;
						ActionPopupWindow.ShowWindow(position,
							new object[] { ReflectionUtils.CreateInstance(keyType), ReflectionUtils.CreateInstance(valType), map },
							delegate (ref object val) {
								object[] o = val as object[];
								EditValueLayouted(new GUIContent("Key"), o[0], keyType, delegate (object v) {
									o[0] = v;
								}, new uNodeUtility.EditValueSettings(settings) { nullable = false });
								EditValueLayouted(new GUIContent("Value"), o[1], valType, delegate (object v) {
									o[1] = v;
								}, settings);
								if(GUILayout.Button(new GUIContent("Add"))) {
									if(!map.Contains(o[0])) {
										uNodeEditorUtility.RegisterUndo(unityObject, "" + "Add Dictonary Value");
										(o[2] as IDictionary).Add(o[0], o[1]);
										fieldValue = o[2];
										if(onChange != null) {
											onChange(fieldValue);
										}
										ActionPopupWindow.CloseLast();
										GUIChanged(unityObject);
									}
								}
							}).headerName = "Add New Dictonary Value";
					}
					if(settings.nullable) {
						position.x += position.width;
						position.width = 16;
						if(EditorGUI.DropdownButton(position, GUIContent.none, FocusType.Keyboard, EditorStyles.miniButton) && Event.current.button == 0) {
							map = null;
							GUI.changed = true;
						}
					}
					IDictionary newMap = map;
					if(newMap != null) {
						if(newMap.Count > 0) {
							List<object> keys = uNodeEditorUtility.GetKeys(newMap);
							List<object> values = uNodeEditorUtility.GetValues(newMap);
							if(keys.Count == values.Count) {
								EditorGUI.indentLevel++;
								for(int i = 0; i < keys.Count; i++) {
									int index = i;
									Rect rect = GetRect();
									EditorGUI.LabelField(rect, new GUIContent("Element " + index));
									if(Event.current.button == 1 && rect.Contains(Event.current.mousePosition)) {
										GenericMenu menu = new GenericMenu();
										menu.AddItem(new GUIContent("Remove"), false, (obj) => {
											int index = (int)obj;
											newMap.Remove(keys[index]);
										}, index);
										menu.AddSeparator("");
										menu.AddItem(new GUIContent("Move To Top"), false, (obj) => {
											int index = (int)obj;
											if(index != 0) {
												uNodeEditorUtility.RegisterUndo(unityObject, "");
												uNodeEditorUtility.ListMoveToTop(keys, (int)obj);
												uNodeEditorUtility.ListMoveToTop(values, (int)obj);
												newMap = ReflectionUtils.CreateInstance(type) as IDictionary;
												for(int x = 0; x < keys.Count; x++) {
													newMap.Add(keys[x], values[x]);
												}
												fieldValue = newMap;
												if(onChange != null) {
													onChange(fieldValue);
												}
												GUIChanged(unityObject);
											}
										}, index);
										menu.AddItem(new GUIContent("Move Up"), false, (obj) => {
											int index = (int)obj;
											if(index != 0) {
												uNodeEditorUtility.RegisterUndo(unityObject, "");
												uNodeEditorUtility.ListMoveUp(keys, (int)obj);
												uNodeEditorUtility.ListMoveUp(values, (int)obj);
												newMap = ReflectionUtils.CreateInstance(type) as IDictionary;
												for(int x = 0; x < keys.Count; x++) {
													newMap.Add(keys[x], values[x]);
												}
												fieldValue = newMap;
												if(onChange != null) {
													onChange(fieldValue);
												}
												GUIChanged(unityObject);
											}
										}, index);
										menu.AddItem(new GUIContent("Move Down"), false, (obj) => {
											int index = (int)obj;
											if(index != keys.Count - 1) {
												uNodeEditorUtility.RegisterUndo(unityObject, "");
												uNodeEditorUtility.ListMoveDown(keys, (int)obj);
												uNodeEditorUtility.ListMoveDown(values, (int)obj);
												newMap = ReflectionUtils.CreateInstance(type) as IDictionary;
												for(int x = 0; x < keys.Count; x++) {
													newMap.Add(keys[x], values[x]);
												}
												fieldValue = newMap;
												if(onChange != null) {
													onChange(fieldValue);
												}
												GUIChanged(unityObject);
											}
										}, index);
										menu.AddItem(new GUIContent("Move To Bottom"), false, (obj) => {
											int index = (int)obj;
											if(index != keys.Count - 1) {
												uNodeEditorUtility.RegisterUndo(unityObject, "");
												uNodeEditorUtility.ListMoveToBottom(keys, (int)obj);
												uNodeEditorUtility.ListMoveToBottom(values, (int)obj);
												newMap = ReflectionUtils.CreateInstance(type) as IDictionary;
												for(int x = 0; x < keys.Count; x++) {
													newMap.Add(keys[x], values[x]);
												}
												fieldValue = newMap;
												if(onChange != null) {
													onChange(fieldValue);
												}
												GUIChanged(unityObject);
											}
										}, index);
										menu.ShowAsContext();
									}
									EditorGUI.indentLevel++;
									EditValueLayouted(new GUIContent("Key"), keys[index], keyType, delegate (object val) {
										if(!newMap.Contains(val)) {
											uNodeEditorUtility.RegisterUndo(unityObject, "");
											keys[index] = val;
											newMap = ReflectionUtils.CreateInstance(type) as IDictionary;
											for(int x = 0; x < keys.Count; x++) {
												newMap.Add(keys[x], values[x]);
											}
											fieldValue = newMap;
											if(onChange != null) {
												onChange(fieldValue);
											}
											GUIChanged(unityObject);
										}
									}, new uNodeUtility.EditValueSettings(settings) { nullable = false });
									EditValueLayouted(new GUIContent("Value"), values[index], valType, delegate (object val) {
										uNodeEditorUtility.RegisterUndo(unityObject, "");
										values[index] = val;
										newMap = ReflectionUtils.CreateInstance(type) as IDictionary;
										for(int x = 0; x < values.Count; x++) {
											newMap.Add(keys[x], values[x]);
										}
										fieldValue = newMap;
										if(onChange != null) {
											onChange(fieldValue);
										}
										GUIChanged(unityObject);
									}, settings);
									EditorGUI.indentLevel--;
								}
								EditorGUI.indentLevel--;
							}
						}
					}
					if(EditorGUI.EndChangeCheck()) {
						uNodeEditorUtility.RegisterUndo(unityObject, "");
						fieldValue = newMap;
						if(onChange != null) {
							onChange(fieldValue);
						}
						GUIChanged(unityObject);
					}
				}
				else {
					DrawNullValue(label, type, delegate (object o) {
						uNodeEditorUtility.RegisterUndo(unityObject, "Create Field Instance");
						if(onChange != null) {
							onChange(o);
						}
						GUIChanged(unityObject);
					});
					EditorGUI.EndChangeCheck();
				}
			}
			else if(type.IsGenericType && type.GetGenericTypeDefinition().IsCastableTo(typeof(HashSet<>))) {
				Type keyType = type.GetGenericArguments()[0];
				if(!IsSupportedType(keyType) && !keyType.IsClass) {
					EditorGUI.EndChangeCheck();
					return;
				}
				IList map = ReflectionUtils.CreateInstance(typeof(List<>).MakeGenericType(keyType)) as IList;
				if(fieldValue == null) {
					if(settings.nullable) {
						if(fieldValue != null)
							GUI.changed = true;
						map = null;
					}
					else {
						GUI.changed = true;
					}
				}
				else {
					foreach(var val in fieldValue as IEnumerable) {
						map.Add(val);
					}
				}
				if(map != null) {
					Rect position = GetRect();
					if(settings.nullable)
						position.width -= 16;
					position = EditorGUI.PrefixLabel(position, label);
					if(EditorGUI.DropdownButton(position, new GUIContent("add new (" + keyType.PrettyName() + ")"), FocusType.Keyboard) && Event.current.button == 0) {
						GUI.changed = false;
						ActionPopupWindow.ShowWindow(position, new object[] { ReflectionUtils.CreateInstance(keyType), map, type },
							delegate (ref object val) {
								object[] o = val as object[];
								EditValueLayouted(new GUIContent("Value"), o[0], keyType, delegate (object v) {
									o[0] = v;
								}, new uNodeUtility.EditValueSettings(settings) { nullable = false });
								if(GUILayout.Button(new GUIContent("Add"))) {
									if(!map.Contains(o[0])) {
										uNodeEditorUtility.RegisterUndo(unityObject, "" + "Add Value");
										(o[1] as IList).Add(o[0]);
										fieldValue = ReflectionUtils.CreateInstance(o[2] as Type, o[1]);
										if(onChange != null) {
											onChange(fieldValue);
										}
										ActionPopupWindow.CloseLast();
										GUIChanged(unityObject);
									}
								}
							}).headerName = "Add New Collection Value";
					}
					if(settings.nullable) {
						position.x += position.width;
						position.width = 16;
						if(EditorGUI.DropdownButton(position, GUIContent.none, FocusType.Keyboard, EditorStyles.miniButton) && Event.current.button == 0) {
							map = null;
							GUI.changed = true;
						}
					}

					var newMap = map;
					if(newMap != null) {
						if(newMap.Count > 0) {
							EditorGUI.indentLevel++;
							for(int i = 0; i < newMap.Count; i++) {
								Rect rect = GetRect();
								EditorGUI.PrefixLabel(rect, new GUIContent("Element " + i));
								EditorGUI.indentLevel++;
								EditValueLayouted(GUIContent.none, newMap[i], keyType, delegate (object val) {
									if(!newMap.Contains(val)) {
										uNodeEditorUtility.RegisterUndo(unityObject, "");
										newMap[i] = val;
										fieldValue = ReflectionUtils.CreateInstance(type, newMap);
										if(onChange != null) {
											onChange(fieldValue);
										}
										GUIChanged(unityObject);
									}
								}, new uNodeUtility.EditValueSettings(settings) { nullable = false });
								EditorGUI.indentLevel--;
								if(Event.current.button == 1 && rect.Contains(Event.current.mousePosition)) {
									GenericMenu menu = new GenericMenu();
									menu.AddItem(new GUIContent("Remove"), false, (obj) => {
										int index = (int)obj;
										newMap.Remove(newMap[index]);
									}, i);
									menu.AddSeparator("");
									menu.AddItem(new GUIContent("Move To Top"), false, (obj) => {
										int index = (int)obj;
										if(index != 0) {
											uNodeEditorUtility.RegisterUndo(unityObject, "");
											uNodeEditorUtility.ListMoveToTop(newMap, (int)obj);
											fieldValue = ReflectionUtils.CreateInstance(type, newMap);
											if(onChange != null) {
												onChange(fieldValue);
											}
											GUIChanged(unityObject);
										}
									}, i);
									menu.AddItem(new GUIContent("Move Up"), false, (obj) => {
										int index = (int)obj;
										if(index != 0) {
											uNodeEditorUtility.RegisterUndo(unityObject, "");
											uNodeEditorUtility.ListMoveUp(newMap, (int)obj);
											fieldValue = ReflectionUtils.CreateInstance(type, newMap);
											if(onChange != null) {
												onChange(fieldValue);
											}
											GUIChanged(unityObject);
										}
									}, i);
									menu.AddItem(new GUIContent("Move Down"), false, (obj) => {
										int index = (int)obj;
										if(index != newMap.Count - 1) {
											uNodeEditorUtility.RegisterUndo(unityObject, "");
											uNodeEditorUtility.ListMoveDown(newMap, (int)obj);
											fieldValue = ReflectionUtils.CreateInstance(type, newMap);
											if(onChange != null) {
												onChange(fieldValue);
											}
											GUIChanged(unityObject);
										}
									}, i);
									menu.AddItem(new GUIContent("Move To Bottom"), false, (obj) => {
										int index = (int)obj;
										if(index != newMap.Count - 1) {
											uNodeEditorUtility.RegisterUndo(unityObject, "");
											uNodeEditorUtility.ListMoveToBottom(newMap, (int)obj);
											fieldValue = ReflectionUtils.CreateInstance(type, newMap);
											if(onChange != null) {
												onChange(fieldValue);
											}
											GUIChanged(unityObject);
										}
									}, i);
									menu.ShowAsContext();
								}
							}
							EditorGUI.indentLevel--;
						}
					}
					if(EditorGUI.EndChangeCheck()) {
						uNodeEditorUtility.RegisterUndo(unityObject, "");
						fieldValue = ReflectionUtils.CreateInstance(type, newMap);
						if(onChange != null) {
							onChange(fieldValue);
						}
						GUIChanged(unityObject);
					}
				}
				else {
					DrawNullValue(label, type, delegate (object o) {
						uNodeEditorUtility.RegisterUndo(unityObject, "Create Field Instance");
						if(onChange != null) {
							onChange(o);
						}
						GUIChanged(unityObject);
					});
					EditorGUI.EndChangeCheck();
				}
			}
			else if(type == typeof(object)) {
				Rect position = GetRect();
				position = EditorGUI.PrefixLabel(position, label);
				if(fieldValue == null) {
					position.width -= 20;
					EditorGUI.LabelField(position, new GUIContent("null", type.PrettyName(true)), EditorStyles.helpBox);
					position.x += position.width;
					position.width = 20;
				}
				else if(fieldValue.GetType() == typeof(object)) {
					position.width -= 20;
					EditorGUI.LabelField(position, new GUIContent("new object()", type.PrettyName(true)), EditorStyles.helpBox);
					position.x += position.width;
					position.width = 20;
				}
				else if(fieldValue.GetType().IsCastableTo(typeof(UnityEngine.Object))) {
					position.width -= 20;
					EditValue(position, GUIContent.none, fieldValue, typeof(UnityEngine.Object), onChange, settings);
					position.x += position.width;
					position.width = 20;
				}
				else if(CanDrawOneLine(fieldValue.GetType())) {
					position.width -= 20;
					EditValue(position, GUIContent.none, fieldValue, fieldValue.GetType(), onChange, settings);
					position.x += position.width;
					position.width = 20;
				}
				else {
					EditorGUI.indentLevel++;
					EditValueLayouted(new GUIContent("Value"), fieldValue, fieldValue.GetType(), onChange, settings);
					EditorGUI.indentLevel--;
				}
				if(EditorGUI.DropdownButton(position, GUIContent.none, FocusType.Keyboard, EditorStyles.popup)) {
					GUI.changed = false;
					ItemSelector.ShowWindow(unityObject, FilterAttribute.DefaultTypeFilter, delegate (MemberData member) {
						Type t = member.startType;
						if(ReflectionUtils.CanCreateInstance(t)) {
							uNodeEditorUtility.RegisterUndo(unityObject, "");
							fieldValue = ReflectionUtils.CreateInstance(t);
							if(onChange != null) {
								onChange(fieldValue);
							}
							GUIChanged(unityObject);
						}
					}).ChangePosition(position.ToScreenRect());
				}
				EditorGUI.EndChangeCheck();
			}
			else if(type.IsClass && !type.IsAbstract) {
				object obj = fieldValue;
				if(obj == null && !settings.nullable) {
					obj = ReflectionUtils.CreateInstance(type);
					fieldValue = obj;
				}
				FieldInfo[] fieldInfo = ReflectionUtils.GetFieldsFromType(type);
				if(fieldInfo != null && fieldInfo.Length > 0) {
					Array.Sort(fieldInfo, (x, y) => {
						if(x.DeclaringType != y.DeclaringType) {
							return string.Compare(x.DeclaringType.IsSubclassOf(y.DeclaringType).ToString(), y.DeclaringType.IsSubclassOf(x.DeclaringType).ToString(), StringComparison.OrdinalIgnoreCase);
						}
						return string.Compare(x.MetadataToken.ToString(), y.MetadataToken.ToString(), StringComparison.OrdinalIgnoreCase);
					});
					if(label != GUIContent.none) {
						var pos = EditorGUI.PrefixLabel(GetRect(), label);
						if(uNodeGUI.Button(pos, obj == null ? "null" : "new " + type.PrettyName() + "()", EditorStyles.miniButton)) {
							GUI.changed = true;
							if(obj != null) {
								if(settings.nullable) {
									obj = null;
								}
							}
							else {
								obj = ReflectionUtils.CreateInstance(type);
							}
						}
						if(obj != null) {
							EditorGUI.indentLevel++;
							ShowFields(fieldInfo, obj, unityObject);
							EditorGUI.indentLevel--;
						}
					}
					else {
						if(obj != null) {
							ShowFields(fieldInfo, obj, unityObject);
						}
					}
				}
				else {
					if(label != GUIContent.none) {
						var pos = EditorGUI.PrefixLabel(GetRect(), label);
						if(uNodeGUI.Button(pos, obj == null ? "null" : "new " + type.PrettyName() + "()", EditorStyles.miniButton)) {
							GUI.changed = true;
							if(obj != null) {
								if(settings.nullable) {
									obj = null;
								}
							}
							else {
								obj = ReflectionUtils.CreateInstance(type);
							}
						}
					}
				}
				if(EditorGUI.EndChangeCheck()) {
					fieldValue = obj;
					if(onChange != null) {
						onChange(fieldValue);
					}
				}
			}
			else {
				EditorGUI.EndChangeCheck();
			}
		}
		#endregion

		/// <summary>
		/// Show a search bar GUI.
		/// </summary>
		/// <param name="searchString"></param>
		/// <param name="label"></param>
		/// <param name="controlName"></param>
		/// <param name="options"></param>
		/// <returns></returns>
		public static string DrawSearchBar(string searchString, GUIContent label, string controlName = null, params GUILayoutOption[] options) {
			EditorGUILayout.BeginHorizontal(GUI.skin.FindStyle("Toolbar"));
			if(controlName != null) {
				GUI.SetNextControlName(controlName);
			}
			searchString = EditorGUILayout.TextField(label, searchString, GUI.skin.FindStyle("ToolbarSeachTextField"), options);
			if(GUILayout.Button("", GUI.skin.FindStyle("ToolbarSeachCancelButton"))) {
				searchString = "";
				GUI.FocusControl(null);
			}
			EditorGUILayout.EndHorizontal();
			return searchString;
		}

		private static Rect lastClickedRect;
		public static bool DragGUIButton(Rect rect, GUIContent label, GUIStyle style, Action onDrag, Action onMouseOver = null) {
			if(rect.Contains(Event.current.mousePosition) && Event.current.button == 0) {
				if(onMouseOver != null) {
					onMouseOver();
				}
				if(Event.current.type == EventType.MouseDown) {
					lastClickedRect = rect;
				}
				if(Event.current.type == EventType.MouseUp) {
					lastClickedRect = Rect.zero;
				}
				if(lastClickedRect == rect && Event.current.type == EventType.MouseDrag) {
					lastClickedRect = Rect.zero;
					if(onDrag != null) {
						onDrag();
					}
				}
			}
			return GUI.Button(rect, label, style);
		}

		public static Rect GUIToScreenRect(Rect rect) {
			Vector2 vector = GUIUtility.GUIToScreenPoint(new Vector2(rect.x, rect.y));
			rect.x = vector.x;
			rect.y = vector.y;
			return rect;
		}

		public static Vector2 GUIToScreenPoint(Vector2 position) {
			return GUIUtility.GUIToScreenPoint(new Vector2(position.x, position.y));
		}

		public static void EditVariableValue(VariableData variable, UnityEngine.Object unityObject, bool drawDecorator = true) {
			var attributes = variable.GetAttributes();
			if(drawDecorator) {
				FieldDecorator.DrawDecorators(attributes);
			}
			string varName = ObjectNames.NicifyVariableName(variable.name);
			EditSerializedValue(variable.serializedValue, new GUIContent(varName), variable.type, unityObject, attributes);
		}

		public static void EditSerializedValue(SerializedValue serializedValue, GUIContent label, Type type = null, UnityEngine.Object unityObject = null, object[] attributes = null) {
			if(type == null) {
				type = serializedValue.type;
				if(type == null) {
					type = typeof(object);
				}
			}
			if(type != null) {
				if(serializedValue.value != null && !type.IsInstanceOfType(serializedValue.value)) {
					serializedValue.ChangeValue(null);
				}
				if(type is RuntimeType) {
					EditRuntimeTypeValueLayouted(label, serializedValue.value, type as RuntimeType, (val) => {
						uNodeEditorUtility.RegisterUndo(unityObject, "");
						serializedValue.ChangeValue(val);
					}, uNodeEditorUtility.IsSceneObject(unityObject), unityObject);
				}
				else if(type.IsArray) {
					if(IsSupportedType(type.GetElementType()) || type.GetElementType().IsClass) {
						EditValueLayouted(label, serializedValue.value, type, (val) => {
							serializedValue.ChangeValue(val);
						}, settings: new uNodeUtility.EditValueSettings() {
							unityObject = unityObject,
							attributes = attributes,
							drawDecorator = false,
							nullable = true,
						});
					}
				}
				else if(type.IsGenericType) {
					EditValueLayouted(label, serializedValue.value, type, (val) => {
						serializedValue.ChangeValue(val);
					}, settings: new uNodeUtility.EditValueSettings() {
						unityObject = unityObject,
						attributes = attributes,
						drawDecorator = false,
						nullable = true,
					});
				}
				else if(IsSupportedType(type) || type.IsClass) {
					if(type == typeof(object) && !object.ReferenceEquals(serializedValue.value, null)) {
						type = serializedValue.value.GetType();
					}
					EditValueLayouted(label, serializedValue.value, type, (val) => {
						serializedValue.ChangeValue(val);
					}, settings: new uNodeUtility.EditValueSettings() {
						unityObject = unityObject,
						attributes = attributes,
						drawDecorator = false,
						nullable = true,
					});
				}
			}
			else {
				var position = EditorGUI.PrefixLabel(GetRect(), label);
				EditorGUI.HelpBox(position, "Type not found", MessageType.Error);
			}
		}

		#region SerializedPropertyUtility
		public static void ShowChildArrayProperty(int arrayIndex, SerializedProperty property, bool onlyVisible = true, int childCount = 999) {
			if(property.arraySize > arrayIndex) {
				SerializedProperty MyListRef = property.GetArrayElementAtIndex(arrayIndex);
				DisplayChildProperty(MyListRef, onlyVisible, childCount);
			}
		}

		public static void DisplayChildProperty(SerializedProperty property, bool onlyVisible = true, int childCount = 999) {
			SerializedProperty prop = property.Copy();
			for(int k = 0; k < childCount; k++) {
				if(k == 0) {
					if(onlyVisible) {
						prop.NextVisible(true);
					}
					else {
						prop.Next(true);
					}
					if(property.FindPropertyRelative(prop.name) == null) {
						break;
					}
				}
				else {
					if(onlyVisible) {
						prop.NextVisible(false);
					}
					else {
						prop.Next(false);
					}
					if(property.FindPropertyRelative(prop.name) == null) {
						break;
					}
				}
				EditorGUILayout.PropertyField(prop, true);
			}
		}
		#endregion
	}
}