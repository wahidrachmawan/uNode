using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Xml;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MaxyGames.UNode.Editors {
	public static class EditorExtensions {
		//public static bool IsRuntimeGraph(this uNodeComponentSystem graph) {
		//	return graph is IIndependentGraph;
		//}

		///// <summary>
		///// Get the persistence object if any
		///// </summary>
		///// <param name="obj"></param>
		///// <typeparam name="T"></typeparam>
		///// <returns></returns>
		//public static T GetPersistenceObject<T>(this T obj) where T : UnityEngine.Object {
		//	return uNodeUtility.GetActualObject(obj);
		//}

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
