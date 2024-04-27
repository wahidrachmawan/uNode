using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace MaxyGames.UNode.Editors {
	public struct DrawerOption {
		public UBind property;
		public bool nullable;
		public bool acceptUnityObject;
		public Action<object> onChanged;
		private GUIContent _label;

		public GUIContent label {
			get {
				if(_label != null)
					return _label;
				return property.label;
			}
			set {
				_label = value;
			}
		}
		public Type type => property.type;
		public object value {
			get => property.value;
			set {
				property.value = value;
				onChanged?.Invoke(value);
			}
		}
		public Attribute[] attributes => property.GetCustomAttributes();
		public UnityEngine.Object unityObject => property.root.value as UnityEngine.Object;
		public void RegisterUndo(string name = "") => property.RegisterUndo(name);

		#region Constructors
		public DrawerOption(UBind property, bool nullable, bool acceptUnityObject, GUIContent label = null, Action<object> onChanged = null) {
			this.property = property;
			this.nullable = nullable;
			this.acceptUnityObject = acceptUnityObject;
			this._label = label;
			this.onChanged = onChanged;
		}
		#endregion
	}

	public abstract class UPropertyDrawer {
		public virtual int order => 0;

		public abstract bool IsValid(Type type, bool layouted);

		public virtual void Draw(Rect position, DrawerOption option) {

		}

		public virtual void DrawLayouted(DrawerOption option) {
			DrawDecorators(option);
			Draw(uNodeGUIUtility.GetRect(), option);
		}

		protected void DrawDecorators(DrawerOption option) {
			FieldDecorator.DrawDecorators(option.attributes);
		}

		public static void DrawChilds(DrawerOption option) {
			var fields = EditorReflectionUtility.GetFields(option.property.valueType);
			for(int i = 0; i < fields.Length; i++) {
				if(fields[i].IsDefined(typeof(HideInInspector), true) || 
					fields[i].IsPrivate && fields[i].IsNotSerialized && !fields[i].IsDefined(typeof(SerializeField), true))
					continue;
				if(fields[i].IsDefined(typeof(NonSerializedAttribute)))
					continue;
				if(fields[i].IsDefined(typeof(HideAttribute), true)) {
					var atts = fields[i].GetCustomAttributes<HideAttribute>();
					bool hide = false;
					foreach(var att in atts) {
						if(string.IsNullOrEmpty(att.targetField)) {
							hide = true;
							break;
						}
						else {
							var tField = fields.FirstOrDefault(f => f.Name == att.targetField);
							if(tField != null) {
								var tValue = tField.GetValueOptimized(option.property.value);
								if(tValue != null) {
									bool isHide = false;
									bool same = true;
									Type targetRefType = tValue.GetType();
									if(targetRefType == typeof(MemberData)) {
										var fieldVal = tValue as MemberData;
										if(fieldVal != null) {
											if(att.hideValue == null) {
												same = (!fieldVal.isAssigned || !fieldVal.TargetSerializedType.isFilled);
												if(att.hideOnSame && same) {
													isHide = true;
												}
												else if(!att.hideOnSame && !same) {
													isHide = true;
												}
											}
											else if(att.hideValue != null && (!att.hideOnSame || fieldVal.isAssigned && fieldVal.TargetSerializedType.isFilled)) {
												Type validType = fieldVal.type;
												if(validType != null) {
													if(att.elementType && (validType.IsArray || validType.IsGenericType)) {
														if(validType.IsArray) {
															validType = validType.GetElementType();
														}
														else {
															validType = validType.GetGenericArguments()[0];
														}
													}
												}
												if(att.hideValue is Type) {
													same = ((Type)att.hideValue) == validType || validType.IsCastableTo((Type)att.hideValue);
													if(att.hideOnSame && same) {
														isHide = true;
													}
													else if(!att.hideOnSame && !same) {
														isHide = true;
													}
												}
												else if(att.hideValue is Type[]) {
													Type[] hT = att.hideValue as Type[];
													for(int x = 0; x < hT.Length; x++) {
														same = hT[x] == validType || validType.IsCastableTo(hT[x]);
														if(att.hideOnSame && same) {
															isHide = true;
															break;
														}
														else if(!att.hideOnSame) {
															if(!same) {
																isHide = true;
																continue;
															}
															else {
																isHide = false;
																break;
															}
														}
													}
												}
											}
										}
									}
									else {
										same = tValue.Equals(att.hideValue);
										if(att.hideOnSame && same) {
											isHide = true;
										}
										else if(!att.hideOnSame && !same) {
											isHide = true;
										}
									}
									if(isHide) {
										if(att.defaultOnHide && tValue != att.defaultValue) {
											fields[i].SetValueOptimized(option.property.value, att.defaultValue);
										}
										hide = true;
										break;
									}
								}
								else {
									throw null;
								}
							}
						}
					}
					if(hide) {
						continue;
					}
				}
				if(fields[i].FieldType.IsDefined(typeof(GraphElementAttribute), true)) {
					continue;
				}
				UInspector.Draw(new DrawerOption() {
					property = option.property[fields[i].Name],
					nullable = option.nullable,
					acceptUnityObject = option.acceptUnityObject,
				});
			}
		}

		protected T GetValue<T>(UBind property, bool nullable = false) {
			var value = property.value;
			if(!(value is T)) {
				if(value != null && value.GetType().IsCastableTo(typeof(T))) {
					property.value = Operator.Convert<T>(value);
					GUI.changed = true;
					return (T)value;
				} else {
					T val = default(T);
					if(value == null && !nullable && ReflectionUtils.CanCreateInstance(typeof(T))) {
						val = (T)ReflectionUtils.CreateInstance(typeof(T));
						property.value = val;
					}
					if(object.ReferenceEquals(value, val) == false) {
						GUI.changed = value != null;
					}
					return val;
				}
			}
			if(value != null) {
				return (T)value;
			} else {
				return default;
			}
		}

		protected object GetValue(UBind property, Type type, bool nullable = false) {
			var value = property.value;
			if(value == null || !value.GetType().IsCastableTo(type)) {
				if(value != null && value.GetType().IsCastableTo(type)) {
					value = Operator.Convert(value, type);
					property.value = value;
					GUI.changed = true;
				} else {
					value = null;
					if(value == null && !nullable && ReflectionUtils.CanCreateInstance(type)) {
						value = ReflectionUtils.CreateInstance(type);
						property.value = value;
					}
					GUI.changed = value != null;
				}
			}
			return value;
		}

		private static List<UPropertyDrawer> _drawers;
		public static List<UPropertyDrawer> FindDrawers() {
			if(_drawers == null) {
				_drawers = new List<UPropertyDrawer>();
				foreach(var assembly in EditorReflectionUtility.GetAssemblies()) {
					try {
						foreach(System.Type type in EditorReflectionUtility.GetAssemblyTypes(assembly)) {
							if(type.IsSubclassOf(typeof(UPropertyDrawer)) && ReflectionUtils.CanCreateInstance(type)) {
								var control = ReflectionUtils.CreateInstance(type) as UPropertyDrawer;
								_drawers.Add(control);
							}
						}
					}
					catch { continue; }
				}
				_drawers.Sort((x, y) => CompareUtility.Compare(x.order, y.order));
			}
			return _drawers;
		}

		private static Dictionary<Type, UPropertyDrawer> _fieldControlMap = new Dictionary<Type, UPropertyDrawer>();
		private static Dictionary<Type, UPropertyDrawer> _fieldLayoutedControlMap = new Dictionary<Type, UPropertyDrawer>();
		private static UPropertyDrawer unsupportedControl = new UnsupportedDrawer();

		public static UPropertyDrawer FindDrawer(Type type, bool layouted) {
			if(type == null)
				return unsupportedControl;
			UPropertyDrawer control;
			if(layouted) {
				if(_fieldLayoutedControlMap.TryGetValue(type, out control)) {
					return control;
				}
			} else {
				if(_fieldControlMap.TryGetValue(type, out control)) {
					return control;
				}
			}
			var controls = FindDrawers();
			for(int i = 0; i < controls.Count; i++) {
				if(controls[i].IsValid(type, layouted)) {
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

		class UnsupportedDrawer : UPropertyDrawer {
			public override void Draw(Rect position, DrawerOption option) {
				position = EditorGUI.PrefixLabel(position, option.property.label);
				EditorGUI.SelectableLabel(position, option.property.label.text);
			}

			public override bool IsValid(Type type, bool layouted) {
				return false;
			}
		}
	}

	public abstract class UPropertyDrawer<T> : UPropertyDrawer {
		public override bool IsValid(Type type, bool layouted) {
			if(type == typeof(T)) {
				return true;
			}
			return false;
		}

		protected T GetValue(UBind property, bool nullable = false) {
			return GetValue<T>(property, nullable);
		}
	}
}