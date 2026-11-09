
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RobotRun
{
    public sealed partial class RoomView : MonoBehaviour
    {
        private Camera roomCamera;
        private Sprite pixel;
        private Transform robot;
        private Transform body;
        private SpriteRenderer robotImage;
        private Sprite neutralRobot;
        private Sprite errorRobot;
        private LevelDefinition level;
        private GameObject robotFocus;
        private GameObject errorFocus;
        private GameObject exitFocus;
        private GameObject exitArrow;
        private GameObject exitCheck;
        private Transform trail;
        private readonly List<SpriteRenderer> trailMarks = new();

        private static readonly Color Mint = new(0.34f, 0.98f, 0.78f);
        private static readonly Color Dark = new(0.065f, 0.105f, 0.16f);

        public void Build(LevelDefinition definition)
        {
            level = definition;
            pixel = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1),
                new Vector2(0.5f, 0.5f), 1f);

            var cameraObject = new GameObject("Room Camera", typeof(Camera));
            cameraObject.transform.SetParent(transform, false);
            roomCamera = cameraObject.GetComponent<Camera>();
            roomCamera.orthographic = true;
            roomCamera.clearFlags = CameraClearFlags.SolidColor;
            roomCamera.backgroundColor = new Color(0.032f, 0.055f, 0.09f);
            FrameRoom();

            BuildFactory();
            DrawGrid();
            for (int y = 0; y < level.Size.y; y++)
            {
                for (int x = 0; x < level.Size.x; x++)
                {
                    var cell = new Vector2Int(x, y);
                    if (level.IsSolid(cell))
                        DrawTile(cell);
                }
            }

            BuildExit();
            if (level.HasPortal)
                BuildPortals();

            BuildRobot();
            robotFocus = FocusFrame("Robot focus", MockupTheme.Cyan);
            errorFocus = FocusFrame("Blocked cell", MockupTheme.Coral);
            exitFocus = FocusFrame("Reached exit", MockupTheme.Mint);
            exitFocus.transform.localPosition = (Vector2)level.Exit;
            BuildMechanics();
            ResetRobot(level.Start);
        }

        private void BuildFactory()
        {
            float centerX = (level.Size.x - 1) / 2f;
            float centerY = (level.Size.y - 1) / 2f;
            var wall = new Color(0.041f, 0.075f, 0.118f);
            var seam = new Color(0.060f, 0.105f, 0.156f);
            var metal = new Color(0.085f, 0.15f, 0.21f);
            var edge = new Color(0.14f, 0.23f, 0.30f);
            for (int index = -3; index <= 3; index++)
            {
                float x = centerX + index * 2.55f;
                Block(transform, "Factory wall panel", new Vector2(x, centerY),
                    new Vector2(2.52f, 7.4f), wall, -20);
                Block(transform, "Panel seam", new Vector2(x, centerY - 0.57f),
                    new Vector2(2.43f, 0.025f), seam, -19);
                for (int vent = 0; vent < 3; vent++)
                    Block(transform, "Vent", new Vector2(x + 0.44f, centerY + 0.27f + vent * 0.11f),
                        new Vector2(0.69f, 0.042f), seam, -19);
            }

            float ceiling = level.Size.y - 0.74f;
            Block(transform, "Upper beam", new Vector2(centerX, ceiling), new Vector2(21f, 0.21f), metal, -16);
            for (int side = -1; side <= 1; side += 2)
            {
                float x = centerX + side * (level.Size.x / 2f + 0.82f);
                Block(transform, "Side column", new Vector2(x, centerY - 0.35f), new Vector2(0.62f, 8.4f), metal, -15);
                Block(transform, "Column edge", new Vector2(x - side * 0.28f, centerY), new Vector2(0.055f, 8.4f), edge, -14);
                for (int joint = 0; joint < 3; joint++)
                {
                    float y = -0.26f + joint * 2.28f;
                    Block(transform, "Column joint", new Vector2(x, y), new Vector2(0.88f, 0.34f), edge, -13);
                    Segment(transform, "Brace", new Vector2(x, y), new Vector2(x + side * 1.65f, y + 1.58f),
                        0.16f, metal, -17);
                    if (joint == 1)
                        Block(transform, "Amber indicator", new Vector2(x, y), new Vector2(0.15f, 0.055f),
                            new Color(0.95f, 0.60f, 0.26f), -12);
                }
            }
        }

        private void DrawGrid()
        {
            var grid = new Color(0.32f, 0.49f, 0.62f, 0.31f);
            float width = level.Size.x;
            float height = level.Size.y;
            for (int x = 0; x <= width; x++)
                Block(transform, "Grid column", new Vector2(x - 0.5f, (height - 1) / 2f), new Vector2(0.009f, height), grid, -4);
            for (int y = 0; y <= height; y++)
                Block(transform, "Grid row", new Vector2((width - 1) / 2f, y - 0.5f), new Vector2(width, 0.009f), grid, -4);
            for (int x = 0; x < 2; x++)
            {
                for (int y = 0; y < 2; y++)
                {
                    var corner = new Vector2(x * width - 0.5f, y * height - 0.5f);
                    var ink = new Color(0.55f, 0.73f, 0.83f);
                    Segment(transform, "Grid bracket", corner, corner + Vector2.right * (x == 0 ? 0.16f : -0.16f), 0.016f, ink, -3);
                    Segment(transform, "Grid bracket", corner, corner + Vector2.up * (y == 0 ? 0.16f : -0.16f), 0.016f, ink, -3);
                }
            }
        }

        private void DrawTile(Vector2 cell)
        {
            var metal = new Color(0.24f, 0.36f, 0.44f);
            var inset = new Color(0.13f, 0.21f, 0.28f);
            Block(transform, "Terrain frame", cell, Vector2.one * 0.99f, metal, 0);
            Block(transform, "Terrain inset", cell + Vector2.down * 0.025f, Vector2.one * 0.86f, inset, 1);
            Block(transform, "Terrain face", cell + Vector2.down * 0.025f, Vector2.one * 0.72f,
                new Color(0.17f, 0.27f, 0.34f), 2);
            Block(transform, "Tile bevel", cell + Vector2.up * 0.46f, new Vector2(0.94f, 0.045f),
                new Color(0.51f, 0.69f, 0.76f), 2);
            for (int side = -1; side <= 1; side += 2)
                Block(transform, "Tile rivet", cell + new Vector2(side * 0.34f, 0.37f),
                    new Vector2(0.053f, 0.015f), metal, 3);
        }

        private void BuildExit()
        {
            var position = (Vector2)level.Exit;
            Block(transform, "Exit frame", position + Vector2.down * 0.035f, new Vector2(0.73f, 0.93f), Mint, 4);
            Block(transform, "Exit inset", position + Vector2.down * 0.033f, new Vector2(0.67f, 0.86f),
                new Color(0.065f, 0.18f, 0.19f), 5);
            exitArrow = Layer("Exit arrow", position).gameObject;
            Segment(exitArrow.transform, "Shaft", new Vector2(-0.11f, 0), new Vector2(0.11f, 0), 0.024f, Mint, 6);
            Segment(exitArrow.transform, "Arrowhead", new Vector2(0.11f, 0), new Vector2(0.04f, 0.06f), 0.024f, Mint, 6);
            Segment(exitArrow.transform, "Arrowhead", new Vector2(0.11f, 0), new Vector2(0.04f, -0.06f), 0.024f, Mint, 6);
            exitCheck = Layer("Exit check", position).gameObject;
            Segment(exitCheck.transform, "Check", new Vector2(-0.08f, 0), new Vector2(-0.025f, -0.048f), 0.029f, Mint, 6);
            Segment(exitCheck.transform, "Check", new Vector2(-0.025f, -0.048f), new Vector2(0.10f, 0.08f), 0.029f, Mint, 6);

            var labelObject = new GameObject("Exit label", typeof(RectTransform));
            labelObject.transform.SetParent(transform, false);
            labelObject.transform.localPosition = position + Vector2.up * 0.56f;
            var label = labelObject.AddComponent<TMPro.TextMeshPro>();
            label.text = "USCITA";
            label.fontSize = 1.3f;
            label.characterSpacing = 4;
            label.color = Mint;
            label.alignment = TMPro.TextAlignmentOptions.Center;
            label.rectTransform.sizeDelta = new Vector2(1.0f, 0.18f);
            label.GetComponent<MeshRenderer>().sortingOrder = 5;
        }

        private void LateUpdate()
        {
            if (roomCamera != null)
                FrameRoom();
            UpdateBeltAnimations();
            UpdatePortalAnimation();
        }

        private void FrameRoom()
        {
            var center = ((Vector2)level.Size - Vector2.one) / 2f;
            float roomFraction = GameCanvasView.HeaderStart - GameCanvasView.ConsoleHeight;
            float halfHeight = Mathf.Max(level.Size.y / (2f * (roomFraction - 0.015f)),
                (level.Size.x + 1f) / (2f * roomCamera.aspect));
            float centerFraction = (GameCanvasView.HeaderStart + GameCanvasView.ConsoleHeight) / 2f;
            roomCamera.orthographicSize = halfHeight;
            roomCamera.transform.position = new Vector3(center.x, center.y - halfHeight * (2f * centerFraction - 1f), -10f);
        }

        private void BuildRobot()
        {
            robot = Layer("Biped Robot", Vector2.zero);
            body = new GameObject("Robot artwork", typeof(SpriteRenderer)).transform;
            body.SetParent(robot, false);
            body.localPosition = Vector2.down * 0.025f;
            neutralRobot = LoadRobotSprite("robot-neutro");
            errorRobot = LoadRobotSprite("robot-errore");
            robotImage = body.GetComponent<SpriteRenderer>();
            robotImage.sortingOrder = 12;
        }

        private static Sprite LoadRobotSprite(string name)
        {
            var texture = Resources.Load<Texture2D>($"Mockup/robot/{name}");
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f), texture.height / 0.95f);
        }

        private Transform Layer(string name, Vector2 position)
        {
            var layer = new GameObject(name).transform;
            layer.SetParent(transform, false);
            layer.localPosition = position;
            return layer;
        }

        private GameObject FocusFrame(string name, Color color)
        {
            var frame = Layer(name, Vector2.zero);
            var halo = color;
            halo.a = 0.15f;
            Block(frame, "Cell fill", Vector2.zero, Vector2.one * 0.99f, halo, 7);
            const float edge = 0.49f;
            for (int side = -1; side <= 1; side += 2)
            {
                Block(frame, "Horizontal edge", new Vector2(0, side * edge), new Vector2(0.99f, 0.016f), color, 8);
                Block(frame, "Vertical edge", new Vector2(side * edge, 0), new Vector2(0.016f, 0.99f), color, 8);
            }
            return frame.gameObject;
        }

        private void Segment(Transform parent, string label, Vector2 from, Vector2 to, float thickness,
            Color color, int order)
        {
            var delta = to - from;
            var line = Block(parent, label, (from + to) / 2f, new Vector2(delta.magnitude, thickness), color, order);
            line.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        }

        private SpriteRenderer Block(Transform parent, string label, Vector2 position, Vector2 size,
            Color color, int order)
        {
            var block = new GameObject(label, typeof(SpriteRenderer));
            block.transform.SetParent(parent, false);
            block.transform.localPosition = position;
            block.transform.localScale = new Vector3(size.x, size.y, 1f);
            var renderer = block.GetComponent<SpriteRenderer>();
            renderer.sprite = pixel;
            renderer.color = color;
            renderer.sortingOrder = order;
            return renderer;
        }

        public void ResetRobot(Vector2Int cell)
        {
            StopAllCoroutines();
            audioSource.Stop();
            foreach (Transform effect in effects)
            {
                effect.gameObject.SetActive(false);
                Destroy(effect.gameObject);
            }
            robot.gameObject.SetActive(true);
            robot.localPosition = (Vector2)cell;
            robot.localScale = Vector3.one;
            robotImage.sprite = neutralRobot;
            robotFocus.SetActive(false);
            errorFocus.SetActive(false);
            exitFocus.SetActive(false);
            exitCheck.SetActive(false);
            exitArrow.SetActive(true);
            if (trail != null)
            {
                trail.gameObject.SetActive(false);
                Destroy(trail.gameObject);
            }
            trail = Layer("Run trail", Vector2.zero);
            trailMarks.Clear();
            ResetPose();
        }

        private void ResetPose()
        {
            body.localScale = Vector3.one;
            body.localRotation = Quaternion.identity;
        }

        public void SnapToCell(Vector2Int cell)
        {
            robot.localPosition = (Vector2)cell;
            ResetPose();
            robotFocus.transform.localPosition = (Vector2)cell;
        }

        public void RecordMovement(SimulationEvent motion)
        {
            FaceMovement(motion);

            if (motion.From == motion.To || motion.Kind == MotionKind.Teleport)
                return;

            var offset = Vector2.down * 0.445f;
            var from = (Vector2)motion.From + offset;
            var to = (Vector2)motion.To + offset;
            for (int dash = 0; dash < 12; dash++)
            {
                var point = Vector2.Lerp(from, to, dash / 12f);
                var size = dash == 0 ? Vector2.one * 0.047f
                : motion.From.x == motion.To.x ? new Vector2(0.016f, 0.043f) : new Vector2(0.043f, 0.016f);
                trailMarks.Add(Block(trail, "Trail mark", point, size, MockupTheme.Cyan, 9));
            }
        }

        private void FaceMovement(SimulationEvent motion)
        {
            if (motion.Kind == MotionKind.Move || motion.Kind == MotionKind.JumpAcross || motion.Kind == MotionKind.Conveyor)
                robot.localScale = new Vector3(motion.To.x.CompareTo(motion.From.x), 1, 1);
        }

        public void StartRun(Vector2Int cell)
        {
            robotFocus.transform.localPosition = (Vector2)cell;
            robotFocus.SetActive(true);
        }

        public void ShowOutcome(RunOutcome outcome, Vector2Int? blockedCell)
        {
            robotFocus.SetActive(false);
            if (outcome == RunOutcome.Success)
            {
                robot.gameObject.SetActive(false);
                exitArrow.SetActive(false);
                exitCheck.SetActive(true);
                exitFocus.SetActive(true);
                foreach (var mark in trailMarks)
                    mark.color = MockupTheme.Mint;
                PlaySound(successSound);
            }
            else
            {
                if (world.Robots == null || !world.Robots[0].Exited)
                    robotImage.sprite = errorRobot;
                foreach (var other in extraRobotViews)
                    other.sprite = errorRobot;
                if (blockedCell.HasValue && level.Contains(blockedCell.Value))
                {
                    errorFocus.transform.localPosition = (Vector2)blockedCell.Value;
                    errorFocus.SetActive(true);
                    if (outcome != RunOutcome.Explosion)
                        StartCoroutine(Burst(blockedCell.Value));
                }
                PlaySound(outcome == RunOutcome.Explosion ? explosionSound : errorSound);
            }
        }

        public IEnumerator PlayEvent(SimulationEvent motion, System.Func<float> speed, bool reducedMotion)
        {
            if (motion.Kind == MotionKind.Finished)
                yield break;

            FaceMovement(motion);
            ShowStepDust(motion);
            ShowPortalTransfer(motion);

            float duration = motion.Kind switch
            {
                MotionKind.Move => 0.24f,
                MotionKind.Rise => 0.15f,
                MotionKind.JumpAcross => 0.18f,
                MotionKind.Fall => 0.13f,
                MotionKind.Teleport => 0.18f,
                _ => 0.13f
            };

            for (float elapsed = 0; elapsed < duration; elapsed += Time.unscaledDeltaTime * speed())
            {
                float t = elapsed / duration;
                float travel = Mathf.SmoothStep(0, 1, t);
                float arc = reducedMotion ? 0 : 4f * t * (1f - t);
                if (motion.Kind == MotionKind.Teleport)
                {
                    robot.localPosition = (Vector2)(t < 0.5f ? motion.From : motion.To);
                    if (!reducedMotion)
                        body.localScale = Vector3.one * Mathf.Lerp(0.65f, 1f, Mathf.Abs(2f * t - 1f));
                }
                else
                {
                    var position = Vector2.Lerp(motion.From, motion.To, travel);
                    if (motion.Kind == MotionKind.Move)
                        position.y += arc * 0.24f;

                    robot.localPosition = position;
                    body.localRotation = Quaternion.Euler(0, 0, motion.Kind == MotionKind.Move ? -arc * 8f : 0);
                }

                yield return null;
            }

            SnapToCell(motion.To);
        }

        public IEnumerator PlayJumpArc(Vector2Int start, SimulationEvent across, Vector2Int landing,
            System.Func<float> speed, bool reducedMotion)
        {
            FaceMovement(across);
            var crest = JumpCrest(start, across, reducedMotion);
            float verticalTravel = across.From.y - start.y + across.To.y - landing.y;
            float duration = 0.34f + verticalTravel * 0.07f;

            for (float elapsed = 0; elapsed < duration; elapsed += Time.unscaledDeltaTime * speed())
            {
                robot.localPosition = JumpPosition(start, crest, landing, elapsed / duration);
                yield return null;
            }

            SnapToCell(landing);
        }

        public IEnumerator PlayFall(Vector2Int start, Vector2Int landing, System.Func<float> speed,
            System.Action reachedCell)
        {
            int cells = start.y - landing.y;
            float duration = 0.23f * Mathf.Sqrt(cells);
            int crossed = 0;
            for (float elapsed = 0; elapsed < duration; elapsed += Time.unscaledDeltaTime * speed())
            {
                float t = elapsed / duration;
                float distance = cells * t * t;
                robot.localPosition = (Vector2)start + Vector2.down * distance;
                robotFocus.transform.localPosition = robot.localPosition;
                while (crossed < Mathf.FloorToInt(distance))
                {
                    crossed++;
                    reachedCell();
                }
                yield return null;
            }

            SnapToCell(landing);
            for (; crossed < cells; crossed++)
                reachedCell();
        }

        public void RecordJumpArc(Vector2Int start, SimulationEvent across, Vector2Int landing, bool reducedMotion)
        {
            FaceMovement(across);
            var crest = JumpCrest(start, across, reducedMotion);
            int samples = 12 * (1 + across.From.y - start.y + across.To.y - landing.y);
            var offset = Vector2.down * 0.445f;
            for (int sample = 0; sample < samples; sample++)
            {
                var position = JumpPosition(start, crest, landing, sample / (float)samples) + offset;
                float size = sample == 0 ? 0.047f : 0.026f;
                trailMarks.Add(Block(trail, "Jump trail mark", position, Vector2.one * size, MockupTheme.Cyan, 9));
            }
        }

        private Vector2 JumpCrest(Vector2Int start, SimulationEvent across, bool reducedMotion)
        {
            var aboveTakeoff = across.From + Vector2Int.up;
            var aboveArrival = across.To + Vector2Int.up;
            bool overheadSpace = level.Contains(aboveTakeoff) && !level.IsSolid(aboveTakeoff) &&
                level.Contains(aboveArrival) && !level.IsSolid(aboveArrival);
            float lift = !reducedMotion && overheadSpace ? 0.24f : 0f;
            return new Vector2(Mathf.Lerp(start.x, across.To.x, 0.42f), across.To.y + lift);
        }

        private static Vector2 JumpPosition(Vector2 start, Vector2 crest, Vector2 landing, float t)
        {
            var tangent = Vector2.right * (Mathf.Sign(landing.x - start.x) * 0.17f);
            if (t < 0.5f)
            {
                var lift = new Vector2(start.x, Mathf.Lerp(start.y, crest.y, 0.76f));
                return Cubic(start, lift, crest - tangent, crest, t * 2f);
            }

            var descend = new Vector2(landing.x, Mathf.Lerp(landing.y, crest.y, 0.69f));
            return Cubic(crest, crest + tangent, descend, landing, t * 2f - 1f);
        }

        private static Vector2 Cubic(Vector2 a, Vector2 b, Vector2 c, Vector2 d, float t)
        {
            float remaining = 1f - t;
            return a * (remaining * remaining * remaining) + b * (3f * remaining * remaining * t) +
                c * (3f * remaining * t * t) + d * (t * t * t);
        }

        private void OnDestroy()
        {
            DestroySounds();
            DestroyMotionEffects();
            DestroyPortalSprites();
            if (pixel != null)
                Destroy(pixel);
            if (neutralRobot != null)
                Destroy(neutralRobot);
            if (errorRobot != null)
                Destroy(errorRobot);
        }
    }
}
