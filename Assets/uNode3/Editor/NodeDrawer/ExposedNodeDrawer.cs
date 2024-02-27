using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace MaxyGames.UNode.Editors.Drawer {
    public class ExposedNodeDrawer : NodeDrawer<Nodes.ExposedNode> {
		public override void DrawLayouted(DrawerOption option) {
			var node = GetNode(option);

			if(node.value.type != null) {
				DrawGUI(node);
			}

			DrawInputs(option);
			DrawOutputs(option);
			DrawErrors(option);
		}

		void DrawGUI(Nodes.ExposedNode node) {
			uNodeGUI.DrawCustomList(node.outputDatas, "Exposed Ports",
				drawElement: (position, index, value) => {
					var vType = value.type;
					string vName = ObjectNames.NicifyVariableName(value.name);
					if(vType != null) {
						position = EditorGUI.PrefixLabel(position, new GUIContent(vName));
						EditorGUI.LabelField(position, vType.prettyName);
					}
					else {
						position = EditorGUI.PrefixLabel(position, new GUIContent(vName));
						EditorGUI.HelpBox(position, "Type not found", MessageType.Error);
					}
				},
				add: (position) => {
					var type = node.value.type;
					bool hasAddMenu = false;
					GenericMenu menu = new GenericMenu();
					var fields = type.GetMembers(BindingFlags.Public | BindingFlags.Instance);
					foreach(var vv in fields) {
						if(vv is FieldInfo || vv is PropertyInfo && (vv as PropertyInfo).CanRead && (vv as PropertyInfo).GetIndexParameters().Length == 0) {
							bool valid = true;
							foreach(var v in node.outputDatas) {
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
						menu.AddItem(new GUIContent("Add All Fields"), false, delegate () {
							bool needUndo = true;
							foreach(var v in fields) {
								if(v is FieldInfo field) {
									if(field.Attributes.HasFlags(FieldAttributes.InitOnly))
										continue;
								}
								else if(v is PropertyInfo property) {
									if(!property.CanRead || property.GetIndexParameters().Length > 0) {
										continue;
									}
								}
								else {
									continue;
								}
								var t = ReflectionUtils.GetMemberType(v);
								bool valid = true;
								foreach(var vv in node.outputDatas) {
									if(v.Name == vv.name) {
										valid = false;
										break;
									}
								}
								if(valid) {
									if(needUndo) {
										needUndo = false;
										//Ensure to undo is called only once
										uNodeEditorUtility.RegisterUndo(node.nodeObject.GetUnityObject());
									}
									node.outputDatas.Add(new Nodes.ExposedNode.OutputData() {
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
							if(!property.CanRead || property.GetIndexParameters().Length > 0) {
								continue;
							}
						}
						else {
							continue;
						}
						var t = ReflectionUtils.GetMemberType(v);
						bool valid = true;
						foreach(var vv in node.outputDatas) {
							if(v.Name == vv.name) {
								valid = false;
								break;
							}
						}
						if(valid) {
							menu.AddItem(new GUIContent("Add Field/" + v.Name), false, delegate () {
								uNodeEditorUtility.RegisterUndo(node.nodeObject.GetUnityObject(), "Add Field:" + v.Name);
								node.outputDatas.Add(new Nodes.ExposedNode.OutputData() {
									name = v.Name,
									type = t,
								});
								uNodeGUIUtility.GUIChanged(node, UIChangeType.Average);
							});
						}
					}
					for(int i = 0; i < node.outputDatas.Count; i++) {
						var v = node.outputDatas[i];
						menu.AddItem(new GUIContent("Remove Field/" + v.name), false, delegate (object obj) {
							uNodeEditorUtility.RegisterUndo(node.nodeObject.GetUnityObject(), "Remove Field:" + v.name);
							node.outputDatas.Remove(v);
							node.Register();
							uNodeGUIUtility.GUIChanged(node, UIChangeType.Average);
						}, v);
					}
					menu.ShowAsContext();
				},
				remove: (index) => {
					uNodeEditorUtility.RegisterUndo(node.nodeObject.GetUnityObject(), "Remove Field:" + node.outputDatas[index].name);
					node.outputDatas.RemoveAt(index);
					node.Register();
					uNodeGUIUtility.GUIChanged(node, UIChangeType.Average);
				},
				reorder: (list, oldIndex, newIndex) => {
					uNodeGUIUtility.GUIChanged(node, UIChangeType.Average);
				}
			);
			//EditorGUILayout.BeginHorizontal();
			//if(GUILayout.Button(new GUIContent("Refresh", ""), EditorStyles.miniButtonLeft)) {
			//	node.Refresh();
			//	uNodeGUIUtility.GUIChanged(node, UIChangeType.Average);
			//}
			//EditorGUILayout.EndHorizontal();
		}
	}
}