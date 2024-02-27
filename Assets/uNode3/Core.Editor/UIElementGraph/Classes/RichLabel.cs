using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace MaxyGames.UNode.Editors {
	public class PopupElement : VisualElement {
		private RichLabel richLabel;
		private TextElement label;

		private bool _richText;
		public bool richText {
			get {
				return _richText;
			}
			set {
				if(value) {
					if(label != null) {
						label.RemoveFromHierarchy();
						label = null;
					}
				} else {
					if(richLabel != null) {
						richLabel.RemoveFromHierarchy();
						richLabel = null;
					}
				}
				_richText = value;
			}
		}

		public string text {
			get {
				if(richText) {
					return richLabel != null ? richLabel.text : string.Empty;
				} else {
					return label != null ? label.text : string.Empty;
				}
			}
			set {
				if (_richText) {
					if (string.IsNullOrEmpty(value)) {
						if (richLabel != null) {
							richLabel.RemoveFromHierarchy();
							richLabel = null;
						}
						return;
					} else if (richLabel == null) {
						Insert(0, richLabel = new RichLabel());
					}
					richLabel.text = value;
				} else {
					if (string.IsNullOrEmpty(value)) {
						if (label != null) {
							label.RemoveFromHierarchy();
							label = null;
						}
						return;
					} else if (label == null) {
						Insert(0, label = new TextElement());
					}
					label.text = value;
				}
			}
		}

		public PopupElement() {
			Init();
		}

		public PopupElement(string text) {
			Init();
			this.text = text;
		}

		void Init() {
			focusable = false;
			pickingMode = PickingMode.Ignore;
			AddToClassList("unity-base-field");
			AddToClassList("unity-base-field__input");
			AddToClassList("unity-enum-field__input");
			var popup = new VisualElement();
			popup.AddToClassList("unity-enum-field__arrow");
			Add(popup);
		}
	}

	public class RichLabel : IMGUIContainer {
		private Func<string> _getText;
		private string _text;
		public string text {
			get {
				if(_getText != null) {
					return _getText();
				}
				return _text;
			}
			set {
				if(_text != value || _getText != null) {
					_getText = null;
					_text = value;
					MarkDirtyLayout();
				}
			}
		}

		public GUIStyle textStyle;

		public RichLabel() {
			Init();
		}

		public RichLabel(string text) {
			Init();
			this.text = text;
		}

		public RichLabel(Func<string> text) {
			Init();
			_getText = text;
		}

		void Init() {
			focusable = false;
			pickingMode = PickingMode.Ignore;
			AddToClassList("unity-text-element");
			AddToClassList("unity-label");
			onGUIHandler = () => {
				if(textStyle == null) {
					UpdateStyle();
				}
				Rect rect = uNodeGUIUtility.GetRect(layout.width, layout.height);
				EditorGUI.LabelField(rect, text, textStyle);
			};
			this.ScheduleAction(() => {
				if(textStyle != null) {
					UpdateStyle();
				}
			}, 1000);
		}

		void UpdateStyle() {
			textStyle = new GUIStyle {
				richText = true,
				alignment = resolvedStyle.unityTextAlign,
				font = resolvedStyle.unityFont,
				fontSize = (int)resolvedStyle.fontSize,
				fontStyle = resolvedStyle.unityFontStyleAndWeight
			};
			textStyle.normal.textColor = resolvedStyle.color;
			//textStyle.margin = new RectOffset(
			//	(int)resolvedStyle.marginLeft, 
			//	(int)resolvedStyle.marginRight, 
			//	(int)resolvedStyle.marginTop,
			//	(int)resolvedStyle.marginBottom);
			textStyle.padding = new RectOffset(
				(int)resolvedStyle.paddingLeft,
				(int)resolvedStyle.paddingRight,
				(int)resolvedStyle.paddingTop,
				(int)resolvedStyle.paddingBottom);
		}

		protected override Vector2 DoMeasure(float desiredWidth, MeasureMode widthMode, float desiredHeight, MeasureMode heightMode) {
			if(textStyle == null) {
				UpdateStyle();
			}
			float num = float.NaN;
			float num2 = float.NaN;
			if(widthMode != MeasureMode.Exactly || heightMode != MeasureMode.Exactly) {
				Vector2 vec = textStyle.CalcSize(new GUIContent(text));
				Rect layout = base.layout;
				if(widthMode == MeasureMode.Exactly) {
					layout.width = desiredWidth;
				}
				if(heightMode == MeasureMode.Exactly) {
					layout.height = desiredHeight;
				}
				num = vec.x;
				num2 = vec.y;
			}
			switch(widthMode) {
				case MeasureMode.Exactly:
					num = desiredWidth;
					break;
				case MeasureMode.AtMost:
					num = Mathf.Min(num, desiredWidth);
					break;
			}
			switch(heightMode) {
				case MeasureMode.Exactly:
					num2 = desiredHeight;
					break;
				case MeasureMode.AtMost:
					num2 = Mathf.Min(num2, desiredHeight);
					break;
			}
			return new Vector2(num, num2);
		}
	}
}