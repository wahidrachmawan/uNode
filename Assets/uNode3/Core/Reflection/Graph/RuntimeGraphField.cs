using System;
using System.Globalization;
using System.Reflection;

namespace MaxyGames.UNode {
    public class RuntimeGraphField : RuntimeField<VariableRef>, ISummary, IRuntimeMemberWithRef {
		public RuntimeGraphField(RuntimeType owner, VariableRef target) : base(owner, target) {

		}

		public override Type FieldType => target.type;

		public override string Name => target.name;

		public override FieldAttributes Attributes {
			get {
				var modifier = target.reference?.modifier;
				if(modifier != null) {
					if(modifier.isPublic) {
						if(modifier.Static || owner.IsAbstract && owner.IsSealed) {
							return FieldAttributes.Public | FieldAttributes.Static;
						}
						return FieldAttributes.Public;
					}
					else if(modifier.isProtected) {
						if(modifier.Internal) {
							return FieldAttributes.FamORAssem;
						}
						else {
							return FieldAttributes.Family;
						}
					}
					else {
						if(modifier.Static || owner.IsAbstract && owner.IsSealed) {
							return FieldAttributes.Private | FieldAttributes.Static;
						}
						return FieldAttributes.Private;
					}
				}
				return base.Attributes;
			}
		}

		public BaseReference GetReference() {
			return target;
		}

		public string GetSummary() {
			return target.GetSummary();
		}

		public override object GetValue(object obj) {
			if(IsStatic) {
				if(owner is RuntimeGraphType runtimeGraph) {
					if(runtimeGraph.target != null) {
						return DoGetValue(runtimeGraph.target);
					}
					throw new NullReferenceException("The graph reference cannot be null");
				}
				throw new NotImplementedException();
			} else if(obj == null) {
				throw new NullReferenceException("The instance member cannot be null");
			}
			return DoGetValue(obj);
		}

		public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, CultureInfo culture) {
			if(IsStatic) {
				if(owner is RuntimeGraphType runtimeGraph) {
					if(runtimeGraph.target != null) {
						DoSetValue(runtimeGraph.target, value);
						return;
					}
					throw new NullReferenceException("The graph reference cannot be null");
				}
				throw new NotImplementedException();
			} else if(obj == null) {
				throw new NullReferenceException("The instance member cannot be null");
			}
			DoSetValue(obj, value);
		}

		protected object DoGetValue(object obj) {
			if(obj is IInstancedGraph instanced && instanced.Instance != null) {
				if(target.reference == null) {
					throw new Exception($"Variable: {target.name} was removed from graph: {owner}");
				}
				return target.reference.Get(instanced.Instance);
			}
			else {
				if(obj is IRuntimeVariable runtime) {
					return runtime.GetVariable(target.name);
				}
				throw new Exception($"Invalid given instance when accessing variable: {Name} from type: {owner.PrettyName(true)}\nThe instance type is: " + (obj != null ? obj.GetType() : "null"));
			}
		}

		protected void DoSetValue(object obj, object value) {
			if(obj is IInstancedGraph instanced && instanced.Instance != null) {
				if(target.reference == null) {
					throw new Exception($"Variable: {target.name} was removed from graph: {owner}");
				}
				target.reference.Set(instanced.Instance, value);
			}
			else {
				if(obj is IRuntimeVariable runtime) {
					runtime.SetVariable(target.name, value);
					return;
				}
				throw new Exception($"Invalid given instance when accessing variable: {Name} from type: {owner.PrettyName(true)}\nThe instance type is: " + (obj != null ? obj.GetType() : "null"));
			}
		}
	}
}