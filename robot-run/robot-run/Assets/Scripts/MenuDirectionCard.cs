using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace RobotRun
{
    public sealed class MenuDirectionCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler,
        IPointerEnterHandler, IPointerExitHandler
    {
        internal MainMenuController Menu;
        internal InstructionCardView Card;
        internal int Direction;
        private bool dragging;
        private bool pointerInside;

        private void Update()
        {
            Card.SetHover(pointerInside && !dragging && Menu.CardsInteractable, Menu.CardHoverScaleIncrease);
        }

        public void OnPointerEnter(PointerEventData pointer)
        {
            if (pointer is ExtendedPointerEventData input && input.device is Mouse)
                pointerInside = true;
        }

        public void OnPointerExit(PointerEventData pointer)
        {
            if (pointer is ExtendedPointerEventData input && input.device is Mouse)
                pointerInside = false;
        }

        public void OnBeginDrag(PointerEventData pointer)
        {
            if (pointer.button != PointerEventData.InputButton.Left) return;
            dragging = Menu.BeginCardDrag(this);
            if (dragging)
            {
                Card.SetHover(false, 0f);
                Menu.MoveCard(this, pointer);
            }
        }

        public void OnDrag(PointerEventData pointer)
        {
            if (dragging) Menu.MoveCard(this, pointer);
        }

        public void OnEndDrag(PointerEventData pointer)
        {
            if (!dragging) return;
            dragging = false;
            Menu.EndCardDrag(this, pointer);
        }

        private void OnApplicationFocus(bool focused)
        {
            if (focused) return;
            pointerInside = false;
            Card.SetHover(false, 0f);
            if (dragging)
            {
                dragging = false;
                Menu.EndCardDrag(this, new PointerEventData(EventSystem.current) { position = new Vector2(-1, -1) });
            }
        }
    }
}