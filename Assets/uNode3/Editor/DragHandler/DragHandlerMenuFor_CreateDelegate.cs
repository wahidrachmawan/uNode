using UnityEngine;
using System.Collections.Generic;
using MaxyGames.UNode.Nodes;
using UnityEngine.UIElements;
using UnityEditor;
using System.Linq;

namespace MaxyGames.UNode.Editors {
	class DragHandlerMenuFor_CreateDelegate : DragHandleMenuAction {
		public override string name => "Create Delegate";

		public override int order => int.MinValue;

		public override void OnClick(DragHandlerData data) {
			var obj = (data.draggedValue as Function);
			if(data is DragHandlerDataForGraphElement d) {
				NodeEditorUtility.AddNewNode(d.graphData, d.mousePositionOnCanvas, delegate (NodeDelegateFunction node) {
					node.member.target = MemberData.CreateFromValue(obj);
					node.Register();
				});
				d.graphEditor.Refresh();
			}
		}

		public override bool IsValid(DragHandlerData data) {
			if(data is DragHandlerDataForGraphElement d) {
				if(d.draggedValue is Function) {
					return true;
				}
			}
			return false;
		}
	}
}