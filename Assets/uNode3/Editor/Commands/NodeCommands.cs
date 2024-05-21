using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MaxyGames.UNode.Nodes;
using UnityEditor;

namespace MaxyGames.UNode.Editors.Commands {
	public class UnblockCommands : NodeMenuCommand {
		public override string name {
			get {
				return "Convert/Unblock";
			}
		}

		public override void OnClick(Node source, Vector2 mousePosition) {
			source.nodeObject.SetParent(source.nodeObject.parent.parent);
			graph.Refresh();
		}

		public override bool IsValidNode(Node source) {
			if(source.nodeObject.parent is NodeObject parent && parent.node is INodeBlock) {
				return true;
			}
			return false;
		}
	}

	public class ConvertMacroCommands : NodeMenuCommand {
		public override string name {
			get {
				return "Convert/To Linked Macro";
			}
		}

		public override void OnClick(Node source, Vector2 mousePosition) {
			MacroNode node = source as MacroNode;
			string path = EditorUtility.SaveFilePanelInProject("Export to macro asset",
				"New Macro.asset",
				"asset",
				"Please enter a file name to save the macro to");
			if(path.Length != 0) {
				uNodeEditorUtility.RegisterUndo(source.GetUnityObject());
				MacroGraph macro = ScriptableObject.CreateInstance<MacroGraph>();

				var oldNodes = node.nodeObject.GetObjectsInChildren().ToArray();
				GraphUtility.CopyPaste.Copy(oldNodes);
				var nodes = GraphUtility.CopyPaste.Paste(macro.GraphData.mainGraphContainer);
				var mapID = new Dictionary<string, string>();
				for(int i = 0; i < nodes.Length; i++) {
					var oldID = oldNodes[i].id;
					//nodes[i].SetParent(macro.GraphData.mainGraphContainer);
					//Because after set parent to another graph, it's ID is changed.
					mapID[nodes[i].id.ToString()] = oldNodes[i].id.ToString();
				}
				//for(int i = 0; i < nodes.Length; i++) {
				//	GraphUtility.Analizer.AnalizeObject(nodes[i], obj => {
				//		if(obj is MemberData mData) {
				//			if(mData.IsTargetingUNode) {
				//				for(int i = 0; i < mData.Items.Length; i++) {
				//					var item = mData.Items[i];
				//					if(item != null && item.reference != null) {
				//						var refVal = item.GetReferenceValue();
				//						if(refVal is UGraphElement graphElement) {
				//							if(graphElement is Variable) {

				//							}
				//						}
				//						else if(item.reference is ParameterRef parameterRef) {
											
				//						}
				//					}
				//				}
				//			}
				//		}
				//		return false;
				//	});
				//}
				//macro.Refresh();
				NodeEditorUtility.AddNewNode(graph.graphData, null, null, mousePositionOnCanvas, (LinkedMacroNode n) => {
					n.macroAsset = macro;
					n.position = node.position;
					n.Register();
					foreach(var c in node.nodeObject.Connections.ToArray()) {
						if(c is FlowConnection flow) {
							if(flow.input.node == node.nodeObject) {
								Connection.CreateAndConnect(n.inputFlows.First(p => mapID[p.id] == flow.input.id), flow.output);
							} else if(flow.output.node == node.nodeObject) {
								Connection.CreateAndConnect(n.outputFlows.First(p => mapID[p.id] == flow.output.id), flow.input);
							}
						} else if(c is ValueConnection value) {
							if(value.input.node == node.nodeObject) {
								Connection.CreateAndConnect(n.inputValues.First(p => mapID[p.id] == value.input.id), value.output);
							} else if(value.output.node == node.nodeObject) {
								Connection.CreateAndConnect(n.outputValues.First(p => mapID[p.id] == value.output.id), value.input);
							}
						}
						c.Disconnect();
					}
				});
				AssetDatabase.CreateAsset(macro, path);
				AssetDatabase.SaveAssets();
				node.nodeObject.Destroy();
			}
			graph.Refresh();
		}

		public override bool IsValidNode(Node source) {
			if(source is MacroNode) {
				return true;
			}
			return false;
		}
	}

	//public class ConvertAsToGetComponentCommands : NodeMenuCommand {
	//	public override string name {
	//		get {
	//			return "Convert/To GetComponent";
	//		}
	//	}

	//	public override void OnClick(Node source, Vector2 mousePosition) {
	//		var node = source as ASNode;
	//		var type = node.ReturnType();
	//		NodeEditorUtility.AddNewNode(graph.editorData, null, null, mousePositionOnCanvas, (GetComponentNode n) => {
	//			n.type = MemberData.CreateFromType(type);
	//			n.target = node.target;
	//			n.editorRect = node.editorRect;
	//			RefactorUtility.RetargetNode(node, n);
	//		});
	//		NodeEditorUtility.RemoveNode(graph.editorData, node);
	//		graph.Refresh();
	//	}

	//	public override bool IsValidNode(Node source) {
	//		if(source is ASNode) {
	//			var node = source as ASNode;
	//			var type = node.ReturnType();
	//			if(type != null && type.IsCastableTo(typeof(Component))) {
	//				return true;
	//			}
	//		}
	//		return false;
	//	}
	//}
}