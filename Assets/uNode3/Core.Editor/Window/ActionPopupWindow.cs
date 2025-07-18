using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace MaxyGames.UNode.Editors {
	/// <summary>
	/// Provide useful function for show custom in popup window.
	/// </summary>
	public class ActionPopupWindow : BaseActionWindow<ActionPopupWindow> {
		public float width = 350, height = 200, maxWidth = 400, maxHeight = 600;
		public bool autoSize = true;

		protected override void Initialize() {
			if(focusedWindow != null) {
				Rect screenRect = focusedWindow.position; // screen-space rect
				Vector2 center = new Vector2(screenRect.x + screenRect.width / 2f, screenRect.y + screenRect.height / 2f);
				ShowAsDropDown(new Rect(center, Vector2.zero), new Vector2(width, height));
			}
			else {
				ShowAsDropDown(Rect.zero, new Vector2(width, height));
			}
			wantsMouseMove = true;
			Focus();
		}

		public override void OnEnable() {
			base.OnEnable();
			var layout = container.layout;
			var oldAutosize = !autoSize;

			Vector2 startPosition = position.position;
			container.ExecuteAndScheduleAction(() => {
				if(autoSize && layout != container.layout) {
					if(startPosition == Vector2.zero) {
						startPosition = position.position;
					}
					layout = container.layout;
					if(float.IsNaN(layout.width) || float.IsNaN(layout.height)) {
						return;
					}
					var pos = position;
					pos.width = Mathf.Clamp(layout.width, 50, maxWidth);
					pos.height = Mathf.Clamp(layout.height, 50, maxHeight) + 4;
					if(pos.width > position.width || pos.height > position.height) {
						pos.width = maxWidth;
						pos.height = maxHeight;
					}
					position = pos;
					pos.x = startPosition.x;
					pos.y = startPosition.y;
					this.ChangePosition(pos);
				}
				if(oldAutosize != autoSize) {
					oldAutosize = autoSize;
					if(autoSize) {
						IStyle style = container.style;
						style.position = new(StyleKeyword.Null);
						style.left = new(StyleKeyword.Null);
						style.top = new(StyleKeyword.Null);
						style.right = new(StyleKeyword.Null);
						style.bottom = new(StyleKeyword.Null);
					}
					else {
						container.StretchToParentSize();
					}
				}
			}, 1);
		}
	}
}