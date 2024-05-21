using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using MaxyGames.UNode;
using MaxyGames.UNode.GenericResolver;
using UnityEngine;



namespace MaxyGames.UNode {
	/// <summary>
	/// The generic method resolver.
	/// This will be created multiple times for handling different scenarios.
	/// </summary>
	/// <remarks>
	/// By using resolver, the generic method will correctly invoked and code gen will correctly generated
	/// </remarks>
    public abstract class GenericMethodResolver {
		/// <summary>
		/// This is the open generic CLR method info
		/// </summary>
		public MethodInfo OpenMethodInfo {
			get;
			internal set;
		}
		/// <summary>
		/// This is the constructed CLR method info
		/// </summary>
		public MethodInfo ConstructedMethodInfo {
			get;
			internal set;
		}
		/// <summary>
		/// This is the runtime method info
		/// </summary>
		public MethodInfo RuntimeMethodInfo {
			get;
			internal set;
		}
		public RegisterGenericMethodResolverAttribute ResolverAttribute {
			get;
			internal set;
		}

		private bool isRuntimeInitialized;
		public void EnsureRuntimeInitialized() {
			if(isRuntimeInitialized == false) {
				isRuntimeInitialized = true;
				OnRuntimeInitialize();
			}
		}

		private bool isGeneratorInitialized;
		public void EnsureGeneratorInitialize() {
			if(isGeneratorInitialized == false) {
				isGeneratorInitialized = true;
				OnGeneratorInitialize();
			}
		}

		/// <summary>
		/// Called the first time the resolver is created
		/// </summary>
		protected virtual void Setup() { }
		/// <summary>
		/// Called only once before using it for runtime
		/// </summary>
		protected virtual void OnRuntimeInitialize() { }
		/// <summary>
		/// Called only once before using it for codegen
		/// </summary>
		protected virtual void OnGeneratorInitialize() { }

		public object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture) {
			EnsureRuntimeInitialized();
			return DoInvoke(obj, invokeAttr, binder, parameters ?? Array.Empty<object>(), culture);
		}

		protected virtual object DoInvoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture) {
			return ConstructedMethodInfo.InvokeOptimized(obj, invokeAttr, binder, parameters, culture);
		}

		public void GenerateCode(List<string> members, string[] parameters) {
			EnsureGeneratorInitialize();
			DoGenerateCode(members, parameters);
		}

		/// <summary>
		/// The code generator for compatibility mode
		/// </summary>
		/// <param name="members"></param>
		/// <param name="parameters"></param>
		protected virtual void DoGenerateCode(List<string> members, string[] parameters) {
			members.Add("".CGInvoke(ConstructedMethodInfo.Name, ConstructedMethodInfo.GetGenericArguments(), parameters));
		}

		private static Dictionary<MethodInfo, RegisterGenericMethodResolverAttribute> _resolvers;
		public static Dictionary<MethodInfo, RegisterGenericMethodResolverAttribute> Resolvers {
			get {
				if(_resolvers == null) {
					_resolvers = new Dictionary<MethodInfo, RegisterGenericMethodResolverAttribute>();
					var atts = ReflectionUtils.GetAssemblyAttributes<RegisterGenericMethodResolverAttribute>();
					if(atts != null) {
						foreach(var a in atts) {
							if(_resolvers.ContainsKey(a.MethodInfo)) {
								Debug.LogWarning("Multiple resolver found for method: " + a.MethodInfo);
							}
							_resolvers[a.MethodInfo] = a;
						}
					}
				}
				return _resolvers;
			}
		}

		public static GenericMethodResolver GetResolver(MethodInfo constructedCLRMethodInfo, MethodInfo runtimeMethodInfo) {
			var openMethodInfo = constructedCLRMethodInfo.GetGenericMethodDefinition();
			GenericMethodResolver result;
			if(Resolvers.TryGetValue(openMethodInfo, out var resolver)) {
				result = Activator.CreateInstance(resolver.resolver) as GenericMethodResolver;
			}
			else {
				result = new Default();
			}
			result.ConstructedMethodInfo = constructedCLRMethodInfo;
			result.OpenMethodInfo = openMethodInfo;
			result.RuntimeMethodInfo = runtimeMethodInfo;
			result.ResolverAttribute = resolver;
			result.Setup();
			return result;
		}

		public class Default : GenericMethodResolver {

		}
	}

	/// <summary>
	/// Use this to register generic method resolver.
	/// </summary>
	[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
	public class RegisterGenericMethodResolverAttribute : Attribute {
		public Type resolver;
		public Type CLRType;
		public string methodName;
		public int genericParameterCount;
		public Type[] parameterTypes;

		public RegisterGenericMethodResolverAttribute(Type resolver, Type CLRType, string methodName, int genericParameterCount, params Type[] parameterTypes) {
			if(genericParameterCount <= 0)
				throw new Exception("Generic Parameter Count must grater than zero");
			if(resolver.IsAbstract) {
				throw new Exception("Resolver cannot be abstract");
			}
			this.resolver = resolver;
			this.CLRType = CLRType;
			this.methodName = methodName;
			this.genericParameterCount = genericParameterCount;
			this.parameterTypes = parameterTypes;
		}

		public RegisterGenericMethodResolverAttribute(Type resolver, Type CLRType, string methodName, params Type[] parameterTypes) {
			this.resolver = resolver;
			this.CLRType = CLRType;
			this.methodName = methodName;
			this.genericParameterCount = 1;
			this.parameterTypes = parameterTypes;
		}

		private MethodInfo methodInfo;
		public MethodInfo MethodInfo {
			get {
				if(methodInfo == null) {
					methodInfo = CLRType.GetMethod(methodName, genericParameterCount, parameterTypes);
					if(methodInfo == null) {
						throw new Exception($"Couldn't resolve generic method resolver for method in: {CLRType.PrettyName(true)}.{methodName}<{genericParameterCount}>({string.Join(", ", parameterTypes.Select(item => item.PrettyName(true)))})\nThe resolver is: {resolver.PrettyName(true)}");
					}
				}
				return methodInfo;
			}
		}
	}
}