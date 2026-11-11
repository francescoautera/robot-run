
using TMPro;
using UnityEngine;

namespace RobotRun
{
    internal sealed class InstructionCardView
    {
        internal const float PreferredWidth = 132f;
        internal const float AspectRatio = 0.80f;

        private static readonly Color RightSurface = new(0.065f, 0.16f, 0.22f);
        private static readonly Color LeftSurface = new(0.24f, 0.125f, 0.12f);
        private static readonly Color JumpSurface = new(0.16f, 0.12f, 0.25f);
        private static readonly Color JumpAccent = new(0.77f, 0.63f, 0.93f);
        private static readonly Color LeftAccent = new(1f, 0.73f, 0.55f);

        private readonly UnityEngine.UI.Image background;
        private readonly UnityEngine.UI.Outline outline;
        private readonly CanvasGroup opacity;
        private readonly TextMeshProUGUI title;
        private readonly TextMeshProUGUI description;
        private readonly TextMeshProUGUI direction;
        private readonly UnityEngine.UI.RawImage illustration;
        private Canvas hoverCanvas;
        private Color restingOutline;
        private float restingOutlineWidth;
        private bool hovered;
        private float hoverScale = 1f;
        private float tutorialScale = 1f;

        internal RectTransform Rect { get; }
        internal RectTransform Face => background.rectTransform;

        internal InstructionCardView(RectTransform rect)
        {
            Rect = rect;
            background = GameCanvasView.Box(Rect, "Card face", RightSurface, 0, 0, 1, 1, false);
            var face = background.rectTransform;
            MockupTheme.Round(background);
            outline = background.gameObject.AddComponent<UnityEngine.UI.Outline>();
            outline.effectDistance = new Vector2(1, -1);
            outline.useGraphicAlpha = false;
            opacity = Rect.gameObject.AddComponent<CanvasGroup>();

            title = GameCanvasView.Label(face, "Title", "", 19, MockupTheme.Ink, TextAlignmentOptions.MidlineLeft,
                0.09f, 0.78f, 0.91f, 0.96f);
            title.fontStyle = FontStyles.Bold;
            var artRegion = GameCanvasView.Region(face, "Art region", 0.065f, 0.35f, 0.935f, 0.77f);
            var picture = GameCanvasView.Region(artRegion, "Instruction illustration", 0, 0, 1, 1);
            illustration = picture.gameObject.AddComponent<UnityEngine.UI.RawImage>();
            illustration.raycastTarget = false;
            var artAspect = picture.gameObject.AddComponent<UnityEngine.UI.AspectRatioFitter>();
            artAspect.aspectMode = UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent;
            artAspect.aspectRatio = 432f / 256f;
            description = GameCanvasView.Label(face, "Description", "", 15, MockupTheme.Ink, TextAlignmentOptions.MidlineLeft,
                0.09f, 0.16f, 0.93f, 0.34f);
            description.fontStyle = FontStyles.Bold;
            direction = GameCanvasView.Label(face, "Direction", "", 13, MockupTheme.Muted, TextAlignmentOptions.MidlineLeft,
                0.09f, 0.04f, 0.93f, 0.17f);
        }

        internal void Show(CardInstruction instruction, Color? highlight = null,
            bool dimmed = false)
        {
            bool right = instruction.Direction > 0;
            bool move = instruction.Type == CardType.Move;
            Color accent = move ? (right ? MockupTheme.Cyan : LeftAccent) : JumpAccent;
            background.color = move ? (right ? RightSurface : LeftSurface) : JumpSurface;
            outline.enabled = true;
            restingOutline = highlight ?? Color.Lerp(background.color, accent, 0.54f);
            restingOutlineWidth = highlight.HasValue ? 2f : 1f;
            UpdateOutline();
            opacity.alpha = dimmed ? 0.51f : 1f;

            title.text = $"{(move ? "MOVE" : "JUMP")} {instruction.Value}";
            illustration.texture = MockupTheme.CardArt(instruction);

            int cells = instruction.Value;
            string unit = cells == 1 ? "cella" : "celle";
            description.text = move ? $"Avanza di {cells} {unit}" : $"Sale di {cells} {unit}";
            string side = right ? "destra" : "sinistra";
            direction.text = move ? $"verso {side}" : $"e va a {side} di 1";
            direction.color = right ? accent : LeftAccent;
        }

        internal void ShowMenuDirection(int side)
        {
            Show(new CardInstruction(0, CardType.Move, 3, side));
            title.text = side < 0 ? "VAI A SINISTRA" : "VAI A DESTRA";
            description.text = side < 0 ? "Raggiungi Esci" : "Raggiungi Inizia";
            direction.text = side < 0 ? "Porta a sinistra" : "Porta a destra";
        }
        internal void SetHandDrag(bool dragging)
        {
            opacity.alpha = dragging ? 0f : 1f;
            opacity.blocksRaycasts = !dragging;
        }

        internal void SetHover(bool active, float scaleIncrease)
        {
            hoverScale = active ? 1f + scaleIncrease : 1f;
            ApplyFaceScale();
            if (hovered == active)
                return;

            hovered = active;
            if (active && hoverCanvas == null)
            {
                // Lift the artwork without changing sibling order or the card's raycast area.
                var parentCanvas = Rect.GetComponentInParent<Canvas>();
                hoverCanvas = background.gameObject.AddComponent<Canvas>();
                hoverCanvas.additionalShaderChannels = parentCanvas.additionalShaderChannels;
            }

            hoverCanvas.overrideSorting = active;
            hoverCanvas.sortingOrder = active ? 1 : 0;
            UpdateOutline();
        }

        internal void SetTutorialScale(float scale)
        {
            tutorialScale = scale;
            ApplyFaceScale();
        }

        private void ApplyFaceScale()
        {
            float scale = hoverScale * tutorialScale;
            if (Face.localScale.x != scale)
                Face.localScale = Vector3.one * scale;
        }

        private void UpdateOutline()
        {
            outline.effectColor = hovered ? Color.Lerp(restingOutline, MockupTheme.Ink, 0.72f) : restingOutline;
            outline.effectDistance = Vector2.one * (hovered ? restingOutlineWidth + 1f : restingOutlineWidth);
        }

    }
}
