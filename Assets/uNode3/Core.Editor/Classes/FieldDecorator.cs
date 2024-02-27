using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MaxyGames.UNode.Editors {
    /// <summary>
	/// The decorator for Field Control
	/// </summary>
	public abstract class FieldDecorator {
		public virtual int order => 0;

		public abstract bool IsValid(Type type);

		public abstract void Draw(object attribute);

		private static List<FieldDecorator> _fieldDecorators;
		public static List<FieldDecorator> FindDecorators() {
			if(_fieldDecorators == null) {
				_fieldDecorators = new List<FieldDecorator>();
				foreach(var assembly in EditorReflectionUtility.GetAssemblies()) {
					try {
						foreach(System.Type type in EditorReflectionUtility.GetAssemblyTypes(assembly)) {
							if(type.IsSubclassOf(typeof(FieldDecorator)) && ReflectionUtils.CanCreateInstance(type)) {
								var control = ReflectionUtils.CreateInstance(type) as FieldDecorator;
								_fieldDecorators.Add(control);
							}
						}
					}
					catch { continue; }
				}
				_fieldDecorators.Sort((x, y) => CompareUtility.Compare(x.order, y.order));
			}
			return _fieldDecorators;
		}

		private static Dictionary<Type, FieldDecorator> _fieldDecoratorMap = new Dictionary<Type, FieldDecorator>();
		public static FieldDecorator FindDecorator(Type type) {
			FieldDecorator decorator;
			if(_fieldDecoratorMap.TryGetValue(type, out decorator)) {
				return decorator;
			}
			var controls = FindDecorators();
			for(int i=0;i<controls.Count;i++) {
				if(controls[i].IsValid(type)) {
					decorator = controls[i];
					break;
				}
			}
			_fieldDecoratorMap[type] = decorator;
			return decorator;
		}

		/// <summary>
		/// Draw the field decorators
		/// </summary>
		/// <param name="attributes"></param>
		public static void DrawDecorators(object[] attributes) {
			if(attributes == null) return;
			foreach(var att in attributes) {
				if(att == null || !(att is PropertyAttribute)) continue;
				var decor = FindDecorator(att.GetType());
				if(decor != null) {
					decor.Draw(att);
				}
			}
		}
	}

	public abstract class FieldDecorator<T> : FieldDecorator where T : PropertyAttribute {
		public override bool IsValid(Type type) {
			return type == typeof(T);
		}
	}

	internal class SpaceDecorator : FieldDecorator<SpaceAttribute> {
		public override void Draw(object attribute) {
			// GUILayout.Space((attribute as SpaceAttribute).height);
			uNodeGUIUtility.GetRect(EditorGUIUtility.labelWidth, (attribute as SpaceAttribute).height);
		}
	}

	internal class HeaderDecorator : FieldDecorator<HeaderAttribute> {
		public override void Draw(object attribute) {
			EditorGUILayout.Space(6);
            EditorGUI.LabelField(uNodeGUIUtility.GetRect(), (attribute as HeaderAttribute).header, EditorStyles.boldLabel);
		}
	}
}