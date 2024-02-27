using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

namespace MaxyGames.UNode {
	[CreateAssetMenu(fileName = "Global Event", menuName ="uNode/Global Event/New ( No Parameters )")]
	public class UGlobalEventAction : UGlobalEvent, IGlobalEventNoParameter {
		public event Action Event;

		public void AddListener(Action action) {
			Event += action;
		}

		public void RemoveListener(Action action) {
			Event -= action;
		}

		public override void ClearListener() {
			Event = null;
		}

		public void Trigger() {
			Event?.Invoke();
		}
	}
}