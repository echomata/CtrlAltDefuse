using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CtrlAltDefuse.Desktop
{
    public sealed class MonitorRaycaster : GraphicRaycaster
    {
        // The scene input module also raycasts. Only the virtual pointer drives this canvas.
        public sealed class MonitorPointer : PointerEventData
        {
            public MonitorPointer(EventSystem events) : base(events) {}
        }

        public override void Raycast(PointerEventData data, List<RaycastResult> results)
        {
            if (data is MonitorPointer) base.Raycast(data, results);
        }
    }
}
