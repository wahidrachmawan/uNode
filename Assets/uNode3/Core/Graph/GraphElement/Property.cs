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
					instance.SetUserData(this, Activator.CreateInstance(ReturnType()));
				}
			}
		}

		public object Get(GraphInstance instance) {
			if(!AutoProperty) {
				if(getRoot != null) {
					return getRoot.Invoke(instance);
				} else {
					throw new System.Exception("Can't get value of Property because no Getter.");
				}
			}
			return instance.GetUserData(this);
		}

		public void Set(GraphInstance instance, object value) {
			if(AutoProperty) {
				instance.SetUserData(this, value);
			} else {
				if(setRoot != null) {
					setRoot.Invoke(instance, new object[] { value });
				} else {
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
	}
}