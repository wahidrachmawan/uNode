using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace MaxyGames.UNode.Editors.UIControl {
	[ControlField(typeof(UnityEngine.Object))]
	public class ObjectControl : ValueControl {
		public ObjectControl(ControlConfig config, bool autoLayout = false) : base(config, autoLayout) {
			Init(autoLayout);
		}

		void Init(bool autoLayout) {
			if(config.owner.nodeObject.graphContainer.IsNativeGraph() == false) {
				if(config.type is RuntimeType) {
					var field = new ObjectRuntimeField() {
						objectType = config.type as RuntimeType,
						value = config.value as UnityEngine.Object,
						allowSceneObjects = uNodeEditorUtility.IsSceneObject(config.targetObject)
					};
					field.RegisterValueChangedCallback((e) => {
						config.OnValueChanged(e.newValue);
						MarkDirtyRepaint();
					});
					Add(field);
				}
				else {
					ObjectField field = new ObjectField() {
						objectType = config.type,
						value = config.value as UnityEngine.Object,
					};
					field.RegisterValueChangedCallback((e) => {
						config.OnValueChanged(e.newValue);
						MarkDirtyRepaint();
					});
					field.allowSceneObjects = uNodeEditorUtility.IsSceneObject(config.targetObject);
					Add(field);
				}
			}
			else {
				var button = new Label() { text = "null" };
				button.AddToClassList("PopupButton");
				button.EnableInClassList("Layout", autoLayout);
				Add(button);
				if(config.value != null) {
					try {
						config.OnValueChanged(null);
					}
					catch { }
				}
			}
		}
	}

	
	public class ObjectRuntimeField : BaseField<Object> {
		public override void SetValueWithoutNotify(Object newValue) {
			newValue = TryReadComponentFromGameObject(newValue, objectType);
			var valueChanged = !EqualityComparer<Object>.Default.Equals(this.value, newValue);

			base.SetValueWithoutNotify(newValue);

			if (valueChanged) {
				m_ObjectFieldDisplay.Update();
			}
		}

		private RuntimeType m_objectType;

		public RuntimeType objectType {
			get { return m_objectType; }
			set {
				if (m_objectType != value) {
					m_objectType = value;
					m_ObjectFieldDisplay.Update();
				}
			}
		}

		public bool allowSceneObjects { get; set; }

		private class ObjectFieldDisplay : VisualElement {
			private readonly ObjectRuntimeField m_ObjectField;
			private readonly Image m_ObjectIcon;
			private readonly Label m_ObjectLabel;

			public static readonly string ussClassName = "unity-object-field-display";
			public static readonly string iconUssClassName = ussClassName + "__icon";
			public static readonly string labelUssClassName = ussClassName + "__label";
			public static readonly string acceptDropVariantUssClassName = ussClassName + "--accept-drop";

			public ObjectFieldDisplay(ObjectRuntimeField objectField) {
				AddToClassList(ussClassName);
				m_ObjectIcon = new Image { scaleMode = ScaleMode.ScaleAndCrop, pickingMode = PickingMode.Ignore };
				m_ObjectIcon.AddToClassList(iconUssClassName);
				m_ObjectLabel = new Label { pickingMode = PickingMode.Ignore };
				m_ObjectLabel.AddToClassList(labelUssClassName);
				m_ObjectField = objectField;

				Update();

				Add(m_ObjectIcon);
				Add(m_ObjectLabel);
			}

			public void Update() {
				var value = m_ObjectField.value;
				var type = m_ObjectField.objectType;
				string name;
				if(value != null) {
					if(value is Component) {
						name = (value as Component).gameObject.name + $" ({type?.Name})";
					} else if(value is ScriptableObject) {
						name = (value as ScriptableObject).name + $"({type?.Name})";
					} else {
						name = value.ToString();
					}
				} else {
					name = $"None ({type?.Name})";
				}
				m_ObjectIcon.image = uNodeEditorUtility.GetTypeIcon(type);
				m_ObjectLabel.text = name;
			}

			protected override void ExecuteDefaultActionAtTarget(EventBase evt) {
				base.ExecuteDefaultActionAtTarget(evt);

				if (evt == null) {
					return;
				}

				if ((evt as MouseDownEvent)?.button == (int)MouseButton.LeftMouse)
					OnMouseDown(evt as MouseDownEvent);
				else if (evt.eventTypeId == KeyDownEvent.TypeId()) {
					var kdEvt = evt as KeyDownEvent;

					if (((evt as KeyDownEvent)?.keyCode == KeyCode.Space) ||
						((evt as KeyDownEvent)?.keyCode == KeyCode.KeypadEnter) ||
						((evt as KeyDownEvent)?.keyCode == KeyCode.Return)) {
						OnKeyboardEnter();
					} else if (kdEvt.keyCode == KeyCode.Delete ||
							   kdEvt.keyCode == KeyCode.Backspace) {
						OnKeyboardDelete();
					}
				} else if (evt.eventTypeId == DragUpdatedEvent.TypeId())
					OnDragUpdated(evt);
				else if (evt.eventTypeId == DragPerformEvent.TypeId())
					OnDragPerform(evt);
				else if (evt.eventTypeId == DragLeaveEvent.TypeId())
					OnDragLeave();
			}

			private void OnDragLeave() {
				// Make sure we've cleared the accept drop look, whether we we in a drop operation or not.
				RemoveFromClassList(acceptDropVariantUssClassName);
			}

			private void OnMouseDown(MouseDownEvent evt) {
				Object actualTargetObject = m_ObjectField.value;
				Component com = actualTargetObject as Component;
				if (com)
					actualTargetObject = com.gameObject;

				if (actualTargetObject == null)
					return;

				// One click shows where the referenced object is, or pops up a preview
				if (evt.clickCount == 1) {
					// ping object
					bool anyModifiersPressed = evt.shiftKey || evt.ctrlKey;
					if (!anyModifiersPressed && actualTargetObject) {
						EditorGUIUtility.PingObject(actualTargetObject);
					}
					evt.StopPropagation();
				}
				// Double click opens the asset in external app or changes selection to referenced object
				else if (evt.clickCount == 2) {
					if (actualTargetObject) {
						AssetDatabase.OpenAsset(actualTargetObject);
						GUIUtility.ExitGUI();
					}
					evt.StopPropagation();
				}
			}

			private void OnKeyboardEnter() {
				m_ObjectField.ShowObjectSelector();
			}

			private void OnKeyboardDelete() {
				m_ObjectField.value = null;
			}

			private Object DNDValidateObject() {
				var reference = DragAndDrop.objectReferences.FirstOrDefault();

				if (reference != null) {
					if(reference is GameObject) {
						foreach(var c in (reference as GameObject).GetComponents<MonoBehaviour>()) {
							if(ReflectionUtils.IsValidRuntimeInstance(c, m_ObjectField.objectType)) {
								reference = c;
								break;
							}
						}
					}
					if (!ReflectionUtils.IsValidRuntimeInstance(reference, m_ObjectField.objectType)) {
						//uNodeEditorUtility.DisplayErrorMessage("Invalid dragged object.");
						return null;
					}
					// If scene objects are not allowed and object is a scene object then clear
					if (!m_ObjectField.allowSceneObjects && !EditorUtility.IsPersistent(reference))
						reference = null;
				}
				return reference;
			}

			private void OnDragUpdated(EventBase evt) {
				Object validatedObject = DNDValidateObject();
				if (validatedObject != null) {
					DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
					AddToClassList(acceptDropVariantUssClassName);

					evt.StopPropagation();
				}
			}

			private void OnDragPerform(EventBase evt) {
				Object validatedObject = DNDValidateObject();
				if (validatedObject != null) {
					DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
					m_ObjectField.value = validatedObject;

					DragAndDrop.AcceptDrag();
					RemoveFromClassList(acceptDropVariantUssClassName);

					evt.StopPropagation();
				}
			}
		}

		private class ObjectFieldSelector : VisualElement {
			private readonly ObjectRuntimeField m_ObjectField;

			public ObjectFieldSelector(ObjectRuntimeField objectField) {
				m_ObjectField = objectField;
			}

			protected override void ExecuteDefaultAction(EventBase evt) {
				base.ExecuteDefaultAction(evt);

				if ((evt as MouseDownEvent)?.button == (int)MouseButton.LeftMouse)
					m_ObjectField.ShowObjectSelector();
			}
		}

		private readonly ObjectFieldDisplay m_ObjectFieldDisplay;

		public new static readonly string ussClassName = "unity-object-field";
		public new static readonly string labelUssClassName = ussClassName + "__label";
		public new static readonly string inputUssClassName = ussClassName + "__input";

		public static readonly string objectUssClassName = ussClassName + "__object";
		public static readonly string selectorUssClassName = ussClassName + "__selector";

		public ObjectRuntimeField() : this(null) { }

		public ObjectRuntimeField(string label) : base(label, null) {
			var visualInput = Children().FirstOrDefault();
			if(visualInput == null) return;
			visualInput.focusable = false;
			labelElement.focusable = false;

			AddToClassList(ussClassName);
			labelElement.AddToClassList(labelUssClassName);

			allowSceneObjects = true;

			m_ObjectFieldDisplay = new ObjectFieldDisplay(this) { focusable = true };
			m_ObjectFieldDisplay.AddToClassList(objectUssClassName);
			var objectSelector = new ObjectFieldSelector(this);
			objectSelector.AddToClassList(selectorUssClassName);
			visualInput.AddToClassList(inputUssClassName);
			visualInput.Add(m_ObjectFieldDisplay);
			visualInput.Add(objectSelector);
		}

		private void OnObjectChanged(Object obj) {
			value = TryReadComponentFromGameObject(obj, objectType);
		}

		internal void ShowObjectSelector() {
			// Since we have nothing useful to do on the object selector closing action, we just do not assign any callback
			// All the object changes will be notified through the OnObjectChanged and a "cancellation" (Escape key) on the ObjectSelector is calling the closing callback without any good object
			var screenPos = this.GetScreenMousePosition(Vector2.zero, EditorWindow.focusedWindow);
			var items = new List<ItemSelector.CustomItem>();
			items.Add(ItemSelector.CustomItem.Create("None", () => {
				OnObjectChanged(null);
			}, "#"));
			items.AddRange(ItemSelector.MakeCustomItemsForInstancedType(objectType, (val) => {
				OnObjectChanged(val as Object);
			}, allowSceneObjects));
			ItemSelector.ShowCustomItem(items).ChangePosition(screenPos);
		}

		private Object TryReadComponentFromGameObject(Object obj, Type type) {
			var go = obj as GameObject;
			if (go != null && type != null) {
				var comps = go.GetComponents<MonoBehaviour>();
				foreach(var comp in comps) {
					if (ReflectionUtils.IsValidRuntimeInstance(comp, objectType)) {
						return comp;
					}
				}
			}
			return obj;
		}
	}
}