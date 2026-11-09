using System;
using UnityEngine;

namespace RobotRun
{
    public enum DeviceKind
    {
        Conveyor, PressurePlate, ToggleSwitch, OneShotSwitch, TimedSwitch,
        FragileHatch, SignalHatch, Trap, FixedBlock, Crate, Tnt, HardeningCrate
    }

    [Serializable]
    public struct DeviceDefinition
    {
        public string Id;
        public DeviceKind Kind;
        public Vector2Int Cell;
        public bool InitiallyActive;
        [Tooltip("Conveyor direction: -1 left, 1 right.")] public int Direction;
        [Min(0)] public int Duration;

        internal bool IsSwitch => Kind >= DeviceKind.PressurePlate && Kind <= DeviceKind.TimedSwitch;
        internal bool IsCrate => Kind >= DeviceKind.Crate;
        internal bool HasTimer => Kind == DeviceKind.TimedSwitch || Kind == DeviceKind.Tnt || Kind == DeviceKind.HardeningCrate;
        internal bool ReceivesSignal => Kind == DeviceKind.Conveyor || Kind == DeviceKind.SignalHatch || Kind == DeviceKind.Trap;
        internal bool IsSolidBody => Kind == DeviceKind.FixedBlock || IsCrate ||
            Kind == DeviceKind.FragileHatch || Kind == DeviceKind.SignalHatch;
    }

    [Serializable]
    public struct SignalLink
    {
        public string Source;
        [Tooltip("Device ID, or portal for the room's portal pair.")] public string Target;
        public bool Inverted;
    }

    public readonly struct DeviceState
    {
        public readonly DeviceDefinition Definition;
        public readonly Vector2Int Cell;
        public readonly bool Active;
        public readonly bool Armed;
        public readonly bool Removed;
        public readonly bool Hardened;
        public readonly bool PendingClose;
        public readonly int Remaining;

        internal DeviceState(DeviceDefinition definition, Vector2Int cell, bool active, bool armed,
            bool removed, bool hardened, bool pendingClose, int remaining)
        {
            Definition = definition;
            Cell = cell;
            Active = active;
            Armed = armed;
            Removed = removed;
            Hardened = hardened;
            PendingClose = pendingClose;
            Remaining = remaining;
        }
    }

    public readonly struct RobotState
    {
        public readonly Vector2Int Cell;
        public readonly bool Exited;

        internal RobotState(Vector2Int cell, bool exited)
        {
            Cell = cell;
            Exited = exited;
        }
    }
}
