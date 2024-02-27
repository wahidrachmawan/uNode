using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

namespace MaxyGames.UNode {
	[CreateAssetMenu(fileName = "Global Event", menuName = "uNode/Global Event/New ( Integer )")]
	public class UGlobalEventInteger : UGlobalEvent, IGlobalEvent<int> {
		public event Action<int> Event;

		public void AddListener(Action<int> action) {
			Event += action;
		}

		public void RemoveListener(Action<int> action) {
			Event -= action;
		}

		public override void ClearListener() {
			Event = null;
		}

		public void Trigger(int value) {
			Event?.Invoke(value);
		}
	}
}