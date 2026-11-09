
using UnityEngine;

namespace RobotRun
{
    internal static class MockupTheme
    {
        internal static readonly Color Navy = new(0.049f, 0.083f, 0.135f);
        internal static readonly Color Panel = new(0.075f, 0.119f, 0.173f);
        internal static readonly Color Border = new(0.27f, 0.37f, 0.45f);
        internal static readonly Color Ink = new(0.92f, 0.96f, 1f);
        internal static readonly Color Muted = new(0.68f, 0.77f, 0.84f);
        internal static readonly Color Mint = new(0.43f, 0.96f, 0.76f);
        internal static readonly Color Cyan = new(0.47f, 0.88f, 1f);
        internal static readonly Color Coral = new(1f, 0.62f, 0.52f);
        internal static readonly Color Portal = new Color32(197, 250, 94, 255);

        private static readonly Texture2D[] CardIcons = new Texture2D[6];
        private static Sprite rounded;
        private static Sprite glow;

        internal static Texture2D CardArt(CardInstruction card)
        {
            bool left = card.Direction < 0;
            int family = card.Type == CardType.Jump ? 0 : card.Value == 4 ? 1 : 2;
            int index = family * 2 + (left ? 1 : 0);
            if (CardIcons[index] == null)
            {
                string icon = family == 0 ? "jump-1" : family == 1 ? "move-4" : "move-1";
                string direction = left ? "sinistra" : "destra";
                CardIcons[index] = Resources.Load<Texture2D>($"Mockup/icone/{icon}-{direction}");
            }

            return CardIcons[index];
        }

        internal static void Round(UnityEngine.UI.Image image)
        {
            if (rounded == null)
                rounded = CreateRoundedSprite();

            image.sprite = rounded;
            image.type = UnityEngine.UI.Image.Type.Sliced;
        }

        internal static void Glow(UnityEngine.UI.Image image)
        {
            if (glow == null)
                glow = CreateRoundedSprite(halo: true);

            image.sprite = glow;
            image.type = UnityEngine.UI.Image.Type.Sliced;
        }

        private static Sprite CreateRoundedSprite(bool halo = false)
        {
            int size = halo ? 32 : 24;
            const float radius = 7;
            float inset = halo ? 4f : 0f;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(0, Mathf.Abs(x + 0.5f - size / 2f) - (size / 2f - radius - inset));
                    float dy = Mathf.Max(0, Mathf.Abs(y + 0.5f - size / 2f) - (size / 2f - radius - inset));
                    float distance = Mathf.Sqrt(dx * dx + dy * dy) - radius;
                    float alpha = halo ? Mathf.Clamp01(1f - Mathf.Abs(distance) / 4f)
                        : Mathf.Clamp01(0.5f - distance);
                    if (halo)
                        alpha *= alpha;
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255));
                }
            }

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.hideFlags = HideFlags.HideAndDontSave;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f),
                100, 0, SpriteMeshType.FullRect, Vector4.one * (halo ? 12 : 8));
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
    }
}
