using System.Collections;
using TMPro;
using UnityEngine;

namespace RobotRun
{
    public sealed partial class RoomView
    {
        private const int PortalColumns = 4;
        private const int PortalFrameCount = PortalColumns * PortalColumns;
        private const float PortalFrameSeconds = 0.08f;
        private const float PortalPixelsPerCell = 144f;
        private static readonly Vector2 PortalPivot = new(0.5f, 56f / 256f);

        private SpriteRenderer[] portalViews;
        private TextMeshPro[] portalBadges;
        private SpriteRenderer legacyPlate;
        private TextMeshPro legacyPlateLabel;
        private Sprite portalOff;
        private Sprite[] portalOn;
        private bool portalActive;
        private float portalStarted;
        private int displayedPortalFrame = -2;

        private void BuildPortals()
        {
            const string folder = "Mockup/portali/";
            var offTexture = Resources.Load<Texture2D>(folder + "portale-spento");
            var sheet = Resources.Load<Texture2D>(folder + "portale-acceso-loop-4x4");
            portalOff = Sprite.Create(offTexture, new Rect(0, 0, offTexture.width, offTexture.height),
                PortalPivot, PortalPixelsPerCell, 0, SpriteMeshType.FullRect);
            portalOn = new Sprite[PortalFrameCount];
            for (int index = 0; index < portalOn.Length; index++)
            {
                portalOn[index] = Sprite.Create(sheet, PortalFrameRect(index, sheet.width / PortalColumns),
                    PortalPivot, PortalPixelsPerCell, 0, SpriteMeshType.FullRect);
            }

            portalViews = new SpriteRenderer[2];
            portalBadges = new TextMeshPro[2];
            BuildPortalEnd(0, level.PortalA, "A");
            BuildPortalEnd(1, level.PortalB, "B");
            if (level.HasLegacyPlate)
            {
                var plate = Layer("Pedana monouso del portale", level.PortalPlate);
                legacyPlate = Artwork(plate, "Sprite", Vector2.zero, 7);
                legacyPlateLabel = WorldLabel(plate, "Etichetta pedana", Vector2.up * 0.62f,
                    new Vector2(1.4f, 0.20f), 1.15f);
            }

            SetPortalEnabled(false);
        }

        private void BuildPortalEnd(int index, Vector2Int cell, string letter)
        {
            var portal = Layer($"Portale {letter}", cell);
            portalViews[index] = Artwork(portal, "Sprite", Vector2.down * 0.5f, 6);
            portalViews[index].flipX = index == 1;
            var badgeCenter = Vector2.up * 0.67f;
            Block(portal, "Fondo etichetta", badgeCenter, new Vector2(0.40f, 0.21f), MockupTheme.Navy, 9);
            portalBadges[index] = WorldLabel(portal, "Estremità della coppia", badgeCenter,
                new Vector2(0.40f, 0.21f), 1.45f);
            portalBadges[index].text = letter;
        }

        internal static Rect PortalFrameRect(int index, int tileSize) => new(
            index % PortalColumns * tileSize, (PortalColumns - 1 - index / PortalColumns) * tileSize,
            tileSize, tileSize);

        internal static int PortalFrameAt(float elapsed) =>
            Mathf.FloorToInt(elapsed / PortalFrameSeconds) % PortalFrameCount;

        private void SetPortalEnabled(bool enabled)
        {
            if (portalViews == null)
                return;

            if (portalActive != enabled)
                portalStarted = Time.unscaledTime;

            portalActive = enabled;
            foreach (var badge in portalBadges)
                badge.color = enabled ? MockupTheme.Portal : MockupTheme.Muted;

            if (legacyPlate != null)
            {
                legacyPlate.sprite = DeviceSprites.OneShot(enabled);
                legacyPlateLabel.text = enabled ? "PEDANA ATTIVA" : "PEDANA";
                legacyPlateLabel.color = enabled ? MockupTheme.Portal : new Color32(255, 202, 113, 255);
            }

            UpdatePortalAnimation();
        }

        private void UpdatePortalAnimation()
        {
            if (portalViews == null)
                return;

            int frame = portalActive ? (effectsReduced ? 0 : PortalFrameAt(Time.unscaledTime - portalStarted)) : -1;
            if (frame == displayedPortalFrame)
                return;

            displayedPortalFrame = frame;
            var sprite = frame < 0 ? portalOff : portalOn[frame];
            foreach (var portal in portalViews)
                portal.sprite = sprite;
        }

        private void ShowPortalTransfer(SimulationEvent motion)
        {
            if (motion.Kind == MotionKind.Teleport)
                StartCoroutine(PortalTrace(motion.From, motion.To, 0.72f));
        }

        private IEnumerator PortalTrace(Vector2 from, Vector2 to, float archHeight)
        {
            var trace = new GameObject("Scia del portale").transform;
            trace.SetParent(effects, false);
            int count = Mathf.Clamp(Mathf.CeilToInt(Vector2.Distance(from, to) * 9f), 8, 65);
            var dots = new SpriteRenderer[count];
            for (int i = 0; i < count; i++)
            {
                float t = (i + 1f) / (count + 1f);
                var point = Vector2.Lerp(from, to, t) + Vector2.up * (4f * t * (1f - t) * archHeight);
                dots[i] = Block(trace, "Tratto luminoso", point, Vector2.one * 0.038f, MockupTheme.Portal, 11);
            }

            const float duration = 0.38f;
            for (float elapsed = 0; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                var tint = MockupTheme.Portal;
                tint.a = effectsReduced ? 0.70f : Mathf.Lerp(1f, 0f, elapsed / duration);
                foreach (var dot in dots)
                    dot.color = tint;
                yield return null;
            }

            Destroy(trace.gameObject);
        }

        private void DestroyPortalSprites()
        {
            if (portalOff != null)
                Destroy(portalOff);
            if (portalOn != null)
                foreach (var frame in portalOn)
                    if (frame != null) Destroy(frame);
        }
    }
}
