using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;

namespace MaxyGames.UNode {
	public abstract class RuntimeNativeType : RuntimeType, INativeType {
		public MemberInfo GetNativeMember() {
			return GetNativeType();
		}

		public abstract Type GetNativeType();

		public override Type[] GetInterfaces() {
			var baseType = BaseType;
			if(baseType != null) {
				return baseType.GetInterfaces();
			}
			return Type.EmptyTypes;
		}
	}

	public class RuntimeNativeGraph : RuntimeNativeType, IIcon, IRuntimeMemberWithRef {
		public GraphAsset target;

		public RuntimeNativeGraph(GraphAsset target) {
			this.target = target;
		}

		public override string Name {
			get {
				return target.GetGraphName();
			}
		}

		public override string Namespace {
			get {
				if(target is INamespace ns) {
					return ns.Namespace;
				}
				else if(target is IScriptGraphType scriptGraphType) {
					if(scriptGraphType.ScriptTypeData.scriptGraph == null)
						throw new Exception("The ScriptGraph asset is null");

					return scriptGraphType.ScriptTypeData.scriptGraph.Namespace;
				}
				return string.Empty;
			}
		}

		public override Type BaseType {
			get {
				if(target is IClassGraph classGraph) {
					return classGraph.InheritType ?? typeof(object);
				}
				else if(target is IScriptInterface) {
					return null;
				}
				return typeof(object);
			}
		}

		public override bool IsValid() {
			try {
				return target != null;
			}
			catch {
				return false;
			}
		}

		#region Build
		public override void Update() {
			if(constructors != null) {
				BuildConstructors();
			}
			if(fields != null) {
				BuildFields();
			}
			if(properties != null) {
				BuildProperties();
			}
			if(methods != null) {
				BuildMethods();
			}
			if(interfaces != null) {
				BuildInterfaces();
			}
		}

		Type[] interfaces;
		private void BuildInterfaces() {
			var baseInterfaces = BaseType != null ? BaseType.GetInterfaces() : Type.EmptyTypes;
			if(target is IInterfaceSystem system) {
				var iface = system.Interfaces;
				if(iface.Count == 0 && baseInterfaces.Length == 0) {
					interfaces = Type.EmptyTypes;
				}
				else {
					List<SerializedType> targetIfaces = new(iface);
					for(int i = 0; i < targetIfaces.Count; i++) {
						if(targetIfaces[i].type == null || baseInterfaces.Contains(iface[i].type)) {
							targetIfaces.RemoveAt(i);
							i--;
							continue;
						}
					}
					interfaces = new Type[targetIfaces.Count + baseInterfaces.Length];
					for(int i = 0; i < targetIfaces.Count; i++) {
						interfaces[i] = targetIfaces[i];
					}
					for(int i = 0; i < baseInterfaces.Length; i++) {
						interfaces[i + targetIfaces.Count] = baseInterfaces[i];
					}
				}
			}
			else {
				interfaces = baseInterfaces;
			}
			
		}

		List<ConstructorInfo> constructors;
		private void BuildConstructors() {
			if(target is IScriptInterface) {
				if(constructors == null) {
					constructors = new List<ConstructorInfo>();
				}
				else {
					constructors.Clear();
				}
				return;
			}
			if(target is IClassModifier cls) {
				if(cls.GetModifier().Static) {
					if(constructors == null) {
						constructors = new List<ConstructorInfo>();
					}
					else {
						constructors.Clear();
					}
					return;
				}
			}
			List<ConstructorInfo> members = new List<ConstructorInfo>();
			if(!this.IsSubclassOf(typeof(MonoBehaviour))) {
				foreach(var m in target.GetConstructors()) {
					if(m.modifier.isPublic) {
						members.Add(new RuntimeNativeGraphConstructor(this, new ConstructorRef(m, target)));
					}
				}
				if(members.Count == 0) {
					members.Add(new RuntimeNativeConstructor(this));
				}
			}
			constructors = members;
		}

		List<FieldInfo> fields;
		Dictionary<int, RuntimeNativeField> m_runtimeFields = new Dictionary<int, RuntimeNativeField>();
		private void BuildFields() {
			var inheritMembers = BaseType != null ? BaseType.GetFields(MemberData.flags) : Array.Empty<FieldInfo>();
			if(fields == null) {
				List<FieldInfo> members = new List<FieldInfo>(inheritMembers);
				foreach(var m in target.GetVariables()) {
					var value = new RuntimeNativeField(this, new VariableRef(m, target));
					m_runtimeFields[m.id] = value;
					members.Add(value);
				}
				fields = members;
			}
			else {
				fields.Clear();
				List<FieldInfo> members = fields;
				members.AddRange(inheritMembers);
				foreach(var (_, m) in m_runtimeFields) {
					if(m.target.isValid) {
						members.Add(m);
					}
				}
				foreach(var m in target.GetVariables()) {
					if(!m_runtimeFields.ContainsKey(m.id)) {
						var value = new RuntimeNativeField(this, new VariableRef(m, target));
						m_runtimeFields[m.id] = value;
						members.Add(value);
					}
				}
				fields = members;
			}
		}

		List<PropertyInfo> properties;
		Dictionary<int, RuntimeNativeProperty> m_runtimeProperties = new Dictionary<int, RuntimeNativeProperty>();
		private void BuildProperties() {
			var inheritMembers = BaseType != null ? BaseType.GetProperties(MemberData.flags) : Array.Empty<PropertyInfo>();
			if(properties == null) {
				List<PropertyInfo> members = new List<PropertyInfo>(inheritMembers);
				foreach(var m in target.GetProperties()) {
					if(m.modifier.Override == false) {
						var value = new RuntimeNativeProperty(this, new PropertyRef(m, target));
						m_runtimeProperties[m.id] = value;
						members.Add(value);
					}
				}
				properties = members;
			}
			else {
				properties.Clear();
				List<PropertyInfo> members = properties;
				members.AddRange(inheritMembers);
				foreach(var (_, m) in m_runtimeProperties) {
					if(m.target.isValid) {
						members.Add(m);
					}
				}
				foreach(var m in target.GetProperties()) {
					if(m.modifier.Override == false && !m_runtimeProperties.ContainsKey(m.id)) {
						var value = new RuntimeNativeProperty(this, new PropertyRef(m, target));
						m_runtimeProperties[m.id] = value;
						members.Add(value);
					}
				}
				properties = members;
			}
		}

		List<MethodInfo> methods;
		Dictionary<int, RuntimeNativeMethod> m_runtimeMethods = new Dictionary<int, RuntimeNativeMethod>();
		private void BuildMethods() {
			var inheritMembers = BaseType != null ? BaseType.GetMethods(MemberData.flags) : Array.Empty<MethodInfo>();
			if(methods == null) {
				List<MethodInfo> members = new List<MethodInfo>(inheritMembers);
				foreach(var m in target.GetFunctions()) {
					if(m.modifier.Override == false) {
						var value = new RuntimeNativeMethod(this, new FunctionRef(m, target));
						m_runtimeMethods[m.id] = value;
						members.Add(value);
					}
				}
				methods = members;
			}
			else {
				methods.Clear();
				List<MethodInfo> members = methods;
				members.AddRange(inheritMembers);
				foreach(var (_, m) in m_runtimeMethods) {
					if(m.target.isValid) {
						members.Add(m);
					}
				}
				foreach(var m in target.GetFunctions()) {
					if(m.modifier.Override == false && !m_runtimeMethods.ContainsKey(m.id)) {
						var value = new RuntimeNativeMethod(this, new FunctionRef(m, target));
						m_runtimeMethods[m.id] = value;
						members.Add(value);
					}
				}
				methods = members;
			}
		}
		#endregion

		#region Overrides
		public override MemberInfo[] GetMembers(BindingFlags bindingAttr) {
			if(this.constructors == null) {
				BuildConstructors();
			}
			if(this.fields == null) {
				BuildFields();
			}
			if(this.properties == null) {
				BuildProperties();
			}
			if(this.methods == null) {
				BuildMethods();
			}
			var constructors = ReflectionUtils.GetConstructorCandidates(this.constructors, bindingAttr, this);
			var fields = ReflectionUtils.GetFieldCandidates(this.fields, bindingAttr, this);
			var properties = ReflectionUtils.GetPropertyCandidates(this.properties, bindingAttr, this);
			var methods = ReflectionUtils.GetMethodCandidates(this.methods, bindingAttr, this);
			var members = new MemberInfo[constructors.Count + fields.Count + properties.Count + methods.Count];
			for(int i = 0; i < constructors.Count; i++) {
				members[i] = constructors[i];
			}
			int idx = constructors.Count;
			for(int i = 0; i < fields.Count; i++) {
				members[i + idx] = fields[i];
			}
			idx += fields.Count;
			for(int i = 0; i < properties.Count; i++) {
				members[i + idx] = properties[i];
			}
			idx += properties.Count;
			for(int i = 0; i < methods.Count; i++) {
				members[i + idx] = methods[i];
			}
			return members;
		}

		public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr) {
			if(constructors == null) {
				BuildConstructors();
			}
			return ReflectionUtils.GetConstructorCandidates(constructors, bindingAttr, this).ToArray();
		}

		public override FieldInfo GetField(string name, BindingFlags bindingAttr) {
			if(fields == null) {
				BuildFields();
			}
			for(int i = 0; i < fields.Count; i++) {
				if(fields[i].Name == name) {
					return fields[i];
				}
			}
			return null;
		}

		public override FieldInfo[] GetFields(BindingFlags bindingAttr) {
			if(fields == null) {
				BuildFields();
			}
			return ReflectionUtils.GetFieldCandidates(fields, bindingAttr, this).ToArray();
		}

		public override MethodInfo[] GetMethods(BindingFlags bindingAttr) {
			if(methods == null) {
				BuildMethods();
			}
			return ReflectionUtils.GetMethodCandidates(methods, bindingAttr, this).ToArray();
		}

		public override PropertyInfo[] GetProperties(BindingFlags bindingAttr) {
			if(properties == null) {
				BuildProperties();
			}
			return ReflectionUtils.GetPropertyCandidates(properties, bindingAttr, this).ToArray();
		}

		public override Type GetInterface(string fullname, bool ignoreCase) {
			if(interfaces == null) {
				BuildInterfaces();
			}
			SplitName(fullname, out var name, out var ns);
			var ifaces = interfaces;
			for(int i=0;i<ifaces.Length;i++) {
				if(!FilterApplyPrefixLookup(ifaces[i], name, ignoreCase)) {
					continue;
				}
				if(ns != null && !ifaces[i].Namespace.Equals(ns)) {
					continue;
				}
				return ifaces[i];
			}
			return null;
		}

		public override Type[] GetInterfaces() {
			if(interfaces == null) {
				BuildInterfaces();
			}
			return interfaces;
		}

		protected override TypeAttributes GetAttributeFlagsImpl() {
			var att = TypeAttributes.Public;
			if(target is IClassModifier cls) {
				var modifier = cls.GetModifier();
				if(modifier.Static) {
					att |= TypeAttributes.Abstract | TypeAttributes.Sealed;
				}
				else if(modifier.Abstract) {
					att |= TypeAttributes.Abstract;
				}
				else if(modifier.Sealed) {
					att |= TypeAttributes.Sealed;
				}
			}
			if(target is IClassGraph) {
				att |= TypeAttributes.Class;
			}
			else if(target is IScriptInterface) {
				att |= TypeAttributes.ClassSemanticsMask;
				att |= TypeAttributes.Interface;
			}
			return att;
		}

		public override bool Equals(object obj) {
			if(obj == null) {
				return target == null || base.Equals(obj);
			}
			return base.Equals(obj);
		}

		public override int GetHashCode() {
			return target.GetHashCode();
		}

		public override object[] GetCustomAttributes(bool inherit) {
			if(target is IAttributeSystem) {
				return (target as IAttributeSystem).GetAttributes();
			}
			return new object[0];
		}

		public override object[] GetCustomAttributes(Type attributeType, bool inherit) {
			if(target is IAttributeSystem) {
				return (target as IAttributeSystem).GetAttributes(attributeType);
			}
			return new object[0];
		}

		public override bool IsAssignableFrom(Type c) {
			if(c is RuntimeType) {
				if(c == null) return false;
				if(this == c) {
					return true;
				}
				if(c.IsSubclassOf(this)) {
					return true;
				}
				return false;
			}
			var type = GetNativeType();
			if(type != null) {
				return type.IsAssignableFrom(c);
			}
			return false;
		}

		public override bool IsInstanceOfType(object o) {
			var type = GetNativeType();
			if(type != null) {
				return type.IsInstanceOfType(o);
			}
			return false;
		}

		protected override bool HasElementTypeImpl() {
			var type = GetNativeType();
			if(type != null) {
				return type.HasElementType;
			}
			return false;
		}

		protected override bool IsArrayImpl() {
			var type = GetNativeType();
			if(type != null) {
				return type.IsArray;
			}
			return false;
		}

		protected override bool IsByRefImpl() {
			var type = GetNativeType();
			if(type != null) {
				return type.IsByRef;
			}
			return false;
		}

		protected override bool IsCOMObjectImpl() {
			var type = GetNativeType();
			if(type != null) {
				return type.IsCOMObject;
			}
			return false;
		}

		public override Type[] GetGenericArguments() {
			var type = GetNativeType();
			if(type != null) {
				return type.GetGenericArguments();
			}
			return Type.EmptyTypes;
		}

		public override Type[] GetGenericParameterConstraints() {
			var type = GetNativeType();
			if(type != null) {
				return type.GetGenericParameterConstraints();
			}
			return Type.EmptyTypes;
		}

		public override Type GetGenericTypeDefinition() {
			var type = GetNativeType();
			if(type != null) {
				return type.GetGenericTypeDefinition();
			}
			return null;
		}
		#endregion

		#region Functions
		public string GetSummary() {
			return target.GraphData.comment;
		}

		public BaseReference GetReference() {
			if(target is IReflectionType t) {
				return new NativeTypeRef(t);
			}
			return new GraphRef(target);
		}

		private Type m_nativeType;
		private bool m_hasGetNativeType;
		public override Type GetNativeType() {
			if(m_hasGetNativeType == false) {
				m_nativeType = FullName.ToType(false);
				m_hasGetNativeType = true;
			}
			return m_nativeType;
		}

		Type IIcon.GetIcon() {
			if(target is IIcon icon) {
				return icon.GetIcon();
			}
			else if(target is ICustomIcon customIcon) {
				return TypeIcons.FromTexture(customIcon.GetIcon());
			}
			return null;
		}
		#endregion
	}

	public class RuntimeNativeEnum : RuntimeNativeType, IIcon, IRuntimeMemberWithRef {
		public readonly EnumScript target;

		public override Type BaseType => typeof(Enum);
		public override string Name => target.ScriptName;

		public override string Namespace {
			get {
				if(target is IScriptGraphType scriptGraphType) {
					if(scriptGraphType.ScriptTypeData.scriptGraph == null)
						throw new Exception("The ScriptGraph asset is null");
					return scriptGraphType.ScriptTypeData.scriptGraph.Namespace;
				}
				return string.Empty;
			}
		}

		public override bool IsEnum => true;

		public RuntimeNativeEnum(EnumScript target) {
			this.target = target;
		}

		public override FieldInfo GetField(string name, BindingFlags bindingAttr) {
			return typeof(Enum).GetField(name, bindingAttr);
		}

		public override FieldInfo[] GetFields(BindingFlags bindingAttr) {
			return typeof(Enum).GetFields(bindingAttr);
		}

		public override MethodInfo[] GetMethods(BindingFlags bindingAttr) {
			return typeof(Enum).GetMethods(bindingAttr);
		}

		private Type m_nativeType;
		private bool m_hasGetNativeType;
		public override Type GetNativeType() {
			if(m_hasGetNativeType == false) {
				m_nativeType = FullName.ToType(false);
				m_hasGetNativeType = true;
			}
			return m_nativeType;
		}

		public override PropertyInfo[] GetProperties(BindingFlags bindingAttr) {
			return typeof(Enum).GetProperties(bindingAttr);
		}

		protected override TypeAttributes GetAttributeFlagsImpl() {
			return TypeAttributes.AutoClass | TypeAttributes.AnsiClass | TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed;
		}

		public Type GetIcon() {
			if(target is IIcon icon) {
				return icon.GetIcon();
			}
			else if(target is ICustomIcon customIcon) {
				return TypeIcons.FromTexture(customIcon.GetIcon());
			}
			return null;
		}

		public BaseReference GetReference() {
			if(target is IReflectionType t) {
				return new NativeTypeRef(t);
			}
			return new UnityObjectReference(target);
		}
	}
}