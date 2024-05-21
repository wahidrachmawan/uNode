using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;

namespace MaxyGames.UNode {
	class GenericFakeType : FakeType {
		readonly Type genericType;
		readonly Type[] genericParameters;
		readonly Type[] parameterConstraints;

		public GenericFakeType(Type genericType, Type[] genericParameters) : base(GetActualGenericType(genericType, genericParameters)) {
			this.genericParameters = genericParameters;
			if(genericType.IsConstructedGenericType) {
				genericType = genericType.GetGenericTypeDefinition();
			}
			this.genericType = genericType;
			parameterConstraints = genericType.GetGenericArguments();
		}

		public override string Name => target.Name.Split('`')[0] + "<" + string.Join(", ", genericParameters.Select(p => p.Name)) + ">";
		public override string FullName => target.FullName.Split('`')[0] + "<" + string.Join(", ", genericParameters.Select(p => p.FullName)) + ">";

		static BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

		public bool IsOnlyNativeTypes() {
			if(!ReflectionUtils.IsNativeType(genericType)) {
				return false;
			}
			for(int i = 0; i < genericParameters.Length; i++) {
				if(!ReflectionUtils.IsNativeType(genericParameters[i])) {
					return false;
				}
			}
			return true;
		}

		public override Type GetNativeType() {
			var gType = ReflectionUtils.GetNativeType(genericType);
			if(gType == null)
				return null;
			var pType = new Type[genericParameters.Length];
			for(int i = 0; i < pType.Length; i++) {
				pType[i] = ReflectionUtils.GetNativeType(genericParameters[i]);
				if(pType[i] == null)
					return null;
			}
			return gType.MakeGenericType(pType);
		}

		public override Type[] GetGenericArguments() {
			EnsureInitialized();
			return genericParameters ?? base.GetGenericArguments();
		}

		public override Type GetGenericTypeDefinition() {
			return genericType;
		}

		public override Type[] GetInterfaces() {
			var ifaces = genericType.GetInterfaces();
			var result = new Type[ifaces.Length];
			for(int i = 0; i < ifaces.Length; i++) {
				var type = ifaces[i];
				if(type.IsGenericType) {
					var types = type.GetGenericArguments();
					for(int x = 0; x < types.Length; x++) {
						for(int y = 0; y < parameterConstraints.Length; y++) {
							if(types[x] == parameterConstraints[y]) {
								types[x] = genericParameters[y];
								break;
							}
						}
					}
					var definition = type.GetGenericTypeDefinition();
					result[i] = ReflectionUtils.MakeGenericType(definition, types);
					continue;
				}
				//TODO: add support for array
				//else if(type.IsArray) {

				//}
				result[i] = type;
			}
			return result;
		}

		public override Type GetInterface(string fullname, bool ignoreCase) {
			SplitName(fullname, out var name, out var ns);
			var parameterConstraints = this.parameterConstraints;
			var ifaces = genericType.GetInterfaces();
			for(int i = 0; i < ifaces.Length; i++) {
				var type = ifaces[i];
				if(!FilterApplyPrefixLookup(type, name, ignoreCase)) {
					continue;
				}
				if(ns != null && !type.Namespace.Equals(ns)) {
					continue;
				}
				if(type.IsGenericType) {
					var types = type.GetGenericArguments();
					for(int x = 0; x < types.Length; x++) {
						for(int y = 0; y < parameterConstraints.Length; y++) {
							if(types[x] == parameterConstraints[y]) {
								types[x] = genericParameters[y];
								break;
							}
						}
					}
					var definition = type.GetGenericTypeDefinition();
					return ReflectionUtils.MakeGenericType(definition, types);
				}
				//TODO: add support for array
				//else if(type.IsArray) {
					
				//}
				return type;
			}
			//var nativeType = GetNativeType();
			//if(nativeType != null) {
			//	return nativeType.GetInterface(name, ignoreCase);
			//}
			return null;
		}

		public override string ToString() {
			return target.Name.Split('`')[0] + "<" + string.Join(", ", genericParameters.Select(t => t.ToString())) + ">";
		}

		private static Type GetActualNativeType(Type type) {
			if(type is RuntimeType) {
				if(type is IRuntimeType prType) {
					return prType.GetNativeType() ?? typeof(IRuntimeClass);
				}
				else {
					return typeof(IRuntimeClass);
				}
			}
			else {
				return type;
			}
		}

		protected override void Initialize() {
			var gArgs = genericType.GetGenericArguments();
			var members = genericType.GetMembers(flags);
			foreach(var member in members) {
				switch(member.MemberType) {
					case MemberTypes.Field: {
						var field = member as FieldInfo;
						var fType = field.FieldType;
						if(fType.IsGenericParameter) {
							field = target.GetField(field.Name, flags);
							if(field == null) {
								break;
							}
							declaredMembers[member] = new FakeField(this, field, genericParameters[IndexOf(gArgs, p => p.Name == fType.Name)]);
						}
						else if(fType.ContainsGenericParameters) {
							field = target.GetField(field.Name, flags);
							if(field == null) {
								break;
							}
							declaredMembers[field] = null;
						}
						break;
					}
					case MemberTypes.Property: {
						var property = member as PropertyInfo;
						var pType = property.PropertyType;
						if(pType.IsGenericParameter) {
							property = target.GetProperty(property.Name, flags);
							if(property == null) {
								break;
							}
							declaredMembers[member] = new FakeProperty(this, property, genericParameters[IndexOf(gArgs, p => p.Name == pType.Name)]);
						}
						else if(pType.ContainsGenericParameters) {
							property = target.GetProperty(property.Name, flags);
							if(property == null) {
								break;
							}
							declaredMembers[property] = null;
						}
						break;
					}
					case MemberTypes.Method: {
						var method = member as MethodInfo;
						bool flag = false;
						var methodType = method.ReturnType;
						if(methodType.IsGenericParameter) {
							methodType = genericParameters[IndexOf(gArgs, p => p.Name == methodType.Name)];
							flag = true;
						}
						var parameters = method.GetParameters();
						Type[] paramTypes = new Type[parameters.Length];
						for(int i = 0; i < parameters.Length; i++) {
							var pType = parameters[i].ParameterType;
							if(pType.IsGenericParameter) {
								parameters[i] = new FakeParameter(parameters[i], genericParameters[IndexOf(gArgs, p => p.Name == pType.Name)]);
								paramTypes[i] = GetActualNativeType(parameters[i].ParameterType);
								flag = true;
							}
							else if(pType.ContainsGenericParameters) {
								void ValidateParameterType(Type t, out Type runtimeType, out Type nativeType) {
									if(t.IsGenericType) {
										var gTypes = t.GetGenericArguments();
										var nativeTypes = new Type[gTypes.Length];
										for(int x = 0; x < gTypes.Length; x++) {
											if(gTypes[x].IsGenericParameter) {
												int idx = IndexOf(gArgs, p => p.Name == gTypes[x].Name);
												if(idx >= 0) {
													gTypes[x] = genericParameters[idx];
													nativeTypes[x] = GetActualNativeType(genericParameters[idx]);
												}
												else {
													runtimeType = null;
													nativeType = null;
													return;
												}
											}
											else if(gTypes[x].ContainsGenericParameters) {
												ValidateParameterType(gTypes[x], out var rType, out var nType);
												if(rType == null) {
													runtimeType = null;
													nativeType = null;
													return;
												}
												gTypes[x] = rType;
												nativeTypes[x] = nType;
											}
											else {
												nativeTypes[x] = t;
											}
										}
										if(t.IsConstructedGenericType) {
											t = t.GetGenericTypeDefinition();
										}
										runtimeType = ReflectionFaker.FakeGenericType(t, gTypes);
										nativeType = t.MakeGenericType(nativeTypes);
									}
									else if(t.IsArray) {
										if(t.GetArrayRank() == 1) {
											var elementType = t.GetElementType();
											Type nElementType;
											if(elementType.IsGenericParameter) {
												int idx = IndexOf(gArgs, p => p.Name == elementType.Name);
												if(idx >= 0) {
													elementType = genericParameters[idx];
													nElementType = GetActualNativeType(genericParameters[idx]);
												}
												else {
													runtimeType = null;
													nativeType = null;
													return;
												}
											}
											else if(elementType.ContainsGenericParameters) {
												ValidateParameterType(elementType, out var rType, out var nType);
												if(rType == null) {
													runtimeType = null;
													nativeType = null;
													return;
												}
												elementType = rType;
												nElementType = nType;
											}
											else {
												nElementType = t;
											}
											runtimeType = ReflectionFaker.FakeArrayType(elementType);
											nativeType = nElementType.MakeArrayType();
										}
										else {
											runtimeType = null;
											nativeType = null;
										}
									}
									else {
										runtimeType = t;
										nativeType = t;
									}
								}
								ValidateParameterType(pType, out var rpType, out var npType);
								if(rpType == null) {
									method = GetActualMethod(method, target);
									if(method != null) {
										declaredMembers[method] = null;
									}
									flag = false;
									break;
								}
								else {
									parameters[i] = new FakeParameter(parameters[i], rpType);
									paramTypes[i] = npType;
									flag = true;
								}
							}
							else {
								paramTypes[i] = pType;
							}
						}
						if(flag) {
							method = target.GetMethod(method.Name, flags, null, paramTypes, null);
							if(method == null) {
								method = GetActualMethod(member as MethodInfo, target);
								if(method != null) {
									declaredMembers[method] = null;
								}
								break;
							}
							bool valid = true;
							for(int i = 0; i < parameters.Length; i++) {
								if(parameters[i].ParameterType is FakeType) {
									valid = false;
									break;
								}
							}
							if(valid) {
								var m = new FakeMethod(this, method, methodType, parameters);
								declaredMembers[method] = m;
							}
							else {
								declaredMembers[method] = null;
							}
						}
						break;
					}
					case MemberTypes.Constructor: {
						var ctor = member as ConstructorInfo;
						var original = ctor.GetParameters();
						var parameters = new ParameterInfo[original.Length];
						Array.Copy(original, parameters, original.Length);
						Type[] paramTypes = new Type[parameters.Length];
						for(int i = 0; i < parameters.Length; i++) {
							var pType = parameters[i].ParameterType;
							if(pType.IsGenericParameter) {
								parameters[i] = new FakeParameter(parameters[i], genericParameters[IndexOf(gArgs, p => p.Name == pType.Name)]);
								paramTypes[i] = GetActualNativeType(parameters[i].ParameterType);
							}
							else if(pType.ContainsGenericParameters) {
								void ValidateParameterType(Type t, out Type runtimeType, out Type nativeType) {
									if(t.IsGenericType) {
										var gTypes = t.GetGenericArguments();
										var nativeTypes = new Type[gTypes.Length];
										for(int x = 0; x < gTypes.Length; x++) {
											if(gTypes[x].IsGenericParameter) {
												int idx = IndexOf(gArgs, p => p.Name == gTypes[x].Name);
												if(idx >= 0) {
													gTypes[x] = genericParameters[idx];
													nativeTypes[x] = GetActualNativeType(genericParameters[idx]);
												}
												else {
													runtimeType = null;
													nativeType = null;
													return;
												}
											}
											else if(gTypes[x].ContainsGenericParameters) {
												ValidateParameterType(gTypes[x], out var rType, out var nType);
												if(rType == null) {
													runtimeType = null;
													nativeType = null;
													return;
												}
												gTypes[x] = rType;
												nativeTypes[x] = nType;
											}
											else {
												nativeTypes[x] = t;
											}
										}
										if(t.IsConstructedGenericType) {
											t = t.GetGenericTypeDefinition();
										}
										runtimeType = ReflectionFaker.FakeGenericType(t, gTypes);
										nativeType = t.MakeGenericType(nativeTypes);
									}
									else if(t.IsArray) {
										if(t.GetArrayRank() == 1) {
											var elementType = t.GetElementType();
											Type nElementType;
											if(elementType.IsGenericParameter) {
												int idx = IndexOf(gArgs, p => p.Name == elementType.Name);
												if(idx >= 0) {
													elementType = genericParameters[idx];
													nElementType = GetActualNativeType(genericParameters[idx]);
												}
												else {
													runtimeType = null;
													nativeType = null;
													return;
												}
											}
											else if(elementType.ContainsGenericParameters) {
												ValidateParameterType(elementType, out var rType, out var nType);
												if(rType == null) {
													runtimeType = null;
													nativeType = null;
													return;
												}
												elementType = rType;
												nElementType = nType;
											}
											else {
												nElementType = t;
											}
											runtimeType = ReflectionFaker.FakeArrayType(elementType);
											nativeType = nElementType.MakeArrayType();
										}
										else {
											runtimeType = null;
											nativeType = null;
										}
									}
									else {
										runtimeType = t;
										nativeType = t;
									}
								}
								ValidateParameterType(pType, out var rpType, out var npType);
								if(rpType == null) {
									ctor = GetActualConstructor(ctor, target);
									if(ctor != null) {
										declaredMembers[ctor] = null;
									}
									break;
								}
								else {
									parameters[i] = new FakeParameter(parameters[i], rpType);
									paramTypes[i] = npType;
								}
							}
							else {
								paramTypes[i] = pType;
							}
						}
						ctor = target.GetConstructor(flags, null, paramTypes, null);
						if(ctor == null) {
							ctor = GetActualConstructor(member as ConstructorInfo, target);
							if(ctor != null) {
								declaredMembers[ctor] = null;
							}
							break;
						}
						bool valid = true;
						for(int i = 0; i < parameters.Length; i++) {
							if(parameters[i].ParameterType is FakeType) {
								valid = false;
								break;
							}
						}
						if(valid) {
							var m = new FakeConstructor(this, ctor, parameters);
							declaredMembers[ctor] = m;
						}
						else {
							declaredMembers[ctor] = null;
						}
						break;
					}
					case MemberTypes.NestedType: {
						var nestedType = target.GetNestedType(member.Name, flags);
						if(nestedType != null) {
							declaredMembers[nestedType] = null;
						}
						break;
					}
					case MemberTypes.Event: {
						var evt = target.GetEvent(member.Name, flags);
						if(evt != null) {
							declaredMembers[evt] = null;
						}
						break;
					}
				}
			}
		}

		#region Others
		private static Type GetActualGenericType(Type type, Type[] typeArguments) {
			if(type.IsGenericTypeDefinition) {
				var arguments = type.GetGenericArguments();
				var param = new Type[typeArguments.Length];
				Array.Copy(typeArguments, param, param.Length);
				for(int i = 0; i < param.Length; i++) {
					param[i] = ReflectionUtils.GetNativeType(param[i]);
					if(param[i] == null) {
						param[i] = ReflectionUtils.GetDefaultConstraint(arguments[i]) ?? ReflectionUtils.GetDefaultConstraint(arguments[i], typeof(int));
						if(param[i] == null) {
							//Fail to get default constraint, so return the original type instead
							return type;
						}
					}
				}
				return type.MakeGenericType(param);
			}
			return type;
		}

		static int IndexOf<TSource>(IEnumerable<TSource> source, Func<TSource, bool> predicate) {
			var index = 0;
			foreach(var item in source) {
				if(predicate.Invoke(item)) {
					return index;
				}
				index++;
			}
			return -1;
		}

		Type GetActualType(Type type) {
			return ReflectionUtils.GetNativeType(type) ?? typeof(IRuntimeClass);
#if false
			if(type is INativeMember) {
				var nativeType = (type as INativeMember).GetNativeMember() as Type;
				if(nativeType != null) {
					return nativeType;
				}
				else {
					return typeof(IRuntimeClass);
				}
			}
			else if(type is IFakeType fakeType) {
				return fakeType.GetNativeType();
			}
			else if(type is IRuntimeType runtimeType) {
				return runtimeType.GetNativeType();
			}
			else if(type.IsGenericParameter) {
				return typeof(IRuntimeClass);
			}
			else if(type.ContainsGenericParameters) {
				if(type.IsGenericType) {
					var types = type.GetGenericArguments();
					for(int i = 0; i < types.Length; i++) {
						types[i] = GetActualType(types[i]);
						if(types[i] == null) {
							return null;
						}
					}
					if(type.IsConstructedGenericType) {
						type = type.GetGenericTypeDefinition();
					}
					return type.MakeGenericType(types);
				}
				else if(type.IsArray) {
					var elementType = GetActualType(type.GetElementType());
					return elementType.MakeArrayType(type.GetArrayRank());
				}
			}
			return type;
#endif
		}

		ConstructorInfo GetActualConstructor(ConstructorInfo ctor, Type type) {
			var mParams = ctor.GetParameters();
			Type[] mpTypes = new Type[mParams.Length];
			for(int i = 0; i < mpTypes.Length; i++) {
				mpTypes[i] = GetActualType(mParams[i].ParameterType);
				if(mpTypes[i] == null)
					return null;
			}
			return type.GetConstructor(flags, null, mpTypes, null);
		}

		MethodInfo GetActualMethod(MethodInfo method, Type type) {
			var mParams = method.GetParameters();
			Type[] mpTypes = new Type[mParams.Length];
			for(int i = 0; i < mpTypes.Length; i++) {
				mpTypes[i] = GetActualType(mParams[i].ParameterType);
				if(mpTypes[i] == null)
					return null;
			}
			return type.GetMethod(method.Name, flags, null, mpTypes, null);
		}
#endregion
	}
}