using UnityEngine;
using System.Collections.Generic;
using MaxyGames.UNode.Nodes;
using UnityEngine.UIElements;
using UnityEditor;

namespace MaxyGames.UNode.Editors {
	class DragHandlerMenuForFunction : DragHandlerMenu {
		public override int order => int.MinValue;

		public override IEnumerable<DropdownMenuItem> GetMenuItems(DragHandlerData data) {
			if(data is DragHandlerDataForGraphElement d) {
				var obj = d.draggedValue as Function;
				yield return new DropdownMenuAction("Invoke", evt => {
					NodeEditorUtility.AddNewNode<MultipurposeNode>(d.graphData, obj.name, null, d.mousePositionOnCanvas, (n) => {
						n.target = MemberData.CreateFromValue(obj);
					});
					d.graphEditor.Refresh();
				}, DropdownMenuAction.AlwaysEnabled);

				if(obj.ReturnType().IsCastableTo(typeof(System.Collections.IEnumerator)) && d.graphData.graph.GetGraphInheritType().IsCastableTo(typeof(MonoBehaviour))) {
					yield return new DropdownMenuAction("Start Coroutine", evt => {
						NodeEditorUtility.AddNewNode(d.graphData, d.mousePositionOnCanvas, delegate (NodeBaseCaller node) {
							node.target = MemberData.CreateFromMember(typeof(MonoBehaviour).GetMethod(nameof(MonoBehaviour.StartCoroutine), new[] { typeof(System.Collections.IEnumerator) }));
							node.Register();

							NodeEditorUtility.AddNewNode<MultipurposeNode>(d.graphData, obj.name, null, new Vector2(d.mousePositionOnCanvas.x - 200, d.mousePositionOnCanvas.y), (n) => {
								n.target = MemberData.CreateFromValue(obj);

								node.parameters[0].input.ConnectTo(n.output);
							});
						});
						d.graphEditor.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
				}
			}
			yield break;
		}

		public override bool IsValid(DragHandlerData data) {
			if(data is DragHandlerDataForGraphElement d) {
				return d.draggedValue is Function;
			}
			return false;
		}
	}
}