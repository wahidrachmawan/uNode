using MaxyGames.UNode;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[assembly: RegisterEventListener(typeof(OnButtonClickListener), UEventID.OnButtonClick)]
[assembly: RegisterEventListener(typeof(OnInputFieldValueChangedListener), UEventID.OnInputFieldValueChanged)]
[assembly: RegisterEventListener(typeof(OnInputFieldEndEditListener), UEventID.OnInputFieldEndEdit)]
[assembly: RegisterEventListener(typeof(OnDropdownValueChangedListener), UEventID.OnDropdownValueChanged)]
[assembly: RegisterEventListener(typeof(OnToggleValueChangedListener), UEventID.OnToggleValueChanged)]
[assembly: RegisterEventListener(typeof(OnScrollbarValueChangedListener), UEventID.OnScrollbarValueChanged)]
[assembly: RegisterEventListener(typeof(OnScrollRectValueChangedListener), UEventID.OnScrollRectValueChanged)]
[assembly: RegisterEventListener(typeof(OnSliderValueChangedListener), UEventID.OnSliderValueChanged)]

[assembly: RegisterEventListener(typeof(OnPointerClickListener), UEventID.OnPointerClick)]
[assembly: RegisterEventListener(typeof(OnPointerDownListener), UEventID.OnPointerDown)]
[assembly: RegisterEventListener(typeof(OnPointerEnterListener), UEventID.OnPointerEnter)]
[assembly: RegisterEventListener(typeof(OnPointerExitListener), UEventID.OnPointerExit)]
[assembly: RegisterEventListener(typeof(OnPointerMoveListener), UEventID.OnPointerMove)]
[assembly: RegisterEventListener(typeof(OnPointerUpListener), UEventID.OnPointerUp)]

namespace MaxyGames.UNode {
	class OnButtonClickListener : UEventListener<Action> {
		public override string eventID => UEventID.OnButtonClick;

		protected override void OnTriggered(Action handler) {
			handler.Invoke();
		}

		void Start() {
			var button = gameObject.GetComponent<UnityEngine.UI.Button>();
			if(button == null) {
				throw new Exception("The GameObject doesn't have UI Button component, please make sure that the required component is attached to the GameObject.");
			}
			button.onClick.AddListener(Trigger);
		}
	}

	class OnInputFieldValueChangedListener : UEventListener<Action<string>, string> {
		public override string eventID => UEventID.OnInputFieldValueChanged;

		protected override void OnTriggered(string value, Action<string> handler) {
			handler.Invoke(value);
		}

		void Start() {
			var ui = gameObject.GetComponent<UnityEngine.UI.InputField>();
			if(ui == null) {
				throw new Exception("The GameObject doesn't have UI InputField component, please make sure that the required component is attached to the GameObject.");
			}
			ui.onValueChanged.AddListener(Trigger);
		}
	}

	class OnInputFieldEndEditListener : UEventListener<Action<string>, string> {
		public override string eventID => UEventID.OnInputFieldEndEdit;

		protected override void OnTriggered(string value, Action<string> handler) {
			handler.Invoke(value);
		}

		void Start() {
			var ui = gameObject.GetComponent<UnityEngine.UI.InputField>();
			if(ui == null) {
				throw new Exception("The GameObject doesn't have UI InputField component, please make sure that the required component is attached to the GameObject.");
			}
			ui.onEndEdit.AddListener(Trigger);
		}
	}

	class OnDropdownValueChangedListener : UEventListener<Action<int>, int> {
		public override string eventID => UEventID.OnDropdownValueChanged;

		protected override void OnTriggered(int value, Action<int> handler) {
			handler.Invoke(value);
		}

		void Start() {
			var ui = gameObject.GetComponent<UnityEngine.UI.Dropdown>();
			if(ui == null) {
				throw new Exception("The GameObject doesn't have UI Dropdown component, please make sure that the required component is attached to the GameObject.");
			}
			ui.onValueChanged.AddListener(Trigger);
		}
	}

	class OnToggleValueChangedListener : UEventListener<Action<bool>, bool> {
		public override string eventID => UEventID.OnToggleValueChanged;

		protected override void OnTriggered(bool value, Action<bool> handler) {
			handler.Invoke(value);
		}

		void Start() {
			var ui = gameObject.GetComponent<UnityEngine.UI.Toggle>();
			if(ui == null) {
				throw new Exception("The GameObject doesn't have UI Toggle component, please make sure that the required component is attached to the GameObject.");
			}
			ui.onValueChanged.AddListener(Trigger);
		}
	}

	class OnScrollbarValueChangedListener : UEventListener<Action<float>, float> {
		public override string eventID => UEventID.OnScrollRectValueChanged;

		protected override void OnTriggered(float value, Action<float> handler) {
			handler.Invoke(value);
		}

		void Start() {
			var ui = gameObject.GetComponent<UnityEngine.UI.Scrollbar>();
			if(ui == null) {
				throw new Exception("The GameObject doesn't have UI Scrollbar component, please make sure that the required component is attached to the GameObject.");
			}
			ui.onValueChanged.AddListener(Trigger);
		}
	}

	class OnScrollRectValueChangedListener : UEventListener<Action<Vector2>, Vector2> {
		public override string eventID => UEventID.OnScrollRectValueChanged;

		protected override void OnTriggered(Vector2 value, Action<Vector2> handler) {
			handler.Invoke(value);
		}

		void Start() {
			var ui = gameObject.GetComponent<UnityEngine.UI.ScrollRect>();
			if(ui == null) {
				throw new Exception("The GameObject doesn't have UI ScrollRect component, please make sure that the required component is attached to the GameObject.");
			}
			ui.onValueChanged.AddListener(Trigger);
		}
	}

	class OnSliderValueChangedListener : UEventListener<Action<float>, float> {
		public override string eventID => UEventID.OnSliderValueChanged;

		protected override void OnTriggered(float value, Action<float> handler) {
			handler.Invoke(value);
		}

		void Start() {
			var ui = gameObject.GetComponent<UnityEngine.UI.Slider>();
			if(ui == null) {
				throw new Exception("The GameObject doesn't have UI Slider component, please make sure that the required component is attached to the GameObject.");
			}
			ui.onValueChanged.AddListener(Trigger);
		}
	}

	class OnPointerClickListener : UEventListener<Action<PointerEventData>, PointerEventData>, IPointerClickHandler {
		public override string eventID => UEventID.OnPointerClick;

		public void OnPointerClick(PointerEventData eventData) {
			Trigger(eventData);
		}

		protected override void OnTriggered(PointerEventData value, Action<PointerEventData> handler) {
			handler.Invoke(value);
		}
	}

	class OnPointerDownListener : UEventListener<Action<PointerEventData>, PointerEventData>, IPointerDownHandler {
		public override string eventID => UEventID.OnPointerDown;

		public void OnPointerDown(PointerEventData eventData) {
			Trigger(eventData);
		}

		protected override void OnTriggered(PointerEventData value, Action<PointerEventData> handler) {
			handler.Invoke(value);
		}
	}

	class OnPointerEnterListener : UEventListener<Action<PointerEventData>, PointerEventData>, IPointerEnterHandler {
		public override string eventID => UEventID.OnPointerEnter;

		public void OnPointerEnter(PointerEventData eventData) {
			Trigger(eventData);
		}

		protected override void OnTriggered(PointerEventData value, Action<PointerEventData> handler) {
			handler.Invoke(value);
		}
	}

	class OnPointerExitListener : UEventListener<Action<PointerEventData>, PointerEventData>, IPointerExitHandler {
		public override string eventID => UEventID.OnPointerExit;

		public void OnPointerExit(PointerEventData eventData) {
			Trigger(eventData);
		}

		protected override void OnTriggered(PointerEventData value, Action<PointerEventData> handler) {
			handler.Invoke(value);
		}
	}

	class OnPointerMoveListener : UEventListener<Action<PointerEventData>, PointerEventData>
#if UNITY_2021_1_OR_NEWER
		, IPointerMoveHandler
#endif
		{
		public override string eventID => UEventID.OnPointerMove;

		public void OnPointerMove(PointerEventData eventData) {
			Trigger(eventData);
		}

		protected override void OnTriggered(PointerEventData value, Action<PointerEventData> handler) {
			handler.Invoke(value);
		}
	}

	class OnPointerUpListener : UEventListener<Action<PointerEventData>, PointerEventData>, IPointerUpHandler {
		public override string eventID => UEventID.OnPointerUp;

		public void OnPointerUp(PointerEventData eventData) {
			Trigger(eventData);
		}

		protected override void OnTriggered(PointerEventData value, Action<PointerEventData> handler) {
			handler.Invoke(value);
		}
	}
}