using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;
using static MaxyGames.UNode.Editors.UIElementUtility;
using System.CodeDom;

namespace MaxyGames.UNode.Editors {
	public enum BlockType {
		Action,
		Condition,
		CoroutineAction,
	}

	public interface INodeBlock {
		UNodeView nodeView { get; }
		BlockType blockType { get; }
		BlockContainer blocks { get; }

		List<UNodeView> blockViews { get; }
	}

	[Serializable]
	public class BlockNodeHandler : IDropTarget {
		public readonly INodeBlock block;
		public UNodeView node => block.nodeView;

		public VisualElement blockElement;
		public Label hintLabelBlock;
		public VisualElement dragDisplay;

		public BlockNodeHandler(INodeBlock block) {
			this.block = block;
			node.name = "node-blocks";
			blockElement = new VisualElement() { name = "block-container" };
			node.mainContainer.Add(blockElement);
			ToggleBlockHint(true);

			dragDisplay = new VisualElement();
			dragDisplay.AddToClassList("dragdisplay");

			node.AddManipulator(new DropableElementManipulator() {
				onDragLeave = evt => {
					RemoveDragIndicator();
				},
				onDragUpdate = evt => {
					Vector2 mousePosition = blockElement.WorldToLocal(evt.mousePosition);
					int blockIndex = GetDragBlockIndex(mousePosition);

					DraggingBlocks(new UNodeView[0], blockIndex);
					if(!m_DragStarted) {
						// TODO: Do something on first DragUpdated event (initiate drag)
						m_DragStarted = true;
						node.AddToClassList("dropping");
					} else {
						// TODO: Do something on subsequent DragUpdated events
						if(DragAndDrop.GetGenericData("uNode") != null ||
							DragAndDrop.visualMode == DragAndDropVisualMode.None &&
							DragAndDrop.objectReferences.Length > 0) {

							bool isPrefab = uNodeEditorUtility.IsPrefab(node.owner.graph.graphData.owner);
							if(isPrefab) {
								if(DragAndDrop.GetGenericData("uNode") != null) {
									var generic = DragAndDrop.GetGenericData("uNode");
									if(generic is UnityEngine.Object && uNodeEditorUtility.IsSceneObject(generic as UnityEngine.Object)) {
										DragAndDrop.visualMode = DragAndDropVisualMode.None;
										return;
									}
								} else if(DragAndDrop.objectReferences.Length > 0) {
									if(uNodeEditorUtility.IsSceneObject(DragAndDrop.objectReferences[0])) {
										DragAndDrop.visualMode = DragAndDropVisualMode.None;
										return;
									}
								}
							}
							DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
						}
					}
				},
				onDragPerform = evt => {
					RemoveDragIndicator();
					Vector2 mousePosition = blockElement.WorldToLocal(evt.mousePosition);
					int blockIndex = GetDragBlockIndex(mousePosition);
					if(block.blocks.childCount == 0) {
						blockIndex = 0;
					}
					DragAndDrop.AcceptDrag();
					m_DragStarted = false;
					node.RemoveFromClassList("dropping");
					if(DragAndDrop.GetGenericData("uNode") != null) {
						var generic = DragAndDrop.GetGenericData("uNode");

						#region Variable
						if(generic is Variable) {//Drag variable
							var UO = DragAndDrop.GetGenericData("uNode-Target") as UnityEngine.Object;
							DragHandleVariable(blockIndex, generic as Variable, UO);
						} else
						#endregion

						#region Property
							if(generic is Property) {//Drag property
							var property = generic as Property;
							DragHandleProperty(blockIndex, property);
						} else
						#endregion

						#region Function
							if(generic is Function) {//Drag functions.
							var function = generic as Function;
							DragHandleFunction(blockIndex, function);
						} else
						#endregion

						#region MemberInfo
						if(generic is MemberInfo) {
							if(generic is FieldInfo) {
								var member = generic as FieldInfo;
								DragHandleMember(blockIndex, member);
							} else if(generic is PropertyInfo) {
								var member = generic as PropertyInfo;
								DragHandleMember(blockIndex, member);
							} else if(generic is MethodInfo) {
								var member = generic as MethodInfo;
								DragHandleMember(blockIndex, member, node.owner.GetTopMousePosition(evt));
							} else if(generic is ConstructorInfo && block.blockType != BlockType.Condition) {
								//TODO: fix this
								//var act = BlockUtility.onAddGetAction(new MemberData(generic as ConstructorInfo));
								//node.RegisterUndo("");
								//block.blocks.InsertBlock(blockIndex, act);
								//node.MarkRepaint();
							}
						}
						#endregion

						#region Visual Element
						//if(generic is VisualElement) {
						//	#region Variable
						//	if(generic is TreeViews.VariableView) {
						//		var view = generic as TreeViews.VariableView;
						//		var variable = view.variable;
						//		var root = view.owner as uNodeRoot;
						//		if(root != node.graph.editorData.graph) {
						//			if(uNodeEditorUtility.IsPrefab(root)) {
						//				root = GraphUtility.GetTempGraphObject(root);
						//				if(root == node.graph.editorData.graph) {
						//					variable = root.GetVariableData(variable.Name);
						//				} else if(view.owner is IClassIdentifier) {
						//					var runtimeType = ReflectionUtils.GetRuntimeType(view.owner as uNodeRoot);
						//					var field = runtimeType.GetField(variable.Name);
						//					if(field != null) {
						//						DragHandleMember(blockIndex, field);
						//					} else {
						//						uNodeEditorUtility.DisplayErrorMessage();
						//					}
						//					return;
						//				} else {
						//					var type = uNodeEditorUtility.GetFullScriptName(view.owner as uNodeRoot).ToType(false);
						//					if(type != null) {
						//						var field = type.GetField(variable.Name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic);
						//						if(field != null) {
						//							DragHandleMember(blockIndex, field);
						//							return;
						//						}
						//					}
						//					EditorUtility.DisplayDialog("Type not found", "You need to compile graph to script in order to use it on another graph.", "OK");
						//					return;
						//				}
						//			} else
						//				return;
						//		}
						//		if(variable != null && root != null) {
						//			DragHandleVariable(blockIndex, variable, root);
						//		}
						//	} else
						//	#endregion

						//	#region Property
						//		if(generic is TreeViews.PropertyView) {
						//		var view = generic as TreeViews.PropertyView;
						//		var property = view.property;
						//		var root = property.owner as uNodeRoot;
						//		if(root != node.graph.editorData.graph) {
						//			if(uNodeEditorUtility.IsPrefab(root)) {
						//				root = GraphUtility.GetTempGraphObject(root);
						//				if(root == node.graph.editorData.graph) {
						//					property = root.GetPropertyData(property.Name);
						//				} else if(property.owner is IClassIdentifier) {
						//					var runtimeType = ReflectionUtils.GetRuntimeType(property.owner as uNodeRoot);
						//					var member = runtimeType.GetProperty(property.Name);
						//					if(member != null) {
						//							DragHandleMember(blockIndex, member);
						//					} else {
						//						uNodeEditorUtility.DisplayErrorMessage();
						//					}
						//					return;
						//				} else {
						//					var type = uNodeEditorUtility.GetFullScriptName(property.owner).ToType(false);
						//					if(type != null) {
						//						var member = type.GetProperty(property.Name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic);
						//						if(member != null) {
						//							DragHandleMember(blockIndex, member);
						//							return;
						//						}
						//					}
						//					EditorUtility.DisplayDialog("Type not found", "You need to compile graph to script in order to use it on another graph.", "OK");
						//					return;
						//				}
						//			} else
						//				return;
						//		}
						//		if(property != null && root != null) {
						//			DragHandleProperty(blockIndex, property);
						//		}
						//	} else
						//	#endregion

						//	#region Function
						//		if(generic is TreeViews.FunctionView) {
						//		var view = generic as TreeViews.FunctionView;
						//		var function = view.function;
						//		var root = function.owner as uNodeRoot;
						//		if(root != node.graph.editorData.graph) {
						//			if(uNodeEditorUtility.IsPrefab(root)) {
						//				root = GraphUtility.GetTempGraphObject(root);
						//				if(root == node.graph.editorData.graph) {
						//					function = root.GetFunction(function.Name, function.GenericParameters.Count, function.Parameters.Select(p => p.Type).ToArray());
						//				} else if(function.owner is IClassIdentifier) {
						//					var runtimeType = ReflectionUtils.GetRuntimeType(function.owner as uNodeRoot);
						//					var member = runtimeType.GetMethod(function.Name, function.Parameters.Select(p => p.Type).ToArray());
						//					if(member != null) {
						//						DragHandleMember(blockIndex, member, node.owner.GetTopMousePosition(evt));
						//					} else {
						//						uNodeEditorUtility.DisplayErrorMessage();
						//					}
						//					return;
						//				} else {
						//					var type = uNodeEditorUtility.GetFullScriptName(function.owner).ToType(false);
						//					if(type != null) {
						//						var member = type.GetMethod(function.Name, function.Parameters.Select(p => p.Type).ToArray());
						//						if(member != null) {
						//							DragHandleMember(blockIndex, member, node.owner.GetTopMousePosition(evt));
						//							return;
						//						}
						//					}
						//					EditorUtility.DisplayDialog("Type not found", "You need to compile graph to script in order to use it on another graph.", "OK");
						//					return;
						//				}
						//			} else
						//				return;
						//		}
						//		if(function != null && root != null) {
						//			DragHandleFunction(blockIndex, function);
						//		}
						//	}
						//	#endregion
						//}
						#endregion
					} else if(DragAndDrop.objectReferences.Length == 1) {//Dragging UnityObject
						var dragObject = DragAndDrop.objectReferences[0];
						DragHandleObject(blockIndex, dragObject, node.owner.GetTopMousePosition(evt));
					}
				},
				onDragExited = evt => {
					RemoveDragIndicator();
					m_DragStarted = false;
				},
			});
		}

		public bool CanDrop(IEnumerable<ISelectable> selection) {
			var blocks = selection.Select(t => t as UNodeView).Where(t => t != null && t.ownerBlock != null);
			if(!blocks.Any()) {
				var nodes = selection.OfType<BaseNodeView>().Select(n => n.nodeObject).Where(n => n != null);
				if(nodes.Any()) {
					return true;
				}
				var ports = selection.OfType<PortView>();
				if(ports.Any()) {
					bool flag = true;
					foreach(var port in ports) {
						if(port == null)
							continue;
						if(port.isFlow) {
							flag = false;
							break;
						} else {
							//TODO: 
						}
					}
					return flag;
				}
				return false;
			}
			foreach(var b in blocks) {
				if(b.ownerBlock != block) {
					switch(block.blockType) {
						case BlockType.Action:
						case BlockType.Condition:
							if(b.ownerBlock.blockType != block.blockType) {
								return false;
							}
							break;
						case BlockType.CoroutineAction:
							if(b.ownerBlock.blockType != block.blockType && b.ownerBlock.blockType != BlockType.Action) {
								return false;
							}
							break;
					}
				}
			}
			return true;
		}

		public void DraggingBlocks(IEnumerable<ISelectable> selection, int index) {
			dragDisplay.RemoveFromHierarchy();
			if(!CanDrop(selection)) {
				return;
			}
			float y = GetBlockIndexY(index, false);
			dragDisplay.style.top = y;
			blockElement.Add(dragDisplay);
		}

		public void RemoveDragIndicator() {
			if(dragDisplay.parent != null)
				blockElement.Remove(dragDisplay);
		}

		bool m_DragStarted;

		public int GetDragBlockIndex(Vector2 mousePosition) {
			for(int i = 0; i < blockElement.childCount; ++i) {
				float y = GetBlockIndexY(i, true);

				if(mousePosition.y <= y) {
					return i;
				}
			}

			return blockElement.childCount;
		}

		public float GetBlockIndexY(int index, bool middle) {
			float y = 0;
			if(blockElement.childCount == 0) {
				return 0;
			}
			if(index >= blockElement.childCount) {
				return blockElement.ElementAt(blockElement.childCount - 1).layout.yMax;
			} else if(middle) {
				return blockElement.ElementAt(index).layout.center.y;
			} else {
				y = blockElement.ElementAt(index).layout.yMin;

				if(index > 0) {
					y = (y + blockElement.ElementAt(index - 1).layout.yMax) * 0.5f;
				}
			}

			return y;
		}

		#region Drag Handler
		private void DragHandleObject(int blockIndex, UnityEngine.Object obj, Vector2 screenPosition) {
			GenericMenu menu = new GenericMenu();
			Action<UnityEngine.Object, string> action = (dOBJ, startName) => {
				menu.AddItem(new GUIContent(startName + "Get"), false, () => {
					FilterAttribute filter = new FilterAttribute();
					filter.MaxMethodParam = int.MaxValue;
					filter.VoidType = true;
					filter.Public = true;
					filter.Instance = true;
					filter.Static = false;
					filter.DisplayDefaultStaticType = false;
					string category = dOBJ.GetType().PrettyName();
					var customItems = ItemSelector.MakeCustomItems(dOBJ.GetType(), filter, category);
					if(customItems != null) {
						customItems.Insert(0, ItemSelector.CustomItem.Create("this", () => {
							//TODO: fix this
							//var act = BlockUtility.onAddGetAction(new MemberData(dOBJ, MemberData.TargetType.Values));
							//node.RegisterUndo("");
							//block.blocks.InsertBlock(blockIndex, act);
							//node.MarkRepaint();
						}, category));
						ItemSelector w = ItemSelector.ShowWindow(dOBJ, filter, delegate (MemberData value) {
							value.instance = new MemberData(dOBJ, MemberData.TargetType.Values);

							node.MarkRepaint();
						}, customItems).ChangePosition(GUIUtility.GUIToScreenPoint(screenPosition));
						w.displayDefaultItem = false;
					}
				});
				menu.AddItem(new GUIContent(startName + "Set"), false, () => {
					FilterAttribute filter = new FilterAttribute();
					filter.SetMember = true;
					filter.MaxMethodParam = int.MaxValue;
					//filter.VoidType = true;
					filter.Public = true;
					filter.Instance = true;
					filter.Static = false;
					filter.DisplayDefaultStaticType = false;
					var customItems = ItemSelector.MakeCustomItems(dOBJ.GetType(), filter, dOBJ.GetType().PrettyName());
					if(customItems != null) {
						ItemSelector w = ItemSelector.ShowWindow(dOBJ, filter, delegate (MemberData value) {
							value.instance = dOBJ;

							node.MarkRepaint();
						}, customItems).ChangePosition(GUIUtility.GUIToScreenPoint(screenPosition));
						w.displayDefaultItem = false;
					}
				});
			};

			action(obj, "");
			if(obj is GameObject) {
				menu.AddSeparator("");
				foreach(var comp in (obj as GameObject).GetComponents<Component>()) {
					action(comp, comp.GetType().Name + "/");
				}
			} else if(obj is Component) {
				menu.AddSeparator("");
				foreach(var comp in (obj as Component).GetComponents<Component>()) {
					action(comp, comp.GetType().Name + "/");
				}
			}
			menu.ShowAsContext();
		}

		private void DragHandleVariable(int blockIndex, Variable variable, UnityEngine.Object owner) {
			GenericMenu menu = new GenericMenu();
			if(block.blockType != BlockType.Condition) {
				//menu.AddItem(new GUIContent("Get"), false, () => {
				//	NodeEditorUtility.AddNewNode<MultipurposeNode>(this.block.blocks, Vector2.zero, n => {
				//		n.target = MemberData.CreateFromValue(variable);
				//		n.owner.SetSiblingIndex(blockIndex);
				//	});
				//	node.MarkRepaint();
				//});
				menu.AddItem(new GUIContent("Set"), false, () => {
					NodeEditorUtility.AddNewNode<Nodes.NodeSetValue>(this.block.blocks, Vector2.zero, n => {
						n.EnsureRegistered();
						n.target.AssignToDefault(MemberData.CreateFromValue(variable));
						n.value.AssignToDefault(MemberData.Default(variable.type));
						n.nodeObject.SetSiblingIndex(blockIndex);
					});
					node.MarkRepaint();
				});
			} else {
				menu.AddItem(new GUIContent("Equality Compare"), false, () => {
					NodeEditorUtility.AddNewNode<Nodes.ComparisonNode>(this.block.blocks, Vector2.zero, n => {
						n.EnsureRegistered();
						n.inputA.AssignToDefault(MemberData.CreateFromValue(variable));
						n.inputB.AssignToDefault(MemberData.Default(variable.type));
						n.nodeObject.SetSiblingIndex(blockIndex);
					});
					node.MarkRepaint();
				});
			}
			menu.ShowAsContext();
		}

		private void DragHandleProperty(int blockIndex, Property property) {
			GenericMenu menu = new GenericMenu();
			if(block.blockType != BlockType.Condition) {
				//if(property.CanGetValue()) {
				//	menu.AddItem(new GUIContent("Get"), false, () => {
				//		NodeEditorUtility.AddNewNode<MultipurposeNode>(this.block.blocks, Vector2.zero, n => {
				//			n.target = MemberData.CreateFromValue(property);
				//			n.owner.SetSiblingIndex(blockIndex);
				//		});
				//		node.MarkRepaint();
				//	});
				//}
				if(property.CanSetValue()) {
					menu.AddItem(new GUIContent("Set"), false, () => {
						NodeEditorUtility.AddNewNode<Nodes.NodeSetValue>(this.block.blocks, Vector2.zero, n => {
							n.EnsureRegistered();
							n.target.AssignToDefault(MemberData.CreateFromValue(property));
							n.value.AssignToDefault(MemberData.Default(property.ReturnType()));
							n.nodeObject.SetSiblingIndex(blockIndex);
						});
						node.MarkRepaint();
					});
				}
			} else {
				menu.AddItem(new GUIContent("Equality Compare"), false, () => {

					node.MarkRepaint();
				});
				menu.AddItem(new GUIContent("Is Compare"), false, () => {

					node.MarkRepaint();
				});
			}
			menu.ShowAsContext();
		}

		private void DragHandleFunction(int blockIndex, Function function) {
			GenericMenu menu = new GenericMenu();
			if(block.blockType != BlockType.Condition) {
				menu.AddItem(new GUIContent("Invoke"), false, () => {
					NodeEditorUtility.AddNewNode<MultipurposeNode>(this.block.blocks, Vector2.zero, n => {
						n.target = MemberData.CreateFromValue(function);
						n.nodeObject.SetSiblingIndex(blockIndex);
					});
					node.MarkRepaint();
				});
				if(function.ReturnType() != typeof(void)) {
					menu.AddItem(new GUIContent("Compare"), false, () => {

						node.MarkRepaint();
					});
				}
			} else if(function.ReturnType() != typeof(void)) {
				menu.AddItem(new GUIContent("Equality Compare"), false, () => {

					node.MarkRepaint();
				});
				menu.AddItem(new GUIContent("Is Compare"), false, () => {

					node.MarkRepaint();
				});
			}
			menu.ShowAsContext();
		}

		private void DragHandleMember(int blockIndex, FieldInfo member) {
			if(member.IsPrivate && !EditorUtility.DisplayDialog("Variable is Private",
				"The variable you're drop is private, it may give error on compile to script.\n\nDo you want to continue?",
				"Continue",
				"Cancel")) {
				return;
			}
			GenericMenu menu = new GenericMenu();
			if(block.blockType != BlockType.Condition) {
				menu.AddItem(new GUIContent("Get"), false, () => {

					node.MarkRepaint();
				});
				menu.AddItem(new GUIContent("Set"), false, () => {

					node.MarkRepaint();
				});
			} else {
				menu.AddItem(new GUIContent("Equality Compare"), false, () => {

					node.MarkRepaint();
				});
				menu.AddItem(new GUIContent("Is Compare"), false, () => {

					node.MarkRepaint();
				});
			}
			menu.ShowAsContext();
		}

		private void DragHandleMember(int blockIndex, PropertyInfo member) {
			bool nonPublic = false;
			if(member.GetGetMethod(false) == null && member.GetSetMethod(false) == null) {
				if(!EditorUtility.DisplayDialog("Property is Private", "The property you're drop is private, it may give error on compile to script.\n\nDo you want to continue?", "Continue", "Cancel")) {
					return;
				}
				nonPublic = true;
			}
			GenericMenu menu = new GenericMenu();
			if(block.blockType != BlockType.Condition) {
				if(member.GetGetMethod(nonPublic) != null) {
					menu.AddItem(new GUIContent("Get"), false, () => {

						node.MarkRepaint();
					});
				}
				if(member.GetSetMethod(nonPublic) != null) {
					menu.AddItem(new GUIContent("Set"), false, () => {

						node.MarkRepaint();
					});
				}
			} else {
				menu.AddItem(new GUIContent("Equality Compare"), false, () => {

					node.MarkRepaint();
				});
				menu.AddItem(new GUIContent("Is Compare"), false, () => {

					node.MarkRepaint();
				});
			}
			menu.ShowAsContext();
		}

		private void DragHandleMember(int blockIndex, MethodInfo member, Vector2 screenPosition = default(Vector2)) {
			if(member.IsPrivate) {
				if(!EditorUtility.DisplayDialog("Function is Private", "The function you're drop is private, it may give error on compile to script.\n\nDo you want to continue?", "Continue", "Cancel")) {
					return;
				}
			}
			if(member.ContainsGenericParameters) {
				var args = member.GetGenericArguments();
				TypeItem[] typeItems = new TypeItem[args.Length];
				for(int i = 0; i < args.Length; i++) {
					var fil = new FilterAttribute(args[i].BaseType);
					fil.ToFilterGenericConstraints(args[i]);
					typeItems[i] = new TypeItem(args[i].BaseType, fil);
				}
				if(args.Length == 1) {
					ItemSelector w = null;
					Action<MemberData> action = delegate (MemberData m) {
						if(w != null) {
							w.Close();
						}
						TypeBuilderWindow.Show(Rect.zero, node.graphData.currentCanvas, typeItems[0].filter, delegate (MemberData[] members) {
							member = ReflectionUtils.MakeGenericMethod(member, members.Select(item => item.startType).ToArray());
							DragHandleMember(blockIndex, member, screenPosition);
						}, new TypeItem(m, typeItems[0].filter));
					};
					w = ItemSelector.ShowAsNew(null, typeItems[0].filter, action).ChangePosition(screenPosition.ToScreenPoint());
				} else {
					TypeBuilderWindow.Show(screenPosition, node.graphData.currentCanvas, new FilterAttribute() { OnlyGetType = true }, (members) => {
						member = ReflectionUtils.MakeGenericMethod(member, members.Select(item => item.startType).ToArray());
						DragHandleMember(blockIndex, member, screenPosition);
					}, typeItems);
				}
			} else {
				GenericMenu menu = new GenericMenu();
				if(block.blockType != BlockType.Condition) {

				} else if(member.ReturnType != typeof(void)) {

				}
				menu.ShowAsContext();
			}
		}
		#endregion

		public void ToggleBlockHint(bool enable) {
			if(enable && hintLabelBlock == null) {
				hintLabelBlock = new Label("Press Space to add blocks") { name = "hint" };
				blockElement.Add(hintLabelBlock);
			} else if(!enable && hintLabelBlock != null) {
				hintLabelBlock.RemoveFromHierarchy();
				hintLabelBlock = null;
			}
		}

		#region Drag & Drop
		bool IDropTarget.CanAcceptDrop(List<ISelectable> selection) {
			return CanDrop(selection);
		}

		bool IDropTarget.DragEnter(DragEnterEvent evt, IEnumerable<ISelectable> selection, IDropTarget enteredTarget, ISelection dragSource) {
			return true;
		}

		bool IDropTarget.DragLeave(DragLeaveEvent evt, IEnumerable<ISelectable> selection, IDropTarget leftTarget, ISelection dragSource) {
			RemoveDragIndicator();
			return true;
		}

		bool IDropTarget.DragUpdated(DragUpdatedEvent evt, IEnumerable<ISelectable> selection, IDropTarget dropTarget, ISelection dragSource) {
			Vector2 mousePosition = blockElement.WorldToLocal(evt.mousePosition);

			int blockIndex = GetDragBlockIndex(mousePosition);

			DraggingBlocks(selection, blockIndex);
			if(!m_DragStarted) {
				// TODO: Do something on first DragUpdated event (initiate drag)
				m_DragStarted = true;
				node.AddToClassList("dropping");
			} else {
				// TODO: Do something on subsequent DragUpdated events
			}

			return true;
		}

		bool IDropTarget.DragPerform(DragPerformEvent evt, IEnumerable<ISelectable> selection, IDropTarget dropTarget, ISelection dragSource) {
			RemoveDragIndicator();

			Vector2 mousePosition = blockElement.WorldToLocal(evt.mousePosition);

			if(!CanDrop(selection))
				return true;
			IEnumerable<UNodeView> draggedNodes = selection.OfType<UNodeView>().Where(n => n.ownerBlock == null);
			int blockIndex = GetDragBlockIndex(mousePosition);
			if(!draggedNodes.Any()) {
				var nodes = selection.OfType<BaseNodeView>().Select(n => n.nodeObject).Where(n => n != null);
				if(nodes.Count() == 0) {
					var portView = selection.OfType<PortView>().FirstOrDefault();
					if(portView != null) {
						//TODO: fix this
						//if(block.blockType != BlockType.Condition) {
						//	//Action block
						//	BlockUtility.ShowAddActionMenu(node.owner.GetScreenMousePosition(evt), bck => {
						//		node.RegisterUndo();
						//		if(blockIndex >= block.blocks.blocks.Count) {
						//			block.blocks.blocks.Add(bck);
						//		} else {
						//			block.blocks.blocks.Insert(blockIndex, bck);
						//		}
						//		node.MarkRepaint();
						//	}, portView.GetConnection());
						//} else {
						//	//Condition block
						//	BlockUtility.ShowAddEventMenu(node.owner.GetScreenMousePosition(evt), portView.GetConnection(), bck => {
						//		node.RegisterUndo();
						//		if(blockIndex >= block.blocks.blocks.Count) {
						//			block.blocks.blocks.Add(bck);
						//		} else {
						//			block.blocks.blocks.Insert(blockIndex, bck);
						//		}
						//		node.MarkRepaint();
						//	});
						//}
					} else {
						uNodeEditorUtility.DisplayErrorMessage();
					}
					return true;
				}
				List<NodeObject> blockItems = new List<NodeObject>();
				if(block.blockType != BlockType.Condition) {
					//Action block
					foreach(var node in nodes) {
						blockItems.Add(node);
					}
				} else {
					//Condition block
					foreach(var node in nodes) {
						blockItems.Add(node);
					}
				}
				blockItems = blockItems.OrderBy(n => n.GetSiblingIndex()).ToList();
				if(blockItems.Count > 0) {
					node.RegisterUndo();
					if(blockIndex >= block.blocks.childCount) {
						for(int i = 0; i < blockItems.Count; i++) {
							blockItems[i].SetParent(block.blocks);
							blockItems[i].PlaceInFront(block.blocks.Last());
						}
					} else {
						var slibing = block.blocks.GetChild(blockIndex);
						for(int i = 0; i < blockItems.Count; i++) {
							if(slibing != blockItems[i])
								blockItems[i].SetParent(block.blocks);
							blockItems[i].PlaceBehind(slibing);
						}
					}
					node.MarkRepaint();
				} else {
					uNodeEditorUtility.DisplayErrorMessage();
				}
				return true;
			} else {
				//Is dragging a node.
				List<NodeObject> validNodes = new List<NodeObject>();
				if(block.blockType != BlockType.Condition) {
					//Action block
					foreach(var node in draggedNodes) {
						var nodeObject = node.nodeObject;
						if(nodeObject.primaryFlowInput == null) {
							uNodeEditorUtility.DisplayErrorMessage($"Cannot convert node to block because node: {nodeObject.GetTitle()} doesn't have primary input flow.");
							return false;
						}
						validNodes.Add(nodeObject);
					}
				} else {
					//Condition block
					foreach(var node in draggedNodes) {
						var nodeObject = node.nodeObject;
						if(nodeObject.primaryValueOutput == null || !nodeObject.primaryValueOutput.type.IsCastableTo(typeof(bool))) {
							uNodeEditorUtility.DisplayErrorMessage($"Cannot convert node to block because node: {nodeObject.GetTitle()} doesn't have primary output or the output type is not a bool.");
							return false;
						}
						validNodes.Add(nodeObject);
					}
				}
				validNodes = validNodes.OrderBy(n => n.GetSiblingIndex()).ToList();
				if(validNodes.Count > 0) {
					node.RegisterUndo("Convert node to blocks");
					if(block.blockType != BlockType.Condition) {
						foreach(var node in validNodes) {
							node.primaryFlowInput?.ClearConnections();
							node.primaryFlowOutput?.ClearConnections();
						}
					} else {

					}
					if(blockIndex >= block.blocks.childCount) {
						for(int i = 0; i < validNodes.Count; i++) {
							validNodes[i].SetParent(block.blocks);
						}
					} else {
						var slibing = block.blocks.GetChild(blockIndex);
						for(int i = 0; i < validNodes.Count; i++) {
							if(slibing != validNodes[i])
								validNodes[i].SetParent(block.blocks);
							validNodes[i].PlaceBehind(slibing);
						}
					}
					node.MarkRepaint();
					node.owner.MarkRepaint(true);
				} else {
					uNodeEditorUtility.DisplayErrorMessage();
				}
			}
			{
				//TODO: fix this
				//var blockItems = draggedBlock.OrderBy(item => block.blocks.IndexOf(item.data)).Select((item) => item.data);
				//for(int i = 0; i < block.blocks.blocks.Count; i++) {
				//	if(blockIndex > i && blockItems.Contains(block.blocks.blocks[i])) {
				//		blockIndex--;
				//		break;
				//	}
				//}
				//node.RegisterUndo();
				//block.blocks.blocks.RemoveAll((item) => blockItems.Contains(item));
				//foreach(var b in draggedBlock) {
				//	if(b.owner != this) {
				//		b.owner.nodeView.RegisterUndo();
				//	}
				//}
				//foreach(var b in draggedBlock) {
				//	if(b.owner != this && b.owner.blocks.blocks.Contains(b.data)) {
				//		b.owner.blocks.blocks.Remove(b.data);
				//	}
				//}
				//if(blockIndex >= block.blocks.blocks.Count) {
				//	block.blocks.blocks.AddRange(blockItems);
				//} else {
				//	block.blocks.blocks.InsertRange(blockIndex, blockItems);
				//}
				//node.MarkRepaint();
				//foreach(var b in draggedBlock) {
				//	if(b.owner != this) {
				//		b.owner.nodeView.MarkRepaint();
				//	}
				//}
			}

			DragAndDrop.AcceptDrag();

			m_DragStarted = false;
			node.RemoveFromClassList("dropping");
			return true;
		}

		bool IDropTarget.DragExited() {
			// TODO: Do something when current drag is canceled
			RemoveDragIndicator();
			m_DragStarted = false;

			return true;
		}
		#endregion
	}

	public abstract class BlockNodeView : BaseNodeView, IDropTarget, INodeBlock {
		public BlockType blockType { get; protected set; }
		public BlockContainer blocks { get; protected set; }
		public List<UNodeView> blockViews => m_blockViews;
		public UNodeView nodeView => this;

		protected List<UNodeView> m_blockViews = new List<UNodeView>();

		public BlockNodeHandler handler;

		protected override void OnSetup() {
			//titleContainer.Add(new RichLabel(() => title) { name = "title-label" });
			m_CollapseButton.RemoveFromHierarchy();
			handler = new BlockNodeHandler(this);
		}

		public override void ReloadView() {
			for(int i = 0; i < blockViews.Count; i++) {
				blockViews[i].RemoveFromHierarchy();
			}
			blockViews.Clear();
			base.ReloadView();
			handler.ToggleBlockHint(blockViews.Count == 0);
		}

		public override void InitializeEdge() {
			base.InitializeEdge();
			for(int i = 0; i < blockViews.Count; i++) {
				blockViews[i].InitializeEdge();
			}
		}

		protected UNodeView CreateBlock(NodeObject node) {
			if(node == null)
				return null;
			if(owner.cachedNodeMap.TryGetValue(node, out var cachedView)) {
				//Clear cached views
				cachedView.RemoveFromHierarchy();
				owner.nodeViews.Remove(cachedView);
			}

			var viewType = UIElementUtility.GetNodeViewTypeFromType(node.node.GetType());

			if(viewType == null)
				viewType = typeof(BaseNodeView);

			var nodeView = Activator.CreateInstance(viewType) as UNodeView;
			nodeView.ownerBlock = this;
			nodeView.Initialize(owner, node);

			owner.nodeViews.Add(nodeView);
			owner.nodeViewsPerNode[node] = nodeView;
			owner.cachedNodeMap[node] = nodeView;

			return nodeView;
		}

		protected void InitializeBlocks(BlockContainer blocks, BlockType blockType) {
			if(blocks != null) {
				this.blocks = blocks;
				this.blockType = blockType;
				border.SetToNoClipping();
				for(int i = 0; i < blocks.childCount; i++) {
					var obj = blocks.GetChild(i) as NodeObject;
					if(obj == null)
						continue;
					obj.position.position = Vector2.zero;
					var block = CreateBlock(obj);
					if(block == null)
						continue;
					handler.blockElement.Add(block);
					blockViews.Add(block);
				}
			}
		}

		bool IDropTarget.CanAcceptDrop(List<ISelectable> selection) {
			return ((IDropTarget)handler).CanAcceptDrop(selection);
		}

		bool IDropTarget.DragUpdated(DragUpdatedEvent evt, IEnumerable<ISelectable> selection, IDropTarget dropTarget, ISelection dragSource) {
			return ((IDropTarget)handler).DragUpdated(evt, selection, dropTarget, dragSource);
		}

		bool IDropTarget.DragPerform(DragPerformEvent evt, IEnumerable<ISelectable> selection, IDropTarget dropTarget, ISelection dragSource) {
			return ((IDropTarget)handler).DragPerform(evt, selection, dropTarget, dragSource);
		}

		bool IDropTarget.DragEnter(DragEnterEvent evt, IEnumerable<ISelectable> selection, IDropTarget enteredTarget, ISelection dragSource) {
			return ((IDropTarget)handler).DragEnter(evt, selection, enteredTarget, dragSource);
		}

		bool IDropTarget.DragLeave(DragLeaveEvent evt, IEnumerable<ISelectable> selection, IDropTarget leftTarget, ISelection dragSource) {
			return ((IDropTarget)handler).DragLeave(evt, selection, leftTarget, dragSource);
		}

		bool IDropTarget.DragExited() {
			return ((IDropTarget)handler).DragExited();
		}
	}
}