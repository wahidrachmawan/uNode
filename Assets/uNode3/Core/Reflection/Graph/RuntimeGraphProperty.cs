using System;
using System.Globalization;
using System.Reflection;

namespace MaxyGames.UNode {
	public class RuntimeGraphProperty : RuntimeProperty<PropertyRef>, ISummary, IRuntimeMemberWithRef {
        public RuntimeGraphProperty(RuntimeType owner, PropertyRef target) : base(owner, target) { }

		public override string Name => target.name;

		public override Type PropertyType => target.ReturnType();

		public string GetSummary() {
			return target.GetSummary();
		}

		private RuntimePropertyGetMethod _getMethod;
		public override MethodInfo GetGetMethod(bool nonPublic) {
			if(_getMethod == null && target.CanGetValue()) {
				_getMethod = new RuntimePropertyGetMethod(owner, this);
			}
			return target.CanGetValue() ? _getMethod : null;
		}

		private RuntimePropertySetMethod _setMethod;
		public override MethodInfo GetSetMethod(bool nonPublic) {
			if(_setMethod == null && target.CanSetValue()) {
				_setMethod = new RuntimePropertySetMethod(owner, this);
			}
			return target.CanSetValue() ? _setMethod : null;
		}

		public override object GetValue(object obj, BindingFlags invokeAttr, Binder binder, object[] index, CultureInfo culture) {
			if(owner.IsAbstract & owner.IsSealed) {
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

		public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, object[] index, CultureInfo culture) {
			if(owner.IsAbstract & owner.IsSealed) {
				if(owner is RuntimeGraphType runtimeGraph) {
					if(runtimeGraph.target != null) {
						DoSetValue(obj, value);
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
					throw new Exception($"Property: {target.name} was removed from graph: {owner}");
				}
				return target.reference.Get(instanced.Instance);
			}
			else {
				if(obj is IRuntimeProperty runtime) {
					return runtime.GetProperty(target.name);
				}
				throw new Exception($"Invalid given instance when accessing property: {Name} from type: {owner.PrettyName(true)}\nThe instance type is: " + (obj != null ? obj.GetType() : "null"));
			}
		}

		protected void DoSetValue(object obj, object value) {
			if(obj is IInstancedGraph instanced && instanced.Instance != null) {
				if(target.reference == null) {
					throw new Exception($"Property: {target.name} was removed from graph: {owner}");
				}
				target.reference.Set(instanced.Instance, value);
			}
			else {
				if(obj is IRuntimeProperty runtime) {
					runtime.SetProperty(target.name, value);
					return;
				}
				throw new Exception($"Invalid given instance when accessing property: {Name} from type: {owner.PrettyName(true)}\nThe instance type is: " + (obj != null ? obj.GetType() : "null"));
			}
		}

		public BaseReference GetReference() {
			return target;
		}
	}

	public class RuntimePropertyGetMethod : RuntimeMethod<RuntimeProperty> {
		public override string Name => target.Name.AddFirst("get_");
		public override Type ReturnType => target.PropertyType;

		public override MethodAttributes Attributes {
			get {
				if(target is IRuntimeMemberWithRef memberWithRef) {
					var reference = memberWithRef.GetReferenceValue();
					if(reference != null) {
						if(reference is Property property) {
							MethodAttributes att;
							 if(property.modifier.isPublic) {
								if(property.getterModifier.isPublic) {
									att = MethodAttributes.Public;
								}
								else if(property.getterModifier.isProtected) {
									if(property.getterModifier.Internal) {
										att = MethodAttributes.FamORAssem;
									}
									else {
										att = MethodAttributes.Family;
									}
								}
								else {
									att = MethodAttributes.Private;
								}
							}
							else if(property.modifier.isProtected && !property.getterModifier.isPrivate) {
								if(property.modifier.Internal) {
									att = MethodAttributes.FamORAssem;
								}
								else {
									att = MethodAttributes.Family;
								}
							}
							else {
								att = MethodAttributes.Private;
							}
							if(property.modifier.Static) {
								att |= MethodAttributes.Static;
							}
							if(property.modifier.Abstract) {
								att |= MethodAttributes.Abstract;
							}
							if(property.modifier.Virtual) {
								att |= MethodAttributes.Virtual;
							}
							return att;
						}
					}
				}
				return base.Attributes;
			}
		}

		public RuntimePropertyGetMethod(RuntimeType owner, RuntimeProperty target) : base(owner, target) { }

		public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture) {
			return target.GetValue(obj);
		}

		public override ParameterInfo[] GetParameters() {
			return new ParameterInfo[0];
		}
	}

	public class RuntimePropertySetMethod : RuntimeMethod<RuntimeProperty> {
		public override string Name => target.Name.AddFirst("set_");
		public override Type ReturnType => target.PropertyType;

		public override MethodAttributes Attributes {
			get {
				if(target is IRuntimeMemberWithRef memberWithRef) {
					var reference = memberWithRef.GetReferenceValue();
					if(reference != null) {
						if(reference is Property property) {
							MethodAttributes att;
							if(property.modifier.isPublic) {
								if(property.setterModifier.isPublic) {
									att = MethodAttributes.Public;
								}
								else if(property.setterModifier.isProtected) {
									if(property.setterModifier.Internal) {
										att = MethodAttributes.FamORAssem;
									}
									else {
										att = MethodAttributes.Family;
									}
								}
								else {
									att = MethodAttributes.Private;
								}
							}
							else if(property.modifier.isProtected && !property.setterModifier.isPrivate) {
								if(property.modifier.Internal) {
									att = MethodAttributes.FamORAssem;
								}
								else {
									att = MethodAttributes.Family;
								}
							}
							else {
								att = MethodAttributes.Private;
							}
							if(property.modifier.Static) {
								att |= MethodAttributes.Static;
							}
							if(property.modifier.Abstract) {
								att |= MethodAttributes.Abstract;
							}
							if(property.modifier.Virtual) {
								att |= MethodAttributes.Virtual;
							}
							return att;
						}
					}
				}
				return base.Attributes;
			}
		}

		public RuntimePropertySetMethod(RuntimeType owner, RuntimeProperty target) : base(owner, target) { }

		public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture) {
			target.SetValueOptimized(obj, parameters[0]);
			return null;
		}

		public override ParameterInfo[] GetParameters() {
			return new ParameterInfo[] { new RuntimeParameterInfo("value", target.PropertyType) };
		}
	}
}