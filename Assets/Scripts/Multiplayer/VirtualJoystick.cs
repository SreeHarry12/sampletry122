using UnityEngine;
using UnityEngine.EventSystems;

namespace Rocket.Multiplayer
{
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private RectTransform handle;
        [SerializeField] private float radius = 90f;
        [SerializeField] private float deadZone = 0.1f;
        [SerializeField] private SharedPlayerInput targetInput;

        public Vector2 Input { get; private set; }

        private RectTransform rectTransform;

        private void Awake()
        {
            rectTransform = transform as RectTransform;
        }

        public void SetTargetInput(SharedPlayerInput inputTarget)
        {
            targetInput = inputTarget;
            targetInput?.SetMobileMove(Input);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            UpdateInput(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            UpdateInput(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Input = Vector2.zero;

            if (handle != null)
            {
                handle.anchoredPosition = Vector2.zero;
            }

            targetInput?.SetMobileMove(Vector2.zero);
        }

        private void UpdateInput(PointerEventData eventData)
        {
            if (rectTransform == null)
            {
                return;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
            {
                return;
            }

            Vector2 normalized = localPoint / Mathf.Max(radius, 0.01f);
            normalized = Vector2.ClampMagnitude(normalized, 1f);

            if (normalized.magnitude < deadZone)
            {
                normalized = Vector2.zero;
            }

            Input = normalized;

            if (handle != null)
            {
                handle.anchoredPosition = normalized * radius;
            }

            targetInput?.SetMobileMove(Input);
        }
    }
}
