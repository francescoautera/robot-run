
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RobotRun
{
    public sealed class GameSessionController : MonoBehaviour
    {
        [SerializeField] private LevelAsset levelAsset;
        [SerializeField] private string otherScene;
        [SerializeField] private string nextLevelCaption;
        [SerializeField] private bool reducedMotion;
        [SerializeField] private bool muted;
        [SerializeField, Min(0f), Tooltip("Incremento di scala al passaggio del mouse. 0.1 equivale al 10%.")]
        private float cardHoverScaleIncrease = 0.1f;

        internal float CardHoverScaleIncrease => Mathf.Max(0f, cardHoverScaleIncrease);
        internal bool ReducedMotion => reducedMotion;
        internal bool CanPlay => simulation == null && draft.CanPlay(levelAsset.TutorialDirection);
        internal string NextLevelCaption => string.IsNullOrWhiteSpace(nextLevelCaption)
            ? "Prossimo livello" : nextLevelCaption;

        private LevelDefinition level;
        private ProgramDraft draft;
        private GridSimulation simulation;
        private RoomView room;
        private GameCanvasView canvas;
        private Coroutine playback;
        private readonly List<(SimulationEvent motion, RunState state, SimulationEvent[] robotMotions)> pendingSteps = new();
        private int nextPendingStep;
        private int jumpAcrossIndex;
        private bool pendingReducedMotion;
        private int speedLevel = 1;
        private RunState initialState;
        private bool HasWorldMechanics => level.Devices.Count > 0 || level.RobotStarts.Count > 1;

        private void Awake()
        {
            level = levelAsset.BuildDefinition();
            draft = new ProgramDraft(levelAsset.Cards, levelAsset.Slots);

            room = gameObject.AddComponent<RoomView>();
            room.Build(level);
            room.SetReducedMotion(reducedMotion);
            room.SetMuted(muted);

            canvas = gameObject.AddComponent<GameCanvasView>();
            canvas.Build(this, levelAsset, draft);
            canvas.ShowProgramming();
            initialState = new GridSimulation(level, Array.Empty<CardInstruction?>()).State;
            ShowDevices(initialState);
        }

        public void Play()
        {
            if (!CanPlay)
                return;

            simulation = new GridSimulation(level, draft.CreateSnapshot());
            room.StartRun(simulation.State.Cell);
            canvas.ShowRun(simulation.State, speedLevel);
            ShowDevices(simulation.State);
            playback = StartCoroutine(PlayRun());
        }

        private IEnumerator PlayRun()
        {
            // Yield once so the controller owns the coroutine before any immediate outcome.
            yield return null;
            while (simulation.State.Outcome == RunOutcome.Running)
            {
                ReadPlaybackBatch();
                if (jumpAcrossIndex >= 0)
                {
                    var first = pendingSteps[0];
                    var across = pendingSteps[jumpAcrossIndex].motion;
                    var last = pendingSteps[pendingSteps.Count - 1];
                    canvas.ShowRun(first.state, speedLevel, showOutcome: false);
                    yield return room.PlayJumpArc(first.motion.From, across, last.motion.To, PlaybackSpeed,
                        pendingReducedMotion);
                    RecordPendingSteps();
                    canvas.ShowRun(last.state, speedLevel, showOutcome: false);
                    room.PlayMotionSound(across);
                    ShowDevices(last.state, feedback: true);
                }
                else if (level.RobotStarts.Count == 1 && pendingSteps[0].motion.Kind == MotionKind.Fall)
                {
                    var first = pendingSteps[0];
                    var last = pendingSteps[pendingSteps.Count - 1];
                    canvas.ShowRun(first.state, speedLevel, showOutcome: false, movement: first.motion);
                    yield return room.PlayFall(first.motion.From, last.motion.To, PlaybackSpeed, PresentFallCell);
                }
                else
                {
                    while (nextPendingStep < pendingSteps.Count)
                    {
                        var step = pendingSteps[nextPendingStep];
                        canvas.ShowRun(step.state, speedLevel, showOutcome: false, movement: step.motion);
                        if (HasWorldMechanics)
                            yield return room.PlayMechanicsStep(step.motion, step.robotMotions, step.state, PlaybackSpeed,
                                pendingReducedMotion);
                        else
                            yield return room.PlayEvent(step.motion, PlaybackSpeed, pendingReducedMotion);
                        room.RecordMovement(step.motion);
                        room.PlayMotionSound(step.motion);
                        nextPendingStep++;
                        ShowDevices(step.state, feedback: true);
                    }
                }
            }

            playback = null;
            ShowOutcome();
        }

        private float PlaybackSpeed() => speedLevel switch
        {
            2 => 1f,
            3 => 3f,
            _ => 0.75f
        };

        private void ReadPlaybackBatch()
        {
            pendingSteps.Clear();
            nextPendingStep = 0;
            jumpAcrossIndex = -1;
            pendingReducedMotion = reducedMotion;

            var first = ReadStep();
            if (level.RobotStarts.Count > 1)
                return;

            if (first == MotionKind.Fall)
            {
                while (simulation.HasPendingFallStep)
                    ReadStep();
                return;
            }

            if (first != MotionKind.Rise)
                return;

            while (simulation.HasPendingJumpStep)
            {
                var kind = ReadStep();
                if (kind == MotionKind.JumpAcross)
                {
                    jumpAcrossIndex = pendingSteps.Count - 1;
                    while (simulation.HasPendingFallStep)
                        ReadStep();
                    break;
                }
            }
        }

        private MotionKind ReadStep()
        {
            var motion = simulation.Step();
            var robotMotions = HasWorldMechanics ? simulation.LastEvents.ToArray() : Array.Empty<SimulationEvent>();
            pendingSteps.Add((motion, simulation.State, robotMotions));
            return motion.Kind;
        }

        private void PresentFallCell()
        {
            var step = pendingSteps[nextPendingStep++];
            room.RecordMovement(step.motion);
            canvas.ShowRun(step.state, speedLevel, showOutcome: false, movement: step.motion);
            ShowDevices(step.state, feedback: true);
        }

        private void RecordPendingSteps()
        {
            if (nextPendingStep >= pendingSteps.Count)
                return;

            if (jumpAcrossIndex >= 0)
            {
                room.RecordJumpArc(pendingSteps[0].motion.From, pendingSteps[jumpAcrossIndex].motion,
                    pendingSteps[pendingSteps.Count - 1].motion.To, pendingReducedMotion);
                nextPendingStep = pendingSteps.Count;
                return;
            }

            while (nextPendingStep < pendingSteps.Count)
                room.RecordMovement(pendingSteps[nextPendingStep++].motion);
        }

        public void ToggleSpeed()
        {
            speedLevel = speedLevel == 3 ? 1 : speedLevel + 1;
            canvas.SetSpeed(speedLevel);
        }

        internal string Inspect(Vector2? point) => room.Inspect(point);
        internal string PortalRouteFrom(Vector2Int cell) => cell == level.PortalA ? "A → B" : "B → A";

        public void Skip()
        {
            if (simulation == null || playback == null)
                return;

            StopPlayback();
            RecordPendingSteps();
            while (simulation.State.Outcome == RunOutcome.Running)
            {
                ReadPlaybackBatch();
                RecordPendingSteps();
            }

            room.SnapToCell(simulation.State.Cell);
            ShowDevices(simulation.State);
            ShowOutcome();
        }

        public void ReturnToProgramming()
        {
            StopPlayback();
            pendingSteps.Clear();
            simulation = null;
            room.ResetRobot(level.Start);
            canvas.ShowProgramming();
            ShowDevices(initialState);
        }

        private void StopPlayback()
        {
            if (playback == null)
                return;

            StopCoroutine(playback);
            playback = null;
        }

        private void ShowDevices(RunState state, bool feedback = false)
        {
            room.RenderWorld(state, feedback);
            canvas.SetPortal(state.PortalEnabled);
        }

        private void ShowOutcome()
        {
            var state = simulation.State;
            room.ShowOutcome(state.Outcome, state.FailedCell);
            canvas.ShowRun(state, speedLevel);
        }

        public void LoadOtherLevel() => SceneManager.LoadScene(otherScene);
    }
}
