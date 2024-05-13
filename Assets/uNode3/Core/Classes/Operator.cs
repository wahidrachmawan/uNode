using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using UnityEngine;

namespace MaxyGames.UNode {
	public static class Operator {
		#region Const
		private const string OP_Equality = "op_Equality";
		private const string OP_Inequality = "op_Inequality";
		#endregion

		#region Validation
		public static bool IsValidEquality(Type leftType, Type rightType) {
			if(leftType == rightType)
				return true;
			if(leftType == typeof(object))
				return true;
			if(rightType == typeof(object))
				return true;
			if(leftType.IsPrimitive && rightType.IsPrimitive || leftType is IRuntimeMember || rightType is IRuntimeMember) {
				if(rightType.IsCastableTo(leftType)) {
					return true;
				}
				else if(leftType.IsCastableTo(rightType)) {
					return true;
				}
			}
			else {
				return ReflectionUtils.GetUserDefinedOperator(leftType, rightType, OP_Equality) != null;
			}
			return false;
		}

		public static bool IsValidInequality(Type leftType, Type rightType) {
			if(leftType == rightType)
				return true;
			if(leftType == typeof(object))
				return true;
			if(rightType == typeof(object))
				return true;
			if(leftType.IsPrimitive && rightType.IsPrimitive || leftType is IRuntimeMember || rightType is IRuntimeMember) {
				if(rightType.IsCastableTo(leftType)) {
					return true;
				}
				else if(leftType.IsCastableTo(rightType)) {
					return true;
				}
			}
			else {
				return ReflectionUtils.GetUserDefinedOperator(leftType, rightType, OP_Inequality) != null;
			}
			return false;
		}
		#endregion

		#region Arithmetic
		public static object Add(object a, object b) {
			if(a == null) {
				throw new ArgumentNullException("a");
			}
			if(b == null) {
				throw new ArgumentNullException("b");
			}
			if(a is Delegate && b is Delegate) {
				Delegate source = a as Delegate;
				Delegate value = b as Delegate;
				if(value.GetType() != source.GetType()) {
					value = Delegate.CreateDelegate(a.GetType(), value.Target, value.Method);
				}
				return Delegate.Combine(source, value);
			}
			return Operators.Add(a, b, a.GetType(), b.GetType());
		}

		public static object Add(object a, object b, Type aType, Type bType) {
			if((a == null || a is Delegate) && b is Delegate) {
				Delegate source = a as Delegate;
				Delegate value = b as Delegate;
				if(source == null) {
					return value;
				}
				if(aType != bType) {
					value = Delegate.CreateDelegate(a.GetType(), value.Target, value.Method);
				}
				return Delegate.Combine(source, value);
			}
			return Operators.Add(a, b, aType, bType);
		}

		public static object Modulo(object a, object b) {
			return Operators.Modulo(a, b, a.GetType(), b.GetType());
		}

		public static object Modulo(object a, object b, Type aType, Type bType) {
			return Operators.Modulo(a, b, aType, bType);
		}

		public static object Subtract(object a, object b) {
			if(a is Delegate && b is Delegate) {
				Delegate source = a as Delegate;
				Delegate value = b as Delegate;
				if(value.GetType() != source.GetType()) {
					value = Delegate.CreateDelegate(a.GetType(), value.Target, value.Method);
				}
				return Delegate.Remove(source, value);
			}
			return Operators.Subtract(a, b, a.GetType(), b.GetType());
		}

		public static object Subtract(object a, object b, Type aType, Type bType) {
			if(a is Delegate && b is Delegate) {
				Delegate source = a as Delegate;
				Delegate value = b as Delegate;
				if(value.GetType() != source.GetType()) {
					value = Delegate.CreateDelegate(a.GetType(), value.Target, value.Method);
				}
				return Delegate.Remove(source, value);
			}
			return Operators.Subtract(a, b, aType, bType);
		}

		public static object Divide(object a, object b) {
			return Operators.Divide(a, b, a.GetType(), b.GetType());
		}

		public static object Divide(object a, object b, Type aType, Type bType) {
			return Operators.Divide(a, b, aType, bType);
		}

		public static object Multiply(object a, object b) {
			return Operators.Multiply(a, b, a.GetType(), b.GetType());
		}

		public static object Multiply(object a, object b, Type aType, Type bType) {
			return Operators.Multiply(a, b, aType, bType);
		}
		#endregion

		#region Comparison
		public static bool Equal(object a, object b) {
			return Operators.Equal(a, b, a != null ? a.GetType() : typeof(object), b != null ? b.GetType() : typeof(object));
		}

		public static bool Equal(object a, object b, Type aType, Type bType) {
			return Operators.Equal(a, b, aType, bType);
		}

		public static bool EqualAlternatif(object a, object b) {
			return Operators.Equal(a, b, a.GetType(), b.GetType());
		}

		public static bool NotEqual(object a, object b) {
			return Operators.NotEqual(a, b, a != null ? a.GetType() : typeof(object), b != null ? b.GetType() : typeof(object));
		}

		public static bool NotEqual(object a, object b, Type aType, Type bType) {
			return Operators.NotEqual(a, b, aType, bType);
		}

		public static bool NotEqualAlternatif(object a, object b) {
			return Operators.NotEqual(a, b, a.GetType(), b.GetType());
		}

		public static bool GreaterThan(object a, object b) {
			return Operators.GreaterThan(a, b, a.GetType(), b.GetType());
		}

		public static bool GreaterThan(object a, object b, Type aType, Type bType) {
			return Operators.GreaterThan(a, b, aType, bType);
		}

		public static bool GreaterThanAlternatif(object a, object b) {
			return Operators.GreaterThan(a, b, a.GetType(), b.GetType());
		}

		public static bool LessThan(object a, object b) {
			return Operators.LessThan(a, b, a.GetType(), b.GetType());
		}

		public static bool LessThan(object a, object b, Type aType, Type bType) {
			return Operators.LessThan(a, b, aType, bType);
		}

		public static bool LessThanAlternatif(object a, object b) {
			return Operators.LessThan(a, b, a.GetType(), b.GetType());
		}

		public static bool GreaterThanOrEqual(object a, object b) {
			return Operators.GreaterThanOrEqual(a, b, a.GetType(), b.GetType());
		}

		public static bool GreaterThanOrEqual(object a, object b, Type aType, Type bType) {
			return Operators.GreaterThanOrEqual(a, b, aType, bType);
		}

		public static bool GreaterThanOrEqualAlternatif(object a, object b) {
			return Operators.GreaterThanOrEqual(a, b, a.GetType(), b.GetType());
		}

		public static bool LessThanOrEqual(object a, object b) {
			return Operators.LessThanOrEqual(a, b, a.GetType(), b.GetType());
		}

		public static bool LessThanOrEqual(object a, object b, Type aType, Type bType) {
			return Operators.LessThanOrEqual(a, b, aType, bType);
		}

		public static bool LessThanOrEqualAlternatif(object a, object b) {
			return Operators.LessThanOrEqual(a, b, a.GetType(), b.GetType());
		}
		#endregion

		#region Unary
		public static T Convert<T>(object a) {
			if(a is T) {
				return (T)a;
			}
			else {
				return (T)Convert(a, typeof(T));
			}
		}

		public static object Convert(object value, Type type) {
			if(type is RuntimeType) {
				if(type.IsInstanceOfType(value)) {
					return value;
				}
				if(value is GameObject gameObject) {
					return gameObject.GetGeneratedComponent(type as RuntimeType);
				}
				else if(value is Component component) {
					return component.GetGeneratedComponent(type as RuntimeType);
				}
				//else if(type.IsInterface) {
				//	return value;
				//}
				else if(!type.IsInstanceOfType(value) && !(value is RuntimeType)) {
					throw new Exception($"Cannot convert '{value.GetType().FullName}' to {type.FullName}");
				}
				// else if(value is ScriptableObject scriptableObject) {

				// }
				return value;
			}
			return Operators.Convert(value, type);
		}

		public static object Negate(object a) {
			return Operators.Negate(a, a.GetType());
		}

		public static object Negate(object a, Type type) {
			return Operators.Negate(a, type);
		}

		public static object Not(object a) {
			return Operators.Not(a, a.GetType());
		}

		public static object Not(object a, Type type) {
			return Operators.Not(a, type);
		}
		#endregion

		#region Others
		public static object Default<T>() {
			return default(T);
		}

		public static object Default(Type type) {
			if(type.IsValueType) {
				return Activator.CreateInstance(type);
			}
			return null;
		}

		public static bool TypeIs<T>(object value) {
			return value is T;
		}

		public static bool TypeIs(object value, Type type) {
			if(type is RuntimeType) {
				return type.IsInstanceOfType(value);
			}
			return Operators.TypeIs(value, type);
		}

		public static T TypeAs<T>(object value) where T : class {
			return value as T;
		}

		public static object TypeAs(object value, Type type) {
			if(type is RuntimeType) {
				return Convert(value, type);
			}
			return Operators.TypeAs(value, type);
		}

		public static object Increment(object value) {
			return Operators.Increment(value, value.GetType());
		}

		public static object Increment(object value, Type type) {
			return Operators.Increment(value, type);
		}

		public static object IncrementPrimitive(object value) {
			if(value == null)
				return null;
			Type type = value.GetType();
			if(type == typeof(byte)) {
				byte o = (byte)value;
				value = ++o;
			}
			else if(type == typeof(sbyte)) {
				sbyte o = (sbyte)value;
				value = ++o;
			}
			else if(type == typeof(char)) {
				char o = (char)value;
				value = ++o;
			}
			else if(type == typeof(short)) {
				short o = (short)value;
				value = ++o;
			}
			else if(type == typeof(ushort)) {
				ushort o = (ushort)value;
				value = ++o;
			}
			else if(type == typeof(int)) {
				int o = (int)value;
				value = ++o;
			}
			else if(type == typeof(uint)) {
				uint o = (uint)value;
				value = ++o;
			}
			else if(type == typeof(long)) {
				long o = (long)value;
				value = ++o;
			}
			else if(type == typeof(ulong)) {
				ulong o = (ulong)value;
				value = ++o;
			}
			else if(type == typeof(float)) {
				float o = (float)value;
				value = ++o;
			}
			else if(type == typeof(double)) {
				double o = (double)value;
				value = ++o;
			}
			else if(type == typeof(decimal) || type.IsCastableTo(typeof(decimal))) {
				decimal o = (decimal)value;
				value = ++o;
			}
			return value;
		}

		public static object Decrement(object value) {
			return Operators.Decrement(value, value.GetType());
		}

		public static object Decrement(object value, Type type) {
			return Operators.Decrement(value, type);
		}

		public static object DecrementPrimitive(object value) {
			if(value == null)
				return null;
			Type type = value.GetType();
			if(type == typeof(byte)) {
				byte o = (byte)value;
				value = --o;
			}
			else if(type == typeof(sbyte)) {
				sbyte o = (sbyte)value;
				value = --o;
			}
			else if(type == typeof(char)) {
				char o = (char)value;
				value = --o;
			}
			else if(type == typeof(short)) {
				short o = (short)value;
				value = --o;
			}
			else if(type == typeof(ushort)) {
				ushort o = (ushort)value;
				value = --o;
			}
			else if(type == typeof(int)) {
				int o = (int)value;
				value = --o;
			}
			else if(type == typeof(uint)) {
				uint o = (uint)value;
				value = --o;
			}
			else if(type == typeof(long)) {
				long o = (long)value;
				value = --o;
			}
			else if(type == typeof(ulong)) {
				ulong o = (ulong)value;
				value = --o;
			}
			else if(type == typeof(float)) {
				float o = (float)value;
				value = --o;
			}
			else if(type == typeof(double)) {
				double o = (double)value;
				value = --o;
			}
			else if(type == typeof(decimal) || type.IsCastableTo(typeof(decimal))) {
				decimal o = (decimal)value;
				value = --o;
			}
			return value;
		}
		#endregion

		//#region Extensions
		//public static T As<T>(this object obj) where T : class {
		//	if(object.ReferenceEquals(obj, null)) {
		//		return default;
		//	}
		//	return obj as T;
		//}

		//public static T To<T>(this object obj) {
		//	if(object.ReferenceEquals(obj, null)) {
		//		return default;
		//	}
		//	return (T)obj;
		//}
		//#endregion
	}

	public static class Operators {

		#region Arithmetic
		static Func<object, object, bool> equal, notEqual, greaterThan, lessThan, greaterThanOrEqual, lessThanOrEqual;

		static Dictionary<int, Func<object, object, object>> _ListAdd;
		public static object Add(object a, object b, Type fType, Type tType) {
			if(a is string || b is string) {
				return string.Concat(a, b);
			}
			if(_ListAdd == null) {
				_ListAdd = new Dictionary<int, Func<object, object, object>>();
			}
			if(fType != tType) {
				if(fType.IsPrimitive && tType.IsPrimitive) {
					if(tType.IsCastableTo(fType)) {
						b = System.Convert.ChangeType(b, fType);
						tType = fType;
					}
					else if(fType.IsCastableTo(tType)) {
						a = System.Convert.ChangeType(a, tType);
						fType = tType;
					}
				}
			}
			Func<object, object, object> func;
			var uid = uNodeUtility.GetHashCode(fType.GetHashCode(), tType.GetHashCode());
			if(!_ListAdd.ContainsKey(uid)) {
				try {
					ParameterExpression paramA = Expression.Parameter(typeof(object), "a"),
						paramB = Expression.Parameter(typeof(object), "b");
					func = Expression.Lambda<Func<object, object, object>>(
						Expression.Convert(
							Expression.Add(
								Expression.Convert(paramA, fType),
								Expression.Convert(paramB, tType)),
							typeof(object)),
						paramA,
						paramB).Compile();
				}
				catch {
					if(fType.IsPrimitive && tType.IsPrimitive) {
						if(fType == typeof(byte)) {
							func = (x, y) => {
								return (byte)x + System.Convert.ToByte(y);
							};
						}
						else if(fType == typeof(sbyte)) {
							func = (x, y) => {
								return (sbyte)x + System.Convert.ToSByte(y);
							};
						}
						else if(fType == typeof(char)) {
							func = (x, y) => {
								return (char)x + System.Convert.ToChar(y);
							};
						}
						else if(fType == typeof(short)) {
							func = (x, y) => {
								return (short)x + (short)System.Convert.ChangeType(y, fType);
							};
						}
						else if(fType == typeof(ushort)) {
							func = (x, y) => {
								return (ushort)x + (ushort)System.Convert.ChangeType(y, fType);
							};
						}
						else if(fType == typeof(int)) {
							func = (x, y) => {
								return (int)x + (int)System.Convert.ChangeType(y, fType);
							};
						}
						else if(fType == typeof(uint)) {
							func = (x, y) => {
								return (uint)x + (uint)System.Convert.ChangeType(y, fType);
							};
						}
						else if(fType == typeof(long)) {
							func = (x, y) => {
								return (long)x + (long)System.Convert.ChangeType(y, fType);
							};
						}
						else if(fType == typeof(ulong)) {
							func = (x, y) => {
								return (ulong)x + (ulong)System.Convert.ChangeType(y, fType);
							};
						}
						else if(fType == typeof(float)) {
							func = (x, y) => {
								return (float)x + (float)System.Convert.ChangeType(y, fType);
							};
						}
						else if(fType == typeof(double)) {
							func = (x, y) => {
								return (double)x + (double)System.Convert.ChangeType(y, fType);
							};
						}
						else {
							throw;
						}
					}
					else {
						var paramTypes = new[] { fType, tType };
						var method = fType.GetMethod("op_Addition", BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic, null, paramTypes, null);
						if(method == null && fType != tType) {
							method = tType.GetMethod("op_Addition", BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic, null, paramTypes, null);
							if(method == null) {
								method = FindMethod(fType, "op_Addition", paramTypes);
								if(method == null) {
									method = FindMethod(tType, "op_Addition", paramTypes);
								}
							}
						}
						if(method != null) {
							bool sameParamType = true;
							var mParamType = method.GetParameters();
							for(int i = 0; i < mParamType.Length; i++) {
								if(mParamType[i].ParameterType != paramTypes[i]) {
									sameParamType = false;
									paramTypes[i] = mParamType[i].ParameterType;
								}
							}
							if(sameParamType) {
								func = (x, y) => {
									return method.InvokeOptimized(null, new[] { x, y });
								};
							}
							else {
								func = (x, y) => {
									return method.InvokeOptimized(null, new[] { Convert(x, paramTypes[0]), Convert(y, paramTypes[1]) });
								};
							}
						}
						else {
							throw;
						}
					}
				}
				_ListAdd.Add(uid, func);
			}
			else {
				func = _ListAdd[uid];
			}
			return func(a, b);
		}

		static Dictionary<int, Func<object, object, object>> _ListModulo;
		public static object Modulo(object a, object b, Type fType, Type tType) {
			if(fType != tType) {
				if(fType.IsPrimitive && tType.IsPrimitive) {
					if(tType.IsCastableTo(fType)) {
						b = System.Convert.ChangeType(b, fType);
						tType = fType;
					}
					else if(fType.IsCastableTo(tType)) {
						a = System.Convert.ChangeType(a, tType);
						fType = tType;
					}
				}
			}
			if(_ListModulo == null) {
				_ListModulo = new Dictionary<int, System.Func<object, object, object>>();
			}
			System.Func<object, object, object> func;
			var uid = uNodeUtility.GetHashCode(fType.GetHashCode(), tType.GetHashCode());
			if(!_ListModulo.ContainsKey(uid)) {
				try {
					ParameterExpression paramA = Expression.Parameter(typeof(object), "a"),
						paramB = Expression.Parameter(typeof(object), "b");
					func = Expression.Lambda<System.Func<object, object, object>>(Expression.Convert(Expression.Modulo(Expression.Convert(paramA, fType), Expression.Convert(paramB, tType)), typeof(object)), paramA, paramB).Compile();
				}
				catch {
					if(fType.IsPrimitive && tType.IsPrimitive) {
						if(fType == typeof(byte)) {
							func = (x, y) => {
								return (byte)x % System.Convert.ToByte(y);
							};
						}
						else if(fType == typeof(sbyte)) {
							func = (x, y) => {
								return (sbyte)x % System.Convert.ToSByte(y);
							};
						}
						else if(fType == typeof(char)) {
							func = (x, y) => {
								return (char)x % System.Convert.ToChar(y);
							};
						}
						else if(fType == typeof(short)) {
							func = (x, y) => {
								return (short)x % (short)System.Convert.ChangeType(y, fType);
							};
						}
						else if(fType == typeof(ushort)) {
							func = (x, y) => {
								return (ushort)x % (ushort)System.Convert.ChangeType(y, fType);
							};
						}
						else if(fType == typeof(int)) {
							func = (x, y) => {
								return (int)x % (int)System.Convert.ChangeType(y, fType);
							};
						}
						else if(fType == typeof(uint)) {
							func = (x, y) => {
								return (uint)x % (uint)System.Convert.ChangeType(y, fType);
							};
						}
						else if(fType == typeof(long)) {
							func = (x, y) => {
								return (long)x % (long)System.Convert.ChangeType(y, fType);
							};
						}
						else if(fType == typeof(ulong)) {
							func = (x, y) => {
								return (ulong)x % (ulong)System.Convert.ChangeType(y, fType);
							};
						}
						else if(fType == typeof(float)) {
							func = (x, y) => {
								return (float)x % (float)System.Convert.ChangeType(y, fType);
							};
						}
						else if(fType == typeof(double)) {
							func = (x, y) => {
								return (double)x % (double)System.Convert.ChangeType(y, fType);
							};
						}
						else {
							throw;
						}
					}
					else {
						var paramTypes = new[] { fType, tType };
						var method = fType.GetMethod("op_Modulus", BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic, null, paramTypes, null);
						if(method == null && fType != tType) {
							method = tType.GetMethod("op_Modulus", BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic, null, paramTypes, null);
							if(method == null) {
								method = FindMethod(fType, "op_Modulus", paramTypes);
								if(method == null) {
									method = FindMethod(tType, "op_Modulus", paramTypes);
								}
							}
						}
						if(method != null) {
							bool sameParamType = true;
							var mParamType = method.GetParameters();
							for(int i = 0; i < mParamType.Length; i++) {
								if(mParamType[i].ParameterType != paramTypes[i]) {
									sameParamType = false;
									paramTypes[i] = mParamType[i].ParameterType;
								}
							}
							if(sameParamType) {
								func = (x, y) => {
									return method.InvokeOptimized(null, new[] { x, y });
								};
							}
							else {
								func = (x, y) => {
									return method.InvokeOptimized(null, new[] { Convert(x, paramTypes[0]), Convert(y, paramTypes[1]) });
								};
							}
						}
						else {
							throw;
						}
					}
				}
				_ListModulo.Add(uid, func);
			}
			else {
				func = _ListModulo[uid];
			}
			return func(a, b);
		}

		static Dictionary<int, Func<object, object, object>> _ListSubtract;
		public static object Subtract(object a, object b, Type fType, Type tType) {
			if(fType != tType) {
				if(fType.IsPrimitive && tType.IsPrimitive) {
					if(tType.IsCastableTo(fType)) {
						b = System.Convert.ChangeType(b, fType);
						tType = fType;
					}
					else if(fType.IsCastableTo(tType)) {
						a = System.Convert.ChangeType(a, tType);
						fType = tType;
					}
				}
			}
			if(_ListSubtract == null) {
				_ListSubtract = new Dictionary<int, System.Func<object, object, object>>();
			}
			System.Func<object, object, object> func;
			var uid = uNodeUtility.GetHashCode(fType.GetHashCode(), tType.GetHashCode());
			if(!_ListSubtract.ContainsKey(uid)) {
				try {
					ParameterExpression paramA = Expression.Parameter(typeof(object), "a"),
						paramB = Expression.Parameter(typeof(object), "b");
					func = Expression.Lambda<System.Func<object, object, object>>(Expression.Convert(Expression.Subtract(Expression.Convert(paramA, fType), Expression.Convert(paramB, tType)), typeof(object)), paramA, paramB).Compile();
				}
				catch {
					if(fType.IsPrimitive && tType.IsPrimitive) {
						if(fType == typeof(byte)) {
							func = (x, y) => {
								return (byte)x - System.Convert.ToByte(y);
							};
						}
						else if(fType == typeof(sbyte)) {
							func = (x, y) => {
								return (sbyte)x - System.Convert.ToSByte(y);
							};
						}
						else if(fType == typeof(char)) {
							func = (x, y) => {
								return (char)x - System.Convert.ToChar(y);
							};
						}
						else if(fType == typeof(short)) {
							func = (x, y) => {
								return (short)x - (short)System.Convert.ChangeType(y, fType);
							};
						}
						else if(fType == typeof(ushort)) {
							func = (x, y) => {
								return (ushort)x - (ushort)System.Convert.ChangeType(y, fType);
							};
						}
						else if(fType == typeof(int)) {
							func = (x, y) => {
								return (int)x - (int)System.Convert.ChangeType(y, fType);
							};
						}
						else if(fType == typeof(uint)) {
							func = (x, y) => {
								return (uint)x - (uint)System.Convert.ChangeType(y, fType);
							};
						}
						else if(fType == typeof(long)) {
							func = (x, y) => {
								return (long)x - (long)System.Convert.ChangeType(y, fType);
							};
						}
						else if(fType == typeof(ulong)) {
							func = (x, y) => {
								return (ulong)x - (ulong)System.Convert.ChangeType(y, fType);
							};
						}
						else if(fType == typeof(float)) {
							func = (x, y) => {
								return (float)x - (float)System.Convert.ChangeType(y, fType);
							};
						}
						else if(fType == typeof(double)) {
							func = (x, y) => {
								return (double)x - (double)System.Convert.ChangeType(y, fType);
							};
						}
						else {
							throw;
						}
					}
					else {
						var paramTypes = new[] { fType, tType };
						var method = fType.GetMethod("op_Subtraction", BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic, null, paramTypes, null);
						if(method == null && fType != tType) {
							method = tType.GetMethod("op_Subtraction", BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic, null, paramTypes, null);
							if(method == null) {
								method = FindMethod(fType, "op_Subtraction", paramTypes);
								if(method == null) {
									method = FindMethod(tType, "op_Subtraction", paramTypes);
								}
							}
						}
						if(method != null) {
							bool sameParamType = true;
							var mParamType = method.GetParameters();
							for(int i = 0; i < mParamType.Length; i++) {
								if(mParamType[i].ParameterType != paramTypes[i]) {
									sameParamType = false;
									paramTypes[i] = mParamType[i].ParameterType;
								}
							}
							if(sameParamType) {
								func = (x, y) => {
									return method.InvokeOptimized(null, new[] { x, y });
								};
							}
							else {
								func = (x, y) => {
									return method.InvokeOptimized(null, new[] { Convert(x, paramTypes[0]), Convert(y, paramTypes[1]) });
								};
							}
						}
						else {
							throw;
						}
					}
				}
				_ListSubtract.Add(uid, func);
			}
			else {
				func = _ListSubtract[uid];
			}
			return func(a, b);
		}

		static Dictionary<int, Func<object, object, object>> _ListDivide;
		public static object Divide(object a, object b, Type fType, Type tType) {
			if(fType != tType) {
				if(fType.IsPrimitive && tType.IsPrimitive) {
					if(tType.IsCastableTo(fType)) {
						b = System.Convert.ChangeType(b, fType);
						tType = fType;
					}
					else if(fType.IsCastableTo(tType)) {
						a = System.Convert.ChangeType(a, tType);
						fType = tType;
					}
				}
			}
			if(_ListDivide == null) {
				_ListDivide = new Dictionary<int, System.Func<object, object, object>>();
			}
			System.Func<object, object, object> func;
			var uid = uNodeUtility.GetHashCode(fType.GetHashCode(), tType.GetHashCode());
			if(!_ListDivide.ContainsKey(uid)) {
				try {
					ParameterExpression paramA = Expression.Parameter(typeof(object), "a"),
						paramB = Expression.Parameter(typeof(object), "b");
					func = Expression.Lambda<System.Func<object, object, object>>(Expression.Convert(Expression.Divide(Expression.Convert(paramA, fType), Expression.Convert(paramB, tType)), typeof(object)), paramA, paramB).Compile();
				}
				catch {
					if(fType.IsPrimitive && tType.IsPrimitive) {
						if(fType == typeof(byte)) {
							func = (x, y) => {
								return (byte)x / System.Convert.ToByte(y);
							};
						}
						else if(fType == typeof(sbyte)) {
							func = (x, y) => {
								return (sbyte)x / System.Convert.ToSByte(y);
							};
						}
						else if(fType == typeof(char)) {
							func = (x, y) => {
								return (char)x / System.Convert.ToChar(y);
							};
						}
						else if(fType == typeof(short)) {
							func = (x, y) => {
								return (short)x / (short)System.Convert.ChangeType(y, fType);
							};
						}
						else if(fType == typeof(ushort)) {
							func = (x, y) => {
								return (ushort)x / (ushort)System.Convert.ChangeType(y, fType);
							};
						}
						else if(fType == typeof(int)) {
							func = (x, y) => {
								return (int)x / (int)System.Convert.ChangeType(y, fType);
							};
						}
						else if(fType == typeof(uint)) {
							func = (x, y) => {
								return (uint)x / (uint)System.Convert.ChangeType(y, fType);
							};
						}
						else if(fType == typeof(long)) {
							func = (x, y) => {
								return (long)x / (long)System.Convert.ChangeType(y, fType);
							};
						}
						else if(fType == typeof(ulong)) {
							func = (x, y) => {
								return (ulong)x / (ulong)System.Convert.ChangeType(y, fType);
							};
						}
						else if(fType == typeof(float)) {
							func = (x, y) => {
								return (float)x / (float)System.Convert.ChangeType(y, fType);
							};
						}
						else if(fType == typeof(double)) {
							func = (x, y) => {
								return (double)x / (double)System.Convert.ChangeType(y, fType);
							};
						}
						else {
							throw;
						}
					}
					else {
						var paramTypes = new[] { fType, tType };
						var method = fType.GetMethod("op_Division", BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic, null, paramTypes, null);
						if(method == null && fType != tType) {
							method = tType.GetMethod("op_Division", BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic, null, paramTypes, null);
							if(method == null) {
								method = FindMethod(fType, "op_Division", paramTypes);
								if(method == null) {
									method = FindMethod(tType, "op_Division", paramTypes);
								}
							}
						}
						if(method != null) {
							bool sameParamType = true;
							var mParamType = method.GetParameters();
							for(int i = 0; i < mParamType.Length; i++) {
								if(mParamType[i].ParameterType != paramTypes[i]) {
									sameParamType = false;
									paramTypes[i] = mParamType[i].ParameterType;
								}
							}
							if(sameParamType) {
								func = (x, y) => {
									return method.InvokeOptimized(null, new[] { x, y });
								};
							}
							else {
								func = (x, y) => {
									return method.InvokeOptimized(null, new[] { Convert(x, paramTypes[0]), Convert(y, paramTypes[1]) });
								};
							}
						}
						else {
							throw;
						}
					}
				}
				_ListDivide.Add(uid, func);
			}
			else {
				func = _ListDivide[uid];
			}
			return func(a, b);
		}

		static Dictionary<int, Func<object, object, object>> _ListMultiply;
		public static object Multiply(object a, object b, Type fType, Type tType) {
			if(fType != tType) {
				if(fType.IsPrimitive && tType.IsPrimitive) {
					if(tType.IsCastableTo(fType)) {
						b = System.Convert.ChangeType(b, fType);
						tType = fType;
					}
					else if(fType.IsCastableTo(tType)) {
						a = System.Convert.ChangeType(a, tType);
						fType = tType;
					}
				}
			}
			if(_ListMultiply == null) {
				_ListMultiply = new Dictionary<int, Func<object, object, object>>();
			}
			System.Func<object, object, object> func;
			var uid = uNodeUtility.GetHashCode(fType.GetHashCode(), tType.GetHashCode());
			if(!_ListMultiply.ContainsKey(uid)) {
				try {
					ParameterExpression paramA = Expression.Parameter(typeof(object), "a"),
						paramB = Expression.Parameter(typeof(object), "b");
					func = Expression.Lambda<System.Func<object, object, object>>(
						Expression.Convert(
							Expression.Multiply(
								Expression.Convert(paramA, fType),
								Expression.Convert(paramB, tType)),
							typeof(object)), paramA, paramB).Compile();
				}
				catch {
					if(fType.IsPrimitive && tType.IsPrimitive) {
						if(fType == typeof(byte)) {
							func = (x, y) => {
								return (byte)x * System.Convert.ToByte(y);
							};
						}
						else if(fType == typeof(sbyte)) {
							func = (x, y) => {
								return (sbyte)x * System.Convert.ToSByte(y);
							};
						}
						else if(fType == typeof(char)) {
							func = (x, y) => {
								return (char)x * System.Convert.ToChar(y);
							};
						}
						else if(fType == typeof(short)) {
							func = (x, y) => {
								return (short)x * (short)System.Convert.ChangeType(y, fType);
							};
						}
						else if(fType == typeof(ushort)) {
							func = (x, y) => {
								return (ushort)x * (ushort)System.Convert.ChangeType(y, fType);
							};
						}
						else if(fType == typeof(int)) {
							func = (x, y) => {
								return (int)x * (int)System.Convert.ChangeType(y, fType);
							};
						}
						else if(fType == typeof(uint)) {
							func = (x, y) => {
								return (uint)x * (uint)System.Convert.ChangeType(y, fType);
							};
						}
						else if(fType == typeof(long)) {
							func = (x, y) => {
								return (long)x * (long)System.Convert.ChangeType(y, fType);
							};
						}
						else if(fType == typeof(ulong)) {
							func = (x, y) => {
								return (ulong)x * (ulong)System.Convert.ChangeType(y, fType);
							};
						}
						else if(fType == typeof(float)) {
							func = (x, y) => {
								return (float)x * (float)System.Convert.ChangeType(y, fType);
							};
						}
						else if(fType == typeof(double)) {
							func = (x, y) => {
								return (double)x * (double)System.Convert.ChangeType(y, fType);
							};
						}
						else {
							throw;
						}
					}
					else {
						var paramTypes = new[] { fType, tType };
						var method = fType.GetMethod("op_Multiply", BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic, null, paramTypes, null);
						if(method == null && fType != tType) {
							method = tType.GetMethod("op_Multiply", BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic, null, paramTypes, null);
							if(method == null) {
								method = FindMethod(fType, "op_Multiply", paramTypes);
								if(method == null) {
									method = FindMethod(tType, "op_Multiply", paramTypes);
								}
							}
						}
						if(method != null) {
							bool sameParamType = true;
							var mParamType = method.GetParameters();
							for(int i = 0; i < mParamType.Length; i++) {
								if(mParamType[i].ParameterType != paramTypes[i]) {
									sameParamType = false;
									paramTypes[i] = mParamType[i].ParameterType;
								}
							}
							if(sameParamType) {
								func = (x, y) => {
									return method.InvokeOptimized(null, new[] { x, y });
								};
							}
							else {
								func = (x, y) => {
									return method.InvokeOptimized(null, new[] { Convert(x, paramTypes[0]), Convert(y, paramTypes[1]) });
								};
							}
						}
						else {
							throw;
						}
					}
				}
				_ListMultiply.Add(uid, func);
			}
			else {
				func = _ListMultiply[uid];
			}
			return func(a, b);
		}

		static MethodInfo FindMethod(Type type, string name, Type[] paramTypes) {
			var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static);
			for(int i = 0; i < methods.Length; i++) {
				var m = methods[i];
				if(m.Name == name) {
					var param = m.GetParameters();
					if(param.Length == paramTypes.Length) {
						bool flag = true;
						for(int x = 0; x < param.Length; x++) {
							if(!paramTypes[x].IsCastableTo(param[x].ParameterType)) {
								flag = false;
								break;
							}
						}
						if(flag) {
							return m;
						}
					}
				}
			}
			return null;
		}
		#endregion

		#region Comparison
		static Dictionary<Type, System.Func<object, object, bool>> listEqual, listNotEqual, listGreaterThan, listLessThan, listGreaterThanOrEqual, listLessThanOrEqual;
		static Dictionary<int, System.Func<object, object, bool>> _listEqual, _listNotEqual, _listGreaterThan, _listLessThan, _listGreaterThanOrEqual, _listLessThanOrEqual;


		public static bool Equal(object a, object b, Type aType, Type bType) {
			if(aType != bType) {
				if(aType.IsPrimitive && bType.IsPrimitive) {
					if(bType.IsCastableTo(aType)) {
						b = System.Convert.ChangeType(b, aType);
						//bType = aType;
					}
					else if(aType.IsCastableTo(bType)) {
						a = System.Convert.ChangeType(a, bType);
						//aType = bType;
					}
				}
			}
			//if(a != null) {
			//	return a.Equals(b);
			//}
			//else if(b != null) {
			//	return b.Equals(a);
			//}
			return object.Equals(a, b);
			//if(_listEqual == null) {
			//	_listEqual = new Dictionary<int, System.Func<object, object, bool>>();
			//}
			//System.Func<object, object, bool> func;
			//var uid = uNodeUtility.GenerateUID(aType.GetHashCode(), bType.GetHashCode());
			//if(!_listEqual.ContainsKey(uid)) {
			//	try {
			//		ParameterExpression paramA = Expression.Parameter(typeof(object), "a"),
			//			paramB = Expression.Parameter(typeof(object), "b");
			//		func = Expression.Lambda<System.Func<object, object, bool>>(Expression.Equal(Expression.Convert(paramA, aType), Expression.Convert(paramB, bType)), paramA, paramB).Compile();
			//	}
			//	catch {
			//		func = (x, y) => {
			//			if(x != null) {
			//				return x.Equals(y);
			//			} else if(y != null) {
			//				return y.Equals(x);
			//			}
			//			return object.Equals(x, y);
			//		};
			//	}
			//	_listEqual.Add(uid, func);
			//} else {
			//	func = _listEqual[uid];
			//}
			//return func(a, b);
		}

		public static bool NotEqual(object a, object b, Type aType, Type bType) {
			if(aType != bType) {
				if(aType.IsPrimitive && bType.IsPrimitive) {
					if(bType.IsCastableTo(aType)) {
						b = System.Convert.ChangeType(b, aType);
						//bType = aType;
					}
					else if(aType.IsCastableTo(bType)) {
						a = System.Convert.ChangeType(a, bType);
						//aType = bType;
					}
				}
			}
			if(a != null) {
				return !a.Equals(b);
			}
			else if(b != null) {
				return !b.Equals(a);
			}
			return !object.Equals(a, b);
			//if(_listNotEqual == null) {
			//	_listNotEqual = new Dictionary<int, System.Func<object, object, bool>>();
			//}
			//System.Func<object, object, bool> func;
			//var uid = uNodeUtility.GenerateUID(aType.GetHashCode(), bType.GetHashCode());
			//if(!_listNotEqual.ContainsKey(uid)) {
			//	try {
			//		ParameterExpression paramA = Expression.Parameter(typeof(object), "a"),
			//			paramB = Expression.Parameter(typeof(object), "b");
			//		func = Expression.Lambda<System.Func<object, object, bool>>(Expression.NotEqual(Expression.Convert(paramA, aType), Expression.Convert(paramB, bType)), paramA, paramB).Compile();
			//	}
			//	catch {
			//		func = (x, y) => {
			//			if(x != null) {
			//				return !x.Equals(y);
			//			} else if(y != null) {
			//				return !y.Equals(x);
			//			}
			//			return !object.Equals(x, y);
			//		};
			//	}
			//	_listNotEqual.Add(uid, func);
			//} else {
			//	func = _listNotEqual[uid];
			//}
			//return func(a, b);
		}

		public static bool GreaterThan(object a, object b, Type aType, Type bType) {
			if(aType != bType) {
				if(aType.IsPrimitive && bType.IsPrimitive) {
					if(bType.IsCastableTo(aType)) {
						b = System.Convert.ChangeType(b, aType);
						bType = aType;
					}
					else if(aType.IsCastableTo(bType)) {
						a = System.Convert.ChangeType(a, bType);
						aType = bType;
					}
				}
			}
			if(_listGreaterThan == null) {
				_listGreaterThan = new Dictionary<int, System.Func<object, object, bool>>();
			}
			System.Func<object, object, bool> func;
			var uid = uNodeUtility.GetHashCode(aType.GetHashCode(), bType.GetHashCode());
			if(!_listGreaterThan.ContainsKey(uid)) {
				try {
					ParameterExpression paramA = Expression.Parameter(typeof(object), "a"),
						paramB = Expression.Parameter(typeof(object), "b");
					func = Expression.Lambda<System.Func<object, object, bool>>(Expression.GreaterThan(Expression.Convert(paramA, aType), Expression.Convert(paramB, bType)), paramA, paramB).Compile();
				}
				catch {
					if(aType.IsPrimitive && bType.IsPrimitive) {
						if(aType == typeof(byte)) {
							func = (x, y) => {
								return (byte)x > System.Convert.ToByte(y);
							};
						}
						else if(aType == typeof(sbyte)) {
							func = (x, y) => {
								return (sbyte)x > System.Convert.ToSByte(y);
							};
						}
						else if(aType == typeof(char)) {
							func = (x, y) => {
								return (char)x > System.Convert.ToChar(y);
							};
						}
						else if(aType == typeof(short)) {
							func = (x, y) => {
								return (short)x > (short)System.Convert.ChangeType(y, aType);
							};
						}
						else if(aType == typeof(ushort)) {
							func = (x, y) => {
								return (ushort)x > (ushort)System.Convert.ChangeType(y, aType);
							};
						}
						else if(aType == typeof(int)) {
							func = (x, y) => {
								return (int)x > (int)System.Convert.ChangeType(y, aType);
							};
						}
						else if(aType == typeof(uint)) {
							func = (x, y) => {
								return (uint)x > (uint)System.Convert.ChangeType(y, aType);
							};
						}
						else if(aType == typeof(long)) {
							func = (x, y) => {
								return (long)x > (long)System.Convert.ChangeType(y, aType);
							};
						}
						else if(aType == typeof(ulong)) {
							func = (x, y) => {
								return (ulong)x > (ulong)System.Convert.ChangeType(y, aType);
							};
						}
						else if(aType == typeof(float)) {
							func = (x, y) => {
								return (float)x > (float)System.Convert.ChangeType(y, aType);
							};
						}
						else if(aType == typeof(double)) {
							func = (x, y) => {
								return (double)x > (double)System.Convert.ChangeType(y, aType);
							};
						}
						else {
							throw;
						}
					}
					else {
						var paramTypes = new[] { aType, bType };
						var method = aType.GetMethod("op_GreaterThan", BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic, null, paramTypes, null);
						if(method == null && aType != bType) {
							method = bType.GetMethod("op_GreaterThan", BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic, null, paramTypes, null);
							if(method == null) {
								method = FindMethod(aType, "op_GreaterThan", paramTypes);
								if(method == null) {
									method = FindMethod(bType, "op_GreaterThan", paramTypes);
								}
							}
						}
						if(method != null) {
							bool sameParamType = true;
							var mParamType = method.GetParameters();
							for(int i = 0; i < mParamType.Length; i++) {
								if(mParamType[i].ParameterType != paramTypes[i]) {
									sameParamType = false;
									paramTypes[i] = mParamType[i].ParameterType;
								}
							}
							if(sameParamType) {
								func = (x, y) => {
									return (bool)method.InvokeOptimized(null, new[] { x, y });
								};
							}
							else {
								func = (x, y) => {
									return (bool)method.InvokeOptimized(null, new[] { Convert(x, paramTypes[0]), Convert(y, paramTypes[1]) });
								};
							}
						}
						else {
							throw;
						}
					}
				}
				_listGreaterThan.Add(uid, func);
			}
			else {
				func = _listGreaterThan[uid];
			}
			return func(a, b);
		}

		public static bool LessThan(object a, object b, Type aType, Type bType) {
			if(aType != bType) {
				if(aType.IsPrimitive && bType.IsPrimitive) {
					if(bType.IsCastableTo(aType)) {
						b = System.Convert.ChangeType(b, aType);
						bType = aType;
					}
					else if(aType.IsCastableTo(bType)) {
						a = System.Convert.ChangeType(a, bType);
						aType = bType;
					}
				}
			}
			if(_listLessThan == null) {
				_listLessThan = new Dictionary<int, System.Func<object, object, bool>>();
			}
			System.Func<object, object, bool> func;
			var uid = uNodeUtility.GetHashCode(aType.GetHashCode(), bType.GetHashCode());
			if(!_listLessThan.ContainsKey(uid)) {
				try {
					ParameterExpression paramA = Expression.Parameter(typeof(object), "a"),
						paramB = Expression.Parameter(typeof(object), "b");
					func = Expression.Lambda<System.Func<object, object, bool>>(Expression.LessThan(Expression.Convert(paramA, aType), Expression.Convert(paramB, bType)), paramA, paramB).Compile();
				}
				catch {
					if(aType.IsPrimitive && bType.IsPrimitive) {
						if(aType == typeof(byte)) {
							func = (x, y) => {
								return (byte)x < System.Convert.ToByte(y);
							};
						}
						else if(aType == typeof(sbyte)) {
							func = (x, y) => {
								return (sbyte)x < System.Convert.ToSByte(y);
							};
						}
						else if(aType == typeof(char)) {
							func = (x, y) => {
								return (char)x < System.Convert.ToChar(y);
							};
						}
						else if(aType == typeof(short)) {
							func = (x, y) => {
								return (short)x < (short)System.Convert.ChangeType(y, aType);
							};
						}
						else if(aType == typeof(ushort)) {
							func = (x, y) => {
								return (ushort)x < (ushort)System.Convert.ChangeType(y, aType);
							};
						}
						else if(aType == typeof(int)) {
							func = (x, y) => {
								return (int)x < (int)System.Convert.ChangeType(y, aType);
							};
						}
						else if(aType == typeof(uint)) {
							func = (x, y) => {
								return (uint)x < (uint)System.Convert.ChangeType(y, aType);
							};
						}
						else if(aType == typeof(long)) {
							func = (x, y) => {
								return (long)x < (long)System.Convert.ChangeType(y, aType);
							};
						}
						else if(aType == typeof(ulong)) {
							func = (x, y) => {
								return (ulong)x < (ulong)System.Convert.ChangeType(y, aType);
							};
						}
						else if(aType == typeof(float)) {
							func = (x, y) => {
								return (float)x < (float)System.Convert.ChangeType(y, aType);
							};
						}
						else if(aType == typeof(double)) {
							func = (x, y) => {
								return (double)x < (double)System.Convert.ChangeType(y, aType);
							};
						}
						else {
							throw;
						}
					}
					else {
						var paramTypes = new[] { aType, bType };
						var method = aType.GetMethod("op_LessThan", BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic, null, paramTypes, null);
						if(method == null && aType != bType) {
							method = bType.GetMethod("op_LessThan", BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic, null, paramTypes, null);
							if(method == null) {
								method = FindMethod(aType, "op_LessThan", paramTypes);
								if(method == null) {
									method = FindMethod(bType, "op_LessThan", paramTypes);
								}
							}
						}
						if(method != null) {
							bool sameParamType = true;
							var mParamType = method.GetParameters();
							for(int i = 0; i < mParamType.Length; i++) {
								if(mParamType[i].ParameterType != paramTypes[i]) {
									sameParamType = false;
									paramTypes[i] = mParamType[i].ParameterType;
								}
							}
							if(sameParamType) {
								func = (x, y) => {
									return (bool)method.InvokeOptimized(null, new[] { x, y });
								};
							}
							else {
								func = (x, y) => {
									return (bool)method.InvokeOptimized(null, new[] { Convert(x, paramTypes[0]), Convert(y, paramTypes[1]) });
								};
							}
						}
						else {
							throw;
						}
					}
				}
				_listLessThan.Add(uid, func);
			}
			else {
				func = _listLessThan[uid];
			}
			return func(a, b);
		}

		public static bool GreaterThanOrEqual(object a, object b, Type aType, Type bType) {
			if(aType != bType) {
				if(aType.IsPrimitive && bType.IsPrimitive) {
					if(bType.IsCastableTo(aType)) {
						b = System.Convert.ChangeType(b, aType);
						bType = aType;
					}
					else if(aType.IsCastableTo(bType)) {
						a = System.Convert.ChangeType(a, bType);
						aType = bType;
					}
				}
			}
			if(_listGreaterThanOrEqual == null) {
				_listGreaterThanOrEqual = new Dictionary<int, System.Func<object, object, bool>>();
			}
			System.Func<object, object, bool> func;
			var uid = uNodeUtility.GetHashCode(aType.GetHashCode(), bType.GetHashCode());
			if(!_listGreaterThanOrEqual.ContainsKey(uid)) {
				try {
					ParameterExpression paramA = Expression.Parameter(typeof(object), "a"),
						paramB = Expression.Parameter(typeof(object), "b");
					func = Expression.Lambda<System.Func<object, object, bool>>(Expression.GreaterThanOrEqual(Expression.Convert(paramA, aType), Expression.Convert(paramB, bType)), paramA, paramB).Compile();
				}
				catch {
					if(aType.IsPrimitive && bType.IsPrimitive) {
						if(aType == typeof(byte)) {
							func = (x, y) => {
								return (byte)x >= System.Convert.ToByte(y);
							};
						}
						else if(aType == typeof(sbyte)) {
							func = (x, y) => {
								return (sbyte)x >= System.Convert.ToSByte(y);
							};
						}
						else if(aType == typeof(char)) {
							func = (x, y) => {
								return (char)x >= System.Convert.ToChar(y);
							};
						}
						else if(aType == typeof(short)) {
							func = (x, y) => {
								return (short)x >= (short)System.Convert.ChangeType(y, aType);
							};
						}
						else if(aType == typeof(ushort)) {
							func = (x, y) => {
								return (ushort)x >= (ushort)System.Convert.ChangeType(y, aType);
							};
						}
						else if(aType == typeof(int)) {
							func = (x, y) => {
								return (int)x >= (int)System.Convert.ChangeType(y, aType);
							};
						}
						else if(aType == typeof(uint)) {
							func = (x, y) => {
								return (uint)x >= (uint)System.Convert.ChangeType(y, aType);
							};
						}
						else if(aType == typeof(long)) {
							func = (x, y) => {
								return (long)x >= (long)System.Convert.ChangeType(y, aType);
							};
						}
						else if(aType == typeof(ulong)) {
							func = (x, y) => {
								return (ulong)x >= (ulong)System.Convert.ChangeType(y, aType);
							};
						}
						else if(aType == typeof(float)) {
							func = (x, y) => {
								return (float)x >= (float)System.Convert.ChangeType(y, aType);
							};
						}
						else if(aType == typeof(double)) {
							func = (x, y) => {
								return (double)x >= (double)System.Convert.ChangeType(y, aType);
							};
						}
						else {
							throw;
						}
					}
					else {
						var paramTypes = new[] { aType, bType };
						var method = aType.GetMethod("op_GreaterThanOrEqual", BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic, null, paramTypes, null);
						if(method == null && aType != bType) {
							method = bType.GetMethod("op_GreaterThanOrEqual", BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic, null, paramTypes, null);
							if(method == null) {
								method = FindMethod(aType, "op_GreaterThanOrEqual", paramTypes);
								if(method == null) {
									method = FindMethod(bType, "op_GreaterThanOrEqual", paramTypes);
								}
							}
						}
						if(method != null) {
							bool sameParamType = true;
							var mParamType = method.GetParameters();
							for(int i = 0; i < mParamType.Length; i++) {
								if(mParamType[i].ParameterType != paramTypes[i]) {
									sameParamType = false;
									paramTypes[i] = mParamType[i].ParameterType;
								}
							}
							if(sameParamType) {
								func = (x, y) => {
									return (bool)method.InvokeOptimized(null, new[] { x, y });
								};
							}
							else {
								func = (x, y) => {
									return (bool)method.InvokeOptimized(null, new[] { Convert(x, paramTypes[0]), Convert(y, paramTypes[1]) });
								};
							}
						}
						else {
							throw;
						}
					}
				}
				_listGreaterThanOrEqual.Add(uid, func);
			}
			else {
				func = _listGreaterThanOrEqual[uid];
			}
			return func(a, b);
		}

		public static bool LessThanOrEqual(object a, object b, Type aType, Type bType) {
			if(aType != bType) {
				if(aType.IsPrimitive && bType.IsPrimitive) {
					if(bType.IsCastableTo(aType)) {
						b = System.Convert.ChangeType(b, aType);
						bType = aType;
					}
					else if(aType.IsCastableTo(bType)) {
						a = System.Convert.ChangeType(a, bType);
						aType = bType;
					}
				}
			}
			if(_listLessThanOrEqual == null) {
				_listLessThanOrEqual = new Dictionary<int, System.Func<object, object, bool>>();
			}
			System.Func<object, object, bool> func;
			var uid = uNodeUtility.GetHashCode(aType.GetHashCode(), bType.GetHashCode());
			if(!_listLessThanOrEqual.ContainsKey(uid)) {
				try {
					ParameterExpression paramA = Expression.Parameter(typeof(object), "a"),
						paramB = Expression.Parameter(typeof(object), "b");
					func = Expression.Lambda<System.Func<object, object, bool>>(Expression.LessThanOrEqual(Expression.Convert(paramA, aType), Expression.Convert(paramB, bType)), paramA, paramB).Compile();
				}
				catch {
					if(aType.IsPrimitive && bType.IsPrimitive) {
						if(aType == typeof(byte)) {
							func = (x, y) => {
								return (byte)x <= System.Convert.ToByte(y);
							};
						}
						else if(aType == typeof(sbyte)) {
							func = (x, y) => {
								return (sbyte)x <= System.Convert.ToSByte(y);
							};
						}
						else if(aType == typeof(char)) {
							func = (x, y) => {
								return (char)x <= System.Convert.ToChar(y);
							};
						}
						else if(aType == typeof(short)) {
							func = (x, y) => {
								return (short)x <= (short)System.Convert.ChangeType(y, aType);
							};
						}
						else if(aType == typeof(ushort)) {
							func = (x, y) => {
								return (ushort)x <= (ushort)System.Convert.ChangeType(y, aType);
							};
						}
						else if(aType == typeof(int)) {
							func = (x, y) => {
								return (int)x <= (int)System.Convert.ChangeType(y, aType);
							};
						}
						else if(aType == typeof(uint)) {
							func = (x, y) => {
								return (uint)x <= (uint)System.Convert.ChangeType(y, aType);
							};
						}
						else if(aType == typeof(long)) {
							func = (x, y) => {
								return (long)x <= (long)System.Convert.ChangeType(y, aType);
							};
						}
						else if(aType == typeof(ulong)) {
							func = (x, y) => {
								return (ulong)x <= (ulong)System.Convert.ChangeType(y, aType);
							};
						}
						else if(aType == typeof(float)) {
							func = (x, y) => {
								return (float)x <= (float)System.Convert.ChangeType(y, aType);
							};
						}
						else if(aType == typeof(double)) {
							func = (x, y) => {
								return (double)x <= (double)System.Convert.ChangeType(y, aType);
							};
						}
						else {
							throw;
						}
					}
					else {
						var paramTypes = new[] { aType, bType };
						var method = aType.GetMethod("op_LessThanOrEqual", BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic, null, paramTypes, null);
						if(method == null && aType != bType) {
							method = bType.GetMethod("op_LessThanOrEqual", BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic, null, paramTypes, null);
							if(method == null) {
								method = FindMethod(aType, "op_LessThanOrEqual", paramTypes);
								if(method == null) {
									method = FindMethod(bType, "op_LessThanOrEqual", paramTypes);
								}
							}
						}
						if(method != null) {
							bool sameParamType = true;
							var mParamType = method.GetParameters();
							for(int i = 0; i < mParamType.Length; i++) {
								if(mParamType[i].ParameterType != paramTypes[i]) {
									sameParamType = false;
									paramTypes[i] = mParamType[i].ParameterType;
								}
							}
							if(sameParamType) {
								func = (x, y) => {
									return (bool)method.InvokeOptimized(null, new[] { x, y });
								};
							}
							else {
								func = (x, y) => {
									return (bool)method.InvokeOptimized(null, new[] { Convert(x, paramTypes[0]), Convert(y, paramTypes[1]) });
								};
							}
						}
						else {
							throw;
						}
					}
				}
				_listLessThanOrEqual.Add(uid, func);
			}
			else {
				func = _listLessThanOrEqual[uid];
			}
			return func(a, b);
		}
		#endregion

		#region Shift
		static Func<object, int, object> _leftShift, _rightShift;
		static Func<object, int, object> leftShift, rightShift;
		static Dictionary<Type, Func<object, int, object>> __leftShift, __rightShift;

		public static object LeftShift(object a, int b) {
			if(leftShift == null) {
				ParameterExpression paramA = Expression.Parameter(typeof(object), "a"),
					paramB = Expression.Parameter(typeof(int), "b");
				leftShift = Expression.Lambda<Func<object, int, object>>(
					Expression.Convert(
						Expression.LeftShift(
							Expression.Convert(paramA, typeof(object)),
							paramB),
						typeof(object)), paramA, paramB).Compile();
			}
			return leftShift(a, b);
		}

		public static object LeftShift(object a, int b, Type firstType) {
			if(__leftShift == null) {
				__leftShift = new Dictionary<Type, Func<object, int, object>>();
			}
			if(!__leftShift.ContainsKey(firstType)) {
				ParameterExpression paramA = Expression.Parameter(typeof(object), "a"),
					paramB = Expression.Parameter(typeof(int), "b");
				__leftShift.Add(firstType, Expression.Lambda<Func<object, int, object>>(
					Expression.Convert(
						Expression.LeftShift(
							Expression.Convert(paramA, firstType),
							paramB),
						typeof(object)), paramA, paramB).Compile());
			}
			return __leftShift[firstType](a, b);
		}

		public static object RightShift(object a, int b) {
			if(rightShift == null) {
				ParameterExpression paramA = Expression.Parameter(typeof(object), "a"),
					paramB = Expression.Parameter(typeof(int), "b");
				rightShift = Expression.Lambda<Func<object, int, object>>(
					Expression.Convert(
						Expression.RightShift(
							Expression.Convert(paramA, typeof(object)),
							paramB),
						typeof(object)), paramA, paramB).Compile();
			}
			return rightShift(a, b);
		}

		public static object RightShift(object a, int b, Type firstType) {
			if(__rightShift == null) {
				__rightShift = new Dictionary<Type, Func<object, int, object>>();
			}
			if(!__rightShift.ContainsKey(firstType)) {
				ParameterExpression paramA = Expression.Parameter(typeof(object), "a"),
					paramB = Expression.Parameter(typeof(int), "b");
				__rightShift.Add(firstType, Expression.Lambda<Func<object, int, object>>(
					Expression.Convert(
						Expression.RightShift(
							Expression.Convert(paramA, firstType),
							paramB),
						typeof(object)), paramA, paramB).Compile());
			}
			return __rightShift[firstType](a, b);
		}
		#endregion

		#region Coalescing
		static Func<object, object, object> coalesce;

		public static object Coalesce(object a, object b) {
			if(coalesce == null) {
				ParameterExpression paramA = Expression.Parameter(typeof(object), "a"),
					paramB = Expression.Parameter(typeof(object), "b");
				coalesce = Expression.Lambda<Func<object, object, object>>(
					Expression.Convert(
						Expression.Coalesce(
							Expression.Convert(paramA, typeof(object)),
							Expression.Convert(paramB, typeof(object))),
						typeof(object)), paramA, paramB).Compile();
			}
			return coalesce(a, b);
		}
		#endregion

		#region Bitwise
		static Func<object, object, object> _and, _or, _exclusiveOr;
		static Func<object, object, object> and, or, exclusiveOr;
		static Dictionary<Type, Func<object, object, object>> _andEnum, _orEnum, _exclusiveOrEnum;

		public static object And(object a, object b) {
			if(a is Enum) {
				Type t = a.GetType();
				if(_andEnum == null) {
					_andEnum = new Dictionary<Type, Func<object, object, object>>();
				}
				if(!_andEnum.ContainsKey(t)) {
					ParameterExpression paramA = Expression.Parameter(typeof(object), "a"),
						paramB = Expression.Parameter(typeof(object), "b");
					_andEnum[t] = Expression.Lambda<Func<object, object, object>>(
						Expression.Convert(
							Expression.And(
								Expression.Convert(paramA, Enum.GetUnderlyingType(t)),
								Expression.Convert(paramB, Enum.GetUnderlyingType(t))),
							typeof(object)), paramA, paramB).Compile();
				}
				return _andEnum[t](a, b);
			}
			else if(typeof(object) == typeof(object)) {
				Type t = a.GetType();
				if(_andEnum == null) {
					_andEnum = new Dictionary<Type, Func<object, object, object>>();
				}
				if(!_andEnum.ContainsKey(t)) {
					ParameterExpression paramA = Expression.Parameter(typeof(object), "a"),
						paramB = Expression.Parameter(typeof(object), "b");
					_andEnum[t] = Expression.Lambda<Func<object, object, object>>(
						Expression.Convert(
							Expression.And(
								Expression.Convert(paramA, t),
								Expression.Convert(paramB, t)),
							typeof(object)), paramA, paramB).Compile();
				}
				return _andEnum[t](a, b);
			}
			if(and == null) {
				ParameterExpression paramA = Expression.Parameter(typeof(object), "a"),
					paramB = Expression.Parameter(typeof(object), "b");
				and = Expression.Lambda<Func<object, object, object>>(
					Expression.Convert(
						Expression.And(
							Expression.Convert(paramA, typeof(object)),
							Expression.Convert(paramB, typeof(object))),
						typeof(object)), paramA, paramB).Compile();
			}
			return and(a, b);
		}

		public static object Or(object a, object b) {
			if(a is Enum) {
				Type t = a.GetType();
				if(_andEnum == null) {
					_andEnum = new Dictionary<Type, Func<object, object, object>>();
				}
				if(!_andEnum.ContainsKey(t)) {
					ParameterExpression paramA = Expression.Parameter(typeof(object), "a"),
						paramB = Expression.Parameter(typeof(object), "b");
					_andEnum[t] = Expression.Lambda<Func<object, object, object>>(
						Expression.Convert(
							Expression.Or(
								Expression.Convert(paramA, Enum.GetUnderlyingType(t)),
								Expression.Convert(paramB, Enum.GetUnderlyingType(t))),
							typeof(object)), paramA, paramB).Compile();
				}
				return _andEnum[t](a, b);
			}
			else if(typeof(object) == typeof(object)) {
				Type t = a.GetType();
				if(_andEnum == null) {
					_andEnum = new Dictionary<Type, Func<object, object, object>>();
				}
				if(!_andEnum.ContainsKey(t)) {
					ParameterExpression paramA = Expression.Parameter(typeof(object), "a"),
						paramB = Expression.Parameter(typeof(object), "b");
					_andEnum[t] = Expression.Lambda<Func<object, object, object>>(
						Expression.Convert(
							Expression.Or(
								Expression.Convert(paramA, t),
								Expression.Convert(paramB, t)),
							typeof(object)), paramA, paramB).Compile();
				}
				return _andEnum[t](a, b);
			}
			if(or == null) {
				ParameterExpression paramA = Expression.Parameter(typeof(object), "a"),
					paramB = Expression.Parameter(typeof(object), "b");
				or = Expression.Lambda<Func<object, object, object>>(
					Expression.Convert(
						Expression.Or(
							Expression.Convert(paramA, typeof(object)),
							Expression.Convert(paramB, typeof(object))),
						typeof(object)), paramA, paramB).Compile();
			}
			return or(a, b);
		}

		public static object ExclusiveOr(object a, object b) {
			if(a is Enum) {
				Type t = a.GetType();
				if(_exclusiveOrEnum == null) {
					_exclusiveOrEnum = new Dictionary<Type, Func<object, object, object>>();
				}
				if(!_exclusiveOrEnum.ContainsKey(t)) {
					ParameterExpression paramA = Expression.Parameter(typeof(object), "a"),
						paramB = Expression.Parameter(typeof(object), "b");
					_exclusiveOrEnum[t] = Expression.Lambda<Func<object, object, object>>(
						Expression.Convert(
							Expression.Or(
								Expression.Convert(paramA, Enum.GetUnderlyingType(t)),
								Expression.Convert(paramB, Enum.GetUnderlyingType(t))),
							typeof(object)), paramA, paramB).Compile();
				}
				return _exclusiveOrEnum[t](a, b);
			}
			else if(typeof(object) == typeof(object)) {
				Type t = a.GetType();
				if(_exclusiveOrEnum == null) {
					_exclusiveOrEnum = new Dictionary<Type, Func<object, object, object>>();
				}
				if(!_exclusiveOrEnum.ContainsKey(t)) {
					ParameterExpression paramA = Expression.Parameter(typeof(object), "a"),
						paramB = Expression.Parameter(typeof(object), "b");
					_exclusiveOrEnum[t] = Expression.Lambda<Func<object, object, object>>(
						Expression.Convert(
							Expression.Or(
								Expression.Convert(paramA, t),
								Expression.Convert(paramB, t)),
							typeof(object)), paramA, paramB).Compile();
				}
				return _exclusiveOrEnum[t](a, b);
			}
			if(exclusiveOr == null) {
				ParameterExpression paramA = Expression.Parameter(typeof(object), "a"),
					paramB = Expression.Parameter(typeof(object), "b");
				exclusiveOr = Expression.Lambda<Func<object, object, object>>(
					Expression.Convert(
						Expression.ExclusiveOr(
							Expression.Convert(paramA, typeof(object)),
							Expression.Convert(paramB, typeof(object))),
						typeof(object)), paramA, paramB).Compile();
			}
			return exclusiveOr(a, b);
		}
		#endregion

		#region Unary
		static System.Func<object, object> negate, not;
		static Dictionary<Type, System.Func<object, object>> _negate, _not;
		static Dictionary<(Type, Type), System.Func<object, object>> convert;

		public static object Convert(object a, Type type) {
			if(a == null) {
				throw new NullReferenceException("The target to convert cannot be null.");
			}
			if(convert == null) {
				convert = new Dictionary<(Type, Type), Func<object, object>>();
			}
			var fromType = a.GetType();
			Func<object, object> func;
			if(!convert.TryGetValue((type, fromType), out func)) {
				if(type == typeof(string)) {
					func = (val) => val.ToString();
				}
				else if(type == typeof(object)) {
					func = (val) => val as object;
				}
				else {
					if(!fromType.IsPrimitive) {
						if(fromType != type && fromType.IsSubclassOf(type) && fromType.IsInterface == false) {
							var methods = fromType.FindImplicitOperator(type);
							foreach(var m in methods) {
								if(m == null)
									continue;
								var param = m.GetParameters();
								if(param.Length == 1) {
									fromType = param[0].ParameterType;
								}
								break;
							}
						}
					}
					else if(type.IsEnum) {
						if(fromType == typeof(float) || fromType == typeof(double)) {
							func = (val) => Enum.ToObject(type, System.Convert.ToInt32(val));
						}
						else {
							func = (val) => Enum.ToObject(type, val);
						}
					}
					//else if(typeof(object).IsPrimitive) {
					//	var FT = typeof(object);
					//	if(FT == type) {
					//		func = (val) => val;
					//	} else if(type.IsPrimitive) {
					//		func = (val) => System.Convert.ChangeType(val, type);
					//	}
					//}
					if(func == null) {
						if(fromType == type) {
							if(type.IsValueType) {
								ParameterExpression paramA = Expression.Parameter(typeof(object), "a");
								//For value type, the value will be duplicated
								func = Expression.Lambda<System.Func<object, object>>(
										Expression.Convert(
											Expression.Convert(paramA, type),
											typeof(object)), paramA).Compile();
							}
							else {
								//For reference type
								func = (val) => val;
							}
						}
						else {
							ParameterExpression paramA = Expression.Parameter(typeof(object), "a");
							func = Expression.Lambda<System.Func<object, object>>(
									Expression.Convert(
										Expression.Convert(
											Expression.Convert(paramA, fromType),
											type),
										typeof(object)), paramA).Compile();
						}
					}
				}
				convert[(type, fromType)] = func;
			}
			try {
				return func(a);
			}
			catch(InvalidCastException ex) {
				throw new InvalidCastException($"Cannot convert '{a.GetType().FullName}' to {type.FullName}" + "\n" + ex.ToString());
			}
			catch(NullReferenceException ex) {
				throw new NullReferenceException($"Cannot convert to {type.FullName} because the value is null.", ex);
			}
			catch(Exception ex) {
				if(a != null) {
					throw new Exception($"Failed to convert: '{a.GetType().FullName}' to {type.FullName}", ex);
				}
				throw;
			}
		}

		public static object Negate(object a) {
			if(negate == null) {
				ParameterExpression paramA = Expression.Parameter(typeof(object), "a");
				negate = Expression.Lambda<System.Func<object, object>>(Expression.Negate(paramA), paramA).Compile();
			}
			return negate(a);
		}

		public static object Negate(object a, Type type) {
			if(_negate == null) {
				_negate = new Dictionary<Type, Func<object, object>>();
			}
			if(!_negate.ContainsKey(type)) {
				ParameterExpression paramA = Expression.Parameter(typeof(object), "a");
				_negate[type] = Expression.Lambda<System.Func<object, object>>(Expression.Convert(Expression.Negate(Expression.Convert(paramA, type)), typeof(object)), paramA).Compile();
			}
			return _negate[type](a);
		}

		public static object BitwiseNot(object a) {
			if(a is int) {
				return ~(int)a;
			}
			else if(a is uint) {
				return ~(uint)a;
			}
			else if(a is long) {
				return ~(long)a;
			}
			else if(a is ulong) {
				return ~(ulong)a;
			}
			else if(a is short) {
				return ~(short)a;
			}
			else if(a is byte) {
				return ~(byte)a;
			}
			else {
				throw new InvalidOperationException("Unsupported type to bitwise not: " + a.GetType());
			}
		}

		public static object Not(object a) {
			if(not == null) {
				ParameterExpression paramA = Expression.Parameter(typeof(object), "a");
				not = Expression.Lambda<System.Func<object, object>>(Expression.Not(paramA), paramA).Compile();
			}
			return not(a);
		}

		public static object Not(object a, Type type) {
			if(_not == null) {
				_not = new Dictionary<Type, Func<object, object>>();
			}
			if(!_not.ContainsKey(type)) {
				ParameterExpression paramA = Expression.Parameter(typeof(object), "a");
				_not[type] = Expression.Lambda<System.Func<object, object>>(Expression.Convert(Expression.Not(Expression.Convert(paramA, type)), typeof(object)), paramA).Compile();
			}
			return _not[type](a);
		}
		#endregion

		#region Other
		static Dictionary<Type, System.Func<object, bool>> typeIs;
		static Dictionary<Type, System.Func<object, object>> typeAs;
		static Dictionary<Type, Func<object, object>> increment;
		static Dictionary<Type, Func<object, object>> decrement;

		public static bool TypeIs(object a, Type type) {
			if(typeIs == null) {
				typeIs = new Dictionary<Type, Func<object, bool>>();
			}
			System.Func<object, bool> func;
			if(!typeIs.TryGetValue(type, out func)) {
				ParameterExpression paramA = Expression.Parameter(typeof(object), "a");
				func = Expression.Lambda<System.Func<object, bool>>(Expression.TypeIs(paramA, type), paramA).Compile();
				typeIs[type] = func;
			}
			return func(a);
		}

		public static object TypeAs(object a, Type type) {
			if(typeAs == null) {
				typeAs = new Dictionary<Type, Func<object, object>>();
			}
			System.Func<object, object> func;
			if(!typeAs.TryGetValue(type, out func)) {
				ParameterExpression paramA = Expression.Parameter(typeof(object), "a");
				func = Expression.Lambda<System.Func<object, object>>(Expression.TypeAs(paramA, type), paramA).Compile();
				typeAs[type] = func;
			}
			return func(a);
		}

		public static object Increment(object obj, Type type) {
			if(type == typeof(byte)) {
				byte o = (byte)obj;
				obj = ++o;
			}
			else if(type == typeof(sbyte)) {
				sbyte o = (sbyte)obj;
				obj = ++o;
			}
			else if(type == typeof(char)) {
				char o = (char)obj;
				obj = ++o;
			}
			else if(type == typeof(short)) {
				short o = (short)obj;
				obj = ++o;
			}
			else if(type == typeof(ushort)) {
				ushort o = (ushort)obj;
				obj = ++o;
			}
			else if(type == typeof(int)) {
				int o = (int)obj;
				obj = ++o;
			}
			else if(type == typeof(uint)) {
				uint o = (uint)obj;
				obj = ++o;
			}
			else if(type == typeof(long)) {
				long o = (long)obj;
				obj = ++o;
			}
			else if(type == typeof(ulong)) {
				ulong o = (ulong)obj;
				obj = ++o;
			}
			else if(type == typeof(float)) {
				float o = (float)obj;
				obj = ++o;
			}
			else if(type == typeof(double)) {
				double o = (double)obj;
				obj = ++o;
			}
			else if(type == typeof(decimal) || type.IsCastableTo(typeof(decimal))) {
				decimal o = (decimal)obj;
				obj = ++o;
			}
			else {
				throw new Exception("Unsupported increment operator of type : " + type.PrettyName(true));
			}
			return obj;
			/*
			if(increment == null) {
				increment = new Dictionary<Type, Func<object, object>>();
			}
			if(!increment.ContainsKey(type)) {
				ParameterExpression paramA = Expression.Parameter(typeof(object), "a");
				increment[type] = Expression.Lambda<System.Func<object, object>>(Expression.Convert(Expression.Increment(Expression.Convert(paramA, type)), typeof(object)), paramA).Compile();
			}
			return increment[type](a);
			*/
		}

		public static object Decrement(object obj, Type type) {
			if(type == typeof(byte)) {
				byte o = (byte)obj;
				obj = --o;
			}
			else if(type == typeof(sbyte)) {
				sbyte o = (sbyte)obj;
				obj = --o;
			}
			else if(type == typeof(char)) {
				char o = (char)obj;
				obj = --o;
			}
			else if(type == typeof(short)) {
				short o = (short)obj;
				obj = --o;
			}
			else if(type == typeof(ushort)) {
				ushort o = (ushort)obj;
				obj = --o;
			}
			else if(type == typeof(int)) {
				int o = (int)obj;
				obj = --o;
			}
			else if(type == typeof(uint)) {
				uint o = (uint)obj;
				obj = --o;
			}
			else if(type == typeof(long)) {
				long o = (long)obj;
				obj = --o;
			}
			else if(type == typeof(ulong)) {
				ulong o = (ulong)obj;
				obj = --o;
			}
			else if(type == typeof(float)) {
				float o = (float)obj;
				obj = --o;
			}
			else if(type == typeof(double)) {
				double o = (double)obj;
				obj = --o;
			}
			else if(type == typeof(decimal) || type.IsCastableTo(typeof(decimal))) {
				decimal o = (decimal)obj;
				obj = --o;
			}
			else {
				throw new Exception("Unsupported decrement operator of type : " + type.PrettyName(true));
			}
			return obj;
			/*
			if(decrement == null) {
				decrement = new Dictionary<Type, Func<object, object>>();
			}
			if(!decrement.ContainsKey(type)) {
				ParameterExpression paramA = Expression.Parameter(typeof(object), "a");
				decrement[type] = Expression.Lambda<System.Func<object, object>>(Expression.Convert(Expression.Decrement(Expression.Convert(paramA, type)), typeof(object)), paramA).Compile();
			}
			return decrement[type](a);
			*/
		}
		#endregion
	}
}