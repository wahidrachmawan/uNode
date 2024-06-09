#pragma warning disable
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.IO;

namespace MaxyGames.UNode.Editors {
	/// <summary>
	/// Provides useful Utility for Editor
	/// </summary>
	public static class uNodeEditorUtility {
		#region Properties
		private static MonoScript[] _monoScripts;
		/// <summary>
		/// Find all MonoScript in the project
		/// </summary>
		public static MonoScript[] MonoScripts {
			get {
				if(_monoScripts == null) {
					_monoScripts = Resources.FindObjectsOfTypeAll<MonoScript>();
					//_monoScripts = MonoImporter.GetAllRuntimeMonoScripts();
				}
				return _monoScripts;
			}
		}
		#endregion

		#region Pro
		internal static class ProBinding {
			public static Action CallbackShowCSharpPreview;
			public static Action CallbackShowGlobalSearch;
			public static Action CallbackShowNodeBrowser;
			public static Action CallbackShowGraphHierarchy;
			public static Action<MemberInfo> CallbackFindInNodeBrowser;
		}

		[MenuItem("Tools/uNode/Live C# Preview")]
		private static void ShowCSharpPreview() {
			if(uNodeEditorUtility.DisplayRequiredProVersion("Realtime C# Preview")) {
				return;
			}
			ProBinding.CallbackShowCSharpPreview?.Invoke();
		}

		[MenuItem("Tools/uNode/Node Browser", false, 100)]
		public static void ShowNodeBrowser() {
			if(uNodeEditorUtility.DisplayRequiredProVersion("Node Browser")) {
				return;
			}
			ProBinding.CallbackShowNodeBrowser?.Invoke();
		}

		[MenuItem("Tools/uNode/Graph Hierarchy", false, 102)]
		public static void ShowGraphHierarchy() {
			if(uNodeEditorUtility.DisplayRequiredProVersion("Graph Hierarchy")) {
				return;
			}
			ProBinding.CallbackShowGraphHierarchy?.Invoke();
		}

		[MenuItem("Tools/uNode/Global Search", false, 103)]
		public static void ShowGlobalSearch() {
			if(uNodeEditorUtility.DisplayRequiredProVersion("Global Search")) {
				return;
			}
			ProBinding.CallbackShowGlobalSearch?.Invoke();
		}

		public static void FindInBrowser(MemberInfo info) {
			if(uNodeEditorUtility.DisplayRequiredProVersion("Node Browser")) {
				return;
			}
			ProBinding.CallbackFindInNodeBrowser?.Invoke(info);
		}
		#endregion

		#region Icons
		public static class Icons {
			internal static readonly MethodInfo EditorGUIUtility_GetScriptObjectFromClass;
			internal static readonly MethodInfo EditorGUIUtility_GetIconForObject;

			static Icons() {
				EditorGUIUtility_GetScriptObjectFromClass = typeof(EditorGUIUtility).GetMethod("GetScript", BindingFlags.Static | BindingFlags.NonPublic);
				EditorGUIUtility_GetIconForObject = typeof(EditorGUIUtility).GetMethod("GetIconForObject", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
			}


			private static Texture2D _flowIcon;
			public static Texture2D flowIcon {
				get {
					if(_flowIcon == null) {
						_flowIcon = Resources.Load<Texture2D>("Icons/IconFlow");
					}
					return _flowIcon;
				}
			}

			private static Texture2D _valueIcon;
			public static Texture2D valueIcon {
				get {
					if(_valueIcon == null) {
						_valueIcon = Resources.Load<Texture2D>("Icons/IconValueWhite");
					}
					return _valueIcon;
				}
			}

			private static Texture2D _valueBlueIcon;
			public static Texture2D valueBlueIcon {
				get {
					if(_valueBlueIcon == null) {
						_valueBlueIcon = Resources.Load<Texture2D>("Icons/IconValueBlue");
					}
					return _valueBlueIcon;
				}
			}

			private static Texture2D _valueYellowIcon;
			public static Texture2D valueYellowIcon {
				get {
					if(_valueYellowIcon == null) {
						_valueYellowIcon = Resources.Load<Texture2D>("Icons/IconValueYellow");
					}
					return _valueYellowIcon;
				}
			}

			private static Texture2D _valueYellowRed;
			public static Texture2D valueYellowRed {
				get {
					if(_valueYellowRed == null) {
						_valueYellowRed = Resources.Load<Texture2D>("Icons/IconValueRed");
					}
					return _valueYellowRed;
				}
			}

			private static Texture2D _valueGreenIcon;
			public static Texture2D valueGreenIcon {
				get {
					if(_valueGreenIcon == null) {
						_valueGreenIcon = Resources.Load<Texture2D>("Icons/IconValueGreen");
					}
					return _valueGreenIcon;
				}
			}

			private static Texture2D _divideIcon;
			public static Texture2D divideIcon {
				get {
					if(_divideIcon == null) {
						_divideIcon = Resources.Load<Texture2D>("Icons/IconFlowDivide");
					}
					return _divideIcon;
				}
			}

			private static Texture2D _clockIcon;
			public static Texture2D clockIcon {
				get {
					if(_clockIcon == null) {
						_clockIcon = Resources.Load<Texture2D>("Icons/IconTime");
					}
					return _clockIcon;
				}
			}

			private static Texture2D _repeatIcon;
			public static Texture2D repeatIcon {
				get {
					if(_repeatIcon == null) {
						_repeatIcon = Resources.Load<Texture2D>("Icons/IconRepeat");
					}
					return _repeatIcon;
				}
			}

			private static Texture2D _repeatOnceIcon;
			public static Texture2D repeatOnceIcon {
				get {
					if(_repeatOnceIcon == null) {
						_repeatOnceIcon = Resources.Load<Texture2D>("Icons/IconRepeatOnce");
					}
					return _repeatOnceIcon;
				}
			}

			private static Texture2D _switchIcon;
			public static Texture2D switchIcon {
				get {
					if(_switchIcon == null) {
						_switchIcon = Resources.Load<Texture2D>("Icons/IconSwitch");
					}
					return _switchIcon;
				}
			}

			private static Texture2D _colorIcon;
			public static Texture2D colorIcon {
				get {
					if(_colorIcon == null) {
						_colorIcon = Resources.Load<Texture2D>("Icons/IconColor");
					}
					return _colorIcon;
				}
			}

			private static Texture2D _rotateIcon;
			public static Texture2D rotateIcon {
				get {
					if(_rotateIcon == null) {
						_rotateIcon = Resources.Load<Texture2D>("Icons/IconRotate");
					}
					return _rotateIcon;
				}
			}

			private static Texture2D _objectIcon;
			public static Texture2D objectIcon {
				get {
					if(_objectIcon == null) {
						_objectIcon = Resources.Load<Texture2D>("Icons/IconObject");
					}
					return _objectIcon;
				}
			}

			private static Texture2D _mouseIcon;
			public static Texture2D mouseIcon {
				get {
					if(_mouseIcon == null) {
						_mouseIcon = Resources.Load<Texture2D>("Icons/mouse_pc");
					}
					return _mouseIcon;
				}
			}

			private static Texture2D _keyIcon;
			public static Texture2D keyIcon {
				get {
					if(_keyIcon == null) {
						_keyIcon = Resources.Load<Texture2D>("Icons/key");
					}
					return _keyIcon;
				}
			}

			private static Texture2D _dateIcon;
			public static Texture2D dateIcon {
				get {
					if(_dateIcon == null) {
						_dateIcon = Resources.Load<Texture2D>("Icons/date");
					}
					return _dateIcon;
				}
			}

			private static Texture2D _listIcon;
			public static Texture2D listIcon {
				get {
					if(_listIcon == null) {
						_listIcon = Resources.Load<Texture2D>("Icons/IconList");
					}
					return _listIcon;
				}
			}

			private static Texture2D _eventIcon;
			public static Texture2D eventIcon {
				get {
					if(_eventIcon == null) {
						_eventIcon = Resources.Load<Texture2D>("Icons/IconEvent");
					}
					return _eventIcon;
				}
			}

			private static Texture2D _bookIcon;
			public static Texture2D bookIcon {
				get {
					if(_bookIcon == null) {
						_bookIcon = Resources.Load<Texture2D>("Icons/book_key");
					}
					return _bookIcon;
				}
			}

			static Dictionary<string, Texture2D> iconMap;
			public static Texture2D GetIcon(string path) {
				if(iconMap == null) {
					iconMap = new Dictionary<string, Texture2D>();
				}
				Texture2D tex;
				if(iconMap.TryGetValue(path, out tex)) {
					return tex;
				}
				tex = Resources.Load<Texture2D>(path);
				if(tex != null) {
					iconMap[path] = tex;
				}
				return tex;
			}
		}

		private static Dictionary<Type, Texture> _iconsMap = new Dictionary<Type, Texture>();

		public static Texture GetIcon(MemberInfo member) {
			switch(member.MemberType) {
				case MemberTypes.Constructor:
				case MemberTypes.Method:
					return GetTypeIcon(typeof(TypeIcons.MethodIcon));
				case MemberTypes.Field:
				case MemberTypes.Event:
					return GetTypeIcon(typeof(TypeIcons.FieldIcon));
				case MemberTypes.NestedType:
				case MemberTypes.TypeInfo:
					Type type = member as Type;
					if(type is ICustomIcon) {
						return GetTypeIcon(type);
					}
					if(type.IsClass) {
						if(type.IsCastableTo(typeof(Delegate))) {
							return GetTypeIcon(typeof(TypeIcons.DelegateIcon));
						}
						return GetTypeIcon(typeof(TypeIcons.ClassIcon));
					} else if(type.IsInterface) {
						return GetTypeIcon(typeof(TypeIcons.InterfaceIcon));
					} else if(type.IsEnum) {
						return GetTypeIcon(typeof(TypeIcons.EnumIcon));
					} else {
						return GetTypeIcon(typeof(TypeIcons.StructureIcon));
					}
				case MemberTypes.Property:
					return GetTypeIcon(typeof(TypeIcons.PropertyIcon));
				default:
					return GetTypeIcon(typeof(TypeIcons.KeywordIcon));
			}
		}

		public static Texture GetIcon(MemberData member) {
			switch(member.targetType) {
				case MemberData.TargetType.Self:
				case MemberData.TargetType.Values:
					return GetTypeIcon(typeof(TypeIcons.KeywordIcon));
				case MemberData.TargetType.uNodeVariable:
				case MemberData.TargetType.Field:
				case MemberData.TargetType.Event:
					return GetTypeIcon(typeof(TypeIcons.FieldIcon));
				case MemberData.TargetType.uNodeLocalVariable:
					return GetTypeIcon(typeof(TypeIcons.LocalVariableIcon));
				case MemberData.TargetType.uNodeProperty:
				case MemberData.TargetType.Property:
					return GetTypeIcon(typeof(TypeIcons.PropertyIcon));
				case MemberData.TargetType.uNodeConstructor:
				case MemberData.TargetType.uNodeFunction:
				case MemberData.TargetType.Method:
				case MemberData.TargetType.Constructor:
					return GetTypeIcon(typeof(TypeIcons.MethodIcon));
				case MemberData.TargetType.uNodeParameter:
				case MemberData.TargetType.uNodeGenericParameter:
					return GetTypeIcon(typeof(TypeIcons.LocalVariableIcon));
				case MemberData.TargetType.Type:
					Type type = member.startType;
					if(type == null) {
						return GetTypeIcon(typeof(TypeIcons.ClassIcon));
					} else if(type.IsClass) {
						if(type is ICustomIcon) {
							return GetTypeIcon(type);
						}
						if(type.IsSubclassOf(typeof(Delegate))) {
							return GetTypeIcon(typeof(TypeIcons.DelegateIcon));
						}
						return GetTypeIcon(typeof(TypeIcons.ClassIcon));
					} else if(type.IsInterface) {
						return GetTypeIcon(typeof(TypeIcons.InterfaceIcon));
					} else if(type.IsEnum) {
						return GetTypeIcon(typeof(TypeIcons.EnumIcon));
					} else if(type == typeof(void)) {
						return GetTypeIcon(typeof(TypeIcons.VoidIcon));
					} else {
						return GetTypeIcon(typeof(TypeIcons.StructureIcon));
					}
				default:
					return GetTypeIcon(typeof(TypeIcons.KeywordIcon));
			}
		}

		/// <summary>
		/// Return a icon for the type of a MemberData.
		/// </summary>
		/// <param name="member"></param>
		/// <returns></returns>
		public static Texture GetTypeIcon(MemberData member) {
			switch(member.targetType) {
				case MemberData.TargetType.Type:
				case MemberData.TargetType.uNodeType:
					var type = member.startType;
					return GetTypeIcon(type);
				default:
					return GetIcon(member);
			}
		}

		/// <summary>
		/// Return a icon for the object.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public static Texture GetTypeIcon(object obj) {
			if(obj is ICustomIcon) {
				var icon = (obj as ICustomIcon).GetIcon();
				if(icon != null) {
					return icon;
				}
				return GetTypeIcon(typeof(TypeIcons.RuntimeTypeIcon));
			} else if(obj is MemberData) {
				return GetTypeIcon(obj as MemberData);
			} else if(obj is IIcon) {
				return GetTypeIcon((obj as IIcon).GetIcon());
			}
			if(obj == null) {
				return GetTypeIcon(typeof(object));
			}
			return GetTypeIcon(obj.GetType());
		}

		private static Texture GetDefaultIcon(Type type) {
			if(type == null || type.IsGenericParameter) return null;
			if(typeof(MonoBehaviour).IsAssignableFrom(type)) {
				var icon = EditorGUIUtility.ObjectContent(null, type)?.image;
				if(icon == EditorGUIUtility.FindTexture("DefaultAsset Icon")) {
					icon = null;
				}
				if(icon != null) {
					return icon;
				} else {
					icon = GetScriptTypeIcon(type.Name);
					if(icon != null) {
						return icon;
					}
				}
			}
			if(typeof(UnityEngine.Object).IsAssignableFrom(type)) {
				Texture icon = EditorGUIUtility.ObjectContent(null, type)?.image;
				if(icon == EditorGUIUtility.FindTexture("DefaultAsset Icon")) {
					icon = EditorGUIUtility.FindTexture("ScriptableObject Icon");
				}
				if(icon != null) {
					return icon;
				}
			}
			return null;
		}

		private static Texture GetScriptTypeIcon(string scriptName) {
			var scriptObject = (UnityEngine.Object)Icons.EditorGUIUtility_GetScriptObjectFromClass.InvokeOptimized(null, new object[] { scriptName });
			if(scriptObject != null) {
				var scriptIcon = Icons.EditorGUIUtility_GetIconForObject.InvokeOptimized(null, new object[] { scriptObject }) as Texture;

				if(scriptIcon != null) {
					return scriptIcon;
				}
			}
			var scriptPath = AssetDatabase.GetAssetPath(scriptObject);
			if(scriptPath != null) {
				switch(Path.GetExtension(scriptPath)) {
					case ".js":
						return EditorGUIUtility.IconContent("js Script Icon").image;
					case ".cs":
						return EditorGUIUtility.IconContent("cs Script Icon").image;
					case ".boo":
						return EditorGUIUtility.IconContent("boo Script Icon").image;
				}
			}
			return null;
		}

		/// <summary>
		/// Return a icon for the type.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static Texture GetTypeIcon(Type type) {
			if(type == null)
				return null;
			if(type.IsByRef) {
				return GetTypeIcon(type.GetElementType());
			}
			Texture result = null;
			if(_iconsMap.TryGetValue(type, out result)) {
				return result;
			}
			if(type is ICustomIcon) {
				var icon = (type as ICustomIcon).GetIcon();
				if(icon != null) {
					return icon;
				}
			}
			else if(type is IIcon) {
				var icon = (type as IIcon).GetIcon();
				if(icon != null) {
					return GetTypeIcon(icon);
				}
			}
			if(type is RuntimeType) {
				if(type.IsArray || type.IsGenericType) {
					return Icons.listIcon;
				}
				var rType = type as RuntimeType;
				return GetTypeIcon(typeof(TypeIcons.RuntimeTypeIcon));
			}
			Texture texture = GetDefaultIcon(type);
			if(texture != null) {
				_iconsMap[type] = texture;
				return texture;
			}
			TypeIcons.IconPathAttribute att = null;
			if(type.IsDefinedAttribute(typeof(TypeIcons.IconPathAttribute))) {
				att = type.GetCustomAttributes(typeof(TypeIcons.IconPathAttribute), true)[0] as TypeIcons.IconPathAttribute;
			}
			if(att != null) {
				result = Icons.GetIcon(att.path);
			} else if(type == typeof(TypeIcons.FlowIcon)) {
				result = Icons.flowIcon;
			} else if(type == typeof(TypeIcons.ValueIcon)) {
				result = Icons.valueIcon;
			} else if(type == typeof(TypeIcons.BranchIcon)) {
				result = Icons.divideIcon;
			} else if(type == typeof(TypeIcons.ClockIcon)) {
				result = Icons.clockIcon;
			} else if(type == typeof(TypeIcons.RepeatIcon)) {
				result = Icons.repeatIcon;
			} else if(type == typeof(TypeIcons.RepeatOnceIcon)) {
				result = Icons.repeatOnceIcon;
			} else if(type == typeof(TypeIcons.SwitchIcon)) {
				result = Icons.switchIcon;
			} else if(type == typeof(TypeIcons.MouseIcon)) {
				result = Icons.mouseIcon;
			} else if(type == typeof(TypeIcons.EventIcon)) {
				result = Icons.eventIcon;
			} else if(type == typeof(TypeIcons.RotationIcon) || type == typeof(Quaternion)) {
				result = Icons.rotateIcon;
			} else if(type == typeof(Color) || type == typeof(Color32)) {
				result = Icons.colorIcon;
			} else if(type == typeof(int)) {
				result = GetTypeIcon(typeof(TypeIcons.IntegerIcon));
			} else if(type == typeof(float)) {
				result = GetTypeIcon(typeof(TypeIcons.FloatIcon));
			}else if(type == typeof(Vector3)) {
				result = GetTypeIcon(typeof(TypeIcons.Vector3Icon));
			} else if(type == typeof(Vector2)) {
				result = GetTypeIcon(typeof(TypeIcons.Vector2Icon));
			} else if(type == typeof(Vector4)) {
				result = GetTypeIcon(typeof(TypeIcons.Vector4Icon));
			} else if(type.IsCastableTo(typeof(UnityEngine.Object))) {
				result = Icons.objectIcon;
			} else if(type.IsCastableTo(typeof(IList))) {
				result = Icons.listIcon;
			} else if(type.IsCastableTo(typeof(IDictionary))) {
				result = Icons.bookIcon;
			} else if(type == typeof(void)) {
				result = GetTypeIcon(typeof(TypeIcons.VoidIcon));
			} else if(type.IsCastableTo(typeof(KeyValuePair<,>))) {
				result = Icons.keyIcon;
			} else if(type == typeof(DateTime) || type == typeof(Time)) {
				result = Icons.dateIcon;
			} else if(type.IsInterface) {
				result = GetTypeIcon(typeof(TypeIcons.InterfaceIcon));
			} else if(type.IsEnum) {
				result = GetTypeIcon(typeof(TypeIcons.EnumIcon));
			} else if(type == typeof(object)) {
				result = Icons.valueBlueIcon;
			} else if(type == typeof(bool)) {
				result = Icons.valueYellowRed;
			} else if(type == typeof(string)) {
				result = GetTypeIcon(typeof(TypeIcons.StringIcon));
			} else if(type == typeof(Type)) {
				result = Icons.valueGreenIcon;
			} else if(type == typeof(UnityEngine.Random) || type == typeof(System.Random)) {
				result = GetTypeIcon(typeof(TypeIcons.RandomIcon));
			} 
			// else if(type == typeof(UnityEngine.Debug)) {
			// 	result = GetTypeIcon(typeof(TypeIcons.BugIcon));
			// } 
			else {
				result = GetIcon(type);
			}
			if(result != null) {
				_iconsMap[type] = result;
			}
			return result;
		}
		#endregion

		#region Styles
		public static Texture2D MakeTexture(int width, int height, Color color) {
			Color[] pix = new Color[width * height];

			for(int i = 0; i < pix.Length; i++)
				pix[i] = color;

			Texture2D result = new Texture2D(width, height);
			result.SetPixels(pix);
			result.Apply();

			return result;
		}
		#endregion

		#region Drag & Drop
		public static void GUIDropArea(Rect rect, Action onDragPerform, Action repaintAction = null) {
			var currentEvent = Event.current;
			if(rect.Contains(currentEvent.mousePosition)) {
				if(currentEvent.type == EventType.DragUpdated) {
					DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
				}
				if(DragAndDrop.visualMode == DragAndDropVisualMode.Copy && (currentEvent.type == EventType.Repaint || currentEvent.type == EventType.Layout)) {
					repaintAction?.Invoke();
				}
				if(currentEvent.type == EventType.DragPerform) {
					DragAndDrop.AcceptDrag();
					onDragPerform();
					Event.current.Use();
				}
			}
		}

		public static void StartCustomDrag(object data, object owner = null) {
			DragAndDrop.PrepareStartDrag();
			DragAndDrop.SetGenericData("uNode", data);
			if(owner != null) {
				DragAndDrop.SetGenericData("uNode-Target", owner);
			}
			DragAndDrop.StartDrag("Drag Node");
		}
		#endregion

		#region MapUtils
		public static List<object> GetKeys(IDictionary map) {
			List<object> keys = new List<object>();
			foreach(var k in map.Keys) {
				keys.Add(k);
			}
			return keys;
		}

		public static List<object> GetValues(IDictionary map) {
			List<object> values = new List<object>();
			foreach(var k in map.Values) {
				values.Add(k);
			}
			return values;
		}

		public static object GetKeyMap(IDictionary map, int index) {
			int i = 0;
			foreach(var k in map.Keys) {
				if(i == index) {
					return k;
				}
				i++;
			}
			return null;
		}

		public static object GetValueMap(IDictionary map, int index) {
			int i = 0;
			foreach(var k in map.Values) {
				if(i == index) {
					return k;
				}
				i++;
			}
			return null;
		}
		#endregion

		#region Transfroms
		/// <summary>
		/// Get the prefab transfrom.
		/// </summary>
		/// <param name="from">The transform to find</param>
		/// <param name="rootFrom">The root transfrom from</param>
		/// <param name="rootPrefab">The root transfrom for find it's child</param>
		/// <returns></returns>
		public static Transform GetPrefabTransform(Transform from, Transform rootFrom, Transform rootPrefab) {
			if(from == rootFrom) {
				return rootPrefab;
			}
			List<int> indexChild = new List<int>();
			Transform t = from;
			while(t.parent != null) {
				indexChild.Insert(0, t.GetSiblingIndex());
				if(t.parent == rootFrom) {
					break;
				}
				t = t.parent;
			}
			t = rootPrefab;
			foreach(var index in indexChild) {
				if(t.childCount <= index)
					return null;
				t = t.GetChild(index);
			}
			return t;
		}
		#endregion

		#region Others
		public static bool DisplayRequiredProVersion(string feature = "") {
			if(uNodeUtility.IsProVersion) {
				return false;
			}
			else {
				if(string.IsNullOrWhiteSpace(feature)) {
					feature = "This";
				}
				DisplayMessage("Required Pro Version", $"{feature} features required uNode pro version");
				return true;
			}
		}

		public static void DisplayErrorMessage(string message = "Something went wrong.") {
			EditorUtility.DisplayDialog("Error", message, "OK");
		}

		public static void DisplayMessage(string title, string message) {
			EditorUtility.DisplayDialog(title, message, "OK");
		}

		internal static void OpenILSpy(MemberInfo info) {
			var type = info as Type ?? info.DeclaringType;
			var assemblyLocation = type.Assembly.Location;
			if(string.IsNullOrEmpty(assemblyLocation)) {
				throw new Exception("Cannot open dynamic assembly");
			}
			var arguments = "\"" + assemblyLocation + "\" ";
			switch(info.MemberType) {
				case MemberTypes.Field:
					arguments += $"\"/navigateTo:F:{type.FullName}.{info.Name}\"";
					break;
				case MemberTypes.Property:
					arguments += $"\"/navigateTo:P:{type.FullName}.{info.Name}\"";
					break;
				case MemberTypes.Event:
					arguments += $"\"/navigateTo:E:{type.FullName}.{info.Name}\"";
					break;
				case MemberTypes.TypeInfo:
				case MemberTypes.NestedType:
					arguments += $"\"/navigateTo:T:{type.FullName}\"";
					break;
				case MemberTypes.Method:
					var method = info as MethodInfo;
					arguments += $"\"/navigateTo:M:{type.FullName}.{info.Name}({string.Join(',', method.GetParameters().Select(p => p.ParameterType.FullName))})\"";
					break;
				case MemberTypes.Constructor:
					var ctor = info as ConstructorInfo;
					arguments += $"\"/navigateTo:M:{type.FullName}.#ctor({string.Join(',', ctor.GetParameters().Select(p => p.ParameterType.FullName))})\"";
					break;
				default:
					throw null;
			}
			OpenILSpy(arguments);
		}

		internal static void OpenILSpy(string arguments) {
			var ilSpyPath = uNodePreference.preferenceData.ilSpyPath;
			if(System.IO.File.Exists(ilSpyPath) == false) {
				DisplayErrorMessage("The ILSpy executable was not found, try change to correct path on uNode Preference");
				return;
			}
			var process = new System.Diagnostics.Process() {
				StartInfo = new System.Diagnostics.ProcessStartInfo() {
					FileName = ilSpyPath,
					UseShellExecute = false,
					Arguments = arguments,
				}
			};
			process.Start();
		}

		public static bool IsTypeSerializable(Type type) {
			if(type.IsPrimitive) {
				return true;
			}
			if(type == typeof(string))
				return true;
			if(type == typeof(decimal))
				return true;
			if(type.IsArray) {
				return type.GetArrayRank() == 1 && IsTypeSerializable(type.ElementType());
			}
			if(type.IsGenericType) {
				if(type.IsGenericTypeDefinition)
					return false;
				if(type.GetGenericArguments().Length > 1)
					return false;
				return type.GetGenericTypeDefinition() == typeof(List<>) && IsTypeSerializable(type.ElementType());
			}
			if(type.IsSerializable) {
				return true;
			}
			if(type.IsEnum) {
				return true;
			}
			if(typeof(UnityEngine.Object).IsAssignableFrom(type))
				return true;
			if(type == typeof(Vector2))
				return true;
			if(type == typeof(Vector2Int))
				return true;
			if(type == typeof(Vector3))
				return true;
			if(type == typeof(Vector3Int))
				return true;
			if(type == typeof(Vector4))
				return true;
			if(type == typeof(AnimationCurve))
				return true;
			if(type == typeof(Bounds))
				return true;
			if(type == typeof(BoundsInt))
				return true;
			if(type == typeof(Color))
				return true;
			if(type == typeof(Color32))
				return true;
			if(type == typeof(Gradient))
				return true;
			//if(type == typeof(Keyframe))
			//	return true;
			if(type == typeof(LayerMask))
				return true;
			return false;
		}

		static string _uNodePath;
		/// <summary>
		/// Get uNode Plugin Path
		/// </summary>
		/// <returns></returns>
		public static string GetUNodePath() {
			if(string.IsNullOrEmpty(_uNodePath)) {
				var path = AssetDatabase.GetAssetPath(uNodeEditorUtility.GetMonoScript(typeof(Graph)));
				_uNodePath = path.Remove(path.LastIndexOf("Core/Graph") - 1);
			}
			return _uNodePath;
		}

		public static string GetRelativePath(string absolutePath) {
			if(absolutePath.Replace('\\', '/').StartsWith(Application.dataPath)) {
				return "Assets" + absolutePath.Substring(Application.dataPath.Length);
			}
			return string.Empty;
		}

		public static bool IsSceneObject(UnityEngine.Object target) {
			if(target == null)
				return false;
			if(!EditorUtility.IsPersistent(target)) {
				Transform root = null;
				if(target is Component) {
					root = (target as Component).transform.root;
				} else if(target is GameObject) {
					root = (target as GameObject).transform.root;
				}
				if(root != null && root.gameObject.name.StartsWith(GraphUtility.KEY_TEMP_OBJECT)) {
					//Ensure to return false when the target is a temporary object
					return false;
				}
				return true;
			}
			return false;
		}

		/// <summary>
		/// Copy string value to clipboard.
		/// </summary>
		/// <param name="value"></param>
		public static void CopyToClipboard(string value) {
			TextEditor te = new TextEditor();
			te.text = value;
			te.SelectAll();
			te.Copy();
		}

		/// <summary>
		/// Get unique identifier based on string
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		public static int GetUIDFromString(string str) {
			return uNodeUtility.GetHashCode(str);
		}

		/// <summary>
		/// Get unique name for graph variable, property, and function
		/// </summary>
		/// <param name="name"></param>
		/// <param name="graph"></param>
		/// <returns></returns>
		public static string GetUniqueNameForGraph(string name, Graph graph) {
			string result = name;
			//int index = 1;
			//bool hasSameName = false;
			//do {
			//	if(hasSameName) {
			//		result = name + index;
			//		index++;
			//	}
			//	hasSameName = false;
			//	foreach(var var in graph.Variables) {
			//		if(var.Name.Equals(result)) {
			//			hasSameName = true;
			//			break;
			//		}
			//	}
			//	foreach(var var in graph.Properties) {
			//		if(var.Name.Equals(result)) {
			//			hasSameName = true;
			//			break;
			//		}
			//	}
			//	foreach(var var in graph.Functions) {
			//		if(var.Name.Equals(result)) {
			//			hasSameName = true;
			//			break;
			//		}
			//	}
			//} while(hasSameName);
			return result;
		}

		/// <summary>
		/// Get full script name including namespace from the graph if exist.
		/// </summary>
		/// <param name="root"></param>
		/// <returns></returns>
		public static string GetFullScriptName(IGraph graph) {
			if(graph == null)
				return "";
			return graph.GetFullGraphName();
		}

		/// <summary>
		/// Get generated script from class system
		/// </summary>
		/// <param name="classSystem"></param>
		/// <returns></returns>
		public static Type GetGeneratedScript(IGraph graph) {
			if(graph == null)
				return null;
			return GetFullScriptName(graph).ToType(false);
		}

		/// <summary>
		/// Get correct control modifier for current OS.
		/// </summary>
		/// <returns>Return Command on OSX otherwise will return Control</returns>
		public static EventModifiers GetControlModifier() {
			if(Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer) {
				return EventModifiers.Command;
			}
			return EventModifiers.Control;
		}

		/// <summary>
		/// Mark UnityObject as dirty so Unity should save it.
		/// </summary>
		/// <param name="target"></param>
		public static void MarkDirty(UnityEngine.Object target) {
			if(target == null) return;
			EditorUtility.SetDirty(target);
#if UNITY_2021_1_OR_NEWER
			var prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
#else
			var prefabStage = UnityEditor.Experimental.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
#endif
			if(prefabStage != null) {
				UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(prefabStage.scene);
			}
		}

		public static void RegisterUndo(UGraphElement graphElement, string name = "") {
			if(graphElement == null) return;
			RegisterUndo(graphElement.graphContainer, name);
		}

		public static void RegisterUndo(IGraph graph, string name = "") {
			var obj = graph as UnityEngine.Object;
			if(obj == null) return;
			RegisterUndo(obj, name);
		}

		public static void RegisterUndo(UnityEngine.Object obj, string name = "") {
			if(obj == null) return;
			Undo.RegisterCompleteObjectUndo(obj, name);
			MarkDirty(obj);
			//if(IsPrefabInstance(obj)) {
			//	uNodeThreadUtility.Queue(() => {
			//		if(obj != null) {
			//			PrefabUtility.RecordPrefabInstancePropertyModifications(obj);
			//		}
			//	});
			//}
		}

		public static MonoScript GetMonoScript(object instance) {
			if(instance != null) {
				if(instance is MonoBehaviour) {
					return MonoScript.FromMonoBehaviour(instance as MonoBehaviour);
				} else if(instance is ScriptableObject) {
					return MonoScript.FromScriptableObject(instance as ScriptableObject);
				}
				return GetMonoScript(instance.GetType());
			}
			return null;
		}

		public static MonoScript GetMonoScript(Type type) {
			foreach(var s in MonoScripts) {
				if(s != null && s.GetClass() == type) {
					return s;
				}
			}
			return null;
		}

		private static System.Text.RegularExpressions.Regex _removeHTMLTagRx = new System.Text.RegularExpressions.Regex("<[^>]*>");
		public static string RemoveHTMLTag(string str) {
			if(string.IsNullOrEmpty(str)) {
				return str;
			}
			return _removeHTMLTagRx.Replace(str, "");
		}

		private static List<CustomGraphAttribute> _customGraphs;
		/// <summary>
		/// Find command pin menu.
		/// </summary>
		/// <returns></returns>
		public static List<CustomGraphAttribute> FindCustomGraph() {
			if(_customGraphs == null) {
				_customGraphs = new List<CustomGraphAttribute>();

				foreach(System.Reflection.Assembly assembly in EditorReflectionUtility.GetAssemblies()) {
					foreach(System.Type type in EditorReflectionUtility.GetAssemblyTypes(assembly)) {
						var atts = type.GetCustomAttributes(typeof(CustomGraphAttribute), true);
						if(atts.Length > 0) {
							foreach(var a in atts) {
								var control = a as CustomGraphAttribute;
								control.type = type;
								_customGraphs.Add(control);
							}
						}
					}
				}
				_customGraphs.Sort((x, y) => string.Compare(x.name, y.name, StringComparison.OrdinalIgnoreCase));
			}
			return _customGraphs;
		}

		/// <summary>
		/// Load Asset by Guid
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="guid"></param>
		/// <returns></returns>
		public static T LoadAssetByGuid<T>(string guid) where T : UnityEngine.Object {
			var path = AssetDatabase.GUIDToAssetPath(guid);
			if(!string.IsNullOrEmpty(path)) {
				var asset = AssetDatabase.LoadAssetAtPath<T>(path);
				return asset;
			}
			return default(T);
		}

		/// <summary>
		/// Find all asset of type T in project
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public static IEnumerable<T> FindAssetsByType<T>() where T : UnityEngine.Object {
			string[] guids = AssetDatabase.FindAssets(string.Format("t:{0}", typeof(T)), new[] {"Assets"});
			if(guids.Length == 0) {
				guids = AssetDatabase.FindAssets(string.Format("t:{0}", typeof(T).Name), new[] {"Assets"});
			}
			for(int i = 0; i < guids.Length; i++) {
				string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
				T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
				if(asset != null) {
					yield return asset;
				}
			}
		}

		/// <summary>
		/// Find all asset of type T in project
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="searchInFolders"></param>
		/// <returns></returns>
		public static IEnumerable<T> FindAssetsByType<T>(string[] searchInFolders) where T : UnityEngine.Object {
			string[] guids = AssetDatabase.FindAssets(string.Format("t:{0}", typeof(T)), searchInFolders);
			if(guids.Length == 0) {
				guids = AssetDatabase.FindAssets(string.Format("t:{0}", typeof(T).Name), searchInFolders);
			}
			for(int i = 0; i < guids.Length; i++) {
				string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
				T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
				if(asset != null) {
					yield return asset;
				}
			}
		}

		/// <summary>
		/// Find all asset of type T in project
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public static IEnumerable<T> FindAssetsByType<T>(Func<Type, bool> validation) where T : UnityEngine.Object {
			string[] guids = AssetDatabase.FindAssets(string.Format("t:{0}", typeof(T)), new[] { "Assets" });
			if(guids.Length == 0) {
				guids = AssetDatabase.FindAssets(string.Format("t:{0}", typeof(T).Name), new[] { "Assets" });
			}
			for(int i = 0; i < guids.Length; i++) {
				string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
				if(validation != null) {
					if(validation(AssetDatabase.GetMainAssetTypeAtPath(assetPath)) == false) {
						continue;
					}
				}
				T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
				if(asset != null) {
					yield return asset;
				}
			}
		}

		/// <summary>
		/// Find all asset guid of type `type` in project
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static string[] FindAssetsGUIDByType(Type type) {
			string[] guids = AssetDatabase.FindAssets(string.Format("t:{0}", type), new[] { "Assets" });
			if(guids.Length == 0) {
				guids = AssetDatabase.FindAssets(string.Format("t:{0}", type.Name), new[] { "Assets" });
			}
			return guids;
		}

		/// <summary>
		/// Find all asset of type `type` in project
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static IEnumerable<UnityEngine.Object> FindAssetsByType(Type type) {
			string[] guids = AssetDatabase.FindAssets(string.Format("t:{0}", type), new[] { "Assets" });
			if (guids.Length == 0) {
				guids = AssetDatabase.FindAssets(string.Format("t:{0}", type.Name), new[] { "Assets" });
			}
			for (int i = 0; i < guids.Length; i++) {
				string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
				var asset = AssetDatabase.LoadAssetAtPath(assetPath, type);
				if (asset != null) {
					yield return asset;
				}
			}
		}

		/// <summary>
		/// Find all prefabs in project
		/// </summary>
		/// <returns></returns>
		public static List<GameObject> FindPrefabs() {
			var guids = AssetDatabase.FindAssets("t:Prefab", new[] {"Assets"});
			var result = new List<GameObject>();
			foreach(var guid in guids) {
				var path = AssetDatabase.GUIDToAssetPath(guid);
				var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
				if(go != null) {
					result.Add(go);
				}
			}
			return result;
		}

		/// <summary>
		/// Find all component of type T in prefab assets in the project.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public static List<T> FindComponentInPrefabs<T>() {
			var result = new List<T>();
			var guids = AssetDatabase.FindAssets("t:Prefab", new[] {"Assets"});
			foreach(var guid in guids) {
				var path = AssetDatabase.GUIDToAssetPath(guid);
				var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
				if(go != null) {
					var comp = go.GetComponent<T>();
					if(comp != null) {
						result.Add(comp);
					}
				}
			}
			return result;
		}

		/// <summary>
		/// Find all component of type type in prefab assets in the project.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public static List<Component> FindComponentInPrefabs(Type type) {
			var result = new List<Component>();
			var guids = AssetDatabase.FindAssets("t:Prefab", new[] {"Assets"});
			foreach(var guid in guids) {
				var path = AssetDatabase.GUIDToAssetPath(guid);
				var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
				if(go != null) {
					var comp = go.GetComponents(type);
					if(comp != null) {
						result.AddRange(comp);
					}
				}
			}
			return result;
		}

		/// <summary>
		/// Find all prefab that have T component in project.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public static List<GameObject> FindPrefabsOfType<T>(bool includeChildren = false) where T : Component {
			var guids = AssetDatabase.FindAssets("t:Prefab", new[] {"Assets"});
			var result = new List<GameObject>();
			foreach(var guid in guids) {
				var path = AssetDatabase.GUIDToAssetPath(guid);
				var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
				if(go != null) {
					var comp = go.GetComponent(typeof(T));
					if(comp != null) {
						result.Add(go);
					} else if(includeChildren) {
						var comps = go.GetComponentsInChildren(typeof(T));
						if(comps.Length > 0) {
							result.Add(go);
						}
					}
				}
			}
			return result;
		}

		public static bool IsNumericType(Type type) {
			switch(Type.GetTypeCode(type)) {
				case TypeCode.Byte:
				case TypeCode.SByte:
				case TypeCode.UInt16:
				case TypeCode.UInt32:
				case TypeCode.UInt64:
				case TypeCode.Int16:
				case TypeCode.Int32:
				case TypeCode.Int64:
				case TypeCode.Decimal:
				case TypeCode.Double:
				case TypeCode.Single:
					return true;
				default:
					return false;
			}
		}

		/// <summary>
		/// True if the target is a prefab.
		/// </summary>
		/// <param name="target"></param>
		/// <returns></returns>
		public static bool IsPrefab(UnityEngine.Object target) {
			if(target != null && target) {
				return PrefabUtility.GetPrefabType(target) == PrefabType.Prefab;
			}
			return false;
		}

		/// <summary>
		/// True if the target is a prefab.
		/// </summary>
		/// <param name="target"></param>
		/// <returns></returns>
		public static bool IsPrefabInstance(UnityEngine.Object target) {
			if(target != null && target) {
				return PrefabUtility.GetPrefabType(target) == PrefabType.PrefabInstance;
			}
			return false;
		}

		/// <summary>
		/// Save prefab asset.
		/// </summary>
		/// <param name="gameObject"></param>
		/// <param name="prefab"></param>
		public static GameObject SavePrefabAsset(GameObject gameObject, GameObject prefab) {
			if(IsPrefabInstance(gameObject)) {
				PrefabUtility.ApplyPrefabInstance(gameObject, InteractionMode.AutomatedAction);
				return prefab;
			}
			if(gameObject != null && prefab != null && !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(prefab))) {
				var result = PrefabUtility.SaveAsPrefabAsset(gameObject, AssetDatabase.GetAssetPath(prefab));
				MarkDirty(prefab);
				return result;
			} else {
				return null;
			}
		}

		public static void UnlockPrefabInstance(GameObject gameObject) {
			if (gameObject != null && PrefabUtility.IsPartOfNonAssetPrefabInstance(gameObject)) {
				PrefabUtility.UnpackPrefabInstance(gameObject, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
			}
		}

		/// <summary>
		/// Set the transform parent.
		/// </summary>
		/// <param name="from"></param>
		/// <param name="to"></param>
		public static void SetParent(Transform from, Transform to) {
			if(!from || !to)
				return;
			if(IsPrefab(to)) {
				Transform tr = PrefabUtility.InstantiatePrefab(to) as Transform;
				from.SetParent(tr);
				SavePrefabAsset(tr.root.gameObject, to.root.gameObject);
				UnityEngine.Object.DestroyImmediate(tr.root.gameObject);
			} else {
				from.SetParent(to);
			}
		}

		/// <summary>
		/// Save editor data.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="fileName"></param>
		public static void SaveEditorData<T>(T value, string fileName) {
			Directory.CreateDirectory(uNodePreference.preferenceDirectory);
			char separator = Path.DirectorySeparatorChar;
			string path = uNodePreference.preferenceDirectory + separator + fileName + ".byte";
			File.WriteAllBytes(path, SerializerUtility.Serialize(value));
		}

		/// <summary>
		/// Load editor data.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="fileName"></param>
		/// <returns></returns>
		public static T LoadEditorData<T>(string fileName) {
			char separator = Path.DirectorySeparatorChar;
			string path = uNodePreference.preferenceDirectory + separator + fileName + ".byte";
			T value;
			if(File.Exists(path)) {
				value = SerializerUtility.Deserialize<T>(File.ReadAllBytes(path));
			} else {
				value = default(T);
			}
			return value;
		}

		/// <summary>
		/// Save editor data.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="guid"></param>
		public static void SaveEditorDataOnDatabase<T>(T value, string guid) {
			uNodeDatabase.instance.SaveEditorData(guid, value);
		}

		/// <summary>
		/// Load editor data.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="guid"></param>
		/// <returns></returns>
		public static T LoadEditorDataOnDatabase<T>(string guid) {
			return uNodeDatabase.instance.LoadEditorData<T>(guid);
		}

		public static void AddDefineSymbols(IEnumerable<string> symbols) {
			string definesString = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
			List<string> allDefines = definesString.Split(';').ToList();
			allDefines.AddRange(symbols.Except(allDefines));
			PlayerSettings.SetScriptingDefineSymbolsForGroup(
				EditorUserBuildSettings.selectedBuildTargetGroup,
				string.Join(";", allDefines.ToArray()));
		}

		public static void RemoveDefineSymbols(IEnumerable<string> symbols) {
			string definesString = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
			List<string> allDefines = definesString.Split(';').ToList();
			allDefines.RemoveAll(d => symbols.Contains(d));
			PlayerSettings.SetScriptingDefineSymbolsForGroup(
				EditorUserBuildSettings.selectedBuildTargetGroup,
				string.Join(";", allDefines.ToArray()));
		}
#endregion

		#region GenericMenuUtils
		public static void ShowGenericOptionMenu(IList array, int index, Action<IList> action, UnityEngine.Object unityObject = null) {
			GenericMenu menu = new GenericMenu();
			menu.AddItem(new GUIContent("Duplicate Element"), false, delegate (object obj) {
				uNodeEditorUtility.RegisterUndo(unityObject, "Duplicate Array Element");
				object newObj = array[index];
				if(newObj != null) {
					if(!newObj.GetType().IsValueType && !(newObj.GetType() == typeof(UnityEngine.Object) || newObj.GetType().IsSubclassOf(typeof(UnityEngine.Object)))) {
						newObj = SerializerUtility.Duplicate(newObj);
					}
				}
				array.Insert(index, newObj);
				if(action != null) {
					action(array);
				}
			}, index);
			menu.AddItem(new GUIContent("Delete Element"), false, delegate (object obj) {
				uNodeEditorUtility.RegisterUndo(unityObject, "Delete Array Element");
				array.RemoveAt(index);
				if(action != null) {
					action(array);
				}
			}, index);
			menu.AddSeparator("");
			if(index != 0) {
				menu.AddItem(new GUIContent("Move To Top"), false, delegate (object obj) {
					uNodeEditorUtility.RegisterUndo(unityObject, "Move Array Element To Top");
					int nums = (int)obj;
					ListMoveToTop(array, nums);
					if(action != null) {
						action(array);
					}
				}, index);
				menu.AddItem(new GUIContent("Move Up"), false, delegate (object obj) {
					uNodeEditorUtility.RegisterUndo(unityObject, "Move Array Element Up");
					int nums = (int)obj;
					ListMoveUp(array, nums);
					if(action != null) {
						action(array);
					}
				}, index);
			}
			if(index + 1 != array.Count) {
				menu.AddItem(new GUIContent("Move Down"), false, delegate (object obj) {
					if(unityObject)
						uNodeEditorUtility.RegisterUndo(unityObject, "Move Array Element Down");
					int nums = (int)obj;
					ListMoveDown(array, nums);
					if(action != null) {
						action(array);
					}
				}, index);
				menu.AddItem(new GUIContent("Move To End"), false, delegate (object obj) {
					uNodeEditorUtility.RegisterUndo(unityObject, "Move Array Element To End");
					int nums = (int)obj;
					ListMoveToBottom(array, nums);
					if(action != null) {
						action(array);
					}
				}, index);
			}
			menu.ShowAsContext();
		}

		public static void ListMoveUp(IList list, int index) {
			if(index != 0) {
				object self = list[index];
				list.RemoveAt(index);
				list.Insert(index - 1, self);
			}
		}

		public static void ListMoveDown(IList list, int index) {
			if(index + 1 != list.Count) {
				object self = list[index];
				list.RemoveAt(index);
				list.Insert(index + 1, self);
			}
		}

		public static void ListMoveToTop(IList list, int index) {
			if(index != 0) {
				var self = list[index];
				list.RemoveAt(index);
				list.Insert(0, self);
			}
		}

		public static void ListMoveToBottom(IList list, int index) {
			if(index + 1 != list.Count) {
				var self = list[index];
				list.RemoveAt(index);
				list.Add(self);
			}
		}

		public static void ShowArrayOptionMenu(Array array, int index, Action<Array> action, UnityEngine.Object unityObject = null) {
			GenericMenu menu = new GenericMenu();
			menu.AddItem(new GUIContent("Duplicate Element"), false, delegate (object obj) {
				uNodeEditorUtility.RegisterUndo(unityObject, "Duplicate Array Element");
				int nums = (int)obj;
				uNodeUtility.DuplicateArrayAt(ref array, nums);
				if(action != null)
					action(array);
			}, index);
			menu.AddItem(new GUIContent("Delete Element"), false, delegate (object obj) {
				uNodeEditorUtility.RegisterUndo(unityObject, "Delete Array Element");
				int nums = (int)obj;
				uNodeUtility.RemoveArrayAt(ref array, nums);
				if(action != null)
					action(array);
			}, index);
			menu.AddSeparator("");
			if(index != 0) {
				menu.AddItem(new GUIContent("Move To Top"), false, delegate (object obj) {
					uNodeEditorUtility.RegisterUndo(unityObject, "Move Array Element To Top");
					int nums = (int)obj;
					object self = array.GetValue(nums);
					object target = null;
					for(int i = 0; i < array.Length; i++) {
						object element = target;
						target = array.GetValue(i);
						if(i == 0) {
							array.SetValue(self, i);
							continue;
						}
						if(i <= nums) {
							array.SetValue(element, i);
							continue;
						}
						array.SetValue(target, i);
					}
					if(action != null)
						action(array);
				}, index);
				menu.AddItem(new GUIContent("Move Up"), false, delegate (object obj) {
					uNodeEditorUtility.RegisterUndo(unityObject, "Move Array Element Up");
					int nums = (int)obj;
					object self = array.GetValue(nums);
					object target = array.GetValue(nums - 1);
					array.SetValue(self, nums - 1);
					array.SetValue(target, nums);
					if(action != null)
						action(array);
				}, index);
			}
			if(index + 1 != array.Length) {
				menu.AddItem(new GUIContent("Move Down"), false, delegate (object obj) {
					uNodeEditorUtility.RegisterUndo(unityObject, "Move Array Element Down");
					int nums = (int)obj;
					object self = array.GetValue(nums);
					object target = array.GetValue(nums + 1);
					array.SetValue(self, nums + 1);
					array.SetValue(target, nums);
					if(action != null)
						action(array);
				}, index);
				menu.AddItem(new GUIContent("Move To End"), false, delegate (object obj) {
					uNodeEditorUtility.RegisterUndo(unityObject, "Move Array Element To End");
					int nums = (int)obj;
					object self = array.GetValue(nums);
					object target = null;
					for(int i = 0; i < array.Length; i++) {
						if(i + 1 == array.Length) {
							array.SetValue(self, i);
							continue;
						}
						if(i < nums)
							continue;
						if(i + 1 != array.Length) {
							target = array.GetValue(i + 1);
						}
						array.SetValue(target, i);
					}
					if(action != null)
						action(array);
				}, index);
			}
			menu.ShowAsContext();
		}

		public static void ShowTypeMenu(object userData, Action<object, Type> onClick) {
			GenericMenu menu = new GenericMenu();
			ShowTypeMenu(userData, onClick, menu);
			menu.ShowAsContext();
		}

		public static void ShowTypeMenu(object userData, Action<object, Type> onClick, GenericMenu menu) {
			menu.AddItem(new GUIContent("String"), false, delegate (object obj) {
				onClick(obj, typeof(string));
			}, userData);
			menu.AddItem(new GUIContent("Bool"), false, delegate (object obj) {
				onClick(obj, typeof(bool));
			}, userData);
			menu.AddItem(new GUIContent("Int"), false, delegate (object obj) {
				onClick(obj, typeof(int));
			}, userData);
			menu.AddItem(new GUIContent("Float"), false, delegate (object obj) {
				onClick(obj, typeof(float));
			}, userData);
			menu.AddItem(new GUIContent("Vector2"), false, delegate (object obj) {
				onClick(obj, typeof(Vector2));
			}, userData);
			menu.AddItem(new GUIContent("Vector3"), false, delegate (object obj) {
				onClick(obj, typeof(Vector3));
			}, userData);
			menu.AddItem(new GUIContent("Vector4"), false, delegate (object obj) {
				onClick(obj, typeof(Vector4));
			}, userData);
			menu.AddItem(new GUIContent("Quaternion"), false, delegate (object obj) {
				onClick(obj, typeof(Quaternion));
			}, userData);
			menu.AddItem(new GUIContent("Rect"), false, delegate (object obj) {
				onClick(obj, typeof(Rect));
			}, userData);
			menu.AddItem(new GUIContent("Color"), false, delegate (object obj) {
				onClick(obj, typeof(Color));
			}, userData);
			menu.AddItem(new GUIContent("Transform"), false, delegate (object obj) {
				onClick(obj, typeof(Transform));
			}, userData);
			menu.AddItem(new GUIContent("GameObject"), false, delegate (object obj) {
				onClick(obj, typeof(GameObject));
			}, userData);
			menu.AddSeparator("");
			menu.AddItem(new GUIContent("Object"), false, delegate (object obj) {
				onClick(obj, typeof(UnityEngine.Object));
			}, userData);
			menu.AddItem(new GUIContent("System.Object"), false, delegate (object obj) {
				onClick(obj, typeof(object));
			}, userData);
			menu.AddSeparator("");
			menu.AddItem(new GUIContent("Array/String"), false, delegate (object obj) {
				onClick(obj, typeof(string[]));
			}, userData);
			menu.AddItem(new GUIContent("Array/Bool"), false, delegate (object obj) {
				onClick(obj, typeof(bool[]));
			}, userData);
			menu.AddItem(new GUIContent("Array/Int"), false, delegate (object obj) {
				onClick(obj, typeof(int[]));
			}, userData);
			menu.AddItem(new GUIContent("Array/Float"), false, delegate (object obj) {
				onClick(obj, typeof(float[]));
			}, userData);
			menu.AddItem(new GUIContent("Array/Vector2"), false, delegate (object obj) {
				onClick(obj, typeof(Vector2[]));
			}, userData);
			menu.AddItem(new GUIContent("Array/Vector3"), false, delegate (object obj) {
				onClick(obj, typeof(Vector3[]));
			}, userData);
			menu.AddItem(new GUIContent("Array/Vector4"), false, delegate (object obj) {
				onClick(obj, typeof(Vector4[]));
			}, userData);
			menu.AddItem(new GUIContent("Array/Quaternion"), false, delegate (object obj) {
				onClick(obj, typeof(Quaternion[]));
			}, userData);
			menu.AddItem(new GUIContent("Array/Rect"), false, delegate (object obj) {
				onClick(obj, typeof(Rect[]));
			}, userData);
			menu.AddItem(new GUIContent("Array/Color"), false, delegate (object obj) {
				onClick(obj, typeof(Color[]));
			}, userData);
			menu.AddItem(new GUIContent("Array/Transform"), false, delegate (object obj) {
				onClick(obj, typeof(Transform[]));
			}, userData);
			menu.AddItem(new GUIContent("Array/GameObject"), false, delegate (object obj) {
				onClick(obj, typeof(GameObject[]));
			}, userData);
			menu.AddSeparator("Array/");
			menu.AddItem(new GUIContent("Array/Object"), false, delegate (object obj) {
				onClick(obj, typeof(UnityEngine.Object[]));
			}, userData);
			menu.AddItem(new GUIContent("Array/System.Object"), false, delegate (object obj) {
				onClick(obj, typeof(object[]));
			}, userData);
			menu.AddSeparator("");
			menu.AddItem(new GUIContent("Generic List/String"), false, delegate (object obj) {
				onClick(obj, typeof(List<string>));
			}, userData);
			menu.AddItem(new GUIContent("Generic List/Bool"), false, delegate (object obj) {
				onClick(obj, typeof(List<bool>));
			}, userData);
			menu.AddItem(new GUIContent("Generic List/Int"), false, delegate (object obj) {
				onClick(obj, typeof(List<int>));
			}, userData);
			menu.AddItem(new GUIContent("Generic List/Float"), false, delegate (object obj) {
				onClick(obj, typeof(List<float>));
			}, userData);
			menu.AddItem(new GUIContent("Generic List/Vector2"), false, delegate (object obj) {
				onClick(obj, typeof(List<Vector2>));
			}, userData);
			menu.AddItem(new GUIContent("Generic List/Vector3"), false, delegate (object obj) {
				onClick(obj, typeof(List<Vector3>));
			}, userData);
			menu.AddItem(new GUIContent("Generic List/Vector4"), false, delegate (object obj) {
				onClick(obj, typeof(List<Vector4>));
			}, userData);
			menu.AddItem(new GUIContent("Generic List/Quaternion"), false, delegate (object obj) {
				onClick(obj, typeof(List<Quaternion>));
			}, userData);
			menu.AddItem(new GUIContent("Generic List/Rect"), false, delegate (object obj) {
				onClick(obj, typeof(List<Rect>));
			}, userData);
			menu.AddItem(new GUIContent("Generic List/Color"), false, delegate (object obj) {
				onClick(obj, typeof(List<Color>));
			}, userData);
			menu.AddItem(new GUIContent("Generic List/Transform"), false, delegate (object obj) {
				onClick(obj, typeof(List<Transform>));
			}, userData);
			menu.AddItem(new GUIContent("Generic List/GameObject"), false, delegate (object obj) {
				onClick(obj, typeof(List<GameObject>));
			}, userData);
			menu.AddSeparator("Generic List/");
			menu.AddItem(new GUIContent("Generic List/Object"), false, delegate (object obj) {
				onClick(obj, typeof(List<UnityEngine.Object>));
			}, userData);
			menu.AddItem(new GUIContent("Generic List/System.Object"), false, delegate (object obj) {
				onClick(obj, typeof(List<object>));
			}, userData);
		}
		#endregion
	}
	
	#region EditorProgressBar
	public static class EditorProgressBar {
		static bool isDisplaying;

		public static bool IsInProgress() {
			return isDisplaying;
		}

#if UNITY_2020_1_OR_NEWER
		static int progressId;
		public static void ShowProgressBar(string description, float progress) {
			if(!isDisplaying) {
				isDisplaying = true;
				progressId = Progress.Start("uNode");
			}
			Progress.Report(progressId, progress, description);
		}

		public static void ClearProgressBar() {
			if(isDisplaying) {
				isDisplaying = false;
				Progress.Remove(progressId);
			}
		}
#else
		static MethodInfo m_Display = null;
		static MethodInfo m_Clear = null;
		static EditorProgressBar() {
			var type = typeof(Editor).Assembly.GetTypes().Where(t => t.Name == "AsyncProgressBar").FirstOrDefault();
			if(type != null) {
				m_Display = type.GetMethod("Display");
				m_Clear = type.GetMethod("Clear");
			}
		}

		public static void ShowProgressBar(string description, float progress) {
			try {
				if(m_Display != null) {
					m_Display.InvokeOptimized(null, new object[] { description, progress });
					isDisplaying = true;
				}
			}
			catch { }
		}

		public static void ClearProgressBar() {
			try {
				if(m_Clear != null) {
					m_Clear.InvokeOptimized(null, null);
					isDisplaying = false;
				}
			}
			catch { }
		}
#endif
	}
	#endregion
}