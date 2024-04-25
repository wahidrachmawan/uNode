using System;
using System.Collections.Generic;
using UnityEngine;

namespace MaxyGames.UNode.Nodes {
	[NodeMenu("Data", "Select", 
		inputs = new[] { typeof(bool), typeof(int), typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(long), typeof(ulong), typeof(uint), typeof(string), typeof(System.Enum) }, 
		outputs = new[] { typeof(bool), typeof(int), typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(long), typeof(ulong), typeof(uint), typeof(string), typeof(System.Enum) })]
	public class SelectNode : ValueNode {
		[Filter(OnlyGetType = true)]
		public SerializedType targetType = typeof(object);
		[NonSerialized]
		public ValueInput target;

		public class Data {
			public string id = uNodeUtility.GenerateUID();
			public MemberData value = MemberData.None;

			[NonSerialized]
			public ValueInput port;
		}
		[HideInInspector]
		public List<Data> datas = new List<Data>();
		[NonSerialized]
		public ValueInput defaultTarget;

		protected override void OnRegister() {
			base.OnRegister();
			target = ValueInput(nameof(target), typeof(object), MemberData.None);
			target.filter = new FilterAttribute(typeof(bool), typeof(int), typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(long), typeof(ulong), typeof(uint), typeof(string), typeof(System.Enum)) { InvalidTargetType = MemberData.TargetType.Null };
			defaultTarget = ValueInput(nameof(defaultTarget), ReturnType, MemberData.None).SetName("Default");
			for(int i = 0; i < datas.Count; i++) {
				var index = i;
				datas[i].port = ValueInput(datas[index].id, ReturnType).SetName(i.ToString());
			}
		}

		public override System.Type ReturnType() {
			if(targetType.isFilled) {
				try {
					return targetType.type;
				}
				catch { }
			}
			return typeof(object);
		}

		public override object GetValue(Flow flow) {
			if(!target.isAssigned)
				throw new Exception("target is unassigned");
			object val = target.GetValue(flow);
			if(object.ReferenceEquals(val, null))
				throw new Exception("value is null");
			for(int i = 0; i < datas.Count; i++) {
				if(!datas[i].port.isAssigned)
					continue;
				object mVal = datas[i].value.Get(flow);
				if(mVal.Equals(val)) {
					return datas[i].port.GetValue(flow);
				}
			}
			return defaultTarget.GetValue(flow);
		}

		protected override string GenerateValueCode() {
			if(!target.isAssigned)
				throw new Exception("target is unassigned");
			var vName = CG.GenerateNewName("sVal");
			string result = null;
			for(int i = 0; i < datas.Count; i++) {
				var val = datas[i];
				if(val.port.isAssigned) {
					if(result == null) {
						result += CG.And(CG.Value(target) + " is var " + vName, CG.Compare(vName, CG.Value(val.value)));
					}
					else {
						result += CG.Compare(vName, CG.Value(val.value));
					}
					result += " ? " + CG.Value(val.port) + " : ";
				}
			}
			if(defaultTarget.isAssigned) {
				result += CG.Value(defaultTarget);
			}
			else {
				result += CG.Value(ReflectionUtils.CreateInstance(targetType.type));
			}
			return result.Wrap();
		}

		public override string GetTitle() {
			return "Select";
		}

		public override void CheckError(ErrorAnalyzer analizer) {
			analizer.CheckPort(target);
			analizer.CheckValue(targetType, nameof(targetType), this);
		}
	}
}