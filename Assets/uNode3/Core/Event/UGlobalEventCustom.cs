using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

namespace MaxyGames.UNode {
	[CreateAssetMenu(fileName = "Global Event", menuName = "uNode/Global Event/New ( Custom Parameters )")]
	public class UGlobalEventCustom : UGlobalEvent, IGlobalEvent<object[]> {
		public event Action<object[]> Event;

		public List<ParameterData> parameters = new List<ParameterData>();

		int IGlobalEvent.ParameterCount => parameters.Count;
		string IGlobalEvent.GetParameterName(int index) => !string.IsNullOrEmpty(parameters[index].name) ? parameters[index].name : "value" + (index + 1);
		Type IGlobalEvent.GetParameterType(int index) => parameters[index].Type;

		public void AddListener(Action<object[]> action) {
			Event += action;
		}

		public void RemoveListener(Action<object[]> action) {
			Event += action;
		}

		public override void ClearListener() {
			Event = null;
		}

		public void Trigger(object[] value) {
			Event?.Invoke(value);
		}

		public override CG.MData GenerateMethodCode(out string[] parameterNames, out string actionCode) {
			var evt = this as IGlobalEvent;
			var count = evt.ParameterCount;
			var mData = CG.generatorData.AddNewGeneratedMethod(CG.GenerateNewName(EventName), typeof(void), new Type[] { typeof(object[]) });
			var names = new string[count];
			var declaredParameters = new string[count];
			for(int i = 0; i < count; i++) {
				names[i] = CG.GenerateNewName("parameterValue");
				declaredParameters[i] = CG.DeclareVariable(names[i], mData.parameters[0].name.CGAccessElement(i.CGValue()).CGConvert(parameters[i].Type));
			}
			parameterNames = names;
			if(count > 0) {
				mData.AddCode(CG.Flow(declaredParameters), int.MinValue);
			}
			actionCode = CG.NewGeneric(typeof(Action), new[] { typeof(object[]).CGType() }, new string[] { mData.name }, null);
			return mData;
		}
	}
}