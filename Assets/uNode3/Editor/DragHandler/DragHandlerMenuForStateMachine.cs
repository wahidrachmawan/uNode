using UnityEngine;
using System.Collections.Generic;
using MaxyGames.UNode.Nodes;
using UnityEngine.UIElements;

namespace MaxyGames.UNode.Editors {
	class DragHandlerMenuForStateMachine : DragHandlerMenu {
		public override int order => int.MinValue;

		public override IEnumerable<DropdownMenuItem> GetMenuItems(DragHandlerData data) {
			if(data is DragHandlerDataForGraphElement d) {
				var obj = d.draggedValue as StateGraphContainer;

				IEnumerable<DropdownMenuItem> DoAction(UGraphElement obj, string path = "") {
					yield return new DropdownMenuAction($"{path}Get State Machine", evt => {
						NodeEditorUtility.AddNewNode(d.graphData, obj.name, null, d.mousePositionOnCanvas, delegate (GetStateMachineNode n) {
							n.kind = GetStateMachineNode.Kind.StateMachine;
							n.reference = obj;
							n.EnsureRegistered();
						});
						d.graphEditor.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					foreach(var state in obj.GetNodesInChildren<IStateNodeWithTransition>()) {
						if(state is AnyStateNode) continue;
						var title = (state as Node).GetTitle();
						yield return new DropdownMenuAction($"{path}{title}/Set State", evt => {
							NodeEditorUtility.AddNewNode(d.graphData, obj.name, null, d.mousePositionOnCanvas, delegate (GetStateMachineNode n) {
								n.kind = GetStateMachineNode.Kind.SetState;
								n.reference = obj;
								n.stateReference = (state as Node).nodeObject;
								n.EnsureRegistered();
							});
							d.graphEditor.Refresh();
						}, DropdownMenuAction.AlwaysEnabled);
						yield return new DropdownMenuAction($"{path}{title}/Get State Is Active", evt => {
							NodeEditorUtility.AddNewNode(d.graphData, obj.name, null, d.mousePositionOnCanvas, delegate (GetStateMachineNode n) {
								n.kind = GetStateMachineNode.Kind.GetState;
								n.reference = obj;
								n.stateReference = (state as Node).nodeObject;
								n.EnsureRegistered();
							});
							d.graphEditor.Refresh();
						}, DropdownMenuAction.AlwaysEnabled);
						if(state is NestedStateNode nested) {
							foreach(var v in DoAction(nested, title + "/")) {
								yield return v;
							}
						}
					}
				}
				foreach(var v in DoAction(obj)) {
					yield return v;
				}
			}
			yield break;
		}

		public override bool IsValid(DragHandlerData data) {
			if(data is DragHandlerDataForGraphElement d) {
				return d.draggedValue is StateGraphContainer;
			}
			return false;
		}
	}
}