using System;
using MaxyGames.UNode;

[assembly: RegisterEventListener(typeof(UpdateListener), UEventID.Update)]
[assembly: RegisterEventListener(typeof(FixedUpdateListener), UEventID.FixedUpdate)]
[assembly: RegisterEventListener(typeof(LateUpdateListener), UEventID.LateUpdate)]
[assembly: RegisterEventListener(typeof(OnAnimatorIKListener), UEventID.OnAnimatorIK)]
[assembly: RegisterEventListener(typeof(OnAnimatorMoveListener), UEventID.OnAnimatorMove)]
[assembly: RegisterEventListener(typeof(OnApplicationFocusListener), UEventID.OnApplicationFocus)]
[assembly: RegisterEventListener(typeof(OnApplicationPauseListener), UEventID.OnApplicationPause)]
[assembly: RegisterEventListener(typeof(OnApplicationQuitListener), UEventID.OnApplicationQuit)]
[assembly: RegisterEventListener(typeof(OnBecameInvisibleListener), UEventID.OnBecameInvisible)]
[assembly: RegisterEventListener(typeof(OnBecameVisibleListener), UEventID.OnBecameVisible)]
[assembly: RegisterEventListener(typeof(OnCollisionEnterListener), UEventID.OnCollisionEnter)]
[assembly: RegisterEventListener(typeof(OnCollisionEnter2DListener), UEventID.OnCollisionEnter2D)]
[assembly: RegisterEventListener(typeof(OnCollisionExitListener), UEventID.OnCollisionExit)]
[assembly: RegisterEventListener(typeof(OnCollisionExit2DListener), UEventID.OnCollisionExit2D)]
[assembly: RegisterEventListener(typeof(OnCollisionStayListener), UEventID.OnCollisionStay)]
[assembly: RegisterEventListener(typeof(OnCollisionStay2DListener), UEventID.OnCollisionStay2D)]
[assembly: RegisterEventListener(typeof(OnDestroyListener), UEventID.OnDestroy)]
[assembly: RegisterEventListener(typeof(OnDisableListener), UEventID.OnDisable)]
[assembly: RegisterEventListener(typeof(OnEnableListener), UEventID.OnEnable)]
[assembly: RegisterEventListener(typeof(OnGUIListener), UEventID.OnGUI)]
[assembly: RegisterEventListener(typeof(OnMouseDownListener), UEventID.OnMouseDown)]
[assembly: RegisterEventListener(typeof(OnMouseDragListener), UEventID.OnMouseDrag)]
[assembly: RegisterEventListener(typeof(OnMouseEnterListener), UEventID.OnMouseEnter)]
[assembly: RegisterEventListener(typeof(OnMouseExitListener), UEventID.OnMouseExit)]
[assembly: RegisterEventListener(typeof(OnMouseOverListener), UEventID.OnMouseOver)]
[assembly: RegisterEventListener(typeof(OnMouseUpListener), UEventID.OnMouseUp)]
[assembly: RegisterEventListener(typeof(OnMouseUpAsButtonListener), UEventID.OnMouseUpAsButton)]
[assembly: RegisterEventListener(typeof(OnPostRenderListener), UEventID.OnPostRender)]
[assembly: RegisterEventListener(typeof(OnPreCullListener), UEventID.OnPreCull)]
[assembly: RegisterEventListener(typeof(OnPreRenderListener), UEventID.OnPreRender)]
[assembly: RegisterEventListener(typeof(OnRenderObjectListener), UEventID.OnRenderObject)]
[assembly: RegisterEventListener(typeof(OnTransformChildrenChangedListener), UEventID.OnTransformChildrenChanged)]
[assembly: RegisterEventListener(typeof(OnTransformParentChangedListener), UEventID.OnTransformParentChanged)]
[assembly: RegisterEventListener(typeof(OnTriggerEnterListener), UEventID.OnTriggerEnter)]
[assembly: RegisterEventListener(typeof(OnTriggerEnter2DListener), UEventID.OnTriggerEnter2D)]
[assembly: RegisterEventListener(typeof(OnTriggerExitListener), UEventID.OnTriggerExit)]
[assembly: RegisterEventListener(typeof(OnTriggerExit2DListener), UEventID.OnTriggerExit2D)]
[assembly: RegisterEventListener(typeof(OnTriggerStayListener), UEventID.OnTriggerStay)]
[assembly: RegisterEventListener(typeof(OnTriggerStay2DListener), UEventID.OnTriggerStay2D)]
[assembly: RegisterEventListener(typeof(OnWillRenderObjectListener), UEventID.OnWillRenderObject)]
[assembly: RegisterEventListener(typeof(OnParticleCollisionListener), UEventID.OnParticleCollision)]

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
	/// <summary>
	/// The available event IDs
	/// </summary>
	public static class UEventID {
		public const string Update = nameof(Update);
		public const string FixedUpdate = nameof(FixedUpdate);
		public const string LateUpdate = nameof(LateUpdate);
		public const string OnAnimatorIK = nameof(OnAnimatorIK);
		public const string OnAnimatorMove = nameof(OnAnimatorMove);
		public const string OnApplicationFocus = nameof(OnApplicationFocus);
		public const string OnApplicationPause = nameof(OnApplicationPause);
		public const string OnApplicationQuit = nameof(OnApplicationQuit);
		public const string OnBecameInvisible = nameof(OnBecameInvisible);
		public const string OnBecameVisible = nameof(OnBecameVisible);
		public const string OnCollisionEnter = nameof(OnCollisionEnter);
		public const string OnCollisionEnter2D = nameof(OnCollisionEnter2D);
		public const string OnCollisionExit = nameof(OnCollisionExit);
		public const string OnCollisionExit2D = nameof(OnCollisionExit2D);
		public const string OnCollisionStay = nameof(OnCollisionStay);
		public const string OnCollisionStay2D = nameof(OnCollisionStay2D);
		public const string OnDestroy = nameof(OnDestroy);
		public const string OnDisable = nameof(OnDisable);
		public const string OnEnable = nameof(OnEnable);
		public const string OnGUI = nameof(OnGUI);
		public const string OnMouseDown = nameof(OnMouseDown);
		public const string OnMouseDrag = nameof(OnMouseDrag);
		public const string OnMouseEnter = nameof(OnMouseEnter);
		public const string OnMouseExit = nameof(OnMouseExit);
		public const string OnMouseOver = nameof(OnMouseOver);
		public const string OnMouseUp = nameof(OnMouseUp);
		public const string OnMouseUpAsButton = nameof(OnMouseUpAsButton);
		public const string OnPostRender = nameof(OnPostRender);
		public const string OnPreCull = nameof(OnPreCull);
		public const string OnPreRender = nameof(OnPreRender);
		public const string OnRenderObject = nameof(OnRenderObject);
		public const string OnTransformChildrenChanged = nameof(OnTransformChildrenChanged);
		public const string OnTransformParentChanged = nameof(OnTransformParentChanged);
		public const string OnTriggerEnter = nameof(OnTriggerEnter);
		public const string OnTriggerEnter2D = nameof(OnTriggerEnter2D);
		public const string OnTriggerExit = nameof(OnTriggerExit);
		public const string OnTriggerExit2D = nameof(OnTriggerExit2D);
		public const string OnTriggerStay = nameof(OnTriggerStay);
		public const string OnTriggerStay2D = nameof(OnTriggerStay2D);
		public const string OnWillRenderObject = nameof(OnWillRenderObject);
		public const string OnParticleCollision = nameof(OnParticleCollision);

		public const string OnButtonClick = nameof(OnButtonClick);
		public const string OnInputFieldValueChanged = nameof(OnInputFieldValueChanged);
		public const string OnInputFieldEndEdit = nameof(OnInputFieldEndEdit);
		public const string OnDropdownValueChanged = nameof(OnDropdownValueChanged);
		public const string OnToggleValueChanged = nameof(OnToggleValueChanged);
		public const string OnScrollbarValueChanged = nameof(OnScrollbarValueChanged);
		public const string OnScrollRectValueChanged = nameof(OnScrollRectValueChanged);
		public const string OnSliderValueChanged = nameof(OnSliderValueChanged);

		public const string OnPointerClick = nameof(OnPointerClick);
		public const string OnPointerDown = nameof(OnPointerDown);
		public const string OnPointerEnter = nameof(OnPointerEnter);
		public const string OnPointerExit = nameof(OnPointerExit);
		public const string OnPointerMove = nameof(OnPointerMove);
		public const string OnPointerUp = nameof(OnPointerUp);
	}
}