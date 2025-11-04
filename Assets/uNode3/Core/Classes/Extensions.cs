using MaxyGames.OdinSerializer.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace MaxyGames.UNode {
	public static class Extensions {
		#region Variables
		private static readonly Type[][] typeHierarchy = {
			new Type[] { typeof(Byte), typeof(SByte), typeof(Char)},
			new Type[] { typeof(Int16), typeof(UInt16)},
			new Type[] { typeof(Int32), typeof(UInt32)},
			new Type[] { typeof(Int64), typeof(UInt64)},
			new Type[] { typeof(Single)},
			new Type[] { typeof(Double)}
		};
		private static Dictionary<Type, string> shorthandMap = new Dictionary<Type, string> {
			{ typeof(Boolean), "bool" },
			{ typeof(Byte), "byte" },
			{ typeof(Char), "char" },
			{ typeof(Decimal), "decimal" },
			{ typeof(Double), "double" },
			{ typeof(Single), "float" },
			{ typeof(Int32), "int" },
			{ typeof(Int64), "long" },
			{ typeof(SByte), "sbyte" },
			{ typeof(Int16), "short" },
			{ typeof(string), "string" },
			{ typeof(UInt32), "uint" },
			{ typeof(UInt64), "ulong" },
			{ typeof(UInt16), "ushort" },
			{ typeof(void), "void" },
			{ typeof(object), "object" },
		};
		private static Dictionary<Type, Dictionary<Type, bool>> castableMap = new Dictionary<Type, Dictionary<Type, bool>>();
		private static Dictionary<Type, Dictionary<Type, bool>> castableMap2 = new Dictionary<Type, Dictionary<Type, bool>>();
		private static Dictionary<MemberInfo, Dictionary<Type, bool>> definedMap = new Dictionary<MemberInfo, Dictionary<Type, bool>>();
		private static Dictionary<Enum, Dictionary<Enum, bool>> enumsMap = new Dictionary<Enum, Dictionary<Enum, bool>>();
		private static object _lockObject = new object();
		private static object _lockObject2 = new object();
		#endregion

		#region Reflection
		private static Dictionary<long, FieldInfo> cachedFields = new Dictionary<long, FieldInfo>();
		private static Dictionary<long, PropertyInfo> cachedProperties = new Dictionary<long, PropertyInfo>();
		private static Dictionary<long, MemberInfo> cachedMembers = new Dictionary<long, MemberInfo>();

		public static FieldInfo GetFieldCached(this Type type, string name) {
			var uid = uNodeUtility.GetHashCode((long)type.GetHashCode(), (long)name.GetHashCode());
			if(!cachedFields.TryGetValue(uid, out var result)) {
				result = type.GetField(name, MemberData.flags);
				cachedFields.Add(uid, result);
			}
			return result;
		}

		public static PropertyInfo GetPropertyCached(this Type type, string name) {
			if(type == null)
				throw new ArgumentNullException(nameof(type));
			var uid = uNodeUtility.GetHashCode((long)type.GetHashCode(), (long)name.GetHashCode());
			if(!cachedProperties.TryGetValue(uid, out var result)) {
				result = type.GetProperty(name, MemberData.flags);
				cachedProperties.Add(uid, result);
			}
			return result;
		}

		public static MemberInfo GetMemberCached(this Type type, string name) {
			if(type == null)
				throw new ArgumentNullException(nameof(type));
			var uid = uNodeUtility.GetHashCode((long)type.GetHashCode(), (long)name.GetHashCode());
			if(!cachedMembers.TryGetValue(uid, out var result)) {
				result = type.GetMember(name, MemberData.flags).FirstOrDefault();
				if(result == null) {
					type = type.BaseType;
					if(type == null)
						return null;
					while(result == null && type != typeof(object)) {
						result = type.GetMember(name, BindingFlags.Instance | BindingFlags.NonPublic).FirstOrDefault();
						type = type.BaseType;
					}
				}
				cachedMembers.Add(uid, result);
			}
			return result;
		}
		#endregion

		#region Ports
		/// <summary>
		/// Establishes a connection between the specified source and destination ports.
		/// </summary>
		/// <remarks>This method creates a connection between the two specified ports by invoking the underlying
		/// connection logic. Ensure that both ports are valid and compatible before calling this method.</remarks>
		/// <param name="source">The source port to connect from. Cannot be <c>null</c>.</param>
		/// <param name="destination">The destination port to connect to. Cannot be <c>null</c>.</param>
		public static void ConnectTo(this UPort source, UPort destination) {
			Connection.CreateAndConnect(source, destination);
		}

		/// <summary>
		/// Connects the specified <see cref="ValueInput"/> source to the given <see cref="MemberData"/> destination.
		/// </summary>
		/// <remarks>This method establishes a connection between the source and destination, assigning the source to
		/// the default value of the destination.</remarks>
		/// <param name="source">The source <see cref="ValueInput"/> to be connected.</param>
		/// <param name="destination">The <see cref="MemberData"/> instance to which the source will be connected.</param>
		public static void ConnectTo(this ValueInput source, MemberData destination) {
			source.AssignToDefault(destination);
		}

		/// <summary>
		/// Establishes a connection between the specified source and destination ports,  designating the connection as a
		/// proxy.
		/// </summary>
		/// <remarks>This method creates a connection between the two specified ports and marks the connection  as a
		/// proxy by setting the <c>isProxy</c> property to <see langword="true"/>.  Ensure that both <paramref
		/// name="source"/> and <paramref name="destination"/> are valid  and properly initialized before calling this
		/// method.</remarks>
		/// <param name="source">The source <see cref="UPort"/> to initiate the connection from.</param>
		/// <param name="destination">The destination <see cref="UPort"/> to connect to.</param>
		public static void ConnectToAsProxy(this UPort source, UPort destination) {
			Connection.CreateAndConnect(source, destination).isProxy = true;
		}

		/// <summary>
		/// Set the port name
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="port"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		public static T SetName<T>(this T port, string name) where T : UPort {
			port.name = name;
			return port;
		}

		/// <summary>
		/// Set the port title
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="port"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		public static T SetTitle<T>(this T port, string title) where T : UPort {
			port.title = title;
			return port;
		}

		/// <summary>
		/// Set the port title
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="port"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		public static T SetTitle<T>(this T port, Func<string> title) where T : UPort {
			port.m_title = title;
			return port;
		}

		/// <summary>
		/// Set the port tooltip
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="port"></param>
		/// <param name="tooltip"></param>
		/// <returns></returns>
		public static T SetTooltip<T>(this T port, string tooltip) where T : UPort {
			if(port == null)
				return null;
			port.tooltip = tooltip;
			return port;
		}

		/// <summary>
		/// Set the port type
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="port"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public static T SetType<T>(this T port, Type type) where T : ValuePort {
			port.type = type;
			return port;
		}

		/// <summary>
		/// Set the port type
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="port"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public static T SetType<T>(this T port, Func<Type> type) where T : ValuePort {
			port.type = null;
			port.dynamicType = type;
			return port;
		}

		public static bool IsPrimaryPort(this UPort port) {
			if(port is ValueOutput) {
				return port.node != null && port.node.primaryValueOutput == port;
			}
			else if(port is FlowInput) {
				return port.node != null && port.node.primaryFlowInput == port;
			}
			else if(port is FlowOutput) {
				return port.node != null && port.node.primaryFlowOutput == port;
			}
			return false;
		}
		#endregion

		//public static bool Contains<T>(this T[] array, T value) {
		//	return (array as IList<T>).Contains(value);
		//}

		//public static void AddRange<T>(this HashSet<T> hash, IEnumerable<T> values) {
		//	foreach(var value in values) {
		//		hash.Add(value);
		//	}
		//}

		/// <summary>
		/// Indicates whether custom attributes of a specified type are applied to a specified member.
		/// </summary>
		/// <param name="info"></param>
		/// <param name="attributeType"></param>
		/// <returns></returns>
		public static bool IsDefinedAttribute(this MemberInfo info, Type attributeType) {
			Dictionary<Type, bool> map = null;
			lock(_lockObject) {
				if(definedMap.TryGetValue(info, out map)) {
					bool val;
					if(map.TryGetValue(attributeType, out val)) {
						return val;
					}
				}
			}
			bool result = info.IsDefined(attributeType, true);
			lock(_lockObject) {
				if(map == null) {
					map = new Dictionary<Type, bool>();
				}
				map[attributeType] = result;
				definedMap[info] = map;
			}
			return result;
		}

		/// <summary>
		/// Indicates whether custom attributes of a specified type are applied to a specified member.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="info"></param>
		/// <returns></returns>
		public static bool IsDefinedAttribute<T>(this MemberInfo info) {
			return IsDefinedAttribute(info, typeof(T));
		}

		/// <summary>
		/// Indicates whether custom attributes of a specified type are applied to a specified member.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="info"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public static bool IsDefinedAttribute<T>(this MemberInfo info, out T value) where T : System.Attribute {
			if(IsDefinedAttribute(info, typeof(T))) {
				value = info.GetCustomAttribute<T>();
				return true;
			}
			value = null;
			return false;
		}

		#region Graphs
		/// <summary>
		/// Get the inherited graph type
		/// </summary>
		/// <param name="graph"></param>
		/// <returns></returns>
		public static Type GetGraphInheritType(this IGraph graph) {
			if(graph is IClassGraph classGraph) {
				return classGraph.InheritType;
			}
			return typeof(object);
		}

		/// <summary>
		/// Get the graph type
		/// </summary>
		/// <param name="graph"></param>
		/// <returns></returns>
		public static Type GetGraphType(this IGraph graph) {
			if(graph is IReflectionType reflectionType) {
				return reflectionType.ReflectionType;
			}
			else if(graph is IClassGraph classGraph) {
				return classGraph.InheritType;
			}
			return null;
		}

		/// <summary>
		/// Return the member info if any.
		/// </summary>
		/// <param name="variable"></param>
		/// <returns></returns>
		public static FieldInfo GetMemberInfo(this Variable variable) {
			var type = variable.graphContainer.GetGraphType();
			if(type != null) {
				var member = type.GetField(variable.name, MemberData.flags);
				if(member != null && member is IRuntimeMemberWithRef withRef && object.ReferenceEquals(withRef.GetReferenceValue(), variable)) {
					return member;
				}
			}
			return null;
		}

		/// <summary>
		/// Return the member info if any.
		/// </summary>
		/// <param name="property"></param>
		/// <returns></returns>
		public static PropertyInfo GetMemberInfo(this Property property) {
			var type = property.graphContainer.GetGraphType();
			if(type != null) {
				var member = type.GetProperty(property.name, MemberData.flags);
				if(member != null && member is IRuntimeMemberWithRef withRef && object.ReferenceEquals(withRef.GetReferenceValue(), property)) {
					return member;
				}
			}
			return null;
		}

		/// <summary>
		/// Return the member info if any.
		/// </summary>
		/// <param name="property"></param>
		/// <returns></returns>
		public static MethodInfo GetMemberInfo(this Function function) {
			var type = function.graphContainer.GetGraphType();
			if(type != null) {
				var members = type.GetMethods(MemberData.flags);
				foreach(var member in members) {
					if(member != null && member is IRuntimeMemberWithRef withRef && object.ReferenceEquals(withRef.GetReferenceValue(), function)) {
						return member;
					}
				}
			}
			return null;
		}

		/// <summary>
		/// Get the name of the graph
		/// </summary>
		/// <param name="graph"></param>
		/// <returns></returns>
		public static string GetGraphName(this IGraph graph) {
			if(graph is IScriptGraphType scriptGraph) {
				return scriptGraph.ScriptName;
			}
			if(graph is IClassGraph) {
				return (graph as IClassGraph).GraphName;
			}
			if(string.IsNullOrEmpty(graph.GraphData.name) && graph is UnityEngine.Object unityObject) {
				return unityObject.name;
			}
			return graph.GraphData.name;
		}

		/// <summary>
		/// Get the ID of the graph
		/// </summary>
		/// <param name="graph"></param>
		/// <returns></returns>
		public static int GetGraphID(this IGraph graph) {
			if(graph == null) return -1;
			if(graph is UnityEngine.Object obj) {
				return uNodeUtility.GetObjectID(obj);
			}
			return graph.GetHashCode();
			//return graph.GraphData.id;
		}

		public static string GetGraphNamespace(this IGraph graph) {
			if(graph == null) return string.Empty;
			if(graph is ITypeGraph typeGraph) {
				return typeGraph.Namespace;
			}
			if(graph is IInstancedGraph instancedGraph) {
				var originalGraph = instancedGraph.OriginalGraph;
				if(originalGraph != graph) {
					return GetGraphNamespace(originalGraph);
				}
			}
			if(graph is INamespace) {
				return (graph as INamespace).Namespace;
			}
			return string.Empty;
		}

		public static string GetFullGraphName(this IGraph graph) {
			if(graph == null) return string.Empty;
			if(graph is ITypeGraph typeGraph) {
				return typeGraph.FullGraphName;
			}
			if(graph is IInstancedGraph instancedGraph) {
				var originalGraph = instancedGraph.OriginalGraph;
				if(originalGraph != graph) {
					return GetFullGraphName(originalGraph);
				}
			}
			if (string.IsNullOrEmpty(graph.GraphData.name) && graph is UnityEngine.Object unityObject) {
				return unityObject.name;
			}
			return graph.GraphData.name;
		}

		/// <summary>
		/// Is the 'type' is implement an interface 'interfaceType'
		/// </summary>
		/// <param name="graph"></param>
		/// <param name="interfaceType"></param>
		/// <returns></returns>
		public static bool HasImplementInterface(this IGraph graph, Type interfaceType) {
			if(graph is IReflectionType reflectionType) {
				return HasImplementInterface(reflectionType.ReflectionType, interfaceType);
			}
			else if(graph is IInterfaceSystem interfaceSystem) {
				return interfaceSystem.Interfaces.Any(iface => ReflectionUtils.IsTypeEqual(iface, interfaceType));
			}
			return false;
		}

		/// <summary>
		/// True if the element is not destroyed or destroyed with safe mode.
		/// </summary>
		/// <param name="element"></param>
		/// <returns></returns>
		public static bool IsValidElement(this UGraphElement element) {
			return !object.ReferenceEquals(element, null) && element.IsValid;
		}

		/// <summary>
		/// True if the variable can be added to the graph
		/// </summary>
		/// <param name="graph"></param>
		/// <returns></returns>
		public static bool CanAddVariable(this Graph graph) {
			return graph?.graphContainer is IGraphWithVariables;
		}

		/// <summary>
		/// True if the property can be added to the graph
		/// </summary>
		/// <param name="graph"></param>
		/// <returns></returns>
		public static bool CanAddProperty(this Graph graph) {
			return graph?.graphContainer is IGraphWithProperties;
		}

		/// <summary>
		/// True if the function can be added to the graph
		/// </summary>
		/// <param name="graph"></param>
		/// <returns></returns>
		public static bool CanAddFunction(this Graph graph) {
			return graph?.graphContainer is IGraphWithFunctions;
		}

		/// <summary>
		/// True if the constructor can be added to the graph
		/// </summary>
		/// <param name="graph"></param>
		/// <returns></returns>
		public static bool CanAddConstructor(this Graph graph) {
			return graph?.graphContainer is IGraphWithConstructors;
		}

		/// <summary>
		/// True if the graph is Native Graph
		/// </summary>
		/// <param name="graph"></param>
		/// <returns></returns>
		public static bool IsNativeGraph(this IGraph graph) {
			if(graph is IReflectionType) {
				var type = ReflectionUtils.GetRuntimeType(graph);
				if(type != null) {
					return type is INativeMember;
				}
			}
			return false;
		}

		public static bool IsInterface(this IGraph graph) {
			if(graph is IClassGraph) {
				return (graph as IClassGraph).IsInterface;
			}
			else if(graph is IReflectionType) {
				var type = ReflectionUtils.GetRuntimeType(graph);
				if(type != null) {
					return type.IsInterface;
				}
			}
			return false;
		}

		public static HashSet<string> GetUsingNamespaces(this IGraph graph) {
			if(graph is IUsingNamespace system) {
				var result = new HashSet<string>();
				foreach(var ns in system.UsingNamespaces) {
					result.Add(ns);
				}
				if(graph is INamespace) {
					var namespaces = (graph as INamespace).Namespace;
					if(!string.IsNullOrEmpty(namespaces)) {
						var strs = namespaces.Split('.');
						for(int i = 0; i < strs.Length; i++) {
							result.Add(string.Join('.', strs, 0, i + 1));
						}
					}
				}
				return result;
			}
			else if(graph is IScriptGraphType scriptGraphType) {
				return scriptGraphType.ScriptTypeData.scriptGraph.GetUsingNamespaces();
			}
			return new HashSet<string>();
		}

		public static HashSet<string> GetUsingNamespaces(this IScriptGraph scriptGraph) {
			var result = new HashSet<string>();
			foreach(var ns in scriptGraph.UsingNamespaces) {
				result.Add(ns);
			}
			{
				var namespaces = scriptGraph.Namespace;
				if(!string.IsNullOrEmpty(namespaces)) {
					var strs = namespaces.Split('.');
					for(int i = 0; i < strs.Length; i++) {
						result.Add(string.Join('.', strs, 0, i + 1));
					}
				}
			}
			return result;
		}

		/// <summary>
		/// Get all defined variable
		/// </summary>
		/// <param name="graph"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		public static IEnumerable<Variable> GetVariables(this IGraph graph) {
			if(graph == null)
				throw new ArgumentNullException(nameof(graph));
			var graphData = graph.GraphData;
			return graphData.variableContainer.collections;
		}

		/// <summary>
		/// Get all variable including inheritance variable
		/// </summary>
		/// <param name="graph"></param>
		/// <returns></returns>
		public static IEnumerable<Variable> GetAllVariables(this IGraph graph) {
			if(graph == null)
				throw new ArgumentNullException(nameof(graph));
			var inherited = RuntimeGraphUtility.GetInheritedGraph(graph);
			if(inherited != null) {
				foreach(var variable in GetAllVariables(inherited)) {
					yield return variable;
				}
			}
			var graphData = graph.GraphData;
			foreach(var variable in graphData.variableContainer.collections) {
				yield return variable;
			}
		}

		/// <summary>
		/// Get all defined property
		/// </summary>
		/// <param name="graph"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		public static IEnumerable<Property> GetProperties(this IGraph graph) {
			if(graph == null)
				throw new ArgumentNullException(nameof(graph));
			var graphData = graph.GraphData;
			return graphData.propertyContainer.collections;
		}

		/// <summary>
		/// Get all defined function
		/// </summary>
		/// <param name="graph"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		public static IEnumerable<Function> GetFunctions(this IGraph graph) {
			if(graph == null)
				throw new ArgumentNullException(nameof(graph));
			var graphData = graph.GraphData;
			return graphData.functionContainer.collections;
		}

		/// <summary>
		/// Get all defined constructor
		/// </summary>
		/// <param name="graph"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		public static IEnumerable<Constructor> GetConstructors(this IGraph graph) {
			if(graph == null)
				throw new ArgumentNullException(nameof(graph));
			var graphData = graph.GraphData;
			return graphData.constructorContainer.collections;
		}

		public static UGraphElement GetGraphElement(this IGraph graph, int id) {
			if(graph == null)
				throw new ArgumentNullException(nameof(graph));
			var graphData = graph.GraphData;
			return graphData?.GetElementByID(id);
		}

		public static T GetGraphElement<T>(this IGraph graph, int id) where T : UGraphElement {
			if(graph == null)
				throw new ArgumentNullException(nameof(graph));
			var graphData = graph.GraphData;
			return graphData?.GetElementByID<T>(id);
		}

		public static Property GetProperty(this IGraph graph, string name) {
			if(graph == null)
				throw new ArgumentNullException(nameof(graph));
			foreach(var p in graph.GetProperties()) {
				if(name == p.name) {
					return p;
				}
			}
			return null;
		}

		public static Variable GetVariable(this IGraph graph, string name) {
			if(graph == null)
				throw new ArgumentNullException(nameof(graph));
			foreach(var p in graph.GetVariables()) {
				if(name == p.name) {
					return p;
				}
			}
			return null;
		}

		public static Function GetFunction(this IGraph graph, string name, params System.Type[] parameters) {
			return GetFunction(graph, name, 0, parameters);
		}

		public static Function GetFunction(this IGraph graph, string name, int genericParameterLength, params System.Type[] parameters) {
			int parameterLength = parameters != null ? parameters.Length : 0;
			foreach(var function in graph.GetFunctions()) { 
				if(function.name == name && function.parameters.Count == parameterLength && function.genericParameters.Length == genericParameterLength) {
					bool isValid = true;
					if(parameterLength != 0) {
						for(int x = 0; x < parameters.Length; x++) {
							var pType = parameters[x];
							if(pType.IsByRef) {
								pType = pType.GetElementType();
							}
							if(genericParameterLength > 0) {
								if(function.parameters[x].Type != null && ReflectionUtils.IsTypeEqual(function.parameters[x].Type, pType) == false) {
									isValid = false;
									break;
								}
							} else if(function.parameters[x].Type != null && !function.parameters[x].Type.IsGenericTypeDefinition && ReflectionUtils.IsTypeEqual(function.parameters[x].Type, pType) == false) {
								isValid = false;
								break;
							}
						}
					}
					if(isValid) {
						return function;
					}
				}
			}
			return null;
		}

		public static T GetNodeInChildren<T>(this UGraphElement element, bool recursive = false) {
			var obj = element.GetObjectInChildren<NodeObject>(n => n.node is T, recursive);
			if(obj?.node is T result) {
				return result;
			}
			return default;
		}

		/// <summary>
		/// Add a new child node to the <paramref name="parent"/>
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="parent"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public static T AddChildNode<T>(this UGraphElement parent, T value) where T : Node {
			parent.AddChild(new NodeObject(value));
			return value;
		}

		public static IEnumerable<T> GetNodesInChildren<T>(this UGraphElement element, bool recursive = false) {
			if(recursive) {
				foreach(var child in element) {
					if(child is NodeObject node && node.node is T c) {
						yield return c;
					}
					foreach(var cc in child.GetNodesInChildren<T>(recursive)) {
						yield return cc;
					}
				}
			} else {
				foreach(var child in element) {
					if(child is NodeObject node && node.node is T c) {
						yield return c;
					}
				}
			}
		}

		/// <summary>
		/// Traverses the parent hierarchy of the specified <see cref="UGraphElement"/> to find a node of the specified type.
		/// </summary>
		/// <remarks>If the graph associated with the specified <paramref name="element"/> has a linked owner, the
		/// method will also search the parent hierarchy of the linked owner.</remarks>
		/// <typeparam name="T">The type of the node to search for.</typeparam>
		/// <param name="element">The starting <see cref="UGraphElement"/> from which to begin the search.</param>
		/// <returns>The first node of type <typeparamref name="T"/> found in the parent hierarchy, or <see langword="default"/> if no
		/// such node exists.</returns>
		public static T GetNodeInParent<T>(this UGraphElement element) {
			var parent = element;
			while(parent != null) {
				if(parent is NodeObject p && p.node is T result) {
					return result;
				}
				parent = parent.parent;
			}
			//In case it is linked graph
			if(element.graph.linkedOwner != null) {
				return GetNodeInParent<T>(element.graph.linkedOwner);
			}
			return default;
		}

		/// <summary>
		/// Traverses the parent hierarchy of the specified <see cref="UGraphElement"/> to find an object or node of the
		/// specified type.
		/// </summary>
		/// <remarks>This method searches the parent hierarchy of the provided <paramref name="element"/> for an
		/// object or node of type <typeparamref name="T"/>. If no match is found in the hierarchy, the method will also check
		/// the linked owner of the graph, if applicable.</remarks>
		/// <typeparam name="T">The type of object or node to search for in the parent hierarchy.</typeparam>
		/// <param name="element">The starting <see cref="UGraphElement"/> from which to begin the search.</param>
		/// <returns>An instance of type <typeparamref name="T"/> if found in the parent hierarchy or linked graph; otherwise, the
		/// default value for type <typeparamref name="T"/>.</returns>
		public static T GetObjectOrNodeInParent<T>(this UGraphElement element) {
			var parent = element;
			while(parent != null) {
				if(parent is T r) {
					return r;
				} else if(parent is NodeObject p && p.node is T result) {
					return result;
				}
				parent = parent.parent;
			}
			//In case it is linked graph
			if(element.graph.linkedOwner != null) {
				return GetObjectOrNodeInParent<T>(element.graph.linkedOwner);
			}
			return default;
		}
		#endregion

		#region Runtimes
		public static T GetVariable<T>(this IRuntimeVariable instance, string name) {
			var obj = instance.GetVariable(name);
			if(obj != null) {
				return (T)obj;
			}
			return default;
		}

		public static T GetProperty<T>(this IRuntimeProperty instance, string name) {
			var obj = instance.GetProperty(name);
			if(obj != null) {
				return (T)obj;
			}
			return default;
		}

		public static object InvokeFunctionByID(this IRuntimeFunction instance, string graphID, int functionID, object[] values) {
			return uNodeHelper.RuntimeUtility.InvokeFunctionByID(instance, graphID, functionID, values);
		}
		#endregion


		/// <summary>
		/// Get parameter data by name.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public static ParameterData GetParameterByName(this IParameterSystem system, string name) {
			foreach(var p in system.Parameters) {
				if(p.name == name) {
					return p;
				}
			}
			return null;
		}
		/// <summary>
		/// Get parameter data by ID.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public static ParameterData GetParameterByID(this IParameterSystem system, string id) {
			foreach(var p in system.Parameters) {
				if(p.id == id) {
					return p;
				}
			}
			return null;
		}

		/// <summary>
		/// Get the attributes from a attribute system
		/// </summary>
		/// <param name="attributeSystem"></param>
		/// <returns></returns>
		public static object[] GetAttributes(this IAttributeSystem attributeSystem) {
			var attribut = attributeSystem.Attributes;
			if(attribut == null) {
				return new object[0];
			}
			object[] att = new object[attribut.Count];
			for (int i = 0; i < att.Length;i++) {
				att[i] = attribut[i].Get();
			}
			return att;
		}

		/// <summary>
		/// Get the attributes from a attribute system
		/// </summary>
		/// <param name="attributeSystem"></param>
		/// <returns></returns>
		public static object[] GetAttributes(this IAttributeSystem attributeSystem, Type attributeType) {
			var attribut = attributeSystem.Attributes;
			if(attribut == null) {
				return new object[0];
			}
			for (int i = 0; i < attribut.Count;i++) {
				if(attribut[i].attributeType.type == attributeType) {
					return new object[] { attribut[i].Get() };
				}
			}
			return new object[0];
		}

		/// <summary>
		/// Is the type is Runtime Type?
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static bool IsRuntimeType(this Type type) {
			return type is RuntimeType;
		}

		/// <summary>
		/// Is the obj is instance of type of T
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="obj"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public static bool IsTypeOf<T>(this object obj) {
			//if(obj is uNodeSpawner spawner) {
			//	return spawner.GetRuntimeInstance() is T;
			//} else if(obj is uNodeAssetInstance asset) {
			//	return asset.GetRuntimeInstance() is T;
			//}
			return obj is T;
		}

		/// <summary>
		/// Is the obj is instance of type of T
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="obj"></param>
		/// <returns></returns>
		public static bool IsElementTypeOf<T>(this UGraphElement obj) {
			if(obj is T) return true;
			if(obj is NodeObject node) {
				return node.node is T;
			}
			return false;
		}

		/// <summary>
		/// Is the obj is instance of type of T
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="obj"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public static bool IsElementTypeOf<T>(this UGraphElement obj, out T value) {
			if(obj is T val) {
				value = val;
				return true;
			}
			if(obj is NodeObject node) {
				if(node.node is T v) {
					value = v;
					return true;
				}
			}
			value = default;
			return false;
		}

		/// <summary>
		/// Is the obj is instance of type of 'type'
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="obj"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public static bool IsTypeOf(this object obj, Type type) {
			if(obj is IRuntimeClass runtime && type is RuntimeType) {
				return ReflectionUtils.IsCastableTo(runtime, type as RuntimeType);
			}
			return Operator.TypeIs(obj, type);
		}

		/// <summary>
		/// Is the obj is instance of type with unique ID of 'uniqueID'
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="uniqueID"></param>
		/// <returns></returns>
		public static bool IsTypeOf(this object obj, string uniqueID) {
			if(obj is IRuntimeClass comp) {
				var type = TypeSerializer.GetRuntimeType(uniqueID);
				if(type == null) {
					throw new Exception($"No graph with name: {uniqueID} found, maybe the graph was removed or database is outdated.");
				}
				return ReflectionUtils.IsCastableTo(comp, type);
			}
			return false;
		}

		/// <summary>
		/// Convert the instance of obj into T
		/// </summary>
		/// <param name="obj"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public static T ConvertTo<T>(this object obj) {
			if(object.ReferenceEquals(obj, null)) {
				return default;
			}
			try {
				return Operator.Convert<T>(obj);
			} catch (InvalidCastException ex) {
				throw new InvalidCastException($"Cannot convert: {obj.GetType()} to : {typeof(T)}", ex);
			}
		}

		/// <summary>
		/// Convert the instance of obj into T
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="obj"></param>
		/// <returns></returns>
		public static T ToRuntimeInstance<T>(this object obj) {
			if(object.ReferenceEquals(obj, null)) {
				return default;
			}
			if(obj is IRuntimeClassContainer container) {
				try {
					return (T)container.RuntimeClass;
				}
				catch(InvalidCastException) {
					throw new InvalidCastException($"Cannot convert: {container.RuntimeClass.GetType()} to : {typeof(T)}");
				}
			}
			try {
				return (T)obj;
			} catch(InvalidCastException) {
				throw new InvalidCastException($"Cannot convert: {obj.GetType()} to : {typeof(T)}");
			}
		}

		/// <summary>
		/// Convert the instance of obj into T
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="obj"></param>
		/// <returns></returns>
		public static T ToRuntimeInstance<T>(this UnityEngine.Component obj) {
			if(obj == null) {
				return default;
			}
			if(obj is IRuntimeClassContainer container) {
				try {
					return (T)container.RuntimeClass;
				}
				catch(InvalidCastException) {
					throw new InvalidCastException($"Cannot convert: {container.RuntimeClass.GetType()} to : {typeof(T)}");
				}
			}
			try {
				if(obj is T) {
					return (T)(object)obj;
				} else if(typeof(T) == typeof(UnityEngine.GameObject)) {
					return (T)(object)obj.gameObject;
				} else if(typeof(T).IsSubclassOf(typeof(UnityEngine.Component))) {
					return obj.GetComponent<T>();
				}
				return (T)(object)obj;
			}
			catch(InvalidCastException) {
				throw new InvalidCastException($"Cannot convert: {obj.GetType()} to : {typeof(T)}");
			}
		}

		public static IRuntimeClass ToRuntimeInstance(this object obj, string uniqueID) {
			if(obj == null) {
				return default;
			}
			try {
				if(obj is IRuntimeClass container && IsTypeOf(container, uniqueID)) {
					return obj as IRuntimeClass;
				} else if(obj is UnityEngine.Component comp) {
					return comp.GetGeneratedComponent(uniqueID);
				} else if(obj is UnityEngine.GameObject go) {
					return go.GetGeneratedComponent(uniqueID);
				}
				return null;
			}
			catch(InvalidCastException) {
				throw new InvalidCastException($"Cannot convert: {obj.GetType()} to Runtime Instance with UID : {uniqueID}");
			}
		}

		public static T ToRuntimeInstance<T>(this object obj, string uniqueID) where T : class, IRuntimeClass {
			if(obj == null) {
				return default;
			}
			try {
				if(obj is IRuntimeClass container && IsTypeOf(container, uniqueID)) {
					return obj as T;
				}
				else if(obj is UnityEngine.Component comp) {
					return comp.GetGeneratedComponent(uniqueID) as T;
				}
				else if(obj is UnityEngine.GameObject go) {
					return go.GetGeneratedComponent(uniqueID) as T;
				}
				return null;
			}
			catch(InvalidCastException) {
				throw new InvalidCastException($"Cannot convert: {obj.GetType()} to Runtime Instance with UID : {uniqueID}");
			}
		}

		/// <summary>
		/// Convert the instance of obj into T
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="obj"></param>
		/// <returns></returns>
		public static T ToRuntimeInstance<T>(this UnityEngine.GameObject obj) {
			if(obj == null) {
				return default;
			}
			try {
				if(typeof(T) == typeof(UnityEngine.GameObject)) {
					return (T)(object)obj;
				}
				return obj.GetComponent<T>();
			}
			catch(InvalidCastException) {
				throw new InvalidCastException($"Cannot convert: {obj.GetType()} to : {typeof(T)}");
			}
		}

		/// <summary>
		/// Determines if an enum has the given flag defined bitwise.
		/// Fallback equivalent to .NET's Enum.HasFlag().
		/// </summary>
		public static bool HasFlags<T>(this T value, T flag) where T : Enum {
			Dictionary<Enum, bool> map = null;
			lock(_lockObject2) {
				if(enumsMap.TryGetValue(value, out map)) {
					bool val;
					if(map.TryGetValue(flag, out val)) {
						return val;
					}
				}
			}
			long lValue = System.Convert.ToInt64(value);
			long lFlag = System.Convert.ToInt64(flag);
			bool result = (lValue & lFlag) != 0;
			lock(_lockObject2) {
				if(map == null) {
					map = new Dictionary<Enum, bool>();
				}
				map[flag] = result;
				enumsMap[value] = map;
			}
			return result;
		}

		#region MemberData
		/// <summary>
		/// Indicate is targeting uNode Member whether it's graph, pin, or nodes.
		/// </summary>
		/// <param name="targetType"></param>
		/// <returns></returns>
		public static bool IsTargetingUNode(this MemberData.TargetType targetType) {
			switch (targetType) {
				case MemberData.TargetType.uNodeConstructor:
				case MemberData.TargetType.uNodeFunction:
				case MemberData.TargetType.uNodeGenericParameter:
				case MemberData.TargetType.uNodeLocalVariable:
				case MemberData.TargetType.uNodeIndexer:
				case MemberData.TargetType.uNodeParameter:
				case MemberData.TargetType.uNodeProperty:
				case MemberData.TargetType.uNodeVariable:
				case MemberData.TargetType.uNodeType:
					return true;
			}
			return false;
		}
		
		/// <summary>
		/// True if targetType is Type or uNodeType
		/// </summary>
		/// <param name="targetType"></param>
		/// <returns></returns>
		public static bool IsTargetingType(this MemberData.TargetType targetType) {
			switch (targetType) {
				case MemberData.TargetType.Type:
				case MemberData.TargetType.uNodeType:
					return true;
			}
			return false;
		}

		public static bool IsTargetingReflection(this MemberData.TargetType targetType) {
			switch(targetType) {
				case MemberData.TargetType.Constructor:
				case MemberData.TargetType.Event:
				case MemberData.TargetType.Field:
				case MemberData.TargetType.Method:
				case MemberData.TargetType.Property:
					return true;
			}
			return false;
		}

		/// <summary>
		/// True if targetType is Values, SelfTarget, or Null
		/// </summary>
		/// <param name="targetType"></param>
		/// <returns></returns>	
		public static bool IsTargetingValue(this MemberData.TargetType targetType) {
			switch (targetType) {
				case MemberData.TargetType.Values:
				case MemberData.TargetType.Self:
				case MemberData.TargetType.Null:
					return true;
			}
			return false;
		}
		
		/// <summary>
		/// True if targetType is uNodeVariable or uNodeLocalVariable
		/// </summary>
		/// <param name="targetType"></param>
		/// <returns></returns>
		public static bool IsTargetingVariable(this MemberData.TargetType targetType) {
			switch (targetType) {
				case MemberData.TargetType.uNodeVariable:
				case MemberData.TargetType.uNodeLocalVariable:
					return true;
			}
			return false;
		}
		
		/// <summary>
		/// The target is targeting graph that's return a value except targeting pin or nodes.
		/// </summary>
		/// <param name="targetType"></param>
		/// <returns></returns>
		public static bool IsTargetingGraphValue(this MemberData.TargetType targetType) {
			switch (targetType) {
				case MemberData.TargetType.uNodeConstructor:
				case MemberData.TargetType.uNodeFunction:
				case MemberData.TargetType.uNodeGenericParameter:
				case MemberData.TargetType.uNodeLocalVariable:
				case MemberData.TargetType.uNodeIndexer:
				case MemberData.TargetType.uNodeParameter:
				case MemberData.TargetType.uNodeProperty:
				case MemberData.TargetType.uNodeVariable:
				case MemberData.TargetType.uNodeType:
					return true;
			}
			return false;
		}
		#endregion

		#region String Utils
		/// <summary>
		/// Add tab after new line.
		/// </summary>
		/// <param name="str"></param>
		/// <param name="tabCount"></param>
		/// <param name="removeEmptyLines">if true, empty line will be removed</param>
		/// <returns></returns>
		public static string AddTabAfterNewLine(this string str, bool removeEmptyLines = true) {
			return AddTabAfterNewLine(str, 1, removeEmptyLines);
		}

		/// <summary>
		/// Add tab after new line.
		/// </summary>
		/// <param name="str"></param>
		/// <param name="tabCount"></param>
		/// <param name="removeEmptyLines">if true, empty line will be removed</param>
		/// <returns></returns>
		public static string AddTabAfterNewLine(this string str, int tabCount, bool removeEmptyLines = true) {
			if(!string.IsNullOrEmpty(str)) {
				List<string> s = str.Split('\n').ToList();
				for(int x = 0; x < s.Count; x++) {
					if(removeEmptyLines && string.IsNullOrEmpty(s[x]) && x != 0) {
						s.RemoveAt(x);
						x--;
						continue;
					}
					if(string.IsNullOrEmpty(s[x]))
						continue;
					s[x] = Tab(tabCount) + s[x];
				}
				str = string.Join("\n", s.ToArray());
			}
			return str;
		}

		/// <summary>
		/// Add Tab
		/// </summary>
		/// <param name="count"></param>
		/// <returns></returns>
		private static string Tab(int count) {
			string data = null;
			for(int i = 0; i < count; i++) {
				data += "\t";
			}
			return data;
		}

		/// <summary>
		/// Add string value if value is not null and empty
		/// </summary>
		/// <param name="str"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public static string Add(this string str, string value) {
			if(!string.IsNullOrEmpty(str)) {
				str += value;
			}
			return str;
		}

		/// <summary>
		/// Add string value if value and targetValue is not null and empty
		/// </summary>
		/// <param name="str"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public static string Add(this string str, string value, string targetValue, bool inverted = false) {
			if(inverted) {
				if(!string.IsNullOrEmpty(str) && string.IsNullOrEmpty(targetValue)) {
					str += value;
				}
			} else {
				if(!string.IsNullOrEmpty(str) && !string.IsNullOrEmpty(targetValue)) {
					str += value;
				}
			}
			return str;
		}

		/// <summary>
		/// Add string value if value is not null and addIfCondition is true
		/// </summary>
		/// <param name="str"></param>
		/// <param name="value"></param>
		/// <param name="addIfCondition"></param>
		/// <returns></returns>
		public static string Add(this string str, string value, bool addIfCondition) {
			if(!string.IsNullOrEmpty(str) && addIfCondition) {
				str += value;
			}
			return str;
		}

		/// <summary>
		/// Add string value in specific index if value is not null and empty
		/// </summary>
		/// <param name="str"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public static string Add(this string str, int index, string value) {
			if(!string.IsNullOrEmpty(str) && value != null) {
				if(index >= str.Length) {
					return str + value;
				}
				str = str.Insert(index, value);
			}
			return str;
		}

		/// <summary>
		/// Add string value on first if value is not null and empty
		/// </summary>
		/// <param name="str"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public static string AddFirst(this string str, string value) {
			if(!string.IsNullOrEmpty(str)) {
				str = value + str;
			}
			return str;
		}

		/// <summary>
		/// Add a new line in first.
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		public static string AddLineInFirst(this string str) {
			return str.AddFirst("\n");
		}

		/// <summary>
		/// Add a new line in end
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		public static string AddLineInEnd(this string str) {
			return str.Add("\n");
		}

		/// <summary>
		/// Add a new statement in the end.
		/// </summary>
		/// <param name="str"></param>
		/// <param name="contents"></param>
		/// <returns></returns>
		public static string AddStatement(this string str, string contents) {
			if(string.IsNullOrEmpty(contents)) {
				return str;
			}
			return str.Add("\n" + contents);
		}

		/// <summary>
		/// Add string value on first if value and targetValue is not null and empty
		/// </summary>
		/// <param name="str"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public static string AddFirst(this string str, string value, string targetValue, bool inverted = false) {
			if(inverted) {
				if(!string.IsNullOrEmpty(str) && string.IsNullOrEmpty(targetValue)) {
					str = value + str;
				}
			} else {
				if(!string.IsNullOrEmpty(str) && !string.IsNullOrEmpty(targetValue)) {
					str = value + str;
				}
			}
			return str;
		}

		/// <summary>
		/// Add string value on first if value is not null and addIfCondition is true
		/// </summary>
		/// <param name="str"></param>
		/// <param name="value"></param>
		/// <param name="addIfCondition"></param>
		/// <returns></returns>
		public static string AddFirst(this string str, string value, bool addIfCondition) {
			if(!string.IsNullOrEmpty(str) && addIfCondition) {
				str = value + str;
			}
			return str;
		}

		/// <summary>
		/// Remove last string if value is not null and empty
		/// </summary>
		/// <param name="str"></param>
		/// <param name="count"></param>
		/// <returns></returns>
		public static string RemoveLast(this string str, int count = 1) {
			if(!string.IsNullOrEmpty(str)) {
				str = str.Remove(str.Length - count, count);
			}
			return str;
		}

		/// <summary>
		/// Add semicolon if value is not null and empty
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		public static string AddSemicolon(this string str) {
			if(!string.IsNullOrEmpty(str)) {
				str = str + ";";
			}
			return str;
		}

		/// <summary>
		/// Remove semicolon if value is not null and empty
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		public static string RemoveSemicolon(this string str) {
			if(!string.IsNullOrEmpty(str)) {
				str = str.Replace(";", "");
			}
			return str;
		}

		/// <summary>
		/// Remove line and tab before the first character
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		public static string RemoveLineAndTabOnFirst(this string str) {
			if(!string.IsNullOrEmpty(str)) {
				string value = "";
				bool flag = false;
				for(int i = 0; i < str.Length; i++) {
					if(str[i] != '\n' && str[i] != '\t') {
						flag = true;
					}
					if(flag) {
						value += str[i];
					}
				}
				return value;
			}
			return str;
		}
		#endregion

		#region Types
		/// <summary>
		/// Converts a serialized type name into a pretty name format.
		/// </summary>
		/// <param name="typeName">The serialized type name to be converted.</param>
		/// <param name="fullName">A boolean value indicating whether to include the full namespace in the resulting type name. <see
		/// langword="true"/> to include the full namespace; otherwise, <see langword="false"/>.</param>
		/// <returns>A pretty name representation of the type name. If the type cannot be deserialized, the original <paramref
		/// name="typeName"/> is returned.</returns>
		public static string PrettyName(this string typeName, bool fullName = false) {
			// Deserialize the type name to get the actual Type object
			Type t = TypeSerializer.Deserialize(typeName, false);
			if(t != null) {
				// If the type is successfully deserialized, generate its pretty name
				return t.PrettyName(fullName);
			}
			// If deserialization fails, return the original type name
			return typeName;
		}

		/// <summary>
		/// Generates a pretty name for the specified <see cref="Type"/>.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="fullName"></param>
		/// <param name="info"></param>
		/// <returns></returns>
		public static string PrettyName(this Type type, bool fullName, ParameterInfo info) {
			if(info != null) {
				// Handle by ref types
				if(info.ParameterType.IsByRef) {
					if(info.IsOut) {
						// Use 'out' modifier for 'out' parameters
						return CSharpTypeName(type, fullName, RefKind.Out);
					}
					else if(info.IsIn) {
						// Use 'in' modifier for 'in' parameters
						return CSharpTypeName(type, fullName, RefKind.In);
					}
					else {
						// Use 'ref' modifier for 'ref' parameters
						return CSharpTypeName(type, fullName, RefKind.Ref);
					}
				}
			}
			return CSharpTypeName(type, fullName);
		}

		/// <summary>
		/// Generates a pretty name for the specified <see cref="Type"/>.
		/// </summary>
		/// <param name="type">The <see cref="Type"/> to generate the name for. Cannot be <see langword="null"/>.</param>
		/// <param name="fullName">A value indicating whether to include the namespace in the generated name. <see langword="true"/> to include the
		/// namespace; otherwise, <see langword="false"/>.</param>
		/// <param name="refKind">Specifies the kind of reference modifier to include in the name, such as <see cref="RefKind.Ref"/> or <see
		/// cref="RefKind.Out"/>. Defaults to <see cref="RefKind.None"/>.</param>
		/// <returns>A string representing the pretty name of the <paramref name="type"/>, optionally including the namespace
		/// and reference modifier based on the provided parameters.</returns>
		public static string PrettyName(this Type type, bool fullName = false, RefKind refKind = RefKind.None) {
			return CSharpTypeName(type, fullName, refKind);
		}

		private static string CSharpTypeName(Type type, bool fullName = false, RefKind refKind = RefKind.None) {
			if(type == null)
				return "null";
			// Handle by ref types
			if(type.IsByRef) {
				switch(refKind) {
					case RefKind.None:
						// Default to 'ref' if no specific ref kind is provided
						return $"&{DoCSharpTypeName(type.GetElementType(), fullName)}";
					case RefKind.In:
						// Use 'in' modifier for 'in' parameters
						return $"in {DoCSharpTypeName(type.GetElementType(), fullName)}";
					case RefKind.Ref:
						// Use 'ref' modifier for 'ref' parameters
						return $"ref {DoCSharpTypeName(type.GetElementType(), fullName)}";
					case RefKind.Out:
						// Use 'out' modifier for 'out' parameters
						return $"out {DoCSharpTypeName(type.GetElementType(), fullName)}";
				}
			}
			return DoCSharpTypeName(type, fullName);
		}

		private static string DoCSharpTypeName(Type type, bool fullName = false) {
			if(type == null)
				return "null";
			// Handle runtime type
			if(type is RuntimeType) {
				// If the type is a RuntimeType, return its name directly
				return type.Name;
			}
			// Handle generic types, and nullable types
			if(type.IsGenericType) {
				// Handle nullable types
				if(type.GetGenericTypeDefinition() == typeof(Nullable<>)) {
					// Get the underlying type of the nullable type and append a '?' to indicate nullability
					return string.Format("{0}?", DoCSharpTypeName(Nullable.GetUnderlyingType(type), fullName));
				}
				else if(!type.ContainsGenericParameters) {
					var nm = fullName ? type.FullName : type.Name;
					// Find the index of the '`' character which indicates the start of generic parameters in the type name
					int idx = nm.IndexOf('`');
					// Recursively get the pretty names of the generic arguments and format them into the type name
					return string.Format("{0}<{1}>", idx >= 0 ? nm.Remove(idx) : nm,
						string.Join(", ", type.GetGenericArguments().Select(a => DoCSharpTypeName(a, fullName))));
				}
			}
			// Handle array types
			if(type.IsArray) {
				// Recursively get the pretty name of the element type and append '[]' to indicate an array
				return string.Format("{0}[]", DoCSharpTypeName(type.GetElementType(), fullName));
			}

			return shorthandMap.ContainsKey(type) ? shorthandMap[type] : fullName ? type.FullName : type.Name;
		}
		#endregion
		
		/// <summary>
		/// Determines whether the specified <see cref="PortAccessibility"/> value allows data to be retrieved.
		/// </summary>
		/// <param name="accessibility">The <see cref="PortAccessibility"/> value to evaluate.</param>
		/// <returns><see langword="true"/> if the <paramref name="accessibility"/> is <see cref="PortAccessibility.ReadWrite"/>  or
		/// <see cref="PortAccessibility.ReadOnly"/>; otherwise, <see langword="false"/>.</returns>
		public static bool CanGet(this PortAccessibility accessibility) {
			return accessibility == PortAccessibility.ReadWrite || accessibility == PortAccessibility.ReadOnly;
		}

		/// <summary>
		/// Determines whether the specified <see cref="PortAccessibility"/> value allows writing to the port.
		/// </summary>
		/// <param name="accessibility">The <see cref="PortAccessibility"/> value to evaluate.</param>
		/// <returns><see langword="true"/> if the port accessibility is <see cref="PortAccessibility.ReadWrite"/> or  <see
		/// cref="PortAccessibility.WriteOnly"/>; otherwise, <see langword="false"/>.</returns>
		public static bool CanSet(this PortAccessibility accessibility) {
			return accessibility == PortAccessibility.ReadWrite || accessibility == PortAccessibility.WriteOnly;
		}

		#region Utility
		/// <summary>
		/// Is the 'type' is implement an interface 'interfaceType'
		/// </summary>
		/// <param name="type"></param>
		/// <param name="interfaceType"></param>
		/// <returns></returns>
		public static bool HasImplementInterface(this Type type, Type interfaceType) {
			if(type == null || interfaceType == null) return false;
			if(type.IsGenericType && interfaceType.IsGenericTypeDefinition) {
				var definition = type.GetGenericTypeDefinition();
				if(definition == interfaceType) {
					return true;
				}
			}
			while(type != null) {
				bool generic = interfaceType.IsGenericTypeDefinition;
				Type[] interfaces = type.GetInterfaces();
				if(interfaces != null) {
					for(int i = 0; i < interfaces.Length; i++) {
						if(generic) {
							var iface = interfaces[i];
							if(iface.IsConstructedGenericType) {
								iface = iface.GetGenericTypeDefinition();
							}
							if(iface == interfaceType || (iface != null && HasImplementInterface(iface, interfaceType))) {
								return true;
							}
						}
						else {
							if(interfaces[i] == interfaceType || (interfaces[i] != null && HasImplementInterface(interfaces[i], interfaceType))) {
								return true;
							}
						}
					}
				}
				type = type.BaseType;
			}
			return false;
		}

		/// <summary>
		/// Is the type has implemented raw generic of <paramref name="generic"/>. Only work for class type, not work for interface.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="generic"></param>
		/// <returns></returns>
		public static bool IsSubclassOfRawGeneric(this Type type, Type generic) {
			while(type != null && type != typeof(object)) {
				var cur = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
				if(generic == cur) {
					return true;
				}
				type = type.BaseType;
			}
			return false;
		}

		/// <summary>
		/// Determines whether an instance of the current Type can be assigned to an instance of the specified Type.
		/// </summary>
		/// <param name="from"></param>
		/// <param name="to"></param>
		/// <returns></returns>
		public static bool IsCastableTo(this Type from, Type to) {
			if(from == null || to == null) return false;//Force false on from or to type is null.
			if(from.IsByRef) {
				from = from.GetElementType();
			}
			if(to.IsByRef) {
				to = to.GetElementType();
			}
			Dictionary<Type, bool> map = null;
			lock(_lockObject) {
				if(castableMap.TryGetValue(from, out map)) {
					bool val;
					if(map.TryGetValue(to, out val)) {
						return val;
					}
				}
			}

			if(from is RuntimeType) {
				if(ReflectionUtils.IsTypeEqual(from, to)) return true;
				if(to == typeof(object)) return true;
				if(ReflectionUtils.IsNativeType(from)) {
					var nt = ReflectionUtils.GetNativeType(from);
					if(nt != null) {
						return nt.IsCastableTo(to);
					}
				}
				if(to.IsInterface) {
					return from.HasImplementInterface(to);
				}
				else if(from.IsInterface) {
					return to.HasImplementInterface(from);
				}
				else if(to is RuntimeType) {
					return to.IsAssignableFrom(from);
				}
				if(from.IsSubclassOf(to)) {
					return true;
				}
				if(from is RuntimeGraphType runtimeGraphType) {
					if(runtimeGraphType.target is IClassDefinition classDefinition) {
						var model = classDefinition.GetModel();
						return model.ProxyScriptType.IsCastableTo(to);
					}
				}
				var baseType = from.BaseType;
				while(baseType is RuntimeType) {
					baseType = baseType.BaseType;
				}
				return baseType.IsCastableTo(to);
			}
			else if(to is RuntimeType) {
				return to.IsAssignableFrom(from);
			}

			if(to.IsAssignableFrom(from)) {
				lock(_lockObject) {
					if(map == null) {
						map = new Dictionary<Type, bool>();
					}
					map[to] = true;
					castableMap[from] = map;
				}
				return true;
			}
			if(from.IsPrimitive && to.IsPrimitive) {
				IEnumerable<Type> lowerTypes = Enumerable.Empty<Type>();
				foreach(Type[] types in typeHierarchy) {
					if(types.Any(t => t == to)) {
						bool r = lowerTypes.Any(t => t == from);
						lock(_lockObject) {
							if(map == null) {
								map = new Dictionary<Type, bool>();
							}
							map[to] = r;
							castableMap[from] = map;
						}
						return r;
					}
					lowerTypes = lowerTypes.Concat(types);
				}
				lock(_lockObject) {
					if(map == null) {
						map = new Dictionary<Type, bool>();
					}
					map[to] = false;
					castableMap[from] = map;
				}
				return false; // IntPtr, UIntPtr, Enum, Boolean
			}
			if(from.IsSubclassOf(typeof(Delegate)) && to.IsSubclassOf(typeof(Delegate))) {
				var a = from.GetMethod("Invoke");
				var b = to.GetMethod("Invoke");

				if(a == null || b == null) return false;
				
				if(a.ReturnType != b.ReturnType) {
					lock(_lockObject) {
						if(map == null) {
							map = new Dictionary<Type, bool>();
						}
						map[to] = false;
						castableMap[from] = map;
					}
					return false;
				}
				var aParams = a.GetParameters();
				var bParams = b.GetParameters();
				if(aParams.Length != bParams.Length) {
					lock(_lockObject) {
						if(map == null) {
							map = new Dictionary<Type, bool>();
						}
						map[to] = false;
						castableMap[from] = map;
					}
					return false;
				}
				for(int i = 0; i < aParams.Length; i++) {
					if(aParams[i].ParameterType != bParams[i].ParameterType) {
						lock(_lockObject) {
							if(map == null) {
								map = new Dictionary<Type, bool>();
							}
							map[to] = false;
							castableMap[from] = map;
						}
						return false;
					}
				}
				lock(_lockObject) {
					if(map == null) {
						map = new Dictionary<Type, bool>();
					}
					map[to] = true;
					castableMap[from] = map;
				}
				return true;
			}
			bool result = from.FindImplicitOperator(to).Any();
			lock(_lockObject) {
				if(map == null) {
					map = new Dictionary<Type, bool>();
				}
				map[to] = result;
				castableMap[from] = map;
			}
			return result;
		}

		/// <summary>
		/// Get the implicit operators
		/// </summary>
		/// <param name="from"></param>
		/// <param name="to"></param>
		/// <returns></returns>
		public static IEnumerable<MethodInfo> FindImplicitOperator(this Type from, Type to) {
			var methods = from.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy).Where(m =>
				m.ReturnType == to &&
				(m.Name == "op_Implicit")
			);
			if(methods.Any()) {
				return methods;
			}
			return to.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy).Where(m =>
				m.ReturnType == to &&
				(m.Name == "op_Implicit") &&
				ReflectionUtils.IsValidMethodParameter(m, from)
			);
		}

		/// <summary>
		/// Determines whether an instance of the current Type can be assigned to an instance of the specified Type.
		/// </summary>
		/// <param name="from"></param>
		/// <param name="to"></param>
		/// <returns></returns>
		public static bool IsCastableTo(this Type from, Type to, bool Explicit) {
			if(!Explicit)
				return from.IsCastableTo(to);
			if(from == null || to == null) return false;//Force false on from or to type is null.
			Dictionary<Type, bool> map = null;
			lock(_lockObject) {
				if(castableMap2.TryGetValue(from, out map)) {
					bool val;
					if(map.TryGetValue(to, out val)) {
						return val;
					}
				}
			}

			if(from is RuntimeType) {
				if(ReflectionUtils.IsTypeEqual(from, to)) return true;
				if(to == typeof(object)) return true;
				if(ReflectionUtils.IsNativeType(from)) {
					var nt = ReflectionUtils.GetNativeType(from);
					if(nt != null) {
						return nt.IsCastableTo(to);
					}
				}
				if(to.IsInterface) {
					return from.HasImplementInterface(to);
				}
				else if(from.IsInterface) {
					return to.HasImplementInterface(from);
				}
				else if(to is RuntimeType) {
					return to.IsAssignableFrom(from);
				}
				if(from.IsSubclassOf(to) || to.IsSubclassOf(from)) {
					return true;
				}
				if(from is RuntimeGraphType runtimeGraphType) {
					if(runtimeGraphType.target is IClassDefinition classDefinition) {
						var model = classDefinition.GetModel();
						return model.ProxyScriptType.IsCastableTo(to, true);
					}
				}
				var baseType = from.BaseType;
				while(baseType is RuntimeType) {
					baseType = baseType.BaseType;
				}
				return baseType.IsCastableTo(to, true);
			}
			else if(to is RuntimeType) {
				return to.IsAssignableFrom(from) || from.IsSubclassOf(to) || to.IsSubclassOf(from);
			}


			if(from.IsEnum && to.IsPrimitive) {
				return to != typeof(bool);
			}
			if(to.IsAssignableFrom(from)) {
				lock(_lockObject) {
					if(map == null) {
						map = new Dictionary<Type, bool>();
					}
					map[to] = true;
					castableMap2[from] = map;
				}
				return true;
			}
			if((from.IsPrimitive) && (to.IsPrimitive)) {
				//bool flag1 = false;
				//bool flag2 = false;
				//foreach(Type[] types in typeHierarchy) {
				//	if(types.Contains(from)) {
				//		flag1 = true;
				//	}
				//	if(types.Contains(to)) {
				//		flag2 = true;
				//	}
				//}
				lock(_lockObject) {
					if(map == null) {
						map = new Dictionary<Type, bool>();
					}
					map[to] = from != typeof(bool) && to != typeof(bool);
					castableMap2[from] = map;
				}
				return false; // IntPtr, UIntPtr, Enum, Boolean
			}
			if(from.IsInterface && to.IsSealed == false) {
				lock(_lockObject) {
					if(map == null) {
						map = new Dictionary<Type, bool>();
					}
					map[to] = true;
					castableMap2[from] = map;
				}
				return true;
			}
			if(from.IsSubclassOf(typeof(Delegate)) && to.IsSubclassOf(typeof(Delegate))) {
				var a = from.GetMethod("Invoke");
				var b = to.GetMethod("Invoke");
				if(a.ReturnType != b.ReturnType) {
					lock(_lockObject) {
						if(map == null) {
							map = new Dictionary<Type, bool>();
						}
						map[to] = false;
						castableMap2[from] = map;
					}
					return false;
				}
				var aParams = a.GetParameters();
				var bParams = b.GetParameters();
				if(aParams.Length != bParams.Length) {
					lock(_lockObject) {
						if(map == null) {
							map = new Dictionary<Type, bool>();
						}
						map[to] = false;
						castableMap2[from] = map;
					}
					return false;
				}
				for(int i = 0; i < aParams.Length; i++) {
					if(aParams[i] != bParams[i]) {
						lock(_lockObject) {
							if(map == null) {
								map = new Dictionary<Type, bool>();
							}
							map[to] = false;
							castableMap2[from] = map;
						}
						return false;
					}
				}
				lock(_lockObject) {
					if(map == null) {
						map = new Dictionary<Type, bool>();
					}
					map[to] = true;
					castableMap2[from] = map;
				}
				return true;
			}
			var methods = from.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy).Where(m => m.ReturnType == to && (m.Name == "op_Implicit" || Explicit && m.Name == "op_Explicit"));
			bool result = methods.Any();
			lock(_lockObject) {
				if(map == null) {
					map = new Dictionary<Type, bool>();
				}
				map[to] = result;
				castableMap2[from] = map;
			}
			return result;
		}

		/// <summary>
		/// Get the element type of a type if available.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static Type ElementType(this Type type) {
			if(type == null)
				return null;
			if(type.HasElementType) {
				return type.GetElementType();
			}
			else if(type.HasImplementInterface(typeof(IEnumerable<>))) {
				return type.GetArgumentsOfInheritedOpenGenericInterface(typeof(IEnumerable<>))[0];
            }
            else if(type.HasImplementInterface(typeof(IEnumerator<>))) {
                return type.GetArgumentsOfInheritedOpenGenericInterface(typeof(IEnumerator<>))[0];
            }
            else if(type.HasImplementInterface(typeof(IEnumerable))) {
				return typeof(object);
			}
			else if(type.HasImplementInterface(typeof(IEnumerator))) {
				return typeof(object);
			}
			else if(type == typeof(IEnumerable) || type == typeof(IEnumerator)) {
				return typeof(object);
			}
			return null;
		}

		/// <summary>
		/// Adds the elements of the specified collection to the target collection.
		/// </summary>
		/// <remarks>This method iterates over the <paramref name="values"/> collection and adds each element to the
		/// <paramref name="collections"/> collection. If <paramref name="values"/> is <see langword="null"/>, the method
		/// performs no operation.</remarks>
		/// <typeparam name="T">The type of elements in the collections.</typeparam>
		/// <param name="collections">The target collection to which the elements will be added. Cannot be <see langword="null"/>.</param>
		/// <param name="values">The collection of elements to add. If <see langword="null"/>, no elements are added.</param>
		public static void AddRange<T>(this ICollection<T> collections, IEnumerable<T> values) {
			if(values != null) {
				foreach(var value in values) {
					collections.Add(value);
				}
			}
		}

		/// <summary>
		/// Converts the elements of the specified <see cref="IEnumerable{T}"/> to a <see cref="HashSet{T}"/>.
		/// </summary>
		/// <typeparam name="T">The type of elements in the source collection.</typeparam>
		/// <param name="collections">The source collection to convert to a <see cref="HashSet{T}"/>.</param>
		/// <returns>A <see cref="HashSet{T}"/> containing the elements of the source collection.</returns>
		public static HashSet<T> ToHashSet<T>(this IEnumerable<T> collections) {
			return new HashSet<T>(collections);
		}
		#endregion
	}
}