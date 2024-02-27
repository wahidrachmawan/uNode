using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using MaxyGames.UNode;

namespace MaxyGames {
	/// <summary>
	/// Privides usefull extensions functions for code generation.
	/// </summary>
	public static class GeneratorExtensions {
		/// <summary>
		/// Split the members
		/// </summary>
		/// <param name="strs"></param>
		/// <returns></returns>
		public static IList<string> CGSplitMember(this string member) {
			if(string.IsNullOrEmpty(member))
				return member == null ? new List<string>() : new List<string>() { member };
			List<string> strs = new List<string>();
			int deep = 0;
			string current = "";
			for(int i=0;i<member.Length;i++) {
				var c = member[i];
				 if(c == '.') {
					if(deep == 0) {
						strs.Add(current);
						current = "";
					} else {
						current += c;
					}
				} else {
					current += c;
					if(c == '<' || c == '(') {
						deep++;
					} else if(c == '>' || c == ')') {
						deep--;
					}
				}
			}
			strs.Add(current);
			return strs;
		}

		/// <summary>
		/// Generate code for values.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static string CGValue(this object value) {
			return CG.Value(value);
		}

		/// <summary>
		/// Generate code for flow.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="from"></param>
		/// <param name="waitTarget"></param>
		/// <returns></returns>
		public static string CGFlow(this FlowOutput port, bool waitTarget = true) {
			return CG.Flow(port, waitTarget);
		}

		/// <summary>
		/// Convert a string into valid variable name.
		/// </summary>
		/// <param name="str"></param>
		/// <param name="owner"></param>
		/// <returns></returns>
		public static string CGName(this string str, object owner) {
			return CG.GenerateName(str, owner);
		}

		/// <summary>
		/// Generate code for type.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static string CGType(this Type value) {
			return CG.Type(value);
		}

		/// <summary>
		/// Generate access element code.
		/// </summary>
		/// <param name="instance"></param>
		/// <param name="index"></param>
		/// <returns></returns>
		public static string CGAccessElement(this string instance, string index) {
			return CG.AccessElement(instance, index);
		}

		/// <summary>
		/// Generate access element code.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="index"></param>
		/// <returns></returns>
		public static string CGAccessElement(this Type type, string index) {
			return CG.AccessElement(type, index);
		}
		/// <summary>
		/// Generate access code.
		/// </summary>
		/// <param name="first"></param>
		/// <param name="members"></param>
		/// <returns></returns>
		public static string CGAccess(this string first, params string[] members) {
			return CG.Access(first, members);
		}

		/// <summary>
		/// Generate access code.
		/// </summary>
		/// <param name="first"></param>
		/// <param name="members"></param>
		/// <returns></returns>
		public static string CGAccess(this Type first, params string[] members) {
			return CG.Access(first, members);
		}

		#region Value Invoke
		/// <summary>
		/// Generate invoke code.
		/// </summary>
		/// <param name="instance"></param>
		/// <param name="functionName"></param>
		/// <param name="paramObject"></param>
		/// <returns></returns>
		public static string CGInvoke(this string instance, string functionName, params string[] paramObject) {
			return CG.Invoke(instance, functionName, paramObject);
		}

		/// <summary>
		/// Generate invoke code.
		/// </summary>
		/// <param name="instance"></param>
		/// <param name="functionName"></param>
		/// <param name="paramObject"></param>
		/// <returns></returns>
		public static string CGInvoke(this string instance, string functionName, Type[] genericType, params string[] paramObject) {
			return CG.Invoke(instance, functionName, genericType, paramObject);
		}

		/// <summary>
		/// Generate invoke code.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="functionName"></param>
		/// <param name="paramObject"></param>
		/// <returns></returns>
		public static string CGInvoke(this Type type, string functionName, params string[] paramObject) {
			return CG.Invoke(type, functionName, paramObject);
		}

		/// <summary>
		/// Generate invoke code.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="functionName"></param>
		/// <param name="paramObject"></param>
		/// <returns></returns>
		public static string CGInvoke(this Type type, string functionName, Type[] genericType, params string[] paramObject) {
			return CG.Invoke(type, functionName, genericType, paramObject);
		}
		#endregion

		#region Flow Invoke
		/// <summary>
		/// Generate invoke code.
		/// </summary>
		/// <param name="instance"></param>
		/// <param name="functionName"></param>
		/// <param name="parameters"></param>
		/// <returns></returns>
		public static string CGFlowInvoke(this string instance, string functionName, params string[] parameters) {
			return CG.FlowInvoke(instance, functionName, parameters);
		}

		/// <summary>
		/// Generate invoke code.
		/// </summary>
		/// <param name="instance"></param>
		/// <param name="functionName"></param>
		/// <param name="parameters"></param>
		/// <returns></returns>
		public static string CGFlowInvoke(this string instance, string functionName, Type[] genericType, params string[] parameters) {
			return CG.FlowInvoke(instance, functionName, genericType, parameters);
		}

		/// <summary>
		/// Generate invoke code.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="functionName"></param>
		/// <param name="parameters"></param>
		/// <returns></returns>
		public static string CGFlowInvoke(this Type type, string functionName, params string[] parameters) {
			return CG.FlowInvoke(type, functionName, parameters);
		}

		/// <summary>
		/// Generate invoke code.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="functionName"></param>
		/// <param name="parameters"></param>
		/// <returns></returns>
		public static string CGFlowInvoke(this Type type, string functionName, Type[] genericType, params string[] parameters) {
			return CG.FlowInvoke(type, functionName, genericType, parameters);
		}
		#endregion

		/// <summary>
		/// Generate type convert code.
		/// </summary>
		/// <param name="instance"></param>
		/// <param name="convertType"></param>
		/// <returns></returns>
		public static string CGConvert(this string instance, Type convertType, bool extension = false) {
			if(extension) {
				CG.RegisterUsingNamespace("MaxyGames.UNode");
				return CG.GenericInvoke(convertType, instance, nameof(Extensions.ConvertTo), null);
			}
			return CG.Type(convertType).Wrap() + instance;
		}

		/// <summary>
		/// Generate a new set code.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <param name="setType"></param>
		/// <returns></returns>
		public static string CGSet(this string left, string right, SetType setType = SetType.Change) {
			return CG.Set(left, right, setType);
		}

		/// <summary>
		/// Generate a new set code.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <param name="setType"></param>
		/// <returns></returns>
		public static string CGSet(this string left, string right, Type leftType, Type rightType, SetType setType = SetType.Change) {
			return CG.Set(left, right, leftType, rightType, setType);
		}

		/// <summary>
		/// Generate a new set code.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <param name="storeType"></param>
		/// <param name="setType"></param>
		/// <returns></returns>
		public static string CGSet(this string left, string right, Type storeType, SetType setType = SetType.Change) {
			return CG.Set(left, right, storeType, storeType, setType);
		}

		/// <summary>
		/// Generate a new set code.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <param name="setType"></param>
		/// <returns></returns>
		public static string CGSetValue(this string left, string right, SetType setType = SetType.Change) {
			return CG.SetValue(left, right, setType);
		}

		/// <summary>
		/// Generate arithmetic operation.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <param name="arithmeticType"></param>
		/// <returns></returns>
		public static string CGArithmetic(this string left, string right, ArithmeticType arithmeticType) {
			return CG.Arithmetic(left, right, arithmeticType);
		}

		/// <summary>
		/// Generate compare code.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <param name="compareType"></param>
		/// <returns></returns>
		public static string CGCompare(this string left, string right, ComparisonType compareType = ComparisonType.Equal) {
			return CG.Compare(left, right, compareType);
		}

		/// <summary>
		/// Generate + operation.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <param name="arithmeticType"></param>
		/// <returns></returns>
		public static string CGAdd(this string left, string right) {
			return CG.Arithmetic(left, right, ArithmeticType.Add);
		}

		/// <summary>
		/// Generate * operation.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <param name="arithmeticType"></param>
		/// <returns></returns>
		public static string CGMultiply(this string left, string right) {
			return CG.Arithmetic(left, right, ArithmeticType.Multiply);
		}

		/// <summary>
		/// Generate * operation.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <param name="arithmeticType"></param>
		/// <returns></returns>
		public static string CGSubtract(this string left, string right) {
			return CG.Arithmetic(left, right, ArithmeticType.Subtract);
		}

		/// <summary>
		/// Generate / operation.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <param name="arithmeticType"></param>
		/// <returns></returns>
		public static string CGDivide(this string left, string right) {
			return CG.Arithmetic(left, right, ArithmeticType.Divide);
		}

		/// <summary>
		/// Generate and code.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static string CGAnd(this string left, string right) {
			return CG.And(left, right);
		}

		/// <summary>
		/// Generate or code.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static string CGOr(this string left, string right) {
			return CG.Or(left, right);
		}

		/// <summary>
		/// Generate not code.
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		public static string CGNot(this string str) {
			if(string.IsNullOrEmpty(str)) {
				return str;
			}
			return "!" + str;
		}

		/// <summary>
		/// Generate not code.
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		public static string CGNot(this string str, bool wrapCode) {
			if(!wrapCode)
				return CGNot(str);
			if(string.IsNullOrEmpty(str)) {
				return str;
			}
			return "!(" + str + ")";
		}

		/// <summary>
		/// Generate negate code.
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		public static string CGNegate(this string str) {
			if(string.IsNullOrEmpty(str)) {
				return str;
			}
			return "-" + str;
		}

		/// <summary>
		/// Generate negate code.
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		public static string CGNegate(this string str, bool wrapCode) {
			if(!wrapCode)
				return CGNegate(str);
			if(string.IsNullOrEmpty(str)) {
				return str;
			}
			return "-(" + str + ")";
		}

		/// <summary>
		/// Wrap a string value with brackets "( code )"
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		public static string Wrap(this string str) {
			return CG.Wrap(str);
		}
	}
}