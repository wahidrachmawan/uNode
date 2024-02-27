using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MaxyGames.UNode.Editors {
	#region Enums
	[Flags]
	public enum CompletionKind {
		Undefined = -10,
		None = 0,
		Namespace = 1 << 0,
		Keyword = 1 << 1,
		Type = 1 << 2,
		Field = 1 << 3,
		Property = 1 << 4,
		Method = 1 << 5,
		uNodeVariable = 1 << 6,
		uNodeProperty = 1 << 7,
		uNodeFunction = 1 << 8,
		uNodeLocalVariable = 1 << 9,
		uNodeParameter = 1 << 10,
		Constructor = 1 << 11,
		Literal = 1 << 12,
		Symbol = 1 << 13,
	}

	public enum KeywordKind {
		None,
		As,
		Bool,
		Break,
		Byte,
		Catch,
		Char,
		Class,
		Const,
		Continue,
		Decimal,
		Default,
		Delegate,
		Do,
		Double,
		Else,
		Enum,
		Event,
		False,
		Float,
		For,
		Foreach,
		Goto,
		If,
		In,
		Int,
		Interface,
		Is,
		Lock,
		Long,
		Namespace,
		New,
		Null,
		Object,
		Ref,
		Return,
		Sbyte,
		Short,
		Sizeof,
		Stackalloc,
		Static,
		String,
		Struct,
		Switch,
		This,
		Throw,
		True,
		Try,
		Typeof,
		Uint,
		Ulong,
		Ushort,
		Using,
		Void,
		While,
	}
	#endregion

	#region Classes
	public class CompletionInfo {
		public string name;
		public MemberInfo member;
		public CompletionKind kind;
		public KeywordKind keywordKind;
		public bool isResolved;

		public bool isSymbol {
			get {
				return kind.HasFlags(CompletionKind.Symbol);
			}
		}
		public bool isKeyword {
			get {
				return kind.HasFlags(CompletionKind.Keyword);
			}
		}
		public bool isDot {
			get {
				return name == ".";
			}
		}

		//uNode Data Infos
		public object targetObject;
		public UnityEngine.Object targetOwner;

		public IList<CompletionInfo> parameterCompletions;
		public IList<CompletionInfo> genericCompletions;

		public Type GetMemberType() {
			return ReflectionUtils.GetMemberType(member);
		}

		public object GetLiteralValue() {
			var targetType = GetMemberType();
			if(targetType == typeof(string)) {
				return name.Remove(0, 1).RemoveLast();
			} else if(targetType == typeof(int)) {
				string s = name;
				if(!char.IsNumber(s[s.Length - 1])) {
					s = s.RemoveLast();
				}
				return int.Parse(s);
			} else if(targetType == typeof(float)) {
				string s = name;
				if(!char.IsNumber(s[s.Length - 1])) {
					s = s.RemoveLast();
				}
				return float.Parse(s);
			} else if(targetType == typeof(double)) {
				string s = name;
				if(!char.IsNumber(s[s.Length - 1])) {
					s = s.RemoveLast();
				}
				return double.Parse(s);
			} else if(targetType == typeof(decimal)) {
				string s = name;
				if(!char.IsNumber(s[s.Length - 1])) {
					s = s.RemoveLast();
				}
				return decimal.Parse(s);
			} else if(targetType == typeof(uint)) {
				string s = name;
				if(!char.IsNumber(s[s.Length - 1])) {
					s = s.RemoveLast();
				}
				return uint.Parse(s);
			} else if(targetType == typeof(long)) {
				string s = name;
				if(!char.IsNumber(s[s.Length - 1])) {
					s = s.RemoveLast();
				}
				return long.Parse(s);
			}
			return null;
		}
	}
	#endregion

	public class CompletionEvaluator {
		#region Classes
		public class CompletionSetting {
			public object owner;

			public Object UnityObject {
				get {
					if(owner is Object) {
						return owner as Object;
					}
					else if(owner is UGraphElement element) {
						return element.graphContainer as Object;
					}
					return null;
				}
			}
			public IEnumerable<string> namespaces;
			public CompletionKind validCompletionKind = CompletionKind.None;
			public bool allowExpression;
			public bool allowStatement;
			public bool allowLiteral = true;
			public bool allowSymbolKeyword;
			public bool allowValueKeyword = true;
			public bool allowTypeKeyword = true;
		}
		private class Data {
			public object owner;

			public Object UnityObject {
				get {
					if(owner is Object) {
						return owner as Object;
					}
					else if(owner is UGraphElement element) {
						return element.graphContainer as Object;
					}
					return null;
				}
			}

			public IEnumerable<Variable> variables {
				get {
					var graph = UnityObject as IGraph;
					if(graph != null) {
						return graph.GetAllVariables();
					}
					return Array.Empty<Variable>();
				}
			}

			public IEnumerable<Property> properties {
				get {
					var graph = UnityObject as IGraph;
					if(graph != null) {
						return graph.GetProperties();
					}
					return Array.Empty<Property>();
				}
			}

			public IEnumerable<Function> functions {
				get {
					var graph = UnityObject as IGraph;
					if(graph != null) {
						return graph.GetFunctions();
					}
					return Array.Empty<Function>();
				}
			}
		}
		#endregion

		public delegate NodeObject CompletionToNodeDelegate(CompletionInfo completion, GraphEditorData editorData, Vector2 graphPosition);

		public static CompletionToNodeDelegate completionToNode;

		/// <summary>
		/// The completions data.
		/// </summary>
		public CompletionInfo[] completions;
		/// <summary>
		/// The member path of completions.
		/// </summary>
		public List<CompletionInfo> memberPaths;

		private Data data;
		private List<Type> types;
		private Dictionary<string, List<Type>> typeMap;
		private int handleCount;
		private int evalCount;

		private CompletionSetting setting;

		private static HashSet<string> symbolKeywords = new HashSet<string>() {
			"as",
			"break",
			//"case",
			"catch",
			"continue",
			"default",
			"delegate",
			"do",
			//"else",
			//"event",
			"for",
			"foreach",
			//"goto",
			"if",
			"is",
			"lock",
			"return",
			//"sizeof",
			//"stackalloc",
			"switch",
			"this",
			"throw",
			"try",
			"typeof",
			"using",
			"while",
		};
		private static HashSet<string> valueKeywords = new HashSet<string>() {
			"false",
			"true",
			"null",
			//"new"
		};
		private static Dictionary<string, Type> typeKeywords = new Dictionary<string, Type>() {
			{"bool", typeof(bool) },
			{"byte", typeof(byte) },
			{"char", typeof(char) },
			{"decimal", typeof(decimal) },
			{"double", typeof(double) },
			{"float", typeof(float) },
			{"int", typeof(int) },
			{"long", typeof(long) },
			{"object", typeof(object) },
			{"sbyte", typeof(sbyte) },
			{"short", typeof(short) },
			{"string", typeof(string) },
			{"uint", typeof(uint) },
			{"ulong", typeof(ulong) },
			{"ushort", typeof(ushort) },
			{"void", typeof(void) },
		};
		public static readonly HashSet<string> symbolMap = new HashSet<string>() {
			"+", "-", "*", "/", "=", "%", "^",
			",", "<", ">", "&", "|", "!",
			"(", ")", "{", "}", "[", "]",
			"\'", "~"
		};
		public static readonly HashSet<string> operatorSymbolMap = new HashSet<string>() {
			"+", "-", "*", "/", "=", "%", "^", "<", ">", "&", "|", "!",
		};
		public static readonly HashSet<string> expressionMap = new HashSet<string>() {

		};

		#region Constructors
		/// <summary>
		/// Creates new evaluator.
		/// </summary>
		public CompletionEvaluator() {
			new Thread(InitializeEvaluator).Start();
			setting = new CompletionSetting();
		}

		/// <summary>
		/// Creates new evaluator.
		/// </summary>
		/// <param name="unityObject"></param>
		public CompletionEvaluator(UnityEngine.Object unityObject, CompletionSetting completionSetting) {
			if(unityObject != null) {
				data = new Data();
				data.owner = unityObject;
			}
			setting = completionSetting;
			new Thread(InitializeEvaluator).Start();
		}

		/// <summary>
		/// Creates new evaluator.
		/// </summary>
		/// <param name="setting"></param>
		public CompletionEvaluator(CompletionSetting setting) {
			if(setting.owner != null) {
				data = new Data();
				data.owner = setting.owner;
			}
			this.setting = setting;
			new Thread(InitializeEvaluator).Start();
		}
		#endregion

		private void InitializeEvaluator() {
			if(setting.validCompletionKind == CompletionKind.None) {
				setting.validCompletionKind = CompletionKind.Constructor |
					CompletionKind.Field |
					CompletionKind.Keyword |
					CompletionKind.Method |
					CompletionKind.Namespace |
					CompletionKind.Property |
					CompletionKind.Type |
					CompletionKind.uNodeFunction |
					CompletionKind.uNodeLocalVariable |
					CompletionKind.uNodeParameter |
					CompletionKind.uNodeProperty |
					CompletionKind.uNodeVariable |
					CompletionKind.Literal;
			}
			HashSet<Type> typeHash = new HashSet<Type>();
			Dictionary<string, List<Type>> map = new Dictionary<string, List<Type>>();
			HashSet<string> NS;
			if(setting.namespaces != null) {
				NS = new HashSet<string>();
				foreach(var ns in setting.namespaces) {
					if(!NS.Contains(ns)) {
						NS.Add(ns);
					}
				}
			} else {
				NS = new HashSet<string>() {
					"UnityEngine",
				};
			}
			var assemblies = new List<Assembly>(EditorReflectionUtility.GetAssemblies());
			foreach(var assembly in assemblies) {
				var types = new List<Type>(EditorReflectionUtility.GetAssemblyTypes(assembly));
				foreach(var type in types) {
					if(!type.IsVisible)
						continue;
					string ns = type.Namespace;
					if(ns == null) {
						ns = "";
					}
					if(ns == "" || NS.Contains(ns)) {
						if(!typeHash.Contains(type)) {
							typeHash.Add(type);
						}
					}
					if(ns != "") {
						List<Type> mapList;
						if(map.TryGetValue(ns, out mapList)) {
							mapList.Add(type);
						} else {
							mapList = new List<Type>();
							mapList.Add(type);
							map.Add(ns, mapList);
						}
					}
				}
			}
			this.types = typeHash.ToList();
			typeMap = map;
		}

		public List<string> SplitToCompletionPath(string input) {
			List<string> result = new List<string>() { "" };
			char lastChar = ' ';
			bool insideString = false;
			for(int i = 0; i < input.Length; i++) {
				var c = input[i];
				if(c == '\"') {
					if(!insideString) {
						if(!string.IsNullOrEmpty(result[result.Count - 1])) {
							result.Add("");
						}
					}
					insideString = !insideString;
					result[result.Count - 1] += c;
					if(!insideString && i + 1 < input.Length) {
						result.Add("");
					}
					continue;
				}
				if(insideString) {
					result[result.Count - 1] += c;
				} else if(char.IsWhiteSpace(c)) {
					if(!char.IsWhiteSpace(lastChar)) {
						if(!string.IsNullOrEmpty(result[result.Count - 1])) {
							result.Add("");
						}
					}
					lastChar = c;
					result[result.Count - 1] += c;
				} else if(symbolMap.Contains(c.ToString()) || c == '.') {
					if(!string.IsNullOrEmpty(result[result.Count - 1])) {
						result.Add("");
					}
					//if(char.IsWhiteSpace(lastChar) || !symbolMap.Contains(lastChar)) {
					//	if(!string.IsNullOrEmpty(result[result.Count - 1])) {
					//		result.Add("");
					//	}
					//}
					lastChar = c;
					result[result.Count - 1] += c;
				} else {
					if(char.IsWhiteSpace(lastChar) || symbolMap.Contains(lastChar.ToString()) || lastChar == '.') {
						if(!string.IsNullOrEmpty(result[result.Count - 1])) {
							result.Add("");
						}
					}
					lastChar = c;
					result[result.Count - 1] += c;
				}
			}
			return result;
		}

		private string JoinMember(List<CompletionInfo> members) {
			string result = "";
			for(int i = members.Count - 1; i >= 0; i--) {
				if(members[i].isSymbol) {
					if(members[i].name == "." || string.IsNullOrEmpty(result))
						continue;
					break;
				}
				if(!string.IsNullOrEmpty(result)) {
					result = result.Insert(0, ".");
				}
				result = result.Insert(0, members[i].name);
			}
			return result;
		}

		private CompletionInfo[] GetCompletions(string input) {
			List<CompletionInfo> memberPath;
			return GetCompletions(input, out memberPath);
		}

		private CompletionInfo[] GetCompletions(string input, out List<CompletionInfo> memberPath) {
			if(types != null && typeMap != null) {
				var path = SplitToCompletionPath(input);
				//path.RemoveAll(p => string.IsNullOrEmpty(p.Trim()));
				List<CompletionInfo> completions = new List<CompletionInfo>();
				HashSet<string> completionNames = new HashSet<string>();
				List<CompletionInfo> members = new List<CompletionInfo>();
				List<CompletionInfo> deepMembers = new List<CompletionInfo>();
				List<CompletionInfo> memberAccess = new List<CompletionInfo>();
				CompletionInfo prev2Member = null;
				CompletionInfo prevMember = null;
				ResolveData resolveData = new ResolveData() {
					members = members,
					completions = completions,
					completionNames = completionNames,
				};
				for(int i = 0; i < path.Count; i++) {
					prev2Member = prevMember;
					string p = path[i];
					string pLower = p.ToLower();
					bool isDot = p == ".";
					bool isLast = i + 1 == path.Count;
					bool isSymbol = symbolMap.Contains(p);
					prevMember = members.LastOrDefault();
					if(memberAccess.Count > 0) {
						prevMember = memberAccess.LastOrDefault();
						memberAccess.RemoveAt(memberAccess.Count - 1);
					}
					{
						resolveData.text = p;
						resolveData.textLower = pLower;
						resolveData.isLast = isLast;
					}
					if((prevMember == null || !isDot && !prevMember.isDot) &&
						(prev2Member == null || prev2Member.isSymbol) || isSymbol || prevMember != null && prevMember.isSymbol) {
						#region Literal
						if(setting.allowLiteral && setting.validCompletionKind.HasFlags(CompletionKind.Literal) && pLower.Length > 0) {//Literal
							if(ResolveLiteral(resolveData)) {
								continue;
							}
						}
						#endregion
						
						#region Keywords
						if(ResolveKeywords(resolveData)) {
							continue;
						}
						#endregion

						#region uNodes
						if(setting.owner != null) {
							if(setting.validCompletionKind.HasFlags(CompletionKind.uNodeLocalVariable) && 
								setting.owner is ILocalVariableSystem LVS) {
								if(ResolveLocalVariable(LVS, resolveData, setting.UnityObject)) {
									continue;
								}
							}
							if(setting.validCompletionKind.HasFlags(CompletionKind.uNodeParameter) && 
								setting.owner is IParameterSystem PS) {
								if(ResolveParameter(PS, resolveData, setting.UnityObject)) {
									continue;
								}
							}
						}
						if(data != null) {
							//Variable
							if(setting.validCompletionKind.HasFlags(CompletionKind.uNodeVariable)) {
								if(ResolveVariable(resolveData, data.variables, data.UnityObject)) {
									continue;
								}
							}
							//Property
							if(setting.validCompletionKind.HasFlags(CompletionKind.uNodeProperty)) {
								if(ResolveProperty(resolveData, data.properties, data.UnityObject)) {
									continue;
								}
							}
							//Function
							if(setting.validCompletionKind.HasFlags(CompletionKind.uNodeFunction)) {
								if(ResolveFunction(resolveData, data.functions, data.UnityObject)) {
									continue;
								}
							}
						}
						#endregion

						#region Types
						if(setting.validCompletionKind.HasFlags(CompletionKind.Type)) {
							if(ResolveType(resolveData, types)) {
								continue;
							}
						}
						#endregion

						#region Namespaces
						if(setting.validCompletionKind.HasFlags(CompletionKind.Namespace)) {
							if(ResolveNamespace(resolveData, typeMap)) {
								continue;
							}
						}
						#endregion

						#region Symbols
						if(isSymbol) {
							members.Add(new CompletionInfo() {
								name = p,
								kind = CompletionKind.Symbol,
								targetObject = data.owner,
								targetOwner = data.UnityObject,
							});
							if(isLast) {
								if(!completionNames.Contains(p)) {
									//string nm = p;
									//for(int x = i - 1; x >= 0; x--) {
									//	if(symbolMap.Contains(path[x])) {
									//		nm = nm.Insert(0, path[x]);
									//	} else {
									//		break;
									//	}
									//}
									completions.Add(new CompletionInfo() {
										name = p,
										kind = CompletionKind.Symbol,
										targetObject = data.owner,
										targetOwner = data.UnityObject,
									});
									completionNames.Add(p);
								}
							}
							switch(p[0]) {
								case '(': {
									//MemberInfo[] memberInfos = GetCompletionMembers(prevMember);
									//if(memberInfos != null) {
									//	var methods = memberInfos.Where(m =>
									//		m.MemberType == MemberTypes.Method &&
									//		m.Name.Equals(prevMember.name)).ToList();
									//	if(methods != null) {
									//		foreach(var m in methods) {
									//			var method = m as MethodInfo;
									//			if(method != null) {

									//			}
									//		}
									//	}
									//}
									deepMembers.Add(prevMember);
									resolveData.isStatic = true;
									break;
								}
								case '<':
									deepMembers.Add(prevMember);
									resolveData.isStatic = true;
									break;
								case '[':
									deepMembers.Add(prevMember);
									resolveData.isStatic = true;
									break;
								case ')':
									if(deepMembers.Count > 0) {
										memberAccess.Add(deepMembers.LastOrDefault());
										deepMembers.RemoveAt(deepMembers.Count - 1);
									}
									break;
								case '>':
									if(deepMembers.Count > 0) {
										memberAccess.Add(deepMembers.LastOrDefault());
										deepMembers.RemoveAt(deepMembers.Count - 1);
									}
									break;
								case ']':
									if(deepMembers.Count > 0) {
										memberAccess.Add(deepMembers.LastOrDefault());
										deepMembers.RemoveAt(deepMembers.Count - 1);
									}
									break;
							}
							continue;
						}
						#endregion

						members.Add(new CompletionInfo() {
							name = p,
							kind = CompletionKind.None,
						});
					} else {
						if(isDot) {
							pLower = "";
						}
						if(prevMember != null && prevMember.kind != CompletionKind.Namespace) {
							MemberInfo[] memberInfos = GetCompletionMembers(prevMember);
							if(memberInfos == null) {
								switch(prevMember.kind) {
									case CompletionKind.None:
										continue;
									case CompletionKind.Symbol:
										continue;
									case CompletionKind.Keyword:
										switch(prevMember.keywordKind) {
											case KeywordKind.This:
												var owner = prevMember.targetOwner;
												if(owner != null) {
													if(owner is INodeRoot) {
														memberInfos = (owner as INodeRoot).GetInheritType().GetMembers();
													} else {
														memberInfos = owner.GetType().GetMembers();
													}
												}
												break;
										}
										if(memberInfos == null)
											continue;
										break;
									default:
										throw new Exception("Couldn't handle CompletionInfo");
								}
							}
							foreach(var m in memberInfos) {
								if(IsGeneratedMember(m)) {
									continue;
								}
								if(isLast) {
									if(m.Name.ToLower().StartsWith(pLower) && !completionNames.Contains(m.Name)) {
										CompletionInfo data = new CompletionInfo() {
											name = m.Name,
											member = m,
										};
										switch(m.MemberType) {
											case MemberTypes.Field:
												data.kind = CompletionKind.Field;
												if(!resolveData.isStatic && (m as FieldInfo).IsStatic) {
													continue;
												}
												break;
											case MemberTypes.Property:
												data.kind = CompletionKind.Property;
												if(!resolveData.isStatic && ReflectionUtils.GetMemberIsStatic(m)) {
													continue;
												}
												break;
											case MemberTypes.Method:
												if((m as MethodInfo).IsGenericMethodDefinition || !resolveData.isStatic && (m as MethodInfo).IsStatic) {
													continue;
												}
												data.kind = CompletionKind.Method;
												break;
											case MemberTypes.NestedType:
											case MemberTypes.TypeInfo:
												if(!resolveData.isStatic || (m as Type).IsGenericTypeDefinition) {
													continue;
												}
												if(!(m as Type).IsVisible || (m as Type).Name.StartsWith("<")) {
													continue;
												}
												data.kind = CompletionKind.Type;
												break;
											default:
												continue;
										}
										completions.Add(data);
										completionNames.Add(m.Name);
									}
								} else {
									if(m.Name.Equals(p)) {
										CompletionInfo data = new CompletionInfo() {
											name = m.Name,
											member = m,
										};
										switch(m.MemberType) {
											case MemberTypes.Field:
												data.kind = CompletionKind.Field;
												if(!(m as FieldInfo).IsStatic) {
													resolveData.isStatic = false;
												}
												break;
											case MemberTypes.Property:
												data.kind = CompletionKind.Property;
												if(!ReflectionUtils.GetMemberIsStatic(m)) {
													resolveData.isStatic = false;
												}
												break;
											case MemberTypes.Method:
												data.kind = CompletionKind.Method;
												if(!(m as MethodInfo).IsStatic) {
													resolveData.isStatic = false;
												}
												break;
											case MemberTypes.NestedType:
											case MemberTypes.TypeInfo:
												data.kind = CompletionKind.Type;
												break;
											default:
												continue;
										}
										members.Add(data);
										break;
									}
								}
							}
						} else if(typeMap != null && resolveData.isStatic) {
							ResolveNamespaceCompletions(p, isLast, members, completions, completionNames);
						}
					}
				}
				completions.Sort((x, y) => string.Compare(x.name, y.name, StringComparison.OrdinalIgnoreCase));
				memberPath = members;
				return completions.ToArray();
			}
			memberPath = null;
			return null;
		}

		class ResolveData {
			public string text;
			public string textLower;
			public bool isLast;
			public bool isDot => text == ".";
			public HashSet<string> completionNames;
			public List<CompletionInfo> members;
			public List<CompletionInfo> completions;

			public bool isStatic = true;
		}

		private static bool ResolveNamespace(ResolveData resolveData, Dictionary<string, List<Type>> typeMap) {
			if(resolveData.isStatic) {
				if(resolveData.isLast) {
					foreach(var pair in typeMap) {
						if(pair.Key.ToLower().StartsWith(resolveData.textLower) && !resolveData.completionNames.Contains(pair.Key)) {
							resolveData.completions.Add(new CompletionInfo() {
								name = pair.Key,
								kind = CompletionKind.Namespace,
							});
							resolveData.completionNames.Add(pair.Key);
						}
					}
				} else {
					foreach(var pair in typeMap) {
						if(pair.Key.Equals(resolveData.text)) {
							resolveData.members.Add(new CompletionInfo() {
								name = pair.Key,
								kind = CompletionKind.Namespace,
							});
							return true;
						}
					}
				}
			}
			return false;
		}

		private static bool ResolveType(ResolveData resolveData, IEnumerable<Type> types) {
			foreach(var t in types) {
				if(resolveData.isLast) {
					if(t.Name.ToLower().StartsWith(resolveData.textLower) && !resolveData.completionNames.Contains(t.Name)) {
						if(t.IsGenericTypeDefinition || !resolveData.isStatic) {
							continue;
						}
						resolveData.completions.Add(new CompletionInfo() {
							name = t.Name,
							member = t,
							kind = CompletionKind.Type,
						});
						resolveData.completionNames.Add(t.Name);
					}
				} else {
					if(t.Name.Equals(resolveData.text)) {
						resolveData.members.Add(new CompletionInfo() {
							name = t.Name,
							member = t,
							kind = CompletionKind.Type,
						});
						return true;
					}
				}
			}
			return false;
		}

		private static bool ResolveFunction(ResolveData resolveData, IEnumerable<Function> functions, Object owner) {
			if(functions == null) return false;
			foreach(var d in functions) {
				if(resolveData.isLast) {
					if(d.name.ToLower().StartsWith(resolveData.textLower) && !resolveData.completionNames.Contains(d.name)) {
						resolveData.completions.Add(new CompletionInfo() {
							name = d.name,
							kind = CompletionKind.uNodeFunction,
							member = d.ReturnType(),
							targetObject = d,
							targetOwner = owner,
						});
						resolveData.completionNames.Add(d.name);
					}
				} else {
					if(d.name.Equals(resolveData.text)) {
						resolveData.members.Add(new CompletionInfo() {
							name = d.name,
							kind = CompletionKind.uNodeFunction,
							member = d.ReturnType(),
							targetObject = d,
							targetOwner = owner,
						});
						return true;
					}
				}
			}
			return false;
		}

		private static bool ResolveProperty(ResolveData resolveData, IEnumerable<Property> property, Object owner) {
			if(property == null) return false;
			foreach(var d in property) {
				if(resolveData.isLast) {
					if(d.name.ToLower().StartsWith(resolveData.textLower) && !resolveData.completionNames.Contains(d.name)) {
						resolveData.completions.Add(new CompletionInfo() {
							name = d.name,
							kind = CompletionKind.uNodeProperty,
							member = d.ReturnType(),
							targetObject = d,
							targetOwner = owner,
						});
						resolveData.completionNames.Add(d.name);
					}
				} else {
					if(d.name.Equals(resolveData.text)) {
						resolveData.members.Add(new CompletionInfo() {
							name = d.name,
							kind = CompletionKind.uNodeProperty,
							member = d.ReturnType(),
							targetObject = d,
							targetOwner = owner,
						});
						return true;
					}
				}
			}
			return false;
		}

		private static bool ResolveVariable(ResolveData resolveData, IEnumerable<Variable> variables, Object owner) {
			if(variables == null) return false;
			foreach(var d in variables) {
				if(resolveData.isLast) {
					if(d.name.ToLower().StartsWith(resolveData.textLower) && !resolveData.completionNames.Contains(d.name)) {
						resolveData.completions.Add(new CompletionInfo() {
							name = d.name,
							kind = CompletionKind.uNodeVariable,
							member = d.type,
							targetObject = d,
							targetOwner = owner,
						});
						resolveData.completionNames.Add(d.name);
					}
				} else {
					if(d.name.Equals(resolveData.text)) {
						resolveData.members.Add(new CompletionInfo() {
							name = d.name,
							kind = CompletionKind.uNodeVariable,
							member = d.type,
							targetObject = d,
							targetOwner = owner,
						});
						return true;
					}
				}
			}
			return false;
		}

		private bool ResolveKeywords(ResolveData resolveData) {
			if(setting.validCompletionKind.HasFlags(CompletionKind.Keyword)) {
				if(setting.allowTypeKeyword) {
					foreach(var pair in typeKeywords) {
						if(resolveData.isLast) {
							if(pair.Key.StartsWith(resolveData.textLower) && !resolveData.completionNames.Contains(pair.Key)) {
								resolveData.completions.Add(new CompletionInfo() {
									name = pair.Key,
									kind = CompletionKind.Keyword,
									keywordKind = GetKeywordKind(pair.Key),
									member = pair.Value,
								});
								resolveData.completionNames.Add(pair.Key);
							}
						} else {
							if(pair.Key.Equals(resolveData.text)) {
								resolveData.members.Add(new CompletionInfo() {
									name = pair.Key,
									kind = CompletionKind.Keyword,
									keywordKind = GetKeywordKind(pair.Key),
									member = pair.Value,
								});
								return true;
							}
						}
					}
				}
				if(setting.allowSymbolKeyword) {
					foreach(var key in symbolKeywords) {
						if(resolveData.isLast) {
							if(key.StartsWith(resolveData.textLower) && !resolveData.completionNames.Contains(key)) {
								resolveData.completions.Add(new CompletionInfo() {
									name = key,
									kind = CompletionKind.Keyword,
									keywordKind = GetKeywordKind(key),
									targetObject = this.data.owner,
									targetOwner = this.data.UnityObject,
								});
								resolveData.completionNames.Add(key);
							}
						} else {
							if(key.Equals(resolveData.text)) {
								resolveData.members.Add(new CompletionInfo() {
									name = key,
									kind = CompletionKind.Keyword,
									keywordKind = GetKeywordKind(key),
									targetObject = this.data.owner,
									targetOwner = this.data.UnityObject,
								});
								return true;
							}
						}
					}
				}
				if(setting.allowValueKeyword) {
					foreach(var key in valueKeywords) {
						if(resolveData.isLast) {
							if(key.StartsWith(resolveData.textLower) && !resolveData.completionNames.Contains(key)) {
								var keywordKind = GetKeywordKind(key);
								resolveData.completions.Add(new CompletionInfo() {
									name = key,
									kind = CompletionKind.Keyword,
									keywordKind = keywordKind,
									member = GetKeywordType(keywordKind),
								});
								resolveData.completionNames.Add(key);
							}
						} else {
							if(key.Equals(resolveData.text)) {
								var keywordKind = GetKeywordKind(key);
								resolveData.members.Add(new CompletionInfo() {
									name = key,
									kind = CompletionKind.Keyword,
									keywordKind = keywordKind,
									member = GetKeywordType(keywordKind),
								});
								return true;
							}
						}
					}
				}
			}
			return false;
		}

		private static bool ResolveLiteral(ResolveData resolveData) {
			if(resolveData.textLower[0] == '\"') {//String
				if(resolveData.text[resolveData.text.Length - 1] != '\"') {
					resolveData.text += '\"';
				}
				if(resolveData.isLast) {
					resolveData.completions.Add(new CompletionInfo() {
						name = resolveData.text,
						kind = CompletionKind.Literal,
						member = typeof(string),
					});
				} else {
					resolveData.members.Add(new CompletionInfo() {
						name = resolveData.text,
						kind = CompletionKind.Literal,
						member = typeof(string),
					});
					resolveData.isStatic = false;
				}
				return true;
			} else if(char.IsNumber(resolveData.text[0])) {//Number
				Type numberType = typeof(int);
				char lastChar = resolveData.textLower.Last();
				switch(lastChar) {
					case 'f':
						numberType = typeof(float);
						break;
					case 'd':
						numberType = typeof(double);
						break;
					case 'm':
						numberType = typeof(decimal);
						break;
					case 'u':
						numberType = typeof(uint);
						break;
					case 'l':
						numberType = typeof(long);
						break;
				}
				if(resolveData.isLast) {
					resolveData.completions.Add(new CompletionInfo() {
						name = resolveData.text,
						kind = CompletionKind.Literal,
						member = numberType,
					});
				} else {
					resolveData.members.Add(new CompletionInfo() {
						name = resolveData.text,
						kind = CompletionKind.Literal,
						member = numberType,
					});
					resolveData.isStatic = false;
				}
				return true;
			}
			return false;
		}

		private static bool ResolveParameter(IParameterSystem parameterSystem, ResolveData resolveData, Object owner) {
			foreach(var d in parameterSystem.Parameters) {
				if(resolveData.isLast) {
					if(d.name.ToLower().StartsWith(resolveData.textLower) && !resolveData.completionNames.Contains(d.name)) {
						resolveData.completions.Add(new CompletionInfo() {
							name = d.name,
							kind = CompletionKind.uNodeParameter,
							member = d.Type,
							targetObject = new ParameterRef(parameterSystem as UGraphElement, d),
							targetOwner = owner,
						});
						resolveData.completionNames.Add(d.name);
					}
				} else {
					if(d.name.Equals(resolveData.text)) {
						resolveData.members.Add(new CompletionInfo() {
							name = d.name,
							kind = CompletionKind.uNodeParameter,
							member = d.Type,
							targetObject = new ParameterRef(parameterSystem as UGraphElement, d),
							targetOwner = owner,
						});
						return true;
					}
				}
			}
			return false;
		}
		
		private static bool ResolveLocalVariable(ILocalVariableSystem localVariableSystem, ResolveData resolveData, Object owner) {
			foreach(var d in localVariableSystem.LocalVariables) {
				if(resolveData.isLast) {
					if(d.name.ToLower().StartsWith(resolveData.textLower) && !resolveData.completionNames.Contains(d.name)) {
						resolveData.completions.Add(new CompletionInfo() {
							name = d.name,
							kind = CompletionKind.uNodeLocalVariable,
							member = d.type,
							targetObject = d,
							targetOwner = owner,
						});
						resolveData.completionNames.Add(d.name);
					}
				} else {
					if(d.name.Equals(resolveData.text)) {
						resolveData.members.Add(new CompletionInfo() {
							name = d.name,
							kind = CompletionKind.uNodeLocalVariable,
							member = d.type,
							targetObject = d,
							targetOwner = owner,
						});
						return true;
					}
				}
			}
			return false;
		}

		public static MemberInfo[] GetCompletionMembers(CompletionInfo completion) {
			if(completion == null)
				return null;
			MemberInfo[] memberInfos = null;
			if(completion.member != null) {
				memberInfos = ReflectionUtils.GetMemberType(completion.member).GetMembers();
			} else if(completion.targetObject is Variable) {
				memberInfos = (completion.targetObject as Variable).type.GetMembers();
			} else if(completion.targetObject is Property) {
				memberInfos = (completion.targetObject as Property).ReturnType().GetMembers();
			} else if(completion.targetObject is Function) {
				memberInfos = (completion.targetObject as Function).ReturnType().GetMembers();
			}
			return memberInfos;
		}

		private void ResolveNamespaceCompletions(string path,
			bool isLast,
			List<CompletionInfo> members,
			List<CompletionInfo> completions,
			HashSet<string> completionNames) {
			if(typeMap != null) {
				List<Type> types;
				string namePath = JoinMember(members);
				{
					if(typeMap.ContainsKey(namePath + "." + path)) {
						members.Add(new CompletionInfo() {
							name = path,
							kind = CompletionKind.Namespace,
						});
						return;
					}
				}
				if(typeMap.TryGetValue(namePath, out types)) {
					string pLower = path.ToLower();
					if(path == ".") {
						pLower = "";
					}
					foreach(var t in types) {
						if(isLast) {
							if(t.Name.ToLower().StartsWith(pLower) && !completionNames.Contains(t.Name)) {
								if(t.IsGenericTypeDefinition || !t.IsVisible || t.Name.StartsWith("<")) {
									continue;
								}
								completions.Add(new CompletionInfo() {
									name = t.Name,
									member = t,
									kind = CompletionKind.Type,
								});
								completionNames.Add(t.Name);
							}
						} else {
							if(t.Name.Equals(path)) {
								members.Add(new CompletionInfo() {
									name = t.Name,
									member = t,
									kind = CompletionKind.Type,
								});
								break;
							}
						}
					}
				}
			}
		}

		public Type GetKeywordType(KeywordKind keywordKind) {
			switch(keywordKind) {
				case KeywordKind.Bool:
				case KeywordKind.True:
				case KeywordKind.False:
					return typeof(bool);
				case KeywordKind.Byte:
					return typeof(byte);
				case KeywordKind.Char:
					return typeof(char);
				case KeywordKind.Decimal:
					return typeof(decimal);
				case KeywordKind.Delegate:
					return typeof(System.Delegate);
				case KeywordKind.Double:
					return typeof(double);
				case KeywordKind.Enum:
					return typeof(Enum);
				case KeywordKind.Float:
					return typeof(float);
				case KeywordKind.Int:
					return typeof(int);
				case KeywordKind.Is:
					return typeof(bool);
				case KeywordKind.Long:
					return typeof(long);
				case KeywordKind.Object:
					return typeof(object);
				case KeywordKind.Sbyte:
					return typeof(sbyte);
				case KeywordKind.Short:
					return typeof(short);
				case KeywordKind.String:
					return typeof(string);
				case KeywordKind.Struct:
					return typeof(ValueType);
				case KeywordKind.This:
					if(setting.owner != null) {
						if(setting.owner is INodeRoot) {
							return (setting.owner as INodeRoot).GetInheritType();
						}
						return setting.owner.GetType();
					}
					break;
				case KeywordKind.Typeof:
					return typeof(Type);
				case KeywordKind.Uint:
					return typeof(uint);
				case KeywordKind.Ulong:
					return typeof(ulong);
				case KeywordKind.Ushort:
					return typeof(ushort);
				case KeywordKind.Void:
					return typeof(void);
			}
			return null;
		}

		public KeywordKind GetKeywordKind(string str) {
			var names = Enum.GetNames(typeof(KeywordKind));
			foreach(var n in names) {
				if(str == n.ToLower()) {
					return (KeywordKind)Enum.Parse(typeof(KeywordKind), n);
				}
			}
			return KeywordKind.None;
		}

		public object GetKeywordValue(KeywordKind keywordKind) {
			switch(keywordKind) {
				case KeywordKind.Bool:
					return default(bool);
				case KeywordKind.True:
					return true;
				case KeywordKind.False:
					return false;
				case KeywordKind.Byte:
					return default(byte);
				case KeywordKind.Char:
					return default(char);
				case KeywordKind.Decimal:
					return default(decimal);
				case KeywordKind.Double:
					return default(double);
				case KeywordKind.Float:
					return default(float);
				case KeywordKind.Int:
					return default(int);
				case KeywordKind.Long:
					return default(long);
				case KeywordKind.Object:
					return new object();
				case KeywordKind.Sbyte:
					return default(sbyte);
				case KeywordKind.Short:
					return default(short);
				case KeywordKind.String:
					return string.Empty;
				case KeywordKind.This:
					return setting.owner;
				case KeywordKind.Uint:
					return default(uint);
				case KeywordKind.Ulong:
					return default(ulong);
			}
			return null;
		}

		private bool IsGeneratedMember(MemberInfo member) {
			return member.Name.StartsWith("set_") ||
				member.Name.StartsWith("get_") ||
				member.Name.StartsWith("op_") ||
				member.Name.StartsWith("add_") ||
				member.Name.StartsWith("remove_");
		}

		public void Evaluate(IList<CompletionInfo> path, CompletionInfo lastPath, Action<CompletionInfo[]> action) {
			if(path == null) {
				return;
			}
			if(path.Count > 0) {
				var lPath = path[path.Count - 1];
				switch(lPath.kind) {
					default:
						path.Add(lastPath);
						break;
					case CompletionKind.None:
						path[path.Count - 1] = lastPath;
						break;
					case CompletionKind.Symbol:
						if(lPath.name != lastPath.name) {
							path.Add(lastPath);
						}
						//switch(lPath.name) {
						//	case "(":
						//	case "[":
						//	case "{":
						//	case "<":
						//		path.Add(lastPath);
						//		break;
						//	//case "<":
						//	//	if(path.Count > 1) {
						//	//		var prevPath = path[path.Count - 2];
						//	//		if(prevPath.kind == CompletionKind.Method) {
						//	//			path.Add(lastPath);
						//	//		}
						//	//	}
						//	//	break;
						//	default:
						//		if(lPath.name != lastPath.name) {
						//			path.Add(lastPath);
						//		}
						//		break;
						//}
						break;
				}
			}
			else {
				path.Add(lastPath);
			}
			CompletionInfo[] result = path.ToArray();
			action(result);
		}

		/// <summary>
		/// Evaluate an given input.
		/// </summary>
		/// <param name="input"></param>
		/// <param name="lastPath"></param>
		/// <param name="action"></param>
		public void Evaluate(string input, CompletionInfo lastPath, Action<CompletionInfo[]> action) {
			evalCount++;
			new Thread(() => {
				int handle = evalCount;

				if(!string.IsNullOrEmpty(input)) {
					List<CompletionInfo> path;
					GetCompletions(input, out path);

					// Avoid old threads overriding with old results
					if(handle == evalCount) {
						uNodeThreadUtility.Queue(() => {
							if(handle == evalCount) {
								Evaluate(path, lastPath, action);
							}
						});
					}
				}
			}).Start();
		}

		/// <summary>
		/// Set input to update completions.
		/// </summary>
		/// <param name="input"></param>
		public void SetInput(string input, Action<CompletionInfo[]> callback = null) {
			handleCount++;
			new Thread(() => {
				int handle = handleCount;

				if(!string.IsNullOrEmpty(input)) {
					List<CompletionInfo> members;
					var result = GetCompletions(input, out members);

					// Avoid old threads overriding with old results
					if(handle == handleCount) {
						completions = result;
						memberPaths = members;
						if(completions == null) {
							return;
						}
						//for(var i = 0; i < completions.Length; i++) {
						//	completions[i].name = input + completions[i].name;
						//}

						//if(completions.Length == 1 && completions[0].name.Trim() == input.Trim()) {
						//	completions = new CompletionInfo[0];
						//}
					}
				} else {
					completions = new CompletionInfo[0];
					memberPaths = new List<CompletionInfo>();
				}
				if(callback != null) {
					uNodeThreadUtility.Queue(() => {
						callback.Invoke(completions);
					});
				}
			}).Start();
		}

		private static List<CompletionInfo> GetParameters(CompletionInfo parent, IList<CompletionInfo> completions) {
			List<CompletionInfo> result = new List<CompletionInfo>();
			int deepLevel = 0;
			int gLevel = 0;
			bool flag = false;
			bool flag2 = false;
			for(int i = 0; i < completions.Count; i++) {
				var c = completions[i];
				if(c == parent) {
					flag = true;
					continue;
				}
				if(flag) {
					if(c.isSymbol) {
						switch(c.name) {
							case "<":
								if(i > 0 && completions[i - 1].kind == CompletionKind.Method) {
									deepLevel++;
									gLevel++;
								} else if(deepLevel > 0 && flag2) {
									result.Add(c);
								}
								break;
							case "[":
							case "{":
								if(deepLevel > 0 && flag2) {
									result.Add(c);
								}
								deepLevel++;
								break;
							case ">":
								if(gLevel > 0 && i > 0 && completions[i - 1].kind == CompletionKind.Type) {
									deepLevel--;
									gLevel--;
								} else if(deepLevel > 0 && flag2) {
									result.Add(c);
								}
								break;
							case "]":
							case "}":
								if(deepLevel > 0 && flag2) {
									result.Add(c);
								}
								deepLevel++;
								break;
							case "(":
								if(deepLevel == 0) {
									flag2 = true;
								} else if(deepLevel > 0 && flag2) {
									result.Add(c);
								}
								deepLevel++;
								break;
							case ")":
								deepLevel--;
								if(deepLevel == 0) {
									flag2 = false;
								} else if(deepLevel > 0 && flag2) {
									result.Add(c);
								}
								break;
							default:
								if(deepLevel > 0 && flag2) {
									result.Add(c);
								}
								break;
						}
						if(deepLevel == 0 && flag && flag2) {
							break;
						}
					} else if(deepLevel > 0 && flag2) {
						result.Add(c);
					} else if(deepLevel == 0) {
						if(c.name.Trim() != "") {
							return result;
						}
					}
				}
			}
			return ResolveCompletions(result);
		}

		private static List<CompletionInfo> GetGenericParameters(CompletionInfo parent, IList<CompletionInfo> completions) {
			List<CompletionInfo> result = new List<CompletionInfo>();
			int deepLevel = 0;
			int gLevel = 0;
			bool flag = false;
			bool flag2 = false;
			for(int i = 0; i < completions.Count; i++) {
				var c = completions[i];
				if(c == parent) {
					flag = true;
					continue;
				}
				if(flag) {
					if(c.isSymbol) {
						switch(c.name) {
							case "(":
							case "[":
							case "{":
								deepLevel++;
								break;
							case ")":
							case "]":
							case "}":
								deepLevel++;
								break;
							case "<":
								if(i > 0 && completions[i - 1].kind == CompletionKind.Method) {
									if(deepLevel == 0) {
										flag2 = true;
									}
									deepLevel++;
									gLevel++;
								} else if(deepLevel > 0 && flag2) {
									result.Add(c);
								}
								break;
							case ">":
								if(gLevel > 0 && i > 0 && completions[i - 1].kind == CompletionKind.Type) {
									deepLevel--;
									if(deepLevel == 0) {
										flag2 = false;
									}
									gLevel--;
									break;
								} else if(deepLevel > 0 && flag2) {
									result.Add(c);
									break;
								}
								break;
						}
						if(deepLevel == 0 && flag && flag2) {
							break;
						}
					} else if(deepLevel > 0 && flag2) {
						result.Add(c);
					} else if(deepLevel == 0) {
						if(c.name.Trim() != "") {
							return result;
						}
					}
				}
			}
			return ResolveCompletions(result);
		}

		private static List<CompletionInfo> GetIndexers(CompletionInfo parent, IList<CompletionInfo> completions) {
			List<CompletionInfo> result = new List<CompletionInfo>();
			int deepLevel = 0;
			bool flag = false;
			bool flag2 = false;
			foreach(var c in completions) {
				if(c == parent) {
					flag = true;
					continue;
				}
				if(flag) {
					if(c.isSymbol) {
						switch(c.name) {
							case "(":
							case "<":
							case "{":
								deepLevel++;
								break;
							case ")":
							case ">":
							case "}":
								deepLevel++;
								break;
							case "[":
								if(deepLevel == 0) {
									flag2 = true;
								}
								deepLevel++;
								break;
							case "]":
								deepLevel--;
								if(deepLevel == 0) {
									flag2 = false;
								}
								break;
						}
						if(deepLevel == 0 && flag && flag2) {
							break;
						}
					} else if(deepLevel > 0 && flag2) {
						result.Add(c);
					} else if(deepLevel == 0) {
						if(c.name.Trim() != "") {
							return result;
						}
					}
				}
			}
			return ResolveCompletions(result);
		}

		private static List<CompletionInfo> ResolveCompletionSymbol(List<CompletionInfo> completions) {
			List<CompletionInfo> result = new List<CompletionInfo>();
			for(int i = 0; i < completions.Count; i++) {
				var c = completions[i];
				if(c.isSymbol) {
					var left = ResolveCompletionSymbol(result);
					var right = completions.Skip(i + 1).ToList();
					if(right != null && right.Count > 0) {
						right = ResolveCompletionSymbol(right);
					}
					result.Clear();
					if(left != null && left.Count > 0 || right != null && right.Count > 0) {
						c.genericCompletions = left;
						c.parameterCompletions = right;
					}
					result.Add(c);
					break;
				} else {
					result.Add(c);
				}
			}
			return result;
		}

		public static List<CompletionInfo> ResolveCompletions(IList<CompletionInfo> completions) {
			List<CompletionInfo> result = new List<CompletionInfo>();
			if(completions == null && completions.Count == 0)
				return result;
			if(result.Any(item => item.isResolved))
				return completions.ToList();
			int deepLevel = 0;
			int gLevel = 0;
			for(int i = 0; i < completions.Count; i++) {
				var c = completions[i];
				if(c.isSymbol) {
					switch(c.name) {
						case "<":
							if(i > 0 && completions[i - 1].kind == CompletionKind.Method) {
								deepLevel++;
								gLevel++;
								continue;
							}
							goto default;
						case "(":
						case "[":
						case "{":
							deepLevel++;
							continue;
						case ")":
						case "]":
						case "}":
							deepLevel--;
							continue;
						case ">":
							if(gLevel > 0 && i > 0 && completions[i - 1].kind == CompletionKind.Type) {
								deepLevel--;
								gLevel--;
								continue;
							}
							goto default;
						default:
							if(deepLevel > 0)
								continue;
							if(operatorSymbolMap.Contains(c.name)) {
								CompletionInfo info = new CompletionInfo() {
									name = c.name,
									kind = c.kind,
									keywordKind = c.keywordKind,
									member = c.member,
									parameterCompletions = c.parameterCompletions,
									genericCompletions = c.genericCompletions,
									targetObject = c.targetObject,
									targetOwner = c.targetOwner,
								};
								while(i + 1 < completions.Count) {
									var nextC = completions[i + 1];
									if(nextC.isSymbol && operatorSymbolMap.Contains(nextC.name)) {
										info.name += nextC.name;
										i++;
									} else {
										break;
									}
								}
								result.Add(info);
							} else {
								result.Add(c);
							}
							continue;
					}
				}
				if(deepLevel > 0)
					continue;
				if(c.isKeyword) {
					result.Add(c);
					switch(c.keywordKind) {
						case KeywordKind.Catch:

							break;
						case KeywordKind.Default:
							c.parameterCompletions = GetParameters(c, completions);
							break;
						case KeywordKind.Delegate:

							break;
						case KeywordKind.Do:

							break;
						case KeywordKind.Else:

							break;
						case KeywordKind.For:
							c.parameterCompletions = GetParameters(c, completions);
							break;
						case KeywordKind.Foreach:
							c.parameterCompletions = GetParameters(c, completions);
							break;
						case KeywordKind.If:
							c.parameterCompletions = GetParameters(c, completions);
							break;
						case KeywordKind.Lock:
							c.parameterCompletions = GetParameters(c, completions);
							break;
						case KeywordKind.Sizeof:
							c.parameterCompletions = GetParameters(c, completions);
							break;
						case KeywordKind.Switch:
							c.parameterCompletions = GetParameters(c, completions);
							break;
						case KeywordKind.Try:

							break;
						case KeywordKind.Typeof:
							c.parameterCompletions = GetParameters(c, completions);
							break;
						case KeywordKind.Using:
							c.parameterCompletions = GetParameters(c, completions);
							break;
						case KeywordKind.While:
							c.parameterCompletions = GetParameters(c, completions);
							break;
					}
				} else if(c.isDot) {
					continue;
				} else {
					switch(c.kind) {
						case CompletionKind.Method:
						case CompletionKind.uNodeFunction:
							c.parameterCompletions = GetParameters(c, completions);
							c.genericCompletions = GetGenericParameters(c, completions);
							break;
						case CompletionKind.Property:
						case CompletionKind.uNodeParameter:
							c.parameterCompletions = GetIndexers(c, completions);
							break;
						case CompletionKind.Constructor:
							c.parameterCompletions = GetParameters(c, completions);
							break;
					}
					result.Add(c);
				}
			}
			var list = ResolveCompletionSymbol(result);
			foreach(var c in list) {
				c.isResolved = true;
			}
			return list;
		}

		/// <summary>
		/// Simplify completions by removing unnecessary completions.
		/// </summary>
		/// <param name="completions"></param>
		/// <returns></returns>
		public static List<CompletionInfo> SimplifyCompletions(IList<CompletionInfo> completions) {
			return completions.Where(item => !string.IsNullOrEmpty(item.name.Trim())).ToList();
		}

		/// <summary>
		/// Convert completions info to Graphs.
		/// </summary>
		/// <param name="completions"></param>
		/// <param name="editorData"></param>
		public static List<NodeObject> CompletionsToGraphs(IList<CompletionInfo> completions, GraphEditorData editorData, Vector2 graphPosition) {
			List<NodeObject> result = new List<NodeObject>();
			completions = ResolveCompletions(completions);
			if(completions == null && completions.Count == 0)
				return result;
			if(completions.Count == 1) {
				var c = completions[0];
				switch(c.kind) {
					case CompletionKind.Literal:
						var member = CompletionsToMemberData(new CompletionInfo[] { completions[0] }, editorData, graphPosition);
						if(member != null) {
							NodeEditorUtility.AddNewNode<MultipurposeNode>(editorData,
								graphPosition,
								(node) => {
									node.target = member;
									node.Register();
									result.Add(node);
								});
							return result;
						}
						break;
				}
			}
			List<CompletionInfo> memberPaths = new List<CompletionInfo>();
			Action action = () => {
				if(memberPaths.Count > 0) {
					List<MemberData> parameters;
					var member = CompletionsToMemberData(memberPaths, out parameters, editorData, graphPosition);
					memberPaths.Clear();
					if(member != null) {
						NodeEditorUtility.AddNewNode<MultipurposeNode>(editorData,
							graphPosition,
							(node) => {
								node.target = member;
								node.Register();
								if(node.parameters != null) {
									for(int i=0;i<node.parameters.Count;i++) {
										if(parameters.Count <= i) {
											break;
										}
										if(node.parameters[i].input != null) {
											node.parameters[i].input.AssignToDefault(parameters[i]);
										}
									}
								}
								if(result.Count > 0) {
									var prevNode = result.Last();
									if(prevNode != null && prevNode.CanGetValue()) {
										node.instance.ConnectTo(prevNode.primaryValueOutput);
									}
								}
								result.Add(node);
							});
					}
				}
			};
			for(int i = 0; i < completions.Count; i++) {
				var c = completions[i];
				switch(c.kind) {
					case CompletionKind.Namespace:
						continue;
					case CompletionKind.Literal:
						action();
						if(c != null) {
							var member = CompletionsToMemberData(new CompletionInfo[] { c }, editorData, graphPosition);
							if(member != null) {
								NodeEditorUtility.AddNewNode<MultipurposeNode>(editorData,
									graphPosition,
									(node) => {
										node.target = member;
										node.Register();
										result.Add(node);
									});
							}
						}
						break;
					case CompletionKind.Symbol: {
						action();
						string symbol = c.name;
						if(i + 2 < completions.Count) {
							var nextC = completions[i + 1];
							if(nextC.isSymbol && operatorSymbolMap.Contains(nextC.name) && operatorSymbolMap.Contains(symbol)) {
								symbol += nextC.name;
								i++;
							}
						}
						var node = completionToNode(new CompletionInfo() {
							name = symbol,
							kind = c.kind,
							keywordKind = c.keywordKind,
							member = c.member,
							parameterCompletions = c.parameterCompletions,
							genericCompletions = c.genericCompletions,
							targetObject = c.targetObject,
							targetOwner = c.targetOwner,
						}, editorData, graphPosition);
						if(node != null) {
							result.Add(node);
						}
						break;
					}
					case CompletionKind.Keyword:
						action();
						switch(c.keywordKind) {
							case KeywordKind.As:
							case KeywordKind.Break:
							case KeywordKind.Continue:
							case KeywordKind.Default:
							case KeywordKind.For:
							case KeywordKind.Foreach:
							case KeywordKind.If:
							case KeywordKind.Is:
							case KeywordKind.Lock:
							case KeywordKind.Return:
							case KeywordKind.Switch:
							case KeywordKind.Throw:
							case KeywordKind.Try:
							case KeywordKind.Using:
							case KeywordKind.While: {
								var node = completionToNode(c, editorData, graphPosition);
								if(node != null) {
									result.Add(node);
								}
								break;
							}
							case KeywordKind.Bool:
								NodeEditorUtility.AddNewNode<MultipurposeNode>(editorData,
									graphPosition,
									(node) => {
										node.target = new MemberData(false);
										node.Register();
										result.Add(node);
									});
								break;
							case KeywordKind.Byte:
								NodeEditorUtility.AddNewNode<MultipurposeNode>(editorData,
									graphPosition,
									(node) => {
										node.target = new MemberData(default(byte));
										node.Register();
										result.Add(node);
									});
								break;
							case KeywordKind.Char:
								NodeEditorUtility.AddNewNode<MultipurposeNode>(editorData,
									graphPosition,
									(node) => {
										node.target = new MemberData(default(char));
										node.Register();
										result.Add(node);
									});
								break;
							case KeywordKind.Decimal:
								NodeEditorUtility.AddNewNode<MultipurposeNode>(editorData,
									graphPosition,
									(node) => {
										node.target = new MemberData(default(decimal));
										node.Register();
										result.Add(node);
									});
								break;
							case KeywordKind.Double:
								NodeEditorUtility.AddNewNode<MultipurposeNode>(editorData,
									graphPosition,
									(node) => {
										node.target = new MemberData(default(double));
										node.Register();
										result.Add(node);
									});
								break;
							case KeywordKind.False:
								NodeEditorUtility.AddNewNode<MultipurposeNode>(editorData,
									graphPosition,
									(node) => {
										node.target = new MemberData(false);
										node.Register();
										result.Add(node);
									});
								break;
							case KeywordKind.Float:
								NodeEditorUtility.AddNewNode<MultipurposeNode>(editorData,
									graphPosition,
									(node) => {
										node.target = new MemberData(default(float));
										node.Register();
										result.Add(node);
									});
								break;
							case KeywordKind.Int:
								NodeEditorUtility.AddNewNode<MultipurposeNode>(editorData,
									graphPosition,
									(node) => {
										node.target = new MemberData(default(int));
										node.Register();
										result.Add(node);
									});
								break;
							case KeywordKind.Long:
								NodeEditorUtility.AddNewNode<MultipurposeNode>(editorData,
									graphPosition,
									(node) => {
										node.target = new MemberData(default(long));
										node.Register();
										result.Add(node);
									});
								break;
							case KeywordKind.New:

								break;
							case KeywordKind.Null:
								NodeEditorUtility.AddNewNode<MultipurposeNode>(editorData,
									graphPosition,
									(node) => {
										node.target = MemberData.Null;
										node.Register();
										result.Add(node);
									});
								break;
							case KeywordKind.Object:
								NodeEditorUtility.AddNewNode<MultipurposeNode>(editorData,
									graphPosition,
									(node) => {
										node.target = new MemberData(new object());
										node.Register();
										result.Add(node);
									});
								break;
							case KeywordKind.Sbyte:
								NodeEditorUtility.AddNewNode<MultipurposeNode>(editorData,
									graphPosition,
									(node) => {
										node.target = new MemberData(default(sbyte));
										node.Register();
										result.Add(node);
									});
								break;
							case KeywordKind.Short:
								NodeEditorUtility.AddNewNode<MultipurposeNode>(editorData,
									graphPosition,
									(node) => {
										node.target = new MemberData(default(short));
										node.Register();
										result.Add(node);
									});
								break;
							case KeywordKind.String:
								NodeEditorUtility.AddNewNode<MultipurposeNode>(editorData,
									graphPosition,
									(node) => {
										node.target = new MemberData("");
										node.Register();
										result.Add(node);
									});
								break;
							case KeywordKind.This:
								NodeEditorUtility.AddNewNode<MultipurposeNode>(editorData,
									graphPosition,
									(node) => {
										node.target = MemberData.This(c.targetOwner);
										node.Register();
										result.Add(node);
									});
								break;
							case KeywordKind.True:
								NodeEditorUtility.AddNewNode<MultipurposeNode>(editorData,
									graphPosition,
									(node) => {
										node.target = new MemberData(true);
										node.Register();
										result.Add(node);
									});
								break;
							case KeywordKind.Typeof:
								if(c.parameterCompletions.Count > 0) {
									if(c.parameterCompletions.Last().member is Type) {
										NodeEditorUtility.AddNewNode<MultipurposeNode>(editorData,
										graphPosition,
										(node) => {
											node.target = new MemberData(c.parameterCompletions.Last().member);
											node.Register();
											result.Add(node);
										});
									}
								}
								else {
									NodeEditorUtility.AddNewNode<MultipurposeNode>(editorData,
									graphPosition,
									(node) => {
										node.target = new MemberData(typeof(object));
										node.Register();
										result.Add(node);
									});
								}
								break;
							case KeywordKind.Uint:
								NodeEditorUtility.AddNewNode<MultipurposeNode>(editorData,
									graphPosition,
									(node) => {
										node.target = new MemberData(default(uint));
										node.Register();
										result.Add(node);
									});
								break;
							case KeywordKind.Ulong:
								NodeEditorUtility.AddNewNode<MultipurposeNode>(editorData,
									graphPosition,
									(node) => {
										node.target = new MemberData(default(ulong));
										node.Register();
										result.Add(node);
									});
								break;
							case KeywordKind.Ushort:
								NodeEditorUtility.AddNewNode<MultipurposeNode>(editorData,
									graphPosition,
									(node) => {
										node.target = new MemberData(default(ushort));
										node.Register();
										result.Add(node);
									});
								break;
							case KeywordKind.Void:
								NodeEditorUtility.AddNewNode<MultipurposeNode>(editorData,
									graphPosition,
									(node) => {
										node.target = new MemberData(typeof(void));
										node.Register();
										result.Add(node);
									});
								break;
						}
						break;
					case CompletionKind.Type:
						memberPaths.Add(c);
						break;
					case CompletionKind.Field:
					case CompletionKind.Method:
					case CompletionKind.Property:
						if(memberPaths.Count == 0) {
							memberPaths.Add(new CompletionInfo() {
								name = c.member.ReflectedType.Name,
								kind = CompletionKind.Type,
								member = c.member.ReflectedType,
								targetOwner = c.targetOwner,
								targetObject = c.targetObject,
							});
						}
						memberPaths.Add(c);
						break;
					case CompletionKind.uNodeVariable:
					case CompletionKind.uNodeProperty:
					case CompletionKind.uNodeFunction:
					case CompletionKind.uNodeLocalVariable:
					case CompletionKind.uNodeParameter:
						memberPaths.Add(c);
						action();
						break;
					default:
						action();
						break;
				}
			}
			action();
			return result;
		}

		public static List<MemberData> MultiCompletionsToMemberData(
			IList<CompletionInfo> completions,
			GraphEditorData editorData,
			Vector2 graphPosition) {
			List<MemberData> result = new List<MemberData>();
			List<List<CompletionInfo>> completionInfos = new List<List<CompletionInfo>>();
			bool flag = true;
			foreach(var c in completions) {
				if(flag) {
					completionInfos.Add(new List<CompletionInfo>());
					flag = false;
				}
				if(c.isSymbol) {
					if(c.name == ",") {
						flag = true;
						continue;
					}
				}
				completionInfos.Last().Add(c);
			}
			foreach(var info in completionInfos) {
				result.Add(CompletionsToMemberData(info, editorData, graphPosition));
			}
			return result;
		}

		/// <summary>
		/// Convert Completions info to MemberData.
		/// </summary>
		/// <param name="completions"></param>
		/// <param name="editorData"></param>
		/// <param name="graphPosition"></param>
		/// <returns></returns>
		public static MemberData CompletionsToMemberData(
			IList<CompletionInfo> completions,
			GraphEditorData editorData = null,
			Vector2 graphPosition = default(Vector2)) {
			List<MemberData> memberParameters;
			var result = CompletionsToMemberData(completions, out memberParameters, editorData, graphPosition);
			if(editorData != null && memberParameters != null && memberParameters.Count > 0) {
				NodeObject n = null;
				NodeEditorUtility.AddNewNode<MultipurposeNode>(editorData,
					graphPosition,
					(node) => {
						node.target = new MemberData(result);
						node.Register();
						if(node.parameters != null) {
							for(int i=0;i<node.parameters.Count;i++) {
								if(memberParameters.Count <= i)
									break;
								if(node.parameters[i].input != null) {
									node.parameters[i].input.AssignToDefault(memberParameters[i]);
								}
							}
						}
						n = node;
					});
				return MemberData.CreateFromValue(new UPortRef(n.primaryValueOutput));
			}
			return result;
		}

		/// <summary>
		/// Convert Completions info to MemberData.
		/// </summary>
		/// <param name="completions"></param>
		/// <param name="memberParameters"></param>
		/// <param name="editorData"></param>
		/// <param name="graphPosition"></param>
		/// <returns></returns>
		public static MemberData CompletionsToMemberData(
		IList<CompletionInfo> completions,
		out List<MemberData> memberParameters,
		GraphEditorData editorData = null,
		Vector2 graphPosition = default(Vector2)) {
			memberParameters = new List<MemberData>();
			//bool toNodes = false;
			if(completions != null && completions.Count > 0) {
				object instanced = null;
				Type startType = null;
				Type targetType = null;
				#region Literal
				if(completions.Count == 1 && completions[0].kind == CompletionKind.Literal) {
					//Handled values
					var info = completions[0];
					instanced = info.GetLiteralValue();
					if(instanced != null) {
						return new MemberData(instanced);
					} else {
						targetType = null;
					}
				}
				#endregion
				List<UnityEngine.Object> genericObjects = new List<UnityEngine.Object>();
				List<MemberData.ItemData> items = new List<MemberData.ItemData>();
				MemberData.TargetType memberTargetType = MemberData.TargetType.None;

				foreach(var c in completions) {
					MemberData.ItemData iData = new MemberData.ItemData() {
						name = c.name,
					};
					switch(c.kind) {
						case CompletionKind.Namespace:
							continue;
						case CompletionKind.Literal:
							targetType = ReflectionUtils.GetMemberType(c.member);
							if(startType == null) {
								startType = targetType;
							}
							instanced = c.GetLiteralValue();
							items.Add(iData);
							continue;
						case CompletionKind.Type:
							targetType = ReflectionUtils.GetMemberType(c.member);
							startType = targetType;
							items.Clear();
							break;
						case CompletionKind.Keyword: {
							Type t = null;
							if(typeKeywords.TryGetValue(c.name, out t)) {
								targetType = ReflectionUtils.GetMemberType(t);
								if(startType == null) {
									startType = targetType;
								}
							} else {
								switch(c.keywordKind) {
									case KeywordKind.Null:
										return MemberData.Null;
									case KeywordKind.None:
										return MemberData.None;
									case KeywordKind.Object:
										return new MemberData(new object());
									case KeywordKind.Bool:
										return new MemberData(default(bool));
									case KeywordKind.Byte:
										return new MemberData(default(byte));
									case KeywordKind.Char:
										return new MemberData(default(char));
									case KeywordKind.Decimal:
										return new MemberData(default(decimal));
									case KeywordKind.Double:
										return new MemberData(default(double));
									case KeywordKind.False:
										return new MemberData(false);
									case KeywordKind.Float:
										return new MemberData(default(float));
									case KeywordKind.Int:
										return new MemberData(default(int));
									case KeywordKind.Long:
										return new MemberData(default(long));
									case KeywordKind.Sbyte:
										return new MemberData(default(sbyte));
									case KeywordKind.Short:
										return new MemberData(default(short));
									case KeywordKind.String:
										return new MemberData("");
									case KeywordKind.This:
										if(editorData != null && editorData.graph != null) {
											//return MemberData.This(editorData.targetRoot);
											return new MemberData(editorData.graph, MemberData.TargetType.Self);
										}
										return MemberData.Null;
									case KeywordKind.True:
										return new MemberData(true);
									case KeywordKind.Uint:
										return new MemberData(default(uint));
									case KeywordKind.Ulong:
										return new MemberData(default(ulong));
									case KeywordKind.Ushort:
										return new MemberData(default(ushort));
									case KeywordKind.Void:
										return new MemberData(typeof(void));
									case KeywordKind.Typeof:
										if(c.parameterCompletions.Count > 0) {
											return new MemberData(c.parameterCompletions.Last().member);
										} else {
											return new MemberData(typeof(object));
										}
									default:
										if(c.member != null) {
											targetType = ReflectionUtils.GetMemberType(c.member);
											if(startType == null) {
												startType = targetType;
											}
										}
										if(editorData != null) {
											var nodes = CompletionsToGraphs(new CompletionInfo[] { c }, editorData, graphPosition);
											if(nodes != null && nodes.Count > 0) {
												return MemberData.CreateFromValue(new UPortRef(nodes.First().primaryValueOutput));
											}
										}
										throw new Exception("Could't handle keyword: " + c.keywordKind.ToString());
								}
							}
						}
						break;
						case CompletionKind.Field:
							if(ReflectionUtils.GetMemberIsStatic(c.member) && c == completions[completions.Count - 1]) {
								if(c.member.DeclaringType.IsEnum) {
									return new MemberData(Enum.Parse(c.member.DeclaringType, c.member.Name));
								}
								return new MemberData(c.member);
							}
							targetType = ReflectionUtils.GetMemberType(c.member);
							if(startType == null) {
								startType = targetType;
							}
							break;
						case CompletionKind.Method:
							MethodInfo methodInfo = c.member as MethodInfo;
							if(methodInfo != null) {
								List<MemberData> parameterMembers = new List<MemberData>();
								List<MemberData> genericMembers = new List<MemberData>();
								if(editorData != null) {
									var parameters = c.parameterCompletions;
									if(parameters != null && parameters.Count > 0) {
										parameterMembers = MultiCompletionsToMemberData(parameters, editorData, graphPosition);
									}
									var genericParameters = c.genericCompletions;
									if(genericParameters != null && genericParameters.Count > 0) {
										genericMembers = MultiCompletionsToMemberData(genericParameters, editorData, graphPosition);
									}
									{//Find best match for method
										var overloads = ReflectionUtils.GetOverloadingMethod(methodInfo);
										foreach(var m in overloads) {
											var p = m.GetParameters();
											var g = m.GetGenericArguments();
											if(p.Length == parameterMembers.Count && g.Length == genericMembers.Count) {
												methodInfo = m;
												break;
											}
										}
										if(methodInfo.IsGenericMethodDefinition) {
											var g = methodInfo.GetGenericArguments();
											if(g.Length == genericMembers.Count) {
												methodInfo = ReflectionUtils.MakeGenericMethod(methodInfo, genericMembers.Select(item => item.startType).ToArray());
											}
										}
									}
									if(parameterMembers.Count > 0) {
										memberParameters.AddRange(parameterMembers);
									} else {
										var param = methodInfo.GetParameters();
										if(param.Length > 0) {
											for(int i = 0; i < param.Length; i++) {
												var t = param[i].ParameterType;
												if(ReflectionUtils.CanCreateInstance(t)) {
													memberParameters.Add(new MemberData(ReflectionUtils.CreateInstance(t)));
												} else {
													memberParameters.Add(MemberData.None);
												}
											}
										}
									}
									//toNodes = true;
								}

								Type[] genericMethodArgs = methodInfo.GetGenericArguments();
								if(genericMethodArgs.Length > 0) {
									TypeData[] param = new TypeData[genericMethodArgs.Length];
									for(int i = 0; i < genericMethodArgs.Length; i++) {
										param[i] = MemberDataUtility.GetTypeData(genericMethodArgs[i], null);
									}
									iData.genericArguments = param;
									//if(info.item.value.GetInstance() is UnityEngine.Object) {
									//	genericObjects.Add(d.item.value.GetInstance() as UnityEngine.Object);
									//}
								}
								ParameterInfo[] paramsInfo = methodInfo.GetParameters();
								if(paramsInfo.Length > 0) {
									TypeData[] paramData = new TypeData[paramsInfo.Length];
									for(int x = 0; x < paramsInfo.Length; x++) {
										TypeData gData = MemberDataUtility.GetTypeData(paramsInfo[x],
											genericMethodArgs != null ? genericMethodArgs.Select(it => it.Name).ToArray() : null);
										paramData[x] = gData;
									}
									iData.parameters = paramData;
								}
								targetType = ReflectionUtils.GetMemberType(methodInfo);
								if(startType == null) {
									startType = targetType;
								}
							}
							break;
						case CompletionKind.Property:
							targetType = ReflectionUtils.GetMemberType(c.member);
							if(startType == null) {
								startType = targetType;
							}
							break;
						case CompletionKind.uNodeVariable:
							targetType = (c.targetObject as Variable).type;
							if(startType == null) {
								startType = targetType;
							}
							memberTargetType = MemberData.TargetType.uNodeVariable;
							iData = MemberDataUtility.CreateItemData(c.targetObject as Variable);
							break;
						case CompletionKind.uNodeLocalVariable:
							targetType = (c.targetObject as Variable).type;
							if(startType == null) {
								startType = targetType;
							}
							memberTargetType = MemberData.TargetType.uNodeLocalVariable;
							iData = MemberDataUtility.CreateItemData(c.targetObject as Variable);
							break;
						case CompletionKind.uNodeParameter:
							targetType = (c.targetObject as ParameterRef).type;
							if(startType == null) {
								startType = targetType;
							}
							memberTargetType = MemberData.TargetType.uNodeParameter;
							iData = MemberDataUtility.CreateItemData(c.targetObject as ParameterRef);
							break;
						case CompletionKind.uNodeProperty:
							targetType = (c.targetObject as Property).ReturnType();
							if(startType == null) {
								startType = targetType;
							}
							memberTargetType = MemberData.TargetType.uNodeProperty;
							iData = MemberDataUtility.CreateItemData(c.targetObject as Property);
							break;
						case CompletionKind.uNodeFunction:
							targetType = (c.targetObject as Function).ReturnType();
							if(startType == null) {
								startType = typeof(MonoBehaviour);
							}
							memberTargetType = MemberData.TargetType.uNodeFunction;
							iData = MemberDataUtility.CreateItemData(c.targetObject as Function);
							if(editorData != null) {
								var func = c.targetObject as Function;
								var parameters = c.parameterCompletions;
								if(parameters != null && parameters.Count > 0) {
									var parameterMembers = MultiCompletionsToMemberData(parameters, editorData, graphPosition);
									if(parameterMembers.Count > 0) {
										memberParameters.AddRange(parameterMembers);
									}
								} else {
									if(func.parameters.Count > 0) {
										foreach(var p in func.parameters) {
											if(ReflectionUtils.CanCreateInstance(p.Type)) {
												memberParameters.Add(new MemberData(ReflectionUtils.CreateInstance(p.Type)));
											} else {
												memberParameters.Add(MemberData.Empty);
											}
										}
									}
								}
							}
							break;
						default:
							if(editorData != null) {
								var nodes = CompletionsToGraphs(completions, editorData, graphPosition);
								if(nodes != null && nodes.Count > 0) {
									return MemberData.CreateFromValue(new UPortRef(nodes.First().primaryValueOutput));
								}
							}
							throw new Exception("Could't handle input: " + c.name);
					}
					items.Add(iData);
				}
				MemberData member = new MemberData();
				{
					member.startType = startType;
					member.type = targetType;
					{
						var c = completions[completions.Count - 1];
						if(c.member != null) {
							member.isStatic = ReflectionUtils.GetMemberIsStatic(c.member);
							if(!member.isStatic && completions.Count > 1) {
								foreach(var d in completions) {
									if(d.kind == CompletionKind.Type || d.kind == CompletionKind.Keyword) {
										continue;
									}
									switch(c.kind) {
										case CompletionKind.Field:
										case CompletionKind.Method:
										case CompletionKind.Property:
											member.isStatic = ReflectionUtils.GetMemberIsStatic(d.member);
											break;
									}
									if(member.isStatic) {
										break;
									}
								}
							}
						}
						if(memberTargetType == MemberData.TargetType.None) {
							switch(c.kind) {
								case CompletionKind.Field:
									memberTargetType = MemberData.TargetType.Field;
									break;
								case CompletionKind.Method:
									memberTargetType = MemberData.TargetType.Method;
									break;
								case CompletionKind.Property:
									memberTargetType = MemberData.TargetType.Property;
									break;
								case CompletionKind.Keyword:
									memberTargetType = MemberData.TargetType.Type;
									break;
								case CompletionKind.Type:
									if(c.member is Type) {
										Type t = c.member as Type;
										if(FilterAttribute.Default.IsValidTypeForValueConstant(t)) {
											return MemberData.CreateValueFromType(t);
										}
										else {
											return MemberData.CreateFromType(t);
										}
									}
									memberTargetType = MemberData.TargetType.Type;
									member.startName = completions.First(item => item.kind == CompletionKind.Type).member.Name;
									break;
							}
						} else {
							switch(memberTargetType) {
								case MemberData.TargetType.uNodeVariable:
								case MemberData.TargetType.uNodeLocalVariable:
								case MemberData.TargetType.uNodeProperty:
								case MemberData.TargetType.uNodeFunction:
								case MemberData.TargetType.uNodeParameter:
								case MemberData.TargetType.uNodeGenericParameter:
									member.isStatic = false;
									break;
							}
							member.instance = instanced;
						}
					}
					member.targetType = memberTargetType;
					member.Items = items.ToArray();
				}
				//if(toNodes) {
				//	var parameters = memberParameters;
				//	NodeComponent n = null;
				//	NodeEditorUtility.AddNewNode<MultipurposeNode>(editorData,
				//		graphPosition,
				//		(node) => {
				//			node.target.target = member;
				//			if(parameters != null) {
				//				node.target.parameters = parameters.ToArray();
				//			}
				//			uNodeEditorUtility.UpdateMultipurposeMember(node.target);
				//			n = node;
				//		});
				//	memberParameters = null;
				//	return new MemberData(n, MemberData.TargetType.ValueNode);
				//}
				return member;
			}
			return null;
		}
	}
}