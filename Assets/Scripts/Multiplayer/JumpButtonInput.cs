using UnityEngine;
using UnityEngine.EventSystems;

namespace Rocket.Multiplayer
{
    public class JumpButtonInput : MonoBehaviour, IPointerDownHandler
    {
        [SerializeField] private SharedPlayerInput targetInput;

        public void SetTargetInput(SharedPlayerInput inputTarget)
        {
            targetInput = inputTarget;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            targetInput?.QueueMobileJump();
        }
    }
}
