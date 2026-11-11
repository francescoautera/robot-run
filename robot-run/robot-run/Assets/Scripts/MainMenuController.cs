using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace RobotRun
{
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField, Min(0f), Tooltip("Incremento di scala al passaggio del mouse. 0.1 equivale al 10%.")]
        private float cardHoverScaleIncrease = 0.1f;

        internal float CardHoverScaleIncrease => Mathf.Max(0f, cardHoverScaleIncrease);
        internal bool CardsInteractable => !running;
        private RectTransform canvas;
        private RectTransform board;
        private RoomView room;
        private TextMeshProUGUI message;
        private TextMeshProUGUI slotHint;
        private UnityEngine.UI.Button play;
        private MenuDirectionCard leftCard;
        private MenuDirectionCard rightCard;
        private MenuDirectionCard selected;
        private bool running;

        private void Awake()
        {
            canvas = new GameObject("Menu Canvas", typeof(RectTransform), typeof(Canvas),
                typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster)).GetComponent<RectTransform>();
            canvas.SetParent(transform, false);
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            scaler.matchWidthOrHeight = 0.5f;
            if (EventSystem.current == null)
                new GameObject("Menu EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule)).transform.SetParent(transform, false);

            room = gameObject.AddComponent<RoomView>();
            room.BuildMenuPreview();
            GameCanvasView.Box(canvas, "Upper wall", MockupTheme.Navy, 0, 0.64f, 1, 1, false);
            GameCanvasView.Box(canvas, "Lower wall", MockupTheme.Navy, 0, 0, 1, 0.26f, false);
            GameCanvasView.Box(canvas, "Left wall", MockupTheme.Navy, 0, 0.26f, 0.125f, 0.64f, false);
            GameCanvasView.Box(canvas, "Right wall", MockupTheme.Navy, 0.875f, 0.26f, 1, 0.64f, false);
            for (int i = 0; i < 10; i++)
            {
                float x = i / 10f;
                GameCanvasView.Box(canvas, "Wall panel", new Color(0.07f, 0.12f, 0.17f),
                    x, 0.64f, x + 0.002f, 1, false);
            }
            GameCanvasView.Box(canvas, "Upper beam", MockupTheme.Panel, 0, 0.88f, 1, 0.91f, false);
            GameCanvasView.Box(canvas, "Left column", MockupTheme.Panel, 0.055f, 0, 0.09f, 1, false);
            GameCanvasView.Box(canvas, "Right column", MockupTheme.Panel, 0.91f, 0, 0.945f, 1, false);
            Text(canvas, "Tagline", "UN PICCOLO PROGRAMMA. UNA GRANDE FUGA.", 14, MockupTheme.Cyan,
                0.2f, 0.93f, 0.8f, 0.97f);
            Text(canvas, "Title", "ROBOT RUN", 104, MockupTheme.Ink, 0.15f, 0.78f, 0.85f, 0.93f).fontStyle = FontStyles.Bold;
            message = Text(canvas, "Instructions", "Scegli una direzione e accompagna il robot alla porta.", 23,
                MockupTheme.Muted, 0.16f, 0.70f, 0.84f, 0.78f);

            Text(canvas, "Hand label", "LA TUA MANO", 15, MockupTheme.Muted, 0.13f, 0.21f, 0.37f, 0.25f);
            board = GameCanvasView.Box(canvas, "Direction board", MockupTheme.Panel,
                0.435f, 0.025f, 0.655f, 0.235f, true).rectTransform;
            MockupTheme.Round(board.GetComponent<UnityEngine.UI.Image>());
            Text(board, "Board title", "SCEGLI LA DIREZIONE", 15, MockupTheme.Muted, 0.05f, 0.84f, 0.95f, 0.99f);
            slotHint = Text(board, "Empty board", "+\nTrascina una carta", 22, MockupTheme.Muted,
                0.1f, 0.12f, 0.9f, 0.82f);

            slotHint.textWrappingMode = TextWrappingModes.Normal;
            leftCard = CreateCard(-1);
            rightCard = CreateCard(1);

            var playFace = GameCanvasView.Box(canvas, "Play", MockupTheme.Mint,
                0.85f, 0.025f, 0.955f, 0.095f, true);
            MockupTheme.Round(playFace);
            play = playFace.gameObject.AddComponent<UnityEngine.UI.Button>();
            play.targetGraphic = playFace;
            play.onClick.AddListener(PlayChoice);
            Text(playFace.rectTransform, "Play text", "▶  Play", 23, MockupTheme.Navy, 0, 0, 1, 1);
            Refresh();
        }

        private static TextMeshProUGUI Text(RectTransform parent, string name, string caption, int size,
            Color color, float x0, float y0, float x1, float y1) =>
            GameCanvasView.Label(parent, name, caption, size, color, TextAlignmentOptions.Center, x0, y0, x1, y1);

        private MenuDirectionCard CreateCard(int direction)
        {
            var hitArea = GameCanvasView.Box(canvas, direction < 0 ? "Left card" : "Right card",
                Color.clear, 0, 0, 0, 0, true);
            hitArea.canvasRenderer.cullTransparentMesh = false;
            hitArea.rectTransform.sizeDelta = new Vector2(InstructionCardView.PreferredWidth,
                InstructionCardView.PreferredWidth / InstructionCardView.AspectRatio);
            var view = new InstructionCardView(hitArea.rectTransform);
            view.ShowMenuDirection(direction);
            var card = hitArea.gameObject.AddComponent<MenuDirectionCard>();
            card.Menu = this;
            card.Direction = direction;
            card.Card = view;
            return card;
        }

        internal bool BeginCardDrag(MenuDirectionCard card)
        {
            if (running) return false;
            if (selected == card) selected = null;
            Refresh();
            card.transform.SetAsLastSibling();
            return true;
        }

        internal void MoveCard(MenuDirectionCard card, PointerEventData pointer)
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas, pointer.position,
                pointer.pressEventCamera, out var point))
                ((RectTransform)card.transform).localPosition = point;
        }

        internal void EndCardDrag(MenuDirectionCard card, PointerEventData pointer)
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(board, pointer.position, pointer.pressEventCamera))
                selected = card;
            Refresh();
        }

        private void Refresh()
        {
            PlaceCard(leftCard);
            PlaceCard(rightCard);
            room.SetMenuDirection(selected == null ? 0 : selected.Direction);
            slotHint.gameObject.SetActive(selected == null);
            play.interactable = selected != null && !running;
            play.targetGraphic.color = play.interactable ? MockupTheme.Mint : MockupTheme.Border;
            message.text = selected == null ? "Scegli una direzione e accompagna il robot alla porta." :
                selected.Direction > 0 ? "La porta Inizia è pronta. Premi Play." : "La porta Esci è pronta. Premi Play.";
        }

        private void PlaceCard(MenuDirectionCard card)
        {
            var rect = (RectTransform)card.transform;
            var anchor = selected == card ? new Vector2(0.545f, 0.11f) :
                new Vector2(card.Direction < 0 ? 0.19f : 0.305f, 0.12f);
            rect.anchorMin = rect.anchorMax = anchor;
            rect.anchoredPosition = Vector2.zero;
            rect.localRotation = Quaternion.identity;
        }

        private void PlayChoice()
        {
            if (selected == null || running) return;
            running = true;
            play.interactable = false;
            StartCoroutine(WalkToDoor(selected.Direction));
        }

        private IEnumerator WalkToDoor(int direction)
        {
            message.text = direction > 0 ? "Si parte!" : "Alla prossima!";
            var cell = new Vector2Int(6, 1);
            room.StartRun(cell);
            for (int step = 0; step < 4; step++)
            {
                var next = cell + Vector2Int.right * direction;
                var motion = new SimulationEvent(MotionKind.Move, cell, next);
                yield return room.PlayEvent(motion, () => 0.75f, false);
                room.RecordMovement(motion);
                room.PlayMotionSound(motion);
                cell = next;
            }
            room.CompleteMenuRun(cell);
            yield return new WaitForSecondsRealtime(0.35f);
            if (direction > 0)
                SceneManager.LoadScene("Tutorial01_MoveRight");
            else
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
        }
    }
}