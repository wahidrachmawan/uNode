using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;

namespace MaxyGames.UNode.Editors {
	[NodeCustomEditor(typeof(Nodes.MakeArrayNode))]
	public class MakeArrayNodeView : BaseNodeView {
		protected override void InitializeView() {
			base.InitializeView();
			Nodes.MakeArrayNode node = targetNode as Nodes.MakeArrayNode;
			ControlView control = new ControlView();
			control.Add(new Button(() => {
				if(node.elements.Count > 0) {
					RegisterUndo();
					node.elements.RemoveAt(node.elements.Count - 1);
					MarkRepaint();
				}
			}) { text = "-" });
			control.Add(new Button(() => {
				RegisterUndo();
				node.elements.Add(new Nodes.MakeArrayNode.PortData());
				node.Register();
				var type = node.elementType.type;
				if(FilterAttribute.DefaultTypeFilter.IsValidTypeForValueConstant(type) && ReflectionUtils.CanCreateInstance(type)) {
					node.elements[node.elements.Count - 1].port.AssignToDefault(ReflectionUtils.CreateInstance(type));
				}
				MarkRepaint();
			}) { text = "+" });
			AddControl(Direction.Input, control);
		}
	}
}