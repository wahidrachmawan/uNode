using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MaxyGames.UNode {
	/// <summary>
	/// Attribute to register the event listeners.
	/// </summary>
	[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
	public class RegisterEventListenerAttribute : Attribute {
		public Type ListenerType {
			get;
			private set;
		}

		public string ListenerID {
			get;
			private set;
		}

		public RegisterEventListenerAttribute(Type listenerType, string listenerID) {
			ListenerType = listenerType;
			ListenerID = listenerID;
		}
	}

#region Interfaces
	public interface IEventListener {
		/// <summary>
		/// The event ID
		/// </summary>
		string eventID { get; }
		/// <summary>
		/// The event owner
		/// </summary>
		GameObject eventOwner { get; }

		/// <summary>
		/// Register a new event
		/// </summary>
		/// <param name="owner"></param>
		/// <param name="handler"></param>
		void Register(UnityEngine.Object owner, Delegate handler);

		/// <summary>
		/// Unregister an event
		/// </summary>
		/// <param name="handler"></param>
		void Unregister(Delegate handler);
	}

	public interface IEventListener<T> : IEventListener where T : Delegate {
		/// <summary>
		/// Register a new event
		/// </summary>
		/// <param name="owner"></param>
		/// <param name="handler"></param>
		void Register(UnityEngine.Object owner, T handler);

		/// <summary>
		/// Unregister an event
		/// </summary>
		/// <param name="handler"></param>
		void Unregister(T handler);
	}
#endregion

#region Base Classes
	public abstract class UBaseEventListener<T> : MonoBehaviour, IEventListener<T> where T : Delegate {
		protected readonly List<KeyValuePair<T, UnityEngine.Object>> events = new List<KeyValuePair<T, UnityEngine.Object>>();

		readonly List<KeyValuePair<T, UnityEngine.Object>> cachedEvents = new List<KeyValuePair<T, UnityEngine.Object>>();
		protected List<KeyValuePair<T, UnityEngine.Object>> GetEventsForTrigger() {
			cachedEvents.Clear();
			for(int i = 0; i < events.Count; i++) {
				cachedEvents.Add(events[i]);
			}
			return cachedEvents;
		}

		[NonSerialized]
		private GameObject _gameObject;
		public new GameObject gameObject {
			get {
				if(object.ReferenceEquals(_gameObject, null)) {
					_gameObject = base.gameObject;
				}
				return _gameObject;
			}
		}

		public abstract string eventID { get; }
		public bool hasEvent => events.Count > 0;

		GameObject IEventListener.eventOwner => gameObject;

		protected virtual void Awake() {
			hideFlags = HideFlags.HideInInspector | HideFlags.DontSaveInEditor;
			_gameObject = base.gameObject;
		}

		protected virtual void OnValidate() {
			if(!Application.isPlaying) {
				DestroyImmediate(this);
			}
		}

		public virtual void Register(UnityEngine.Object owner, T handler) {
			events.Add(new KeyValuePair<T, UnityEngine.Object>(handler, owner));
		}

		public virtual void Unregister(T handler) {
			int index = events.FindIndex(pair => pair.Key == handler);
			if(index < 0) {
				//throw new Exception("Attempt to Unregister unregistered handler");
				return;
			}
			events.RemoveAt(index);
		}

		public void UnregisterAll() {
			//events.Clear();
			var map = new List<KeyValuePair<T, UnityEngine.Object>>(events);
			foreach(var pair in map) {
				Unregister(pair.Key);
			}
		}

		void IEventListener.Register(UnityEngine.Object owner, Delegate handler) {
			if(handler is T del || handler.GetType().IsCastableTo(typeof(T)) && (del = (T)handler) != null) {
				Register(owner, del);
			} else {
				throw new Exception("The handler value is not correct, the value must be: " + typeof(T).FullName);
			}
		}

		void IEventListener.Unregister(Delegate handler) {
			if(handler == null) return;
			if(handler is T del || handler.GetType().IsCastableTo(typeof(T)) && (del = (T)handler) != null) {
				Unregister(del);
			} else {
				throw new Exception("The handler value is not correct, the value must be: " + typeof(T).FullName);
			}
		}
	}

	public abstract class UEventListener<T> : UBaseEventListener<T> where T : Delegate {
		public virtual void Trigger() {
			var events = GetEventsForTrigger();
			for(int i = 0; i < events.Count; i++) {
				if(events[i].Value != null) {
					OnTriggered(events[i].Key);
				}
			}
		}

		protected abstract void OnTriggered(T handler);
	}

	public abstract class UEventListener<T, P1> : UBaseEventListener<T> where T : Delegate {
		public virtual void Trigger(P1 value) {
			var events = GetEventsForTrigger();
			for(int i = 0; i < events.Count; i++) {
				if(events[i].Value != null) {
					OnTriggered(value, events[i].Key);
				}
			}
		}

		protected abstract void OnTriggered(P1 value, T handler);
	}

	public abstract class UEventListener<T, P1, P2> : UBaseEventListener<T> where T : Delegate {
		public virtual void Trigger(P1 valueA, P2 valueB) {
			var events = GetEventsForTrigger();
			for(int i = 0; i < events.Count; i++) {
				if(events[i].Value != null) {
					OnTriggered(valueA, valueB, events[i].Key);
				}
			}
		}

		protected abstract void OnTriggered(P1 valueA, P2 valueB, T handler);
	}
#endregion

#region Listeners
	class UpdateListener : UEventListener<Action> {
		public override string eventID => UEventID.Update;

		public override void Trigger() {
			var events = GetEventsForTrigger();
			for(int i = 0; i < events.Count; i++) {
				if(events[i].Value != null) {
					if(events[i].Value is Behaviour behaviour && !behaviour.enabled) {
						//Skip when the owner is disabled.
						continue;
					}
					OnTriggered(events[i].Key);
				}
			}
		}

		protected override void OnTriggered(Action handler) {
			handler.Invoke();
		}

		void Update() {
			Trigger();
		}

		public override void Register(UnityEngine.Object owner, Action handler) {
			base.Register(owner, handler);
			if(hasEvent && !enabled) {
				//Enable the event
				enabled = true;
			}
		}

		public override void Unregister(Action handler) {
			base.Unregister(handler);
			if(!hasEvent && enabled) {
				//Disable the event to free some performance cost
				enabled = false;
			}
		}
	}

	class FixedUpdateListener : UEventListener<Action> {
		public override string eventID => UEventID.FixedUpdate;

		public override void Trigger() {
			var events = GetEventsForTrigger();
			for(int i = 0; i < events.Count; i++) {
				if(events[i].Value != null) {
					if(events[i].Value is Behaviour behaviour && !behaviour.enabled) {
						//Skip when the owner is disabled.
						continue;
					}
					OnTriggered(events[i].Key);
				}
			}
		}

		protected override void OnTriggered(Action handler) {
			handler.Invoke();
		}

		void FixedUpdate() {
			Trigger();
		}

		public override void Register(UnityEngine.Object owner, Action handler) {
			base.Register(owner, handler);
			if(events.Count > 0 && !enabled) {
				//Enable the event
				enabled = true;
			}
		}

		public override void Unregister(Action handler) {
			base.Unregister(handler);
			if(events.Count == 0 && enabled) {
				//Disable the event to free some performance cost
				enabled = false;
			}
		}
	}

	class LateUpdateListener : UEventListener<Action> {
		public override string eventID => UEventID.LateUpdate;

		public override void Trigger() {
			var events = GetEventsForTrigger();
			for(int i = 0; i < events.Count; i++) {
				if(events[i].Value != null) {
					if(events[i].Value is Behaviour behaviour && !behaviour.enabled) {
						//Skip when the owner is disabled.
						continue;
					}
					OnTriggered(events[i].Key);
				}
			}
		}

		protected override void OnTriggered(Action handler) {
			handler.Invoke();
		}

		void LateUpdate() {
			Trigger();
		}

		public override void Register(UnityEngine.Object owner, Action handler) {
			base.Register(owner, handler);
			if(events.Count > 0 && !enabled) {
				//Enable the event
				enabled = true;
			}
		}

		public override void Unregister(Action handler) {
			base.Unregister(handler);
			if(events.Count == 0 && enabled) {
				//Disable the event to free some performance cost
				enabled = false;
			}
		}
	}

	class OnAnimatorIKListener : UEventListener<Action<int>, int> {
		public override string eventID => UEventID.OnAnimatorIK;

		protected override void OnTriggered(int value, Action<int> handler) {
			handler.Invoke(value);
		}

		void OnAnimatorIK(int layerIndex) {
			Trigger(layerIndex);
		}
	}

	class OnAnimatorMoveListener : UEventListener<Action> {
		public override string eventID => UEventID.OnAnimatorMove;

		protected override void OnTriggered(Action handler) {
			handler.Invoke();
		}

		void OnAnimatorMove() {
			Trigger();
		}
	}

	class OnApplicationFocusListener : UEventListener<Action<bool>, bool> {
		public override string eventID => UEventID.OnApplicationFocus;

		protected override void OnTriggered(bool value, Action<bool> handler) {
			handler.Invoke(value);
		}

		void OnApplicationFocus(bool focus) {
			Trigger(focus);
		}
	}

	class OnApplicationPauseListener : UEventListener<Action<bool>, bool> {
		public override string eventID => UEventID.OnApplicationPause;

		protected override void OnTriggered(bool value, Action<bool> handler) {
			handler.Invoke(value);
		}

		void OnApplicationPause(bool pauseStatus) {
			Trigger(pauseStatus);
		}
	}

	class OnApplicationQuitListener : UEventListener<Action> {
		public override string eventID => UEventID.OnApplicationQuit;

		protected override void OnTriggered(Action handler) {
			handler.Invoke();
		}

		void OnApplicationQuit() {
			Trigger();
		}
	}

	class OnBecameInvisibleListener : UEventListener<Action> {
		public override string eventID => UEventID.OnBecameInvisible;

		protected override void OnTriggered(Action handler) {
			handler.Invoke();
		}

		void OnBecameInvisible() {
			Trigger();
		}
	}

	class OnBecameVisibleListener : UEventListener<Action> {
		public override string eventID => UEventID.OnBecameVisible;

		protected override void OnTriggered(Action handler) {
			handler.Invoke();
		}

		void OnBecameVisible() {
			Trigger();
		}
	}

	class OnCollisionEnter2DListener : UEventListener<Action<Collision2D>, Collision2D> {
		public override string eventID => UEventID.OnCollisionEnter2D;

		protected override void OnTriggered(Collision2D value, Action<Collision2D> handler) {
			handler.Invoke(value);
		}

		void OnCollisionEnter2D(Collision2D col) {
			Trigger(col);
		}
	}

	class OnCollisionExit2DListener : UEventListener<Action<Collision2D>, Collision2D> {
		public override string eventID => UEventID.OnCollisionExit2D;

		protected override void OnTriggered(Collision2D value, Action<Collision2D> handler) {
			handler.Invoke(value);
		}

		void OnCollisionExit2D(Collision2D col) {
			Trigger(col);
		}
	}

	class OnCollisionStay2DListener : UEventListener<Action<Collision2D>, Collision2D> {
		public override string eventID => UEventID.OnCollisionEnter2D;

		protected override void OnTriggered(Collision2D value, Action<Collision2D> handler) {
			handler.Invoke(value);
		}

		void OnCollisionStay2D(Collision2D col) {
			Trigger(col);
		}
	}

	class OnCollisionEnterListener : UEventListener<Action<Collision>, Collision> {
		public override string eventID => UEventID.OnCollisionEnter;

		protected override void OnTriggered(Collision value, Action<Collision> handler) {
			handler.Invoke(value);
		}

		void OnCollisionEnter(Collision col) {
			Trigger(col);
		}
	}

	class OnCollisionExitListener : UEventListener<Action<Collision>, Collision> {
		public override string eventID => UEventID.OnCollisionExit;

		protected override void OnTriggered(Collision value, Action<Collision> handler) {
			handler.Invoke(value);
		}

		void OnCollisionExit(Collision col) {
			Trigger(col);
		}
	}

	class OnCollisionStayListener : UEventListener<Action<Collision>, Collision> {
		public override string eventID => UEventID.OnCollisionStay;

		protected override void OnTriggered(Collision value, Action<Collision> handler) {
			handler.Invoke(value);
		}

		void OnCollisionStay(Collision col) {
			Trigger(col);
		}
	}

	class OnGUIListener : UEventListener<Action> {
		public override string eventID => UEventID.OnGUI;

		protected override void OnTriggered(Action handler) {
			handler.Invoke();
		}

		void OnGUI() {
			Trigger();
		}
	}

	class OnMouseEnterListener : UEventListener<Action> {
		public override string eventID => UEventID.OnMouseEnter;

		protected override void OnTriggered(Action handler) {
			handler.Invoke();
		}

		void OnMouseEnter() {
			Trigger();
		}
	}

	class OnMouseDownListener : UEventListener<Action> {
		public override string eventID => UEventID.OnMouseDown;

		protected override void OnTriggered(Action handler) {
			handler.Invoke();
		}

		void OnMouseDown() {
			Trigger();
		}
	}

	class OnMouseDragListener : UEventListener<Action> {
		public override string eventID => UEventID.OnMouseDrag;

		protected override void OnTriggered(Action handler) {
			handler.Invoke();
		}

		void OnMouseDrag() {
			Trigger();
		}
	}

	class OnMouseExitListener : UEventListener<Action> {
		public override string eventID => UEventID.OnMouseExit;

		protected override void OnTriggered(Action handler) {
			handler.Invoke();
		}

		void OnMouseExit() {
			Trigger();
		}
	}

	class OnMouseOverListener : UEventListener<Action> {
		public override string eventID => UEventID.OnMouseOver;

		protected override void OnTriggered(Action handler) {
			handler.Invoke();
		}

		void OnMouseOver() {
			Trigger();
		}
	}

	class OnMouseUpAsButtonListener : UEventListener<Action> {
		public override string eventID => UEventID.OnMouseUpAsButton;

		protected override void OnTriggered(Action handler) {
			handler.Invoke();
		}

		void OnMouseUpAsButton() {
			Trigger();
		}
	}

	class OnMouseUpListener : UEventListener<Action> {
		public override string eventID => UEventID.OnMouseUp;

		protected override void OnTriggered(Action handler) {
			handler.Invoke();
		}

		void OnMouseUp() {
			Trigger();
		}
	}

	class OnParticleCollisionListener : UEventListener<Action<GameObject>, GameObject> {
		public override string eventID => UEventID.OnParticleCollision;

		protected override void OnTriggered(GameObject value, Action<GameObject> handler) {
			handler.Invoke(value);
		}

		void OnParticleCollision(GameObject gameObject) {
			Trigger(gameObject);
		}
	}

	class OnPostRenderListener : UEventListener<Action> {
		public override string eventID => UEventID.OnPostRender;

		protected override void OnTriggered(Action handler) {
			handler.Invoke();
		}

		void OnPostRender() {
			Trigger();
		}
	}

	class OnPreCullListener : UEventListener<Action> {
		public override string eventID => UEventID.OnPreCull;

		protected override void OnTriggered(Action handler) {
			handler.Invoke();
		}

		void OnPreCull() {
			Trigger();
		}
	}

	class OnPreRenderListener : UEventListener<Action> {
		public override string eventID => UEventID.OnPreRender;

		protected override void OnTriggered(Action handler) {
			handler.Invoke();
		}

		void OnPreRender() {
			Trigger();
		}
	}

	class OnRenderObjectListener : UEventListener<Action> {
		public override string eventID => UEventID.OnRenderObject;

		protected override void OnTriggered(Action handler) {
			handler.Invoke();
		}

		void OnRenderObject() {
			Trigger();
		}
	}

	class OnTransformChildrenChangedListener : UEventListener<Action> {
		public override string eventID => UEventID.OnTransformChildrenChanged;

		protected override void OnTriggered(Action handler) {
			handler.Invoke();
		}

		void OnTransformChildrenChanged() {
			Trigger();
		}
	}

	class OnTransformParentChangedListener : UEventListener<Action> {
		public override string eventID => UEventID.OnTransformParentChanged;

		protected override void OnTriggered(Action handler) {
			handler.Invoke();
		}

		void OnTransformParentChanged() {
			Trigger();
		}
	}

	class OnTriggerEnter2DListener : UEventListener<Action<Collider2D>, Collider2D> {
		public override string eventID => UEventID.OnTriggerEnter2D;

		protected override void OnTriggered(Collider2D value, Action<Collider2D> handler) {
			handler.Invoke(value);
		}

		void OnTriggerEnter2D(Collider2D col) {
			Trigger(col);
		}
	}

	class OnTriggerExit2DListener : UEventListener<Action<Collider2D>, Collider2D> {
		public override string eventID => UEventID.OnTriggerExit2D;

		protected override void OnTriggered(Collider2D value, Action<Collider2D> handler) {
			handler.Invoke(value);
		}

		void OnTriggerExit2D(Collider2D col) {
			Trigger(col);
		}
	}

	class OnTriggerStay2DListener : UEventListener<Action<Collider2D>, Collider2D> {
		public override string eventID => UEventID.OnTriggerStay2D;

		protected override void OnTriggered(Collider2D value, Action<Collider2D> handler) {
			handler.Invoke(value);
		}

		void OnTriggerStay2D(Collider2D col) {
			Trigger(col);
		}
	}

	class OnTriggerEnterListener : UEventListener<Action<Collider>, Collider> {
		public override string eventID => UEventID.OnTriggerEnter;

		protected override void OnTriggered(Collider value, Action<Collider> handler) {
			handler.Invoke(value);
		}

		void OnTriggerEnter(Collider col) {
			Trigger(col);
		}
	}

	class OnTriggerExitListener : UEventListener<Action<Collider>, Collider> {
		public override string eventID => UEventID.OnTriggerExit;

		protected override void OnTriggered(Collider value, Action<Collider> handler) {
			handler.Invoke(value);
		}

		void OnTriggerExit(Collider col) {
			Trigger(col);
		}
	}

	class OnTriggerStayListener : UEventListener<Action<Collider>, Collider> {
		public override string eventID => UEventID.OnTriggerStay;

		protected override void OnTriggered(Collider value, Action<Collider> handler) {
			handler.Invoke(value);
		}

		void OnTriggerStay(Collider col) {
			Trigger(col);
		}
	}

	class OnWillRenderObjectListener : UEventListener<Action> {
		public override string eventID => UEventID.OnWillRenderObject;

		protected override void OnTriggered(Action handler) {
			handler.Invoke();
		}

		void OnWillRenderObject() {
			Trigger();
		}
	}

	class OnDestroyListener : UEventListener<Action> {
		public override string eventID => UEventID.OnDestroy;

		protected override void OnTriggered(Action handler) {
			handler.Invoke();
		}

		void OnDestroy() {
			Trigger();
		}
	}

	class OnEnableListener : UEventListener<Action> {
		public override string eventID => UEventID.OnEnable;

		protected override void OnTriggered(Action handler) {
			handler.Invoke();
		}

		private bool init;

		void OnEnable() {
			if(!init)
				return;
			Trigger();
		}

		void Start() {
			init = true;

			var events = GetEventsForTrigger();
			for(int i = 0; i < events.Count; i++) {
				if(events[i].Value != null) {
					if(events[i].Value is Behaviour behaviour && !behaviour.enabled) {
						//Skip when the owner is disabled.
						continue;
					}
					OnTriggered(events[i].Key);
				}
			}
		}
	}

	class OnDisableListener : UEventListener<Action> {
		public override string eventID => UEventID.OnDisable;

		protected override void OnTriggered(Action handler) {
			handler.Invoke();
		}

		void OnDisable() {
			Trigger();
		}
	}
#endregion

#region UIListeners
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
		,IPointerMoveHandler
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
#endregion
}