using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MaxyGames.UNode.Editors {
	/// <summary>
	/// The field control for editing values
	/// </summary>
	public abstract class FieldControl {
		public virtual int order => 0;
		
		public abstract bool IsValidControl(Type type, bool layouted);

		public virtual void Draw(Rect position, GUIContent label, object value, Type type, Action<object> onChanged, uNodeUtility.EditValueSettings settings) {

		}

		public virtual float GetControlHeight(GUIContent label, object value, Type type, uNodeUtility.EditValueSettings settings) {
			return 18f;
		}
		
		public virtual void DrawLayouted(object value, GUIContent label, Type type, Action<object> onChanged, uNodeUtility.EditValueSettings settings) {
			DrawDecorators(settings);
			if(string.IsNullOrEmpty(label.tooltip)) {
				label.tooltip = settings?.Tooltip;
			}
			Draw(uNodeGUIUtility.GetRect(EditorGUIUtility.labelWidth, GetControlHeight(label, value, type, settings)), label, value, type, onChanged, settings);
		}

		protected void DrawDecorators(uNodeUtility.EditValueSettings settings) {
			if(settings.drawDecorator)
				FieldDecorator.DrawDecorators(settings.attributes);
		}

		protected void ValidateValue<T>(ref object value, bool nullable = false) {
			if (!(value is T)) {
				if (value != null && value.GetType().IsCastableTo(typeof(T))) {
					value = (T)value;
					GUI.changed = true;
				} else {
					value = default(T);
					if(value == null && !nullable && ReflectionUtils.CanCreateInstance(typeof(T))) {
						value = ReflectionUtils.CreateInstance(typeof(T));
					}
					GUI.changed = value != null;
				}
			}
		}

		protected void ValidateValue(ref object value, Type type, bool nullable = false) {
			if(value == null || value.GetType().IsCastableTo(type) == false) {
				if(value != null && value.GetType().IsCastableTo(type)) {
					value = Operator.Convert(value, type);
					GUI.changed = true;
				}
				else {
					if(!nullable && ReflectionUtils.CanCreateInstance(type) || type.IsValueType) {
						value = ReflectionUtils.CreateInstance(type);
					}
					else {
						value = null;
					}
					GUI.changed = value != null;
				}
			}
		}

		private static List<FieldControl> _fieldControls;
		public static List<FieldControl> FindControls() {
			if(_fieldControls == null) {
				_fieldControls = new List<FieldControl>();
				foreach(var assembly in EditorReflectionUtility.GetAssemblies()) {
					try {
						foreach(System.Type type in EditorReflectionUtility.GetAssemblyTypes(assembly)) {
							if(type.IsSubclassOf(typeof(FieldControl)) && ReflectionUtils.CanCreateInstance(type)) {
								var control = ReflectionUtils.CreateInstance(type) as FieldControl;
								_fieldControls.Add(control);
							}
						}
					}
					catch { continue; }
				}
				_fieldControls.Sort((x, y) => CompareUtility.Compare(x.order, y.order));
			}
			return _fieldControls;
		}

		private static Dictionary<Type, FieldControl> _fieldControlMap = new Dictionary<Type, FieldControl>();
		private static Dictionary<Type, FieldControl> _fieldLayoutedControlMap = new Dictionary<Type, FieldControl>();
		private static FieldControl unsupportedControl = new Control.UnsupportedFieldControl();

		public static FieldControl FindControl(Type type, bool layouted) {
			if(type == null) return unsupportedControl;
			FieldControl control;
			if(layouted) {
				if(_fieldLayoutedControlMap.TryGetValue(type, out control)) {
					return control;
				}
			} else {
				if(_fieldControlMap.TryGetValue(type, out control)) {
					return control;
				}
			}
			var controls = FindControls();
			for(int i=0;i<controls.Count;i++) {
				if(controls[i].IsValidControl(type, layouted)) {
					control = controls[i];
					break;
				}
			}
			if(layouted) {
				_fieldLayoutedControlMap[type] = control;
			} else {
				_fieldControlMap[type] = control;
			}
			return control;
		}
	}
	
	public abstract class FieldControl<T> : FieldControl {
		public override bool IsValidControl(Type type, bool layouted) {
			if (type == typeof(T)) {
				return true;
			}
			return false;
		}

		protected void ValidateValue(ref object value, bool nullable = false) {
			if (!(value is T)) {
				if (value != null && value.GetType().IsCastableTo(typeof(T))) {
					value = Operators.Convert(value, typeof(T));
					GUI.changed = true;
				} else {
					value = default(T);
					if(value == null && !nullable && ReflectionUtils.CanCreateInstance(typeof(T))) {
						value = ReflectionUtils.CreateInstance(typeof(T));
					}
					GUI.changed |= value != null;
				}
			}
		}
	}
}

namespace MaxyGames.UNode.Editors.Control {
	class UnsupportedFieldControl : FieldControl {
		public override bool IsValidControl(Type type, bool layouted) {
			return false;
		}

		public override void Draw(Rect position, GUIContent label, object value, Type type, Action<object> onChanged, uNodeUtility.EditValueSettings settings) {
			position = EditorGUI.PrefixLabel(position, label);
			EditorGUI.SelectableLabel(position, label.text);
		}
	}
}