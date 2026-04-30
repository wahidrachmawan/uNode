using UnityEngine;
using System.Collections.Generic;
using MaxyGames.UNode.Nodes;
using UnityEngine.UIElements;
using UnityEditor;
using System.Linq;

namespace MaxyGames.UNode.Editors {
	class DragHandlerMenuForProperty : DragHandlerMenu {
		public override int order => int.MinValue;

		public override IEnumerable<DropdownMenuItem> GetMenuItems(DragHandlerData data) {
			if(data is DragHandlerDataForGraphElement d) {
				var obj = d.draggedValue as Property;
				if(obj.CanGetValue()) {
					yield return new DropdownMenuAction("Get", evt => {
						NodeEditorUtility.AddNewNode(d.graphData, obj.name, null, d.mousePositionOnCanvas, delegate (MultipurposeNode n) {
							var mData = MemberData.CreateFromValue(obj);
							n.target = mData;
							n.EnsureRegistered();
						});
						d.graphEditor.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
				}
				if(obj.CanSetValue()) {
					yield return new DropdownMenuAction("Set", evt => {
						NodeEditorUtility.AddNewNode(d.graphData, obj.name, null, d.mousePositionOnCanvas, delegate (Nodes.NodeSetValue n) {
							n.EnsureRegistered();
							var mData = MemberData.CreateFromValue(obj);
							n.target.AssignToDefault(mData);
							if(mData.type != null) {
								n.value.AssignToDefault(MemberData.Default(mData.type));
							}
						});
						d.graphEditor.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
				}
			}
			yield break;
		}

		public override bool IsValid(DragHandlerData data) {
			if(data is DragHandlerDataForGraphElement d) {
				return d.draggedValue is Property prop && (prop.CanGetValue() || prop.CanSetValue());
			}
			return false;
		}
	}
}