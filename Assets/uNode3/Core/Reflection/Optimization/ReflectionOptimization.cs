using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Collections.Generic;

namespace MaxyGames.UNode {
	public static class ReflectionOptimization {
		public static readonly bool isAOT = false;
		private static readonly Dictionary<FieldInfo, FieldAccessor> optimizedFields;
		private static readonly Dictionary<PropertyInfo, PropertyAccessor> optimizedProperties;
		private static readonly Dictionary<MethodInfo, MethodInvoker> optimizedMethods;
		private static readonly Dictionary<MemberInfo, bool> supportOptimizations;
		private static readonly int initFrameCount;

#if UNITY_EDITOR
		const int cacheDelayTime = 60;
#endif

		static ReflectionOptimization() {
			optimizedFields = new Dictionary<FieldInfo, FieldAccessor>();
			optimizedProperties = new Dictionary<PropertyInfo, PropertyAccessor>();
			optimizedMethods = new Dictionary<MethodInfo, MethodInvoker>();
			supportOptimizations = new Dictionary<MemberInfo, bool>();
			if(uNodeUtility.isPlaying) {
				initFrameCount = UnityEngine.Time.frameCount;
			}
#if ENABLE_IL2CPP
			isAOT = true;
#endif
		}

		private static bool SupportsOptimization(MemberInfo memberInfo) {
			if(isAOT || memberInfo is IRuntimeMember) {
				return false;
			}
			if(!supportOptimizations.TryGetValue(memberInfo, out var result)) {
				result = ReflectionUtils.GetMemberIsStatic(memberInfo) || !memberInfo.DeclaringType.IsValueType;
				supportOptimizations.Add(memberInfo, result);
			}
			return result;
			//if(memberInfo.DeclaringType.IsValueType && !ReflectionUtils.GetMemberIsStatic(memberInfo)) {
			//	return false;
			//}
			//return true;
		}

		public static bool SupportsOptimization(this MethodInfo methodInfo) {
			if(!(SupportsOptimization(methodInfo as MemberInfo))) {
				return false;
			}
			//if(isAOT && methodInfo.IsVirtual && !methodInfo.IsFinal) {
			//	return false;
			//}
			var parameters = methodInfo.GetParameters();
			if(parameters.Length > 5) {
				return false;
			}
			if(parameters.Any(parameter => parameter.ParameterType.IsByRef)) {
				return false;
			}
			if(methodInfo.CallingConvention == CallingConventions.VarArgs) {
				return false;
			}
			return true;
		}

		#region Fields
		public static bool SupportsOptimization(this FieldInfo fieldInfo) {
			return SupportsOptimization(fieldInfo as MemberInfo);
		}

		public static object GetValueOptimized(this FieldInfo fieldInfo, object target) {
#if ENABLE_IL2CPP
			return fieldInfo.GetValue(target);
#else
			return GetFieldAccessor(fieldInfo).GetValue(target);
#endif
		}

		public static void SetValueOptimized(this FieldInfo fieldInfo, object target, object value) {
#if ENABLE_IL2CPP
			fieldInfo.SetValue(target, value);
#else
			GetFieldAccessor(fieldInfo).SetValue(target, value);
#endif
		}

		public static FieldAccessor GetFieldAccessor(FieldInfo fieldInfo) {
			if(!optimizedFields.TryGetValue(fieldInfo, out var field)) {
				if(SupportsOptimization(fieldInfo)) {
					Type accessorType;
					if(fieldInfo.IsStatic) {
						accessorType = typeof(StaticFieldAccessor<>).MakeGenericType(fieldInfo.FieldType);
					}
					else {
						accessorType = typeof(InstanceFieldAccessor<,>).MakeGenericType(fieldInfo.DeclaringType, fieldInfo.FieldType);
					}
					field = Activator.CreateInstance(accessorType, fieldInfo) as FieldAccessor;
				}
				else {
					field = new ReflectionFieldAccessor(fieldInfo);
				}
				optimizedFields[fieldInfo] = field;
			}
			return field;
		}
		#endregion

		#region Properties
		public static bool SupportsOptimization(this PropertyInfo propertyInfo) {
			return SupportsOptimization(propertyInfo as MemberInfo) && propertyInfo.GetIndexParameters().Length == 0;
		}

		public static object GetValueOptimized(this PropertyInfo propertyInfo, object target) {
#if ENABLE_IL2CPP
			return propertyInfo.GetValue(target);
#else
			return GetPropertyAccessor(propertyInfo).GetValue(target);
#endif
		}

		public static void SetValueOptimized(this PropertyInfo propertyInfo, object target, object value) {
#if ENABLE_IL2CPP
			propertyInfo.SetValue(target, value);
#else
			GetPropertyAccessor(propertyInfo).SetValue(target, value);
#endif
		}

		public static PropertyAccessor GetPropertyAccessor(PropertyInfo propertyInfo) {
			if(!optimizedProperties.TryGetValue(propertyInfo, out var property)) {
				if(SupportsOptimization(propertyInfo)) {
#if UNITY_EDITOR
					if(uNodeUtility.isPlaying && UnityEngine.Time.frameCount - initFrameCount < cacheDelayTime) {
						//This is used for fix crash that's happen on the game start or domain reload.
						return new ReflectionPropertyAccessor(propertyInfo);
					}
#endif
					Type accessorType;
					if(ReflectionUtils.GetMemberIsStatic(propertyInfo)) {
						accessorType = typeof(StaticPropertyAccessor<>).MakeGenericType(propertyInfo.PropertyType);
					}
					else {
						accessorType = typeof(InstancePropertyAccessor<,>).MakeGenericType(propertyInfo.DeclaringType, propertyInfo.PropertyType);
					}
					property = Activator.CreateInstance(accessorType, propertyInfo) as PropertyAccessor;
				}
				else {
					property = new ReflectionPropertyAccessor(propertyInfo);
				}
				optimizedProperties[propertyInfo] = property;
			}
			return property;
		}
		#endregion

		#region Methods
#if UNITY_2022_2_OR_NEWER
		[UnityEngine.HideInCallstack]
#endif
		public static object InvokeOptimized(this MethodInfo methodInfo, object target, params object[] parameters) {
#if ENABLE_IL2CPP
			return methodInfo.Invoke(target, parameters);
#else
			return GetMethodInvoker(methodInfo).Invoke(target, parameters);
#endif
		}

		public static MethodInvoker GetMethodInvoker(MethodInfo methodInfo) {
			MethodInvoker method;
			if(!optimizedMethods.TryGetValue(methodInfo, out method)) {
				if(SupportsOptimization(methodInfo)) {
					Type invokerType;
					var parameters = methodInfo.GetParameters();
					if(methodInfo.ReturnType == typeof(void)) {
						if(methodInfo.IsStatic) {
							if(parameters.Length == 0) {
								invokerType = typeof(StaticActionInvoker);
							}
							else if(parameters.Length == 1) {
								invokerType = typeof(StaticActionInvoker<>).MakeGenericType(parameters[0].ParameterType);
							}
							else if(parameters.Length == 2) {
								invokerType = typeof(StaticActionInvoker<,>).MakeGenericType(parameters[0].ParameterType, parameters[1].ParameterType);
							}
							else if(parameters.Length == 3) {
								invokerType = typeof(StaticActionInvoker<,,>).MakeGenericType(parameters[0].ParameterType, parameters[1].ParameterType, parameters[2].ParameterType);
							}
							else if(parameters.Length == 4) {
								invokerType = typeof(StaticActionInvoker<,,,>).MakeGenericType(parameters[0].ParameterType, parameters[1].ParameterType, parameters[2].ParameterType, parameters[3].ParameterType);
							}
							else if(parameters.Length == 5) {
								invokerType = typeof(StaticActionInvoker<,,,,>).MakeGenericType(parameters[0].ParameterType, parameters[1].ParameterType, parameters[2].ParameterType, parameters[3].ParameterType, parameters[4].ParameterType);
							}
							else {
								throw new NotSupportedException();
							}
						}
						else {
							if(parameters.Length == 0) {
								invokerType = typeof(InstanceActionInvoker<>).MakeGenericType(methodInfo.DeclaringType);
							}
							else if(parameters.Length == 1) {
								invokerType = typeof(InstanceActionInvoker<,>).MakeGenericType(methodInfo.DeclaringType, parameters[0].ParameterType);
							}
							else if(parameters.Length == 2) {
								invokerType = typeof(InstanceActionInvoker<,,>).MakeGenericType(methodInfo.DeclaringType, parameters[0].ParameterType, parameters[1].ParameterType);
							}
							else if(parameters.Length == 3) {
								invokerType = typeof(InstanceActionInvoker<,,,>).MakeGenericType(methodInfo.DeclaringType, parameters[0].ParameterType, parameters[1].ParameterType, parameters[2].ParameterType);
							}
							else if(parameters.Length == 4) {
								invokerType = typeof(InstanceActionInvoker<,,,,>).MakeGenericType(methodInfo.DeclaringType, parameters[0].ParameterType, parameters[1].ParameterType, parameters[2].ParameterType, parameters[3].ParameterType);
							}
							else if(parameters.Length == 5) {
								invokerType = typeof(InstanceActionInvoker<,,,,,>).MakeGenericType(methodInfo.DeclaringType, parameters[0].ParameterType, parameters[1].ParameterType, parameters[2].ParameterType, parameters[3].ParameterType, parameters[4].ParameterType);
							}
							else {
								throw new NotSupportedException();
							}
						}
					}
					else {
						if(methodInfo.IsStatic) {
							if(parameters.Length == 0) {
								invokerType = typeof(StaticFuncInvoker<>).MakeGenericType(methodInfo.ReturnType);
							}
							else if(parameters.Length == 1) {
								invokerType = typeof(StaticFuncInvoker<,>).MakeGenericType(parameters[0].ParameterType, methodInfo.ReturnType);
							}
							else if(parameters.Length == 2) {
								invokerType = typeof(StaticFuncInvoker<,,>).MakeGenericType(parameters[0].ParameterType, parameters[1].ParameterType, methodInfo.ReturnType);
							}
							else if(parameters.Length == 3) {
								invokerType = typeof(StaticFuncInvoker<,,,>).MakeGenericType(parameters[0].ParameterType, parameters[1].ParameterType, parameters[2].ParameterType, methodInfo.ReturnType);
							}
							else if(parameters.Length == 4) {
								invokerType = typeof(StaticFuncInvoker<,,,,>).MakeGenericType(parameters[0].ParameterType, parameters[1].ParameterType, parameters[2].ParameterType, parameters[3].ParameterType, methodInfo.ReturnType);
							}
							else if(parameters.Length == 5) {
								invokerType = typeof(StaticFuncInvoker<,,,,,>).MakeGenericType(parameters[0].ParameterType, parameters[1].ParameterType, parameters[2].ParameterType, parameters[3].ParameterType, parameters[4].ParameterType, methodInfo.ReturnType);
							}
							else {
								throw new NotSupportedException();
							}
						}
						else {
							if(parameters.Length == 0) {
								invokerType = typeof(InstanceFuncInvoker<,>).MakeGenericType(methodInfo.DeclaringType, methodInfo.ReturnType);
							}
							else if(parameters.Length == 1) {
								invokerType = typeof(InstanceFuncInvoker<,,>).MakeGenericType(methodInfo.DeclaringType, parameters[0].ParameterType, methodInfo.ReturnType);
							}
							else if(parameters.Length == 2) {
								invokerType = typeof(InstanceFuncInvoker<,,,>).MakeGenericType(methodInfo.DeclaringType, parameters[0].ParameterType, parameters[1].ParameterType, methodInfo.ReturnType);
							}
							else if(parameters.Length == 3) {
								invokerType = typeof(InstanceFuncInvoker<,,,,>).MakeGenericType(methodInfo.DeclaringType, parameters[0].ParameterType, parameters[1].ParameterType, parameters[2].ParameterType, methodInfo.ReturnType);
							}
							else if(parameters.Length == 4) {
								invokerType = typeof(InstanceFuncInvoker<,,,,,>).MakeGenericType(methodInfo.DeclaringType, parameters[0].ParameterType, parameters[1].ParameterType, parameters[2].ParameterType, parameters[3].ParameterType, methodInfo.ReturnType);
							}
							else if(parameters.Length == 5) {
								invokerType = typeof(InstanceFuncInvoker<,,,,,,>).MakeGenericType(methodInfo.DeclaringType, parameters[0].ParameterType, parameters[1].ParameterType, parameters[2].ParameterType, parameters[3].ParameterType, parameters[4].ParameterType, methodInfo.ReturnType);
							}
							else {
								throw new NotSupportedException();
							}
						}
					}
					method = Activator.CreateInstance(invokerType, methodInfo) as MethodInvoker;
				}
				else {
					method = new ReflectionInvoker(methodInfo);
				}
				optimizedMethods[methodInfo] = method;
			}
			return method;
		}
		#endregion
	}

	#region MethodInvoker
	public abstract class MethodInvoker {
		public readonly MethodInfo methodInfo;

		public MethodInvoker(MethodInfo methodInfo) {
			this.methodInfo = methodInfo;
			Initialize();
		}

		protected abstract void Initialize();
#if UNITY_2022_2_OR_NEWER
		[UnityEngine.HideInCallstack]
#endif
		public abstract object Invoke(object target, params object[] parameters);
	}

	internal class ReflectionInvoker : MethodInvoker {
		public ReflectionInvoker(MethodInfo methodInfo) : base(methodInfo) {
		}

		protected override void Initialize() {

		}

#if UNITY_2022_2_OR_NEWER
		[UnityEngine.HideInCallstack]
#endif
		public override object Invoke(object target, params object[] parameters) {
			return methodInfo.Invoke(target, parameters);
		}
	}

	internal abstract class InstanceExpressonInvoker<TTarget> : ExpressonInvoker {
		protected InstanceExpressonInvoker(MethodInfo methodInfo) : base(methodInfo) { }

		protected override void Initialize() {
			var targetExpression = Expression.Parameter(typeof(TTarget), "target");
			var parameterExpressions = GetParameterExpressions();
			var parameterExpressionsIncludingTarget = new ParameterExpression[1 + parameterExpressions.Length];
			parameterExpressionsIncludingTarget[0] = targetExpression;
			Array.Copy(parameterExpressions, 0, parameterExpressionsIncludingTarget, 1, parameterExpressions.Length);

			var callExpression = Expression.Call(targetExpression, methodInfo, parameterExpressions);

			CompileExpression(callExpression, parameterExpressionsIncludingTarget);
		}
	}

	internal abstract class ExpressonInvoker : MethodInvoker {
		protected ExpressonInvoker(MethodInfo methodInfo) : base(methodInfo) { }

		protected override void Initialize() {
			var parameterExpressions = GetParameterExpressions();
			var callExpression = Expression.Call(methodInfo, parameterExpressions);

			CompileExpression(callExpression, parameterExpressions);
		}

		protected ParameterExpression[] GetParameterExpressions() {
			var parameterTypes = GetParameterTypes();
			var parameterExpressions = new ParameterExpression[parameterTypes.Length];
			for(var i = 0; i < parameterTypes.Length; i++) {
				parameterExpressions[i] = Expression.Parameter(parameterTypes[i], "parameter" + i);
			}
			return parameterExpressions;
		}

		protected abstract void CompileExpression(MethodCallExpression callExpression, ParameterExpression[] parameterExpressions);

		protected abstract Type[] GetParameterTypes();
	}
	#endregion

	#region InstanceActionInvoker
	internal class InstanceActionInvoker<TTarget> : InstanceExpressonInvoker<TTarget> {
		private Action<TTarget> invoker;

		public InstanceActionInvoker(MethodInfo methodInfo) : base(methodInfo) { }

#if UNITY_2022_2_OR_NEWER
		[UnityEngine.HideInCallstack]
#endif
		public override object Invoke(object target, params object[] parameters) {
			invoker(target.ConvertTo<TTarget>());
			return null;
		}

		protected override void CompileExpression(MethodCallExpression callExpression, ParameterExpression[] parameterExpressions) {
			invoker = Expression.Lambda<Action<TTarget>>(callExpression, parameterExpressions).Compile();
		}

		protected override Type[] GetParameterTypes() {
			return Type.EmptyTypes;
		}
	}

	internal class InstanceActionInvoker<TTarget, T> : InstanceExpressonInvoker<TTarget> {
		private Action<TTarget, T> invoker;

		public InstanceActionInvoker(MethodInfo methodInfo) : base(methodInfo) { }

#if UNITY_2022_2_OR_NEWER
		[UnityEngine.HideInCallstack]
#endif
		public override object Invoke(object target, params object[] parameters) {
			invoker(target.ConvertTo<TTarget>(), parameters[0].ConvertTo<T>());
			return null;
		}

		protected override void CompileExpression(MethodCallExpression callExpression, ParameterExpression[] parameterExpressions) {
			invoker = Expression.Lambda<Action<TTarget, T>>(callExpression, parameterExpressions).Compile();
		}

		protected override Type[] GetParameterTypes() {
			return new[] { typeof(T) };
		}
	}

	internal class InstanceActionInvoker<TTarget, T1, T2> : InstanceExpressonInvoker<TTarget> {
		private Action<TTarget, T1, T2> invoker;

		public InstanceActionInvoker(MethodInfo methodInfo) : base(methodInfo) { }

#if UNITY_2022_2_OR_NEWER
		[UnityEngine.HideInCallstack]
#endif
		public override object Invoke(object target, params object[] parameters) {
			invoker(
				target.ConvertTo<TTarget>(),
				parameters[0].ConvertTo<T1>(),
				parameters[1].ConvertTo<T2>()
			);
			return null;
		}

		protected override void CompileExpression(MethodCallExpression callExpression, ParameterExpression[] parameterExpressions) {
			invoker = Expression.Lambda<Action<TTarget, T1, T2>>(callExpression, parameterExpressions).Compile();
		}

		protected override Type[] GetParameterTypes() {
			return new[] { typeof(T1), typeof(T2) };
		}
	}

	internal class InstanceActionInvoker<TTarget, T1, T2, T3> : InstanceExpressonInvoker<TTarget> {
		private Action<TTarget, T1, T2, T3> invoker;

		public InstanceActionInvoker(MethodInfo methodInfo) : base(methodInfo) { }

#if UNITY_2022_2_OR_NEWER
		[UnityEngine.HideInCallstack]
#endif
		public override object Invoke(object target, params object[] parameters) {
			invoker(
				target.ConvertTo<TTarget>(),
				parameters[0].ConvertTo<T1>(),
				parameters[1].ConvertTo<T2>(),
				parameters[2].ConvertTo<T3>()
			);
			return null;
		}

		protected override void CompileExpression(MethodCallExpression callExpression, ParameterExpression[] parameterExpressions) {
			invoker = Expression.Lambda<Action<TTarget, T1, T2, T3>>(callExpression, parameterExpressions).Compile();
		}

		protected override Type[] GetParameterTypes() {
			return new[] { typeof(T1), typeof(T2), typeof(T3) };
		}
	}

	internal class InstanceActionInvoker<TTarget, T1, T2, T3, T4> : InstanceExpressonInvoker<TTarget> {
		private Action<TTarget, T1, T2, T3, T4> invoker;

		public InstanceActionInvoker(MethodInfo methodInfo) : base(methodInfo) { }

#if UNITY_2022_2_OR_NEWER
		[UnityEngine.HideInCallstack]
#endif
		public override object Invoke(object target, params object[] parameters) {
			invoker(
				target.ConvertTo<TTarget>(),
				parameters[0].ConvertTo<T1>(),
				parameters[1].ConvertTo<T2>(),
				parameters[2].ConvertTo<T3>(),
				parameters[3].ConvertTo<T4>()
			);
			return null;
		}

		protected override void CompileExpression(MethodCallExpression callExpression, ParameterExpression[] parameterExpressions) {
			invoker = Expression.Lambda<Action<TTarget, T1, T2, T3, T4>>(callExpression, parameterExpressions).Compile();
		}

		protected override Type[] GetParameterTypes() {
			return new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4) };
		}
	}

	internal class InstanceActionInvoker<TTarget, T1, T2, T3, T4, T5> : InstanceExpressonInvoker<TTarget> {
		private Action<TTarget, T1, T2, T3, T4, T5> invoker;

		public InstanceActionInvoker(MethodInfo methodInfo) : base(methodInfo) { }

#if UNITY_2022_2_OR_NEWER
		[UnityEngine.HideInCallstack]
#endif
		public override object Invoke(object target, params object[] parameters) {
			invoker(
				target.ConvertTo<TTarget>(),
				parameters[0].ConvertTo<T1>(),
				parameters[1].ConvertTo<T2>(),
				parameters[2].ConvertTo<T3>(),
				parameters[3].ConvertTo<T4>(),
				parameters[4].ConvertTo<T5>()
			);
			return null;
		}

		protected override void CompileExpression(MethodCallExpression callExpression, ParameterExpression[] parameterExpressions) {
			invoker = Expression.Lambda<Action<TTarget, T1, T2, T3, T4, T5>>(callExpression, parameterExpressions).Compile();
		}

		protected override Type[] GetParameterTypes() {
			return new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5) };
		}
	}
	#endregion

	#region InstanceFuncInvoker
	internal class InstanceFuncInvoker<TTarget, TResult> : InstanceExpressonInvoker<TTarget> {
		private Func<TTarget, TResult> invoker;

		public InstanceFuncInvoker(MethodInfo methodInfo) : base(methodInfo) { }

#if UNITY_2022_2_OR_NEWER
		[UnityEngine.HideInCallstack]
#endif
		public override object Invoke(object target, params object[] parameters) {
			return invoker(target.ConvertTo<TTarget>());
		}

		protected override void CompileExpression(MethodCallExpression callExpression, ParameterExpression[] parameterExpressions) {
			invoker = Expression.Lambda<Func<TTarget, TResult>>(callExpression, parameterExpressions).Compile();
		}

		protected override Type[] GetParameterTypes() {
			return Type.EmptyTypes;
		}
	}

	internal class InstanceFuncInvoker<TTarget, T, TResult> : InstanceExpressonInvoker<TTarget> {
		private Func<TTarget, T, TResult> invoker;

		public InstanceFuncInvoker(MethodInfo methodInfo) : base(methodInfo) { }

#if UNITY_2022_2_OR_NEWER
		[UnityEngine.HideInCallstack]
#endif
		public override object Invoke(object target, params object[] parameters) {
			return invoker(target.ConvertTo<TTarget>(), parameters[0].ConvertTo<T>());
		}

		protected override void CompileExpression(MethodCallExpression callExpression, ParameterExpression[] parameterExpressions) {
			invoker = Expression.Lambda<Func<TTarget, T, TResult>>(callExpression, parameterExpressions).Compile();
		}

		protected override Type[] GetParameterTypes() {
			return new[] { typeof(T) };
		}
	}

	internal class InstanceFuncInvoker<TTarget, T1, T2, TResult> : InstanceExpressonInvoker<TTarget> {
		private Func<TTarget, T1, T2, TResult> invoker;

		public InstanceFuncInvoker(MethodInfo methodInfo) : base(methodInfo) { }

#if UNITY_2022_2_OR_NEWER
		[UnityEngine.HideInCallstack]
#endif
		public override object Invoke(object target, params object[] parameters) {
			return invoker(
				target.ConvertTo<TTarget>(),
				parameters[0].ConvertTo<T1>(),
				parameters[1].ConvertTo<T2>()
			);
		}

		protected override void CompileExpression(MethodCallExpression callExpression, ParameterExpression[] parameterExpressions) {
			invoker = Expression.Lambda<Func<TTarget, T1, T2, TResult>>(callExpression, parameterExpressions).Compile();
		}

		protected override Type[] GetParameterTypes() {
			return new[] { typeof(T1), typeof(T2) };
		}
	}

	internal class InstanceFuncInvoker<TTarget, T1, T2, T3, TResult> : InstanceExpressonInvoker<TTarget> {
		private Func<TTarget, T1, T2, T3, TResult> invoker;

		public InstanceFuncInvoker(MethodInfo methodInfo) : base(methodInfo) { }

#if UNITY_2022_2_OR_NEWER
		[UnityEngine.HideInCallstack]
#endif
		public override object Invoke(object target, params object[] parameters) {
			return invoker(
				target.ConvertTo<TTarget>(),
				parameters[0].ConvertTo<T1>(),
				parameters[1].ConvertTo<T2>(),
				parameters[2].ConvertTo<T3>()
			);
		}

		protected override void CompileExpression(MethodCallExpression callExpression, ParameterExpression[] parameterExpressions) {
			invoker = Expression.Lambda<Func<TTarget, T1, T2, T3, TResult>>(callExpression, parameterExpressions).Compile();
		}

		protected override Type[] GetParameterTypes() {
			return new[] { typeof(T1), typeof(T2), typeof(T3) };
		}
	}

	internal class InstanceFuncInvoker<TTarget, T1, T2, T3, T4, TResult> : InstanceExpressonInvoker<TTarget> {
		private Func<TTarget, T1, T2, T3, T4, TResult> invoker;

		public InstanceFuncInvoker(MethodInfo methodInfo) : base(methodInfo) { }

#if UNITY_2022_2_OR_NEWER
		[UnityEngine.HideInCallstack]
#endif
		public override object Invoke(object target, params object[] parameters) {
			return invoker(
				target.ConvertTo<TTarget>(),
				parameters[0].ConvertTo<T1>(),
				parameters[1].ConvertTo<T2>(),
				parameters[2].ConvertTo<T3>(),
				parameters[3].ConvertTo<T4>()
			);
		}

		protected override void CompileExpression(MethodCallExpression callExpression, ParameterExpression[] parameterExpressions) {
			invoker = Expression.Lambda<Func<TTarget, T1, T2, T3, T4, TResult>>(callExpression, parameterExpressions).Compile();
		}

		protected override Type[] GetParameterTypes() {
			return new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4) };
		}
	}

	internal class InstanceFuncInvoker<TTarget, T1, T2, T3, T4, T5, TResult> : InstanceExpressonInvoker<TTarget> {
		private Func<TTarget, T1, T2, T3, T4, T5, TResult> invoker;

		public InstanceFuncInvoker(MethodInfo methodInfo) : base(methodInfo) { }

#if UNITY_2022_2_OR_NEWER
		[UnityEngine.HideInCallstack]
#endif
		public override object Invoke(object target, params object[] parameters) {
			return invoker(
				target.ConvertTo<TTarget>(),
				parameters[0].ConvertTo<T1>(),
				parameters[1].ConvertTo<T2>(),
				parameters[2].ConvertTo<T3>(),
				parameters[3].ConvertTo<T4>(),
				parameters[4].ConvertTo<T5>()
			);
		}

		protected override void CompileExpression(MethodCallExpression callExpression, ParameterExpression[] parameterExpressions) {
			invoker = Expression.Lambda<Func<TTarget, T1, T2, T3, T4, T5, TResult>>(callExpression, parameterExpressions).Compile();
		}

		protected override Type[] GetParameterTypes() {
			return new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5) };
		}
	}
	#endregion

	#region StaticActionInvoker
	internal class StaticActionInvoker : ExpressonInvoker {
		private Action invoker;

		public StaticActionInvoker(MethodInfo methodInfo) : base(methodInfo) { }

#if UNITY_2022_2_OR_NEWER
		[UnityEngine.HideInCallstack]
#endif
		public override object Invoke(object target, params object[] parameters) {
			invoker();
			return null;
		}

		protected override void CompileExpression(MethodCallExpression callExpression, ParameterExpression[] parameterExpressions) {
			invoker = Expression.Lambda<Action>(callExpression, parameterExpressions).Compile();
		}

		protected override Type[] GetParameterTypes() {
			return Type.EmptyTypes;
		}
	}

	internal class StaticActionInvoker<T> : MethodInvoker {
		private Action<T> invoker;

		public StaticActionInvoker(MethodInfo methodInfo) : base(methodInfo) { }

#if UNITY_2022_2_OR_NEWER
		[UnityEngine.HideInCallstack]
#endif
		public override object Invoke(object target, params object[] parameters) {
			try {
				invoker(parameters[0].ConvertTo<T>());
			}
			catch(Exception ex) {
				throw new Exception("Error invoking: " + methodInfo, ex);
			}
			return null;
		}

		protected override void Initialize() {
			invoker = (Action<T>)Delegate.CreateDelegate(typeof(Action<T>), methodInfo);
		}
	}

	internal class StaticActionInvoker<T1, T2> : ExpressonInvoker {
		private Action<T1, T2> invoker;

		public StaticActionInvoker(MethodInfo methodInfo) : base(methodInfo) { }

#if UNITY_2022_2_OR_NEWER
		[UnityEngine.HideInCallstack]
#endif
		public override object Invoke(object target, params object[] parameters) {
			invoker(parameters[0].ConvertTo<T1>(), parameters[1].ConvertTo<T2>());
			return null;
		}

		protected override void CompileExpression(MethodCallExpression callExpression, ParameterExpression[] parameterExpressions) {
			invoker = Expression.Lambda<Action<T1, T2>>(callExpression, parameterExpressions).Compile();
		}

		protected override Type[] GetParameterTypes() {
			return new[] { typeof(T1), typeof(T2) };
		}
	}

	internal class StaticActionInvoker<T1, T2, T3> : ExpressonInvoker {
		private Action<T1, T2, T3> invoker;

		public StaticActionInvoker(MethodInfo methodInfo) : base(methodInfo) { }

#if UNITY_2022_2_OR_NEWER
		[UnityEngine.HideInCallstack]
#endif
		public override object Invoke(object target, params object[] parameters) {
			invoker(parameters[0].ConvertTo<T1>(), parameters[1].ConvertTo<T2>(), parameters[2].ConvertTo<T3>());
			return null;
		}

		protected override void CompileExpression(MethodCallExpression callExpression, ParameterExpression[] parameterExpressions) {
			invoker = Expression.Lambda<Action<T1, T2, T3>>(callExpression, parameterExpressions).Compile();
		}

		protected override Type[] GetParameterTypes() {
			return new[] { typeof(T1), typeof(T2), typeof(T3) };
		}
	}

	internal class StaticActionInvoker<T1, T2, T3, T4> : ExpressonInvoker {
		private Action<T1, T2, T3, T4> invoker;

		public StaticActionInvoker(MethodInfo methodInfo) : base(methodInfo) { }

#if UNITY_2022_2_OR_NEWER
		[UnityEngine.HideInCallstack]
#endif
		public override object Invoke(object target, params object[] parameters) {
			invoker(parameters[0].ConvertTo<T1>(), parameters[1].ConvertTo<T2>(), parameters[2].ConvertTo<T3>(), parameters[3].ConvertTo<T4>());
			return null;
		}

		protected override void CompileExpression(MethodCallExpression callExpression, ParameterExpression[] parameterExpressions) {
			invoker = Expression.Lambda<Action<T1, T2, T3, T4>>(callExpression, parameterExpressions).Compile();
		}

		protected override Type[] GetParameterTypes() {
			return new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4) };
		}
	}

	internal class StaticActionInvoker<T1, T2, T3, T4, T5> : ExpressonInvoker {
		private Action<T1, T2, T3, T4, T5> invoker;

		public StaticActionInvoker(MethodInfo methodInfo) : base(methodInfo) { }

#if UNITY_2022_2_OR_NEWER
		[UnityEngine.HideInCallstack]
#endif
		public override object Invoke(object target, params object[] parameters) {
			invoker(parameters[0].ConvertTo<T1>(), parameters[1].ConvertTo<T2>(), parameters[2].ConvertTo<T3>(), parameters[3].ConvertTo<T4>(), parameters[4].ConvertTo<T5>());
			return null;
		}

		protected override void CompileExpression(MethodCallExpression callExpression, ParameterExpression[] parameterExpressions) {
			invoker = Expression.Lambda<Action<T1, T2, T3, T4, T5>>(callExpression, parameterExpressions).Compile();
		}

		protected override Type[] GetParameterTypes() {
			return new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5) };
		}
	}
	#endregion

	#region StaticFuncInvoker
	internal class StaticFuncInvoker<TResult> : ExpressonInvoker {
		private Func<TResult> invoker;

		public StaticFuncInvoker(MethodInfo methodInfo) : base(methodInfo) { }

#if UNITY_2022_2_OR_NEWER
		[UnityEngine.HideInCallstack]
#endif
		public override object Invoke(object target, params object[] parameters) {
			return invoker();
		}

		protected override void CompileExpression(MethodCallExpression callExpression, ParameterExpression[] parameterExpressions) {
			invoker = Expression.Lambda<Func<TResult>>(callExpression, parameterExpressions).Compile();
		}

		protected override Type[] GetParameterTypes() {
			return Type.EmptyTypes;
		}
	}

	internal class StaticFuncInvoker<T, TResult> : ExpressonInvoker {
		private Func<T, TResult> invoker;

		public StaticFuncInvoker(MethodInfo methodInfo) : base(methodInfo) { }

#if UNITY_2022_2_OR_NEWER
		[UnityEngine.HideInCallstack]
#endif
		public override object Invoke(object target, params object[] parameters) {
			return invoker(parameters[0].ConvertTo<T>());
		}

		protected override void CompileExpression(MethodCallExpression callExpression, ParameterExpression[] parameterExpressions) {
			invoker = Expression.Lambda<Func<T, TResult>>(callExpression, parameterExpressions).Compile();
		}

		protected override Type[] GetParameterTypes() {
			return new[] { typeof(T) };
		}
	}

	internal class StaticFuncInvoker<T1, T2, TResult> : ExpressonInvoker {
		private Func<T1, T2, TResult> invoker;

		public StaticFuncInvoker(MethodInfo methodInfo) : base(methodInfo) { }

#if UNITY_2022_2_OR_NEWER
		[UnityEngine.HideInCallstack]
#endif
		public override object Invoke(object target, params object[] parameters) {
			return invoker(
				parameters[0].ConvertTo<T1>(),
				parameters[1].ConvertTo<T2>()
			);
		}

		protected override void CompileExpression(MethodCallExpression callExpression, ParameterExpression[] parameterExpressions) {
			invoker = Expression.Lambda<Func<T1, T2, TResult>>(callExpression, parameterExpressions).Compile();
		}

		protected override Type[] GetParameterTypes() {
			return new[] { typeof(T1), typeof(T2) };
		}
	}

	internal class StaticFuncInvoker<T1, T2, T3, TResult> : ExpressonInvoker {
		private Func<T1, T2, T3, TResult> invoker;

		public StaticFuncInvoker(MethodInfo methodInfo) : base(methodInfo) { }

#if UNITY_2022_2_OR_NEWER
		[UnityEngine.HideInCallstack]
#endif
		public override object Invoke(object target, params object[] parameters) {
			return invoker(
				parameters[0].ConvertTo<T1>(),
				parameters[1].ConvertTo<T2>(),
				parameters[2].ConvertTo<T3>()
			);
		}

		protected override void CompileExpression(MethodCallExpression callExpression, ParameterExpression[] parameterExpressions) {
			invoker = Expression.Lambda<Func<T1, T2, T3, TResult>>(callExpression, parameterExpressions).Compile();
		}

		protected override Type[] GetParameterTypes() {
			return new[] { typeof(T1), typeof(T2), typeof(T3) };
		}
	}

	internal class StaticFuncInvoker<T1, T2, T3, T4, TResult> : ExpressonInvoker {
		private Func<T1, T2, T3, T4, TResult> invoker;

		public StaticFuncInvoker(MethodInfo methodInfo) : base(methodInfo) { }

#if UNITY_2022_2_OR_NEWER
		[UnityEngine.HideInCallstack]
#endif
		public override object Invoke(object target, params object[] parameters) {
			return invoker(
				parameters[0].ConvertTo<T1>(),
				parameters[1].ConvertTo<T2>(),
				parameters[2].ConvertTo<T3>(),
				parameters[3].ConvertTo<T4>()
			);
		}

		protected override void CompileExpression(MethodCallExpression callExpression, ParameterExpression[] parameterExpressions) {
			invoker = Expression.Lambda<Func<T1, T2, T3, T4, TResult>>(callExpression, parameterExpressions).Compile();
		}

		protected override Type[] GetParameterTypes() {
			return new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4) };
		}
	}

	internal class StaticFuncInvoker<T1, T2, T3, T4, T5, TResult> : ExpressonInvoker {
		private Func<T1, T2, T3, T4, T5, TResult> invoker;

		public StaticFuncInvoker(MethodInfo methodInfo) : base(methodInfo) { }

#if UNITY_2022_2_OR_NEWER
		[UnityEngine.HideInCallstack]
#endif
		public override object Invoke(object target, params object[] parameters) {
			return invoker(
				parameters[0].ConvertTo<T1>(),
				parameters[1].ConvertTo<T2>(),
				parameters[2].ConvertTo<T3>(),
				parameters[3].ConvertTo<T4>(),
				parameters[4].ConvertTo<T5>()
			);
		}

		protected override void CompileExpression(MethodCallExpression callExpression, ParameterExpression[] parameterExpressions) {
			invoker = Expression.Lambda<Func<T1, T2, T3, T4, T5, TResult>>(callExpression, parameterExpressions).Compile();
		}

		protected override Type[] GetParameterTypes() {
			return new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5) };
		}
	}
	#endregion

	#region FieldAccessor
	public abstract class FieldAccessor {
		public readonly FieldInfo fieldInfo;

		protected FieldAccessor(FieldInfo fieldInfo) {
			this.fieldInfo = fieldInfo;
		}

#if UNITY_2022_2_OR_NEWER
		[UnityEngine.HideInCallstack]
#endif
		public abstract object GetValue(object target);
#if UNITY_2022_2_OR_NEWER
		[UnityEngine.HideInCallstack]
#endif
		public abstract void SetValue(object target, object value);
	}

	internal class ReflectionFieldAccessor : FieldAccessor {
		public ReflectionFieldAccessor(FieldInfo fieldInfo) : base(fieldInfo) { }

#if UNITY_2022_2_OR_NEWER
		[UnityEngine.HideInCallstack]
#endif
		public override object GetValue(object target) {
			return fieldInfo.GetValue(target);
		}

#if UNITY_2022_2_OR_NEWER
		[UnityEngine.HideInCallstack]
#endif
		public override void SetValue(object target, object value) {
			fieldInfo.SetValue(target, value);
		}
	}

	internal class StaticFieldAccessor<T> : FieldAccessor {
		private Func<T> getter;
		private Action<T> setter;

		public StaticFieldAccessor(FieldInfo fieldInfo) : base(fieldInfo) {
			if(fieldInfo.IsLiteral) {
				var constant = (T)fieldInfo.GetValue(null);
				getter = () => constant;
			}
			else if(ReflectionOptimization.isAOT) {
				getter = () => (T)fieldInfo.GetValue(null);
				setter = (value) => fieldInfo.SetValue(null, value);
			}
			else {
				var fieldExpression = Expression.Field(null, fieldInfo);
				getter = Expression.Lambda<Func<T>>(fieldExpression).Compile();
				if(!fieldInfo.IsInitOnly) {
					var valueExpression = Expression.Parameter(typeof(T));
					var assignExpression = Expression.Assign(fieldExpression, valueExpression);
					setter = Expression.Lambda<Action<T>>(assignExpression, valueExpression).Compile();
				}
			}
		}

#if UNITY_2022_2_OR_NEWER
		[UnityEngine.HideInCallstack]
#endif
		public override object GetValue(object target) {
			return getter();
		}

#if UNITY_2022_2_OR_NEWER
		[UnityEngine.HideInCallstack]
#endif
		public override void SetValue(object target, object value) {
			setter(value.ConvertTo<T>());
		}
	}

	internal class InstanceFieldAccessor<TTarget, T> : FieldAccessor {
		private Func<TTarget, T> getter;
		private Action<TTarget, T> setter;

		public InstanceFieldAccessor(FieldInfo fieldInfo) : base(fieldInfo) {
			if(ReflectionOptimization.isAOT) {
				getter = (obj) => (T)fieldInfo.GetValue(obj);
				setter = (obj, value) => fieldInfo.SetValue(obj, value);
			}
			else {
				var targetExpression = Expression.Parameter(typeof(TTarget), "target");
				var fieldExpression = Expression.Field(targetExpression, fieldInfo);
				getter = Expression.Lambda<Func<TTarget, T>>(fieldExpression, targetExpression).Compile();

				if(!fieldInfo.IsInitOnly) {
					var valueExpression = Expression.Parameter(typeof(T));
					var assignExpression = Expression.Assign(fieldExpression, valueExpression);
					setter = Expression.Lambda<Action<TTarget, T>>(assignExpression, targetExpression, valueExpression).Compile();
				}
			}
		}

#if UNITY_2022_2_OR_NEWER
		[UnityEngine.HideInCallstack]
#endif
		public override object GetValue(object target) {
			return getter(target.ConvertTo<TTarget>());
		}

#if UNITY_2022_2_OR_NEWER
		[UnityEngine.HideInCallstack]
#endif
		public override void SetValue(object target, object value) {
			setter(target.ConvertTo<TTarget>(), value.ConvertTo<T>());
		}
	}
	#endregion

	#region Propertie
	public abstract class PropertyAccessor {
		public readonly PropertyInfo propertyInfo;
		protected PropertyAccessor(PropertyInfo propertyInfo) {
			this.propertyInfo = propertyInfo;
		}

#if UNITY_2022_2_OR_NEWER
		[UnityEngine.HideInCallstack]
#endif
		public abstract object GetValue(object target);
#if UNITY_2022_2_OR_NEWER
		[UnityEngine.HideInCallstack]
#endif
		public abstract void SetValue(object target, object value);
	}

	internal class StaticPropertyAccessor<T> : PropertyAccessor {
		private Func<T> getter;
		private Action<T> setter;

		public StaticPropertyAccessor(PropertyInfo propertyInfo) : base(propertyInfo) {
			var getterInfo = propertyInfo.GetGetMethod(true);
			var setterInfo = propertyInfo.GetSetMethod(true);

			//if(ReflectionOptimization.isAOT) {
			if(getterInfo != null) {
				getter = (Func<T>)getterInfo.CreateDelegate(typeof(Func<T>));
			}
			if(setterInfo != null) {
				setter = (Action<T>)setterInfo.CreateDelegate(typeof(Action<T>));
			}
			//} else {
			//	if(getterInfo != null) {
			//		var propertyExpression = Expression.Property(null, propertyInfo);
			//		getter = Expression.Lambda<Func<T>>(propertyExpression).Compile();
			//	}

			//	if(setterInfo != null) {
			//		setter = (Action<T>)setterInfo.CreateDelegate(typeof(Action<T>));
			//	}
			//}
		}

#if UNITY_2022_2_OR_NEWER
		[UnityEngine.HideInCallstack]
#endif
		public override object GetValue(object target) {
			return getter();
		}

#if UNITY_2022_2_OR_NEWER
		[UnityEngine.HideInCallstack]
#endif
		public override void SetValue(object target, object value) {
			setter(value.ConvertTo<T>());
		}
	}

	internal class InstancePropertyAccessor<TTarget, T> : PropertyAccessor {
		private Func<TTarget, T> getter;
		private Action<TTarget, T> setter;

		public InstancePropertyAccessor(PropertyInfo propertyInfo) : base(propertyInfo) {
			var getterInfo = propertyInfo.GetGetMethod(true);
			var setterInfo = propertyInfo.GetSetMethod(true);

			//if(ReflectionOptimization.isAOT) {
			if(getterInfo != null) {
				getter = (Func<TTarget, T>)getterInfo.CreateDelegate(typeof(Func<TTarget, T>));
			}
			if(setterInfo != null) {
				setter = (Action<TTarget, T>)setterInfo.CreateDelegate(typeof(Action<TTarget, T>));
			}
			//} else {
			//	var targetExpression = Expression.Parameter(typeof(TTarget), "target");
			//	if(getterInfo != null) {
			//		var propertyExpression = Expression.Property(targetExpression, propertyInfo);
			//		getter = Expression.Lambda<Func<TTarget, T>>(propertyExpression, targetExpression).Compile();
			//	}
			//	if(setterInfo != null) {
			//		setter = (Action<TTarget, T>)setterInfo.CreateDelegate(typeof(Action<TTarget, T>));
			//	}
			//}
		}

#if UNITY_2022_2_OR_NEWER
		[UnityEngine.HideInCallstack]
#endif
		public override object GetValue(object target) {
			return getter(target.ConvertTo<TTarget>());
		}

#if UNITY_2022_2_OR_NEWER
		[UnityEngine.HideInCallstack]
#endif
		public override void SetValue(object target, object value) {
			setter(target.ConvertTo<TTarget>(), value.ConvertTo<T>());
		}
	}

	internal class ReflectionPropertyAccessor : PropertyAccessor {
		public ReflectionPropertyAccessor(PropertyInfo propertyInfo) : base(propertyInfo) {

		}

#if UNITY_2022_2_OR_NEWER
		[UnityEngine.HideInCallstack]
#endif
		public override object GetValue(object target) {
			return propertyInfo.GetValue(target, null);
		}

#if UNITY_2022_2_OR_NEWER
		[UnityEngine.HideInCallstack]
#endif
		public override void SetValue(object target, object value) {
			propertyInfo.SetValue(target, value);
		}
	}
	#endregion
}