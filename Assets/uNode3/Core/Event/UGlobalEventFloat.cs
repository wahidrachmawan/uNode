using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

namespace MaxyGames.UNode {
	[CreateAssetMenu(fileName = "Global Event", menuName = "uNode/Global Event/New ( Float )")]
	public class UGlobalEventFloat : UGlobalEvent, IGlobalEvent<float> {
		public event Action<float> Event;

		public void AddListener(Action<float> action) {
			Event += action;
		}

		public void RemoveListener(Action<float> action) {
			Event -= action;
		}

		public override void ClearListener() {
			Event = null;
		}

		public void Trigger(float value) {
			Event?.Invoke(value);
		}
	}
}