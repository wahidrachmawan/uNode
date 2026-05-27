using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace MaxyGames.UNode.Editors {
	public static class UInspector {
		public static void Draw(UBind property) {
			Draw(property, false, true, null);
		}

		public static void Draw(UBind property, GUIContent label) {
			Draw(property, false, true, label);
		}

		public static void Draw(DrawerOption option) {
			if(option.property == null) return;
			if(option.property.isRoot) {
				UPropertyDrawer.DrawChilds(ref option);
			}
			else {
				var drawer = UPropertyDrawer.FindDrawer(option.type, true);
				if(drawer != null) {
					drawer.DrawLayouted(ref option);
				}
				else {

				}
			}
		}

		public static void Draw(DrawerOption option, Type type) {
			if(option.property == null) return;
			if(option.property.isRoot) {
				UPropertyDrawer.DrawChilds(ref option);
			}
			else {
				var drawer = UPropertyDrawer.FindDrawer(type, true);
				if(drawer != null) {
					drawer.DrawLayouted(ref option);
				}
				else {

				}
			}
		}

		public static void Draw(UBind property, bool nullable = false, bool acceptUnityObject = true, GUIContent label = null, Type type = null, BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy) {
			if(property == null) return;
			if(type == null) {
				Draw(new DrawerOption() {
					property = property,
					nullable = nullable,
					acceptUnityObject = acceptUnityObject,
					label = label,
					flags = flags,
				});
			}
			else {
				Draw(new DrawerOption() {
					property = property,
					nullable = nullable,
					acceptUnityObject = acceptUnityObject,
					label = label,
					flags = flags,
				}, type);
			}
		}

		public static void EditValueLayouted(GUIContent label,
			object fieldValue,
			Type type,
			Action<object> onChange = null,
			EditValueSettings settings = null) {
			uNodeGUIUtility.EditValueLayouted(label, fieldValue, type, onChange, settings);
		}

		public static void EditValueLayouted<T>(GUIContent label,
			T fieldValue,
			Action<T> onChange = null,
			EditValueSettings settings = null) {
			uNodeGUIUtility.EditValueLayouted(label, fieldValue, onChange, settings);
		}

		public static void DrawChilds(DrawerOption option) {
			UPropertyDrawer.DrawChilds(ref option);
		}

		public static void DrawChilds(UBind property, bool nullable = false, bool acceptUnityObject = true, BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy) {
			var option = new DrawerOption() {
				property = property,
				nullable = nullable,
				acceptUnityObject = acceptUnityObject,
				flags = flags,
			};
			UPropertyDrawer.DrawChilds(ref option);
		}
	}
}