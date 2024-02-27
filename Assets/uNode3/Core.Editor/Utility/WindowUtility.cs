using UnityEngine;
using UnityEditor;

namespace MaxyGames.UNode.Editors {
	public static class WindowUtility {
		/// <summary>
		/// Convert mouse position to rect.
		/// </summary>
		/// <param name="mPos"></param>
		/// <param name="windowSize"></param>
		/// <returns></returns>
		public static Rect MousePosToRect(Vector2 mPos, Vector2 windowSize) {
			return new Rect(new Vector2(mPos.x + windowSize.x, mPos.y), new Vector2(windowSize.x, windowSize.y));
		}

		/// <summary>
		/// Get the mouse position for context menu.
		/// </summary>
		/// <param name="mousePos"></param>
		/// <returns></returns>
		public static Vector2 GetMousePositionForMenu(this EditorWindow window, Vector2 mousePos) {
			if(window == null)
				return mousePos;
			return new Vector2(mousePos.x + window.position.x, mousePos.y + window.position.y);
		}

		public static Vector2 ScreenToWindow(this EditorWindow window, Vector2 mousePos) {
			if(window == null) {
				return mousePos;
			}
			return new Vector2(mousePos.x - window.position.x, mousePos.y - window.position.y);
		}

		/// <summary>
		/// Change the Editor Window position
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="window"></param>
		/// <param name="position"></param>
		/// <returns></returns>
		public static T ChangePosition<T>(this T window, Vector2 position, bool showAsDropDown = true) where T : EditorWindow {
			if(showAsDropDown) {
				// var oldPos = new Rect(position, new Vector2(30, 30));
				window.ShowAsDropDown(new Rect(position.x, position.y, window.position.width, 0), window.position.size);
				// position = window.position.position;
				// if(!window.position.Overlaps(new Rect(oldPos.position - (oldPos.size / 2), oldPos.size))) {
				// 	position = oldPos.position;
				// }
				if (position.x < Screen.currentResolution.width && position.y < Screen.currentResolution.height)
				{
					if (position.x + window.position.width > Screen.currentResolution.width)
					{
						position.x = Screen.currentResolution.width - window.position.width;
					}
					if (position.y + window.position.height + 30 > Screen.currentResolution.height)
					{
						position.y = Screen.currentResolution.height - window.position.height - 30;
					}
			   		window.position = new Rect(position, window.position.size);
				}
			} else if(position.x < Screen.currentResolution.width && position.y < Screen.currentResolution.height) {
                if (position.x + window.position.width > Screen.currentResolution.width)
                {
                    position.x = Screen.currentResolution.width - window.position.width;
                }
                if (position.y + window.position.height + 30 > Screen.currentResolution.height)
                {
                    position.y = Screen.currentResolution.height - window.position.height - 30;
                }
                window.position = new Rect(position, window.position.size);
            }
                return window;
        }

		/// <summary>
		/// Change the Editor Window position
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="window"></param>
		/// <param name="position"></param>
		/// <returns></returns>
		public static T ChangePosition<T>(this T window, Rect position, bool showAsDropDown = true) where T : EditorWindow {
			if(showAsDropDown) {
				window.ShowAsDropDown(new Rect(position.x, position.y, window.position.width, 0), window.position.size);
				if (position.x < Screen.currentResolution.width && position.y < Screen.currentResolution.height)
				{
					if (position.x + window.position.width > Screen.currentResolution.width)
					{
						position.x = Screen.currentResolution.width - window.position.width;
					}
					if (position.y + window.position.height + 30 > Screen.currentResolution.height)
					{
						position.y = Screen.currentResolution.height - window.position.height - 30;
					}
			   		window.position = new Rect(position.position, window.position.size);
				}
			} else if(position.x < Screen.currentResolution.width && position.y < Screen.currentResolution.height){
                if (position.x + window.position.width > Screen.currentResolution.width)
                {
                    position.x = Screen.currentResolution.width - window.position.width;
                }
                if (position.y + window.position.height + 30 > Screen.currentResolution.height)
                {
                    position.y = Screen.currentResolution.height - window.position.height - 30;
                }
                window.position = new Rect(position.position, window.position.size);
            }
			return window;
        }

		public static void CloseAllOpenWindows<T>() {
			var windows = Resources.FindObjectsOfTypeAll(typeof(T));
			foreach (var window in windows) {
				try {
					((EditorWindow)window).Close();
				} catch {
					Object.DestroyImmediate(window);
				}
			}
		}
	}
}