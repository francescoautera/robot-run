
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RobotRun
{
    public enum CardType { Move, Jump }

    [Serializable]
    public struct CardSpec
    {
        public CardType Type;
        [Min(1)] public int Value;
        [Min(1)] public int Quantity;
    }

    public readonly struct CardInstruction
    {
        public int SpecIndex { get; }
        public CardType Type { get; }
        public int Value { get; }
        public int Direction { get; }

        public CardInstruction(int specIndex, CardType type, int value, int direction = 1)
        {
            SpecIndex = specIndex;
            Type = type;
            Value = value;
            Direction = direction;
        }

        public CardInstruction Flipped() => new(SpecIndex, Type, Value, -Direction);
    }

    [CreateAssetMenu(menuName = "Robot Run/Level")]
    public sealed class LevelAsset : ScriptableObject
    {
        [SerializeField] private string title;
        [SerializeField] private string objective;
        [SerializeField] private Vector2Int size;
        [SerializeField] private Vector2Int start;
        [SerializeField] private Vector2Int exit;
        [SerializeField] private Vector2Int[] terrain;
        [SerializeField, Min(1)] private int slots;
        [SerializeField] private CardSpec[] cards;
        [SerializeField] private bool hasPortal;
        [SerializeField] private Vector2Int portalA;
        [SerializeField] private Vector2Int portalB;
        [SerializeField] private Vector2Int portalPlate;
        [SerializeField, Range(-1, 1)] private int tutorialDirection;
        [SerializeField] private string tutorialHeading;
        [SerializeField] private Vector2Int[] extraRobots;
        [SerializeField] private DeviceDefinition[] devices;
        [SerializeField] private SignalLink[] signalLinks;
        [SerializeField] private bool portalControlledByDevices;
        [SerializeField] private bool portalInitiallyActive;

        public string Title => title;
        public string Objective => objective;
        public int Slots => slots;
        public bool HasPortal => hasPortal;
        public IReadOnlyList<CardSpec> Cards => cards;
        public bool IsTutorial => tutorialDirection != 0;
        public int TutorialDirection => tutorialDirection;
        public string TutorialHeading => tutorialHeading;

        public LevelDefinition BuildDefinition()
        {
            if (slots < 1 || cards == null || cards.Length == 0 ||
                Array.Exists(cards, card => !Enum.IsDefined(typeof(CardType), card.Type) ||
                    card.Quantity < 1 || card.Value < 1 || card.Value == int.MaxValue))
                throw new InvalidOperationException($"Invalid card inventory in {name}.");

            if (IsTutorial && (tutorialDirection != -1 && tutorialDirection != 1 || slots != 1 ||
                cards.Length != 1 || cards[0].Quantity != 1))
            {
                throw new InvalidOperationException($"A tutorial must have one card and a required direction in {name}.");
            }

            var solid = new HashSet<Vector2Int>(terrain ?? Array.Empty<Vector2Int>());
            var definition = new LevelDefinition(size, start, exit, solid, hasPortal, portalA, portalB, portalPlate,
                devices, signalLinks, extraRobots, portalControlledByDevices, portalInitiallyActive);

            if (!definition.Contains(start) || solid.Contains(start) ||
                !definition.Contains(exit) || solid.Contains(exit) ||
                hasPortal && (portalA == portalB || !definition.Contains(portalA) ||
                    !definition.Contains(portalB) || solid.Contains(portalA) || solid.Contains(portalB)))
            {
                throw new InvalidOperationException($"Invalid start, exit or portal configuration in {name}.");
            }

            if (hasPortal && !portalControlledByDevices && (!definition.Contains(portalPlate) || solid.Contains(portalPlate) ||
                !solid.Contains(portalPlate + Vector2Int.down) ||
                portalPlate == portalA || portalPlate == portalB || portalPlate == exit))
            {
                throw new InvalidOperationException($"Invalid portal plate configuration in {name}.");
            }

            definition.ValidateDevices();
            return definition;
        }
    }

    public sealed class LevelDefinition
    {
        private readonly HashSet<Vector2Int> terrain;

        public Vector2Int Size { get; }
        public Vector2Int Start { get; }
        public Vector2Int Exit { get; }
        public bool HasPortal { get; }
        public Vector2Int PortalA { get; }
        public Vector2Int PortalB { get; }
        public Vector2Int PortalPlate { get; }
        public bool HasLegacyPlate { get; }
        public bool PortalInitiallyActive { get; }
        public IReadOnlyList<Vector2Int> RobotStarts { get; }
        public IReadOnlyList<DeviceDefinition> Devices { get; }
        public IReadOnlyList<SignalLink> SignalLinks { get; }

        internal LevelDefinition(Vector2Int size, Vector2Int start, Vector2Int exit,
            HashSet<Vector2Int> terrain, bool hasPortal, Vector2Int portalA, Vector2Int portalB,
            Vector2Int portalPlate, DeviceDefinition[] devices = null, SignalLink[] links = null,
            Vector2Int[] extraRobots = null, bool portalControlledByDevices = false, bool portalInitiallyActive = false)
        {
            Size = size;
            Start = start;
            Exit = exit;
            this.terrain = terrain;
            HasPortal = hasPortal;
            PortalA = portalA;
            PortalB = portalB;
            PortalPlate = portalPlate;
            HasLegacyPlate = hasPortal && !portalControlledByDevices;
            PortalInitiallyActive = hasPortal && portalInitiallyActive;
            Devices = devices ?? Array.Empty<DeviceDefinition>();
            SignalLinks = links ?? Array.Empty<SignalLink>();
            var starts = new List<Vector2Int> { start };
            if (extraRobots != null)
                starts.AddRange(extraRobots);
            RobotStarts = starts;
        }

        internal void ValidateDevices()
        {
            var ids = new Dictionary<string, DeviceDefinition>(StringComparer.Ordinal);
            var bodies = new HashSet<Vector2Int>();
            var placements = new HashSet<(Vector2Int, DeviceKind)>();
            foreach (var device in Devices)
            {
                if (string.IsNullOrWhiteSpace(device.Id) || device.Id == "portal" || ids.ContainsKey(device.Id) ||
                    !Enum.IsDefined(typeof(DeviceKind), device.Kind) || !Contains(device.Cell) || IsSolid(device.Cell) ||
                    device.HasTimer && device.Duration < 1 ||
                    device.Kind == DeviceKind.Conveyor && device.Direction != -1 && device.Direction != 1 ||
                    !placements.Add((device.Cell, device.Kind)) ||
                    device.IsSolidBody && !bodies.Add(device.Cell))
                    throw new InvalidOperationException($"Invalid device {device.Id} at {device.Cell}.");

                ids.Add(device.Id, device);
            }

            var starts = new HashSet<Vector2Int>();
            foreach (var cell in RobotStarts)
            {
                if (!Contains(cell) || IsSolid(cell) || bodies.Contains(cell) || !starts.Add(cell))
                    throw new InvalidOperationException($"Invalid robot start at {cell}.");
            }

            var polarity = new Dictionary<string, bool>(StringComparer.Ordinal);
            foreach (var link in SignalLinks)
            {
                bool targetValid = link.Target == "portal" ? HasPortal && !HasLegacyPlate
                    : !string.IsNullOrEmpty(link.Target) && ids.TryGetValue(link.Target, out var receiver) && receiver.ReceivesSignal;
                if (string.IsNullOrEmpty(link.Source) || !ids.TryGetValue(link.Source, out var source) ||
                    !source.IsSwitch || !targetValid ||
                    polarity.TryGetValue(link.Target, out bool previous) && previous != link.Inverted)
                    throw new InvalidOperationException($"Invalid signal link {link.Source} → {link.Target}.");

                polarity[link.Target] = link.Inverted;
            }
        }

        public bool Contains(Vector2Int cell) =>
            cell.x >= 0 && cell.x < Size.x && cell.y >= 0 && cell.y < Size.y;

        public bool IsSolid(Vector2Int cell) => terrain.Contains(cell);
    }
}
