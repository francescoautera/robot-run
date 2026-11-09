using System.Collections;
using UnityEngine;

namespace RobotRun
{
    public sealed partial class RoomView
    {
        private sealed class BeltAnimation
        {
            private const int ArrowCount = 4;
            private const float TrackWidth = 0.84f;
            private static readonly Color ArrowColor = new Color32(125, 222, 254, 255);
            private readonly GameObject track;
            private readonly Transform[] arrows = new Transform[ArrowCount];
            private readonly SpriteRenderer[,] strokes = new SpriteRenderer[ArrowCount, 2];
            private readonly int direction;

            public BeltAnimation(RoomView room, Transform parent, int direction)
            {
                this.direction = direction;
                track = new GameObject("Animated belt tread");
                track.transform.SetParent(parent, false);
                // Replace the fixed arrows inside the artwork; its frame and rollers stay visible.
                room.Block(track.transform, "Tread strip", new Vector2(0, -0.273f), new Vector2(0.86f, 0.171f),
                    new Color32(35, 84, 105, 255), 6);

                for (int i = 0; i < ArrowCount; i++)
                {
                    var arrow = new GameObject("Moving chevron").transform;
                    arrow.SetParent(track.transform, false);
                    arrows[i] = arrow;
                    for (int part = 0; part < 2; part++)
                    {
                        var stroke = room.Block(arrow, "Chevron stroke", Vector2.zero,
                            new Vector2(0.10f, 0.024f), ArrowColor, 7);
                        stroke.transform.localPosition = new Vector2(-0.026f, part == 0 ? 0.028f : -0.028f);
                        stroke.transform.localRotation = Quaternion.Euler(0, 0, part == 0 ? -38f : 38f);
                        strokes[i, part] = stroke;
                    }
                }

                track.SetActive(false);
            }

            public void Render(bool active, float time)
            {
                if (track.activeSelf != active)
                    track.SetActive(active);
                if (!active)
                    return;

                const float edge = TrackWidth / 2f;
                for (int i = 0; i < arrows.Length; i++)
                {
                    float x = Mathf.Repeat(time * 0.80f + i * TrackWidth / ArrowCount, TrackWidth) - edge;
                    float visibility = Mathf.Clamp01((edge - Mathf.Abs(x)) / 0.10f);
                    arrows[i].localPosition = new Vector2(x * direction, -0.273f);
                    arrows[i].localScale = new Vector3(direction * visibility, visibility, 1f);
                    var tint = ArrowColor;
                    tint.a = visibility;
                    for (int part = 0; part < 2; part++)
                        strokes[i, part].color = tint;
                }
            }
        }

        private Texture2D dustTexture;
        private Sprite dustSprite;

        private void UpdateBeltAnimations()
        {
            if (world.Devices == null)
                return;

            for (int i = 0; i < deviceViews.Length; i++)
                deviceViews[i].Belt?.Render(world.Devices[i].Active && !effectsReduced, Time.unscaledTime);
        }

        private void ShowStepDust(SimulationEvent motion)
        {
            if (motion.Kind != MotionKind.Move)
                return;

            StartCoroutine(StepDust(motion.From, Mathf.Sign(motion.To.x - motion.From.x)));
        }

        private IEnumerator StepDust(Vector2Int cell, float direction)
        {
            var origin = (Vector2)cell + new Vector2(-0.11f * direction, -0.40f);
            var cloud = Layer("Polvere del passo", origin);
            cloud.SetParent(effects, false);
            var sprite = GetDustSprite();
            var tint = new Color(0.72f, 0.81f, 0.86f);
            var puffs = new[]
            {
                Artwork(cloud, "Puff", new Vector2(-0.085f * direction, 0.014f), 11),
                Artwork(cloud, "Puff", new Vector2(0f, 0.035f), 11),
                Artwork(cloud, "Puff", new Vector2(0.085f * direction, 0.010f), 11)
            };
            for (int i = 0; i < puffs.Length; i++)
            {
                puffs[i].sprite = sprite;
                puffs[i].transform.localScale = Vector3.one * (i == 1 ? 0.17f : 0.12f);
            }

            const float duration = 0.19f;
            for (float elapsed = 0; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                float t = elapsed / duration;
                if (!effectsReduced)
                {
                    cloud.localPosition = origin + new Vector2(-0.12f * direction, 0.055f) * t;
                    cloud.localScale = Vector3.one * Mathf.Lerp(0.78f, 1.30f, t);
                }

                tint.a = 0.60f * (1f - t) * (1f - t);
                foreach (var puff in puffs)
                    puff.color = tint;
                yield return null;
            }

            Destroy(cloud.gameObject);
        }

        private Sprite GetDustSprite()
        {
            if (dustSprite != null)
                return dustSprite;

            const int size = 24;
            var colors = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    var offset = new Vector2(x + 0.5f - size / 2f, y + 0.5f - size / 2f);
                    float opacity = Mathf.Clamp01((1f - offset.magnitude / (size / 2f)) * 3f);
                    colors[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(opacity * 255));
                }
            }

            dustTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            dustTexture.wrapMode = TextureWrapMode.Clamp;
            dustTexture.SetPixels32(colors);
            dustTexture.Apply(false, true);
            dustSprite = Sprite.Create(dustTexture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size,
                0, SpriteMeshType.FullRect);
            return dustSprite;
        }

        private void DestroyMotionEffects()
        {
            if (dustSprite != null) Destroy(dustSprite);
            if (dustTexture != null) Destroy(dustTexture);
        }
    }
}
