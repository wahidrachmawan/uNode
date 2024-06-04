using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using MaxyGames.UNode.Nodes;

namespace MaxyGames.UNode {
	public sealed class Function : BaseFunction, IAttributeSystem, IGenericParameterSystem, IIcon {
		/// <summary>
		/// The modifier of this function.
		/// </summary>
		[Tooltip("The modifier of this function.")]
		public FunctionModifier modifier = new FunctionModifier();
		/// <summary>
		/// The return type of this function.
		/// </summary>
		[Tooltip("The return type of this function.")]
		[Filter(OnlyGetType = true, UnityReference = false, VoidType = true), HideInInspector]
		public SerializedType returnType = typeof(void);

		/// <summary>
		/// The list of attribute on this function.
		/// </summary>
		[HideInInspector]
		public List<AttributeData> attributes = new List<AttributeData>();
		/// <summary>
		/// The list of generic parameter on this function.
		/// </summary>
		[HideInInspector]
		public GenericParameterData[] genericParameters = new GenericParameterData[0];

		List<AttributeData> IAttributeSystem.Attributes {
			get {
				return attributes;
			}
		}

		public IList<GenericParameterData> GenericParameters {
			get {
				return genericParameters;
			}
			set {
				if(value is GenericParameterData[]) {
					genericParameters = value as GenericParameterData[];
					return;
				}
				genericParameters = value.ToArray();
			}
		}

		public override bool AllowCoroutine() {
			if(returnType != null && returnType.isAssigned) {
				System.Type rType = returnType;
				return rType == typeof(IEnumerable) || rType == typeof(IEnumerator) || rType.HasImplementInterface(typeof(IEnumerator<>)) || rType.HasImplementInterface(typeof(IEnumerable<>));
			}
			return false;
		}

		#region Invokes
		/// <summary>
		/// Invoke this function.
		/// </summary>
		/// <returns></returns>
		public object Invoke(GraphInstance instance) {
			return Invoke(instance, null);
		}

		public object Invoke(GraphInstance instance, object[] parameter) {
			//In case it is a virtual/override function
			if(instance.graph != graphContainer && (modifier.Virtual || modifier.Override || graphContainer.IsInterface())) {
				var pTypes = parameters.Select(p => p.Type).ToArray();
				var graph = instance.graph;
				Function function = null;
				while(function == null) {
					function = graph.GetFunction(name, pTypes);
					if(function == null) {
						var inheritType = graph.GetGraphInheritType();
						if(inheritType is IRuntimeMemberWithRef runtime) {
							graph = runtime.GetReferenceValue() as IGraph;
							if(graph == null)
								throw null;
						}
						else {
							throw null;
						}
					}
				}
				return function.DoInvoke(instance, parameter);
			}
			return DoInvoke(instance, parameter);
		}

		/// <summary>
		/// Invoke this function.
		/// </summary>
		/// <param name="parameter"></param>
		/// <returns></returns>
		internal object DoInvoke(GraphInstance instance, object[] parameter) {
			//if((parameter == null || parameter.Length == 0) && owner is uNodeRuntime runtime) {
			//	//Invoke Custom Event in State Graph
			//	if(runtime.customMethod.TryGetValue(Name, out var method)) {
			//		method.Trigger();
			//	}
			//}
			var outPort = Entry.exit;
			if (outPort.GetTargetFlow() == null) {
				if(returnType != typeof(void)) {
					throw new Exception("The output port of the Entry is not assigned.");
				}
				return null;
			}

			void InitializeParameterAndLocalVariable(Flow flow) {
				//Init local variable to initial value
				if(LocalVariables != null) {
					foreach(var data in LocalVariables) {
						if(data.resetOnEnter) {
							flow.SetLocalData(data, data.defaultValue);
						}
					}
				}
				if(parameter != null) {
					for(int i = 0; i < parameter.Length; i++) {
						if(parameters[i].Type.IsValueType && parameters[i].isByRef == false) {
							flow.SetLocalData(null, parameters[i], ReflectionUtils.ValuePassing(parameter[i]));
						}
						else {
							flow.SetLocalData(null, parameters[i], parameter[i]);
						}
						//parameters[i].value = parameter[i];
					}
					//Debug.Log($"Invoking function: {Name} with parameters:{string.Join(", ", parameter)}");
				}
			}

			Type rType = returnType;
			if(outPort.IsCoroutine()) {
				if(rType.HasImplementInterface(typeof(IEnumerator<>))) {
					var flow = new CoroutineGraphRunner(instance, Entry.enter);
					var iterator = flow.NewCoroutine();

					InitializeParameterAndLocalVariable(iterator);

					var method = typeof(IteratorUtilities).GetMemberCached(nameof(IteratorUtilities.WrapEnumerator)) as System.Reflection.MethodInfo;
					method = method.MakeGenericMethod(rType.GenericTypeArguments[0]);
					return method.InvokeOptimized(null, iterator.GetIterator());
				}
				else if(rType.HasImplementInterface(typeof(IEnumerable<>))) {
					var flow = new CoroutineGraphRunner(instance, Entry.enter);
					var iterator = flow.NewCoroutine();

					InitializeParameterAndLocalVariable(iterator);

					var method = typeof(IteratorUtilities).GetMemberCached(nameof(IteratorUtilities.WrapEnumerable)) as System.Reflection.MethodInfo;
					method = method.MakeGenericMethod(rType.GenericTypeArguments[0]);
					return method.InvokeOptimized(null, iterator.GetIterator());
				}
				else if(rType == typeof(IEnumerable)) {
					return new IteratorUtilities.IEnumarableWrapper() {
						instance = instance,
						port = Entry.enter,
						onInitialize = InitializeParameterAndLocalVariable,
					};
				}
				else if(rType == typeof(IEnumerator)) {
					var flow = new CoroutineGraphRunner(instance, Entry.enter);
					var iterator = flow.NewCoroutine();

					InitializeParameterAndLocalVariable(iterator);

					return iterator.GetIterator();
				}
				throw new NotSupportedException(rType.FullName);
			}
			else {
				var runner = new RegularGraphRunner(instance, Entry.enter);
				var flow = runner.New();

				InitializeParameterAndLocalVariable(flow);

				flow.Run();

				if(HasRefOrOut) {
					if(parameter != null) {
						for(int i = 0; i < parameter.Length; i++) {
							parameter[i] = flow.GetLocalData(null, parameters[i]);
						}
					}
				}

				if(rType != null && rType != typeof(void)) {
					var js = flow.jumpStatement;
					if(js == null || js.jumpType != JumpStatementType.Return) {
						throw new Exception("No return value in function:" + name);
					}
					else {
						return js.value;
					}
				}
				return null;
			}
		}

		static class IteratorUtilities {
			public struct IEnumarableWrapper : IEnumerable {
				public GraphInstance instance;
				public FlowInput port;
				public Action<Flow> onInitialize;

				public IEnumerator GetEnumerator() {
					var flow = new CoroutineGraphRunner(instance, port).NewCoroutine();
					onInitialize(flow);
					return flow.GetIterator();
				}
			}

			public struct IEnumerableWrapper<T> : IEnumerable<T> {
				public IEnumerator<T> target;

				public IEnumerator<T> GetEnumerator() {
					return target;
				}

				IEnumerator IEnumerable.GetEnumerator() {
					return target;
				}
			}

			public struct IEnumeratorWrapper<T> : IEnumerator<T> {
				public IEnumerator target;

				public T Current => target.Current.ConvertTo<T>();
				object IEnumerator.Current => target.Current;

				public void Dispose() {

				}

				public bool MoveNext() {
					return target.MoveNext();
				}

				public void Reset() {
					target.Reset();
				}
			}

			public static IEnumerator<T> WrapEnumerator<T>(IEnumerator iterator) {
				return new IEnumeratorWrapper<T>() { target = iterator };
			}

			public static IEnumerable<T> WrapEnumerable<T>(IEnumerator iterator) {
				return new IEnumerableWrapper<T>() { target = WrapEnumerator<T>(iterator) };
			}
		}
		#endregion

		#region Create Delegates
		public Delegate GetDelegate(GraphInstance instance) {
			return instance.GetDelegate(this);
		}
		#endregion

		/// <summary>
		/// Get the generic parameter by name
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public GenericParameterData GetGenericParameter(string name) {
			for(int i = 0; i < genericParameters.Length; i++) {
				if(genericParameters[i].name == name) {
					return genericParameters[i];
				}
			}
			return null;
			//throw new System.Exception(name + " GenericParameter not found");
		}

		/// <summary>
		/// Get the return type of this function.
		/// </summary>
		/// <returns></returns>
		public override System.Type ReturnType() {
			if(returnType != null && returnType.isAssigned) {
				return returnType;
			}
			return typeof(void);
		}

		[System.Runtime.Serialization.OnDeserialized]
		void Init() {
			if(modifier == null)
				modifier = new FunctionModifier();
		}

		Type IIcon.GetIcon() {
			return typeof(TypeIcons.MethodIcon);
		}

		#region Constructors
		public Function() { }

		public Function(string name, Type returnType = null, IEnumerable<ParameterData> parameters = null) {
			this.name = name;
			this.returnType = returnType ?? typeof(void);
			if(parameters != null)
				this.parameters = new List<ParameterData>(parameters);
		}
		#endregion
	}
}