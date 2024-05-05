using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

namespace MaxyGames.UNode {
	public class Variable : UGraphElement, IVariable, IVariableModifier, IAttributeSystem {
		[HideInInspector]
		public SerializedValue serializedValue;

		public Type type {
			get {
				return serializedValue?.type ?? typeof(object);
			}
			set {
				if(serializedValue == null) {
					serializedValue = new SerializedValue();
				}
				serializedValue.type = value;
			}
		}

		public SerialiedTypeKind typeKind {
			get {
				if(serializedValue != null) {
					return serializedValue.serializedType.typeKind;
				}
				return SerialiedTypeKind.None;
			}
		}


		public bool isOpenGeneric => serializedValue?.serializedType.isOpenGeneric ?? false;

		public object defaultValue {
			get {
				return serializedValue.value;
			}
			set {
				serializedValue = new SerializedValue(value, type);
			}
		}

		/// <summary>
		/// Are this variable only can be get.
		/// </summary>
		public bool onlyGet { get; set; }
		/// <summary>
		/// Reset the value on executed. ( Only for local variable )
		/// </summary>
		public bool resetOnEnter;

		public object Get(Flow flow) {
			if(resetOnEnter) {
				return flow.GetLocalData(this);
			}
			return flow.GetElementData(this);
		}

		public void Set(Flow flow, object value) {
			if(resetOnEnter) {
				flow.SetLocalData(this, ReflectionUtils.ValuePassing(value));
				return;
			}
			flow.SetElementData(this, ReflectionUtils.ValuePassing(value));
		}

		public object Get(GraphInstance instance) {
			if(resetOnEnter) {
				throw new Exception("Unable to get local variable, use Get(Flow) instead.");
			}
			return instance.GetElementData(this);
		}

		public void Set(GraphInstance instance, object value) {
			if(resetOnEnter) {
				throw new Exception("Unable to set local variable, use Set(Flow, object) instead.");
			}
			instance.SetElementData(this, value);
		}

		public override void OnRuntimeInitialize(GraphInstance instance) {
			if(resetOnEnter)
				return;
			if(instance.HasElementData(this))
				return;
			instance.SetElementData(this, SerializerUtility.Duplicate(defaultValue));
		}

		public string GetSummary() {
			return comment;
		}

		#region Editor
		/// <summary>
		/// The attribute data for script generation
		/// </summary>
		public List<AttributeData> attributes = new();

		/// <summary>
		/// The variable modifier for script generation
		/// </summary>
		public FieldModifier modifier = new FieldModifier();

		public bool showInInspector {
			get {
				if(modifier != null && modifier.Public || attributes.Any(a => a.type == typeof(SerializeField))) {
					return true;
				}
				return false;
			}
		}

		List<AttributeData> IAttributeSystem.Attributes { get => attributes; }

		public FieldModifier GetModifier() {
			return modifier;
		}

		[System.Runtime.Serialization.OnDeserialized]
		void OnAfterDeserialized() {
			if(resetOnEnter) {
				if(GetObjectInParent<ILocalVariableSystem>() == null) {
					resetOnEnter = false;
				}
			}
		}
		#endregion
	}
}