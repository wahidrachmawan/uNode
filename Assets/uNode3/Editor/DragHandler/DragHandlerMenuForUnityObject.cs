using UnityEngine;
using System.Collections.Generic;
using MaxyGames.UNode.Nodes;
using UnityEngine.UIElements;
using UnityEditor;
using System.Linq;
using System.Reflection;
using UnityEngine.Events;

namespace MaxyGames.UNode.Editors {
	class DragHandlerMenuForUnityObject : DragHandlerMenu {
		public override int order => int.MinValue;

		public override IEnumerable<DropdownMenuItem> GetMenuItems(DragHandlerData data) {
			if(data is DragHandlerDataForGraphElement d) {
				var obj = d.draggedValue as UnityEngine.Object;

				bool valid = true;
				if(uNodeEditorUtility.IsSceneObject(obj)) {
					if(!(d.graphData.graph is IIndependentGraph)) {
						valid = false;
					}
					else if(!EditorUtility.IsPersistent(obj) && !uNodeEditorUtility.IsSceneObject(d.graphData.owner)) {
						valid = false;
					}
					else if(d.graphData.graph.GetGraphType().IsSubclassOf(typeof(UnityEngine.Object)) == false) {
						valid = false;
					}
				}

				IEnumerable<DropdownMenuItem> action(UnityEngine.Object obj, string startName) {
					yield return new DropdownMenuAction(startName + "Get", evt => {
						FilterAttribute filter = new FilterAttribute();
						filter.MaxMethodParam = int.MaxValue;
						filter.VoidType = true;
						filter.Public = true;
						filter.Instance = true;
						filter.Static = false;
						filter.DisplayDefaultStaticType = false;
						var type = obj.GetType();
						if(obj is IRuntimeClass || obj is IReflectionType || obj is IInstancedGraph) {
							type = ReflectionUtils.GetRuntimeType(obj);
						}
						string category = type.PrettyName();
						var customItems = ItemSelector.MakeCustomItems(type, filter, category, ItemSelector.CategoryInherited);
						if(customItems != null) {
							if(type.IsInterface == false && valid) {
								customItems.Insert(0, ItemSelector.CustomItem.Create("this", () => {
									var value = new MemberData(obj, MemberData.TargetType.Values);
									value.startType = type;
									NodeEditorUtility.AddNewNode(d.graphData, d.mousePositionOnCanvas, delegate (MultipurposeNode n) {
										n.target = MemberData.CreateFromValue(obj);
									});
									d.graphEditor.Refresh();
								}, category));
							}
							ItemSelector w = ItemSelector.ShowWindow(obj, filter, delegate (MemberData value) {
								if(!valid) {
									ShowInvalidReference(obj, d.graphData);
								}
								if(type.IsInterface || !valid) {
									obj = null;
								}
								var mData = new MemberData(obj, MemberData.TargetType.Values);
								mData.startType = type;
								value.startType = type;
								value.instance = mData;
								NodeEditorUtility.AddNewNode(d.graphData, d.mousePositionOnCanvas, delegate (MultipurposeNode n) {
									n.target = value;
								});
								d.graphEditor.Refresh();
							}, customItems).ChangePosition(d.mousePositionOnScreen);
							w.displayDefaultItem = false;
						}
					}, DropdownMenuAction.AlwaysEnabled);

					yield return new DropdownMenuAction(startName + "Set", evt => {
						FilterAttribute filter = new FilterAttribute();
						filter.SetMember = true;
						filter.MaxMethodParam = int.MaxValue;
						//filter.VoidType = true;
						filter.Public = true;
						filter.Instance = true;
						filter.Static = false;
						filter.DisplayDefaultStaticType = false;
						var type = obj.GetType();
						if(obj is IRuntimeClass || obj is IReflectionType || obj is IInstancedGraph) {
							type = ReflectionUtils.GetRuntimeType(obj);
						}
						var customItems = ItemSelector.MakeCustomItems(type, filter, type.PrettyName(), ItemSelector.CategoryInherited);
						if(customItems != null) {
							ItemSelector w = ItemSelector.ShowWindow(obj, filter, delegate (MemberData value) {
								if(!valid) {
									ShowInvalidReference(obj, d.graphData);
								}
								if(type.IsInterface || !valid) {
									obj = null;
								}
								value.instance = obj;
								value.startType = type;
								NodeEditorUtility.AddNewNode(d.graphData, d.mousePositionOnCanvas, delegate (MultipurposeNode mNode) {
									mNode.target = value;
									mNode.Register();
									NodeEditorUtility.AddNewNode<Nodes.NodeSetValue>(d.graphData, d.mousePositionOnCanvas, (n) => {
										n.target.AssignToDefault(mNode.output);
									});
								});
								d.graphEditor.Refresh();
							}, customItems).ChangePosition(d.mousePositionOnScreen);
							w.displayDefaultItem = false;
						}
					}, DropdownMenuAction.AlwaysEnabled);

					if(d.graphData.graph is IGraphWithVariables && obj.GetType() != typeof(MonoScript)) {
						yield return new DropdownMenuAction(startName + "Create variable with type: " + obj.GetType().PrettyName(), evt => {
							string name = "newVariable";
							if(obj is GameObject gameObject) {
								name = uNodeUtility.AutoCorrectName(gameObject.name);
							}
							else if(obj is Component component) {
								name = uNodeUtility.AutoCorrectName(component.gameObject.name);
							}
							if(name.Length > 1)
								name = char.ToLower(name[0]) + name.Substring(1);
							var variable = d.graphData.graphData.variableContainer.AddVariable(name, obj.GetType());
							if(valid) {
								variable.defaultValue = obj;
							}
							NodeEditorUtility.AddNewNode<MultipurposeNode>(d.graphData, d.mousePositionOnCanvas, (n) => {
								n.target = MemberData.CreateFromValue(variable);
							});
							d.graphEditor.Refresh();
							if(!valid) {
								ShowInvalidReference(obj, d.graphData);
							}
						}, DropdownMenuAction.AlwaysEnabled);
					}
					if(obj is UGlobalEvent) {
						yield return new DropdownMenuAction(startName + "Trigger Gloal Event", evt => {
							NodeEditorUtility.AddNewNode<NodeTriggerGlobalEvent>(d.graphData, d.mousePositionOnCanvas, (n) => {
								n.target = obj as UGlobalEvent;
							});
							d.graphEditor.Refresh();
						}, DropdownMenuAction.AlwaysEnabled);
					}

					var members = obj.GetType().GetMembers().Where(m => m.MemberType.HasFlags(MemberTypes.Field | MemberTypes.Event | MemberTypes.Property) && ReflectionUtils.CanGetMember(m));

					foreach(var m in members) {
						var info = m;
						var mType = ReflectionUtils.GetMemberType(m);
						if(mType.IsSubclassOf(typeof(System.Delegate)) || mType.IsCastableTo(typeof(UnityEventBase))) {
							yield return new DropdownMenuAction(startName + "Members/" + info.Name + " - Create callback", evt => {
								NodeEditorUtility.AddNewNode(d.graphData, d.mousePositionOnCanvas, delegate (MultipurposeNode mNode) {
									mNode.target = MemberData.CreateFromMember(info);
									mNode.Register();
									if(mType.IsSubclassOf(typeof(System.Delegate))) {
										NodeEditorUtility.AddNewNode<Nodes.NodeSetValue>(d.graphData, d.mousePositionOnCanvas + new Vector2(100, 0), (n) => {
											n.setType = SetType.Add;
											n.target.AssignToDefault(mNode.output);
											NodeEditorUtility.AddNewNode<Nodes.NodeLambda>(d.graphData, d.mousePositionOnCanvas + new Vector2(0, 100), (lambda) => {
												lambda.delegateType = mType;
												lambda.Register();
												lambda.output.ConnectTo(n.value);
											});
										});
									}
									else if(mType.IsCastableTo(typeof(UnityEventBase))) {
										var listener = mType.GetMethod(nameof(UnityEvent.AddListener));
										if(listener != null) {
											var dType = listener.GetParameters()[0].ParameterType;
											NodeEditorUtility.AddNewNode<MultipurposeNode>(d.graphData, d.mousePositionOnCanvas + new Vector2(100, 0), (n) => {
												n.target = MemberData.CreateFromMember(listener);
												n.Register();
												n.instance.ConnectTo(mNode.output);
												NodeEditorUtility.AddNewNode<Nodes.NodeLambda>(d.graphData, d.mousePositionOnCanvas + new Vector2(0, 100), (lambda) => {
													lambda.delegateType = mType;
													lambda.Register();
													lambda.output.ConnectTo(n.parameters[0].input);
												});
											});
										}
									}
								});
								d.graphEditor.Refresh();
								if(!valid) {
									ShowInvalidReference(obj, d.graphData);
								}
							}, DropdownMenuAction.AlwaysEnabled);

							if(d.graphData.currentCanvas is MainGraphContainer && d.graphData.graph is IStateGraph) {
								yield return new DropdownMenuAction(startName + "Members/" + info.Name + " - Create event listener", evt => {
									NodeEditorUtility.AddNewNode(d.graphData, d.mousePositionOnCanvas, delegate (CSharpEventListener node) {
										node.target = MemberData.CreateFromMember(info);
										node.Register();
										if(valid) {
											node.instance.AssignToDefault(MemberData.CreateFromValue(obj));
										}
									});
									d.graphEditor.Refresh();
									if(!valid) {
										ShowInvalidReference(obj, d.graphData);
									}
								}, DropdownMenuAction.AlwaysEnabled);
							}
						}
					}
				}
				{
					foreach(var menu in action(obj, "")) {
						yield return menu;
					}
				}
				if(obj is GameObject) {
					yield return new DropdownMenuSeparator("");
					foreach(var comp in (obj as GameObject).GetComponents<Component>()) {
						foreach(var menu in action(comp, comp.GetType().Name + "/")) {
							yield return menu;
						}
					}
				}
				else if(obj is Component) {
					yield return new DropdownMenuSeparator("");
					foreach(var comp in (obj as Component).GetComponents<Component>()) {
						if(comp == obj) continue;
						foreach(var menu in action(comp, comp.GetType().Name + "/")) {
							yield return menu;
						}
					}
				}
			}
			yield break;
		}

		public override bool IsValid(DragHandlerData data) {
			if(data is DragHandlerDataForGraphElement d) {
				return d.draggedValue is UnityEngine.Object;
			}
			return false;
		}

		void ShowInvalidReference(UnityEngine.Object obj, GraphEditorData graphData) {
			if(!(graphData.graph is IIndependentGraph)) {
				EditorUtility.DisplayDialog("", "The c# graph cannot reference project and scene object, the reference will be null.", "Ok");
				return;
			}
			else if(!EditorUtility.IsPersistent(obj) && !uNodeEditorUtility.IsSceneObject(graphData.owner)) {
				EditorUtility.DisplayDialog("", "The project graph cannot reference scene object, the reference will be null.", "Ok");
				return;
			}
			else if(graphData.graph.GetGraphType().IsSubclassOf(typeof(UnityEngine.Object)) == false) {
				EditorUtility.DisplayDialog("", "The graph that's not inherited from UnityEngine.Object cannot reference project and scene object, the reference will be null.", "Ok");
				return;
			}
		}
	}
}