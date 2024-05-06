using System;
using System.Linq;
using System.Globalization;
using System.Reflection;

namespace MaxyGames.UNode {
	public class RuntimeGraphMethod : RuntimeMethod<FunctionRef>, ISummary, IRuntimeMemberWithRef {
		private Type[] functionTypes;
		
		public RuntimeGraphMethod(RuntimeType owner, FunctionRef target) : base(owner, target) {
			this.target = target;
		}

		public override string Name => target.name;

		public override Type ReturnType => target.ReturnType();

		public override MethodAttributes Attributes {
			get {
				var modifier = target.reference?.modifier;
				if(modifier != null) {
					MethodAttributes att;
					if(modifier.isPublic) {
						att = MethodAttributes.Public;
					}
					else if(modifier.isProtected) {
						if(modifier.Internal) {
							att = MethodAttributes.FamORAssem;
						}
						else {
							att = MethodAttributes.Family;
						}
					}
					else {
						att = MethodAttributes.Private;
					}
					if(modifier.Virtual)
						att |= MethodAttributes.Virtual;
					if(modifier.Static || owner.IsAbstract && owner.IsSealed)
						att |= MethodAttributes.Static;
					return att;
				}
				return base.Attributes;
			}
		}

		public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture) {
			if(IsStatic) {
				if(owner is RuntimeGraphType runtimeGraph) {
					if(runtimeGraph.target != null) {
						return DoInvoke(runtimeGraph.target, parameters);
					}
					throw new NullReferenceException("The graph reference cannot be null");
				}
				throw new NotImplementedException();
			} else if(obj == null) {
				throw new NullReferenceException("The instance member cannot be null");
			}
			return DoInvoke(obj, parameters);
		}

		protected object DoInvoke(object obj, object[] parameters) {
			if(obj is IInstancedGraph instanced && instanced.Instance != null) {
				if(target.reference == null) {
					throw new Exception($"Function: {target.name} was removed from graph: {owner}");
				}
				return target.reference.Invoke(instanced.Instance, parameters);
			}
			else {
				if(obj is IRuntimeFunction runtime) {
					return runtime.InvokeFunction(target.name, GetParamTypes(), parameters);
				}
				throw new Exception($"Invalid given instance when accessing function: {Name} from type: {owner.PrettyName(true)}\nThe instance type is: " + (obj != null ? obj.GetType() : "null"));
			}
		}

		//TODO: cache return parameters
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
	}
}