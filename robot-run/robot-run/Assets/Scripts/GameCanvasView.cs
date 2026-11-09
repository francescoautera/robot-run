
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace RobotRun
{
    public sealed class GameCanvasView : MonoBehaviour
    {
        internal const float HeaderStart = 0.94f;
        internal const float ConsoleHeight = 0.27f;

        private static readonly Color Tile = new(0.10f, 0.16f, 0.23f);
        private static readonly Color Ink = MockupTheme.Ink;
        private static readonly Color Muted = MockupTheme.Muted;
        private static readonly Color TutorialGold = new(1f, 0.79f, 0.47f);

        private GameSessionController session;
        private LevelAsset level;
        private ProgramDraft draft;
        private RectTransform root;
        private RectTransform programZone;
        private RectTransform programRow;
        private Vector2 programSize;
        private RectTransform insertionGuide;
        private RectTransform inventoryZone;
        private RectTransform inventoryRow;
        private Vector2 handSize;
        private readonly List<InstructionCardView> visibleHand = new();
        private InstructionCardView dragGhost;
        private TextMeshProUGUI sequenceCount;
        private RectTransform tutorialBanner;
        private TextMeshProUGUI tutorialStep;
        private TextMeshProUGUI tutorialHeading;
        private TextMeshProUGUI tutorialAction;
        private TextMeshProUGUI tutorialDetail;
        private UnityEngine.UI.Outline tutorialBoardOutline;
        private UnityEngine.UI.Outline tutorialPlayOutline;
        private TutorialGlow[] tutorialPlacement;
        private TutorialGlow[] tutorialRotation;
        private TutorialGlow[] tutorialStart;
        private TutorialGlow[] activeTutorialGlows;
        private float tutorialPulseStarted;
        private UnityEngine.UI.Image sequenceAccent;
        private RectTransform runSummary;
        private UnityEngine.UI.Image summaryAccent;
        private TextMeshProUGUI summaryHeading;
        private TextMeshProUGUI summaryTitle;
        private TextMeshProUGUI summaryDetail;
        private InstructionCardView[] slots;
        private InstructionCardView[][] inventory;
        private UnityEngine.UI.Button play;
        private UnityEngine.UI.Button skip;
        private UnityEngine.UI.Button speed;
        private UnityEngine.UI.Button reset;
        private UnityEngine.UI.Button nextLevel;
        private TextMeshProUGUI portalStatus;
        private RectTransform inspection;
        private TextMeshProUGUI inspectionText;
        private CardDragTarget dragSource;
        private CardDragTarget pointerCard;
        private InstructionCardView hoveredCard;
        private int dragPointer;
        private int hoverSlot = -1;
        private bool editing;
        private bool canContinue;

        public void Build(GameSessionController controller, LevelAsset asset, ProgramDraft program)
        {
            session = controller;
            level = asset;
            draft = program;

            root = new GameObject("Game Canvas", typeof(RectTransform), typeof(Canvas),
                typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster))
                .GetComponent<RectTransform>();
            root.SetParent(transform, false);
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            scaler.matchWidthOrHeight = 0.5f;

            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystem.transform.SetParent(transform, false);

            var levelBadge = PanelBox(root, "Level Badge", 0.017f, 0.946f, 0.177f, 0.979f);
            Label(levelBadge, "Level Title", asset.Title.ToUpperInvariant(), 15, Ink, TextAlignmentOptions.MidlineLeft,
                0.06f, 0, 0.94f, 1).characterSpacing = 2;
            if (!level.IsTutorial)
            {
                Button(root, "Other Level", "Cambia livello", 0.881f, 0.947f, 0.976f, 0.978f,
                    session.LoadOtherLevel, 14);
            }

            var console = Region(root, "Programming Console", 0, 0, 1, ConsoleHeight);
            programZone = PanelBox(console, "Sequence Board", 0.435f, 0.07f, 0.715f, 0.92f);
            sequenceAccent = Box(programZone, "Sequence accent", MockupTheme.Mint, 0.043f, 0.987f, 0.18f, 1, false);
            sequenceCount = Label(programZone, "Sequence count", "", 15, Muted, TextAlignmentOptions.MidlineRight,
                0.52f, 0.84f, 0.95f, 0.97f);
            programRow = Region(programZone, "Program Cards", 0.043f, 0.033f, 0.957f, 0.84f);
            slots = new InstructionCardView[draft.SlotCount];
            for (int index = 0; index < slots.Length; index++)
                slots[index] = BuildCard(programRow, $"Program card {index + 1}", index, isSlot: true);
            insertionGuide = Box(programZone, "Insertion guide", MockupTheme.Mint,
                0.5f, 0.10f, 0.5f, 0.79f, false).rectTransform;
            insertionGuide.sizeDelta = new Vector2(3f, 0f);
            insertionGuide.gameObject.SetActive(false);

            inventoryZone = Region(console, "Hand", 0.125f, 0.055f, 0.399f, 0.895f);
            var handLabel = Label(inventoryZone, "Hand label", "CARTE IN MANO", 15, Muted, TextAlignmentOptions.Center,
                0, 0.88f, 1, 1);
            handLabel.characterSpacing = 2;
            inventoryRow = Region(inventoryZone, "Available Cards", 0.02f, 0.05f, 0.98f, 0.86f);
            inventory = new InstructionCardView[level.Cards.Count][];
            for (int index = 0; index < inventory.Length; index++)
            {
                var spec = level.Cards[index];
                var instruction = new CardInstruction(index, spec.Type, spec.Value);
                var copies = inventory[index] = new InstructionCardView[spec.Quantity];
                for (int copy = 0; copy < copies.Length; copy++)
                {
                    copies[copy] = BuildCard(inventoryRow, $"Inventory {index + 1} copy {copy + 1}", index, isSlot: false);
                    copies[copy].Show(instruction, highlight: level.IsTutorial ? TutorialGold : null);
                }
            }

            runSummary = PanelBox(console, "Run Summary", 0.13f, 0.21f, 0.406f, 0.68f);
            summaryAccent = Box(runSummary, "Summary accent", MockupTheme.Cyan, 0, 0.12f, 0.007f, 0.88f, false);
            summaryHeading = Label(runSummary, "Run phase", "", 15, Ink, TextAlignmentOptions.MidlineLeft,
                0.05f, 0.69f, 0.96f, 0.91f);
            summaryHeading.characterSpacing = 2;
            summaryTitle = Label(runSummary, "Run headline", "", 24, Ink, TextAlignmentOptions.MidlineLeft,
                0.05f, 0.37f, 0.96f, 0.69f);
            summaryTitle.fontStyle = FontStyles.Bold;
            summaryDetail = Label(runSummary, "Run detail", "", 17, Muted, TextAlignmentOptions.MidlineLeft,
                0.05f, 0.12f, 0.96f, 0.36f);

            nextLevel = Button(console, "Next Level", session.NextLevelCaption, 0.797f, 0.40f, 0.96f, 0.66f,
                ContinueToNextLevel);
            SetPrimary(nextLevel, true);
            play = Button(console, "Play", "Play", 0.855f, 0.095f, 0.96f, 0.35f, session.Play,
                separateFace: level.IsTutorial, icon: ButtonIconKind.Play);
            speed = Button(console, "Speed", "×1", 0.735f, 0.095f, 0.788f, 0.35f, session.ToggleSpeed);
            skip = Button(console, "Skip", "Skip", 0.797f, 0.095f, 0.864f, 0.35f, session.Skip);
            reset = Button(console, "Reset", "Retry", 0.873f, 0.095f, 0.96f, 0.35f, session.ReturnToProgramming,
                icon: ButtonIconKind.Retry);
            if (asset.HasPortal)
            {
                var portalBadge = PanelBox(root, "Portal Badge", 0.413f, 0.947f, 0.587f, 0.978f);
                portalStatus = Label(portalBadge, "Portal Status", "", 15, Muted, TextAlignmentOptions.Center,
                    0, 0, 1, 1);
            }

            if (level.IsTutorial)
                BuildTutorialBanner();
            else
            {
                inspection = PanelBox(root, "Device inspection", 0.143f, 0.842f, 0.715f, 0.925f);
                inspectionText = Label(inspection, "Device details", "", 17, Ink, TextAlignmentOptions.MidlineLeft,
                    0.025f, 0.04f, 0.98f, 0.96f);
                inspectionText.textWrappingMode = TextWrappingModes.Normal;
                inspection.gameObject.SetActive(false);
            }

            dragGhost = new InstructionCardView(Region(root, "Dragged Card", 0.5f, 0.5f, 0.5f, 0.5f));
            dragGhost.Rect.pivot = new Vector2(0.5f, 0);
            dragGhost.Rect.gameObject.SetActive(false);
        }

        private void BuildTutorialBanner()
        {
            tutorialBanner = PanelBox(root, "Tutorial Banner", 0.144f, 0.732f, 0.425f, 0.887f);
            Box(tutorialBanner, "Guide accent", TutorialGold, 0.045f, 0.986f, 0.18f, 1f, false);
            var guide = Label(tutorialBanner, "Guide label", "GUIDA DEL ROBOT", 15, TutorialGold,
                TextAlignmentOptions.MidlineLeft, 0.045f, 0.74f, 0.64f, 0.94f);
            guide.characterSpacing = 2;
            tutorialStep = Label(tutorialBanner, "Guide step", "", 14, Muted, TextAlignmentOptions.MidlineRight,
                0.66f, 0.74f, 0.95f, 0.94f);
            tutorialHeading = Label(tutorialBanner, "Guide heading", "", 27, Ink, TextAlignmentOptions.MidlineLeft,
                0.045f, 0.50f, 0.95f, 0.76f);
            tutorialHeading.fontStyle = FontStyles.Bold;
            tutorialAction = Label(tutorialBanner, "Guide action", "", 20, Ink, TextAlignmentOptions.MidlineLeft,
                0.045f, 0.28f, 0.95f, 0.51f);
            tutorialAction.fontStyle = FontStyles.Bold;
            tutorialDetail = Label(tutorialBanner, "Guide explanation", "", 18, Muted, TextAlignmentOptions.MidlineLeft,
                0.045f, 0.07f, 0.95f, 0.29f);
            tutorialBoardOutline = programZone.GetComponent<UnityEngine.UI.Outline>();
            tutorialPlayOutline = play.targetGraphic.GetComponent<UnityEngine.UI.Outline>();

            var handCard = inventory[0][0];
            var programCard = slots[0];
            var playFace = play.targetGraphic.rectTransform;
            tutorialPlacement = new[]
            {
                new TutorialGlow(programZone, TutorialGold),
                new TutorialGlow(handCard.Face, TutorialGold, handCard.SetTutorialScale)
            };
            tutorialRotation = new[]
            {
                new TutorialGlow(programCard.Face, TutorialGold, programCard.SetTutorialScale)
            };
            tutorialStart = new[]
            {
                new TutorialGlow(playFace, TutorialGold, scale => playFace.localScale = Vector3.one * scale)
            };
        }

        private InstructionCardView BuildCard(RectTransform row, string name, int index, bool isSlot)
        {
            float anchorX = isSlot ? 0f : 0.5f;
            var holder = Region(row, name, anchorX, 0.5f, anchorX, 0.5f);
            var background = Box(holder, "Card", Color.clear, 0, 0, 1, 1, true);
            background.canvasRenderer.cullTransparentMesh = false;
            var target = background.gameObject.AddComponent<CardDragTarget>();
            target.View = this;
            target.IsSlot = isSlot;
            target.Index = index;
            target.Card = new InstructionCardView(background.rectTransform);
            return target.Card;
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (dragSource != null && keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                CancelDrag();

            if (canContinue && keyboard != null &&
                (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame))
            {
                var selected = EventSystem.current?.currentSelectedGameObject;
                if (selected == null || !selected.activeInHierarchy)
                {
                    ContinueToNextLevel();
                    return;
                }
            }

            if (editing && inventoryRow.rect.size != handSize)
                LayoutHand();
            if (programRow.rect.size != programSize)
                LayoutSequence();

            RefreshHover();
            UpdateTutorialGlow();
            if (inspection != null)
                UpdateInspection();
        }

        private void ContinueToNextLevel()
        {
            if (!canContinue)
                return;

            canContinue = false;
            session.LoadOtherLevel();
        }

        private void UpdateInspection()
        {
            Vector2? point = null;
            if (Mouse.current != null)
            {
                var cursor = Mouse.current.position.ReadValue();
                if (cursor.y > Screen.height * ConsoleHeight && cursor.y < Screen.height * HeaderStart)
                    point = cursor;
            }
            string details = session.Inspect(point);
            bool visible = !string.IsNullOrEmpty(details);
            if (inspection.gameObject.activeSelf != visible)
                inspection.gameObject.SetActive(visible);
            if (visible)
                inspectionText.text = details;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                pointerCard = null;
                RefreshHover();
            }
        }

        internal void SetPointerCard(CardDragTarget target, bool entered)
        {
            if (entered)
                pointerCard = target;
            else if (pointerCard == target)
                pointerCard = null;

            RefreshHover();
        }

        private void RefreshHover()
        {
            var nextHover = dragSource == null && pointerCard != null && pointerCard.isActiveAndEnabled
                ? pointerCard.Card : null;

            if (hoveredCard != nextHover)
            {
                hoveredCard?.SetHover(false, 0f);
                hoveredCard = nextHover;
            }

            hoveredCard?.SetHover(true, session.CardHoverScaleIncrease);
        }

        public void ShowProgramming()
        {
            editing = true;
            canContinue = false;
            CancelDrag();
            inventoryZone.gameObject.SetActive(true);
            runSummary.gameObject.SetActive(false);
            play.gameObject.SetActive(true);
            nextLevel.gameObject.SetActive(false);
            if (level.IsTutorial)
                tutorialBanner.gameObject.SetActive(true);
            reset.gameObject.SetActive(false);
            skip.gameObject.SetActive(false);
            speed.gameObject.SetActive(false);
            RenderCards();
        }

        public void ShowRun(RunState state, int speedLevel, bool showOutcome = true, SimulationEvent? movement = null)
        {
            editing = false;
            CancelDrag();
            inventoryZone.gameObject.SetActive(false);
            runSummary.gameObject.SetActive(true);
            var outcome = showOutcome ? state.Outcome : RunOutcome.Running;
            bool running = outcome == RunOutcome.Running;
            bool success = outcome == RunOutcome.Success;
            if (success && !canContinue)
                EventSystem.current?.SetSelectedGameObject(null);
            canContinue = success;
            play.gameObject.SetActive(false);
            nextLevel.gameObject.SetActive(success);
            if (level.IsTutorial)
            {
                tutorialBanner.gameObject.SetActive(false);
                SetTutorialOutline(tutorialBoardOutline, false);
                SetTutorialGlow(null);
            }
            reset.gameObject.SetActive(true);
            skip.gameObject.SetActive(running);
            speed.gameObject.SetActive(running);

            var motion = movement?.Kind;
            bool transferring = running && motion == MotionKind.Teleport;
            Color phaseColor = running ? (transferring ? MockupTheme.Portal : MockupTheme.Cyan)
                : success ? MockupTheme.Mint : MockupTheme.Coral;
            summaryAccent.color = phaseColor;
            summaryHeading.color = phaseColor;
            summaryHeading.text = running ? "IN SIMULAZIONE" : success ? "LIVELLO COMPLETATO" : "TENTATIVO FALLITO";
            var card = state.ActiveSlot >= 0 ? draft[state.ActiveSlot] : null;
            string cardName = card.HasValue ? $"{(card.Value.Type == CardType.Move ? "MOVE" : "JUMP")} {card.Value.Value}" : "";
            summaryTitle.text = outcome switch
            {
                RunOutcome.Success => "Robot in salvo",
                _ when !running => state.Cause ?? "Tentativo interrotto",
                _ when transferring => $"Trasferimento {session.PortalRouteFrom(movement.Value.From)}",
                _ when motion == MotionKind.Conveyor => $"Nastro attivo · {state.MoveRemaining} passi MOVE rimasti",
                _ when motion == MotionKind.Fall => "Caduta",
                _ when !card.HasValue => "Avvio sequenza",
                _ when state.MoveRemaining > 0 => $"{cardName} · {state.MoveRemaining} passi rimasti",
                _ => cardName
            };
            summaryDetail.text = success ? $"Uscita raggiunta al ciclo {state.Cycles:00}" : $"Ciclo {state.Cycles:00}";
            if (transferring)
                summaryDetail.text += " · Nessun ciclo aggiunto";
            if (!running && !success && state.ActiveSlot >= 0)
                summaryDetail.text = $"Carta {state.ActiveSlot + 1:00} · Passo {state.CardStep} · {summaryDetail.text}";
            if (state.Robots.Count > 1)
            {
                int exited = 0;
                foreach (var robot in state.Robots)
                    if (robot.Exited) exited++;
                summaryDetail.text += $" · Usciti {exited}/{state.Robots.Count}";
            }
            SetSpeed(speedLevel);
            RenderCards(state.ActiveSlot, outcome);
        }

        public void SetPortal(bool enabled)
        {
            if (portalStatus == null)
                return;

            portalStatus.text = enabled ? "PORTALE ATTIVO" : "PORTALE SPENTO";
            portalStatus.color = enabled ? MockupTheme.Portal : Muted;
        }

        public void SetSpeed(int speedLevel) =>
            speed.GetComponentInChildren<TextMeshProUGUI>(true).text = $"×{speedLevel}";

        private void RenderCards(int activeSlot = -1, RunOutcome outcome = RunOutcome.Running)
        {
            bool ready = session.CanPlay;
            if (editing)
            {
                play.interactable = ready;
                SetPrimary(play, ready);
                if (level.IsTutorial)
                    RenderTutorialBanner(ready);
            }

            sequenceCount.text = $"{draft.CardCount} / {draft.SlotCount} carte";
            sequenceAccent.color = editing || outcome == RunOutcome.Success ? MockupTheme.Mint
                : outcome == RunOutcome.Running ? MockupTheme.Cyan : MockupTheme.Coral;
            for (int index = 0; index < slots.Length; index++)
            {
                var card = draft[index];
                var tile = slots[index];
                tile.Rect.parent.gameObject.SetActive(card.HasValue);
                if (!card.HasValue)
                    continue;

                bool active = !editing && index == activeSlot && outcome == RunOutcome.Running;
                bool blocked = !editing && index == activeSlot && outcome == RunOutcome.Blocked;
                bool completed = !editing &&
                    (index < activeSlot || index == activeSlot &&
                        (outcome == RunOutcome.Success || outcome == RunOutcome.ProgramEnded));
                Color? highlight = blocked ? MockupTheme.Coral : active ? MockupTheme.Cyan
                    : index == hoverSlot ? MockupTheme.Mint : editing && level.IsTutorial && !ready ? TutorialGold : null;
                tile.Show(card.Value, highlight: highlight, dimmed: completed);
            }

            LayoutSequence();
            if (editing)
                RenderHand();

            RefreshHover();
        }

        private void RenderTutorialBanner(bool ready)
        {
            bool hasCard = draft.CardCount > 0;
            int stepCount = level.TutorialDirection < 0 ? 3 : 2;
            int step = ready ? stepCount : hasCard ? stepCount - 1 : 1;
            tutorialStep.text = $"PASSO {step} / {stepCount}";
            tutorialHeading.text = ready ? "Avvia il robot" : level.TutorialHeading;

            var spec = level.Cards[0];
            string cardName = $"{(spec.Type == CardType.Move ? "MOVE" : "JUMP")} {spec.Value}";
            string direction = level.TutorialDirection < 0 ? "sinistra" : "destra";
            tutorialAction.text = ready ? "Premi Play per avviare la simulazione."
                : hasCard ? $"Fai clic destro sulla carta per girarla a {direction}."
                : $"Trascina {cardName} sulla plancia.";
            tutorialDetail.text = ready ? "La luce azzurra indica il comando in corso." : level.Objective;
            SetTutorialOutline(tutorialBoardOutline, !hasCard);
            SetTutorialOutline(tutorialPlayOutline, ready);
            SetTutorialGlow(ready ? tutorialStart : hasCard ? tutorialRotation : tutorialPlacement);
        }

        private void SetTutorialGlow(TutorialGlow[] targets)
        {
            if (activeTutorialGlows == targets)
                return;

            if (activeTutorialGlows != null)
            {
                foreach (var glow in activeTutorialGlows)
                    glow.Clear();
            }

            activeTutorialGlows = targets;
            tutorialPulseStarted = Time.unscaledTime;
            UpdateTutorialGlow();
        }

        private void UpdateTutorialGlow()
        {
            if (activeTutorialGlows == null)
                return;

            float progress = Mathf.Clamp01(Mathf.Repeat(Time.unscaledTime - tutorialPulseStarted, 1.8f) / 1.2f);
            float strength = session.ReducedMotion ? 0f : 0.5f - 0.5f * Mathf.Cos(progress * 2f * Mathf.PI);
            foreach (var glow in activeTutorialGlows)
                glow.Apply(strength);
        }

        private static void SetTutorialOutline(UnityEngine.UI.Outline outline, bool highlighted)
        {
            outline.effectColor = highlighted ? TutorialGold : MockupTheme.Border;
            outline.effectDistance = highlighted ? new Vector2(2, -2) : new Vector2(1, -1);
        }

        private void RenderHand()
        {
            var draggedCard = dragSource != null ? dragSource.gameObject : null;
            visibleHand.Clear();
            for (int index = 0; index < inventory.Length; index++)
            {
                int available = draft.Available(index);
                var copies = inventory[index];
                for (int copy = 0; copy < copies.Length; copy++)
                {
                    var tile = copies[copy];
                    bool dragging = tile.Rect.gameObject == draggedCard;
                    // Keep the source active so uGUI can still deliver OnEndDrag.
                    tile.SetHandDrag(dragging);
                    tile.Rect.parent.gameObject.SetActive(copy < available || dragging);
                    if (copy < available && !dragging)
                        visibleHand.Add(tile);
                }
            }

            LayoutHand();
        }

        private void LayoutHand()
        {
            handSize = inventoryRow.rect.size;
            const float edgePadding = 12f;
            float usableWidth = Mathf.Max(0f, handSize.x - 2f * edgePadding);
            var cardSize = CardSize(usableWidth, handSize.y);
            int intervals = visibleHand.Count - 1;
            float step = CardStep(usableWidth, cardSize.x, visibleHand.Count);
            for (int index = 0; index < visibleHand.Count; index++)
            {
                var card = visibleHand[index].Rect;
                var holder = (RectTransform)card.parent;
                holder.sizeDelta = cardSize;
                holder.anchoredPosition = new Vector2((index - intervals / 2f) * step, 0f);
                float angle = intervals > 0 ? Mathf.Lerp(9f, -9f, index / (float)intervals) : 0f;
                card.localRotation = Quaternion.Euler(0, 0, angle);
            }
        }

        private void LayoutSequence()
        {
            programSize = programRow.rect.size;
            var cardSize = CardSize(programSize.x, programSize.y);
            int count = draft.CardCount;
            float step = CardStep(programSize.x, cardSize.x, count);
            for (int index = 0; index < count; index++)
            {
                var holder = (RectTransform)slots[index].Rect.parent;
                holder.sizeDelta = cardSize;
                holder.anchoredPosition = new Vector2(cardSize.x / 2f + index * step, 0f);
            }
        }

        private static Vector2 CardSize(float width, float height)
        {
            float cardWidth = Mathf.Min(InstructionCardView.PreferredWidth,
                Mathf.Min(width, height * InstructionCardView.AspectRatio));
            return new Vector2(cardWidth, cardWidth / InstructionCardView.AspectRatio);
        }

        private static float CardStep(float width, float cardWidth, int count) =>
            count > 1 ? Mathf.Min(cardWidth + 12f, (width - cardWidth) / (count - 1)) : 0f;

        public void Flip(int index)
        {
            if (!editing || dragSource != null)
                return;

            draft.Flip(index);
            RenderCards();
        }

        public void BeginDrag(CardDragTarget source, PointerEventData pointer)
        {
            if (!editing || dragSource != null)
                return;

            CardInstruction card;
            if (source.IsSlot)
            {
                var current = draft[source.Index];
                if (!current.HasValue)
                    return;

                card = current.Value;
            }
            else
            {
                if (draft.Available(source.Index) == 0)
                    return;

                var spec = level.Cards[source.Index];
                card = new CardInstruction(source.Index, spec.Type, spec.Value);
            }

            dragSource = source;
            dragPointer = pointer.pointerId;
            RefreshHover();
            dragGhost.Show(card);
            dragGhost.Rect.sizeDelta = ((RectTransform)source.transform).rect.size;
            dragGhost.Rect.gameObject.SetActive(true);
            if (!source.IsSlot)
                RenderHand();
            UpdateDrag(source, pointer);
        }

        public void UpdateDrag(CardDragTarget source, PointerEventData pointer)
        {
            if (!OwnsDrag(source, pointer))
                return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(root, pointer.position, null, out var local);
            var bounds = root.rect;
            var size = dragGhost.Rect.sizeDelta;
            float x = Mathf.Clamp(local.x, bounds.xMin + size.x / 2, bounds.xMax - size.x / 2);
            float y = local.y + 12f;
            if (y + size.y > bounds.yMax)
                y = local.y - size.y - 12f;
            dragGhost.Rect.anchoredPosition = new Vector2(x, Mathf.Max(bounds.yMin, y));
            var drop = SequenceDrop(pointer);
            ShowInsertionGuide(drop.replaceOrSwap ? -1 : drop.index);
            int highlighted = drop.replaceOrSwap ? drop.index : -1;
            if (highlighted == hoverSlot)
                return;

            hoverSlot = highlighted;
            RenderCards();
        }

        public void EndDrag(CardDragTarget source, PointerEventData pointer)
        {
            if (!OwnsDrag(source, pointer))
                return;

            var drop = SequenceDrop(pointer);
            if (drop.index >= 0)
            {
                if (!source.IsSlot)
                    draft.Place(source.Index, drop.index);
                else if (drop.replaceOrSwap)
                    draft.Swap(source.Index, drop.index);
                else
                    draft.Move(source.Index, drop.index);
            }
            else if (source.IsSlot)
            {
                draft.Remove(source.Index);
            }

            CancelDrag();
        }

        private bool OwnsDrag(CardDragTarget source, PointerEventData pointer) =>
            editing && dragSource == source && dragPointer == pointer.pointerId;

        private (int index, bool replaceOrSwap) SequenceDrop(PointerEventData pointer)
        {
            if (!RectTransformUtility.RectangleContainsScreenPoint(programZone, pointer.position))
                return (-1, false);

            if (!dragSource.IsSlot && draft.CardCount == draft.SlotCount)
                return (draft.SlotCount - 1, true);

            var hit = pointer.pointerCurrentRaycast.gameObject;
            var target = hit != null ? hit.GetComponentInParent<CardDragTarget>() : null;
            if (dragSource.IsSlot && target != null && target.IsSlot)
                return (target.Index, true);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(programZone, pointer.position, null, out var local);
            for (int index = 0; index < draft.CardCount; index++)
            {
                if (local.x < CardEdgeX(index, 0.5f))
                    return (index, false);
            }

            return (draft.CardCount, false);
        }

        private void ShowInsertionGuide(int index)
        {
            insertionGuide.gameObject.SetActive(index >= 0);
            if (index < 0)
                return;

            float x;
            if (draft.CardCount == 0)
                x = programZone.rect.xMin + programZone.rect.width * 0.043f;
            else if (index == 0)
                x = CardEdgeX(0, 0f) - 6f;
            else if (index == draft.CardCount)
                x = CardEdgeX(index - 1, 1f) + 6f;
            else
                x = (CardEdgeX(index - 1, 1f) + CardEdgeX(index, 0f)) / 2f;

            var bounds = programZone.rect;
            insertionGuide.anchoredPosition = new Vector2(Mathf.Clamp(x, bounds.xMin + 4f, bounds.xMax - 4f), 0f);
        }

        private float CardEdgeX(int index, float position)
        {
            var rect = slots[index].Rect;
            var edge = rect.TransformPoint(new Vector2(Mathf.Lerp(rect.rect.xMin, rect.rect.xMax, position), 0f));
            return programZone.InverseTransformPoint(edge).x;
        }

        private void CancelDrag()
        {
            if (dragSource == null)
                return;

            dragSource = null;
            hoverSlot = -1;
            ShowInsertionGuide(-1);
            dragGhost.Rect.gameObject.SetActive(false);
            if (editing)
                RenderCards();
            else
                RefreshHover();
        }

        private static UnityEngine.UI.Button Button(RectTransform parent, string name, string caption,
            float x0, float y0, float x1, float y1, UnityEngine.Events.UnityAction action, int fontSize = 20,
            bool separateFace = false, ButtonIconKind? icon = null)
        {
            var hitArea = Box(parent, name, separateFace ? Color.clear : Tile, x0, y0, x1, y1, true);
            var background = hitArea;
            if (separateFace)
            {
                hitArea.canvasRenderer.cullTransparentMesh = false;
                background = Box(hitArea.rectTransform, "Button face", Tile, 0, 0, 1, 1, false);
            }

            StylePanel(background);
            var button = hitArea.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = background;
            button.onClick.AddListener(action);
            if (icon.HasValue)
            {
                var iconRect = Region(background.rectTransform, "Button Icon", 0.20f, 0.23f, 0.36f, 0.77f);
                var graphic = iconRect.gameObject.AddComponent<ButtonIcon>();
                graphic.raycastTarget = false;
                graphic.color = Ink;
                graphic.SetKind(icon.Value);
            }

            Label(background.rectTransform, "Button Label", caption, fontSize, Ink,
                icon.HasValue ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.Center,
                icon.HasValue ? 0.41f : 0.06f, 0, 0.94f, 1).fontStyle = FontStyles.Bold;
            return button;
        }

        private static void SetPrimary(UnityEngine.UI.Button button, bool enabled)
        {
            button.targetGraphic.color = enabled ? MockupTheme.Mint : Tile;
            var foreground = enabled ? MockupTheme.Navy : Muted;
            button.GetComponentInChildren<TextMeshProUGUI>(true).color = foreground;
            var icon = button.GetComponentInChildren<ButtonIcon>(true);
            if (icon != null)
                icon.color = foreground;
        }

        private static RectTransform PanelBox(RectTransform parent, string name, float x0, float y0, float x1, float y1)
        {
            var panel = Box(parent, name, MockupTheme.Panel, x0, y0, x1, y1, false);
            StylePanel(panel);
            return panel.rectTransform;
        }

        private static void StylePanel(UnityEngine.UI.Image image)
        {
            MockupTheme.Round(image);
            var outline = image.gameObject.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = MockupTheme.Border;
            outline.effectDistance = new Vector2(1, -1);
            outline.useGraphicAlpha = false;
        }

        internal static UnityEngine.UI.Image Box(RectTransform parent, string name, Color color,
            float x0, float y0, float x1, float y1, bool raycast)
        {
            var box = Region(parent, name, x0, y0, x1, y1).gameObject.AddComponent<UnityEngine.UI.Image>();
            box.color = color;
            box.raycastTarget = raycast;
            return box;
        }

        internal static TextMeshProUGUI Label(RectTransform parent, string name, string value, int size,
            Color color, TextAlignmentOptions alignment, float x0, float y0, float x1, float y1)
        {
            var label = Region(parent, name, x0, y0, x1, y1).gameObject.AddComponent<TextMeshProUGUI>();
            label.text = value;
            label.color = color;
            label.fontSize = size;
            label.enableAutoSizing = true;
            label.fontSizeMin = size * 0.6f;
            label.fontSizeMax = size;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.alignment = alignment;
            label.raycastTarget = false;
            return label;
        }

        internal static RectTransform Region(RectTransform parent, string name, float x0, float y0, float x1, float y1)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(x0, y0);
            rect.anchorMax = new Vector2(x1, y1);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }
    }
}
