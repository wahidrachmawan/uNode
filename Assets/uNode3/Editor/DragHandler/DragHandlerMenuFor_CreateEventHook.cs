using UnityEngine;
using System.Collections.Generic;
using MaxyGames.UNode.Nodes;
using UnityEngine.UIElements;
using UnityEditor;

namespace MaxyGames.UNode.Editors {
	class DragHandlerMenuFor_CreateEventHook : DragHandleMenuAction {
		public override string name => "Create Event Hook";

		public override int order => int.MinValue;

		public override void OnClick(DragHandlerData data) {
			if(data is DragHandlerDataForGraphElement d) {
				NodeEditorUtility.AddNewNode(d.graphData, d.mousePositionOnCanvas, delegate (EventHook node) {
					if(data.draggedValue is Variable variable) {
						node.target.AssignToDefault(MemberData.CreateFromValue(variable));
					}
					else if(data.draggedValue is Property property) {
						node.target.AssignToDefault(MemberData.CreateFromValue(property));
					}
					else if(data.draggedValue is Function function) {
						NodeEditorUtility.AddNewNode(d.graphData, new(d.mousePositionOnCanvas.x - 100, d.mousePositionOnCanvas.y - 100), (MultipurposeNode tNode) => {
							tNode.target = MemberData.CreateFromValue(function);
							tNode.Register();
							node.target.ConnectTo(tNode.output);
						});
					}
					node.Register();
				});
				d.graphEditor.Refresh();
			}
		}

		public override bool IsValid(DragHandlerData data) {
			if(data is DragHandlerDataForGraphElement d) {
				if(d.draggedValue is Variable variable) {
					return variable.type.IsSubclassOf(typeof(System.Delegate));
				}
				if(d.draggedValue is Property property) {
					return property.ReturnType().IsSubclassOf(typeof(System.Delegate));
				}
				if(d.draggedValue is Function function) {
					return function.ReturnType().IsSubclassOf(typeof(System.Delegate));
				}
			}
			return false;
		}
	}
}