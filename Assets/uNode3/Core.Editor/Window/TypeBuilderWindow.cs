using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace MaxyGames.UNode.Editors {
	public class TypeBuilderWindow : EditorWindow {
		public Event currentEvent;
		public TypeItem[] Value;
		public bool canAddType = false;
		public object targetObject;
		private Vector2 scrollPos;
		private TypeItem[] targetType;
		private Rect contentRect;

		private Action<MemberData[]> selectCallback;
		private FilterAttribute filter;

		#region ShowWindows
		public static TypeBuilderWindow Show(Vector2 pos,
			object targetObject,
			FilterAttribute filter,
			Action<MemberData[]> selectCallback,
			params TypeItem[] targetType) {
			TypeBuilderWindow window = CreateInstance(typeof(TypeBuilderWindow)) as TypeBuilderWindow;
			window.targetObject = targetObject;
			window.targetType = targetType;
			window.filter = filter;
			window.Init(pos);
			window.selectCallback = selectCallback;
			return window;
		}

		public static TypeBuilderWindow Show(Rect rect,
			object targetObject,
			FilterAttribute filter,
			Action<MemberData[]> selectCallback,
			params TypeItem[] targetType) {
			TypeBuilderWindow window = CreateInstance(typeof(TypeBuilderWindow)) as TypeBuilderWindow;
			window.targetObject = targetObject;
			window.targetType = targetType;
			window.filter = filter;
			window.selectCallback = selectCallback;
			window.Init(rect);
			return window;
		}
		#endregion

		public static Rect GUIToScreenRect(Rect rect) {
			Vector2 vector = GUIUtility.GUIToScreenPoint(new Vector2(rect.x, rect.y));
			rect.x = vector.x;
			rect.y = vector.y;
			return rect;
		}

		private void Init(Vector2 POS) {
			Rect rect = new Rect(new Vector2(POS.x + 250, POS.y), new Vector2(250, 170));
			ShowAsDropDown(rect, new Vector2(250, 500));
			Setup();
		}

		private void Init(Rect rect) {
			rect = GUIToScreenRect(rect);
			rect.width = 250;
			ShowAsDropDown(rect, new Vector2(rect.width, 500));
			Setup();
		}

		private void Setup() {
			wantsMouseMove = true;
			if(filter == null) {
				filter = new FilterAttribute();
				filter.VoidType = true;
			} else {
				filter = new FilterAttribute(filter);
			}
			filter.OnlyGetType = true;
			filter.InvalidTargetType |= MemberData.TargetType.Null;
			if(targetType == null || targetType.Length == 0) {
				targetType = new TypeItem[] { filter.Types == null || filter.Types.Count == 0 ? typeof(object) : filter.Types[0] };
			} else if(targetType.Any((t) => t == null)) {
				for(int i = 0; i < targetType.Length; i++) {
					if(targetType[i] == null) {
						targetType[i] = filter.Types == null || filter.Types.Count == 0 ? typeof(object) : filter.Types[0];
					}
				}
			}
			Value = targetType;
			foreach(var value in Value) {
				if(value.filter != null) {
					value.filter.OnlyGetType = true;
				}
			}
			Focus();
		}

		void OnGUI() {
			HandleKeyboard();
			Rect cRect = EditorGUILayout.BeginVertical("Box");
			GUILayout.Label(new GUIContent("Type Builder"), EditorStyles.toolbarButton);
			if(contentRect.height > 350)
				scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
			currentEvent = Event.current;
			if(Value != null) {
				for(int i = 0; i < Value.Length; i++) {
					if(Value[i] == null) {
						Value[i] = new TypeItem(typeof(object));
					}
					int current = i;
					EditType(Value[i], (item) => {
						Value[current] = item;
						Repaint();
					});
				}
			}
			if(canAddType) {
				EditorGUILayout.BeginHorizontal();
				GUILayout.Label("Types");
				GUILayout.Space(50);
				if(GUILayout.Button(new GUIContent("-"))) {
					ArrayUtility.RemoveAt(ref Value, Value.Length - 1);
				}
				if(GUILayout.Button(new GUIContent("+"))) {
					ArrayUtility.Add(ref Value, new TypeItem(typeof(object)));
				}
				EditorGUILayout.EndHorizontal();
			}
			if(GUILayout.Button(new GUIContent("Select"))) {
				if(filter.Types == null ||
					filter.Types.Count == 0 ||
					filter.Types.Contains(typeof(object)) ||
					Value.All(i => (filter.IsValidType(i.type))) ||
					Value.All(i => (i.Value != null && filter.IsValidType(i.Value.startType)))
				) {
					Close();
					selectCallback(Value.Select(item => item.Value).ToArray());
				}
			}
			if(contentRect.height > 350)
				GUI.EndScrollView();
			EditorGUILayout.EndVertical();
			if(contentRect == Rect.zero && Event.current.type == EventType.Repaint) {
				contentRect = cRect;
				maxSize = new Vector2(maxSize.x, contentRect.height + 10);
			}
			//GUILayout.FlexibleSpace();
		}

		private void EditDeepType(TypeItem item, int deepLevel, Action<TypeItem> onChange) {
			if(item.type != null && item.type.IsGenericType) {
				var gType = item.type.GetGenericArguments();
				if(gType.Length > 0) {
					var filter = new FilterAttribute(item.filter);
					filter.Types.Clear();
					filter.OnlyGetType = true;
					filter.UnityReference = false;
					//filter.DisplayRuntimeType = false;
					for(int i = 0; i < gType.Length; i++) {
						var current = i;
						EditType(gType[i], filter, deepLevel, (type) => {
							if(type != null) {
								var typeDefinition = item.type.GetGenericTypeDefinition();
								gType[current] = type;
								item.SetType(ReflectionUtils.MakeGenericType(typeDefinition, gType));
								onChange(item);
							}
						});
					}
				}
			}
		}

		private void EditType(Type type, FilterAttribute filter, int deepLevel, Action<Type> onChange) {
			string tName = type.PrettyName();
			Rect rect = GUILayoutUtility.GetRect(new GUIContent(tName), "Button");
			rect.x += 15 * deepLevel;
			rect.width -= 15 * deepLevel;
			if(GUI.Button(rect, new GUIContent(tName, type.FullName))) {
				if(Event.current.button == 0) {
					ItemSelector.ShowAsNew(targetObject, filter, delegate (MemberData value) {
						onChange(value.startType);
					}).ChangePosition(rect.ToScreenRect());
				} else {
					AutoCompleteWindow.CreateWindow(uNodeGUIUtility.GUIToScreenRect(rect), (items) => {
						var member = CompletionEvaluator.CompletionsToMemberData(items);
						if(member != null) {
							onChange(member.startType);
							return true;
						}
						return false;
					}, new CompletionEvaluator.CompletionSetting() {
						validCompletionKind = CompletionKind.Type | CompletionKind.Namespace | CompletionKind.Keyword,
					});
				}
			}
			if(type.IsGenericType) {
				var gType = type.GetGenericArguments();
				if(gType.Length > 0) {
					for(int i = 0; i < gType.Length; i++) {
						var current = i;
						EditType(gType[i], filter, deepLevel + 1, (t) => {
							if(t != null) {
								var typeDefinition = type.GetGenericTypeDefinition();
								gType[current] = t;
								onChange(ReflectionUtils.MakeGenericType(typeDefinition, gType));
							}
						});
					}
				}
			}
		}

		private void EditType(TypeItem item, Action<TypeItem> onChange) {
			FilterAttribute f = item.filter ?? filter;
			string valName = item.DisplayName;
			EditorGUILayout.BeginVertical("Box");
			Rect rect = GUILayoutUtility.GetRect(new GUIContent(valName), "Button");
			if(GUI.Button(rect, new GUIContent(valName, valName))) {
				if(Event.current.button == 0) {
					if(Event.current.shift || Event.current.control) {
						AutoCompleteWindow.CreateWindow(uNodeGUIUtility.GUIToScreenRect(rect), (items) => {
							var member = CompletionEvaluator.CompletionsToMemberData(items);
							if(member != null) {
								item.Value = member;
								onChange(item);
								return true;
							}
							return false;
						}, new CompletionEvaluator.CompletionSetting() {
							validCompletionKind = CompletionKind.Type | CompletionKind.Namespace | CompletionKind.Keyword,
						});
					}
					ItemSelector.ShowAsNew(targetObject, f, delegate (MemberData value) {
						item.Value = value;
						onChange(item);
					}).ChangePosition(rect.ToScreenRect());
				} else {
					var type = item.type;
					GenericMenu menu = new GenericMenu();
					if(type.IsGenericType) {
						var args = type.GetGenericArguments();
						foreach(var t in args) {
							menu.AddItem(new GUIContent($"To {t.PrettyName()}"), false, () => {
								item.SetType(t);
								contentRect = Rect.zero;
								Repaint();
							});
						}
					}
					menu.AddItem(new GUIContent($"To List<{type.PrettyName()}>"), false, () => {

						item.SetType(ReflectionUtils.MakeGenericType(typeof(List<>), type));
						contentRect = Rect.zero;
						Repaint();
					});
					menu.AddItem(new GUIContent($"To HashSet<{type.PrettyName()}>"), false, () => {

						item.SetType(ReflectionUtils.MakeGenericType(typeof(HashSet<>), type));
						contentRect = Rect.zero;
						Repaint();
					});
					menu.AddItem(new GUIContent($"To Dictionary<{type.PrettyName()}, {typeof(object).PrettyName()}>"), false, () => {
						item.SetType(ReflectionUtils.MakeGenericType(typeof(Dictionary<,>), type, typeof(object)));
						contentRect = Rect.zero;
						Repaint();
					});
					menu.ShowAsContext();
				}
			}
			EditDeepType(item, 1, onChange);
			if(f.CanManipulateArray()) {
				EditorGUILayout.BeginHorizontal();
				GUILayout.Label("Array");
				GUILayout.Space(50);
				if(GUILayout.Button(new GUIContent("-"))) {
					if(item.array > 0) {
						item.array--;
					}
					onChange(item);
				}
				if(GUILayout.Button(new GUIContent("+"))) {
					item.array++;
					onChange(item);
				}
				EditorGUILayout.EndHorizontal();
			}
			EditorGUILayout.EndVertical();
		}

		void HandleKeyboard() {
			Event current = Event.current;
			if(current.type == EventType.KeyDown) {
				if(current.keyCode == KeyCode.Escape) {
					Close();
					return;
				}
			}
			if(current.type == EventType.KeyDown) {
				Focus();
			}
		}
	}

	public class TypeItem {
		public string Name;
		public Type type;
		public int array = 0;

		public FilterAttribute filter;

		private MemberData _value;
		public MemberData Value {
			get {
				Type t = type;
				int i = array;
				if(_value != null) {
					if(_value.targetType == MemberData.TargetType.Type) {
						t = _value.startType;
					} else if(_value.targetType == MemberData.TargetType.uNodeGenericParameter) {
						return _value;
					} else {
						if(_value.Items != null && _value.Items.Length > 0) {
							while(array > 0) {
								_value.Items[_value.Items.Length - 1].name += "[]";
								array--;
							}
						}
						return _value;
					}
				}
				while(i > 0) {
					t = ReflectionUtils.MakeArrayType(t);
					i--;
				}
				return new MemberData(t, MemberData.TargetType.Type);
			}
			set {
				_value = value;
				type = value.startType;
			}
		}

		public string DisplayName {
			get {
				string name = Name;
				if(_value != null) {
					name = _value.DisplayName(false, false);
				}
				int i = array;
				while(i > 0) {
					name += "[]";
					i--;
				}
				return name;
			}
		}

		public TypeItem() {
			type = typeof(object);
			Name = type.PrettyName();
		}

		public TypeItem(Type type, FilterAttribute filter = null) {
			SetType(type);
			this.filter = filter;
		}

		public TypeItem(MemberData member, FilterAttribute filter = null) {
			if(member == null) {
				member = new MemberData(typeof(object));
			}
			_value = SerializerUtility.Duplicate(member);
			Name = _value.name;
			if(_value.targetType == MemberData.TargetType.Type) {
				type = _value.startType;
				if(type != null) {
					while(type.IsArray) {
						type = type.GetElementType();
						_value = new MemberData(type);
						array++;
					}
				}
			} else {
				type = _value.type;
			}
			this.filter = filter;
		}

		public static implicit operator TypeItem(Type type) {
			return new TypeItem(type);
		}

		public static implicit operator TypeItem(MemberData member) {
			return new TypeItem(member);
		}

		public void SetType(Type type) {
			_value = null;
			if(type == null) {
				type = typeof(object);
			}
			while(type.IsArray) {
				type = type.GetElementType();
				array++;
			}
			this.type = type;
			Name = type.PrettyName();
		}
	}
}