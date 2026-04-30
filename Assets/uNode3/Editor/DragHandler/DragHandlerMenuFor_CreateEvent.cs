using UnityEngine;
using System.Collections.Generic;
using MaxyGames.UNode.Nodes;
using UnityEngine.UIElements;
using UnityEditor;

namespace MaxyGames.UNode.Editors {
	class DragHandlerMenuFor_CreateEvent : DragHandleMenuAction {
		public override string name => "Create Event Listener";

		public override int order => int.MinValue;

		public override void OnClick(DragHandlerData data) {
			if(data is DragHandlerDataForGraphElement d) {
				NodeEditorUtility.AddNewNode(d.graphData, d.mousePositionOnCanvas, delegate (CSharpEventListener node) {
					if(data.draggedValue is Variable variable) {
						var member = variable.GetMemberInfo();
						if(member != null) {
							node.target = MemberData.CreateFromMember(member);
							node.Register();
							node.instance.AssignToDefault(MemberData.This(d.graphData.graph));
						}
					}
					else if(data.draggedValue is Property property) {
						var member = property.GetMemberInfo();
						if(member != null) {
							node.target = MemberData.CreateFromMember(member);
							node.Register();
							node.instance.AssignToDefault(MemberData.This(d.graphData.graph));
						}
					}
					node.Register();
				});
				d.graphEditor.Refresh();
			}
		}

		public override bool IsValid(DragHandlerData data) {
			if(data is DragHandlerDataForGraphElement d) {
				if(d.graphData.currentCanvas is MainGraphContainer) {
					if(d.graphData.graph is not IStateGraph) {
						return false;
					}
				}
				else if(d.graphData.currentCanvas is NodeObject nodeObject) {
					if(nodeObject.node is not INodeWithEventHandler) {
						return false;
					}
				}
				if(d.draggedValue is Variable variable) {
					return variable.type.IsSubclassOf(typeof(System.Delegate));
				}
				if(d.draggedValue is Property property) {
					return property.ReturnType().IsSubclassOf(typeof(System.Delegate));
				}
			}
			return false;
		}
	}
}