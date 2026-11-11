using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

namespace RobotRun
{
    public sealed partial class RoomView
    {
        private sealed class DeviceView
        {
            public Transform Root;
            public SpriteRenderer Body;
            public SpriteRenderer TimerDisplay;
            public TextMeshPro Label;
            public BeltAnimation Belt;
        }

        private readonly List<(string source, string target, bool inverted, GameObject line)> signalViews = new();
        private SpriteRenderer[] extraRobotViews = Array.Empty<SpriteRenderer>();
        private DeviceView[] deviceViews = Array.Empty<DeviceView>();
        private RunState world;
        private DeviceTooltipText tooltipText;
        private Transform effects;
        private AudioSource audioSource;
        private AudioClip stepSound, deviceSound, explosionSound, errorSound, successSound;
        private bool effectsReduced;

        private void BuildMechanics()
        {
            tooltipText = Resources.Load<DeviceTooltipText>(DeviceTooltipText.ResourceName);
            if (tooltipText == null)
                Debug.LogError("Impossibile caricare lo ScriptableObject DeviceTooltipText da Resources.");

            effects = Layer("Mechanic effects", Vector2.zero);
            deviceViews = new DeviceView[level.Devices.Count];
            var cells = new Dictionary<string, Vector2Int>(StringComparer.Ordinal);
            for (int i = 0; i < deviceViews.Length; i++)
            {
                var definition = level.Devices[i];
                cells.Add(definition.Id, definition.Cell);
                var root = Layer(definition.Id, definition.Cell);
                bool hatch = definition.Kind == DeviceKind.FragileHatch || definition.Kind == DeviceKind.SignalHatch;
                int order = definition.Kind == DeviceKind.Conveyor ? 5 : 8;
                var body = Artwork(root, "Device sprite", Vector2.zero, order);
                body.flipX = definition.Kind == DeviceKind.Conveyor && definition.Direction < 0;
                var label = definition.HasTimer || hatch
                    ? WorldLabel(root, "Additional state", Vector2.up * 0.55f, new Vector2(0.84f, 0.25f), 1.2f)
                    : null;
                var timerDisplay = definition.Kind == DeviceKind.TimedSwitch
                    ? Artwork(root, "Visible timer display", Vector2.up * 0.61f, 14)
                    : null;
                var belt = definition.Kind == DeviceKind.Conveyor ? new BeltAnimation(this, root, definition.Direction) : null;
                deviceViews[i] = new DeviceView { Root = root, Body = body, Label = label, TimerDisplay = timerDisplay, Belt = belt };
            }

            foreach (var link in level.SignalLinks)
            {
                if (link.Target == "portal")
                {
                    BuildWire(link.Source, link.Target, cells[link.Source], level.PortalA, link.Inverted);
                    BuildWire(link.Source, link.Target, cells[link.Source], level.PortalB, link.Inverted);
                }
                else
                    BuildWire(link.Source, link.Target, cells[link.Source], cells[link.Target], link.Inverted);
            }
            if (level.HasPortal)
                BuildWire("portal", "portal", level.PortalA, level.PortalB);
            if (level.HasLegacyPlate)
                BuildWire("legacyPlate", "portal", level.PortalPlate, level.PortalA);

            BuildSignalPipes();

            extraRobotViews = new SpriteRenderer[level.RobotStarts.Count - 1];
            for (int i = 0; i < extraRobotViews.Length; i++)
            {
                var extra = Layer($"Robot {i + 2}", level.RobotStarts[i + 1]);
                var sprite = extra.gameObject.AddComponent<SpriteRenderer>();
                sprite.sprite = neutralRobot;
                sprite.color = new Color(0.77f, 0.90f, 1f);
                sprite.sortingOrder = 12;
                extraRobotViews[i] = sprite;
                WorldLabel(extra, "Robot number", Vector2.up * 0.55f, Vector2.one, 1.1f).text = $"{i + 2}";
            }

            roomCamera.gameObject.AddComponent<AudioListener>();
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.volume = 0.24f;
            stepSound = Tone("Step", 185f, 95f, 0.072f);
            deviceSound = Tone("Device", 390f, 630f, 0.13f);
            explosionSound = Tone("Explosion", 105f, 31f, 0.25f, noisy: true);
            errorSound = Tone("Error", 210f, 75f, 0.18f);
            successSound = Tone("Exit", 320f, 720f, 0.23f);
        }

        private static SpriteRenderer Artwork(Transform parent, string name, Vector2 position, int order)
        {
            var spriteObject = new GameObject(name, typeof(SpriteRenderer));
            spriteObject.transform.SetParent(parent, false);
            spriteObject.transform.localPosition = position;
            var sprite = spriteObject.GetComponent<SpriteRenderer>();
            sprite.sortingOrder = order;
            return sprite;
        }

        private static TextMeshPro WorldLabel(Transform parent, string name, Vector2 position, Vector2 size, float fontSize)
        {
            var labelObject = new GameObject(name, typeof(RectTransform));
            labelObject.transform.SetParent(parent, false);
            labelObject.transform.localPosition = position;
            var label = labelObject.AddComponent<TextMeshPro>();
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.fontSize = fontSize;
            label.rectTransform.sizeDelta = size;
            label.GetComponent<MeshRenderer>().sortingOrder = 14;
            return label;
        }

        private void BuildWire(string source, string target, Vector2 from, Vector2 to, bool inverted = false)
        {
            var line = Layer($"Signal {source} to {target}", Vector2.zero).gameObject;
            Segment(line.transform, "Signal line", from, to, 0.028f,
                inverted ? MockupTheme.Coral : MockupTheme.Cyan, 10);
            line.SetActive(false);
            signalViews.Add((source, target, inverted, line));
        }

        public void RenderWorld(RunState state, bool feedback = false)
        {
            if (feedback && world.Robots != null)
                ShowDeviceChanges(state);

            world = state;
            SetPortalEnabled(state.PortalEnabled);
            robot.gameObject.SetActive(!state.Robots[0].Exited);
            if (state.Robots[0].Exited)
                robotFocus.SetActive(false);
            for (int i = 0; i < extraRobotViews.Length; i++)
            {
                var sprite = extraRobotViews[i];
                var next = state.Robots[i + 1];
                sprite.transform.localPosition = RobotSurfacePosition(next.Cell);
                if (state.StepNumber == 0)
                    sprite.transform.localScale = Vector3.one;
                sprite.sprite = neutralRobot;
                sprite.gameObject.SetActive(!next.Exited);
            }

            for (int i = 0; i < deviceViews.Length; i++)
            {
                var view = deviceViews[i];
                var device = state.Devices[i];
                view.Root.gameObject.SetActive(!device.Removed);
                if (device.Removed)
                    continue;

                view.Root.localPosition = (Vector2)device.Cell;
                view.Body.sprite = DeviceSprites.Get(device);
                if (view.Label != null)
                    view.Label.text = ExtraDeviceLabel(device);
                if (view.TimerDisplay != null)
                {
                    bool covered = false;
                    foreach (var robotState in state.Robots)
                        if (!robotState.Exited && robotState.Cell == device.Cell)
                            covered = true;

                    view.TimerDisplay.enabled = covered && device.Remaining <= 2;
                    if (view.TimerDisplay.enabled)
                        view.TimerDisplay.sprite = DeviceSprites.TimerDisplay(device);
                }
            }
        }

        public IEnumerator PlayMechanicsStep(SimulationEvent motion, IReadOnlyList<SimulationEvent> robotMotions,
            RunState state, Func<float> speed, bool reducedMotion)
        {
            var previous = world;
            FaceMovement(motion);
            var motionKinds = new MotionKind?[state.Robots.Count];
            foreach (var robotMotion in robotMotions)
            {
                motionKinds[robotMotion.RobotIndex] = robotMotion.Kind;
                ShowStepDust(robotMotion);
                ShowPortalTransfer(robotMotion);
            }
            const float duration = 0.19f;
            for (float elapsed = 0; elapsed < duration; elapsed += Time.unscaledDeltaTime * speed())
            {
                float t = Mathf.Clamp01(elapsed / duration);
                float blend = Mathf.SmoothStep(0, 1, t);
                for (int i = 0; i < state.Robots.Count; i++)
                {
                    var from = previous.Robots[i].Cell;
                    var to = state.Robots[i].Cell;
                    bool teleport = motionKinds[i] == MotionKind.Teleport;
                    bool onBelt = motionKinds[i] == MotionKind.Conveyor;
                    bool falling = motionKinds[i] == MotionKind.Fall;
                    var position = teleport ? RobotSurfacePosition(t < 0.5f ? from : to)
                        : Vector2.Lerp(RobotSurfacePosition(from), RobotSurfacePosition(to), onBelt || falling ? t : blend);
                    if (!reducedMotion && !teleport && !onBelt && from.y == to.y && from.x != to.x)
                        position.y += 0.17f * 4f * t * (1f - t);
                    var view = i == 0 ? robot : extraRobotViews[i - 1].transform;
                    view.localPosition = position;
                    if (i != 0 && !teleport && from.x != to.x)
                        view.localScale = new Vector3(Math.Sign(to.x - from.x), 1, 1);
                }
                for (int i = 0; i < deviceViews.Length; i++)
                    deviceViews[i].Root.localPosition = Vector2.Lerp(previous.Devices[i].Cell, state.Devices[i].Cell, blend);
                yield return null;
            }

            SnapToCell(state.Cell);
        }

        private static string ExtraDeviceLabel(DeviceState state)
        {
            if (state.PendingClose)
                return "ATT";
            if (state.Definition.Kind == DeviceKind.FragileHatch && state.Armed && !state.Active)
                return "!";

            int maxPrintedValue = state.Definition.Kind == DeviceKind.HardeningCrate ? 1 : 2;
            return state.Definition.HasTimer && state.Remaining > maxPrintedValue
                ? state.Remaining.ToString() : "";
        }

        public string Inspect(Vector2? screenPoint)
        {
            var selected = new HashSet<string>(StringComparer.Ordinal);
            var text = new StringBuilder();
            if (screenPoint.HasValue && world.Devices != null)
            {
                var position = roomCamera.ScreenToWorldPoint(new Vector3(screenPoint.Value.x, screenPoint.Value.y, 10f));
                var cell = new Vector2Int(Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.y));
                if (level.Contains(cell))
                {
                    if (level.HasPortal && (cell == level.PortalA || cell == level.PortalB))
                    {
                        selected.Add("portal");
                        AppendInspectionText(text, tooltipText?.Portal(world.PortalEnabled), "  |  ");
                    }
                    if (level.HasLegacyPlate && cell == level.PortalPlate)
                    {
                        selected.Add("legacyPlate");
                        AppendInspectionText(text, tooltipText?.PortalPlate(world.PortalEnabled), "  |  ");
                    }
                    foreach (var device in world.Devices)
                    {
                        if (device.Removed || device.Cell != cell)
                            continue;
                        selected.Add(device.Definition.Id);
                        AppendInspectionText(text, tooltipText?.Device(device), "  |  ");
                    }
                }
            }

            var linked = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var wire in signalViews)
            {
                bool show = selected.Contains(wire.source) || selected.Contains(wire.target);
                if (!pipedSignals.Contains(wire.line) && wire.line.activeSelf != show)
                    wire.line.SetActive(show);
                if (show && wire.source != wire.target)
                {
                    var endpoint = selected.Contains(wire.source) ? wire.target : wire.source;
                    string name = tooltipText?.LinkEndpoint(endpoint, wire.inverted);
                    if (!string.IsNullOrWhiteSpace(name))
                        linked.Add(name);
                }
            }
            if (linked.Count > 0)
                AppendInspectionText(text, tooltipText?.Links(string.Join(", ", linked)), "  ·  ");
            return text.ToString();
        }

        private static void AppendInspectionText(StringBuilder text, string description, string separator)
        {
            if (string.IsNullOrWhiteSpace(description))
                return;
            if (text.Length > 0)
                text.Append(separator);
            text.Append(description);
        }

        private void ShowDeviceChanges(RunState state)
        {
            if (state.PortalEnabled != world.PortalEnabled)
            {
                PlaySound(deviceSound);
                if (state.PortalEnabled && level.HasLegacyPlate)
                    StartCoroutine(PortalTrace((Vector2)level.PortalPlate + Vector2.down * 0.24f,
                        (Vector2)level.PortalA + Vector2.up * 0.34f, 0.28f));
            }

            for (int i = 0; i < state.Devices.Count; i++)
            {
                var before = world.Devices[i];
                var after = state.Devices[i];
                if (!before.Removed && after.Removed && after.Definition.Kind == DeviceKind.Tnt && level.Contains(after.Cell))
                {
                    PlaySound(explosionSound);
                    StartCoroutine(ShowTntExplosion(after.Cell));
                }
                else if (!before.Hardened && after.Hardened && !after.Removed)
                {
                    PlaySound(deviceSound);
                    StartCoroutine(ShowDeviceFrame(DeviceSprites.HardeningZero, after.Cell, 0.13f, 13, fade: false));
                }
                else if (before.Active != after.Active || before.Hardened != after.Hardened ||
                    before.Armed != after.Armed || before.Remaining != after.Remaining)
                {
                    if (before.Active != after.Active || before.Hardened != after.Hardened || before.Armed != after.Armed)
                        PlaySound(deviceSound);
                    StartCoroutine(Flash(after.Cell, MockupTheme.Cyan));
                }
            }
        }

        private IEnumerator ShowTntExplosion(Vector2Int cell)
        {
            yield return ShowDeviceFrame(DeviceSprites.TntZero, cell, 0.10f, 13, fade: false);
            yield return ShowDeviceFrame(DeviceSprites.ExplosionArea, cell, 0.29f, 11, fade: !effectsReduced);
        }

        private IEnumerator ShowDeviceFrame(Sprite sprite, Vector2Int cell, float duration, int order, bool fade)
        {
            var frame = Artwork(effects, "Device feedback", cell, order);
            frame.sprite = sprite;
            for (float time = 0; time < duration; time += Time.unscaledDeltaTime)
            {
                if (fade)
                    frame.color = new Color(1f, 1f, 1f, 1f - time / duration);
                yield return null;
            }
            Destroy(frame.gameObject);
        }

        private IEnumerator Flash(Vector2Int cell, Color tint)
        {
            var renderer = Block(effects, "Effect pulse", cell, Vector2.one * 0.94f, tint, 11);
            const float duration = 0.19f;
            for (float time = 0; time < duration && renderer != null; time += Time.unscaledDeltaTime)
            {
                tint.a = 0.54f * (1f - time / duration);
                renderer.color = tint;
                yield return null;
            }
            if (renderer != null)
                Destroy(renderer.gameObject);
        }

        private IEnumerator Burst(Vector2Int cell)
        {
            if (effectsReduced)
            {
                yield return Flash(cell, MockupTheme.Coral);
                yield break;
            }

            var origin = (Vector2)cell;
            var burst = Layer("Cortocircuito", origin);
            burst.SetParent(effects, false);
            var rays = new SpriteRenderer[6];
            for (int i = 0; i < rays.Length; i++)
            {
                float angle = i * 60f;
                var direction = Quaternion.Euler(0, 0, angle) * Vector3.right;
                rays[i] = Block(burst, "Scintilla", direction * 0.22f,
                    new Vector2(0.25f, 0.034f), MockupTheme.Coral, 13);
                rays[i].transform.localRotation = Quaternion.Euler(0, 0, angle);
            }

            const float duration = 0.22f;
            for (float elapsed = 0; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                float t = elapsed / duration;
                burst.localScale = Vector3.one * Mathf.Lerp(0.65f, 1.55f, t);
                var color = MockupTheme.Coral;
                color.a = 1f - t;
                foreach (var ray in rays)
                    ray.color = color;
                yield return null;
            }
            Destroy(burst.gameObject);
        }

        public void SetMuted(bool muted) => audioSource.mute = muted;
        public void SetReducedMotion(bool reduced) => effectsReduced = reduced;

        public void PlayMotionSound(SimulationEvent motion)
        {
            if (motion.Kind == MotionKind.Move || motion.Kind == MotionKind.Conveyor ||
                motion.Kind == MotionKind.JumpAcross || motion.Kind == MotionKind.JumpBlocked)
                PlaySound(stepSound);
            else if (motion.Kind == MotionKind.Teleport)
                PlaySound(deviceSound);
        }

        private void PlaySound(AudioClip clip)
        {
            if (audioSource != null && clip != null && !audioSource.mute)
                audioSource.PlayOneShot(clip);
        }

        private static AudioClip Tone(string name, float start, float end, float duration, bool noisy = false)
        {
            const int sampleRate = 22050;
            var samples = new float[(int)(duration * sampleRate)];
            float phase = 0;
            var random = new System.Random(274);
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)samples.Length;
                phase += Mathf.Lerp(start, end, t) * (2f * Mathf.PI / sampleRate);
                float tone = noisy ? (float)(random.NextDouble() * 2 - 1) * 0.66f + Mathf.Sin(phase) * 0.34f
                    : Mathf.Sin(phase);
                samples[i] = tone * Mathf.Min(t * 50f, 1f) * (1f - t) * (1f - t);
            }
            var clip = AudioClip.Create(name, samples.Length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private void DestroySounds()
        {
            foreach (var clip in new[] { stepSound, deviceSound, explosionSound, errorSound, successSound })
                if (clip != null) Destroy(clip);
        }
    }
}
