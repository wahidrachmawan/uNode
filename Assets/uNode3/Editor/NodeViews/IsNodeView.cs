using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;

namespace MaxyGames.UNode.Editors {
	[NodeCustomEditor(typeof(Nodes.ISNode))]
	public class IsNodeView : BaseNodeView {
		protected override void InitializeView() {
			base.InitializeView();
			if (uNodeUtility.preferredDisplay != DisplayKind.Full) {
				var control = UIElementUtility.CreateControl(
					this, 
					nameof(Nodes.ISNode.type), 
					filter: new FilterAttribute() { OnlyGetType = true, DisplayRuntimeType = true, ArrayManipulator = true });
				if(control != null) {
					var label = control.Query<Label>().First();
					if(label != null) {
						label.RemoveFromHierarchy();
					}
					AddControl(Direction.Input, control);
				}
				ConstructCompactTitle("target", control: control);
			}
		}
	}
}