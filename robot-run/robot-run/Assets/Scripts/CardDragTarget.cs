
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace RobotRun
{
    public sealed class CardDragTarget : MonoBehaviour, IBeginDragHandler, IDragHandler,
        IEndDragHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public GameCanvasView View { get; set; }
        public bool IsSlot { get; set; }
        public int Index { get; set; }
        internal InstructionCardView Card { get; set; }

        public void OnPointerEnter(PointerEventData pointer)
        {
            if (pointer is ExtendedPointerEventData input && input.device is Mouse)
                View.SetPointerCard(this, true);
        }

        public void OnPointerExit(PointerEventData pointer)
        {
            if (pointer is ExtendedPointerEventData input && input.device is Mouse)
                View.SetPointerCard(this, false);
        }

        public void OnBeginDrag(PointerEventData pointer)
        {
            if (pointer.button == PointerEventData.InputButton.Left)
                View.BeginDrag(this, pointer);
        }

        public void OnDrag(PointerEventData pointer) => View.UpdateDrag(this, pointer);

        public void OnEndDrag(PointerEventData pointer) => View.EndDrag(this, pointer);

        public void OnPointerClick(PointerEventData pointer)
        {
            if (IsSlot && pointer.button == PointerEventData.InputButton.Right)
                View.Flip(Index);
        }
    }
}
