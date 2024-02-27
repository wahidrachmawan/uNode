using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace MaxyGames.UNode.Editors.UIControl {
	public class DefaultControl : ValueControl {
		PopupElement button;

		public DefaultControl(ControlConfig config, bool autoLayout = false) : base(config, autoLayout) {
			Init(autoLayout);
		}

		void Init(bool autoLayout) {
			if (config.type is RuntimeType) {
				if(config.type.IsSubclassOf(typeof(UnityEngine.Object))) {
					if(ReflectionUtils.IsNativeType(config.type)) {
						var nativeType = ReflectionUtils.GetNativeType(config.type);
						if(nativeType != null) {
							var field = new ObjectField() {
								objectType = nativeType,
								value = config.value as UnityEngine.Object,
								allowSceneObjects = uNodeEditorUtility.IsSceneObject(config.owner.nodeObject.GetUnityObject()),
							};
							field.RegisterValueChangedCallback((e) => {
								config.OnValueChanged(e.newValue);
								MarkDirtyRepaint();
							});
							Add(field);
						}
						else {
							var button = new Label() { text = "null" };
							button.AddToClassList("PopupButton");
							button.EnableInClassList("Layout", autoLayout);
							Add(button);
						}
					}
					else {
						var field = new ObjectRuntimeField() {
							objectType = config.type as RuntimeType,
							value = config.value as UnityEngine.Object,
							allowSceneObjects = uNodeEditorUtility.IsSceneObject(config.owner.nodeObject.GetUnityObject()),
						};
						field.RegisterValueChangedCallback((e) => {
							config.OnValueChanged(e.newValue);
							MarkDirtyRepaint();
						});
						Add(field);
					}
				}
				else {
					var button = new Label() { text = "null" };
					button.AddToClassList("PopupButton");
					button.EnableInClassList("Layout", autoLayout);
					Add(button);
				}
			} else {
				button = new PopupElement();
				button.EnableInClassList("Layout", autoLayout);
				button.AddManipulator(new LeftMouseClickable(OnClick));
				{
					if (config.value is SerializedType) {
						button.text = (config.value as SerializedType).prettyName;
					} else {
						button.text = uNodeUtility.GetDisplayName(config.value);
					}
					button.tooltip = button.text;
					if (button.text.Length > 25) {
						button.text = button.text.Substring(0, 25);
					}
				}
				Add(button);
			}
		}

		void OnClick(MouseUpEvent mouseDownEvent) {
			var val = config.value;
			var mPos = mouseDownEvent.mousePosition;
			if(UnityEditor.EditorWindow.focusedWindow != null) {
				mPos = (mouseDownEvent.currentTarget as VisualElement).GetScreenMousePosition(
					mouseDownEvent.localMousePosition,
					UnityEditor.EditorWindow.focusedWindow);
			}
			if(config.type == typeof(SerializedType)) {
				if(config.filter.OnlyGetType) {
					if(config.filter.CanManipulateArray()) {
						TypeBuilderWindow.Show(Vector2.zero, config.targetCanvas, config.filter, delegate (MemberData[] types) {
							config.value = new SerializedType(types[0].startType);
							config.OnValueChanged(config.value);
							config.owner.OnValueChanged();
							config.owner.MarkRepaint();
						}, new TypeItem[1] { (val as SerializedType)?.type ?? typeof(object) }).ChangePosition(mPos);
					} else {
						ItemSelector.ShowWindow(null, config.filter, (m) => {
							config.value = new SerializedType(m.startType);
							config.OnValueChanged(config.value);
							config.owner.OnValueChanged();
							config.owner.MarkRepaint();
						}).ChangePosition(mPos);
					}
				}
			} else if(config.type == typeof(MemberData)) {
				if(config.filter.OnlyGetType) {
					if(config.filter.CanManipulateArray()) {
						TypeBuilderWindow.Show(Vector2.zero, config.targetCanvas, config.filter, delegate (MemberData[] types) {
							config.value = types[0];
							config.OnValueChanged(config.value);
							config.owner.OnValueChanged();
							config.owner.MarkRepaint();
						}, new TypeItem[1] { val as MemberData }).ChangePosition(mPos);
					} else {
						ItemSelector.ShowWindow((val as MemberData)?.GetInstance(), config.filter, (m) => {
							config.value = m;
							config.OnValueChanged(m);
							config.owner.OnValueChanged();
							config.owner.MarkRepaint();
						}).ChangePosition(mPos);
					}
				}
			} else {
				ActionPopupWindow.ShowWindow(Vector2.zero, () => {
					uNodeGUIUtility.EditValueLayouted(GUIContent.none, val, config.type, (obj) => {
						config.owner.RegisterUndo();
						val = obj;
						config.OnValueChanged(obj);
						config.owner.MarkRepaint();
					}, new uNodeUtility.EditValueSettings() {
						attributes = new object[] { config.filter },
						unityObject = config.owner.targetNode.GetUnityObject()
					});
				}, 300, 300).ChangePosition(mPos);
			}
		}

		protected override void ImmediateRepaint() {
			//if (uNodeThreadUtility.frame % 2 == 0) return;
			//if (visible && resolvedStyle.opacity != 0 && button != null) {
			//	if (config.value != null && config.value as uNodeRoot == config.owner.targetNode?.owner) {
			//		button.text = "this";
			//	} else {
			//		button.text = uNodeUtility.GetDisplayName(config.value);
			//	}
			//	button.tooltip = button.text;
			//	if (button.text.Length > 25) {
			//		button.text = button.text.Substring(0, 25);
			//	}
			//}
		}
	}

	public class PopupControl : ValueControl {
		string label;
		PopupElement button;
		System.Action onClick;

		public PopupControl(string label, bool autoLayout = false) : base(null, autoLayout) {
			this.label = label;
			Init(autoLayout);
		}

		public PopupControl(string label, System.Action onClick, bool autoLayout = false) : base(null, autoLayout) {
			this.label = label;
			this.onClick = onClick;
			Init(autoLayout);
		}

		void Init(bool autoLayout) {
			button = new PopupElement();
			button.EnableInClassList("Layout", autoLayout);
			button.AddManipulator(new LeftMouseClickable(OnClick));
			{
				button.text = label;
				if(button.text.Length > 25) {
					button.text = button.text.Substring(0, 25);
				}
			}
			Add(button);
		}

		void OnClick(MouseUpEvent mouseDownEvent) {
			var mPos = mouseDownEvent.mousePosition;
			if(UnityEditor.EditorWindow.focusedWindow != null) {
				mPos = (mouseDownEvent.currentTarget as VisualElement).GetScreenMousePosition(
					mouseDownEvent.localMousePosition,
					UnityEditor.EditorWindow.focusedWindow);
			}
			onClick?.Invoke();
		}
	}
}