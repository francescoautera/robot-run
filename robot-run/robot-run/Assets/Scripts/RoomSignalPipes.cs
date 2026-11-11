using System;
using System.Collections.Generic;
using UnityEngine;

namespace RobotRun
{
    public sealed partial class RoomView
    {
        private sealed class PipePulse
        {
            internal List<Vector2Int> Path;
            internal GameObject Root;
            internal SpriteRenderer Glow;
            internal readonly SpriteRenderer[] Bolts = new SpriteRenderer[10];
            internal readonly SpriteRenderer[] Halos = new SpriteRenderer[10];
        }

        private sealed class PipeCircuit
        {
            internal string Source;
            internal bool Inverted;
            internal bool Powered;
            internal float Started;
            internal readonly Dictionary<Vector2Int, int> Connections = new();
            internal readonly List<(SpriteRenderer view, Sprite off, Sprite on)> Tiles = new();
            internal readonly List<PipePulse> Pulses = new();
        }

        private readonly List<PipeCircuit> pipeCircuits = new();
        private readonly HashSet<GameObject> pipedSignals = new();
        private readonly Dictionary<string, Sprite> pipeSprites = new();

        private void BuildSignalPipes()
        {
            var pipeRoot = Layer("Signal pipes", Vector2.zero);
            var circuits = new Dictionary<(string source, bool inverted), PipeCircuit>();
            foreach (var wire in signalViews)
            {
                if (wire.source == wire.target)
                    continue;

                Vector2Int from;
                if (wire.source == "legacyPlate")
                    from = level.PortalPlate;
                else
                {
                    int index = FindPipeSource(wire.source);
                    if (index < 0 || !level.Devices[index].IsSwitch)
                        continue;
                    from = level.Devices[index].Cell;
                }

                // Portal signal metadata contains separate endpoints for A and B.
                var endpoints = wire.target == "portal"
                    ? new[] { level.PortalA, level.PortalB }
                    : new[] { level.Devices[FindPipeSource(wire.target)].Cell };
                bool hasPipe = false;
                foreach (var to in endpoints)
                {
                    if (!ShouldDrawPipe(from, to))
                        continue;

                    var key = (wire.source, wire.inverted);
                    if (!circuits.TryGetValue(key, out var circuit))
                    {
                        circuit = new PipeCircuit { Source = wire.source, Inverted = wire.inverted };
                        circuits.Add(key, circuit);
                        pipeCircuits.Add(circuit);
                    }
                    var path = PipePath(from, to);
                    // The two portal wires describe the same circuit.
                    bool duplicate = circuit.Pulses.Exists(p => p.Path[0] == from &&
                        p.Path[p.Path.Count - 1] == to);
                    if (!duplicate)
                    {
                        for (int i = 1; i < path.Count; i++)
                        {
                            AddPipeConnection(circuit.Connections, path[i - 1], path[i]);
                            AddPipeConnection(circuit.Connections, path[i], path[i - 1]);
                        }
                        var root = new GameObject("Scarica " + wire.source);
                        root.transform.SetParent(pipeRoot, false);
                        var pulse = new PipePulse { Path = path, Root = root };
                        pulse.Glow = Artwork(root.transform, "Nucleo luminoso", from, 5);
                        pulse.Glow.sprite = PipeSprite("accessori/nodo-acceso");
                        pulse.Glow.transform.localScale = Vector3.one * 0.95f;
                        for (int i = 0; i < pulse.Bolts.Length; i++)
                        {
                            pulse.Halos[i] = Block(root.transform, "Alone elettrico", Vector2.zero,
                                Vector2.one, new Color(0.68f, 1f, 0.20f, 0.28f), 5);
                            pulse.Bolts[i] = Block(root.transform, "Arco elettrico", Vector2.zero,
                                Vector2.one, new Color(0.95f, 1f, 0.78f), 6);
                        }
                        root.SetActive(false);
                        circuit.Pulses.Add(pulse);
                    }
                    hasPipe = true;
                }

                if (hasPipe)
                    pipedSignals.Add(wire.line);
            }

            foreach (var circuit in pipeCircuits)
            {
                foreach (var tile in circuit.Connections)
                {
                    string name = PipeTileName(tile.Value);
                    var view = Artwork(pipeRoot, "Tubo " + circuit.Source, (Vector2)tile.Key + Vector2.down * 0.3f, 4);
                    var off = PipeSprite("spenti/" + name);
                    var on = PipeSprite("accesi/" + name);
                    view.sprite = off;
                    circuit.Tiles.Add((view, off, on));
                }
            }
        }

        internal static bool ShouldDrawPipe(Vector2Int from, Vector2Int to)
        {
            if (from.x == to.x && from.y > to.y)
                return false;
            return Mathf.Abs(to.x - from.x) + Mathf.Abs(to.y - from.y) >= 3;
        }

        internal static List<Vector2Int> PipePath(Vector2Int from, Vector2Int to)
        {
            var path = new List<Vector2Int> { from };
            if (from.x == to.x)
            {
                AddPipeLeg(path, to);
                return path;
            }
            int row = Mathf.Max(0, Mathf.Min(from.y, to.y) - 1);
            AddPipeLeg(path, new Vector2Int(from.x, row));
            AddPipeLeg(path, new Vector2Int(to.x, row));
            AddPipeLeg(path, to);
            return path;
        }

        private static void AddPipeLeg(List<Vector2Int> path, Vector2Int end)
        {
            var cell = path[path.Count - 1];
            while (cell != end)
            {
                cell += new Vector2Int(Math.Sign(end.x - cell.x), Math.Sign(end.y - cell.y));
                path.Add(cell);
            }
        }

        private static void AddPipeConnection(Dictionary<Vector2Int, int> tiles, Vector2Int cell, Vector2Int next)
        {
            var direction = next - cell;
            int bit = direction.x > 0 ? 1 : direction.y > 0 ? 2 : direction.x < 0 ? 4 : 8;
            tiles.TryGetValue(cell, out int mask);
            tiles[cell] = mask | bit;
        }

        internal static string PipeTileName(int mask) => mask switch
        {
            1 => "terminale-ingresso-destra",
            2 => "terminale-ingresso-su",
            4 => "terminale-ingresso-sinistra",
            8 => "terminale-ingresso-giu",
            5 => "dritto-orizzontale",
            10 => "dritto-verticale",
            3 => "angolo-su-destra",
            6 => "angolo-su-sinistra",
            9 => "angolo-giu-destra",
            12 => "angolo-giu-sinistra",
            7 => "raccordo-t-su",
            11 => "raccordo-t-destra",
            13 => "raccordo-t-giu",
            14 => "raccordo-t-sinistra",
            15 => "raccordo-croce",
            _ => throw new ArgumentOutOfRangeException(nameof(mask))
        };

        private int FindPipeSource(string id)
        {
            for (int i = 0; i < level.Devices.Count; i++)
                if (level.Devices[i].Id == id)
                    return i;
            return -1;
        }

        private Sprite PipeSprite(string name)
        {
            if (!pipeSprites.TryGetValue(name, out var sprite))
            {
                sprite = Resources.Load<Sprite>("Mockup/Devices/tubi-portali/" + name);
                if (sprite == null)
                    Debug.LogError("Sprite tubo mancante: " + name);
                pipeSprites.Add(name, sprite);
            }
            return sprite;
        }

        private static Vector2 PipePoint(List<Vector2Int> path, float distance)
        {
            distance = Mathf.Clamp(distance, 0f, path.Count - 1);
            int index = Mathf.Min(Mathf.FloorToInt(distance), path.Count - 2);
            return Vector2.Lerp(path[index], path[index + 1], distance - index) + Vector2.down * 0.3f;
        }

        private static void RenderPipeDischarge(PipePulse pulse, float distance, float time)
        {
            var head = PipePoint(pulse.Path, distance);
            pulse.Glow.transform.localPosition = head;
            float intensity = 0.5f + 0.5f * Mathf.Sin(time * 19f);
            pulse.Glow.transform.localScale = Vector3.one * Mathf.Lerp(0.85f, 1.05f, intensity);
            pulse.Glow.color = new Color(0.90f, 1f, 0.65f, Mathf.Lerp(0.75f, 0.95f, intensity));

            var previous = head;
            for (int i = 0; i < pulse.Bolts.Length; i++)
            {
                float trailingDistance = distance - (i + 1) * 0.085f;
                var point = PipePoint(pulse.Path, trailingDistance);
                var tangent = PipePoint(pulse.Path, trailingDistance + 0.04f) - point;
                var normal = new Vector2(-tangent.y, tangent.x).normalized;
                float jitter = Mathf.PerlinNoise(i * 3.71f, time * 16f) - 0.5f;
                point += normal * (jitter * 0.15f);
                var delta = point - previous;
                var center = (point + previous) * 0.5f;
                var angle = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
                float fade = 1f - i / (float)pulse.Bolts.Length;
                var bolt = pulse.Bolts[i];
                var halo = pulse.Halos[i];
                bool visible = trailingDistance >= 0f;
                bolt.enabled = visible;
                halo.enabled = visible;
                bolt.transform.localPosition = halo.transform.localPosition = center;
                bolt.transform.localRotation = halo.transform.localRotation = angle;
                bolt.transform.localScale = new Vector3(delta.magnitude, 0.027f * fade + 0.008f, 1f);
                halo.transform.localScale = new Vector3(delta.magnitude + 0.025f, 0.095f * fade, 1f);
                bolt.color = new Color(0.97f, 1f, 0.82f, fade);
                halo.color = new Color(0.68f, 1f, 0.20f, fade * 0.32f);
                previous = point;
            }
        }
        private void UpdateSignalPipes()
        {
            if (world.Devices == null)
                return;

            foreach (var circuit in pipeCircuits)
            {
                int source = FindPipeSource(circuit.Source);
                bool active = circuit.Source == "legacyPlate" ? world.PortalEnabled :
                    source >= 0 && !world.Devices[source].Removed && world.Devices[source].Active;
                bool powered = active != circuit.Inverted;
                if (powered != circuit.Powered)
                {
                    circuit.Powered = powered;
                    circuit.Started = Time.unscaledTime;
                    foreach (var tile in circuit.Tiles)
                        tile.view.sprite = powered ? tile.on : tile.off;
                }

                foreach (var pulse in circuit.Pulses)
                {
                    float elapsed = Time.unscaledTime - circuit.Started;
                    float distance = Mathf.Repeat(elapsed * 3f, pulse.Path.Count - 1 + 1.5f);
                    bool visible = powered && !effectsReduced && distance < pulse.Path.Count - 1;
                    if (pulse.Root.activeSelf != visible)
                        pulse.Root.SetActive(visible);
                    if (visible)
                        RenderPipeDischarge(pulse, distance, elapsed);
                }
            }
        }
    }
}