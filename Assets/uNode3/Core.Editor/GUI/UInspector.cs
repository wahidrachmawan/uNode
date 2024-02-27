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
			Draw(property, label: null);
		}

		public static void Draw(DrawerOption option) {
			if(option.property == null) return;
			if(option.property.isRoot) {
				UPropertyDrawer.DrawChilds(option);
			} else {
				var drawer = UPropertyDrawer.FindDrawer(option.type, true);
				if(drawer != null) {
					drawer.DrawLayouted(option);
				} else {

				}
			}
		}

		public static void Draw(DrawerOption option, Type type) {
			if(option.property == null) return;
			if(option.property.isRoot) {
				UPropertyDrawer.DrawChilds(option);
			} else {
				var drawer = UPropertyDrawer.FindDrawer(type, true);
				if(drawer != null) {
					drawer.DrawLayouted(option);
				} else {

				}
			}
		}

		public static void Draw(UBind property, bool nullable = false, bool acceptUnityObject = true, GUIContent label = null, Type type = null) {
			if(property == null) return;
			if(type == null) {
				Draw(new DrawerOption() {
					property = property,
					nullable = nullable,
					acceptUnityObject = acceptUnityObject,
					label = label,
				});
			} else {
				Draw(new DrawerOption() {
					property = property,
					nullable = nullable,
					acceptUnityObject = acceptUnityObject,
					label = label,
				}, type);
			}
		}

		public static void DrawChilds(DrawerOption option) {
			UPropertyDrawer.DrawChilds(option);
		}

		public static void DrawChilds(UBind property, bool nullable = false, bool acceptUnityObject = true) {
			UPropertyDrawer.DrawChilds(new DrawerOption() {
				property = property,
				nullable = nullable,
				acceptUnityObject = acceptUnityObject,
			});
		}
	}
}