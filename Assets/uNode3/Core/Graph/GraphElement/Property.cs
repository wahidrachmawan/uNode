using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

namespace MaxyGames.UNode {
	public sealed class Property : UGraphElement, IGraphValue, IPropertyModifier, IAttributeSystem, ISummary {
		public SerializedType type = typeof(object);

		public PropertyModifier modifier = new PropertyModifier();
		public FunctionModifier getterModifier = new FunctionModifier(), setterModifier = new FunctionModifier();

		public List<AttributeData> attributes = new();
		public List<AttributeData> fieldAttributes = new();
		public List<AttributeData> getterAttributes = new();
		public List<AttributeData> setterAttributes = new();

		public PropertyAccessorKind accessor = PropertyAccessorKind.ReadWrite;

		[HideInInspector]
		public Function setRoot, getRoot;

		public bool AutoProperty {
			get {
				return setRoot == null && getRoot == null;
			}
		}

		List<AttributeData> IAttributeSystem.Attributes { get => attributes; }

		public bool CanGetValue() {
			return AutoProperty && (accessor == PropertyAccessorKind.ReadWrite || accessor == PropertyAccessorKind.ReadOnly) || getRoot != null;
		}

		public bool CanSetValue() {
			return AutoProperty && (accessor == PropertyAccessorKind.ReadWrite || accessor == PropertyAccessorKind.WriteOnly)  || setRoot != null;
		}

		public bool CreateGetter() {
			if(getRoot == null) {
				getRoot = AddChild(new Function("Getter", ReturnType()));
				return true;
			}
			return false;
		}

		public bool CreateSeter() {
			if(setRoot == null) {
				setRoot = AddChild(new Function("Setter", typeof(void), new[] { new ParameterData("value", ReturnType()) }));
				return true;
			}
			return false;
		}

		public override void OnRuntimeInitialize(GraphInstance instance) {
			if(AutoProperty) {
				if(ReturnType().IsValueType) {
					instance.SetElementData(this, Activator.CreateInstance(ReturnType()));
				}
			}
		}

		public object Get(GraphInstance instance) {
			if(instance.graph != graphContainer && (modifier.Virtual || modifier.Override || graphContainer.IsInterface())) {
				var graph = instance.graph;
				Property property = null;
				while(property == null) {
					property = graph.GetProperty(name);
					if(property == null) {
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
				return property.DoGet(instance);
			}
			return DoGet(instance);
		}

		public void Set(GraphInstance instance, object value) {
			if(instance.graph != graphContainer && (modifier.Virtual || modifier.Override || graphContainer.IsInterface())) {
				var graph = instance.graph;
				Property property = null;
				while(property == null) {
					property = graph.GetProperty(name);
					if(property == null) {
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
				property.DoSet(instance, value);
				return;
			}
			DoSet(instance, value);
		}

		internal object DoGet(GraphInstance instance) {
			if(!AutoProperty) {
				if(getRoot != null) {
					return getRoot.DoInvoke(instance, null);
				}
				else {
					throw new System.Exception("Can't get value of Property because no Getter.");
				}
			}
			return instance.GetElementData(this);
		}

		internal void DoSet(GraphInstance instance, object value) {
			if(AutoProperty) {
				instance.SetElementData(this, ReflectionUtils.ValuePassing(value));
			}
			else {
				if(setRoot != null) {
					setRoot.Invoke(instance, new object[] { value });
				}
				else {
					throw new System.Exception("Can't set value of Property because no Setter.");
				}
			}
		}

		public System.Type ReturnType() {
			if(type != null) {
				return type.type;
			}
			return typeof(object);
		}

		public PropertyModifier GetModifier() {
			return modifier;
		}

		public string GetSummary() {
			return comment;
		}

		object IGraphValue.Get(Flow flow) {
			return Get(flow);
		}

		void IGraphValue.Set(Flow flow, object value) {
			Set(flow, value);
		}
	}
}