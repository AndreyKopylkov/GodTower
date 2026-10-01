using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GodTower.UI
{
    /// <summary>Tells whether a screen point is over a raycast-target UI element (buttons, panels), so gameplay input can ignore it.</summary>
    public sealed class UiPointerFilter
    {
        private readonly List<RaycastResult> _results = new();
        private PointerEventData _pointer;
        private EventSystem _eventSystem;

        public bool IsOverUi(Vector2 screenPosition)
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
                return false;

            if (_pointer == null || _eventSystem != eventSystem)
            {
                _eventSystem = eventSystem;
                _pointer = new PointerEventData(eventSystem);
            }

            _pointer.position = screenPosition;
            _results.Clear();
            eventSystem.RaycastAll(_pointer, _results);
            return _results.Count > 0;
        }
    }
}
