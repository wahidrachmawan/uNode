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
		#region Generate Set Code
		/// <summary>
		/// Function for get code for set.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <param name="leftType"></param>
		/// <param name="rightType"></param>
		/// <returns></returns>
		public static string Set(object left, object right, Type leftType, Type rightType = null) {
			return Set(left, right, SetType.Change, leftType, rightType);
		}

		/// <summary>
		/// Function for generate code for set a value.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <param name="setType"></param>
		/// <param name="leftType"></param>
		/// <param name="rightType"></param>
		/// <returns></returns>
		public static string Set(object left, object right, SetType setType = SetType.Change, Type leftType = null, Type rightType = null) {
			if(left == null || right == null)
				return null;
			object firstVal = left;
			string set;
			if(right is string) {
				set = right as string;
			} else if(right is char[]) {
				set = new string(right as char[]);
			} else {
				set = Value(right, autoConvert:true);
			}
			string result = null;
			MemberData source = left as MemberData;
			if(source == null && left is ValueInput valueInput) {
				if(valueInput.UseDefaultValue) {
					source = valueInput.defaultValue;
				} else {
					result = Value(valueInput, setVariable: true);
				}
			}
			if(source != null) {
				if(source.type != null && source.type.IsValueType) {
					MemberInfo[] memberInfo = source.GetMembers(false);
					// if(memberInfo[0] is RuntimeType && memberInfo[memberInfo.Length - 1] is IRuntimeMember){
					// 	throw null;
					// } else 
					if(memberInfo != null && memberInfo.Length > 1 && ReflectionUtils.GetMemberType(memberInfo[memberInfo.Length - 2]).IsValueType) {
						string varName = GenerateNewName("tempVar");
						string data = Type(ReflectionUtils.GetMemberType(memberInfo[memberInfo.Length - 2])) + " " + varName + " = ";
						var pVal = Value((object)source);
						var pVal2 = pVal.Remove(pVal.IndexOf(ParseStartValue(source, setVariable:true)), ParseStartValue(source).Length + 1);
						if(pVal.LastIndexOf(".") >= 0) {
							pVal = pVal.Remove(pVal.LastIndexOf("."));
						}
						data += pVal + ";\n";
						switch(setType) {
							case SetType.Subtract:
								data = data + varName + "." + pVal2.CGSplitMember().Last() + " -= " + set + ";";
								break;
							case SetType.Divide:
								data = data + varName + "." + pVal2.CGSplitMember().Last() + " /= " + set + ";";
								break;
							case SetType.Add:
								data = data + varName + "." + pVal2.CGSplitMember().Last() + " += " + set + ";";
								break;
							case SetType.Multiply:
								data = data + varName + "." + pVal2.CGSplitMember().Last() + " *= " + set + ";";
								break;
							case SetType.Modulo:
								data = data + varName + "." + pVal2.CGSplitMember().Last() + " %= " + set + ";";
								break;
							default:
								data = data + varName + "." + pVal2.CGSplitMember().Last() + " = " + set + ";";
								break;
						}
						if(leftType != null && !leftType.IsCastableTo(typeof(Delegate)) && !(leftType is RuntimeType)) {
							if(rightType == null || !rightType.IsCastableTo(leftType) && !rightType.IsValueType && rightType != typeof(string)) {
								if(leftType.IsValueType) {
									varName = varName.Insert(0, "(" + Type(leftType) + ")");
								} else if(set != "null") {
									varName = varName + " as " + Type(leftType);
								}
							}
						}
						return data + "\n" + pVal + " = " + varName + ";";
					} else {
						result = Value(left, setVariable:true);
					}
				} else {
					result = Value(left, setVariable:true);
					if(source.type is RuntimeGraphType && right is MemberData) {
						MemberData mVal = right as MemberData;
						if(mVal.type != source.type) {
							set = set.CGAccess(GenerateGetGeneratedComponent(null, source.type as RuntimeGraphType));
						}
					}
				}
			} else if(left is string) {
				result = left.ToString();
			}
			if(leftType != null && !leftType.IsCastableTo(typeof(Delegate)) && !(leftType is RuntimeType)) {
				if(rightType == null || !rightType.IsCastableTo(leftType) && !rightType.IsValueType && rightType != typeof(string)) {
					if(leftType.IsValueType) {
						set = set.Insert(0, "(" + Type(leftType) + ")");
					} else if(set != "null") {
						set = set + " as " + Type(leftType);
					}
				}
			}
			bool flag = !generatePureScript && result.EndsWith("\")");
			//if(includeGraphInformation && firstVal is ValueInput && !result.EndsWith("*/")) {
			//	var port = firstVal as ValueInput;
			//	var node = port.GetTargetNode();
			//	if(node != null) {
			//		result = WrapWithInformation(result, node);
			//	}
			//}
			if(flag) {
				var strs = result.CGSplitMember();
				var lastStr = strs[strs.Count - 1];
				string setCode = null;
				if(lastStr.StartsWith(nameof(IRuntimeClass.GetVariable) + "<", StringComparison.Ordinal)) {
					setCode = nameof(IRuntimeClass.SetVariable);
					if(set != "null" && leftType.IsCastableTo(typeof(Delegate))) {
						set = set.Wrap().Insert(0, "(" + Type(leftType) + ")");
					}
				} else if(lastStr.StartsWith(nameof(IRuntimeClass.GetProperty) + "<", StringComparison.Ordinal)) {
					setCode = nameof(IRuntimeClass.SetProperty);
					if(set != "null" && leftType.IsCastableTo(typeof(Delegate))) {
						set = set.Wrap().Insert(0, "(" + Type(leftType) + ")");
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
					{//for change the operator code
						int firstIndex = lastStr.IndexOf("\"");
						string vName = lastStr.Substring(firstIndex, lastStr.LastIndexOf("\"") - firstIndex + 1);
						if(code != '=') {
							strs[strs.Count - 1] = DoGenerateInvokeCode(setCode, new string[] { vName, set, code.CGValue() }).AddSemicolon();
						} else {
							strs[strs.Count - 1] = DoGenerateInvokeCode(setCode, new string[] { vName, set }).AddSemicolon();
						}
					}
					result = string.Join(".", strs);
					if(debugScript && setting.debugValueNode && firstVal is ValueInput) {
						return Debug(firstVal as ValueInput, result, true).AddSemicolon().AddLineInFirst();
					}
					return result;
				}
			}
			switch(setType) {
				case SetType.Subtract:
					result = result + " -= " + set + ";";
					break;
				case SetType.Divide:
					result = result + " /= " + set + ";";
					break;
				case SetType.Add:
					result = result + " += " + set + ";";
					break;
				case SetType.Multiply:
					result = result + " *= " + set + ";";
					break;
				case SetType.Modulo:
					result = result + " %= " + set + ";";
					break;
				default:
					result = result + " = " + set + ";";
					break;
			}
			if(debugScript && setting.debugValueNode && firstVal is ValueInput) {
				if(Utility.IsEvent(firstVal as ValueInput) == false) {
					return Debug(firstVal as ValueInput, result.RemoveLast(), true).AddSemicolon().AddLineInFirst();
				}
			}
			return result;
		}
		#endregion

        #region CompareCode
        /// <summary>
		/// Function for get code for comparing
		/// </summary>
		/// <param name="compareType"></param>
		/// <returns></returns>
		public static string GetCompareCode(ComparisonType compareType = ComparisonType.Equal) {
			switch(compareType) {
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
			return null;
		}

		/// <summary>
		/// Function for get code for compare 2 object
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <param name="compareType"></param>
		/// <returns></returns>
		public static string GetCompareCode(object left, object right, ComparisonType compareType = ComparisonType.Equal) {
			if(left == null && right == null)
				throw new System.Exception();
			if(left != null && left.GetType().IsValueType && right == null)
				return null;
			if(right != null && right.GetType().IsValueType && left == null)
				return null;
			if(left is MemberData) {
				left = Value(left);
			}
			string data2 = Value(right);
			if(right is string) {
				data2 = (string)right;
			}
			switch(compareType) {
				case ComparisonType.Equal:
					return left.ToString() + " == " + data2;
				case ComparisonType.GreaterThan:
					return left.ToString() + " > " + data2;
				case ComparisonType.GreaterThanOrEqual:
					return left.ToString() + " >= " + data2;
				case ComparisonType.LessThan:
					return left.ToString() + " < " + data2;
				case ComparisonType.LessThanOrEqual:
					return left.ToString() + " <= " + data2;
				case ComparisonType.NotEqual:
					return left.ToString() + " != " + data2;
			}
			throw new InvalidCastException();
		}
        #endregion
		
		#region Generate YieldReturn
		/// <summary>
		/// Get yield return value code.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static string GetYieldReturn(object value) {
			return "yield return " + Value(value) + ";";
		}
		#endregion

		#region String Wrap
		/// <summary>
		/// Wrap a string value so the string will be generated without quotes.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static StringWrapper WrapString(string value) {
			return new StringWrapper(value);
		}

		/// <summary>
		/// Wrap a string value with brackets "( code )"
		/// </summary>
		/// <param name="code"></param>
		/// <param name="onlyOnContainSpace"></param>
		/// <returns></returns>
		public static string Wrap(string code, bool onlyOnContainSpace = false) {
			if(string.IsNullOrEmpty(code)) {
				return code;
			}
			if(onlyOnContainSpace && !code.Contains(' ')) {
				return code;
			}
			return "(" + code + ")";
		}

		/// <summary>
		/// Wrap a string value with brackets "{ <paramref name="code"/> }"
		/// </summary>
		/// <param name="code"></param>
		/// <param name="onlyOnContainSpace"></param>
		/// <returns></returns>
		public static string WrapBraces(string code, bool onlyOnContainSpace = false) {
			if(string.IsNullOrEmpty(code)) {
				return code;
			}
			if(onlyOnContainSpace && !code.Contains(' ')) {
				return code;
			}
			return "{ " + code + " }";
		}
		#endregion
	}
}
