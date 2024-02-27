using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

namespace MaxyGames.UNode {
	[CreateAssetMenu(fileName = "Global Event", menuName = "uNode/Global Event/New ( Bool )")]
	public class UGlobalEventBool : UGlobalEvent, IGlobalEvent<bool> {
		public event Action<bool> Event;

		public void AddListener(Action<bool> action) {
			Event += action;
		}

		public void RemoveListener(Action<bool> action) {
			Event -= action;
		}

		public override void ClearListener() {
			Event = null;
		}

		public void Trigger(bool value) {
			Event?.Invoke(value);
		}
	}
}