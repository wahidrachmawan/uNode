using System;
using UnityEngine;
using System.Collections.Generic;

namespace MaxyGames.UNode.Nodes {
    [EventMenu("Behavior", "On Destroy")]
    [StateEvent]
    public class OnDestroyEvent : BaseComponentEvent {

        public override void GenerateEventCode() {
            DoGenerateCode(UEventID.OnDestroy);
        }
    }
}