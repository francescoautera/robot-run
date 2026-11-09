using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RobotRun
{
    public enum RunOutcome { Running, Success, Blocked, FellOut, ProgramEnded, Trap, Explosion, Crushed, PortalBlocked, Stalled }
    public enum MotionKind { Move, Rise, JumpAcross, Fall, Teleport, JumpBlocked, Finished, Conveyor, Environment }

    public readonly struct RunState
    {
        public Vector2Int Cell { get; }
        public int ActiveSlot { get; }
        public int MoveRemaining { get; }
        public int Cycles { get; }
        public RunOutcome Outcome { get; }
        public bool PortalEnabled { get; }
        public int StepNumber { get; }
        public int CardStep { get; }
        public string Cause { get; }
        public Vector2Int? FailedCell { get; }
        public IReadOnlyList<RobotState> Robots { get; }
        public IReadOnlyList<DeviceState> Devices { get; }

        internal RunState(int activeSlot, int moveRemaining, int cycles, RunOutcome outcome, bool portalEnabled,
            int step, int cardStep, string cause, Vector2Int? failedCell, RobotState[] robots, DeviceState[] devices)
        {
            Cell = robots[0].Cell;
            ActiveSlot = activeSlot;
            MoveRemaining = moveRemaining;
            Cycles = cycles;
            Outcome = outcome;
            PortalEnabled = portalEnabled;
            StepNumber = step;
            CardStep = cardStep;
            Cause = cause;
            FailedCell = failedCell;
            Robots = Array.AsReadOnly(robots);
            Devices = Array.AsReadOnly(devices);
        }
    }

    public readonly struct SimulationEvent
    {
        public MotionKind Kind { get; }
        public Vector2Int From { get; }
        public Vector2Int To { get; }
        public int RobotIndex { get; }
        public int Index { get; }

        internal SimulationEvent(MotionKind kind, Vector2Int from, Vector2Int to, int robot = 0, int index = 0)
        {
            Kind = kind;
            From = from;
            To = to;
            RobotIndex = robot;
            Index = index;
        }
    }

    public sealed class GridSimulation
    {
        private sealed class Robot
        {
            public Vector2Int Cell;
            public int Remaining;
            public bool Settling = true;
            public bool Exited;
            public Vector2Int? Portal;
        }

        private sealed class Device
        {
            public DeviceDefinition Definition;
            public Vector2Int Cell;
            public bool Active;
            public bool Armed;
            public bool Removed;
            public bool Hardened;
            public bool PendingClose;
            public int Remaining;
        }

        private sealed class Intent
        {
            public int Robot;
            public MotionKind Kind;
            public Vector2Int To;
            public int Crate = -1;
            public Vector2Int CrateTo;
        }

        private readonly LevelDefinition level;
        private readonly CardInstruction?[] program;
        private readonly Robot[] robots;
        private readonly Device[] devices;
        private readonly Dictionary<string, Device> byId = new(StringComparer.Ordinal);
        private readonly List<SimulationEvent> events = new();
        private readonly HashSet<string> visited = new();
        private bool legacyPortal;
        private bool portalEnabled;
        private int slot = -1;
        private int cycles;
        private int step;
        private int cardStartedAt;
        private int eventIndex;
        private RunOutcome outcome;
        private string cause;
        private Vector2Int? failedCell;

        public RunState State { get; private set; }
        public IReadOnlyList<SimulationEvent> LastEvents { get; }
        private CardInstruction? CurrentCard => slot >= 0 ? program[slot] : null;

        internal bool HasPendingJumpStep => robots.Length == 1 && outcome == RunOutcome.Running &&
            !robots[0].Portal.HasValue && !robots[0].Settling && robots[0].Remaining > 0 &&
            CurrentCard?.Type == CardType.Jump && !Array.Exists(devices, CanFall);

        internal bool HasPendingFallStep => robots.Length == 1 && outcome == RunOutcome.Running &&
            !robots[0].Portal.HasValue && robots[0].Settling && !IsSolid(robots[0].Cell + Vector2Int.down) &&
            !Array.Exists(devices, CanFall);

        public GridSimulation(LevelDefinition level, CardInstruction?[] program)
        {
            this.level = level;
            this.program = (CardInstruction?[])program.Clone();
            LastEvents = events.AsReadOnly();
            robots = new Robot[level.RobotStarts.Count];
            for (int i = 0; i < robots.Length; i++)
                robots[i] = new Robot { Cell = level.RobotStarts[i] };

            devices = new Device[level.Devices.Count];
            for (int i = 0; i < devices.Length; i++)
            {
                var definition = level.Devices[i];
                var device = new Device { Definition = definition, Cell = definition.Cell, Active = definition.InitiallyActive };
                if (definition.Kind == DeviceKind.TimedSwitch && device.Active)
                {
                    device.Remaining = definition.Duration;
                    device.Armed = true;
                }
                devices[i] = device;
                byId.Add(definition.Id, device);
            }

            ResolveContacts(null, null, null);
            CaptureState();
        }

        public SimulationEvent Step()
        {
            if (outcome != RunOutcome.Running)
                throw new InvalidOperationException("The attempt has ended.");

            events.Clear();
            step++;
            if (!visited.Add(Signature()))
                Fail(RunOutcome.Stalled, "Il percorso si ripete senza avanzare nel programma.");

            if (outcome == RunOutcome.Running)
                Advance();

            CaptureState();
            foreach (var motion in events)
                if (motion.RobotIndex == 0)
                    return motion;

            var cell = robots[0].Cell;
            return new SimulationEvent(outcome == RunOutcome.Running ? MotionKind.Environment : MotionKind.Finished,
                cell, cell, -1, eventIndex++);
        }

        private void Advance()
        {
            var robotBefore = new Vector2Int[robots.Length];
            var crateBefore = new Vector2Int[devices.Length];
            for (int i = 0; i < robots.Length; i++)
                robotBefore[i] = robots[i].Cell;
            for (int i = 0; i < devices.Length; i++)
                crateBefore[i] = devices[i].Cell;

            if (DropCrates())
            {
                ResolveContacts(robotBefore, crateBefore, null);
                return;
            }

            var intents = PlanRound();
            if (intents.Count == 0 && outcome == RunOutcome.Running)
            {
                if (!BeginNextCard())
                {
                    Fail(RunOutcome.ProgramEnded, "Programma esaurito.");
                    return;
                }
                intents = PlanRound();
            }

            ValidateConflicts(intents);
            if (outcome != RunOutcome.Running)
                return;

            bool horizontal = intents.Exists(intent => intent.Kind == MotionKind.Move ||
                intent.Kind == MotionKind.JumpAcross || intent.Kind == MotionKind.Conveyor);
            if (horizontal)
            {
                cycles++;
                TickTimers();
            }

            foreach (var intent in intents)
                Commit(intent);

            CheckTraps();
            ResolveContacts(robotBefore, crateBefore, intents);
        }

        private List<Intent> PlanRound()
        {
            var intents = new List<Intent>(robots.Length);
            for (int i = 0; i < robots.Length && outcome == RunOutcome.Running; i++)
            {
                var robot = robots[i];
                if (robot.Exited)
                    continue;

                var intent = Plan(i, robot);
                if (intent != null)
                    intents.Add(intent);
            }
            return intents;
        }

        private Intent Plan(int index, Robot robot)
        {
            if (robot.Portal.HasValue)
            {
                var destination = robot.Portal.Value;
                if (IsSolid(destination) || RobotAt(destination, index))
                {
                    Fail(RunOutcome.PortalBlocked, "Destinazione del portale occupata.", destination);
                    return null;
                }

                return new Intent { Robot = index, Kind = MotionKind.Teleport, To = destination };
            }

            var below = robot.Cell + Vector2Int.down;
            bool supported = IsSolid(below);
            if (robot.Settling && !supported)
            {
                if (RobotAt(below, index))
                {
                    Fail(RunOutcome.Blocked, "Un robot blocca la caduta.", below);
                    return null;
                }
                return new Intent { Robot = index, Kind = MotionKind.Fall, To = below };
            }

            if (robot.Settling || robot.Remaining == 0)
            {
                foreach (var device in devices)
                    if (supported && device.Definition.Kind == DeviceKind.Conveyor && device.Active && device.Cell == robot.Cell)
                        return PlanHorizontal(index, robot, device.Definition.Direction, MotionKind.Conveyor);
            }

            if (robot.Remaining == 0 || !CurrentCard.HasValue)
                return null;

            var card = CurrentCard.Value;
            if (card.Type == CardType.Move)
                return PlanHorizontal(index, robot, card.Direction, MotionKind.Move);

            // A fresh jump rises Value times and reserves its final unit for the horizontal step.
            if (robot.Remaining > 1)
            {
                var above = robot.Cell + Vector2Int.up;
                return CanEnter(above, index)
                    ? new Intent { Robot = index, Kind = MotionKind.Rise, To = above }
                    : new Intent { Robot = index, Kind = MotionKind.JumpBlocked, To = robot.Cell };
            }

            return PlanHorizontal(index, robot, card.Direction, MotionKind.JumpAcross);
        }

        private Intent PlanHorizontal(int index, Robot robot, int direction, MotionKind kind)
        {
            var delta = new Vector2Int(direction, 0);
            var destination = robot.Cell + delta;
            if (CanEnter(destination, index))
                return new Intent { Robot = index, Kind = kind, To = destination };

            if (kind == MotionKind.JumpAcross)
                return new Intent { Robot = index, Kind = MotionKind.JumpBlocked, To = robot.Cell };

            int crate = CrateAt(destination);
            var beyond = destination + delta;
            if (crate >= 0 && !devices[crate].Hardened && CanEnter(beyond, index) && !RobotAt(destination, index))
                return new Intent { Robot = index, Kind = kind, To = destination, Crate = crate, CrateTo = beyond };

            string detail = kind == MotionKind.Conveyor ? "Il nastro è bloccato."
                : crate >= 0 && devices[crate].Hardened ? "La cassa indurita blocca il passo."
                : crate >= 0 ? "La cassa non può essere spinta."
                : RobotAt(destination, index) ? "Un robot occupa la destinazione." : "Il passo è bloccato da un ostacolo.";
            Fail(RunOutcome.Blocked, detail, destination);
            return null;
        }

        private bool BeginNextCard()
        {
            for (int next = slot + 1; next < program.Length; next++)
            {
                if (!program[next].HasValue)
                    continue;

                slot = next;
                cardStartedAt = step;
                var card = program[next].Value;
                foreach (var robot in robots)
                    if (!robot.Exited)
                        robot.Remaining = card.Value + (card.Type == CardType.Jump ? 1 : 0);
                return true;
            }

            return false;
        }

        private void ValidateConflicts(List<Intent> intents)
        {
            if (outcome != RunOutcome.Running)
                return;

            var destinations = new HashSet<Vector2Int>();
            var pushed = new HashSet<int>();
            foreach (var intent in intents)
            {
                if (intent.Kind == MotionKind.JumpBlocked)
                    continue;
                if (!destinations.Add(intent.To) || intent.Crate >= 0 &&
                    (!pushed.Add(intent.Crate) || !destinations.Add(intent.CrateTo)))
                {
                    Fail(RunOutcome.Blocked, "Due movimenti simultanei si ostacolano.", intent.To);
                    return;
                }
            }
        }

        private void Commit(Intent intent)
        {
            var robot = robots[intent.Robot];
            var from = robot.Cell;
            robot.Cell = intent.To;
            if (intent.Kind == MotionKind.Teleport)
                robot.Portal = null;
            if (intent.Kind == MotionKind.Move || intent.Kind == MotionKind.Rise)
                robot.Remaining--;
            if (intent.Kind == MotionKind.JumpAcross || intent.Kind == MotionKind.JumpBlocked)
                robot.Remaining = 0;
            robot.Settling = intent.Kind != MotionKind.Rise;

            if (intent.Crate >= 0)
            {
                var crate = devices[intent.Crate];
                crate.Cell = intent.CrateTo;
                if (crate.Definition.HasTimer && !crate.Armed)
                {
                    crate.Armed = true;
                    crate.Remaining = crate.Definition.Duration;
                }
            }

            events.Add(new SimulationEvent(intent.Kind, from, intent.To, intent.Robot, eventIndex++));
            if (intent.Kind == MotionKind.Fall && !level.Contains(intent.To))
                Fail(RunOutcome.FellOut, "Caduta fuori stanza.", intent.To);
        }

        private bool DropCrates()
        {
            bool moved = false;
            // Read the occupancy before moving any crate. A stack settles from the bottom across rounds.
            var falling = new List<Device>();
            foreach (var device in devices)
                if (CanFall(device))
                    falling.Add(device);

            foreach (var crate in falling)
            {
                var below = crate.Cell + Vector2Int.down;
                if (RobotAt(below))
                    Fail(RunOutcome.Crushed, "Un robot è stato schiacciato da una cassa.", below);
                else
                {
                    crate.Cell = below;
                    crate.Removed = !level.Contains(below);
                }
                moved = true;
            }
            return moved;
        }

        private bool CanFall(Device device) => device.Definition.IsCrate && !device.Removed && !device.Hardened &&
            !IsSolid(device.Cell + Vector2Int.down);

        private void TickTimers()
        {
            foreach (var device in devices)
            {
                if (device.Remaining <= 0 || device.Removed)
                    continue;

                device.Remaining--;
                if (device.Definition.Kind == DeviceKind.TimedSwitch && device.Remaining == 0)
                    device.Active = false;
            }
        }

        private void ResolveContacts(Vector2Int[] robotBefore, Vector2Int[] crateBefore, List<Intent> intents)
        {
            foreach (var device in devices)
            {
                var kind = device.Definition.Kind;
                if (kind == DeviceKind.PressurePlate)
                    device.Active = Occupied(device.Cell);
                else if (robotBefore != null && device.Definition.IsSwitch)
                {
                    int entries = Entries(device.Cell, robotBefore, crateBefore);
                    if (entries == 0)
                        continue;
                    if (kind == DeviceKind.ToggleSwitch && entries % 2 != 0)
                        device.Active = !device.Active;
                    else if (kind == DeviceKind.OneShotSwitch)
                        device.Active = true;
                    else if (kind == DeviceKind.TimedSwitch)
                    {
                        device.Active = true;
                        device.Armed = true;
                        device.Remaining = device.Definition.Duration;
                    }
                }
            }

            if (level.HasLegacyPlate && RobotAt(level.PortalPlate))
                legacyPortal = true;
            ResolveReceivers();
            foreach (var device in devices)
            {
                if (device.Definition.Kind == DeviceKind.FragileHatch && !device.Active)
                {
                    var above = device.Cell + Vector2Int.up;
                    bool occupied = Occupied(above);
                    if (device.Armed && !occupied)
                        device.Active = true;
                    if (occupied)
                        device.Armed = true;
                    if (intents != null && device.Armed)
                        foreach (var intent in intents)
                            if (intent.To == above && robots[intent.Robot].Remaining == 0 &&
                                intent.Kind != MotionKind.Rise && intent.Kind != MotionKind.JumpBlocked)
                                device.Active = true;
                    if (crateBefore != null && CrateAt(above) is int index && index >= 0 &&
                        crateBefore[index] != devices[index].Cell)
                        device.Active = true;
                }
            }

            ResolveTimedCrates();
            CheckTraps();

            if (outcome != RunOutcome.Running)
                return;

            bool allExited = true;
            bool reachedExit = false;
            for (int i = 0; i < robots.Length; i++)
            {
                var robot = robots[i];
                if (robot.Exited)
                    continue;
                if (robot.Cell == level.Exit)
                {
                    robot.Exited = true;
                    reachedExit = true;
                    continue;
                }
                allExited = false;
                if (!portalEnabled)
                {
                    robot.Portal = null;
                    continue;
                }
                if (robotBefore != null && robot.Cell == robotBefore[i])
                    continue;
                if (intents != null && intents.Exists(intent => intent.Robot == i && intent.Kind == MotionKind.Teleport))
                    continue;
                if (robot.Cell == level.PortalA)
                    robot.Portal = level.PortalB;
                else if (robot.Cell == level.PortalB)
                    robot.Portal = level.PortalA;
            }

            if (reachedExit)
            {
                RefreshPressurePlates();
                ResolveReceivers();
                CheckTraps();
            }
            if (allExited && outcome == RunOutcome.Running)
                outcome = RunOutcome.Success;
        }

        private void RefreshPressurePlates()
        {
            foreach (var device in devices)
                if (device.Definition.Kind == DeviceKind.PressurePlate)
                    device.Active = Occupied(device.Cell);
        }

        private void ResolveReceivers()
        {
            portalEnabled = level.HasPortal && (level.HasLegacyPlate ? legacyPortal : SignalValue("portal", level.PortalInitiallyActive));
            if (!portalEnabled)
                foreach (var robot in robots)
                    robot.Portal = null;

            foreach (var device in devices)
            {
                if (!device.Definition.ReceivesSignal)
                    continue;
                bool active = SignalValue(device.Definition.Id, device.Definition.InitiallyActive);
                if (device.Definition.Kind == DeviceKind.SignalHatch)
                {
                    device.PendingClose = !active && Occupied(device.Cell);
                    device.Active = active || device.PendingClose;
                }
                else
                    device.Active = active;
            }
        }
        private void CheckTraps()
        {
            foreach (var robot in robots)
                if (!robot.Exited)
                    foreach (var device in devices)
                        if (device.Definition.Kind == DeviceKind.Trap && device.Active && robot.Cell == device.Cell)
                            Fail(RunOutcome.Trap, $"Trappola attiva: {device.Definition.Id}.", robot.Cell);
        }

        private void ResolveTimedCrates()
        {
            var exploding = new SortedDictionary<string, Device>(StringComparer.Ordinal);
            foreach (var device in devices)
            {
                if (device.Removed || !device.Armed || device.Remaining > 0)
                    continue;
                if (device.Definition.Kind == DeviceKind.Tnt)
                    exploding[device.Definition.Id] = device;
                else if (device.Definition.Kind == DeviceKind.HardeningCrate)
                    device.Hardened = true;
            }

            while (exploding.Count > 0)
            {
                using var first = exploding.GetEnumerator();
                first.MoveNext();
                var pair = first.Current;
                exploding.Remove(pair.Key);
                var origin = pair.Value.Cell;
                pair.Value.Removed = true;
                foreach (var robot in robots)
                    if (!robot.Exited && InBlast(origin, robot.Cell))
                        Fail(RunOutcome.Explosion, $"Esplosione TNT: {pair.Key}.", robot.Cell);

                foreach (var crate in devices)
                {
                    if (!crate.Definition.IsCrate || crate.Removed || !InBlast(origin, crate.Cell))
                        continue;
                    if (crate.Definition.Kind == DeviceKind.Tnt)
                        exploding[crate.Definition.Id] = crate;
                    else
                        crate.Removed = true;
                }
            }
        }

        private static bool InBlast(Vector2Int origin, Vector2Int cell) =>
            Math.Abs(origin.x - cell.x) + Math.Abs(origin.y - cell.y) <= 1;

        private bool SignalValue(string target, bool fallback)
        {
            bool found = false;
            bool active = false;
            bool inverted = false;
            foreach (var link in level.SignalLinks)
            {
                if (link.Target != target)
                    continue;
                found = true;
                active |= byId[link.Source].Active;
                inverted = link.Inverted;
            }
            return found ? active != inverted : fallback;
        }

        private int Entries(Vector2Int cell, Vector2Int[] robotBefore, Vector2Int[] crateBefore)
        {
            int count = 0;
            for (int i = 0; i < robots.Length; i++)
                if (!robots[i].Exited && robots[i].Cell == cell && robotBefore[i] != cell)
                    count++;
            for (int i = 0; i < devices.Length; i++)
                if (devices[i].Definition.IsCrate && !devices[i].Removed && devices[i].Cell == cell && crateBefore[i] != cell)
                    count++;
            return count;
        }

        private bool CanEnter(Vector2Int cell, int robot) =>
            level.Contains(cell) && !IsSolid(cell) && !RobotAt(cell, robot);

        private bool Occupied(Vector2Int cell) => RobotAt(cell) || CrateAt(cell) >= 0;

        private bool RobotAt(Vector2Int cell, int except = -1)
        {
            for (int i = 0; i < robots.Length; i++)
                if (i != except && !robots[i].Exited && robots[i].Cell == cell)
                    return true;
            return false;
        }

        private int CrateAt(Vector2Int cell)
        {
            for (int i = 0; i < devices.Length; i++)
                if (devices[i].Definition.IsCrate && !devices[i].Removed && devices[i].Cell == cell)
                    return i;
            return -1;
        }

        private bool IsSolid(Vector2Int cell)
        {
            if (level.IsSolid(cell))
                return true;
            foreach (var device in devices)
            {
                if (device.Removed || device.Cell != cell || !device.Definition.IsSolidBody)
                    continue;
                if (device.Definition.IsCrate || device.Definition.Kind == DeviceKind.FixedBlock || !device.Active)
                    return true;
            }
            return false;
        }

        private void Fail(RunOutcome result, string reason, Vector2Int? cell = null)
        {
            if (outcome != RunOutcome.Running)
                return;
            outcome = result;
            cause = reason;
            failedCell = cell;
        }

        private void CaptureState()
        {
            var robotStates = Array.ConvertAll(robots, robot => new RobotState(robot.Cell, robot.Exited));
            var deviceStates = Array.ConvertAll(devices, device => new DeviceState(device.Definition, device.Cell,
                device.Active, device.Armed, device.Removed, device.Hardened, device.PendingClose, device.Remaining));
            State = new RunState(slot, CurrentCard?.Type == CardType.Move ? robots[0].Remaining : 0,
                cycles, outcome, portalEnabled, step, slot < 0 ? 0 : step - cardStartedAt + 1,
                cause, failedCell, robotStates, deviceStates);
        }

        private string Signature()
        {
            var key = new StringBuilder().Append(slot).Append('|').Append(legacyPortal).Append('|');
            foreach (var robot in robots)
                key.Append(robot.Cell.x).Append(',').Append(robot.Cell.y).Append(',').Append(robot.Remaining)
                    .Append(',').Append(robot.Settling).Append(',').Append(robot.Exited)
                    .Append(',').Append(robot.Portal?.x).Append(',').Append(robot.Portal?.y).Append('|');
            foreach (var device in devices)
                key.Append(device.Cell.x).Append(',').Append(device.Cell.y).Append(',').Append(device.Active)
                    .Append(',').Append(device.Armed).Append(',').Append(device.Removed).Append(',').Append(device.Hardened)
                    .Append(',').Append(device.PendingClose).Append(',').Append(device.Remaining).Append('|');
            return key.ToString();
        }
    }
}
