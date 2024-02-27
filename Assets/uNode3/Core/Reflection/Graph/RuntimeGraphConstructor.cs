using System;
using System.Linq;
using System.Globalization;
using System.Reflection;

namespace MaxyGames.UNode {
	public class RuntimeGraphConstructor : RuntimeConstructor {
		private Type[] parameterTypes;
		private Func<object[], object> onInvoked;

		public RuntimeGraphConstructor(RuntimeGraphType owner, Func<object[], object> onInvoked) : base(owner) {
			parameterTypes = Type.EmptyTypes;
			this.onInvoked = onInvoked;
		}

		public RuntimeGraphConstructor(RuntimeGraphType owner, Func<object[], object> onInvoked, params Type[] parameterTypes) : base(owner) {
			this.parameterTypes = parameterTypes;
			this.onInvoked = onInvoked;
		}

		public override string Name => owner.Name;

		private ParameterInfo[] m_parameterInfos;
		public override ParameterInfo[] GetParameters() {
			if(m_parameterInfos == null) {
				if(parameterTypes != null) {
					var param = new ParameterInfo[parameterTypes.Length];
					for(int i = 0; i < parameterTypes.Length; i++) {
						param[i] = new RuntimeParameterInfo(parameterTypes[i]);
					}
					return param;
				}
				else {
					m_parameterInfos = Array.Empty<ParameterInfo>();
				}
			}
			return m_parameterInfos;
		}

		protected virtual Type[] GetParamTypes() {
			return parameterTypes;
		}

		public override object Invoke(BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture) {
			return onInvoked(parameters);
		}

		public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture) {
			return onInvoked(parameters);
		}
	}
}