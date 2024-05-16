using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace MaxyGames {
	public static class CustomDelegate {
		/// <summary>
		/// Creates a new Action Delegate with or without parameters
		/// </summary>
		/// <param name="body"></param>
		/// <param name="types"></param>
		/// <returns></returns>
		public static Delegate CreateActionDelegate(Action<object[]> body, params Type[] types) {
			return ActionHelper.CreateAction(body, types);
		}

		/// <summary>
		/// Creates new Func Delegate, the return type is the last type in the 'types' parameter
		/// </summary>
		/// <param name="body"></param>
		/// <param name="types"></param>
		/// <returns></returns>
		public static Delegate CreateFuncDelegate(Func<object[], object> body, params Type[] types) {
			return FuncHelper.CreateFunc(body, types);
		}

		public static Type GetActionDelegateType(params Type[] types) {
			return Expression.GetActionType(types);
			//return ActionHelper.GetActionType(types);
		}

		public static Type GetFuncDelegateType(params Type[] types) {
			return Expression.GetFuncType(types);
			//return FuncHelper.GetFuncType(types);
		}

		public static Delegate CreateDelegate(MethodInfo methodInfo, object target) {
			Func<Type[], Type> getType;
			var isAction = methodInfo.ReturnType.Equals((typeof(void)));
			var types = methodInfo.GetParameters().Select(p => p.ParameterType);

			if(isAction) {
				getType = Expression.GetActionType;
			}
			else {
				getType = Expression.GetFuncType;
				types = types.Concat(new[] { methodInfo.ReturnType });
			}

			if(methodInfo.IsStatic) {
				return Delegate.CreateDelegate(getType(types.ToArray()), methodInfo);
			}

			return Delegate.CreateDelegate(getType(types.ToArray()), target, methodInfo.Name);
		}
	}

	class ActionHelper {
		private static readonly System.Type[] actionTypes = new System.Type[] { typeof(ActionHelper), typeof(ActionHelper<>), typeof(ActionHelper<,>), typeof(ActionHelper<,,>), typeof(ActionHelper<,,,>), typeof(ActionHelper<,,,,>), typeof(ActionHelper<,,,,,>), typeof(ActionHelper<,,,,,,>), typeof(ActionHelper<,,,,,,,>), typeof(ActionHelper<,,,,,,,,>), typeof(ActionHelper<,,,,,,,,,>) };

		protected virtual Delegate CreateActionImpl(Action<object[]> body) {
			return new Action(() => body(null));
		}

		protected virtual Type Type() { return typeof(Action); }

		public static Delegate CreateAction(Action<object[]> body, params Type[] types) {
			var helperType = types.Length == 0 ? actionTypes[0] : actionTypes[types.Length].MakeGenericType(types);
			var helper = Activator.CreateInstance(helperType) as ActionHelper;
			return helper.CreateActionImpl(body);
		}

		public static Type GetActionType(params Type[] types) {
			var helperType = types.Length == 0 ? actionTypes[0] : actionTypes[types.Length].MakeGenericType(types);
			var helper = Activator.CreateInstance(helperType) as ActionHelper;
			return helper.Type();
		}
	}

	abstract class FuncHelper {
		private static readonly System.Type[] funcTypes = new System.Type[] { typeof(FuncHelper<>), typeof(FuncHelper<,>), typeof(FuncHelper<,,>), typeof(FuncHelper<,,,>), typeof(FuncHelper<,,,,>), typeof(FuncHelper<,,,,,>), typeof(FuncHelper<,,,,,,>), typeof(FuncHelper<,,,,,,,>), typeof(FuncHelper<,,,,,,,,>), typeof(FuncHelper<,,,,,,,,,>), typeof(FuncHelper<,,,,,,,,,,>) };

		protected abstract Delegate CreateFuncImpl(Func<object[], object> body);
		protected abstract Type Type();

		public static Delegate CreateFunc(Func<object[], object> body, params Type[] types) {
			var helperType = funcTypes[types.Length - 1].MakeGenericType(types);
			var helper = Activator.CreateInstance(helperType) as FuncHelper;
			return helper.CreateFuncImpl(body);
		}

		public static Type GetFuncType(params Type[] types) {
			var helperType = funcTypes[types.Length - 1].MakeGenericType(types);
			var helper = Activator.CreateInstance(helperType) as FuncHelper;
			return helper.Type();
		}
	}

	#region Func
	class FuncHelper<TResult> : FuncHelper {
		protected override Delegate CreateFuncImpl(Func<object[], object> body) {
			return new Func<TResult>(() => {
				object obj = body(null);
				if(obj == null) {
					return default(TResult);
				}
				return (TResult)obj;
			});
		}

		protected override Type Type() {
			return typeof(Func<TResult>);
		}
	}

	class FuncHelper<T, TResult> : FuncHelper {
		protected override Delegate CreateFuncImpl(Func<object[], object> body) {
			return new Func<T, TResult>(p1 => {
				object obj = body(new object[] { p1 });
				if(obj == null) {
					return default(TResult);
				}
				return (TResult)obj;
			});
		}

		protected override Type Type() {
			return typeof(Func<T, TResult>);
		}
	}

	class FuncHelper<T1, T2, TResult> : FuncHelper {
		protected override Delegate CreateFuncImpl(Func<object[], object> body) {
			return new Func<T1, T2, TResult>((p1, p2) => {
				object obj = body(new object[] { p1, p2 });
				if(obj == null) {
					return default(TResult);
				}
				return (TResult)obj;
			});
		}

		protected override Type Type() {
			return typeof(Func<T1, T2, TResult>);
		}
	}

	class FuncHelper<T1, T2, T3, TResult> : FuncHelper {
		protected override Delegate CreateFuncImpl(Func<object[], object> body) {
			return new Func<T1, T2, T3, TResult>((p1, p2, p3) => {
				object obj = body(new object[] { p1, p2, p3 });
				if(obj == null) {
					return default(TResult);
				}
				return (TResult)obj;
			});
		}

		protected override Type Type() {
			return typeof(Func<T1, T2, T3, TResult>);
		}
	}

	class FuncHelper<T1, T2, T3, T4, TResult> : FuncHelper {
		protected override Delegate CreateFuncImpl(Func<object[], object> body) {
			return new Func<T1, T2, T3, T4, TResult>((p1, p2, p3, p4) => {
				object obj = body(new object[] { p1, p2, p3, p4 });
				if(obj == null) {
					return default(TResult);
				}
				return (TResult)obj;
			});
		}

		protected override Type Type() {
			return typeof(Func<T1, T2, T3, T4, TResult>);
		}
	}

	class FuncHelper<T1, T2, T3, T4, T5, TResult> : FuncHelper {
		protected override Delegate CreateFuncImpl(Func<object[], object> body) {
			return new Func<T1, T2, T3, T4, T5, TResult>((p1, p2, p3, p4, p5) => {
				object obj = body(new object[] { p1, p2, p3, p4, p5 });
				if(obj == null) {
					return default(TResult);
				}
				return (TResult)obj;
			});
		}

		protected override Type Type() {
			return typeof(Func<T1, T2, T3, T4, T5, TResult>);
		}
	}

	class FuncHelper<T1, T2, T3, T4, T5, T6, TResult> : FuncHelper {
		protected override Delegate CreateFuncImpl(Func<object[], object> body) {
			return new Func<T1, T2, T3, T4, T5, T6, TResult>((p1, p2, p3, p4, p5, p6) => {
				object obj = body(new object[] { p1, p2, p3, p4, p5, p6 });
				if(obj == null) {
					return default(TResult);
				}
				return (TResult)obj;
			});
		}

		protected override Type Type() {
			return typeof(Func<T1, T2, T3, T4, T5, T6, TResult>);
		}
	}

	class FuncHelper<T1, T2, T3, T4, T5, T6, T7, TResult> : FuncHelper {
		protected override Delegate CreateFuncImpl(Func<object[], object> body) {
			return new Func<T1, T2, T3, T4, T5, T6, T7, TResult>((p1, p2, p3, p4, p5, p6, p7) => {
				object obj = body(new object[] { p1, p2, p3, p4, p5, p6, p7 });
				if(obj == null) {
					return default(TResult);
				}
				return (TResult)obj;
			});
		}

		protected override Type Type() {
			return typeof(Func<T1, T2, T3, T4, T5, T6, T7, TResult>);
		}
	}

	class FuncHelper<T1, T2, T3, T4, T5, T6, T7, T8, TResult> : FuncHelper {
		protected override Delegate CreateFuncImpl(Func<object[], object> body) {
			return new Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult>((p1, p2, p3, p4, p5, p6, p7, p8) => {
				object obj = body(new object[] { p1, p2, p3, p4, p5, p6, p7, p8 });
				if(obj == null) {
					return default(TResult);
				}
				return (TResult)obj;
			});
		}

		protected override Type Type() {
			return typeof(Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult>);
		}
	}

	class FuncHelper<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult> : FuncHelper {
		protected override Delegate CreateFuncImpl(Func<object[], object> body) {
			return new Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>((p1, p2, p3, p4, p5, p6, p7, p8, p9) => {
				object obj = body(new object[] { p1, p2, p3, p4, p5, p6, p7, p8, p9 });
				if(obj == null) {
					return default(TResult);
				}
				return (TResult)obj;
			});
		}

		protected override Type Type() {
			return typeof(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>);
		}
	}

	class FuncHelper<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult> : FuncHelper {
		protected override Delegate CreateFuncImpl(Func<object[], object> body) {
			return new Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult>((p1, p2, p3, p4, p5, p6, p7, p8, p9, p10) => {
				object obj = body(new object[] { p1, p2, p3, p4, p5, p6, p7, p8, p9, p10 });
				if(obj == null) {
					return default(TResult);
				}
				return (TResult)obj;
			});
		}

		protected override Type Type() {
			return typeof(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult>);
		}
	}
	#endregion
	#region Action
	class ActionHelper<T> : ActionHelper {
		protected override Delegate CreateActionImpl(Action<object[]> body) {
			return new Action<T>(p1 => body(new object[] { p1 }));
		}

		protected override Type Type() {
			return typeof(Action<T>);
		}
	}

	class ActionHelper<T1, T2> : ActionHelper {
		protected override Delegate CreateActionImpl(Action<object[]> body) {
			return new Action<T1, T2>((p1, p2) => body(new object[] { p1, p2 }));
		}

		protected override Type Type() {
			return typeof(Action<T1, T2>);
		}
	}

	class ActionHelper<T1, T2, T3> : ActionHelper {
		protected override Delegate CreateActionImpl(Action<object[]> body) {
			return new Action<T1, T2, T3>((p1, p2, p3) => body(new object[] { p1, p2, p3 }));
		}

		protected override Type Type() {
			return typeof(Action<T1, T2, T3>);
		}
	}

	class ActionHelper<T1, T2, T3, T4> : ActionHelper {
		protected override Delegate CreateActionImpl(Action<object[]> body) {
			return new Action<T1, T2, T3, T4>((p1, p2, p3, p4) => body(new object[] { p1, p2, p3, p4 }));
		}

		protected override Type Type() {
			return typeof(Action<T1, T2, T3, T4>);
		}
	}

	class ActionHelper<T1, T2, T3, T4, T5> : ActionHelper {
		protected override Delegate CreateActionImpl(Action<object[]> body) {
			return new Action<T1, T2, T3, T4, T5>((p1, p2, p3, p4, p5) => body(new object[] { p1, p2, p3, p4, p5 }));
		}

		protected override Type Type() {
			return typeof(Action<T1, T2, T3, T4, T5>);
		}
	}

	class ActionHelper<T1, T2, T3, T4, T5, T6> : ActionHelper {
		protected override Delegate CreateActionImpl(Action<object[]> body) {
			return new Action<T1, T2, T3, T4, T5, T6>((p1, p2, p3, p4, p5, p6) => body(new object[] { p1, p2, p3, p4, p5, p6 }));
		}

		protected override Type Type() {
			return typeof(Action<T1, T2, T3, T4, T5, T6>);
		}
	}

	class ActionHelper<T1, T2, T3, T4, T5, T6, T7> : ActionHelper {
		protected override Delegate CreateActionImpl(Action<object[]> body) {
			return new Action<T1, T2, T3, T4, T5, T6, T7>((p1, p2, p3, p4, p5, p6, p7) => body(new object[] { p1, p2, p3, p4, p5, p6, p7 }));
		}

		protected override Type Type() {
			return typeof(Action<T1, T2, T3, T4, T5, T6, T7>);
		}
	}

	class ActionHelper<T1, T2, T3, T4, T5, T6, T7, T8> : ActionHelper {
		protected override Delegate CreateActionImpl(Action<object[]> body) {
			return new Action<T1, T2, T3, T4, T5, T6, T7, T8>((p1, p2, p3, p4, p5, p6, p7, p8) => body(new object[] { p1, p2, p3, p4, p5, p6, p7, p8 }));
		}

		protected override Type Type() {
			return typeof(Action<T1, T2, T3, T4, T5, T6, T7, T8>);
		}
	}

	class ActionHelper<T1, T2, T3, T4, T5, T6, T7, T8, T9> : ActionHelper {
		protected override Delegate CreateActionImpl(Action<object[]> body) {
			return new Action<T1, T2, T3, T4, T5, T6, T7, T8, T9>((p1, p2, p3, p4, p5, p6, p7, p8, p9) => body(new object[] { p1, p2, p3, p4, p5, p6, p7, p8, p9 }));
		}

		protected override Type Type() {
			return typeof(Action<T1, T2, T3, T4, T5, T6, T7, T8, T9>);
		}
	}

	class ActionHelper<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> : ActionHelper {
		protected override Delegate CreateActionImpl(Action<object[]> body) {
			return new Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>((p1, p2, p3, p4, p5, p6, p7, p8, p9, p10) => body(new object[] { p1, p2, p3, p4, p5, p6, p7, p8, p9, p10 }));
		}

		protected override Type Type() {
			return typeof(Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>);
		}
	}
	#endregion
}