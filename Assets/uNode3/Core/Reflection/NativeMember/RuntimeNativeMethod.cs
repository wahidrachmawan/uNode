using System;
using System.Linq;
using System.Globalization;
using System.Reflection;

namespace MaxyGames.UNode {
	public class RuntimeNativeMethod : RuntimeMethod<FunctionRef>, ISummary, IRuntimeMemberWithRef, INativeMethod {
		private Type[] functionTypes;
		
		public RuntimeNativeMethod(RuntimeNativeType owner, FunctionRef target) : base(owner, target) {
			this.target = target;
		}

		private Exception throwIfNotCompiled => new Exception($"The graph: {target.reference.graphContainer} need to be compiled first.");

		public override string Name => target.name;

		public override Type ReturnType => target.ReturnType();

		public override MethodAttributes Attributes {
			get {
				var att = MethodAttributes.Public;
				var modifier = target.reference?.modifier;
				if(modifier != null) {
					if(modifier.Private) {
						att = MethodAttributes.Private;
					}
					if(modifier.Protected) {
						if(modifier.Internal) {
							att = MethodAttributes.FamORAssem;
						}
						else {
							att = MethodAttributes.Family;
						}
					}
					if(modifier.Static) {
						att |= MethodAttributes.Static;
					}
					if(modifier.Abstract) {
						att |= MethodAttributes.Abstract;
					}
					if(modifier.Virtual) {
						att |= MethodAttributes.Virtual;
					}
				}
				if(owner.IsAbstract && owner.IsSealed) {
					att |= MethodAttributes.Static;
				}
				return att;
			}
		}

		public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture) {
			var member = GetNativeMethod();
			if(member != null) {
				return member.InvokeOptimized(obj, parameters);
			}
			else {
				throw throwIfNotCompiled;
			}
		}

		public override ParameterInfo[] GetParameters() {
			var types = target.reference?.Parameters;
			if(types != null) {
				var param = new ParameterInfo[types.Count];
				for (int i = 0; i < types.Count;i++) {
					param[i] = new RuntimeGraphParameter(types[i]);
				}
				return param;
			}
			return new ParameterInfo[0];
		}

		public string GetSummary() {
			return target.GetSummary();
		}

		private Type[] GetParamTypes() {
			if(functionTypes == null) {
				functionTypes = target.reference?.Parameters.Select(p => p.Type).ToArray();
			}
			return functionTypes;
		}

		public BaseReference GetReference() {
			return target;
		}

		private MethodInfo _nativeMember;
		public MethodInfo GetNativeMethod() {
			if(_nativeMember == null) {
				var type = (owner as RuntimeNativeType).GetNativeType();
				if(type != null) {
					_nativeMember = type.GetMethod(Name, MemberData.flags, null, GetParamTypes(), null);
				}
			}
			return _nativeMember;
		}

		public MemberInfo GetNativeMember() {
			return GetNativeMethod();
		}
	}
}