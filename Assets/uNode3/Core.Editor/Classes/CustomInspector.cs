using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

namespace MaxyGames.UNode.Editors {
	public class CustomInspector : ScriptableObject {
		public GraphEditorData editorData;

		[NonSerialized]
		internal GraphEditorData unserializedEditorData;

		private static CustomInspector _default;
		internal static CustomInspector Default {
			get {
				if(_default == null) {
					_default = CreateInstance<CustomInspector>();
				}
				return _default;
			}
		}

		private static Dictionary<UnityEngine.Object, Editor> customEditors = new Dictionary<UnityEngine.Object, Editor>();

		public static Editor GetEditor(UnityEngine.Object obj) {
			if(!customEditors.TryGetValue(obj, out var editor) || !editor) {
				editor = Editor.CreateEditor(obj);
				customEditors[obj] = editor;
			}
			return editor;
		}

		public static void ResetEditor() {
			customEditors.Clear();
		}

		public static void ShowInspector(GraphEditorData editorData, int limitMultiEdit = 5) {
			//Check if we have select some element or not
			if(editorData.selectedCount == 0) {
				if(editorData.RootOwner is IScriptGraph scriptGraph) {
					EditorGUI.DropShadowLabel(uNodeGUIUtility.GetRect(), "Namespaces");
					EditorGUILayout.Space();
					scriptGraph.Namespace = EditorGUILayout.DelayedTextField("Namespace", scriptGraph.Namespace);
					uNodeGUI.DrawNamespace("Using Namespaces", scriptGraph.UsingNamespaces, scriptGraph as UnityEngine.Object, (arr) => {
						scriptGraph.UsingNamespaces = arr as List<string> ?? arr.ToList();
						uNodeEditorUtility.MarkDirty(scriptGraph as UnityEngine.Object);
					});
				}
				if(editorData.currentCanvas.IsValidElement()) {
					EditorGUI.DropShadowLabel(uNodeGUIUtility.GetRect(), "Graph");
					EditorGUILayout.Space();
					DrawGraphInspector(editorData.graph);
					if(editorData.currentCanvas is not MainGraphContainer) {
						EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
						EditorGUI.DropShadowLabel(uNodeGUIUtility.GetRect(), "Edited Canvas");
						EditorGUILayout.Space();
						var bind = UBind.FromGraphElement(editorData.currentCanvas);
						if(editorData.currentCanvas is NodeObject nodeObject && nodeObject.node != null) {
							DrawNodeEditorWithHeader(editorData, nodeObject);
						} else {
							UInspector.Draw(new DrawerOption(bind, false, true));
						}
					}
				} else if(editorData.graph != null) {
					EditorGUI.DropShadowLabel(uNodeGUIUtility.GetRect(), "Graph");
					EditorGUILayout.Space();
					DrawGraphInspector(editorData.graph);
				} else if(editorData.owner != null) {
					DrawUnitObject(editorData.owner);
				}
				return;
			}
			int drawCount = 0;
			foreach(var selection in editorData.selecteds) {
				if(selection == null)
					continue;
				if(drawCount >= limitMultiEdit)
					break;
				if(drawCount > 0) {
					EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
				}
				if(selection is NodeObject nodeObject) {
					if(editorData.selectedCount == 1) {
						DrawNodeEditorWithHeader(editorData, nodeObject);
					} else {
						EditorGUILayout.BeginHorizontal();
						Rect rect1 = GUILayoutUtility.GetRect(220, 18);
						//GUI.backgroundColor = Color.gray;
						GUI.Box(new Rect(rect1.x - 30, rect1.y, rect1.width + 50, rect1.height), GUIContent.none, (GUIStyle)"dockarea");
						//GUI.backgroundColor = Color.white;
						GUI.Label(rect1, nodeObject.name, EditorStyles.boldLabel);
						if(Event.current.type == EventType.MouseUp && Event.current.button == 1 && rect1.Contains(Event.current.mousePosition)) {
							GenericMenu menu = new GenericMenu();
							ShowInspectorMenu(menu, nodeObject);
						}
						if(Event.current.clickCount == 2 && Event.current.button == 0 && rect1.Contains(Event.current.mousePosition)) {
							RenameNode(nodeObject, rect1);
						}
						EditorGUILayout.EndHorizontal();
						if(DrawNodeEditor(editorData, nodeObject)) {
							drawCount++;
						}
					}
				} else if(selection is UGraphElement element){
					var bind = UBind.FromGraphElement(element);
					UInspector.Draw(new DrawerOption(bind, false, true));
					drawCount++;
				}
				else if(selection is UPort port) {
					Connection connection = port.ValidConnections.FirstOrDefault();
					if(connection != null && connection.isValid) {
						var input = connection.Input;
						var output = connection.Output;
						if(connection is ValueConnection) {
							if(input.node.node is Nodes.NodeValueConverter) {
								var node = input.node.node as Nodes.NodeValueConverter;
								DrawNodeEditorWithHeader(editorData, node);
								output = node.input.GetTargetPort() ?? output;
							}
							else if(output.node.node is Nodes.NodeValueConverter) {
								var node = output.node.node as Nodes.NodeValueConverter;
								DrawNodeEditorWithHeader(editorData, node);
								output = node.input.GetTargetPort() ?? output;
							}
						}
						{
							EditorGUI.DropShadowLabel(uNodeGUIUtility.GetRect(), "Input");
							if(connection is FlowConnection) {
								EditorGUI.LabelField(
									EditorGUI.PrefixLabel(uNodeGUIUtility.GetRect(), new GUIContent("Type")),
									new GUIContent("Flow", uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.FlowIcon))));
							}
							else {
								var portType = (input as ValueInput).type;
								EditorGUI.LabelField(
									EditorGUI.PrefixLabel(uNodeGUIUtility.GetRect(), new GUIContent("Type")),
									new GUIContent(portType.PrettyName(false), uNodeEditorUtility.GetTypeIcon(portType)));
							}
						}
						{
							EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
							EditorGUI.DropShadowLabel(uNodeGUIUtility.GetRect(), "Output");
							if(connection is FlowConnection) {
								EditorGUI.LabelField(
									EditorGUI.PrefixLabel(uNodeGUIUtility.GetRect(), new GUIContent("Type")),
									new GUIContent("Flow", uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.FlowIcon))));
							}
							else {
								var portType = (output as ValueOutput).type;
								EditorGUI.LabelField(
									EditorGUI.PrefixLabel(uNodeGUIUtility.GetRect(), new GUIContent("Type")),
									new GUIContent(portType.PrettyName(false), uNodeEditorUtility.GetTypeIcon(portType)));
							}
						}
						{
							EditorGUI.DropShadowLabel(uNodeGUIUtility.GetRect(), "Debug");
							var graph = port.node.graphContainer as UnityEngine.Object;
							if(EditorUtility.IsPersistent(graph)) {
								bool flag = GraphDebug.debugMessage.HasMessage(connection);
								var flag2 = EditorGUILayout.Toggle(new GUIContent("Show log on enter"), flag);
								if(flag != flag2) {
									flag = flag2;
									GraphDebug.debugMessage.SetMessage(connection, flag ? "" : null);
									GraphDebug.debugMessage.Save();
									uNodeGUIUtility.GUIChanged(connection.Input.node, UIChangeType.Average);
									uNodeGUIUtility.GUIChanged(connection.Output.node, UIChangeType.Average);
								}
								if(flag) {
									var text = GraphDebug.debugMessage.GetMessage(connection);
									var message = EditorGUILayout.TextField(new GUIContent("Debug message"), text);
									if(text != message) {
										GraphDebug.debugMessage.SetMessage(connection, message);
										GraphDebug.debugMessage.Save();
									}
#if !UNODE_PRO
									EditorGUILayout.HelpBox("This feature only work on uNode pro version", MessageType.Warning);
#endif
								}
							}
						}
					}
				}
				else if(editorData.selectedCount == 1) {
					if(selection is IGraph graph) {
						EditorGUI.DropShadowLabel(uNodeGUIUtility.GetRect(), "Graph");
						EditorGUILayout.Space();
						DrawGraphInspector(graph);
					}
					else if(selection is UnityEngine.Object unityObject) {
						DrawUnitObject(unityObject);
					}
				}
			}
			if(drawCount >= limitMultiEdit) {
				EditorGUILayout.BeginVertical("Box");
				EditorGUILayout.HelpBox("Multi Editing Limit : " + limitMultiEdit, MessageType.Info);
				EditorGUILayout.EndVertical();
			}
		}

		private static void RenameObject(UnityEngine.Object obj, Rect rect) {
			string name = obj.name;
			ActionPopupWindow.ShowWindow(rect,
				onGUI: () => {
					name = EditorGUILayout.TextField("Name", name);
				},
				onGUIBottom: () => {
					if(GUILayout.Button("Apply") || Event.current.keyCode == KeyCode.Return) {
						if(EditorUtility.IsPersistent(obj)) {
							if(AssetDatabase.IsMainAsset(obj)) {
								AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(obj), name);
							} else {
								uNodeEditorUtility.RegisterUndo(obj, "Rename");
								obj.name = name;
							}
						} else {
							uNodeEditorUtility.RegisterUndo(obj, "Rename");
							obj.name = name;
						}
						ActionPopupWindow.CloseLast();
						uNodeGUIUtility.GUIChangedMajor(obj);
					}
				});
		}

		private static void RenameNode(NodeObject node, Rect rect) {
			string name = node.name;
			ActionPopupWindow.ShowWindow(rect,
				onGUI: () => {
					name = EditorGUILayout.TextField("Name", name);
				},
				onGUIBottom: () => {
					if(GUILayout.Button("Apply") || Event.current.keyCode == KeyCode.Return) {
						uNodeEditorUtility.RegisterUndo(node.GetUnityObject(), "Rename node");
						node.name = name;
						ActionPopupWindow.CloseLast();
						uNodeGUIUtility.GUIChanged(node);
					}
				});
		}

		// private static void DrawLine(float height = 1, float expandingWidth = 15) {
		// 	var rect = uNodeEditorUtility.GetRectCustomHeight(height);
		// 	Handles.color = Color.gray;
		// 	Handles.DrawLine(new Vector2(rect.x - expandingWidth, rect.y), new Vector2(rect.width + expandingWidth, rect.y));
		// }

		public static void DrawUnitObject(UnityEngine.Object obj) {
			if(obj == null)
				return;
			if(obj as EnumScript) {
				EditorGUI.DropShadowLabel(uNodeGUIUtility.GetRect(), "Enum");
				EditorGUILayout.Space();

				var asset = obj as EnumScript;
				Type icon;
				if(asset is IIcon) {
					icon = (asset as IIcon).GetIcon() ?? typeof(TypeIcons.EnumIcon);
				}
				else {
					icon = typeof(TypeIcons.EnumIcon);
				}
				EditorGUILayout.BeginHorizontal();
				GUI.DrawTexture(uNodeGUIUtility.GetRect(GUILayout.Width(32), GUILayout.Height(32)), uNodeEditorUtility.GetTypeIcon(icon));
				EditorGUILayout.BeginVertical();
				EditorGUILayout.BeginHorizontal();
				if(AssetDatabase.IsSubAsset(asset)) {
					EditorGUILayout.LabelField(asset.name);
					var rect = uNodeGUIUtility.GetRect(GUILayout.MaxHeight(EditorGUIUtility.singleLineHeight), GUILayout.MaxWidth(EditorGUIUtility.singleLineHeight));
					GUI.DrawTexture(rect, Resources.Load<Texture2D>("uNodeIcon/edit_button"));
					if(rect.Contains(Event.current.mousePosition) && Event.current.type == EventType.MouseDown && Event.current.button == 0) {
						RenameObject(asset, rect);
					}
				}
				else {
					EditorGUILayout.LabelField(asset.name);
				}
				EditorGUILayout.EndHorizontal();
				GUILayout.Space(2);
				var comment = uNodeGUI.TextInput(asset.summary, "(Summary)", true);
				if(comment != asset.summary) {
					uNodeEditorUtility.RegisterUndo(asset);
					asset.summary = comment;
				}
				EditorGUILayout.EndVertical();
				EditorGUILayout.EndHorizontal();
				EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
			}
			Editor editor = GetEditor(obj);
			if(editor != null) {
				if(editor is GraphAssetEditor graphAssetEditor) {
					var monoScript = uNodeEditorUtility.GetMonoScript(obj);
					if(monoScript != null) {
						EditorGUI.BeginDisabledGroup(true);
						EditorGUILayout.ObjectField("Script", monoScript, typeof(MonoScript), true);
						EditorGUI.EndDisabledGroup();
					}
					graphAssetEditor.DrawGUI(false);
				}
				else {
					editor.OnInspectorGUI();
				}
			}
			if(GUI.changed) {
				uNodeGUIUtility.GUIChanged(obj);
			}
		}

		public static void DrawGraphInspector(IGraph graph) {
			var graphData = graph.GraphData;
			if(graphData.IsValidElement()) {
				Type icon;
				if(graph is IIcon) {
					icon = (graph as IIcon).GetIcon() ?? typeof(TypeIcons.ClassIcon);
				} else {
					icon = typeof(TypeIcons.ClassIcon);
				}
				EditorGUILayout.BeginHorizontal();
				GUI.DrawTexture(uNodeGUIUtility.GetRect(GUILayout.Width(32), GUILayout.Height(32)), uNodeEditorUtility.GetTypeIcon(icon));
				EditorGUILayout.BeginVertical();
				EditorGUILayout.BeginHorizontal();
				var asset = graph as UnityEngine.Object;
				if(asset != null) {
					if(AssetDatabase.IsSubAsset(asset)) {
						EditorGUILayout.LabelField(asset.name);
						var rect = uNodeGUIUtility.GetRect(GUILayout.MaxHeight(EditorGUIUtility.singleLineHeight), GUILayout.MaxWidth(EditorGUIUtility.singleLineHeight));
						GUI.DrawTexture(rect, Resources.Load<Texture2D>("uNodeIcon/edit_button"));
						if(rect.Contains(Event.current.mousePosition) && Event.current.type == EventType.MouseDown && Event.current.button == 0) {
							RenameObject(asset, rect);
						}
					} else {
						EditorGUILayout.LabelField(asset.name);
						if(graphData.name != asset.name) {
							graphData.name = asset.name;
						}
					}
				} else {
					var name = uNodeGUI.TextInput(graphData.name, "(Name)", false);
					if(!string.IsNullOrEmpty(name) && name != graphData.name) {
						if(graph is UnityEngine.Object)
							uNodeEditorUtility.RegisterUndo(graph as UnityEngine.Object);
						graphData.name = name;
					}
				}
				EditorGUILayout.EndHorizontal();
				GUILayout.Space(2);
				var comment = uNodeGUI.TextInput(graphData.comment, "(Summary)", true);
				if(comment != graphData.comment) {
					if(graph is UnityEngine.Object)
						uNodeEditorUtility.RegisterUndo(graph as UnityEngine.Object);
					graphData.comment = comment;
				}
				EditorGUILayout.EndVertical();
				EditorGUILayout.EndHorizontal();
				EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
			}
			if(graph is UnityEngine.Object) {
				DrawUnitObject(graph as UnityEngine.Object);
			}

			GraphUtility.ErrorChecker.DrawErrorMessages(graphData);
		}

		private static void DrawNodeEditorWithHeader(GraphEditorData editorData, NodeObject node) {
			NodeDrawer.DrawNicelyHeader(node, node.GetNodeIcon(), editorData.graph as UnityEngine.Object);
			DrawNodeEditor(editorData, node);
		}

		private static bool DrawNodeEditor(GraphEditorData editorData, NodeObject node) {
			if(editorData == null || editorData.owner == null || node == null)
				return false;
			if(node.node == null) {
				EditorGUILayout.HelpBox("Missing node type: " + node.serializedData.serializedType, MessageType.Error);
				if(GUILayout.Button("Change Node Type")) {
					var win = ItemSelector.ShowType(editorData.owner, new FilterAttribute(typeof(Node)) { DisplayAbstractType = false }, member => {
						Undo.RegisterCompleteObjectUndo(editorData.owner, "");
						node.serializedData.type = member.startType;
						(node as ISerializationCallbackReceiver).OnAfterDeserialize();
						uNodeGUIUtility.GUIChangedMajor(node);
					}).ChangePosition(Event.current.mousePosition.ToScreenPoint());
					win.usingNamespaces = new() { "MaxyGames", "MaxyGames.UNode", "MaxyGames.UNode.Nodes" };
					foreach(var ns in node.graphContainer.GetUsingNamespaces()) {
						win.usingNamespaces.Add(ns);
					}
				}
				return true;
			}
			else {
				var property = editorData.bindGraph.NodeElement(node.id);
				if(property == null)
					return false;
				EditorGUI.BeginChangeCheck();
				UInspector.Draw(property, label: GUIContent.none, type: node.node.GetType());
				if(EditorGUI.EndChangeCheck() || UnityEngine.GUI.changed) {
					uNodeGUIUtility.GUIChanged(node);
					uNodeEditor.ForceRepaint();
				}
				//UInspector.DrawChilds(property);
				//#if UseProfiler
				//				Profiler.BeginSample("Check Node Error");
				//#endif
				//				try {
				//					uNodeUtility.ClearEditorError(node);
				//					node.CheckError();
				//				}
				//				catch(System.Exception ex) {
				//					uNodeUtility.RegisterEditorError(node, ex.ToString());
				//				}
				//#if UseProfiler
				//				Profiler.EndSample();
				//#endif
			}
			return true;
		}

		/// <summary>
		/// Used to show inspector context menu.
		/// </summary>
		/// <param name="menu"></param>
		/// <param name="component"></param>
		public static void ShowInspectorMenu(GenericMenu menu, NodeObject node) {
			MonoScript ms = uNodeEditorUtility.GetMonoScript(node?.node);
			if(node?.node is Nodes.HLNode hlNode) {
				ms = uNodeEditorUtility.GetMonoScript(hlNode.type);
			}
			if(ms != null) {
				menu.AddItem(new GUIContent("Find Script"), false, delegate () {
					EditorGUIUtility.PingObject(ms);
				});
				menu.AddItem(new GUIContent("Edit Script"), false, delegate () {
					AssetDatabase.OpenAsset(ms);
				});
			}
			menu.ShowAsContext();
			Event.current.Use();
		}
	}

	[CustomEditor(typeof(CustomInspector))]
	public class CustomInspectorEditor : Editor {
		public override void OnInspectorGUI() {
			CustomInspector Target = (CustomInspector)target;

			var data = Target.unserializedEditorData ?? Target.editorData;
			EditorGUI.BeginChangeCheck();
			CustomInspector.ShowInspector(data);
			if(EditorGUI.EndChangeCheck() || UnityEngine.GUI.changed) {
				//uNodeGUIUtility.GUIChanged(Target);
				uNodeEditor.ForceRepaint();
			}
		}
	}
}