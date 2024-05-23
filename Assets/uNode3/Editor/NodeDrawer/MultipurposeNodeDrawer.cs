using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;

namespace MaxyGames.UNode.Editors.Drawer {
    public class MultipurposeNodeDrawer : NodeDrawer<MultipurposeNode> {
		public override void DrawLayouted(DrawerOption option) {
			DrawInputs(option);
			DrawOutputs(option);
			DrawErrors(option);
		}

		protected override void DrawInputs(DrawerOption option) {
			DrawInputs(option, GetNode(option));
		}

		protected void DrawInputs(DrawerOption option, MultipurposeNode node, bool showAddButton = true, FilterAttribute filter = null, Action customChangeAction = null) {
			var member = node.member;
			if(member.datas?.Length > 0) {
				foreach(var data in member.datas) {
					if(data.parameters.Any(p => p.refKind == RefKind.Out)) {
						EditorGUI.BeginChangeCheck();
						UInspector.Draw(option.property[nameof(node.useOutputParameters)]);
						if(EditorGUI.EndChangeCheck()) {
							uNodeGUIUtility.GUIChanged(node, UIChangeType.Average);
						}
						break;
					}
				}
				if(member.target.targetType == MemberData.TargetType.Constructor) {
					var type = member.target.type;
					var initializers = member.initializers;
					uNodeGUI.DrawCustomList(initializers, "Initialiers",
							drawElement: (position, index, value) => {
								EditorGUI.LabelField(position, value.name);
							},
							add: _ => {
								bool hasAddMenu = false;
								GenericMenu menu = new GenericMenu();
								if(type.IsArray || type.IsCastableTo(typeof(IList))) {
									uNodeEditorUtility.RegisterUndo(option.unityObject, "Add Field");
									initializers.Add(new MultipurposeMember.InitializerData() {
										name = "Element",
										type = type.ElementType(),
									});
									for(int i = 0; i < initializers.Count; i++) {
										initializers[i].name = "Element" + i;
									}
									uNodeGUIUtility.GUIChanged(node, UIChangeType.Average);
								}
								else if(type.IsCastableTo(typeof(IDictionary))) {
									uNodeEditorUtility.RegisterUndo(option.unityObject, "Add Field");
									var method = type.GetMethod("Add");
									var parameters = method.GetParameters();
									if(parameters.Length > 1) {
										var init = new MultipurposeMember.InitializerData() {
											name = "Element",
											type = SerializedType.None
										};
										init.elementInitializers = new MultipurposeMember.ComplexElementInitializer[parameters.Length];
										for(int i = 0; i < parameters.Length; i++) {
											init.elementInitializers[i] = new MultipurposeMember.ComplexElementInitializer() {
												name = parameters[i].Name,
												type = parameters[i].ParameterType,
											};
										}
										initializers.Add(init);
										for(int i = 0; i < initializers.Count; i++) {
											initializers[i].name = "Element" + i;
										}
									}
									uNodeGUIUtility.GUIChanged(node, UIChangeType.Average);
								}
								else {
									var fields = type.GetMembers(BindingFlags.Public | BindingFlags.Instance);
									foreach(var vv in fields) {
										if(vv is FieldInfo || vv is PropertyInfo && (vv as PropertyInfo).CanWrite && (vv as PropertyInfo).GetIndexParameters().Length == 0) {
											bool valid = true;
											foreach(var v in initializers) {
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
										menu.AddItem(new GUIContent("Add All Fields"), false, () => {
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
												foreach(var vv in initializers) {
													if(v.Name == vv.name) {
														valid = false;
														break;
													}
												}
												if(valid) {
													uNodeEditorUtility.RegisterUndo(option.unityObject, "");
													initializers.Add(new MultipurposeMember.InitializerData() {
														name = v.Name,
														type = t,
													});
												}
											}
											uNodeGUIUtility.GUIChanged(node, UIChangeType.Average);
										});
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
										foreach(var vv in initializers) {
											if(v.Name == vv.name) {
												valid = false;
												break;
											}
										}
										if(valid) {
											menu.AddItem(new GUIContent("Add Field/" + v.Name), false, () => {
												uNodeEditorUtility.RegisterUndo(option.unityObject, "Add Field:" + v.Name);
												initializers.Add(new MultipurposeMember.InitializerData() {
													name = v.Name,
													type = t,
												});
												uNodeGUIUtility.GUIChanged(node, UIChangeType.Average);
											});
										}
									}
								}
								menu.ShowAsContext();
							},
							remove: index => {
								initializers.RemoveAt(index);
								uNodeGUIUtility.GUIChanged(node, UIChangeType.Average);
							},
							reorder: (_, _, _) => {
								uNodeGUIUtility.GUIChanged(node, UIChangeType.Average);
							});
					using(new EditorGUILayout.HorizontalScope()) {
						if(GUILayout.Button(new GUIContent("Refresh", ""), EditorStyles.miniButtonLeft)) {
							if(type.IsArray || type.IsCastableTo(typeof(IList))) {
								for(int i = 0; i < initializers.Count; i++) {
									initializers[i].name = "Element" + i;
								}
							}
							else {
								var fields = type.GetMembers();
								for(int x = 0; x < fields.Length; x++) {
									var field = fields[x];
									var t = ReflectionUtils.GetMemberType(field);
									for(int y = 0; y < initializers.Count; y++) {
										if(field.Name == initializers[y].name) {
											if(t != initializers[y].type) {
												initializers[y].name = field.Name;
												initializers[y].type = t;

											}
											break;
										}
									}
								}
							}
							uNodeGUIUtility.GUIChanged(node, UIChangeType.Average);
						}
						if(GUILayout.Button(new GUIContent("Reset", ""), EditorStyles.miniButtonRight)) {
							uNodeEditorUtility.RegisterUndo(option.unityObject, "Reset Initializer");
							initializers.Clear();
							uNodeGUIUtility.GUIChanged(node, UIChangeType.Average);
						}
					}
				}
			}
			using(new EditorGUILayout.VerticalScope("Box")) {
				EditorGUILayout.LabelField("Inputs", EditorStyles.centeredGreyMiniLabel);

				DrawMember(node, node.member, showAddButton, filter, customChangeAction);
			}
		}

		public static void DrawMember(NodeObject node, MultipurposeMember member, bool showAddButton = true, FilterAttribute filter = null, Action customChangeAction = null) {
			{
				EditorGUILayout.BeginHorizontal("Box");
				if(GUILayout.Button(new GUIContent(member.target.GetDisplayName().Split('.').FirstOrDefault()), EditorStyles.popup)) {
					GUI.changed = false;
					if(customChangeAction != null) {
						customChangeAction();
					}
					else {
						if(filter == null) {
							filter = new FilterAttribute() {
								MaxMethodParam = int.MaxValue,
								Static = true,
								VoidType = true,
							};
						}
						ItemSelector.ShowWindow(
							node,
							filter,
							customItems: ItemSelector.MakeCustomItems(node, filter),
							selectCallback: item => {
								uNodeEditorUtility.RegisterUndo(node.graphContainer as UnityEngine.Object);
								var instance = item.instance != null ? item.instance : MemberDataUtility.GetActualInstance(member.target);
								if(member.target.isDeepTarget && !item.isDeepTarget) {
									var memberItems = member.target.GetMembers();
									var currentType = member.target.startType;
									if(currentType != item.type) {
										member.target = item;
									}
									else {
										if(item.targetType.IsTargetingReflection()) {
											if(memberItems.Length > 1) {
												member.target = item;
											}
											else {
												var targetMembers = item.GetMembers();
												memberItems[0] = targetMembers[0];
												member.target = MemberData.CreateFromMembers(memberItems);
											}
										}
										else {
											switch(item.targetType) {
												case MemberData.TargetType.uNodeVariable:
												case MemberData.TargetType.uNodeLocalVariable:
												case MemberData.TargetType.uNodeParameter:
													instance = item.instance;
													member.target.startName = item.startName;
													member.target.targetType = item.targetType;
													break;
												default:
													if(instance != null && instance == item.instance) {
														instance = item;
													}
													member.target = MemberData.CreateFromMembers(memberItems);
													break;
											}
										}
									}
								}
								else {
									member.target = item;
								}
								if(member.target.IsRequiredInstance()) {
									member.target.instance = instance;
									node.Register();
								}
								uNodeGUIUtility.GUIChanged(node, UIChangeType.Average);
							}).ChangePosition(Event.current.mousePosition.ToScreenPoint());
					}
				}
				EditorGUILayout.EndHorizontal();
			}

			if(member.target.targetType == MemberData.TargetType.Values) {
				EditorGUILayout.LabelField(new GUIContent("Value : " + member.target.type.PrettyName(), uNodeEditorUtility.GetTypeIcon(member.target.type)), EditorStyles.boldLabel);
			}
			else if(member.instance != null) {
				EditorGUILayout.LabelField(new GUIContent("Instance : " + member.target.startType.PrettyName(), uNodeEditorUtility.GetTypeIcon(member.target.startType)), EditorStyles.boldLabel);
			}
			if(member.target.Items.Length > 0) {
				MemberInfo[] members = null;
				{//For documentation
					members = member.target.GetMembers(false);
					if(members != null && members.Length > 0 && members.Length + 1 != member.target.Items.Length) {
						members = null;
					}
				}
				int totalParam = 0;
				bool flag = false;
				switch(member.target.targetType) {
					case MemberData.TargetType.uNodeFunction: {
						var reference = member.target.startItem.GetReferenceValue() as Function;
						if(reference != null) {
							var summary = reference.comment;
							if(!string.IsNullOrEmpty(summary)) {
								EditorGUI.indentLevel++;
								EditorGUILayout.LabelField(summary, EditorStyles.wordWrappedLabel);
								EditorGUI.indentLevel--;
							}
							foreach(var p in reference.Parameters) {
								EditorGUILayout.LabelField(
									new GUIContent(
										ObjectNames.NicifyVariableName(p.name) + " : " + p.Type.PrettyName(),
										uNodeEditorUtility.GetTypeIcon(p.Type)),
									EditorStyles.boldLabel);
							}
							return;
						}
						break;
					}
					case MemberData.TargetType.uNodeVariable: {
						var reference = member.target.startItem.GetReferenceValue() as Variable;
						if(reference != null) {
							var summary = reference.GetSummary();
							if(!string.IsNullOrEmpty(summary)) {
								EditorGUI.indentLevel++;
								EditorGUILayout.LabelField(summary, EditorStyles.wordWrappedLabel);
								EditorGUI.indentLevel--;
							}
							return;
						}
						break;
					}
					case MemberData.TargetType.uNodeProperty: {
						var reference = member.target.startItem.GetReferenceValue() as Property;
						if(reference != null) {
							var summary = reference.GetSummary();
							if(!string.IsNullOrEmpty(summary)) {
								EditorGUI.indentLevel++;
								EditorGUILayout.LabelField(summary, EditorStyles.wordWrappedLabel);
								EditorGUI.indentLevel--;
							}
							return;
						}
						break;
					}
				}
				for(int i = 0; i < member.target.Items.Length; i++) {
					//Skip the first item since we already draw it
					if(i != 0) {
						if(members != null && (member.target.isDeepTarget || !member.target.IsTargetingUNode)) {
							MemberInfo memberInfo = members[i - 1];
							if(memberInfo is MethodBase) {
								var method = memberInfo as MethodBase;
								if(flag) {
									EditorGUILayout.Space();
								}
								var documentation = XmlDoc.XMLFromMember(memberInfo);
								if(GUILayout.Button(new GUIContent(EditorReflectionUtility.GetPrettyMethodName(method)), EditorStyles.popup)) {
									GUI.changed = false;
									ChangeMembers(member, members, i - 1, node, filter, Event.current.mousePosition.ToScreenPoint());
								}


								EditorGUI.indentLevel++;
								if(documentation != null && documentation["summary"] != null) {
									EditorGUILayout.LabelField(documentation["summary"].InnerText.Trim(), EditorStyles.wordWrappedLabel);
								}
								else if(memberInfo is ISummary) {
									if(!string.IsNullOrEmpty((memberInfo as ISummary).GetSummary())) {
										EditorGUILayout.LabelField((memberInfo as ISummary).GetSummary(), EditorStyles.wordWrappedLabel);
									}
								}
								if(method is MethodInfo && method.IsGenericMethod) {
									DrawChangeGenericArguments(node, member, method as MethodInfo, members);
								}
								EditorGUI.indentLevel--;
								var parameters = method.GetParameters();
								if(parameters.Length > 0) {
									totalParam += parameters.Length;
									for(int x = 0; x < parameters.Length; x++) {
										System.Type PType = parameters[x].ParameterType;
										if(PType != null) {
											EditorGUILayout.LabelField(new GUIContent(ObjectNames.NicifyVariableName(parameters[x].Name) + " : " + PType.PrettyName(), uNodeEditorUtility.GetTypeIcon(PType), PType.PrettyName(true)), EditorStyles.boldLabel);
											EditorGUI.indentLevel++;
											if(documentation != null && documentation["param"] != null) {
												XmlNode paramDoc = null;
												XmlNode doc = documentation["param"];
												while(doc.NextSibling != null) {
													if(doc.Attributes["name"] != null && doc.Attributes["name"].Value.Equals(parameters[x].Name)) {
														paramDoc = doc;
														break;
													}
													doc = doc.NextSibling;
												}
												if(paramDoc != null && !string.IsNullOrEmpty(paramDoc.InnerText)) {
													//Show documentation
													EditorGUILayout.LabelField(paramDoc.InnerText.Trim(), EditorStyles.wordWrappedLabel);
												}
											}
											else if(PType is ISummary) {
												if(!string.IsNullOrEmpty((PType as ISummary).GetSummary())) {
													EditorGUILayout.LabelField((PType as ISummary).GetSummary(), EditorStyles.wordWrappedLabel);
												}
											}
											EditorGUI.indentLevel--;
										}
									}
								}
								flag = true;
								continue;
							}
							else {
								if(flag) {
									EditorGUILayout.Space();
								}
								var documentation = XmlDoc.XMLFromMember(memberInfo);
								if(GUILayout.Button(new GUIContent(memberInfo.Name), EditorStyles.popup)) {
									GUI.changed = false;
									ChangeMembers(member, members, i - 1, node, filter, Event.current.mousePosition.ToScreenPoint());
								}
								EditorGUI.indentLevel++;
								if(documentation != null && documentation["summary"] != null) {
									EditorGUILayout.LabelField(documentation["summary"].InnerText.Trim(), EditorStyles.wordWrappedLabel);
								}
								else if(memberInfo is ISummary) {
									if(!string.IsNullOrEmpty((memberInfo as ISummary).GetSummary())) {
										EditorGUILayout.LabelField((memberInfo as ISummary).GetSummary(), EditorStyles.wordWrappedLabel);
									}
								}
								EditorGUI.indentLevel--;
								flag = true;
								continue;
							}
						}
					}
				}
				var targetType = member.target.targetType;
				if(members != null &&
					(targetType.IsTargetingVariable() || targetType.IsTargetingReflection() || targetType == MemberData.TargetType.uNodeParameter) &&
					(member.target.type != typeof(void) || members.Length > 1)) {

					EditorGUILayout.BeginHorizontal();
					GUILayout.FlexibleSpace();
					if(members.Length > 1) {
						if(GUILayout.Button("-", EditorStyles.miniButtonRight, GUILayout.Width(30))) {
							GUI.changed = false;
							uNodeEditorUtility.RegisterUndo(node.GetUnityObject());
							MemberInfo[] newMembers = new MemberInfo[members.Length - 1];
							Array.Copy(members, newMembers, members.Length - 1);
							member.target = MemberData.CreateFromMembers(newMembers);
							node.Register();
							uNodeGUIUtility.GUIChanged(node, UIChangeType.Average);
						}
					}
					if(showAddButton) {
						if(GUILayout.Button("+", members.Length > 1 ? EditorStyles.miniButtonLeft : EditorStyles.miniButton, GUILayout.Width(30))) {
							GUI.changed = false;
							if(members.Length == 0) {

							}
							else {
								var memberInfo = members[members.Length - 1];
								if(filter == null) {
									filter = new FilterAttribute() {
										MaxMethodParam = int.MaxValue,
										Static = ReflectionUtils.GetMemberIsStatic(memberInfo),
										VoidType = true,
									};
								}
								else {
									filter = new FilterAttribute(filter);
									filter.Static = ReflectionUtils.GetMemberIsStatic(memberInfo);
								}
								if(memberInfo is MethodInfo) {
									filter.InvalidTargetType = MemberData.TargetType.Constructor;
								}
								ItemSelector.ShowCustomItem(
									ItemSelector.MakeCustomItems(ReflectionUtils.GetMemberType(memberInfo), filter, "Data", ItemSelector.CategoryInherited),
									item => {
										uNodeEditorUtility.RegisterUndo(node.GetUnityObject());
										var memberItems = item.GetMembers();
										var currentType = ReflectionUtils.GetMemberType(memberInfo);
										MemberInfo[] newMembers = new MemberInfo[members.Length + memberItems.Length];
										Array.Copy(members, newMembers, members.Length);
										for(int x = 0; x < memberItems.Length; x++) {
											newMembers[x + members.Length] = memberItems[x];
										}
										member.target = MemberData.CreateFromMembers(newMembers);
										node.Register();
										uNodeGUIUtility.GUIChanged(node, UIChangeType.Average);
									},
									targetObject: node).ChangePosition(Event.current.mousePosition.ToScreenPoint());
							}
						}
					}
					EditorGUILayout.EndHorizontal();
				}
			}
		}

		static void DrawChangeGenericArguments(NodeObject node, MultipurposeMember target, MethodInfo method, MemberInfo[] members) {
			var methodDefinition = method.GetGenericMethodDefinition();
			var rawGenericArguments = methodDefinition.GetGenericArguments();
			var genericArguments = method.GetGenericArguments();
			for(int x = 0; x < genericArguments.Length; x++) {
				var index = x;
				var arg = genericArguments[index];
				using(new EditorGUILayout.HorizontalScope()) {
					EditorGUILayout.PrefixLabel(rawGenericArguments[index].Name);
					var rect = uNodeGUIUtility.GetRect();
					if(EditorGUI.DropdownButton(rect, new GUIContent(arg.PrettyName(), arg.FullName), FocusType.Keyboard)) {
						var filter = new FilterAttribute();
						filter.ToFilterGenericConstraints(rawGenericArguments[index]);
						ItemSelector.ShowType(node, filter, member => {
							genericArguments[index] = member.startType;
							var changedMethod = ReflectionUtils.MakeGenericMethod(methodDefinition, genericArguments);

							List<MemberInfo> infos = new List<MemberInfo>(members);
							var methodIndex = infos.IndexOf(method);
							infos[methodIndex] = changedMethod;

							if(ReflectionUtils.GetMemberType(method) != ReflectionUtils.GetMemberType(changedMethod)) {
								if(infos.Count > methodIndex + 1  && ReflectionUtils.GetMemberType(changedMethod).IsCastableTo(infos[methodIndex + 1].DeclaringType) == false) {
									uNodeEditorUtility.RegisterUndo(node.GetUnityObject());
									uNodeUtility.ResizeList(infos, methodIndex + 1);
									target.target = MemberData.CreateFromMembers(infos);
									node.Register();
									uNodeGUIUtility.GUIChanged(node, UIChangeType.Average);
									return;
								}
							}
							uNodeEditorUtility.RegisterUndo(node.GetUnityObject());
							target.target = MemberData.CreateFromMembers(infos);
							node.Register();
							uNodeGUIUtility.GUIChanged(node, UIChangeType.Average);
						}).ChangePosition(rect.ToScreenRect());
					}
				}
			}
		}

		static void ChangeMembers(MultipurposeMember member, MemberInfo[] members, int index, Node node, FilterAttribute filter, Vector2 mousePosition) {
			var memberInfo = members[index];
			if(filter == null) {
				filter = new FilterAttribute() {
					MaxMethodParam = int.MaxValue,
					Static = ReflectionUtils.GetMemberIsStatic(memberInfo),
					VoidType = true,
				};
			}
			else {
				filter.Static = ReflectionUtils.GetMemberIsStatic(memberInfo);
			}
			if(memberInfo is MethodInfo) {
				filter.InvalidTargetType = MemberData.TargetType.Constructor;
			}
			else if(index == 0) {
				filter.Static = true;
			} 
			ItemSelector.ShowCustomItem(
				ItemSelector.MakeCustomItems(memberInfo.DeclaringType, filter, "Data", ItemSelector.CategoryInherited),
				item => {
					ChangeMembers(member, members, index, node, item);
				},
				targetObject: node).ChangePosition(mousePosition);
		}

		static void ChangeMembers(MultipurposeMember member, MemberInfo[] members, int index, Node node, MemberData item) {
			uNodeEditorUtility.RegisterUndo(node.nodeObject.GetUnityObject());
			var memberInfo = members[index];
			var memberItems = item.GetMembers();
			var currentType = ReflectionUtils.GetMemberType(memberInfo);
			var newType = item.type;
			if(memberItems.Length == 1 && memberInfo is MethodInfo oldMethod && oldMethod.IsConstructedGenericMethod) {
				//For auto change the new generic method parameter type to previously generic method parameter type
				var newMember = memberItems[0];
				if(newMember is MethodInfo method && method.IsGenericMethod) {
					try {
						if(method.IsConstructedGenericMethod) {
							if(method.ReturnType != currentType) {
								method = method.GetGenericMethodDefinition();
							}
						}
						if(method.IsConstructedGenericMethod == false) {
							method = ReflectionUtils.MakeGenericMethod(method, oldMethod.GetGenericArguments());
							memberItems[0] = method;
							newType = method.ReturnType;
						}
					}
					catch { }
				}
			}
			if(currentType != newType || memberItems.Length > 1) {
				MemberInfo[] newMembers = new MemberInfo[index + memberItems.Length];
				Array.Copy(members, newMembers, index + 1);
				for(int x = 0; x < memberItems.Length; x++) {
					newMembers[x + index] = memberItems[x];
				}
				member.target = MemberData.CreateFromMembers(newMembers);
			}
			else {
				members[index] = memberItems[0];
				member.target = MemberData.CreateFromMembers(members);
			}
			node.Register();
			uNodeGUIUtility.GUIChanged(node, UIChangeType.Average);
		}
	}
}