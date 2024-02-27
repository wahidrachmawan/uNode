using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace MaxyGames.UNode.Nodes {
	public class ExposedNode : Node {
		[Serializable]
		public class OutputData {
			public string name;
			public SerializedType type = typeof(object);
			[NonSerialized]
			public ValueOutput port;
		}
		[HideInInspector]
		public List<OutputData> outputDatas = new List<OutputData>();
		[Tooltip("The value to expose.")]
		public ValueInput value { get; set; }

		protected override void OnRegister() {
			value = ValueInput(nameof(value), () => value.ValueType);
			for(int i = 0; i < outputDatas.Count; i++) {
				var d = outputDatas[i];
				d.port = ValueOutput(d.name, d.type.type);
				d.port.AssignGetCallback(instance => {
					var val = value.GetValue(instance);
					var member = val.GetType().GetMemberCached(d.name);
					if(member is FieldInfo field) {
						return field.GetValueOptimized(val);
					}
					else if(member is PropertyInfo prop) {
						return prop.GetValueOptimized(val);
					}
					else {
						throw null;
					}
				});
			}
		}

		public override void OnGeneratorInitialize() {
			for(int i = 0; i < outputDatas.Count; i++) {
				var data = outputDatas[i];
				if(data.port != null && data.port.isConnected) {
					CG.RegisterPort(data.port, () => {
						return CG.Value(value).CGAccess(data.name);
					});
				}
			}
		}

		public override string GetTitle() => "Exposed";

		#region Editors
		public void Refresh(bool addFields = false) {
			var type = value.type;
			var fields = type.GetMembers(BindingFlags.Public | BindingFlags.Instance);
			for(int x = 0; x < outputDatas.Count; x++) {
				bool valid = false;
				for(int y = 0; y < fields.Length; y++) {
					if(fields[y].MemberType != MemberTypes.Property && fields[y].MemberType != MemberTypes.Field)
						continue;
					if(outputDatas[x].name == fields[y].Name) {
						valid = true;
						break;
					}
				}
				if(!valid) {
					outputDatas.RemoveAt(x);
					x--;
				}
			}
			for(int x = 0; x < fields.Length; x++) {
				var m = fields[x];
				if(m is FieldInfo field) {
					if(field.Attributes.HasFlags(FieldAttributes.InitOnly))
						continue;
				}
				else if(m is PropertyInfo property) {
					if(!property.CanRead || property.GetIndexParameters().Length > 0) {
						continue;
					}
				}
				else {
					continue;
				}
				var t = ReflectionUtils.GetMemberType(m);
				bool found = false;
				for(int y = 0; y < outputDatas.Count; y++) {
					if(m.Name == outputDatas[y].name) {
						if(t != outputDatas[y].type.type) {
							outputDatas[y] = new OutputData() { name = m.Name, type = t };
						}
						found = true;
						break;
					}
				}
				if(!found && addFields) {
					outputDatas.Add(new OutputData() { name = m.Name, type = t });
				}
			}
		}
		#endregion
	}
}