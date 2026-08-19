using UnityEngine;
using UnityEngine.EventSystems;

namespace Rocket.Multiplayer
{
    public class TouchLookArea : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private SharedPlayerInput targetInput;
        [SerializeField] private float sensitivity = 0.15f;
        [SerializeField] private bool invertY = true;

        private int activePointerId = int.MinValue;

        public void SetTargetInput(SharedPlayerInput inputTarget)
        {
            targetInput = inputTarget;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (activePointerId == int.MinValue)
            {
                activePointerId = eventData.pointerId;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId != activePointerId)
            {
                return;
            }

            Vector2 delta = eventData.delta * sensitivity;
            if (invertY)
            {
                delta.y = -delta.y;
            }

            targetInput?.AddMobileLook(delta);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId == activePointerId)
            {
                activePointerId = int.MinValue;
            }
        }
    }
}
