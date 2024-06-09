using UnityEngine;
using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Object = UnityEngine.Object;

namespace MaxyGames.UNode {
	/// <summary>
	/// Class Utility for uNode.
	/// </summary>
	public static class uNodeUtility {
		#region Callback
		internal static Func<UnityEngine.Object, int> getObjectID;
		#endregion

		#region Texture
		private static Texture2D _debugPoint;
		public static Texture2D DebugPoint {
			get {
				if(_debugPoint == null) {
					_debugPoint = Resources.Load<Texture2D>("Debug_Point");
				}
				return _debugPoint;
			}
		}
		#endregion

		#region Editor
		static uNodeUtility() {
#if !UNITY_EDITOR
			EditorTextSetting setting = new EditorTextSetting() {
				useRichText = false,
			};
			richTextColor = () => setting;
			getColorForType = (_) => Color.clear;
#endif
		}

#if UNITY_EDITOR
		internal static class ProBinding {
			public static Func<Graph, Graph> CallbackGetTrimmedGraph;

			public static Graph GetTrimmedGraph(Graph graph) {
				return CallbackGetTrimmedGraph?.Invoke(graph);
			}
		}
#endif

#if UNODE_TRIM_ON_BUILD
		internal static HashSet<UnityEngine.Object> trimmedObjects;
#endif

		public static bool IsProVersion {
			get {
#if UNODE_PRO
				return true;
#else
				return false;
#endif
			}
		}

		/// <summary>
		/// this will filled from uNodeEditorInitializer
		/// </summary>
		public static Func<EditorTextSetting> richTextColor;
		/// <summary>
		/// this will filled from uNodeEditorInitializer
		/// </summary>
		public static Func<Type, Color> getColorForType;
		/// <summary>
		/// True, if running in editor.
		/// </summary>
		public static bool isInEditor = false;
		public static bool preferredLongName {
			get {
				return preferredDisplay == DisplayKind.Full;
			}
		}
		public static DisplayKind preferredDisplay = DisplayKind.Default;

		/// <summary>
		/// True if undo / redo action is performed.
		/// </summary>
		public static bool undoRedoPerformed { get; internal set; }

		public static bool isOSXPlatform => Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer;

		public static bool isPlaying = true;

		//public static GUIContent GetDisplayContent(object value) {
		//	string name = GetDisplayName(value);
		//	if(string.IsNullOrEmpty(name)) {
		//		return GUIContent.none;
		//	}
		//	return new GUIContent(name, name);
		//}

		public static EditorTextSetting GetRichTextSetting() {
			return richTextColor();
		}

		/// <summary>
		/// Wrap Text with HTML color code
		/// </summary>
		/// <param name="text"></param>
		/// <param name="color"></param>
		/// <returns></returns>
		private static string WrapWithColor(this string text, Color color) {
			return string.Format("<color=#{0}>{1}</color>", ColorUtility.ToHtmlStringRGB(color), text);
		}

		/// <summary>
		/// Wrap Text with HTML bold code
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		public static string WrapTextWithBold(string text) {
			return string.Format("<b>{0}</b>", text);
		}

		/// <summary>
		/// Wrap Text with HTML italic code
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		public static string WrapTextWithItalic(string text) {
			return string.Format("<i>{0}</i>", text);
		}

		/// <summary>
		/// Wrap Text with HTML color code, the color is keyword color
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		public static string WrapTextWithKeywordColor(string text) {
			return WrapTextWithColor(text, GetRichTextSetting().keywordColor);
		}

		/// <summary>
		/// Wrap Text with HTML color code, the color is type color
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		public static string WrapTextWithTypeColor(string text) {
			return WrapTextWithColor(text, GetRichTextSetting().typeColor);
		}

		/// <summary>
		/// Wrap Text with HTML color code, the color is type color
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		public static string WrapTextWithTypeColor(string text, Type type) {
			return WrapTextWithColor(text, getColorForType(type));
		}

		/// <summary>
		/// Wrap Text with HTML color code, the color is other color
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		public static string WrapTextWithOtherColor(string text) {
			return WrapTextWithColor(text, GetRichTextSetting().otherColor);
		}

		/// <summary>
		/// Wrap Text with HTML color code
		/// </summary>
		/// <param name="text"></param>
		/// <param name="color"></param>
		/// <returns></returns>
		public static string WrapTextWithColor(string text, Color color) {
			return string.Format("<color=#{0}>{1}</color>", ColorUtility.ToHtmlStringRGB(color), text);
		}

		/// <summary>
		/// Wrap Text with HTML color code
		/// </summary>
		/// <param name="text"></param>
		/// <param name="color"></param>
		/// <param name="ignoreClearColor"></param>
		/// <returns></returns>
		public static string WrapTextWithColor(string text, Color color, bool ignoreClearColor) {
			if(!ignoreClearColor && color.a == 0) {
				return text;
			}
			return string.Format("<color=#{0}>{1}</color>", ColorUtility.ToHtmlStringRGB(color), text);
		}

		public static void LogAsJson(object value) {
			if(value == null) {
				Debug.Log(null);
			}
			else {
				Debug.Log(System.Text.Encoding.UTF8.GetString(OdinSerializer.SerializationUtility.SerializeValue(value, OdinSerializer.DataFormat.JSON)));
			}
		}
#endregion

		#region Runtime
		private static uNodeDatabase resourceDatabase;
		/// <summary>
		/// Get uNode database and create new one if doesn't exist.
		/// </summary>
		/// <returns></returns>
		public static uNodeDatabase GetDatabase() {
			if(resourceDatabase == null) {
				if(uNodeThreadUtility.IsInMainThread) {
					DoInitDatabase();
				}
				else {
					uNodeThreadUtility.QueueAndWait(DoInitDatabase);
				}
			}
			return resourceDatabase;
		}

		private static void DoInitDatabase() {
			resourceDatabase = Resources.Load<uNodeDatabase>("uNodeDatabase");
#if UNITY_EDITOR
			if(resourceDatabase == null) {
				resourceDatabase = ScriptableObject.CreateInstance<uNodeDatabase>();
				var dbDir = "Assets" + System.IO.Path.DirectorySeparatorChar + "uNode.Generated" + System.IO.Path.DirectorySeparatorChar + "Resources";
				System.IO.Directory.CreateDirectory(dbDir);
				var path = dbDir + System.IO.Path.DirectorySeparatorChar + "uNodeDatabase.asset";
				Debug.Log($"No database found, creating new database in: {path}");
				UnityEditor.AssetDatabase.CreateAsset(resourceDatabase, path);
			}
#endif
		}

		public static IGlobalEvent GetGlobalEvent(string guid) {
			return GetDatabase().GetGlobalEvent(guid);
		}

		public static T RuntimeGetValue<T>(Func<T> func) {
			return func();
		}
		#endregion

		#region Others
		/// <summary>
		/// Class setting for edit value
		/// </summary>
		public class EditValueSettings {
			/// <summary>
			/// The parent object.
			/// </summary>
			public object parentValue;
			/// <summary>
			/// Allow UnityReference to be edited.
			/// </summary>
			public bool acceptUnityObject = true;
			/// <summary>
			/// Allow reference/class object to be null.
			/// </summary>
			public bool nullable = false;
			public UnityEngine.Object unityObject;
			public object[] attributes;

			public string Tooltip {
				get {
					if(attributes != null) {
						for(int i = 0; i < attributes.Length; i++) {
							if(attributes[i] is TooltipAttribute tooltip) {
								return tooltip.tooltip;
							}
						}
					}
					return string.Empty;
				}
			}

			public bool drawDecorator = true;

			public EditValueSettings() { }

			public EditValueSettings(object parentInstance) {
				this.parentValue = parentInstance;
			}

			public EditValueSettings(bool acceptUnityObject, bool nullable, object parentInstance = null) {
				this.acceptUnityObject = acceptUnityObject;
				this.nullable = nullable;
				this.parentValue = parentInstance;
			}

			public EditValueSettings(EditValueSettings other) {
				this.parentValue = other.parentValue;
				this.nullable = other.nullable;
				this.acceptUnityObject = other.acceptUnityObject;
				this.attributes = other.attributes;
				this.unityObject = other.unityObject;
				this.drawDecorator = other.drawDecorator;
			}
		}

		public static Graph GetGraphData(object obj) {
			if(obj is IGraph) {
				return (obj as IGraph).GraphData;
			}
			if(obj is UGraphElement) {
				return (obj as UGraphElement).graph;
			}
			return null;
		}

		/// <summary>
		/// Validate that variable name is valid
		/// </summary>
		/// <param name="Name">The name to validate</param>
		/// <param name="otherNames">Optional: other name to validate that contains Name</param>
		/// <returns></returns>
		public static bool IsValidVariableName(string variableName, IList<string> otherNames = null) {
			if(string.IsNullOrEmpty(variableName) || otherNames != null && otherNames.Contains(variableName)) {
				return false;
			}
			if(variableName.Length == 0)
				return false;
			var strs = variableName;
			for(int i = 0; i < strs.Length; i++) {
				var c = strs[i];
				if(i == 0 && char.IsDigit(c)) {
					return false;
				}
				else if(c == ' ') {
					return false;
				}
				else if(char.IsSymbol(c) && c != '@') {
					return false;
				}
			}
			return true;
		}

		private static HashSet<string> csharpKeyword = new HashSet<string>() {
			"for",
			"foreach",
			"while",
			"using",
			"try",
			"catch",
			"finally",
			"do",
			"out",
			"in",
			"ref",
			"interface",
			"class",
			"struct",
			"enum",
			"false",
			"true",
			"null",
			"bool",
			"byte",
			"char",
			"decimal",
			"double",
			"float",
			"int",
			"long",
			"object",
			"sbyte",
			"short",
			"string",
			"uint",
			"ulong",
		};
		public static string AutoCorrectName(string name) {
			if(name == null)
				return "_";
			if(csharpKeyword.Contains(name)) {
				return "_" + name;
			}
			var strs = name.ToList();
			for(int i = 0; i < strs.Count; i++) {
				var c = strs[i];
				if(i == 0 && char.IsDigit(c)) {
					strs.Insert(0, '_');
					i--;
				}
				else if(c == ' ') {
					strs[i] = '_';
				}
				else if(!char.IsLetterOrDigit(c) && c != '@' && c != '_') {
					strs.RemoveAt(i);
					i--;
				}
			}
			return string.Join("", strs);
		}

		private static System.Random _random;
		/// <summary>
		/// Generate a unique id.
		/// </summary>
		/// <returns></returns>
		public static string GenerateUID() {
#if UNITY_EDITOR
			try {
				if(_random == null)
					_random = new System.Random();
				//Generate guid ( we use this because it's much smaller data size than using System.Guid )
				return $"{_random.Next().ToString("x")}{_random.Next().ToString("x")}";
				//return UnityEditor.GUID.Generate().ToString();
			}
			catch { }
#endif
			//In case error or not within Unity Editor, generate unique id using System.Guid instead.
			return Guid.NewGuid().ToString();
		}
		private static System.Security.Cryptography.MD5 MD5;

		#region Hashing
		/// <summary>
		/// Get hashcode based on multiple hashcode
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		public static int GetHashCode(IEnumerable<int> hashCodes) {
			int result = 17;
			foreach(var c in hashCodes) {
				unchecked {
					result = result * 31 + c;
				}
			}
			return result;
		}

		/// <summary>
		/// Generate a unique number from a 2 value
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static int GetHashCode(int a, int b) {
			return ((a + b) * (a + b + 1) / 2) + b;
		}

		/// <summary>
		/// Get hashcode from a 2 value
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static long GetHashCode(long a, long b) {
			return ((a + b) * (a + b + 1) / 2) + b;
		}

		/// <summary>
		/// Get hashcode based on string
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		/// <remarks>When input same, the result should be same but not 100% guarantee</remarks>
		public static int GetHashCode(string str) {
			const uint offsetBasis = 2166136261;
			const uint prime = 16777619;

			uint result = offsetBasis;
			foreach(var c in str) {
				result = prime * (result ^ (byte)(c & 255));
				result = prime * (result ^ (byte)(c >> 8));
			}
			return (int)result;
		}

		/// <summary>
		/// Get unique identifier based on byte array
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		/// <remarks>When input same, the result should be same but not 100% guarantee</remarks>
		public static int GetHashCode(byte[] bytes) {
			const uint offsetBasis = 2166136261;
			const uint prime = 16777619;

			uint result = offsetBasis;
			foreach(var c in bytes) {
				unchecked {
					result = prime * (result ^ c);
				}
			}
			return (int)result;
		}

		/// <summary>
		/// Get hashcode based on byte array
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		/// <remarks>When input same, the result should be same but not 100% guarantee</remarks>
		public static long GetHashCode64(byte[] bytes) {
			const ulong offsetBasis = 14695981039346656037;
			const ulong prime = 1099511628211;

			ulong result = offsetBasis;
			foreach(var c in bytes) {
				unchecked {
					result = prime * (result ^ c);
				}
			}
			return (long)result;
		}

		/// <summary>
		/// Get file hash
		/// </summary>
		/// <param name="filePath"></param>
		/// <returns></returns>
		/// <remarks>When input same, the result should be same but not 100% guarantee</remarks>
		public static long GetFileHash(string filePath) {
			using var md5 = System.Security.Cryptography.MD5.Create();
			using(var stream = System.IO.File.OpenRead(filePath)) {
				var hashBytes = md5.ComputeHash(stream);
				return GetHashCode64(hashBytes);
			}
		}
		#endregion

		/// <summary>
		/// Get object unique identifier.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public static int GetObjectID(UnityEngine.Object obj) {
			if(getObjectID == null) {
#if UNITY_EDITOR
				throw new Exception("uNode is not initialized");
#else
				return obj.GetHashCode();
#endif
			}
			return getObjectID(obj);
		}

		#endregion

		#region GetDisplayName

		public static string GetFullDisplayName(object value) {
			if(value is MemberData) {
				return GetDisplayName(value as MemberData, true, true);
			}
			return GetDisplayName(value);
		}

		public static string GetDisplayName(this MemberData member, bool longName = false, bool typeTargetWithTypeof = true) {
			if(member == null || !member.isAssigned) {
				return "(none)";
			}
			return member.DisplayName(longName, typeTargetWithTypeof);
		}

		public static string GetDisplayName(this MemberData member, DisplayKind displayKind, bool typeTargetWithTypeof = true) {
			if(member == null || !member.isAssigned) {
				return "(none)";
			}
			switch(member.targetType) {
				case MemberData.TargetType.None:
					return "(none)";
				case MemberData.TargetType.Self:
					return "this";
				case MemberData.TargetType.Null:
					return "null";
				case MemberData.TargetType.NodePort:
					//if(member.GetTargetNode() == null) {
					//	goto case MemberData.TargetType.None;
					//}
					return "#Port";
				case MemberData.TargetType.Values:
					return GetDisplayName(member.Get(null));
					//if(member.type != null) {
					//	return member.type.PrettyName().WrapWithColor(editorColor.typeColor);
					//}
					//return member.targetTypeName.WrapWithColor(editorColor.typeColor);
			}
			if(displayKind != DisplayKind.Partial || member.IsTargetingUNode) {
				return member.DisplayName(displayKind == DisplayKind.Full, typeTargetWithTypeof);
			}
			string result = member.DisplayName(displayKind == DisplayKind.Full, typeTargetWithTypeof);
			if(!member.isDeepTarget && member.Items.Length > 1) {
				int index = result.IndexOf('.');
				return result.Substring(0, index > 0 ? index : 0);
			}
			return result;
		}

		public static string GetDisplayName(object value) {
			if(!object.ReferenceEquals(value, null)) {
				if(value is MemberData) {
					MemberData member = value as MemberData;
					if(member.isAssigned) {
						return GetDisplayName(member, preferredLongName, true);
					}
					return "(none)";
				}
				else if(value is MultipurposeMember) {
					MultipurposeMember member = value as MultipurposeMember;
					if(member.target != null) {
						return GetDisplayName(member.target, preferredLongName, true);
					}
					else {
						return "Unassigned";
					}
				}
				else if(value is UnityEngine.Object uobj && uobj != null) {
					return $"{uobj.name} ({uobj.GetType().PrettyName()})";
				}
				else if(value is string) {
					return "\"" + value.ToString() + "\"";
				}
				else if(value is Type) {//Type
					return (value as Type).PrettyName();
				}
				else if(value is Vector2) {//Vector2
					Vector2 vec = (Vector2)value;
					if(vec == Vector2.down) {
						return "Vector2.down";
					}
					else if(vec == Vector2.left) {
						return "Vector2.left";
					}
					else if(vec == Vector2.one) {
						return "Vector2.one";
					}
					else if(vec == Vector2.right) {
						return "Vector2.right";
					}
					else if(vec == Vector2.up) {
						return "Vector2.up";
					}
					else if(vec == Vector2.zero) {
						return "Vector2.zero";
					}
					return $"({vec.x}, {vec.y})";
				}
				else if(value is Vector3) {//Vector3
					Vector3 vec = (Vector3)value;
					if(vec == Vector3.back) {
						return "Vector3.back";
					}
					else if(vec == Vector3.down) {
						return "Vector3.down";
					}
					else if(vec == Vector3.forward) {
						return "Vector3.forward";
					}
					else if(vec == Vector3.left) {
						return "Vector3.left";
					}
					else if(vec == Vector3.one) {
						return "Vector3.one";
					}
					else if(vec == Vector3.right) {
						return "Vector3.right";
					}
					else if(vec == Vector3.up) {
						return "Vector3.up";
					}
					else if(vec == Vector3.zero) {
						return "Vector3.zero";
					}
					return $"({vec.x}, {vec.y}, {vec.z})";
				}
				else if(value is Color) {//Color
					Color color = (Color)value;
					if(color == Color.black) {
						return "Color.black";
					}
					else if(color == Color.blue) {
						return "Color.blue";
					}
					else if(color == Color.clear) {
						return "Color.clear";
					}
					else if(color == Color.cyan) {
						return "Color.cyan";
					}
					else if(color == Color.gray) {
						return "Color.gray";
					}
					else if(color == Color.green) {
						return "Color.green";
					}
					else if(color == Color.grey) {
						return "Color.grey";
					}
					else if(color == Color.magenta) {
						return "Color.magenta";
					}
					else if(color == Color.red) {
						return "Color.red";
					}
					else if(color == Color.white) {
						return "Color.white";
					}
					else if(color == Color.yellow) {
						return "Color.yellow";
					}
					return $"({color.r}, {color.g}, {color.b}, {color.a})";
				}
				else if(value is Enum) {//Enum
					if(value is ComparisonType) {
						switch((ComparisonType)value) {
							case ComparisonType.Equal:
								return "==";
							case ComparisonType.GreaterThan:
								return ">";
							case ComparisonType.GreaterThanOrEqual:
								return ">=";
							case ComparisonType.LessThan:
								return "<";
							case ComparisonType.LessThanOrEqual:
								return "<=";
							case ComparisonType.NotEqual:
								return "!=";
						}
					}
					else if(value is ArithmeticType) {
						switch((ArithmeticType)value) {
							case ArithmeticType.Add:
								return "+";
							case ArithmeticType.Divide:
								return "/";
							case ArithmeticType.Modulo:
								return "%";
							case ArithmeticType.Multiply:
								return "*";
							case ArithmeticType.Subtract:
								return "-";
						}
					}
					else if(value is SetType) {
						switch((SetType)value) {
							case SetType.Add:
								return "+=";
							case SetType.Change:
								return "=";
							case SetType.Divide:
								return "/=";
							case SetType.Modulo:
								return "%=";
							case SetType.Multiply:
								return "*=";
							case SetType.Subtract:
								return "-=";
						}
					}
					else {
						return value.ToString();
					}
				}
				if(value.GetType().IsPrimitive) {
					return value.ToString();
				}
				return (value.GetType()).PrettyName();
			}
			return "null";
		}

		public static string GetObjectName(object value) {
			if(!object.ReferenceEquals(value, null)) {
				if(value is MemberData) {
					MemberData member = value as MemberData;
					if(member.isAssigned) {
						return GetDisplayName(member, preferredLongName, true);
					}
					return "(none)";
				}
				else if(value is MultipurposeMember) {
					MultipurposeMember member = value as MultipurposeMember;
					if(member.target != null) {
						return GetDisplayName(member.target, preferredLongName, true);
					}
					else {
						return "Unassigned";
					}
				}
				else if(value is IGraph) {
					return (value as IGraph).GetGraphName();
				}
				else if(value is UnityEngine.Object uobj) {
					if(uobj == null) {
						return "null";
					}
					return uobj.name;
				}
				else if(value is IInstancedGraph) {
					return (value as IInstancedGraph).OriginalGraph.GetGraphName();
				}
				else if(value is string) {
					return "\"" + value.ToString() + "\"";
				}
				else if(value is Type) {//Type
					return (value as Type).PrettyName();
				}
				else if(value is Vector2) {//Vector2
					Vector2 vec = (Vector2)value;
					return $"({vec.x}, {vec.y})";
				}
				else if(value is Vector2Int) {//Vector2Int
					Vector2Int vec = (Vector2Int)value;
					return $"({vec.x}, {vec.y})";
				}
				else if(value is Vector3) {//Vector3
					Vector3 vec = (Vector3)value;
					return $"({vec.x}, {vec.y}, {vec.z})";
				}
				else if(value is Vector3Int) {//Vector3Int
					Vector3 vec = (Vector3Int)value;
					return $"({vec.x}, {vec.y}, {vec.z})";
				}
				else if(value is Vector4) {//Vector4
					Vector4 vec = (Vector4)value;
					return $"({vec.x}, {vec.y}, {vec.z}, {vec.w})";
				}
				else if(value is Color) {//Color
					Color color = (Color)value;
					if(color == Color.black) {
						return "Color.black";
					}
					else if(color == Color.blue) {
						return "Color.blue";
					}
					else if(color == Color.clear) {
						return "Color.clear";
					}
					else if(color == Color.cyan) {
						return "Color.cyan";
					}
					else if(color == Color.gray) {
						return "Color.gray";
					}
					else if(color == Color.green) {
						return "Color.green";
					}
					else if(color == Color.grey) {
						return "Color.grey";
					}
					else if(color == Color.magenta) {
						return "Color.magenta";
					}
					else if(color == Color.red) {
						return "Color.red";
					}
					else if(color == Color.white) {
						return "Color.white";
					}
					else if(color == Color.yellow) {
						return "Color.yellow";
					}
					return $"({color.r}, {color.g}, {color.b}, {color.a})";
				}
				else if(value is Enum) {//Enum
					if(value is ComparisonType) {
						switch((ComparisonType)value) {
							case ComparisonType.Equal:
								return "==";
							case ComparisonType.GreaterThan:
								return ">";
							case ComparisonType.GreaterThanOrEqual:
								return ">=";
							case ComparisonType.LessThan:
								return "<";
							case ComparisonType.LessThanOrEqual:
								return "<=";
							case ComparisonType.NotEqual:
								return "!=";
						}
					}
					else if(value is ArithmeticType) {
						switch((ArithmeticType)value) {
							case ArithmeticType.Add:
								return "+";
							case ArithmeticType.Divide:
								return "/";
							case ArithmeticType.Modulo:
								return "%";
							case ArithmeticType.Multiply:
								return "*";
							case ArithmeticType.Subtract:
								return "-";
						}
					}
					else if(value is SetType) {
						switch((SetType)value) {
							case SetType.Add:
								return "+=";
							case SetType.Change:
								return "=";
							case SetType.Divide:
								return "/=";
							case SetType.Modulo:
								return "%=";
							case SetType.Multiply:
								return "*=";
							case SetType.Subtract:
								return "-=";
						}
					}
					else {
						return value.ToString();
					}
				}

				return value.ToString();
			}
			return "null";
		}
		#endregion

		#region GetNicelyDisplayName
		public static string GetNicelyDisplayName(MultipurposeMember member, bool richName = false) {
			var editorColor = richTextColor();
			if(!editorColor.useRichText) {
				return GetFullDisplayName(member);
			}
			if(member.target == null || !member.target.isAssigned) {
				return "(none)".WrapWithColor(editorColor.otherColor);
			}
			if(member.target.isTargeted) {
				switch(member.target.targetType) {
					case MemberData.TargetType.None:
					case MemberData.TargetType.Self:
					case MemberData.TargetType.Null:
					case MemberData.TargetType.NodePort:
						return GetNicelyDisplayName(member.target, richName: richName);
					case MemberData.TargetType.Values:
						return GetNicelyDisplayName(member.target.Get(null), richName: richName);
				}
				string result = null;
				var mTarget = member.target;
				if(mTarget.Items.Length > 0) {
					if(mTarget.targetType == MemberData.TargetType.Constructor) {
						result += "new".WrapWithColor(editorColor.keywordColor) + " " + mTarget.type.PrettyName().WrapWithColor(editorColor.typeColor);
					}
					int skipIndex = 0;
					Color typeColor;
					if(mTarget.type != null) {
						typeColor = getColorForType(mTarget.type);
					}
					else {
						typeColor = editorColor.typeColor;
					}
					for(int i = 0; i < mTarget.Items.Length; i++) {
						if(!string.IsNullOrEmpty(result) && (mTarget.targetType != MemberData.TargetType.Constructor)) {
							result += ".";
						}
						if(mTarget.targetType != MemberData.TargetType.uNodeGenericParameter &&
							mTarget.targetType != MemberData.TargetType.Type &&
							mTarget.targetType != MemberData.TargetType.Constructor) {

							if(i == 0) {
								switch(mTarget.targetType) {
									case MemberData.TargetType.uNodeVariable:
									case MemberData.TargetType.uNodeLocalVariable:
									case MemberData.TargetType.uNodeProperty:
										result += ("$" + mTarget.Items[i].GetActualName()).WrapWithColor(getColorForType(mTarget.startType));
										break;
									case MemberData.TargetType.uNodeFunction:
									case MemberData.TargetType.uNodeGenericParameter:
									case MemberData.TargetType.uNodeParameter:
										result += mTarget.Items[i].GetActualName().WrapWithColor(getColorForType(mTarget.startType));
										break;
									default:
										if(preferredDisplay == DisplayKind.Partial && mTarget.Items.Length > 1 && !member.target.IsTargetingUNode) {
											if(member.target.isDeepTarget)
												continue;
											break;
										}
										if(mTarget.isStatic) {
											result += mTarget.Items[i].GetActualName().WrapWithColor(editorColor.typeColor);
										}
										else if(mTarget.Items.Length > 1) {
											result += mTarget.Items[i].GetActualName().WrapWithColor(getColorForType(mTarget.startType));
										}
										else {
											result += mTarget.Items[i].GetActualName().WrapWithColor(typeColor);
										}
										break;
								}
							}
							else if(i + 1 == mTarget.Items.Length) {
								result += mTarget.Items[i].GetActualName().WrapWithColor(typeColor);
							}
							else {
								result += mTarget.Items[i].GetActualName().WrapWithColor(editorColor.misleadingColor);
							}
						}
						MemberData.ItemData iData = mTarget.Items[i];
						if(iData != null) {
							string[] paramsType;
							string[] genericType;
							MemberDataUtility.GetItemName(mTarget.Items[i],
								out genericType,
								out paramsType);
							if(genericType.Length > 0) {
								for(int x = 0; x < genericType.Length; x++) {
									Type t = genericType[x].ToType(false);
									genericType[x] = t.PrettyName().WrapWithColor(getColorForType(t));
								}
								if(mTarget.targetType != MemberData.TargetType.uNodeGenericParameter &&
									mTarget.targetType != MemberData.TargetType.Type) {

									result += string.Format("<{0}>", string.Join(", ", genericType));
								}
								else {
									result += string.Format("{0}", string.Join(", ", genericType));
									if(mTarget.Items[i].GetActualName().Contains("[")) {
										bool valid = false;
										for(int x = 0; x < mTarget.Items[i].GetActualName().Length; x++) {
											if(!valid) {
												if(mTarget.Items[i].GetActualName()[x] == '[') {
													valid = true;
												}
											}
											if(valid) {
												result += mTarget.Items[i].GetActualName()[x];
											}
										}
									}
								}
							}
							if(member.hasRegister && paramsType.Length > 0) {
								if(member.parameters != null) {
									for(int x = 0; x < paramsType.Length; x++) {
										if(x + skipIndex >= member.parameters.Count) continue;
										if(member.parameters[x + skipIndex]?.input != null) {
											paramsType[x] = GetNicelyDisplayName(member.parameters[x + skipIndex].input, richName: richName);
										}
									}
								}
								result += string.Format("({0})", string.Join(", ", paramsType));
								skipIndex += paramsType.Length;
							}
						}
					}
				}
				if(!string.IsNullOrEmpty(result)) {
					return result;
				}
			}
			return GetNicelyDisplayName(member.target, DisplayKind.Full, true, richName: richName);
		}


		public static string GetNicelyDisplayName(this MemberData member, bool typeTargetWithTypeof = true, bool richName = true) {
			return GetNicelyDisplayName(member, preferredDisplay, typeTargetWithTypeof, richName);
		}

		public static string GetNicelyDisplayName(this MemberData member, DisplayKind displayKind, bool typeTargetWithTypeof = true, bool richName = true) {
			var editorColor = richTextColor();
			if(!richName || !editorColor.useRichText) {
				return GetDisplayName(member, displayKind, typeTargetWithTypeof);
			}
			if(member == null)
				return "(none)".WrapWithColor(editorColor.otherColor);
			switch(member.targetType) {
				case MemberData.TargetType.None:
					return "(none)".WrapWithColor(editorColor.otherColor);
				case MemberData.TargetType.Self:
					return "this".WrapWithColor(editorColor.keywordColor);
				case MemberData.TargetType.Null:
					return "null".WrapWithColor(editorColor.keywordColor);
				case MemberData.TargetType.NodePort:
					//if(member.GetTargetNode() == null) {
					//	goto case MemberData.TargetType.None;
					//}
					return "#Port".WrapWithColor(member.type != null ? getColorForType(member.type) : editorColor.otherColor);
				case MemberData.TargetType.Values:
					return GetNicelyDisplayName(member.Get(null), richName);
					//if(member.type != null) {
					//	return member.type.PrettyName().WrapWithColor(editorColor.typeColor);
					//}
					//return member.targetTypeName.WrapWithColor(editorColor.typeColor);
			}
			string result = null;
			if(member.isTargeted && member.Items.Length > 0) {
				if(member.targetType == MemberData.TargetType.Constructor) {
					result += "new".WrapWithColor(editorColor.keywordColor) + " " + member.type.PrettyName().WrapWithColor(editorColor.typeColor);
				}
				Color typeColor;
				if(member.type != null) {
					typeColor = getColorForType(member.type);
				}
				else {
					typeColor = editorColor.typeColor;
				}
				for(int i = 0; i < member.Items.Length; i++) {
					if(!string.IsNullOrEmpty(result) && (member.targetType != MemberData.TargetType.Constructor)) {
						result += ".";
					}
					if(member.targetType != MemberData.TargetType.uNodeGenericParameter &&
						member.targetType != MemberData.TargetType.Type &&
						member.targetType != MemberData.TargetType.Constructor) {
						if(i == 0) {
							switch(member.targetType) {
								case MemberData.TargetType.uNodeVariable:
								case MemberData.TargetType.uNodeLocalVariable:
								case MemberData.TargetType.uNodeProperty:
									result += ("$" + member.startName).WrapWithColor(getColorForType(member.startType));
									break;
								case MemberData.TargetType.uNodeFunction:
								case MemberData.TargetType.uNodeGenericParameter:
								case MemberData.TargetType.uNodeParameter:
									result += member.startName.WrapWithColor(getColorForType(member.startType));
									break;
								default:
									if(displayKind == DisplayKind.Partial && member.Items.Length > 1 && !member.IsTargetingUNode) {
										if(member.isDeepTarget)
											continue;
										break;
									}
									if(member.isStatic) {
										result += member.startName.WrapWithColor(editorColor.typeColor);
									}
									else if(member.Items.Length > 1) {
										result += member.startName.WrapWithColor(getColorForType(member.startType));
									}
									else {
										result += member.startName.WrapWithColor(typeColor);
									}
									break;
							}
						}
						else if(i + 1 == member.Items.Length) {
							result += member.Items[i].GetActualName().WrapWithColor(typeColor);
						}
						else {
							result += member.Items[i].GetActualName().WrapWithColor(editorColor.misleadingColor);
						}
					}
					MemberData.ItemData iData = member.Items[i];
					if(iData != null) {
						string[] paramsType;
						string[] genericType;
						MemberDataUtility.GetRichItemName(member.Items[i],
							out genericType,
							out paramsType);
						if(genericType.Length > 0) {
							if(member.targetType != MemberData.TargetType.uNodeGenericParameter && member.targetType != MemberData.TargetType.Type) {
								result += string.Format("<{0}>", string.Join(", ", genericType));
							}
							else {
								result += string.Format("{0}", string.Join(", ", genericType));
								if(member.Items[i].GetActualName().Contains("[")) {
									bool valid = false;
									for(int x = 0; x < member.Items[i].GetActualName().Length; x++) {
										if(!valid) {
											if(member.Items[i].GetActualName()[x] == '[') {
												valid = true;
											}
										}
										if(valid) {
											result += member.Items[i].GetActualName()[x];
										}
									}
								}
							}
						}
						if(displayKind == DisplayKind.Full) {
							if(paramsType.Length > 0 ||
								member.targetType == MemberData.TargetType.uNodeFunction ||
								member.targetType == MemberData.TargetType.uNodeConstructor ||
								member.targetType == MemberData.TargetType.Constructor ||
								member.targetType == MemberData.TargetType.Method && !member.isDeepTarget) {
								for(int x = 0; x < paramsType.Length; x++) {
									Type t = paramsType[x].ToType(false);
									if(t == null) {
										paramsType[x] = paramsType[x].WrapWithColor(getColorForType(null));
									}
									else {
										paramsType[x] = t.PrettyName().WrapWithColor(getColorForType(t));
									}
								}
								result += string.Format("({0})", string.Join(", ", paramsType));
							}
						}
					}
				}
			}
			if(!string.IsNullOrEmpty(result)) {
				if(member.targetType == MemberData.TargetType.Type) {
					if(!typeTargetWithTypeof) {
						return result;
					}
					return "typeof".WrapWithColor(editorColor.keywordColor) + "(" + result + ")";
				}
				return result;
			}
			switch(member.targetType) {
				case MemberData.TargetType.uNodeVariable:
				case MemberData.TargetType.uNodeLocalVariable:
				case MemberData.TargetType.uNodeProperty:
					return ("$" + member.name).WrapWithColor(getColorForType(member.type));
				case MemberData.TargetType.uNodeGenericParameter:
				case MemberData.TargetType.uNodeParameter:
					return member.name.WrapWithColor(getColorForType(member.type));
				case MemberData.TargetType.Type:
					if(!typeTargetWithTypeof) {
						return member.startType.PrettyName().WrapWithColor(editorColor.typeColor);
					}
					return "typeof".WrapWithColor(editorColor.keywordColor) + "(" + member.startType.PrettyName().WrapWithColor(editorColor.typeColor) + ")";
			}
			return !string.IsNullOrEmpty(member.name) ? member.name : "(none)".WrapWithColor(editorColor.otherColor);
		}

		public static string GetRichTypeName(Type type, bool withTypeof = true) {
			return GetRichTypeName(type.PrettyName(), withTypeof);
		}

		public static string GetRichTypeName(string type, bool withTypeof = true) {
			var editorColor = richTextColor();
			if(!editorColor.useRichText) {
				if(!withTypeof) {
					return type;
				}
				return "typeof(" + type + ")";
			}
			if(!withTypeof) {
				return type.WrapWithColor(editorColor.typeColor);
			}
			return "typeof".WrapWithColor(editorColor.keywordColor) + "(" + type.WrapWithColor(editorColor.typeColor) + ")";
		}

		public static string GetNicelyDisplayName(object value, bool richName = true) {
			var editorColor = richTextColor();
			if(!richName || !editorColor.useRichText) {
				return GetFullDisplayName(value);
			}
			if(!object.ReferenceEquals(value, null)) {
				if(value is MemberData) {
					MemberData member = value as MemberData;
					if(member.isTargeted) {
						switch(member.targetType) {
							case MemberData.TargetType.Values:
								return GetNicelyDisplayName(member.Get(null), richName: richName);
							default:
								return GetNicelyDisplayName(member, preferredDisplay, true, richName: richName);
						}
					}
					return "(none)".WrapWithColor(editorColor.otherColor);
				}
				else if(value is MultipurposeMember) {
					MultipurposeMember member = value as MultipurposeMember;
					return GetNicelyDisplayName(member, richName: richName);
				}
				else if(value is UnityEngine.Object) {
					if(!(value is Component)) {
						return (value as UnityEngine.Object).name;
					}
				}
				else if(value is UPort) {
					if(richName) {
						if(value is ValueInput) {
							var port = value as ValueInput;
							return port.GetRichName();
						}
					}
					return "#Port";
				}
				else if(value is string) {
					return ("\"" + value.ToString() + "\"").WrapWithColor(getColorForType(typeof(string)));
				}
				else if(value is Type) {//Type
					Type type = value as Type;
					if(type.IsInterface) {
						return type.PrettyName().WrapWithColor(editorColor.interfaceColor);
					}
					else if(type.IsEnum) {
						return type.PrettyName().WrapWithColor(editorColor.enumColor);
					}
					return type.PrettyName().WrapWithColor(editorColor.typeColor);
				}
				else if(value is SerializedType) {
					SerializedType type = value as SerializedType;
					return type.GetRichName();
				}
				else if(value is Vector2) {//Vector2
					Vector2 vec = (Vector2)value;
					if(vec == Vector2.down) {
						return "Vector2".WrapWithColor(editorColor.typeColor) + "." + "down".WrapWithColor(getColorForType(typeof(Vector2)));
					}
					else if(vec == Vector2.left) {
						return "Vector2".WrapWithColor(editorColor.typeColor) + "." + "left".WrapWithColor(getColorForType(typeof(Vector2)));
					}
					else if(vec == Vector2.one) {
						return "Vector2".WrapWithColor(editorColor.typeColor) + "." + "one".WrapWithColor(getColorForType(typeof(Vector2)));
					}
					else if(vec == Vector2.right) {
						return "Vector2".WrapWithColor(editorColor.typeColor) + "." + "right".WrapWithColor(getColorForType(typeof(Vector2)));
					}
					else if(vec == Vector2.up) {
						return "Vector2".WrapWithColor(editorColor.typeColor) + "." + "up".WrapWithColor(getColorForType(typeof(Vector2)));
					}
					else if(vec == Vector2.zero) {
						return "Vector2".WrapWithColor(editorColor.typeColor) + "." + "zero".WrapWithColor(getColorForType(typeof(Vector2)));
					}
					string result = "new".WrapWithColor(editorColor.keywordColor) + " Vector2".WrapWithColor(editorColor.typeColor);
					result += "(";
					result += vec.x.ToString().WrapWithColor(getColorForType(typeof(float))) + ", ";
					result += vec.y.ToString().WrapWithColor(getColorForType(typeof(float))) + ")";
					return result;
				}
				else if(value is Vector3) {//Vector3
					Vector3 vec = (Vector3)value;
					if(vec == Vector3.back) {
						return "Vector3".WrapWithColor(editorColor.typeColor) + "." + "back".WrapWithColor(getColorForType(typeof(Vector3)));
					}
					else if(vec == Vector3.down) {
						return "Vector3".WrapWithColor(editorColor.typeColor) + "." + "down".WrapWithColor(getColorForType(typeof(Vector3)));
					}
					else if(vec == Vector3.forward) {
						return "Vector3".WrapWithColor(editorColor.typeColor) + "." + "forward".WrapWithColor(getColorForType(typeof(Vector3)));
					}
					else if(vec == Vector3.left) {
						return "Vector3".WrapWithColor(editorColor.typeColor) + "." + "left".WrapWithColor(getColorForType(typeof(Vector3)));
					}
					else if(vec == Vector3.one) {
						return "Vector3".WrapWithColor(editorColor.typeColor) + "." + "one".WrapWithColor(getColorForType(typeof(Vector3)));
					}
					else if(vec == Vector3.right) {
						return "Vector3".WrapWithColor(editorColor.typeColor) + "." + "right".WrapWithColor(getColorForType(typeof(Vector3)));
					}
					else if(vec == Vector3.up) {
						return "Vector3".WrapWithColor(editorColor.typeColor) + "." + "up".WrapWithColor(getColorForType(typeof(Vector3)));
					}
					else if(vec == Vector3.zero) {
						return "Vector3".WrapWithColor(editorColor.typeColor) + "." + "zero".WrapWithColor(getColorForType(typeof(Vector3)));
					}
					string result = "new".WrapWithColor(editorColor.keywordColor) + " Vector3".WrapWithColor(editorColor.typeColor);
					result += "(";
					result += vec.x.ToString().WrapWithColor(getColorForType(typeof(float))) + ", ";
					result += vec.y.ToString().WrapWithColor(getColorForType(typeof(float))) + ", ";
					result += vec.z.ToString().WrapWithColor(getColorForType(typeof(float))) + ")";
					return result;
				}
				else if(value is Color) {//Color
					Color color = (Color)value;
					if(color == Color.black) {
						return "Color".WrapWithColor(editorColor.typeColor) + "." + "black".WrapWithColor(getColorForType(typeof(Color)));
					}
					else if(color == Color.blue) {
						return "Color".WrapWithColor(editorColor.typeColor) + "." + "blue".WrapWithColor(getColorForType(typeof(Color)));
					}
					else if(color == Color.clear) {
						return "Color".WrapWithColor(editorColor.typeColor) + "." + "clear".WrapWithColor(getColorForType(typeof(Color)));
					}
					else if(color == Color.cyan) {
						return "Color".WrapWithColor(editorColor.typeColor) + "." + "cyan".WrapWithColor(getColorForType(typeof(Color)));
					}
					else if(color == Color.gray) {
						return "Color".WrapWithColor(editorColor.typeColor) + "." + "gray".WrapWithColor(getColorForType(typeof(Color)));
					}
					else if(color == Color.green) {
						return "Color".WrapWithColor(editorColor.typeColor) + "." + "green".WrapWithColor(getColorForType(typeof(Color)));
					}
					else if(color == Color.grey) {
						return "Color".WrapWithColor(editorColor.typeColor) + "." + "grey".WrapWithColor(getColorForType(typeof(Color)));
					}
					else if(color == Color.magenta) {
						return "Color".WrapWithColor(editorColor.typeColor) + "." + "magenta".WrapWithColor(getColorForType(typeof(Color)));
					}
					else if(color == Color.red) {
						return "Color".WrapWithColor(editorColor.typeColor) + "." + "red".WrapWithColor(getColorForType(typeof(Color)));
					}
					else if(color == Color.white) {
						return "Color".WrapWithColor(editorColor.typeColor) + "." + "white".WrapWithColor(getColorForType(typeof(Color)));
					}
					else if(color == Color.yellow) {
						return "Color".WrapWithColor(editorColor.typeColor) + "." + "yellow".WrapWithColor(getColorForType(typeof(Color)));
					}
					string result = "new".WrapWithColor(editorColor.keywordColor) + " Color".WrapWithColor(editorColor.typeColor);
					result += "(";
					result += color.r.ToString("0.##").WrapWithColor(getColorForType(typeof(float))) + ", ";
					result += color.g.ToString("0.##").WrapWithColor(getColorForType(typeof(float))) + ", ";
					result += color.b.ToString("0.##").WrapWithColor(getColorForType(typeof(float))) + ", ";
					result += color.a.ToString("0.##").WrapWithColor(getColorForType(typeof(float))) + ")";
					return result;
				}
				else if(value is Enum) {//Enum
					if(value is ComparisonType || value is ArithmeticType || value is SetType) {
						return GetDisplayName(value);
					}
					else {
						return value.ToString().WrapWithColor(editorColor.enumColor);
					}
				}
				else if(value is MethodInfo) {
					return GetNicelyMethodName(value as MethodInfo);
				}
				if(value.GetType().IsPrimitive) {
					return value.ToString().WrapWithColor(getColorForType(value.GetType()));
				}
				return value.GetType().PrettyName().WrapWithColor(getColorForType(value.GetType()));
			}
			return "null".WrapWithColor(editorColor.keywordColor);
		}

		public static string GetNicelyMethodName(this MethodInfo method, bool includeReturnType = true) {
			ParameterInfo[] info = method.GetParameters();
			string mConstructur = null;
			if(method.IsGenericMethod) {
				foreach(Type arg in method.GetGenericArguments()) {
					if(string.IsNullOrEmpty(mConstructur)) {
						mConstructur += "<" + GetNicelyDisplayName(arg);
						continue;
					}
					mConstructur += "," + GetNicelyDisplayName(arg);
				}
				mConstructur += ">";
			}
			mConstructur += "(";
			for(int i = 0; i < info.Length; i++) {
				mConstructur += GetNicelyDisplayName(info[i].ParameterType) + " " + info[i].Name;
				if(i + 1 < info.Length) {
					mConstructur += ", ";
				}
			}
			mConstructur += ")";
			if(includeReturnType) {
				return GetNicelyDisplayName(method.ReturnType) + " " + method.Name + mConstructur;
			}
			else {
				return method.Name + mConstructur;
			}
		}
		#endregion

		#region Classes
		public class ErrorMessage {
			public string message;
			public Action<Vector2> autoFix;
			public InfoType type = InfoType.Error;

			public string niceMessage => message;
		}

		public class GraphErrorData {
			public UGraphElementRef element;

			private List<ErrorMessage> errors = new List<ErrorMessage>();

			public int GetErrorCount() {
				return errors.Count;
			}

			public IEnumerable<ErrorMessage> GetErrors() {
				return errors;
			}

			public IEnumerable<ErrorMessage> GetErrors(InfoType type) {
				return errors.Where(item => item.type == type);
			}

			public int GetErrorCount(InfoType type) {
				return errors.Count(item => item.type == type);
			}

			public bool HasError() => errors.Count > 0;

			public bool HasError(InfoType type) {
				return errors.Any(item => item.type == type);
			}

			public void AddError(ErrorMessage error) {
				errors.Add(error);
			}

			public void ClearErrors() {
				errors.Clear();
			}
		}
		#endregion

		#region ListUtility
		//public static void AddList(ref IList list, object value) {
		//	if(list is Array) {
		//		Array arr = list as Array;
		//		AddArray(ref arr, value);
		//		list = arr;
		//	} else {
		//		list.Add(value);
		//	}
		//}

		//public static void RemoveListAt(ref IList list, int index) {
		//	if(list is Array) {
		//		Array arr = list as Array;
		//		RemoveArrayAt(ref arr, index);
		//		list = arr;
		//	} else {
		//		list.RemoveAt(index);
		//	}
		//}

		public static void ResizeList<T>(List<T> list, int newSize, bool copyValue = false) {
			ResizeList(list, typeof(T), newSize);
		}

		public static void ResizeList(IList list, Type elementType, int newSize, bool copyValue = false) {
			if(newSize < 0) {
				newSize = 0;
			}
			if(list == null || newSize == list.Count) {
				return;
			}
			while(newSize > list.Count) {
				if(list.Count == 0) {
					list.Add(ReflectionUtils.CreateInstance(elementType));
				}
				object newObj = list[list.Count - 1];
				if(newObj != null) {
					if(copyValue) {
						if(!newObj.GetType().IsValueType && !(newObj.GetType() == typeof(UnityEngine.Object) || newObj.GetType().IsSubclassOf(typeof(UnityEngine.Object)))) {
							newObj = SerializerUtility.Duplicate(newObj);
						}
					}
					else {
						newObj = ReflectionUtils.CreateInstance(elementType);
					}
				}
				list.Add(newObj);
			}
			while(newSize < list.Count) {
				if(list.Count == 0) {
					break;
				}
				list.RemoveAt(list.Count - 1);
			}
		}

		public static void ReorderList(IList list, int oldIndex, int newIndex) {
			if(oldIndex == newIndex)
				return;
			var val = list[oldIndex];
			if(oldIndex < newIndex) {
				for(int i = oldIndex; i < newIndex; i++) {
					list[i] = list[i + 1];
				}
				list[newIndex] = val;
			}
			else {
				for(int i = oldIndex; i > newIndex; i--) {
					list[i] = list[i - 1];
				}
				list[newIndex] = val;
			}
		}


		public static void InsertList<T>(ref IList<T> list, int index, T value) {
			if(list is Array) {
				var obj = new List<T>(list);
				obj.Insert(index, value);
				list = obj.ToArray();
			}
			else {
				list.Insert(index, value);
			}
		}

		public static void AddList<T>(ref IList<T> list, T value) {
			if(list is Array) {
				var obj = new List<T>(list);
				obj.Add(value);
				list = obj.ToArray();
			}
			else {
				list.Add(value);
			}
		}

		public static void RemoveList<T>(ref IList<T> list, T value) {
			if(list is Array) {
				var obj = new List<T>(list);
				obj.Remove(value);
				list = obj.ToArray();
			}
			else {
				list.Remove(value);
			}
		}

		public static void RemoveListAt<T>(ref IList<T> list, int index) {
			if(list is Array) {
				var obj = new List<T>(list);
				obj.RemoveAt(index);
				list = obj.ToArray();
			}
			else {
				list.RemoveAt(index);
			}
		}
		#endregion

		#region ArrayUtility
		public static Array ResizeArray(Array array, Type elementType, int newSize) {
			if(newSize < 0) {
				newSize = 0;
			}
			if(array == null || newSize == array.Length) {
				return array;
			}
			Array array2 = Array.CreateInstance(elementType, newSize);
			int num = Math.Min(newSize, array.Length);
			for(int i = 0; i < num; i++) {
				array2.SetValue(array.GetValue(i), i);
			}
			return array2;
		}

		public static void DuplicateArrayAt(ref Array array, int index) {
			Array array2 = Array.CreateInstance(array.GetType().GetElementType(), array.Length + 1);
			int skipped = 0;
			for(int i = 0; i < array2.Length; i++) {
				array2.SetValue(array.GetValue(i - skipped), i);
				if(index == i) {
					object obj = array.GetValue(i);
					if(obj != null) {
						if(!obj.GetType().IsValueType && !(obj.GetType() == typeof(UnityEngine.Object) || obj.GetType().IsSubclassOf(typeof(UnityEngine.Object)))) {
							obj = SerializerUtility.Duplicate(obj);
						}
					}
					array2.SetValue(obj, i + 1);
					skipped++;
					i += 1;
				}
			}
			array = array2;
		}

		public static T[] CreateArrayFrom<T>(IList<T> from) {
			if(from == null)
				return default;
			T[] value = new T[from.Count];
			for(int i = 0; i < from.Count; i++) {
				value[i] = from[i];
			}
			return value;
		}

		public static void AddArray(ref Array array, object value) {
			Array array2 = Array.CreateInstance(array.GetType().GetElementType(), array.Length + 1);
			for(int i = 0; i < array.Length; i++) {
				array2.SetValue(array.GetValue(i), i);
			}
			array2.SetValue(value, array.Length);
			array = array2;
		}

		public static void AddArray<T>(ref T[] array, T value) {
			Array array2 = Array.CreateInstance(typeof(T), array.Length + 1);
			for(int i = 0; i < array.Length; i++) {
				array2.SetValue(array.GetValue(i), i);
			}
			array2.SetValue(value, array.Length);
			array = array2 as T[];
		}

		public static void AddArrayAt<T>(ref T[] array, T value, int index) {
			T[] array2 = new T[array.Length + 1];
			for(int i = 0; i < array2.Length; i++) {
				if(i == index) {
					array2[i] = value;
					continue;
				}
				array2[i] = array[i > index ? i - 1 : i];
			}
			array = array2;
		}

		public static void RemoveArray(ref Array array, object value) {
			for(int i = 0; i < array.Length; i++) {
				if(array.GetValue(i) == value) {
					RemoveArrayAt(ref array, i);
					break;
				}
			}
		}

		public static void RemoveArray<T>(ref T[] array, T value) {
			for(int i = 0; i < array.Length; i++) {
				if(object.Equals(array[i], value)) {
					RemoveArrayAt(ref array, i);
					break;
				}
			}
		}

		public static void RemoveArrayAt(ref Array array, int index) {
			Array array2 = Array.CreateInstance(array.GetType().GetElementType(), array.Length - 1);
			int skipped = 0;
			for(int i = 0; i < array.Length; i++) {
				if(index == i) { skipped++; continue; }
				array2.SetValue(array.GetValue(i), i - skipped);
			}
			array = array2;
		}

		public static void RemoveArrayAt<T>(ref T[] array, int index) {
			Array array2 = Array.CreateInstance(typeof(T), array.Length - 1);
			int skipped = 0;
			for(int i = 0; i < array.Length; i++) {
				if(index == i) { skipped++; continue; }
				array2.SetValue(array.GetValue(i), i - skipped);
			}
			array = array2 as T[];
		}
		#endregion

		#region Graph Utility
		/// <summary>
		/// True if <paramref name="source"/> can be cast to <paramref name="other"/>
		/// </summary>
		/// <param name="source"></param>
		/// <param name="other"></param>
		/// <returns></returns>
		public static bool IsCastableGraph(IGraph source, IGraph other) {
			return IsAssignableGraph(other, source);
		}

		internal static IEnumerable<IGraph> GetSubGraphs(IGraph graph) {
			var db = GetDatabase();
			if(db != null && graph is IReflectionType reflectionType) {
				var type = reflectionType.ReflectionType;
				foreach(var data in db.graphDatabases) {
					if(data.asset is IReflectionType rType && rType.ReflectionType.IsSubclassOf(type)) {
						yield return data.asset;
					}
				}
			}
			yield break;
		}

		/// <summary>
		/// True if the <paramref name="other"/> is equal, or subclass of <paramref name="source"/> or <paramref name="other"/> is implementing interface of <paramref name="source"/>
		/// </summary>
		/// <param name="source"></param>
		/// <param name="other"></param>
		/// <returns></returns>
		public static bool IsAssignableGraph(IGraph source, IGraph other) {
			if(source == other)
				return true;
			if(source is IReflectionType sourceType && other is IReflectionType destinationType) {
				return sourceType.ReflectionType.IsAssignableFrom(destinationType.ReflectionType);
			}
			return false;
		}
		#endregion

		public static T CloneObject<T>(T value) {
			return SerializerUtility.Duplicate(value);
		}

		public static bool IsValidAsyncType(Type type) {
			if(type == typeof(void))
				return true;
			if(typeof(System.Threading.Tasks.Task).IsAssignableFrom(type)) {
				return true;
			}
			var attributes = type.GetCustomAttributes(true);
			if(attributes.Length > 0) {
				foreach(var att in attributes) {
					//We manually compare it's full type name in case you're using UniTask.
					if(att.GetType().FullName == "System.Runtime.CompilerServices.AsyncMethodBuilderAttribute") {
						return true;
					}
				}
			}
			return false;
		}

		public static Type GetAsyncReturnType(Type type) {
			if(type == typeof(void))
				return typeof(void);
			if(type == typeof(System.Threading.Tasks.Task)) {
				return typeof(void);
			}
			var awaiterMethod = type.GetMemberCached(nameof(System.Threading.Tasks.Task.GetAwaiter));
			if(awaiterMethod != null && awaiterMethod is MethodInfo methodInfo) {
				var returnType = methodInfo.ReturnType;
				if(returnType.HasImplementInterface(typeof(System.Runtime.CompilerServices.INotifyCompletion))) {
					var resultMethod = returnType.GetMemberCached("GetResult") as MethodInfo;
					if(resultMethod != null) {
						return resultMethod.ReturnType;
					}
				}
			}
			return null;
		}

		static System.Threading.Thread mainThread;
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
		internal static void Init() {
			mainThread = System.Threading.Thread.CurrentThread;
		}

		public static bool IsInMainThread => System.Threading.Thread.CurrentThread == mainThread;
	}

	[Serializable]
	public class EditorTextSetting {
		public bool useRichText = true;
		public Color typeColor = new Color(0, 0.8f, 0.37f);
		public Color keywordColor = new Color(0, 0.46f, 0.83f);
		public Color interfaceColor = new Color(0.8f, 0.77f, 0);
		public Color enumColor = new Color(0.8f, 0.8f, 0);
		public Color otherColor = new Color(0.95f, 0.33f, 0.32f);
		public Color misleadingColor = new Color(0.85f, 0.85f, 0.85f);
		public Color summaryColor = new Color(0, 0.7f, 0);
	}

	/// <summary>
	/// Provides useful function for string manipulation.
	/// </summary>
	public static class StringHelper {
		static readonly IDictionary<string, string> m_replaceDict = new Dictionary<string, string>();
		static readonly char[] m_escapeCodes = new[] {
			'\a',
			'\b',
			'\f',
			'\n',
			'\r',
			'\t',
			'\v',

			'\\',
			'\0',
			'\"',

		};

		const string ms_regexEscapes = @"[\a\b\f\n\r\t\v\\""]";
		static readonly Regex m_regex = new Regex(ms_regexEscapes, RegexOptions.Compiled);

		public static string StringLiteral(string input) {
			return m_regex.Replace(input, match);
		}

		public static string StringLiteralCode(string input) {
			for(int i=0;i< m_escapeCodes.Length;i++) {
				if(input.Contains(m_escapeCodes[i])) {
					return $"@\"{input}\"";
				}
			}
			return $"\"{input}\"";
		}

		public static string CharLiteral(char c) {
			return c == '\'' ? @"'\''" : string.Format("'{0}'", c);
		}

		private static string match(Match m) {
			string match = m.ToString();
			if(m_replaceDict.ContainsKey(match)) {
				return m_replaceDict[match];
			}

			throw new NotSupportedException();
		}

		static StringHelper() {
			m_replaceDict.Add("\a", @"\a");
			m_replaceDict.Add("\b", @"\b");
			m_replaceDict.Add("\f", @"\f");
			m_replaceDict.Add("\n", @"\n");
			m_replaceDict.Add("\r", @"\r");
			m_replaceDict.Add("\t", @"\t");
			m_replaceDict.Add("\v", @"\v");

			m_replaceDict.Add("\\", @"\\");
			m_replaceDict.Add("\0", @"\0");

			m_replaceDict.Add("\"", "\\\"");
		}
	}
}