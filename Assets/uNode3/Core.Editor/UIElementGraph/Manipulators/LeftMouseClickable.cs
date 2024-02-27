using System;
using UnityEngine.UIElements;

namespace MaxyGames.UNode.Editors {
	public class LeftMouseClickable : MouseManipulator {
		public event Action<MouseUpEvent> clicked;
		public bool stopPropagationOnClick = true;
		private bool active = false;

		public LeftMouseClickable(Action<MouseUpEvent> handler) {
			clicked = handler;
			activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
		}

		protected override void RegisterCallbacksOnTarget() {
			target.RegisterCallback<MouseDownEvent>(OnMouseDown);
			target.RegisterCallback<MouseUpEvent>(OnMouseUp);
		}

		protected override void UnregisterCallbacksFromTarget() {
			target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
			target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
		}

		protected void OnMouseDown(MouseDownEvent evt) {
			if(clicked != null && evt.button == 0) {
				active = true;
				//target.CaptureMouse();
				if(stopPropagationOnClick)
					evt.StopPropagation();
			}
		}

		protected void OnMouseUp(MouseUpEvent evt) {
			if(evt != null && active && CanStopManipulation(evt)) {
				active = false;
				//target.ReleaseMouse();
				if(target.ContainsPoint(evt.localMousePosition)) {
					clicked(evt);
				}
				if(stopPropagationOnClick)
					evt.StopPropagation();
			}
		}
	}
}