using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

namespace MaxyGames.UNode {
	[CreateAssetMenu(fileName = "Global Event", menuName = "uNode/Global Event/New ( String )")]
	public class UGlobalEventString : UGlobalEvent, IGlobalEvent<string> {
		public event Action<string> Event;

		public void AddListener(Action<string> action) {
			Event += action;
		}

		public void RemoveListener(Action<string> action) {
			Event -= action;
		}

		public override void ClearListener() {
			Event = null;
		}

		public void Trigger(string value) {
			Event?.Invoke(value);
		}
	}
}