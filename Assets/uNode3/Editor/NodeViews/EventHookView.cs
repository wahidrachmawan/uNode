﻿using System;
using System.Linq;
using System.Reflection;
using UnityEngine.Events;

namespace MaxyGames.UNode.Editors {
	[NodeCustomEditor(typeof(Nodes.EventHook))]
	public class EventHookView : BaseNodeView {
		protected override void OnReloadView() {
			base.OnReloadView();
		}

		public override void OnValueChanged() {
			//Ensure to repaint every value changed.
			MarkRepaint();
		}
	}
}