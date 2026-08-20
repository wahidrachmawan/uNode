using System;
using System.Globalization;
using System.Reflection;

namespace MaxyGames.UNode {
	/// <summary>
	/// Shared helpers for members that live in the hand-written half of a `partial` graph.
	/// </summary>
	internal static class ExternalMemberUtility {
		internal const BindingFlags flags =
			BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

		/// <summary>
		/// The real compiled type behind an instance, or null when the instance is still
		/// a uNode runtime graph (ie. the graph has not been compiled to C# yet).
		/// </summary>
		internal static Type NativeTypeOf(object obj) {
			if(obj == null)
				return null;
			if(obj is IInstancedGraph)
				return null;
			var type = obj.GetType();
			return type is RuntimeType ? null : type;
		}

		internal static Exception NotCompiled(RuntimeType owner, string name) {
			return new Exception(
				$"`{name}` is declared in the hand-written half of the partial class `{owner.PrettyName(true)}`, " +
				"so it only exists once the graph is compiled to C#.\n" +
				"It can be wired up in the graph, but it cannot be executed in reflection (interpreted) mode. " +
				"Compile the graph to C# to run it.");
		}
	}

	/// <summary>
	/// A field declared in the hand-written half of a `partial` graph.
	/// </summary>
	public class RuntimeGraphExternalField : RuntimeField, ISummary {
		public readonly PartialMemberInfo info;

		public RuntimeGraphExternalField(RuntimeType owner, PartialMemberInfo info) : base(owner) {
			this.info = info;
		}

		public override string Name => info.name;
		public override Type FieldType => info.type ?? typeof(object);

		public override FieldAttributes Attributes {
			get {
				var att = info.isPublic ? FieldAttributes.Public : FieldAttributes.Private;
				if(info.isStatic)
					att |= FieldAttributes.Static;
				return att;
			}
		}

		public string GetSummary() => info.summary;

		public override object[] GetCustomAttributes(bool inherit) => Array.Empty<object>();

		public override object[] GetCustomAttributes(Type attributeType, bool inherit) => Array.Empty<object>();

		private FieldInfo Native(object obj) {
			return ExternalMemberUtility.NativeTypeOf(obj)?.GetField(info.name, ExternalMemberUtility.flags);
		}

		public override object GetValue(object obj) {
			var native = Native(obj);
			if(native != null)
				return native.GetValue(obj);
			throw ExternalMemberUtility.NotCompiled(owner, Name);
		}

		public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, CultureInfo culture) {
			var native = Native(obj);
			if(native != null) {
				native.SetValue(obj, value, invokeAttr, binder, culture);
				return;
			}
			throw ExternalMemberUtility.NotCompiled(owner, Name);
		}
	}

	/// <summary>
	/// A property declared in the hand-written half of a `partial` graph.
	/// </summary>
	public class RuntimeGraphExternalProperty : RuntimeProperty, ISummary {
		public readonly PartialMemberInfo info;

		public RuntimeGraphExternalProperty(RuntimeType owner, PartialMemberInfo info) : base(owner) {
			this.info = info;
		}

		public override string Name => info.name;
		public override Type PropertyType => info.type ?? typeof(object);
		public override bool CanRead => info.canRead;
		public override bool CanWrite => info.canWrite;

		public string GetSummary() => info.summary;

		//Without accessors a property is filtered out of every member lookup,
		//so both are built on demand from what the source scan found.
		private RuntimePropertyGetMethod m_getMethod;
		public override MethodInfo GetGetMethod(bool nonPublic) {
			if(!info.canRead)
				return null;
			if(m_getMethod == null) {
				m_getMethod = new RuntimePropertyGetMethod(owner, this);
			}
			return m_getMethod;
		}

		private RuntimePropertySetMethod m_setMethod;
		public override MethodInfo GetSetMethod(bool nonPublic) {
			if(!info.canWrite)
				return null;
			if(m_setMethod == null) {
				m_setMethod = new RuntimePropertySetMethod(owner, this);
			}
			return m_setMethod;
		}

		public override MethodInfo[] GetAccessors(bool nonPublic) {
			var get = GetGetMethod(nonPublic);
			var set = GetSetMethod(nonPublic);
			if(get != null && set != null)
				return new[] { get, set };
			if(get != null)
				return new[] { get };
			if(set != null)
				return new[] { set };
			return Array.Empty<MethodInfo>();
		}

		private PropertyInfo Native(object obj) {
			return ExternalMemberUtility.NativeTypeOf(obj)?.GetProperty(info.name, ExternalMemberUtility.flags);
		}

		public override object GetValue(object obj, BindingFlags invokeAttr, Binder binder, object[] index, CultureInfo culture) {
			var native = Native(obj);
			if(native != null)
				return native.GetValue(obj, invokeAttr, binder, index, culture);
			throw ExternalMemberUtility.NotCompiled(owner, Name);
		}

		public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, object[] index, CultureInfo culture) {
			var native = Native(obj);
			if(native != null) {
				native.SetValue(obj, value, invokeAttr, binder, index, culture);
				return;
			}
			throw ExternalMemberUtility.NotCompiled(owner, Name);
		}
	}

	/// <summary>
	/// A method declared in the hand-written half of a `partial` graph.
	/// </summary>
	public class RuntimeGraphExternalMethod : RuntimeMethod, ISummary {
		public readonly PartialMemberInfo info;
		private ParameterInfo[] parameters;

		public RuntimeGraphExternalMethod(RuntimeType owner, PartialMemberInfo info) : base(owner) {
			this.info = info;
		}

		public override string Name => info.name;
		public override Type ReturnType => info.type ?? typeof(void);

		public override MethodAttributes Attributes {
			get {
				var att = info.isPublic ? MethodAttributes.Public : MethodAttributes.Private;
				if(info.isStatic)
					att |= MethodAttributes.Static;
				return att;
			}
		}

		public string GetSummary() => info.summary;

		public override ParameterInfo[] GetParameters() {
			if(parameters == null) {
				var source = info.parameters ?? Array.Empty<PartialParameterInfo>();
				parameters = new ParameterInfo[source.Length];
				for(int i = 0; i < source.Length; i++) {
					var p = source[i];
					var type = p.type ?? typeof(object);
					if(p.refKind != RefKind.None) {
						type = ReflectionUtils.MakeByRefType(type);
					}
					var att = ParameterAttributes.None;
					if(p.refKind == RefKind.Out)
						att = ParameterAttributes.Out;
					else if(p.refKind == RefKind.In)
						att = ParameterAttributes.In;
					if(p.hasDefaultValue)
						att |= ParameterAttributes.HasDefault | ParameterAttributes.Optional;
					parameters[i] = new RuntimeParameterInfo(p.name, type, att);
				}
			}
			return parameters;
		}

		public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture) {
			var nativeType = ExternalMemberUtility.NativeTypeOf(obj);
			var native = nativeType?.GetMethod(info.name, ExternalMemberUtility.flags, null, info.ParameterTypes(), null);
			if(native != null)
				return native.Invoke(obj, invokeAttr, binder, parameters, culture);
			throw ExternalMemberUtility.NotCompiled(owner, Name);
		}
	}
}
