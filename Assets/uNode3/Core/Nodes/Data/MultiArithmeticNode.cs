using UnityEngine;
using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

namespace MaxyGames.UNode.Nodes {
	// [NodeMenu("Operator", "Arithmetic {+} {-} {/} {*} {%} {^}")]
	public class MultiArithmeticNode : ValueNode {
		public class PortData {
			public string id = uNodeUtility.GenerateUID();
			public SerializedType type = typeof(float);

			[NonSerialized]
			public ValueInput port;
		}

		public ArithmeticType operatorKind = ArithmeticType.Add;
		public List<PortData> inputs = new List<PortData>() { new PortData(), new PortData() };

		protected override void OnRegister() {
			base.OnRegister();
			while(inputs.Count < 2) {
				inputs.Add(new PortData());
			}
			for(int i = 0; i < inputs.Count; i++) {
				int index = i;
				inputs[i].port = ValueInput(inputs[i].id, () => inputs[index].type.type).SetName("input " + (i + 1));
			}
		}

		public override System.Type ReturnType() {
			try {
				bool isDivide = operatorKind == ArithmeticType.Divide || operatorKind == ArithmeticType.Modulo;
				object obj = ReflectionUtils.CreateInstance(inputs[0].type.type);
				if(isDivide) {
					//For fix zero divide error.
					obj = Operator.IncrementPrimitive(obj);
				}
				for(int i = 1; i < inputs.Count; i++) {
					object obj2 = ReflectionUtils.CreateInstance(inputs[i].type.type);
					if(isDivide) {
						//For fix zero divide error.
						obj2 = Operator.IncrementPrimitive(obj2);
					}
					obj = uNodeHelper.ArithmeticOperator(obj, obj2, operatorKind);
				}
				if(!object.ReferenceEquals(obj, null)) {
					return obj.GetType();
				}
			}
			catch { }
			return typeof(object);
		}

		public override object GetValue(Flow flow) {
			object obj = inputs[0].port.GetValue(flow, inputs[0].type.type);
			for(int i = 1; i < inputs.Count; i++) {
				obj = uNodeHelper.ArithmeticOperator(obj, inputs[i].port.GetValue(flow, inputs[i].type.type), operatorKind);
			}
			if(!object.ReferenceEquals(obj, null)) {
				return obj;
			}
			throw null;
		}

		protected override string GenerateValueCode() {
			string contents = null;
			for(int i = 0; i < inputs.Count; i++) {
				string val;
				if(inputs[i].port.ValueType == inputs[i].port.type || inputs[i].port.type.IsSubclassOf(inputs[i].port.ValueType)) {
					val = inputs[i].port.CGValue();
				}
				else {
					val = inputs[i].port.CGValue().CGConvert(inputs[i].port.type);
				}
				if(i == 0) {
					contents = val;
				}
				else {
					contents = CG.Arithmetic(contents, val, operatorKind).Wrap();
				}
			}
			return contents;
		}

		public override string GetTitle() {
			return operatorKind.ToString();
		}

		public override string GetRichName() {
			string separator = null;
			switch(operatorKind) {
				case ArithmeticType.Add:
					separator = " + ";
					break;
				case ArithmeticType.Divide:
					separator = " / ";
					break;
				case ArithmeticType.Modulo:
					separator = " % ";
					break;
				case ArithmeticType.Multiply:
					separator = " * ";
					break;
				case ArithmeticType.Subtract:
					separator = " - ";
					break;
			}
			return string.Join(separator, from input in inputs select input.port.GetRichName());
		}

		public override System.Type GetNodeIcon() {
			switch(operatorKind) {
				case ArithmeticType.Add:
					return typeof(TypeIcons.AddIcon);
				case ArithmeticType.Divide:
					return typeof(TypeIcons.DivideIcon);
				case ArithmeticType.Subtract:
					return typeof(TypeIcons.SubtractIcon);
				case ArithmeticType.Multiply:
					return typeof(TypeIcons.MultiplyIcon);
				case ArithmeticType.Modulo:
					return typeof(TypeIcons.ModuloIcon);
			}
			return typeof(TypeIcons.CalculatorIcon);
		}

		public override void CheckError(ErrorAnalyzer analizer) {
			if(!analizer.CheckPort(inputs.Select(p => p.port))) {
				try {
					bool isDivide = operatorKind == ArithmeticType.Divide || operatorKind == ArithmeticType.Modulo;
					object obj = ReflectionUtils.CreateInstance(inputs[0].type);
					//if(isDivide) {
					//	//For fix zero divide error.
					//	obj = Operator.IncrementPrimitive(obj);
					//}
					for(int i = 1; i < inputs.Count; i++) {
						object obj2 = ReflectionUtils.CreateInstance(inputs[i].type);
						if(isDivide) {
							//For fix zero divide error.
							obj2 = Operator.IncrementPrimitive(obj2);
						}
						obj = uNodeHelper.ArithmeticOperator(obj, obj2, operatorKind);
					}
				}
				catch(System.Exception ex) {
					analizer.RegisterError(this, ex.Message);
				}
			}
		}
	}
}