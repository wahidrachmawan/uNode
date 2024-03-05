using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

namespace MaxyGames.UNode {
	public class ValueInput : ValuePort {
		/// <summary>
		/// The default value of the port
		/// </summary>
		public MemberData defaultValue;

		/// <summary>
		/// The port filter
		/// </summary>
		[NonSerialized]
		public FilterAttribute filter;

		public bool UseDefaultValue => connections.Count == 0;

		/// <summary>
		/// Get target type of the port
		/// </summary>
		public MemberData.TargetType TargetType {
			get {
				if(UseDefaultValue) return defaultValue.targetType;
				if(ValidConnections.Any()) return MemberData.TargetType.NodePort;
				return MemberData.TargetType.None;
			}
		}

		/// <summary>
		/// True if the port is assigned ( Use default value or have valid connection )
		/// </summary>
		public bool isAssigned {
			get {
				if(UseDefaultValue) {
					return defaultValue.isAssigned;
				} else {
					return isConnected && connections[0].isValid;
				}
			}
		}

		/// <summary>
		/// Get connected target port
		/// </summary>
		/// <returns></returns>
		public ValueOutput GetTargetPort() {
			if(connections.Count > 0) {
				return connections[0].output;
			}
			return null;
		}

		/// <summary>
		/// Get connected target node
		/// </summary>
		/// <returns></returns>
		public NodeObject GetTargetNode() {
			if (connections.Count > 0) {
				return connections[0].output.node;
			}
			return null;
		}

		/// <summary>
		/// Get rich name of the port
		/// </summary>
		/// <returns></returns>
		public string GetRichName() {
			if(UseDefaultValue) {
				return defaultValue.GetNicelyDisplayName(richName: true);
			} else {
				var targetPort = this.GetTargetPort();
				if(targetPort != null) {
					if(targetPort == targetPort.node?.primaryValueOutput) {
						return targetPort.node.GetRichName();
					}
					return targetPort.GetPrettyName();
				}
			}
			return GetPrettyName();
		}

		public void Validate() {
			if(defaultValue != null) {
				switch(defaultValue.targetType) {
					case MemberData.TargetType.NodePort:
						Connection.CreateAndConnect(this, defaultValue.Get<ValueOutput>(null));
						this.defaultValue = MemberData.None;
						break;
					case MemberData.TargetType.Values:
						if(defaultValue.type.IsCastableTo(type) == false) {
							defaultValue.CopyFrom(MemberData.CreateValueFromType(type));
						}
						break;
				}
			}
		}

		/// <summary>
		/// Assign the port
		/// </summary>
		/// <param name="defaultValue"></param>
		public void AssignToDefault(MemberData defaultValue) {
			if(defaultValue == null)
				defaultValue = MemberData.None;
			ClearConnections();
			this.defaultValue = defaultValue;
			Validate();
		}

		/// <summary>
		/// Assign the port
		/// </summary>
		/// <param name="defaultValue"></param>
		public void AssignToDefault(object defaultValue) {
			if(defaultValue == null)
				defaultValue = MemberData.None;
			if(defaultValue is IGraph) {
				AssignToDefault(MemberData.This(defaultValue));
			} 
			else if(defaultValue is MemberData) {
				AssignToDefault(defaultValue as MemberData);
			}
			else if(defaultValue is ValueOutput) {
				ClearConnections();
				Connection.CreateAndConnect(this, defaultValue as ValueOutput);
				this.defaultValue = MemberData.None;
			}
			else if(defaultValue is ValueInput) {
				var port = defaultValue as ValueInput;
				if(port.GetTargetPort() != null) {
					AssignToDefault(port.GetTargetPort());
				}
				else {
					AssignToDefault(MemberData.Clone(port.defaultValue));
				}
			}
			else {
				AssignToDefault(MemberData.CreateFromValue(defaultValue));
			}
		}

		/// <summary>
		/// The value type of the port
		/// </summary>
		public Type ValueType {
			get {
				if(UseDefaultValue && defaultValue != null && defaultValue.isAssigned) {
					return defaultValue.type ?? typeof(object);
				} else if(connections.Count == 1) {
					return connections[0].output.type;
				}
				if(_type != null)
					return _type;
				return typeof(object);
			}
		}

		public ValueInput(NodeObject node) : base(node) { }

		public ValueInput(NodeObject node, string id, Type type) : base(node) {
			this.id = id;
			this.type = type;
		}

		public ValueInput(NodeObject node, string id, Func<Type> type) : base(node) {
			this.id = id;
			this.dynamicType = type;
		}

		public ValueInput(NodeObject node, string name) : base(node) {
			this.name = name;
			this.id = name;
		}

		/// <summary>
		/// Get the port value
		/// </summary>
		/// <param name="flow"></param>
		/// <returns></returns>
		/// <exception cref="GraphException"></exception>
		public object GetValue(Flow flow) {
#if UNITY_EDITOR
			if(node.IsValidElement() == false) {
				//For live editing
				var validPort = flow.GetValidPort(this);
				if(validPort != null) {
					//We found the valid element and so we get the value from the valid element instead.
					return validPort.GetValue(flow);
				}
				//Log to console if the valid node is not found.
				Debug.Log(GraphException.GetMessage("Live editing: trying to get on invalid node." + "Node: " + node.GetTitle() + " - id:" + node.id, node));
			}
#endif
			if(UseDefaultValue) {
				return defaultValue.Get(flow);
			} else {
				if(connections.Count == 1) {
					return connections[0].GetValue(flow);
				} else {
					if(connections.Count > 1) {
						throw new GraphException("Attemp to get value on multiple connection which is not allowed", node);
					}
					throw new GraphException("Invalid connections.", node);
				}
			}
		}

		/// <summary>
		/// Get the port value
		/// </summary>
		/// <param name="flow"></param>
		/// <param name="convertType"></param>
		/// <returns></returns>
		public object GetValue(Flow flow, Type convertType) {
			if(convertType != null) {
				object resultValue = GetValue(flow);
				if(resultValue != null) {
					if(resultValue.GetType() == type)
						return resultValue;
					return Operator.Convert(resultValue, type);
				}
			}
			return GetValue(flow);
		}

		/// <summary>
		/// Get the port value
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="flow"></param>
		/// <returns></returns>
		public T GetValue<T>(Flow flow) {
			var obj = GetValue(flow);
			if(!object.ReferenceEquals(obj, null)) {
				return Operator.Convert<T>(obj);
			}
			return default;
		}

		/// <summary>
		/// Set the port value
		/// </summary>
		/// <param name="flow"></param>
		/// <param name="value"></param>
		/// <exception cref="Exception"></exception>
		/// <exception cref="System.Exception"></exception>
		public void SetValue(Flow flow, object value) {
#if UNITY_EDITOR
			if(node.IsValidElement() == false) {
				//For live editing
				var validPort = flow.GetValidPort(this);
				if(validPort != null) {
					//We found the valid element and so we get the value from the valid element instead.
					validPort.SetValue(flow, value);
					return;
				}
				//Log to console if the valid node is not found.
				Debug.Log(new GraphException("Live editing: trying to set on invalid node." + "Node: " + node.GetTitle() + " - id:" + node.id, node));
			}
#endif
			if(UseDefaultValue) {
				if(defaultValue.isAssigned) {
					if(defaultValue.CanSetValue()) {
						defaultValue.Set(flow, value);
					} else {
						throw new Exception("Unable to set value for the input because the input cannot be set.");
					}
				} else {
					throw new Exception("The port is unassigned");
				}
			} else if(connections.Count == 1) {
				connections[0].SetValue(flow, value);
			} else {
				if(connections.Count > 1) {
					throw new Exception("Attemp to get value on multiple connection which is not allowed");
				}
				throw new System.Exception("Invalid connections.");
			}
		}

		public void Restore(ValueInput other) {
			if(other.id != id)
				throw new Exception("Cannot restore port because the id is different.");
			name = other.name;
			if(other.dynamicType != null)
				dynamicType = other.dynamicType;
			else if(other._type != null)
				_type = other._type;
		}
	}
}