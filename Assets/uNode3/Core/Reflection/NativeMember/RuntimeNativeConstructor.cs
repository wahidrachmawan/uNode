using System;
using System.Linq;
using System.Globalization;
using System.Reflection;

namespace MaxyGames.UNode {
	public class RuntimeNativeConstructor : RuntimeConstructor, INativeMember {
		private Type[] parameterTypes;

		public RuntimeNativeConstructor(RuntimeNativeType owner) : base(owner) {
			parameterTypes = Type.EmptyTypes;
		}

		public RuntimeNativeConstructor(RuntimeNativeType owner, params Type[] parameterTypes) : base(owner) {
			this.parameterTypes = parameterTypes;
		}

		private Exception throwIfNotCompile => new Exception($"The runtime type: {owner.FullName} is not compiled.");

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
				} else {
					m_parameterInfos = Array.Empty<ParameterInfo>();
				}
			}
			return m_parameterInfos;
		}

		protected virtual Type[] GetParamTypes() {
			return parameterTypes;
		}

		private ConstructorInfo _nativeMember;
		public virtual ConstructorInfo GetNativeConstructor() {
			if(_nativeMember == null) {
				var type = (owner as RuntimeNativeType).GetNativeType();
				if(type != null) {
					_nativeMember = type.GetConstructor(MemberData.flags, null, GetParamTypes(), null);
					if(_nativeMember == null && type.IsValueType && GetParamTypes().Length == 0) {
						_nativeMember = ReflectionUtils.GetDefaultConstructor(type);
					}
				}
			}
			return _nativeMember;
		}

		public MemberInfo GetNativeMember() {
			return GetNativeConstructor();
		}

		public override object Invoke(BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture) {
			var ctor = GetNativeConstructor();
			if(ctor == null)
				throw throwIfNotCompile;
			return ctor.Invoke(invokeAttr, binder, parameters, culture);
		}

		public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture) {
			var ctor = GetNativeConstructor();
			if(ctor == null)
				throw throwIfNotCompile;
			return ctor.Invoke(obj, invokeAttr, binder, parameters, culture);
		}
	}

	public class RuntimeNativeGraphConstructor : RuntimeNativeConstructor, IRuntimeMemberWithRef {
		public ConstructorRef target;
		public RuntimeNativeGraphConstructor(RuntimeNativeType owner, ConstructorRef target) : base(owner) {
			this.target = target;
		}

		public override ParameterInfo[] GetParameters() {
			return target.reference.parameters.Select(p => new RuntimeGraphParameter(p)).ToArray();
		}

		public BaseReference GetReference() {
			return target;
		}

		protected override Type[] GetParamTypes() {
			return target.reference.parameters.Select(p => p.Type).ToArray();
		}
	}
}