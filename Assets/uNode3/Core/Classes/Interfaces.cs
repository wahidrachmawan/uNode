using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using System;
using System.Collections;
using System.Linq;

namespace MaxyGames.UNode {
	public interface INamespace {
		string Namespace { get; }
	}

	public interface IUsingNamespace {
		List<string> UsingNamespaces { get; }
	}

	public interface INamespaceSystem : INamespace, IUsingNamespace { }

	public interface ISetableNamespace : INamespace {
		new string Namespace { get; set; }
	}

	public interface ISetableUsingNamespace : IUsingNamespace {
		new List<string> UsingNamespaces { get; set; }
	}

	/// <summary>
	/// A graph container that has <see cref="Graph"/> data.
	/// </summary>
	public interface IGraph {
		Graph GraphData { get; }
	}

	public interface ITypeWithScriptData {
		/// <summary>
		/// The generated script data (must not be null).
		/// </summary>
		GeneratedScriptData ScriptData { get; }
	}

	/// <summary>
	/// A graph container with script data
	/// </summary>
	public interface IGraphWithScriptData : IGraph, ITypeWithScriptData { }

	public interface ITypeGraph : IGraphWithScriptData {
		/// <summary>
		/// The graph name, by default this should be same with the DisplayName.
		/// This also used for the class name for generating script so this should be unique without spaces or symbol.
		/// </summary>
		string GraphName { get; }
		/// <summary>
		/// The graph namespaces.
		/// </summary>
		string Namespace { get; }
		/// <summary>
		/// The full graph name including the namespaces
		/// </summary>
		string FullGraphName {
			get {
				string ns = Namespace;
				if(!string.IsNullOrEmpty(ns)) {
					return ns + "." + GraphName;
				}
				else {
					return GraphName;
				}
			}
		}
		/// <summary>
		/// Return the Full Type name of the generated script, empty if it never generated into script.
		/// </summary>
		string GeneratedTypeName => FullGraphName;
	}

	public interface IClassDefinition : IClassGraph {
		ClassDefinitionModel GetModel();
	}

	public interface IClassGraph : ITypeGraph {
		/// <summary>
		/// True if the graph is a struct
		/// </summary>
		bool IsStruct => InheritType == typeof(ValueType);
		/// <summary>
		/// True if the graph is interface
		/// </summary>
		bool IsInterface => InheritType == null;
		/// <summary>
		/// The inherit type of the graph.
		/// </summary>
		Type InheritType { get; }
	}

	public interface IReference {
		object ReferenceValue { get; }
	}

	/// <summary>
	/// Implement in node to handle childs event nodes
	/// </summary>
	public interface INodeWithEventHandler {
		bool CanTrigger(GraphInstance instance);
		string GenerateTriggerCode(string contents) => contents;
	}

	/// <summary>
	/// The object with custom icon
	/// </summary>
	public interface ICustomIcon {
		Texture GetIcon();
	}

	/// <summary>
	/// Interface for custom icon for nodes
	/// </summary>
	public interface IIcon {
		System.Type GetIcon();
	}

	public interface IGroup { }

	public interface IScriptGraph : INamespaceSystem, ISetableNamespace, ISetableUsingNamespace {
		/// <summary>
		/// The list of types
		/// </summary>
		ScriptTypeList TypeList { get; }
		/// <summary>
		/// The generated script data (must not be null).
		/// </summary>
		GeneratedScriptData ScriptData { get; }
	}

	/// <summary>
	/// Implement this interface if the graph is intended only for generating c# script
	/// </summary>
	public interface IScriptGraphType : ITypeWithScriptData {
		/// <summary>
		/// The script name without the namespace
		/// </summary>
		string ScriptName { get; }
		/// <summary>
		/// Get the lastly generated type name
		/// </summary>
		string TypeName => ScriptData.typeName;
		/// <summary>
		/// The script type data
		/// </summary>
		ScriptTypeData ScriptTypeData { get; }
	}

	/// <summary>
	/// A base member for Runtime Type, Field, Property, Parameter and Method
	/// </summary>
	public interface IRuntimeMember {

	}

	/// <summary>
	/// A interface to mark Runtime Type as runtime member so it is handled differently to native runtime member.
	/// </summary>
	public interface IRuntimeType {
		Type GetNativeType();
	}

	/// <summary>
	/// A interface to mark Runtime Type to native member so it handled differently to regular runtime member.
	/// </summary>
	public interface INativeMember {
		/// <summary>
		/// Get the native member of Runtime Type.
		/// </summary>
		/// <returns></returns>
		public MemberInfo GetNativeMember();
	}

	public interface INativeMethod : INativeMember {
		MemberInfo INativeMember.GetNativeMember() {
			return GetNativeMethod();
		}

		public MethodInfo GetNativeMethod();
	}

	public interface INativeField : INativeMember {
		MemberInfo INativeMember.GetNativeMember() {
			return GetNativeField();
		}

		public FieldInfo GetNativeField();
	}

	public interface INativeProperty : INativeMember {
		MemberInfo INativeMember.GetNativeMember() {
			return GetNativeProperty();
		}

		public PropertyInfo GetNativeProperty();
	}

	public interface INativeType : INativeMember {
		MemberInfo INativeMember.GetNativeMember() {
			return GetNativeType();
		}

		public Type GetNativeType();
	}

	/// <summary>
	/// A runtime member with a reference.
	/// </summary>
	public interface IRuntimeMemberWithRef : IRuntimeMember {
		BaseReference GetReference();

		object GetReferenceValue() => GetReference()?.ReferenceValue;
	}

	/// <summary>
	/// A interface for all Fake Type, Field, Property, Parameter and Method
	/// </summary>
	public interface IFakeMember {
		/// <summary>
		/// The original member
		/// </summary>
		object Original { get; }
		/// <summary>
		/// True if the member is a native member
		/// </summary>
		bool IsNativeMember { get; }
	}

	public interface IGenericMethodWithResolver {
		GenericMethodResolver GetResolver();
	}

	public interface IFakeType : IFakeMember {
		Type GetNativeType();
	}

	public interface IReflectionType {
		RuntimeType ReflectionType { get; }
	}

	/// <summary>
	/// A independent graph that have its own namespace and using namespace data
	/// </summary>
	public interface IIndependentGraph : INamespaceSystem { }

	/// <summary>
	/// A runtime graph that can be referenced without using instance like static class in C#
	/// </summary>
	public interface ISingletonGraph : IRuntimeComponent {
		bool IsPersistence { get; }
		IRuntimeClass Instance { get; }
	}

	public interface IInstancedGraph {
		IGraph OriginalGraph { get; }
		GraphInstance Instance { get; }
		Type GraphType {
			get {
				return (OriginalGraph as IReflectionType)?.ReflectionType;
			}
		}
	}

	public interface IRuntimeGraphWrapper : IRuntimeClass, IRuntimeInterface, IRuntimeClassContainer, IInstancedGraph {
		List<VariableData> WrappedVariables { get; }
	}

	public interface INodeRoot {
		System.Type GetInheritType();
	}

	public interface IValue : IGetValue, ISetValue {

	}

	internal interface INodeAsEdge { }

	public interface IGraphValue {
		object Get(Flow flow);
		void Set(Flow flow, object value);
	}

	public interface IObjectReference {
		object ReferenceValue { get; }
	}

	internal interface IValueReference { }

	internal interface IGraphElement { }

	internal interface IDebugableDisplay {
		internal string DebugDisplay { get; }
	}

	public interface IGetValue {
		object Get();
	}

	public interface ISetValue {
		void Set(object value);
	}

	public interface IParameterSystem {
		IList<ParameterData> Parameters { get; }
	}

	public interface IGenericParameterSystem {
		IList<GenericParameterData> GenericParameters { get; set; }
		GenericParameterData GetGenericParameter(string name);
	}

	public interface IAttributeSystem {
		List<AttributeData> Attributes { get;}
	}

	public interface IGraphWithProperties : IGraph { }

	public interface IGraphWithVariables : IGraph { }

	public interface ILocalVariableSystem {
		IEnumerable<Variable> LocalVariables { get; }
	}

	public interface IGraphWithFunctions : IGraph { }

	public interface IGraphWithConstructors : IGraph { }

	public interface IGraphWithAttributes : IGraph, IAttributeSystem {
		List<AttributeData> IAttributeSystem.Attributes {
			get {
				var graph = GraphData;
				if(graph != null) {
					if(graph.attributes == null) {
						graph.attributes = new List<AttributeData>();
					}
					return graph.attributes;
				}
				return null;
			}
		}
	}

	public interface IVariable : IGraphValue {
		System.Type type { get; }
	}

	/// <summary>
	/// Interface for in editor error checking.
	/// </summary>
	public interface IErrorCheck {
		void CheckError(ErrorAnalyzer analizer);
	}

	/// <summary>
	/// Interface for display nicely names
	/// </summary>
	public interface IPrettyName {
		string GetPrettyName();
	}

	/// <summary>
	/// Interface for display rich nicely names
	/// </summary>
	public interface IRichName {
		string GetRichName();
	}

	/// <summary>
	/// Interface for display human friendly name
	/// </summary>
	interface IFriendlyName {
		string GetFriendlyName();
	}

	public interface ICustomMainGraph {
		string MainGraphTitle => "Event Graph";
		/// <summary>
		/// The main graph scope, separated by comma
		/// </summary>
		string MainGraphScope => NodeScope.All;
		bool AllowCoroutine => false;
		/// <summary>
		/// If true, user are allowed to create a node, etc.
		/// </summary>
		bool CanCreateOnMainGraph => true;
	}

	public interface IGraphWithEventGraph {
		/// <summary>
		/// The list of supported event graph type
		/// </summary>
		HashSet<string> SupportedEventGraphs { get; }
	}

	public interface IEventGraphCanvas {
		string Title { get; }
		string Scope => NodeScope.FlowGraph;
		bool AllowCoroutine => false;
	}

	/// <summary>
	/// An interface to implement Macro Graph
	/// </summary>
	public interface IMacroGraph : IIndependentGraph {
		bool HasCoroutineNode { get; }

		IEnumerable<Nodes.MacroPortNode> InputFlows { get; }
		IEnumerable<Nodes.MacroPortNode> InputValues { get; }
		IEnumerable<Nodes.MacroPortNode> OutputFlows { get; }
		IEnumerable<Nodes.MacroPortNode> OutputValues { get; }
	}

	public interface ILinkedMacro : IMacro {
		IMacroGraph LinkedMacro { get; }
	}

	/// <summary>
	/// An interface to implement State Graph
	/// </summary>
	public interface IStateGraph {
		/// <summary>
		/// True when the graph can create state graph
		/// </summary>
		bool CanCreateStateGraph { get; }
	}

	/// <summary>
	/// An interface for an object that's refresable ( editor only )
	/// </summary>
	public interface IRefreshable {
		void Refresh();
	}

	/// <summary>
	/// Interface to implement interface system for a class or struct graph
	/// </summary>
	public interface IInterfaceSystem {
		List<SerializedType> Interfaces { get; }
	}

	/// <summary>
	/// Interface to implement nested class graph
	/// </summary>
	public interface IGraphWithNestedTypes {
		//uNodeData NestedClass { get; set; }
	}

	/// <summary>
	/// Interface to implement class system graph
	/// </summary>
	public interface IClassSystem : IGraphWithAttributes, IGraphWithConstructors, IGraphWithVariables, IGraphWithProperties, IGraphWithFunctions {

	}


	public interface IEventGenerator {
		void GenerateEventCode();
	}

	public interface IGeneratorPrePostInitializer {
		/// <summary>
		/// Called before code generator is initialized
		/// </summary>
		void OnPreInitializer() { }
		/// <summary>
		/// Called after code generator is initialized
		/// </summary>
		void OnPostInitializer() { }
	}

	/// <summary>
	/// An interface for Macro node
	/// </summary>
	public interface IMacro {
		IEnumerable<Nodes.MacroPortNode> InputFlows { get; }
		IEnumerable<Nodes.MacroPortNode> InputValues { get; }
		IEnumerable<Nodes.MacroPortNode> OutputFlows { get; }
		IEnumerable<Nodes.MacroPortNode> OutputValues { get; }
	}

	/// <summary>
	/// An interface for SuperNode / Group Node
	/// </summary>
	public interface ISuperNode : INodeWithConnection {
		/// <summary>
		/// The nested flow node, usually this is a entry points
		/// </summary>
		IEnumerable<NodeObject> NestedFlowNodes { get; }
		/// <summary>
		/// The supported scope, multiple scope is supported separated by `,` or `|`.
		/// </summary>
		string SupportedScope => NodeScope.FlowGraph;
		/// <summary>
		/// Are coroutine is supported inside this node?
		/// </summary>
		/// <returns></returns>
		bool AllowCoroutine();
		IEnumerable<NodeObject> INodeWithConnection.Connections => NestedFlowNodes;
	}

	public interface IElementWithEntry {
		public BaseEntryNode Entry { get; }
		public void RegisterEntry(BaseEntryNode node) { }
	}

	public interface ISuperNodeWithEntry : ISuperNode, IElementWithEntry {
		IEnumerable<NodeObject> INodeWithConnection.Connections => NestedFlowNodes.Append(Entry);
	}

	public interface INodeWithConnection {
		IEnumerable<NodeObject> Connections { get; }
	}

	public interface IStackedNode : INodeWithConnection {
		IEnumerable<NodeObject> StackedNodes { get; }
		IEnumerable<NodeObject> INodeWithConnection.Connections => StackedNodes;
	}

	public interface IRerouteNode {
		UPort Input { get; }

		UPort Output { get; }
	}

	/// <summary>
	/// Interface to implement runtime function which can be Invoked by its unique name and parameters
	/// </summary>
	public interface IRuntimeFunction {
		object InvokeFunction(string name, object[] values);
		object InvokeFunction(string name, System.Type[] parameters, object[] values);
	}

	/// <summary>
	/// Interface to implement runtime variable which can be Set and Get a variable value by its unique name
	/// </summary>
	public interface IRuntimeVariable {
		void SetVariable(string name, object value);
		object GetVariable(string name);
	}

	/// <summary>
	/// Interface to implement runtime property which can be Set and Get a property value by its unique name
	/// </summary>
	public interface IRuntimeProperty {
		void SetProperty(string name, object value);
		object GetProperty(string name);
	}

	/// <summary>
	/// A runtime graph that is inherith from IRuntimeClass and with additional functions
	/// </summary>
	public interface IRuntimeGraph : IRuntimeClass {
		void ExecuteFunction(string name);
	}

	/// <summary>
	/// A runtime class graph that has function, variable, and property
	/// </summary>
	public interface IRuntimeClass : IRuntimeFunction, IRuntimeVariable, IRuntimeProperty, IClassIdentifier {
		void SetVariable(string name, object value, char @operator);
		void SetProperty(string name, object value, char @operator);
	}

	public interface IRuntimeInterface {
		IEnumerable<Type> GetInterfaces();
	}

	/// <summary>
	/// Used only for MonoBehaviour sub classes that's identified as instance of Class Component
	/// </summary>
	public interface IRuntimeComponent : IRuntimeClass, IRuntimeInterface { }

	/// <summary>
	/// Used only for ScriptableObject sub classes that's identified as instance of Class Asset
	/// </summary>
	public interface IRuntimeAsset : IRuntimeClass, IRuntimeInterface { }
	
	public interface IRuntimeClassContainer {
		IRuntimeClass RuntimeClass { get; }
		bool IsInitialized { get; }
		void ResetInitialization();
	}

	/// <summary>
	/// Use for identify graph that's supported to reference using RuntimeType
	/// </summary>
	public interface IClassIdentifier {
		string uniqueIdentifier { get; }
	}
	
	/// <summary>
	/// Interface for implementing class modifier
	/// </summary>
	public interface IVariableModifier {
		FieldModifier GetModifier();
	}

	/// <summary>
	/// Interface for implementing class modifier
	/// </summary>
	public interface IPropertyModifier {
		PropertyModifier GetModifier();
	}

	/// <summary>
	/// Interface for implementing class modifier
	/// </summary>
	public interface IClassModifier {
		ClassModifier GetModifier();
	}

	/// <summary>
	/// Interface for implementing class modifier
	/// </summary>
	public interface IInterfaceModifier {
		InterfaceModifier GetModifier();
	}

	public interface IScriptInterface : IInterfaceModifier, ITypeWithScriptData { }

	/// <summary>
	/// Interface for describing the instance
	/// </summary>
	public interface ISummary {
		/// <summary>
		/// Get the summary
		/// </summary>
		/// <returns></returns>
		string GetSummary();
	}

	/// <summary>
	/// An interface for implementing flow node that has one input and output
	/// </summary>
	public interface IFlowNode {
		void Execute(object graph);
	}

	internal interface IHighLevelNode {
		/// <summary>
		/// Return the node type
		/// </summary>
		Type NodeType { get; }
	}

	/// <summary>
	/// An interface for implementing data node
	/// </summary>
	public interface IDataNode {
		object GetValue(object graph);
		Type ReturnType();
	}

	public interface IDataNode<T> : IDataNode {
		new T GetValue(object graph);
	}

	/// <summary>
	/// A class that's implement IDataNode with additional functions
	/// </summary>
	public abstract class DataNode : IDataNode {
		public abstract object GetValue(object graph);

		public T GetValue<T>(object graph) {
			var val = GetValue(graph);
			if(val == null) {
				return default;
			}
			return (T)val;
		}

		public abstract Type ReturnType();
	}

	/// <summary>
	/// A class that's implement IDataNode with additional functions
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public abstract class DataNode<T> : IDataNode<T> {
		object IDataNode.GetValue(object graph) {
			return GetValue(graph);
		}

		public abstract T GetValue(object graph);

		public T1 GetValue<T1>(object graph) {
			var val = GetValue(graph) as object;
			if(val == null) {
				return default;
			}
			return (T1)val;
		}

		public virtual Type ReturnType() {
			return typeof(T);
		}
	}

	/// <summary>
	/// An interface for implementing State node which return if its a Success or Failure
	/// commands:
	/// =>return true; will finish the node with success state.
	/// =>return false; will finish the node with failure state.
	/// </summary>
	public interface IStateNode {
		bool Execute(object graph);
	}

	/// <summary>
	/// An interface for implementing Coroutine State Node
	/// commands:
	/// =>yield return break; will finish the coroutine with success state and execute On Success flow.
	/// =>yield return true; and => yield return "Success"; will finish the coroutine with success state and execute On Success flow.
	/// =>yield return false; and => yield return "Failure"; will finish the coroutine with failure state and execute On Failure flow.
	/// When the Execute function is finished without above command the node will finish with success state.
	/// </summary>
	public interface IStateCoroutineNode {
		IEnumerable Execute(object graph);
	}

	/// <summary>
	/// An interface for implementing Coroutine node
	/// </summary>
	public interface ICoroutineNode {
		IEnumerable Execute(object graph);
	}

	/// <summary>
	/// Definiton of flow port ( only for output )
	/// </summary>
	public struct FlowPortDefinition { }

	/// <summary>
	/// Definition of value port
	/// </summary>
	public struct ValuePortDefinition { }

	/// <summary>
	/// Implement this interface for make node for instanced members.
	/// </summary>
	public interface IInstanceNode { 
		
	}

	/// <summary>
	/// Implement this interface to make node for static members, the members must use static keyword.
	/// Use <see cref="IInstanceNode"/> instead if the node required having to store a value and use later.
	/// </summary>
	public interface IStaticNode {

	}

	/// <summary>
	/// Only used for enum members, and not usable for variable
	/// </summary>
	[AttributeUsage(AttributeTargets.Field)]
	public class PortDiscardAttribute : Attribute {

	}

	[AttributeUsage(AttributeTargets.All)]
	public class PortDescriptionAttribute : Attribute {
		public string name;
		public string description;

		public PortDescriptionAttribute() { }

		public PortDescriptionAttribute(string name) {
			this.name = name;
		}

		public PortDescriptionAttribute(string name, string description) {
			this.name = name;
			this.description = description;
		}
	}

	public abstract class NodePortAttribute : PortDescriptionAttribute {
		public string id;
		/// <summary>
		/// If true, the port will be primary port
		/// </summary>
		public bool primary;
		/// <summary>
		/// Only for value input/output
		/// </summary>
		public Type type;
	}

	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Method)]
	public class InputAttribute : NodePortAttribute {
		/// <summary>
		/// Only for value input
		/// </summary>
		public FilterAttribute filter;

		/// <summary>
		/// Assign this to automatic call the flow output when the input is finished ( only for flow input )
		/// </summary>
		public string exit;

		/// <summary>
		/// Assign this to automatic assign the default value of the port, the value is the name of the field, property or method and can be instance or static and public or private. ( only for value input )
		/// </summary>
		public string defaultValue;

		public InputAttribute() {

		}

		public InputAttribute(string name) {
			this.name = name;
		}

		public InputAttribute(string name, Type type) {
			this.name = name;
			this.type = type;
		}

		public InputAttribute(Type type) {
			this.type = type;
		}
	}

	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
	public class OutputAttribute : NodePortAttribute {
		public OutputAttribute() {

		}

		public OutputAttribute(string name) {
			this.name = name;
		}

		public OutputAttribute(string name, Type type) {
			this.name = name;
			this.type = type;
		}

		public OutputAttribute(Type type) {
			this.type = type;
		}
	}

	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Method)]
	public class PrimaryInputAttribute : InputAttribute {
		public PrimaryInputAttribute() {
			primary = true;
		}

		public PrimaryInputAttribute(string name) {
			primary = true;
			this.name = name;
		}

		public PrimaryInputAttribute(string name, Type type) {
			primary = true;
			this.name = name;
			this.type = type;
		}

		public PrimaryInputAttribute(Type type) {
			primary = true;
			this.type = type;
		}
	}

	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Method)]
	public class PrimaryOutputAttribute : OutputAttribute {
		public PrimaryOutputAttribute() {
			primary = true;
		}

		public PrimaryOutputAttribute(string name) {
			primary = true;
			this.name = name;
		}

		public PrimaryOutputAttribute(string name, Type type) {
			primary = true;
			this.name = name;
			this.type = type;
		}

		public PrimaryOutputAttribute(Type type) {
			primary = true;
			this.type = type;
		}
	}
}