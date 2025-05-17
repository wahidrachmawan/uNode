using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;

namespace MaxyGames.UNode.Editors {
	[NodeCustomEditor(typeof(Nodes.MakeDictionaryNode))]
	public class MakeDictionaryNodeView : BaseNodeView {
		protected override void InitializeView() {
			base.InitializeView();
			Nodes.MakeDictionaryNode node = targetNode as Nodes.MakeDictionaryNode;
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
				node.elements.Add(new Nodes.MakeDictionaryNode.PortData());
				node.Register();
				var keyType = node.keyType.type;
				var valueType = node.valueType.type;
				if(FilterAttribute.DefaultTypeFilter.IsValidTypeForValueConstant(keyType) && ReflectionUtils.CanCreateInstance(keyType)) {
					node.elements[node.elements.Count - 1].keyPort.AssignToDefault(ReflectionUtils.CreateInstance(keyType));
				}
				if(FilterAttribute.DefaultTypeFilter.IsValidTypeForValueConstant(valueType) && ReflectionUtils.CanCreateInstance(valueType)) {
					node.elements[node.elements.Count - 1].valuePort.AssignToDefault(ReflectionUtils.CreateInstance(valueType));
				}
				MarkRepaint();
			}) { text = "+" });
			AddControl(Direction.Input, control);
		}
	}
}