using MaxyGames.UNode;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace MaxyGames {
	public static partial class CG {
		#region Others
		/// <summary>
		/// Generate a flow statements code
		/// </summary>
		/// <param name="statements"></param>
		/// <returns></returns>
		public static string Flow(params string[] statements) {
			string result = null;
			for(int i = 0; i < statements.Length; i++) {
				if(string.IsNullOrEmpty(result)) {
					result += statements[i];
				} else {
					result += statements[i].AddLineInFirst();
				}
			}
			return result;
		}

		/// <summary>
		/// Generate a flow statements code
		/// </summary>
		/// <param name="statements"></param>
		/// <returns></returns>
		public static string Flow(IEnumerable<string> statements) {
			string result = null;
			foreach(var str in statements) {
				if(string.IsNullOrEmpty(result)) {
					result += str;
				}
				else {
					result += str.AddLineInFirst();
				}
			}
			return result;
		}
		#endregion

		#region Keyword
		/// <summary>
		/// Generate this keyword.
		/// </summary>
		/// <returns></returns>
		public static string This {
			get {
				if(generationState.isStatic) {
					return graph.GetGraphName();
				}
				else {
					return "this";
				}
			}
		}

		/// <summary>
		/// Generate null keyword.
		/// </summary>
		/// <returns></returns>
		public const string Null = "null";

		public const string KeywordVar = "var";
		#endregion

		#region If Statement
		/// <summary>
		/// Generate a new if statement code.
		/// </summary>
		/// <param name="conditions"></param>
		/// <param name="contents"></param>
		/// <param name="isAnd"></param>
		/// <returns></returns>
		public static string If(string[] conditions, string contents, bool isAnd = true) {
			string condition = null;
			for(int i = 0; i < conditions.Length; i++) {
				if(i > 0) {
					condition += isAnd ? " && " : " || ";
				}
				condition += conditions[i];
			}
			return Condition("if", condition, contents);
		}

		/// <summary>
		/// Generate a new if statement code.
		/// </summary>
		/// <param name="condition"></param>
		/// <param name="contents"></param>
		/// <returns></returns>
		public static string If(string condition, string contents) {
			return Condition("if", condition, contents);
		}

		/// <summary>
		/// Generate a new if statement code.
		/// </summary>
		/// <param name="condition"></param>
		/// <param name="contents"></param>
		/// <param name="elseContents"></param>
		/// <returns></returns>
		public static string If(string condition, string contents, string elseContents) {
			if(!string.IsNullOrEmpty(elseContents)) {
				if(string.IsNullOrEmpty(contents)) {
					return Condition("if", condition.AddFirst("!(").Add(")"), elseContents);
				}
				string data = Condition("if", condition, contents);
				data += " else {" + elseContents.AddLineInFirst().AddTabAfterNewLine(1) + "\n}";
				return data;
			}
			return Condition("if", condition, contents);
		}

		/// <summary>
		/// Generate a new if statement code.
		/// </summary>
		/// <param name="conditions"></param>
		/// <param name="contents"></param>
		/// <param name="elseContents"></param>
		/// <param name="isAnd"></param>
		/// <returns></returns>
		public static string If(string[] conditions, string contents, string elseContents, bool isAnd = true) {
			string condition = null;
			for(int i = 0; i < conditions.Length; i++) {
				if(i > 0) {
					condition += isAnd ? " && " : " || ";
				}
				condition += conditions[i];
			}
			if(!string.IsNullOrEmpty(elseContents)) {
				if(string.IsNullOrEmpty(contents)) {
					return Condition("if", condition.AddFirst("!(").Add(")"), elseContents);
				}
				string data = Condition("if", condition, contents);
				data += " else {" + elseContents.AddLineInFirst().AddTabAfterNewLine(1) + "\n}";
				return data;
			}
			return Condition("if", condition, contents);
		}
		#endregion

		#region Switch Statement
		public static string Switch(string value, string[] cases, string[] contents, string @default = null) {
			if(cases.Length != contents.Length) {
				throw new InvalidOperationException("The cases length is not same with contents length");
			}
			string data = "switch(" + value + ") {";
			string swithContents = null;
			for(int i = 0; i < cases.Length; i++) {
				swithContents += "\ncase " + cases[i] + ": {";
				if(!string.IsNullOrEmpty(contents[i])) {
					swithContents += ("\n" + contents[i]).AddTabAfterNewLine(1);
				}
				swithContents += "\n}\nbreak;";
			}
			if(!string.IsNullOrEmpty(@default)) {
				swithContents += "\ndefault: {";
				swithContents += ("\n" + @default).AddTabAfterNewLine(1);
				swithContents += "\n}\nbreak;";
			}
			data += swithContents.AddTabAfterNewLine(1).AddLineInEnd() + "}";
			return data;
		}

		public static string Switch(string value, IList<(string caseValue, string content)> cases, string @default = null) {
			string data = "switch(" + value + ") {";
			string swithContents = null;
			for(int i = 0; i < cases.Count; i++) {
				var pair = cases[i];
				swithContents += "\ncase " + pair.caseValue + ": {";
				if(!string.IsNullOrEmpty(pair.content)) {
					swithContents += ("\n" + pair.content).AddTabAfterNewLine(1);
				}
				swithContents += "\n}\nbreak;";
			}
			if(!string.IsNullOrEmpty(@default)) {
				swithContents += "\ndefault: {";
				swithContents += ("\n" + @default).AddTabAfterNewLine(1);
				swithContents += "\n}\nbreak;";
			}
			data += swithContents.AddTabAfterNewLine(1).AddLineInEnd() + "}";
			return data;
		}

		public static string Switch(string value, IList<KeyValuePair<string[], string>> cases, string @default = null) {
			string data = "switch(" + value + ") {";
			string swithContents = null;
			for(int i = 0; i < cases.Count; i++) {
				var pair = cases[i];
				for(int x = 0; x < pair.Key.Length; x++) {
					swithContents += "\ncase " + pair.Key[x] + ":";
				}
				swithContents += " {";
				if(!string.IsNullOrEmpty(pair.Value)) {
					swithContents += ("\n" + pair.Value).AddTabAfterNewLine(1);
				}
				swithContents += "\n}\nbreak;";
			}
			if(!string.IsNullOrEmpty(@default)) {
				swithContents += "\ndefault: {";
				swithContents += ("\n" + @default).AddTabAfterNewLine(1);
				swithContents += "\n}\nbreak;";
			}
			data += swithContents.AddTabAfterNewLine(1).AddLineInEnd() + "}";
			return data;
		}
		#endregion

		#region For Statement
		/// <summary>
		/// Generate a new for statement code.
		/// </summary>
		/// <param name="variableName"></param>
		/// <param name="condition"></param>
		/// <param name="firstValue"></param>
		/// <param name="setVariable"></param>
		/// <param name="setType"></param>
		/// <param name="contents"></param>
		/// <returns></returns>
		public static string For(string variableName, string condition, object firstValue, object setVariable, SetType setType, string contents) {
			string data = "for(" +
				Type(typeof(int)) + " " + variableName + " = " + Value(firstValue) + ";" +
				condition + ";" + Set(variableName, setVariable, setType) + ") {";
			if(!string.IsNullOrEmpty(contents)) {
				data += ("\n" + contents).AddTabAfterNewLine(1) + "\n";
			}
			data += "}";
			return data;
		}

		/// <summary>
		/// Generate a new for statement code.
		/// </summary>
		/// <param name="initializer"></param>
		/// <param name="condition"></param>
		/// <param name="iterator"></param>
		/// <param name="contents"></param>
		/// <returns></returns>
		public static string For(string initializer, string condition, string iterator, string contents) {
			string data = "for(" + initializer + "; " + condition + "; " + iterator + ") {";
			if(!string.IsNullOrEmpty(contents)) {
				data += ("\n" + contents).AddTabAfterNewLine(1) + "\n";
			}
			data += "}";
			return data;
		}
		#endregion

		#region Set Code
		/// <summary>
		/// Generate a new set code without semicolon.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <param name="setType"></param>
		/// <returns></returns>
		public static string SetValue(string left, string right, SetType setType = SetType.Change) {
			switch(setType) {
				case SetType.Change:
					return left + " = " + right;
				case SetType.Add:
					return left + " += " + right;
				case SetType.Subtract:
					return left + " -= " + right;
				case SetType.Multiply:
					return left + " *= " + right;
				case SetType.Divide:
					return left + " /= " + right;
				case SetType.Modulo:
					return left + " %= " + right;
			}
			return null;
		}

		/// <summary>
		/// Generate a new set code.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <param name="setType"></param>
		/// <returns></returns>
		public static string Set(string left, string right, SetType setType = SetType.Change) {
			switch(setType) {
				case SetType.Change:
					return left + " = " + right + ";";
				case SetType.Add:
					return left + " += " + right + ";";
				case SetType.Subtract:
					return left + " -= " + right + ";";
				case SetType.Multiply:
					return left + " *= " + right + ";";
				case SetType.Divide:
					return left + " /= " + right + ";";
				case SetType.Modulo:
					return left + " %= " + right + ";";
			}
			return null;
		}

		public static string Set(string left, string right, Type leftType, Type rightType, SetType setType = SetType.Change) {
			if(leftType != null && !leftType.IsCastableTo(typeof(Delegate)) && !(leftType is RuntimeType)) {
				if(rightType == null || !rightType.IsCastableTo(leftType) && !rightType.IsValueType && rightType != typeof(string)) {
					if(leftType.IsValueType) {
						right = right.Insert(0, "(" + Type(leftType) + ")");
					} else if(right != "null") {
						right = right + " as " + Type(leftType);
					}
				}
			}
			bool flag = !generatePureScript && left.EndsWith("\")");
			if(flag) {
				var strs = left.CGSplitMember();
				var lastStr = strs[strs.Count - 1];
				string setCode = null;
				if(lastStr.StartsWith(nameof(IRuntimeClass.GetVariable) + "<", StringComparison.Ordinal)) {
					setCode = nameof(IRuntimeClass.SetVariable);
					if(right != "null" && leftType.IsCastableTo(typeof(Delegate))) {
						right = right.Wrap().Insert(0, "(" + Type(leftType) + ")");
					}
				} else if(lastStr.StartsWith(nameof(IRuntimeClass.GetProperty) + "<", StringComparison.Ordinal)) {
					setCode = nameof(IRuntimeClass.GetProperty);
					if(right != "null" && leftType.IsCastableTo(typeof(Delegate))) {
						right = right.Wrap().Insert(0, "(" + Type(leftType) + ")");
					}
				}
				if(setCode != null) {
					char code = '=';
					switch(setType) {
						case SetType.Subtract:
							code = '-';
							break;
						case SetType.Divide:
							code = '/';
							break;
						case SetType.Add:
							code = '+';
							break;
						case SetType.Multiply:
							code = '*';
							break;
						case SetType.Modulo:
							code = '%';
							break;
					}
					int firstIndex = lastStr.IndexOf("\"");
					string vName = lastStr.Substring(firstIndex, lastStr.LastIndexOf("\"") - firstIndex + 1);
					if(code != '=') {
						strs[strs.Count - 1] = DoGenerateInvokeCode(setCode, new string[] { vName, right, code.CGValue() }).AddSemicolon();
					} else {
						strs[strs.Count - 1] = DoGenerateInvokeCode(setCode, new string[] { vName, right }).AddSemicolon();
					}
					left = string.Join(".", strs);
					return left;
				}
			}
			return Set(left, right, setType);
		}
		#endregion

		#region Access Code
		/// <summary>
		/// Generate access code.
		/// </summary>
		/// <param name="instance"></param>
		/// <param name="members"></param>
		/// <returns></returns>
		public static string AccessElement(string instance, string index) {
			return instance.Add("[").Add(index).Add("]");
		}

		/// <summary>
		/// Generate access code.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="members"></param>
		/// <returns></returns>
		public static string AccessElement(Type type, string index) {
			return AccessElement(Type(type), index);
		}

		/// <summary>
		/// Generate access code.
		/// </summary>
		/// <param name="instance"></param>
		/// <param name="index"></param>
		/// <param name="set"></param>
		/// <returns></returns>
		public static string AccessElement(object instance, string index, bool set = false) {
			if(set == false && graph.IsNativeGraph() == false) {
				if(instance is ValueInput) {
					var port = instance as ValueInput;
					var type = port.ValueType;
					if(ReflectionUtils.IsNativeType(type) == false) {
						var elementType = type.ElementType();
						if(elementType != null) {
							return Convert(AccessElement(GeneratePort(port, set), index), elementType);
						}
					}
				}
			}
			return AccessElement(Value(instance), index);
		}

		/// <summary>
		/// Generate access code.
		/// </summary>
		/// <param name="instance"></param>
		/// <param name="members"></param>
		/// <returns></returns>
		public static string Access(string instance, params string[] members) {
			string result = instance;
			if(members.Length > 0 && (string.IsNullOrEmpty(result) || result == "null")) {
				throw new Exception("The generated instance is null");
			}
			foreach(var m in members) {
				result += m.AddFirst(".");
			}
			return result;
		}

		/// <summary>
		/// Generate access code.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="members"></param>
		/// <returns></returns>
		public static string Access(Type type, params string[] members) {
			string result = Type(type);
			return Access(result, members);
		}

		/// <summary>
		/// Generate access code.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="members"></param>
		/// <returns></returns>
		public static string Access(object instance, params string[] members) {
			string result = Value(instance);
			return Access(result, members);
		}
		#endregion

		#region New Object Code
		/// <summary>
		/// Generate a new object creation code.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="parameters"></param>
		/// <returns></returns>
		public static string New(Type type, params string[] parameters) {
			return New(Type(type), parameters);
		}

		/// <summary>
		/// Generate a new object creation code.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="parameters"></param>
		/// <returns></returns>
		public static string New(string type, params string[] parameters) {
			string paramName = parameters != null ? string.Join(", ", parameters.Where(item => !string.IsNullOrEmpty(item))) : null;
			return $"new {type}({paramName})";
		}

		/// <summary>
		/// Generate a new object creation code.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="parameters"></param>
		/// <returns></returns>
		public static string New(Type type, IEnumerable<string> parameters, IEnumerable<string> initializers) {
			return New(Type(type), parameters, initializers);
		}

		/// <summary>
		/// Generate a new object creation code.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="parameters"></param>
		/// <returns></returns>
		public static string New(string type, IEnumerable<string> parameters, IEnumerable<string> initializers) {
			string paramName = parameters != null ? string.Join(", ", parameters.Where(item => !string.IsNullOrEmpty(item))) : null;
			string initName = initializers != null ? string.Join(", ", initializers) : null;
			var result = $"new {type}({paramName})";
			if(string.IsNullOrEmpty(initName) == false) {
				result += " { " + initName + " }";
			}
			return result;
		}

		/// <summary>
		/// Generate a new object creation code.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="genericParameters"></param>
		/// <param name="parameters"></param>
		/// <param name="initializers"></param>
		/// <returns></returns>
		public static string NewGeneric(Type type, IEnumerable<string> genericParameters, IEnumerable<string> parameters, IEnumerable<string> initializers) {
			return NewGeneric(Type(type), genericParameters, parameters, initializers);
		}

		/// <summary>
		/// Generate a new object creation code.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="parameters"></param>
		/// <returns></returns>
		public static string NewGeneric(string type, IEnumerable<string> genericParameters, IEnumerable<string> parameters, IEnumerable<string> initializers) {
			string paramName = parameters != null ? string.Join(", ", parameters) : null;
			string initName = initializers != null ? string.Join(", ", initializers) : null;
			string genericNames = genericParameters != null ? string.Join(", ", genericParameters) : null;
			if(!string.IsNullOrEmpty(genericNames)) {
				genericNames = $"<{genericNames}>";
			}
			var result = $"new {type}{genericNames}({paramName})";
			if(initName != null) {
				result += " { " + initName + " }";
			}
			return result;
		}
		#endregion

		#region Attributes
		public static string Attribute(Type type, IEnumerable<string> parameters = null, Dictionary<string, string> initializers = null) {
			return new AData(type, parameters, initializers).GenerateCode();
		}
		#endregion

		#region Value Generic Invoke
		/// <summary>
		/// Generate generic invoke code.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="variable"></param>
		/// <param name="functionName"></param>
		/// <param name="paramObject"></param>
		/// <returns></returns>
		public static string GenericInvoke<T>(string variable, string functionName, params string[] paramObject) {
			return GenericInvoke(typeof(T), variable, functionName, paramObject);
		}

		/// <summary>
		/// Generate generic invoke code.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="variable"></param>
		/// <param name="functionName"></param>
		/// <param name="paramObject"></param>
		/// <returns></returns>
		public static string GenericInvoke<T>(object variable, string functionName, params string[] paramObject) {
			return GenericInvoke(typeof(T), variable, functionName, paramObject);
		}

		/// <summary>
		/// Generate generic invoke code.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="type"></param>
		/// <param name="functionName"></param>
		/// <param name="paramObject"></param>
		/// <returns></returns>
		public static string GenericInvoke<T>(Type type, string functionName, params string[] paramObject) {
			return GenericInvoke(typeof(T), type, functionName, paramObject);
		}

		/// <summary>
		/// Generate generic invoke code.
		/// </summary>
		/// <param name="genericType"></param>
		/// <param name="instance"></param>
		/// <param name="functionName"></param>
		/// <param name="paramObject"></param>
		/// <returns></returns>
		public static string GenericInvoke(Type genericType, string instance, string functionName, params string[] paramObject) {
			if(string.IsNullOrEmpty(instance))
				return DoGenerateInvokeCode(functionName, paramObject, new Type[1] { genericType });
			return DoGenerateInvokeCode(instance + "." + functionName, paramObject, new Type[1] { genericType });
		}

		/// <summary>
		/// Generate generic invoke code.
		/// </summary>
		/// <param name="genericType"></param>
		/// <param name="instance"></param>
		/// <param name="functionName"></param>
		/// <param name="paramObject"></param>
		/// <returns></returns>
		public static string GenericInvoke(Type genericType, object instance, string functionName, params string[] paramObject) {
			string data = Value(instance);
			if(string.IsNullOrEmpty(data))
				return null;
			return DoGenerateInvokeCode(data + "." + functionName, paramObject, new Type[1] { genericType });
		}

		/// <summary>
		/// Generate generic invoke code.
		/// </summary>
		/// <param name="genericType"></param>
		/// <param name="type"></param>
		/// <param name="functionName"></param>
		/// <param name="paramObject"></param>
		/// <returns></returns>
		public static string GenericInvoke(Type genericType, Type type, string functionName, params string[] paramObject) {
			string data = Type(type);
			if(string.IsNullOrEmpty(data))
				return null;
			return DoGenerateInvokeCode(data + "." + functionName, paramObject, new Type[1] { genericType });
		}

		/// <summary>
		/// Generate generic invoke code.
		/// </summary>
		/// <param name="genericType"></param>
		/// <param name="instance"></param>
		/// <param name="functionName"></param>
		/// <param name="paramObject"></param>
		/// <returns></returns>
		public static string GenericInvoke(Type[] genericType, string instance, string functionName, params string[] paramObject) {
			if(string.IsNullOrEmpty(instance))
				return DoGenerateInvokeCode(functionName, paramObject, genericType);
			return DoGenerateInvokeCode(instance + "." + functionName, paramObject, genericType);
		}

		/// <summary>
		/// Generate generic invoke code.
		/// </summary>
		/// <param name="genericType"></param>
		/// <param name="instance"></param>
		/// <param name="functionName"></param>
		/// <param name="paramObject"></param>
		/// <returns></returns>
		public static string GenericInvoke(Type[] genericType, object instance, string functionName, params string[] paramObject) {
			string data = Value(instance);
			if(string.IsNullOrEmpty(data))
				return null;
			return DoGenerateInvokeCode(data + "." + functionName, paramObject, genericType);
		}

		/// <summary>
		/// Generate generic invoke code.
		/// </summary>
		/// <param name="genericType"></param>
		/// <param name="type"></param>
		/// <param name="functionName"></param>
		/// <param name="paramObject"></param>
		/// <returns></returns>
		public static string GenericInvoke(Type[] genericType, Type type, string functionName, params string[] paramObject) {
			string data = Type(type);
			if(string.IsNullOrEmpty(data))
				return null;
			return DoGenerateInvokeCode(data + "." + functionName, paramObject, genericType);
		}
		#endregion

		#region Generic InvokeCode
		/// <summary>
		/// Generate generic invoke code.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="instance"></param>
		/// <param name="functionName"></param>
		/// <param name="paramObject"></param>
		/// <returns></returns>
		public static string FlowGenericInvoke<T>(string instance, string functionName, params string[] paramObject) {
			return FlowGenericInvoke(typeof(T), instance, functionName, paramObject);
		}

		/// <summary>
		/// Generate generic invoke code.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="type"></param>
		/// <param name="functionName"></param>
		/// <param name="paramObject"></param>
		/// <returns></returns>
		public static string FlowGenericInvoke<T>(Type type, string functionName, params string[] paramObject) {
			return FlowGenericInvoke(typeof(T), type, functionName, paramObject);
		}

		/// <summary>
		/// Generate generic invoke code.
		/// </summary>
		/// <param name="genericType"></param>
		/// <param name="instance"></param>
		/// <param name="functionName"></param>
		/// <param name="paramObject"></param>
		/// <returns></returns>
		public static string FlowGenericInvoke(Type genericType, string instance, string functionName, params string[] paramObject) {
			if(string.IsNullOrEmpty(instance))
				return DoGenerateInvokeCode(functionName, paramObject, new Type[1] { genericType }).AddSemicolon();
			return DoGenerateInvokeCode(instance + "." + functionName, paramObject, new Type[1] { genericType }).AddSemicolon();
		}

		/// <summary>
		/// Generate generic invoke code.
		/// </summary>
		/// <param name="genericType"></param>
		/// <param name="type"></param>
		/// <param name="functionName"></param>
		/// <param name="paramObject"></param>
		/// <returns></returns>
		public static string FlowGenericInvoke(Type genericType, Type type, string functionName, params string[] paramObject) {
			string data = Type(type);
			if(string.IsNullOrEmpty(data))
				return null;
			return DoGenerateInvokeCode(data + "." + functionName, paramObject, new Type[1] { genericType }).AddSemicolon();
		}

		/// <summary>
		/// Generate generic invoke code.
		/// </summary>
		/// <param name="genericType"></param>
		/// <param name="instance"></param>
		/// <param name="functionName"></param>
		/// <param name="paramObject"></param>
		/// <returns></returns>
		public static string FlowGenericInvoke(Type[] genericType, string instance, string functionName, params string[] paramObject) {
			if(string.IsNullOrEmpty(instance))
				return DoGenerateInvokeCode(functionName, paramObject, genericType).AddSemicolon();
			return DoGenerateInvokeCode(instance + "." + functionName, paramObject, genericType).AddSemicolon();
		}

		/// <summary>
		/// Generate generic invoke code.
		/// </summary>
		/// <param name="genericType"></param>
		/// <param name="type"></param>
		/// <param name="functionName"></param>
		/// <param name="paramObject"></param>
		/// <returns></returns>
		public static string FlowGenericInvoke(Type[] genericType, Type type, string functionName, params string[] paramObject) {
			string data = Type(type);
			if(string.IsNullOrEmpty(data))
				return null;
			return DoGenerateInvokeCode(data + "." + functionName, paramObject, genericType).AddSemicolon();
		}
		#endregion

		#region Break
		/// <summary>
		/// Get break code.
		/// </summary>
		/// <returns></returns>
		public static string Break() {
			return "break;";
		}
		/// <summary>
		/// Get continue code.
		/// </summary>
		/// <returns></returns>
		public static string Continue() {
			return "continue;";
		}

		/// <summary>
		/// Get yield break code.
		/// </summary>
		/// <returns></returns>
		public static string YieldBreak() {
			return "yield break;";
		}
		#endregion

		#region Routine
		public static string Routine(params string[] parameters) {
			for(int i = 0; i < parameters.Length; i++) {
				if(!string.IsNullOrEmpty(parameters[i])) {
					break;
				}
				if(i + 1 == parameters.Length) {
					return null;
				}
			}
			return CG.Invoke(typeof(Runtime.Routine), nameof(Runtime.Routine.New), parameters);
		}
		public static string RoutineYield(params string[] parameters) {
			for(int i = 0; i < parameters.Length; i++) {
				if(!string.IsNullOrEmpty(parameters[i])) {
					break;
				}
				if(i + 1 == parameters.Length) {
					return null;
				}
			}
			return CG.Invoke(typeof(Runtime.Routine), nameof(Runtime.Routine.Yield), parameters);
		}

		public static string RoutineEvent(string contents) {
			return CG.Invoke(typeof(Runtime.Routine), nameof(Runtime.Routine.Event), contents);
		}

		public static string RoutineCreate(string contents) {
			if(string.IsNullOrEmpty(contents))
				return null;
			return Invoke(typeof(Runtime.EventCoroutine), nameof(Runtime.EventCoroutine.Create), Value(graph), contents);
		}
		#endregion

		#region Value Invoke
		/// <summary>
		/// Generate invoke code.
		/// </summary>
		/// <param name="instance"></param>
		/// <param name="functionName"></param>
		/// <param name="paramObject"></param>
		/// <returns></returns>
		public static string Invoke(string instance, string functionName, params string[] paramObject) {
			return Invoke(instance, functionName, null, paramObject);
		}

		/// <summary>
		/// Generate invoke code.
		/// </summary>
		/// <param name="instance"></param>
		/// <param name="functionName"></param>
		/// <param name="genericType"></param>
		/// <param name="paramObject"></param>
		/// <returns></returns>
		public static string Invoke(string instance, string functionName, Type[] genericType, params string[] paramObject) {
			if(string.IsNullOrEmpty(instance)) {
				return DoGenerateInvokeCode(functionName, paramObject, genericType);
			} else if(string.IsNullOrEmpty(functionName)) {
				return DoGenerateInvokeCode(instance, paramObject, genericType);
			}
			if(instance.EndsWith("[]")) {
				return DoGenerateInvokeCode(instance.RemoveLast(2) + "[" + functionName + "]", paramObject);
			}
			return DoGenerateInvokeCode(instance + "." + functionName, paramObject, genericType);
		}

		/// <summary>
		/// Generate invoke code.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="functionName"></param>
		/// <param name="paramObject"></param>
		/// <returns></returns>
		public static string Invoke(Type type, string functionName, params string[] paramObject) {
			return Invoke(type, functionName, null, paramObject);
		}

		/// <summary>
		/// Generate invoke code.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="functionName"></param>
		/// <param name="paramObject"></param>
		/// <returns></returns>
		public static string Invoke(Type type, string functionName, Type[] genericType, params string[] paramObject) {
			string data = Type(type);
			if(string.IsNullOrEmpty(data))
				return null;
			if(data.EndsWith("[]")) {
				return DoGenerateInvokeCode(data.RemoveLast(2) + "[" + functionName + "]", paramObject);
			}
			return DoGenerateInvokeCode(data + "." + functionName, paramObject, genericType);
		}

		/// <summary>
		/// Generate invoke code for static member.
		/// </summary>
		/// <param name="functionName"></param>
		/// <param name="paramObject"></param>
		/// <returns></returns>
		public static string StaticInvoke(string functionName, params string[] paramObject) {
			return DoGenerateInvokeCode(functionName, paramObject, new string[0]);
		}

		/// <summary>
		/// Generate invoke code for static member.
		/// </summary>
		/// <param name="functionName"></param>
		/// <param name="paramObject"></param>
		/// <param name="genericTypes"></param>
		/// <returns></returns>
		public static string StaticInvoke(string functionName, string[] paramObject, string[] genericTypes) {
			return DoGenerateInvokeCode(functionName, paramObject, genericTypes);
		}
		#endregion

		#region Flow Invoke
		/// <summary>
		/// Generate invoke code.
		/// </summary>
		/// <param name="variable"></param>
		/// <param name="functionName"></param>
		/// <param name="paramObject"></param>
		/// <returns></returns>
		public static string FlowInvoke(string variable, string functionName, params string[] paramObject) {
			return FlowInvoke(variable, functionName, null, paramObject);
		}

		/// <summary>
		/// Generate invoke code.
		/// </summary>
		/// <param name="variable"></param>
		/// <param name="functionName"></param>
		/// <param name="genericType"></param>
		/// <param name="paramObject"></param>
		/// <returns></returns>
		public static string FlowInvoke(string variable, string functionName, Type[] genericType, params string[] paramObject) {
			if(string.IsNullOrEmpty(variable)) {
				return DoGenerateInvokeCode(functionName, paramObject, genericType).AddSemicolon();
			} else if(string.IsNullOrEmpty(functionName)) {
				return DoGenerateInvokeCode(variable, paramObject, genericType).AddSemicolon();
			}
			if(variable.EndsWith("[]")) {
				return DoGenerateInvokeCode(variable.RemoveLast(2) + "[" + functionName + "]", paramObject).AddSemicolon();
			}
			return DoGenerateInvokeCode(variable + "." + functionName, paramObject, genericType).AddSemicolon();
		}

		/// <summary>
		/// Generate invoke code.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="functionName"></param>
		/// <param name="paramObject"></param>
		/// <returns></returns>
		public static string FlowInvoke(Type type, string functionName, params string[] paramObject) {
			return FlowInvoke(type, functionName, null, paramObject);
		}

		/// <summary>
		/// Generate invoke code.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="functionName"></param>
		/// <param name="paramObject"></param>
		/// <returns></returns>
		public static string FlowInvoke(Type type, string functionName, Type[] genericType, params string[] paramObject) {
			string data = Type(type);
			if(string.IsNullOrEmpty(data))
				return null;
			if(data.EndsWith("[]")) {
				return DoGenerateInvokeCode(data.RemoveLast(2) + "[" + functionName + "]", paramObject).AddSemicolon();
			}
			return DoGenerateInvokeCode(data + "." + functionName, paramObject, genericType).AddSemicolon();
		}

		/// <summary>
		/// Generate invoke code for static member.
		/// </summary>
		/// <param name="functionName"></param>
		/// <param name="paramObject"></param>
		/// <returns></returns>
		public static string FlowStaticInvoke(string functionName, params string[] paramObject) {
			return DoGenerateInvokeCode(functionName, paramObject, new string[0]).AddSemicolon();
		}

		/// <summary>
		/// Generate invoke code for static member.
		/// </summary>
		/// <param name="functionName"></param>
		/// <param name="paramObject"></param>
		/// <param name="genericTypes"></param>
		/// <returns></returns>
		public static string FlowStaticInvoke(string functionName, string[] paramObject, string[] genericTypes) {
			return DoGenerateInvokeCode(functionName, paramObject, genericTypes).AddSemicolon();
		}
		#endregion

		#region Invoke Code

		private static string DoGenerateInvokeCode(string functionName, string[] paramObject, Type[] genericType = null) {
			string paramName = null;
			if(paramObject != null) {
				for(int i = 0; i < paramObject.Length; i++) {
					if(string.IsNullOrEmpty(paramObject[i]))
						continue;
					if(i != 0) {
						paramName += ", ";
					}
					paramName += paramObject[i];
				}
			}
			string genericData = null;
			if(genericType != null && genericType.Length > 0) {
				genericData += "<";
				for(int i = 0; i < genericType.Length; i++) {
					if(i > 0) {
						genericData += ", ";
					}
					genericData += Type(genericType[i]);
				}
				genericData += ">";
			}
			if(string.IsNullOrEmpty(paramName)) {
				return functionName + genericData + "()";
			}
			if(functionName.EndsWith("[]")) {
				return functionName.RemoveLast(2) + "[" + paramName + "]";
			} else if(functionName.EndsWith("]") && string.IsNullOrEmpty(paramName)) {
				return functionName.AddSemicolon();
			}
			return functionName + genericData + "(" + paramName + ")";
		}

		private static string DoGenerateInvokeCode(string functionName, string[] paramValues, string[] genericTypes) {
			string paramName = "";
			if(paramValues != null) {
				for(int i = 0; i < paramValues.Length; i++) {
					if(i != 0) {
						paramName += ", ";
					}
					paramName += paramValues[i];
				}
			}
			string genericData = null;
			if(genericTypes != null && genericTypes.Length > 0) {
				genericData += "<";
				for(int i = 0; i < genericTypes.Length; i++) {
					if(i > 0) {
						genericData += ", ";
					}
					genericData += genericTypes[i];
				}
				genericData += ">";
			}
			if(string.IsNullOrEmpty(paramName)) {
				return functionName + genericData + "()";
			}
			if(functionName.EndsWith("[]")) {
				return functionName.RemoveLast(2) + "[" + paramName + "]";
			} else if(functionName.EndsWith("]") && string.IsNullOrEmpty(paramName)) {
				return functionName.AddSemicolon();
			}
			return functionName + genericData + "(" + paramName + ")";
		}
		#endregion

		#region Foreach Statement
		/// <summary>
		/// Generate a new foreach statement code.
		/// </summary>
		/// <param name="elementType"></param>
		/// <param name="variableName"></param>
		/// <param name="iteration"></param>
		/// <param name="contents"></param>
		/// <returns></returns>
		public static string Foreach(Type elementType, string variableName, string iteration, string contents) {
			string data = "foreach(" + (elementType != null ? Type(elementType) : "var") + " " + variableName + " in " + iteration + ") {";
			if(!string.IsNullOrEmpty(contents)) {
				data += ("\n" + contents).AddTabAfterNewLine(1) + "\n";
			}
			data += "}";
			return data;
		}
		#endregion

		#region Make Array
		/// <summary>
		/// Generate a new array creation code.
		/// </summary>
		/// <param name="elementType"></param>
		/// <param name="values"></param>
		/// <returns></returns>
		public static string MakeArray(Type elementType, params string[] values) {
			string elementObject = "[0]";
			if(values != null && values.Length > 0) {
				int index = 0;
				elementObject = "[" + //values.Length + 
					"] {";
				foreach(object o in values) {
					if(index != 0) {
						elementObject += ",";
					}
					elementObject += " " + o;
					index++;
				}
				elementObject += " }";
			}
			return "new " + Type(elementType) + elementObject;
		}

		/// <summary>
		/// Generate a new array creation code.
		/// </summary>
		/// <param name="elementType"></param>
		/// <param name="arrayLength"></param>
		/// <param name="values"></param>
		/// <returns></returns>
		public static string MakeArray(Type elementType, ValueInput arrayLength, params string[] values) {
			string length = arrayLength.isAssigned ? Value(arrayLength) : string.Empty;
			string elementObject = "[" + length + "]";
			if(values != null && values.Length > 0) {
				int index = 0;
				elementObject = "[" + length + "] {";
				foreach(object o in values) {
					if(index != 0) {
						elementObject += ",";
					}
					elementObject += " " + o;
					index++;
				}
				elementObject += " }";
			}
			return "new " + Type(elementType) + elementObject;
		}

		/// <summary>
		/// Generate a new array creation code.
		/// </summary>
		/// <param name="elementType"></param>
		/// <param name="arrayLength"></param>
		/// <param name="values"></param>
		/// <returns></returns>
		public static string MakeArray(Type elementType, MemberData arrayLength, params string[] values) {
			string length = arrayLength.isTargeted ? Value((object)arrayLength) : string.Empty;
			string elementObject = "[" + length + "]";
			if(values != null && values.Length > 0) {
				int index = 0;
				elementObject = "[" + length + "] {";
				foreach(object o in values) {
					if(index != 0) {
						elementObject += ",";
					}
					elementObject += " " + o;
					index++;
				}
				elementObject += " }";
			}
			return "new " + Type(elementType) + elementObject;
		}
		#endregion

		#region Lambda
		/// <summary>
		/// Generate correct lambda codes for event, eg: `() => { }` or `M_Generated()` based of it's contents.
		/// </summary>
		/// <param name="contents"></param>
		/// <returns></returns>
		public static string LambdaForEvent(string contents) {
			var strs = contents.Split('\n');
			int yieldCount = 0;
			int lastYieldIndex = 0;
			for(int i = 0; i < strs.Length; i++) {
				var str = strs[i];
				if(str.Contains("yield ")) {
					yieldCount++;
					lastYieldIndex = i;
				}
			}
			if(yieldCount == 0) {
				//var method = generatorData.AddNewGeneratedMethod(null, "void");
				//method.code = contents;
				return Lambda(contents);
			} else if(yieldCount == 1 && lastYieldIndex + 1 == strs.Length) {
				strs[lastYieldIndex] = strs[lastYieldIndex].Replace("yield ", "");
				//var method = generatorData.AddNewGeneratedMethod(null, "void");
				//method.code = string.Join("\n", strs);
				return Lambda(string.Join("\n", strs));
			} else {
				var method = generatorData.AddNewGeneratedMethod(null, typeof(IEnumerable));
				method.code = contents;
				return method.name + "()";
			}
		}

		/// <summary>
		/// Generate lambda code, eg: () => { }.
		/// </summary>
		/// <param name="contents"></param>
		/// <returns></returns>
		public static string Lambda(string contents) {
			return Lambda(null, null, contents);
		}

		/// <summary>
		/// Generate lambda code, () => { }..
		/// </summary>
		/// <param name="types"></param>
		/// <param name="parameterNames"></param>
		/// <param name="contents"></param>
		/// <returns></returns>
		public static string Lambda(IList<Type> types, IList<string> parameterNames, string contents) {
			if(types != null && parameterNames != null && types.Count != parameterNames.Count)
				return null;
			string parameters = null;
			if(types != null && parameterNames != null) {
				for(int i = 0; i < types.Count; i++) {
					if(i != 0) {
						parameters += ", ";
					}
					parameters += Type(types[i]) + " " + parameterNames[i];
				}
			}
			string data = "(" + parameters + ") => {";
			if(!string.IsNullOrEmpty(contents)) {
				data += contents.AddLineInFirst().AddTabAfterNewLine(1).AddLineInEnd();
			}
			data += "}";
			return data;
		}

		/// <summary>
		/// Generate lambda code.
		/// </summary>
		/// <param name="contents"></param>
		/// <returns></returns>
		public static string SimplifiedLambda(string contents) {
			return SimplifiedLambda(null, null, contents);
		}

		/// <summary>
		/// Generate lambda code.
		/// </summary>
		/// <param name="types"></param>
		/// <param name="parameterNames"></param>
		/// <param name="contents"></param>
		/// <returns></returns>
		public static string SimplifiedLambda(IList<Type> types, IList<string> parameterNames, string contents) {
			if(types != null && parameterNames != null && types.Count != parameterNames.Count)
				return null;
			string parameters = null;
			if(types != null && parameterNames != null) {
				for(int i = 0; i < types.Count; i++) {
					if(i != 0) {
						parameters += ", ";
					}
					parameters += Type(types[i]) + " " + parameterNames[i];
				}
			}
			return "(" + parameters + ") => " + contents;
		}

		/// <summary>
		/// Generate lambda code.
		/// </summary>
		/// <param name="parameterNames"></param>
		/// <param name="contents"></param>
		/// <returns></returns>
		public static string SimplifiedLambda(IEnumerable<string> parameterNames, string contents) {
			string parameters = null;
			if(parameterNames != null) {
				parameters = string.Join(", ", parameterNames);
			}
			return "(" + parameters + ") => " + contents;
		}
		#endregion

		#region Condition
		/// <summary>
		/// Generate a new condition code.
		/// </summary>
		/// <param name="conditionKey"></param>
		/// <param name="conditionContent"></param>
		/// <param name="contents"></param>
		/// <returns></returns>
		public static string Condition(string conditionKey, string conditionContent, string contents) {
			string data = conditionKey + "(" + conditionContent + ") {";
			if(conditionKey.Equals("do")) {
				data = conditionKey + " {";
			}
			if(!string.IsNullOrEmpty(contents)) {
				data += ("\n" + contents).AddTabAfterNewLine(1) + "\n";
			}
			data += "}";
			if(conditionKey.Equals("do")) {
				data += " while(" + conditionContent + ");";
			}
			return data;
		}
		#endregion

		#region Arithmeti Code
		/// <summary>
		/// Generate " + " operator code.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static string Add(string left, string right) {
			return Arithmetic(left, right, ArithmeticType.Add);
		}

		/// <summary>
		/// Generate " / " operator code.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static string Divide(string left, string right) {
			return Arithmetic(left, right, ArithmeticType.Divide);
		}

		/// <summary>
		/// Generate " % " operator code.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static string Modulo(string left, string right) {
			return Arithmetic(left, right, ArithmeticType.Modulo);
		}

		/// <summary>
		/// Generate " * " operator code.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static string Multiply(string left, string right) {
			return Arithmetic(left, right, ArithmeticType.Multiply);
		}

		/// <summary>
		/// Generate " - " operator code.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static string Subtract(string left, string right) {
			return Arithmetic(left, right, ArithmeticType.Subtract);
		}

		/// <summary>
		/// Generate arithmetic operation.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <param name="compareType"></param>
		/// <returns></returns>
		public static string Arithmetic(string left, string right, ArithmeticType arithmeticType = ArithmeticType.Add) {
			if(left == null && right == null)
				return null;
			switch(arithmeticType) {
				case ArithmeticType.Add:
					return left + " + " + right;
				case ArithmeticType.Divide:
					return left + " / " + right;
				case ArithmeticType.Modulo:
					return left + " % " + right;
				case ArithmeticType.Multiply:
					return left + " * " + right;
				case ArithmeticType.Subtract:
					return left + " - " + right;
			}
			throw new System.InvalidCastException();
		}
		#endregion

		#region Operator Code
		/// <summary>
		/// Generate And (left && right) code.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <param name="compareType"></param>
		/// <returns></returns>
		public static string And(string left, string right) {
			return left + " && " + right;
		}

		/// <summary>
		/// Generate Convert code.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public static string Convert(MemberData value, Type type) {
			if(type is RuntimeType && type is not INativeMember && value.type.IsCastableTo(typeof(IRuntimeClass))) {
				RegisterUsingNamespace("MaxyGames.UNode");
				if(generatePureScript) {
					return Value((object)value).CGAccess(
							DoGenerateInvokeCode(
								nameof(Extensions.ToRuntimeInstance),
								new string[0],
								new Type[] { type }
							)
						);
				} else {
					return Value(value).CGAccess(
							DoGenerateInvokeCode(
								nameof(Extensions.ToRuntimeInstance),
								new string[] { type.FullName.AddFirst(KEY_runtimeInterfaceKey, type.IsInterface).CGValue() },
								new[] { GetCompatibilityType(type) }
							)
						);
				}
			}
			return Convert(Value((object)value), Type(type));
		}

		/// <summary>
		/// Generate Convert code.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public static string Convert(ValueInput value, Type type) {
			if(type is RuntimeType && type is not INativeMember && value.ValueType.IsCastableTo(typeof(IRuntimeClass))) {
				RegisterUsingNamespace("MaxyGames.UNode");
				if(generatePureScript) {
					return Value(value).CGAccess(
							DoGenerateInvokeCode(
								nameof(Extensions.ToRuntimeInstance),
								new string[0],
								new Type[] { type }
							)
						);
				}
				else {
					return Value(value).CGAccess(
							DoGenerateInvokeCode(
								nameof(Extensions.ToRuntimeInstance),
								new string[] { type.FullName.AddFirst(KEY_runtimeInterfaceKey, type.IsInterface).CGValue() },
								new[] { GetCompatibilityType(type) }
							)
						);
				}
			}
			return Convert(Value(value), Type(type));
		}

		/// <summary>
		/// Generate Convert code.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public static string Convert(string value, Type type) {
			if(type is RuntimeType && type is not INativeMember) {
				RegisterUsingNamespace("MaxyGames.UNode");
				if(generatePureScript) {
					return value.CGAccess(
							DoGenerateInvokeCode(
								nameof(Extensions.ToRuntimeInstance),
								new string[0],
								new Type[] { type }
							)
						);
				} else {
					return value.CGAccess(
							DoGenerateInvokeCode(
								nameof(Extensions.ToRuntimeInstance),
								new string[] { type.FullName.AddFirst(KEY_runtimeInterfaceKey, type.IsInterface).CGValue() },
								new[] { GetCompatibilityType(type) }
							)
						);
				}
			}
			return Convert(value, Type(type));
		}

		/// <summary>
		/// Generate Convert code.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="type"></param>
		/// <param name="compatibility"></param>
		/// <returns></returns>
		public static string Convert(string value, Type type, bool compatibility) {
			if(!compatibility || generatePureScript) {
				return Convert(value, type);
			}
			if(type is RuntimeType && type is not INativeMember) {
				RegisterUsingNamespace("MaxyGames.UNode");
				return value.CGAccess(
					DoGenerateInvokeCode(
						nameof(Extensions.ToRuntimeInstance),
						new string[] { GetUniqueNameForType(type as RuntimeType) },
						new Type[] { GetCompatibilityType(type) }
					)
				);
			}
			return Convert(value, Type(type));
		}

		/// <summary>
		/// Generate Convert code.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public static string Convert(string value, string type) {
			if(string.IsNullOrEmpty(value) == false && value[0] == '-') {
				//In case it is negative number
                return "((" + type + ")(" + value + "))";
            }
			return "((" + type + ")" + value + ")";
		}

		/// <summary>
		/// Get Generated unique type name
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static string GetUniqueNameForType(RuntimeType type) {
			return type.FullName.AddFirst(KEY_runtimeInterfaceKey, type.IsInterface).CGValue();
		}

		/// <summary>
		/// Generate As code.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public static string As(ValueInput value, Type type) {
			if(type is RuntimeType && type is not INativeMember && value.ValueType.IsCastableTo(typeof(IRuntimeClass))) {
				RegisterUsingNamespace("MaxyGames.UNode");
				if(generatePureScript) {
					return Value(value).CGAccess(
							DoGenerateInvokeCode(
								nameof(Extensions.ToRuntimeInstance),
								new string[0],
								new Type[] { type }
							)
						);
				}
				else {
					return Value(value).CGAccess(
							DoGenerateInvokeCode(
								nameof(Extensions.ToRuntimeInstance),
								new string[] { GetUniqueNameForType(type as RuntimeType) },
								new Type[] { GetCompatibilityType(type) }
							)
						);
				}
			}
			if(type.IsValueType) {
				return Convert(Value(value), Type(type));
			}
			return As(Value(value), Type(type));
		}

		/// <summary>
		/// Generate As code.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public static string As(MemberData value, Type type) {
			if(type is RuntimeType && type is not INativeMember && value.type.IsCastableTo(typeof(IRuntimeClass))) {
				RegisterUsingNamespace("MaxyGames.UNode");
				if(generatePureScript) {
					return Value((object)value).CGAccess(
							DoGenerateInvokeCode(
								nameof(Extensions.ToRuntimeInstance),
								new string[0],
								new Type[] { type }
							)
						);
				} else {
					return Value(value).CGAccess(
							DoGenerateInvokeCode(
								nameof(Extensions.ToRuntimeInstance),
								new string[] { GetUniqueNameForType(type as RuntimeType) },
								new Type[] { GetCompatibilityType(type) }
							)
						);
				}
			}
			if(type.IsValueType) {
				return Convert(Value((object)value), Type(type));
			}
			return As(Value((object)value), Type(type));
		}

		/// <summary>
		/// Generate As code.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public static string As(string value, string type) {
			return "(" + value + " as " + type + ")";
		}

		/// <summary>
		/// Generate As code.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public static string As(string value, Type type) {
			if(type is RuntimeType && type is not INativeMember) {
				RegisterUsingNamespace("MaxyGames.UNode");
				if(generatePureScript) {
					return value.CGAccess(
							DoGenerateInvokeCode(
								nameof(Extensions.ToRuntimeInstance),
								new string[0],
								new Type[] { type }
							)
						);
				} else {
					return value.CGAccess(
							DoGenerateInvokeCode(
								nameof(Extensions.ToRuntimeInstance),
								new string[] { GetUniqueNameForType(type as RuntimeType) },
								new Type[] { GetCompatibilityType(type) }
							)
						);
				}
			}
			if(type.IsValueType) {
				return Convert(value, Type(type));
			}
			return "(" + value + " as " + type + ")";
		}

		/// <summary>
		/// Generate Is code.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public static string Is(ValueInput value, Type type) {
			if(type is RuntimeType && type is not INativeMember && value.ValueType.IsCastableTo(typeof(IRuntimeClass))) {
				RegisterUsingNamespace("MaxyGames.UNode");
				return Value(value).CGAccess(
						DoGenerateInvokeCode(
							nameof(Extensions.IsTypeOf),
							new string[0],
							new Type[] { type }
						)
					);
			}
			return Is(Value((object)value), type);
		}

		/// <summary>
		/// Generate Is code.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public static string Is(MemberData value, Type type) {
			if(type is RuntimeType && type is not INativeMember && value.type.IsCastableTo(typeof(IRuntimeClass))) {
				RegisterUsingNamespace("MaxyGames.UNode");
				return Value((object)value).CGAccess(
						DoGenerateInvokeCode(
							nameof(Extensions.IsTypeOf),
							new string[0],
							new Type[] { type }
						)
					);
			}
			return Is(Value((object)value), type);
		}

		/// <summary>
		/// Generate Is code.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public static string Is(string value, Type type) {
			return Is(value, Type(type));
		}

		/// <summary>
		/// Generate Is code.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public static string Is(string value, string type) {
			return "(" + value + " is " + type + ")";
		}

		/// <summary>
		/// Generate Or (left || right) code.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <param name="compareType"></param>
		/// <returns></returns>
		public static string Or(string left, string right) {
			return left + " || " + right;
		}

		public static string Operator(string left, string right, BitwiseType operatorType) {
			switch(operatorType) {
				case BitwiseType.And:
					return left + " & " + right;
				case BitwiseType.Or:
					return left + " | " + right;
				case BitwiseType.ExclusiveOr:
					return left + " ^ " + right;
				default:
					throw new System.InvalidCastException();
			}
		}

		/// <summary>
		/// Function for get correctly operator code
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <param name="operatorType"></param>
		/// <returns></returns>
		public static string Operator(string left, string right, ShiftType operatorType) {
			switch(operatorType) {
				case ShiftType.LeftShift:
					return left + " << " + right;
				case ShiftType.RightShift:
					return left + " >> " + right;
				default:
					throw new System.InvalidCastException();
			}
		}
		#endregion

		#region Compare Code
		/// <summary>
		/// Generate compare code.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <param name="compareType"></param>
		/// <returns></returns>
		public static string Compare(string left, string right, ComparisonType compareType = ComparisonType.Equal) {
			if(left == null && right == null)
				return null;
			switch(compareType) {
				case ComparisonType.Equal:
					return left + " == " + right;
				case ComparisonType.GreaterThan:
					return left + " > " + right;
				case ComparisonType.GreaterThanOrEqual:
					return left + " >= " + right;
				case ComparisonType.LessThan:
					return left + " < " + right;
				case ComparisonType.LessThanOrEqual:
					return left + " <= " + right;
				case ComparisonType.NotEqual:
					return left + " != " + right;
			}
			throw new InvalidCastException();
		}
		#endregion

		#region Return
		/// <summary>
		/// Generate return value code, eg: `return null`.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static string Return(string value = null) {
			if(string.IsNullOrEmpty(value))
				return "return null;";
			return "return " + value + ";";
		}
		#endregion

		#region Yield Return
		/// <summary>
		/// Generate yield return value code, eg: `yield return null;`.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static string YieldReturn(string value) {
			if(value == null) {
				return "yield return null;";
			}
			return "yield return " + value + ";";
		}
		#endregion

		#region Commentaries
		/// <summary>
		/// Generate a single line comments
		/// </summary>
		/// <param name="contents"></param>
		/// <returns></returns>
		public static string Comment(string contents) {
			return contents.AddFirst("/*").Add("*/");
		}
		#endregion

		#region Block
		/// <summary>
		/// Generate a new block of codes.
		/// </summary>
		/// <param name="contents"></param>
		/// <returns></returns>
		public static string Block(string contents) {
			string data = "{";
			if(!string.IsNullOrEmpty(contents)) {
				data += ("\n" + contents).AddTabAfterNewLine(1) + "\n";
			}
			data += "}";
			return data;
		}
		#endregion
	}
}