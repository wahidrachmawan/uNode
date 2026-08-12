using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Xml;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace MaxyGames.UNode.Editors {
	public static class EditorExtensions {
		public static void AppendMenu(this GenericMenu menu, IEnumerable<DropdownMenuItem> menuItems) {
			foreach(var menuItem in menuItems) {
				if(menuItem is DropdownMenuAction action) {
					switch(action.status) {
						case DropdownMenuAction.Status.None:
						case DropdownMenuAction.Status.Normal:
						case DropdownMenuAction.Status.Checked:
							menu.AddItem(new GUIContent(action.name), action.status == DropdownMenuAction.Status.Checked, () => {
								action.Execute();
							});
							break;
						case DropdownMenuAction.Status.Disabled:
							menu.AddDisabledItem(new GUIContent(action.name));
							break;
					}
				}
				else if(menuItem is DropdownMenuSeparator separator) {
					menu.AddSeparator(separator.subMenuPath);
				}
			}
		}

		/// <summary>
		/// inserts value into list using a custom binary-like search based on IComparable<T> results (preserves ordering)
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="list"></param>
		/// <param name="value"></param>
		public static void AddItemSorted<T>(this List<T> list, T value) where T : IComparable<T> {
			int index = list.FindLastIndex(item => value.CompareTo(item) == 0);
			if(index < 0) {
				index = list.FindIndex(item => value.CompareTo(item) > 0);
				if(index > 0) {
					index--;
				}
			}
			else {
				index++;
			}
			if(index >= 0 && list.Count > index) {
				list.Insert(index, value);
			}
			else if(index == -1 && list.Count > 0) {
				list.Insert(0, value);
			}
			else {
				list.Add(value);
			}
		}

		/// <summary>
		/// Convert rect into Screen rect
		/// </summary>
		/// <param name="rect"></param>
		/// <returns></returns>
		public static Rect ToScreenRect(this Rect rect) {
			return uNodeGUIUtility.GUIToScreenRect(rect);
		}

		/// <summary>
		/// Convert point into screen point.
		/// </summary>
		/// <param name="point"></param>
		/// <returns></returns>
        public static Vector2 ToScreenPoint(this Vector2 point) {
			return GUIUtility.GUIToScreenPoint(point);
		}

		/// <summary>
		/// Convert point into screen point.
		/// </summary>
		/// <param name="point"></param>
		/// <returns></returns>
		public static Vector2 ToScreenPoint(this Vector3 point) {
			return GUIUtility.GUIToScreenPoint(point);
		}

		public static UnityEngine.Object GetUnityObject(this UGraphElement graphElement) {
			if(graphElement == null) {
				throw new ArgumentNullException(nameof(graphElement));
			}
			return graphElement.graphContainer as UnityEngine.Object;
		}

		public static UnityEngine.Object GetUnityObject(this Node node) {
			if(node == null) {
				throw new ArgumentNullException(nameof(node));
			}
			return node.nodeObject.graphContainer as UnityEngine.Object;
		}
	}
}
