
using System;
using UnityEngine;

namespace RobotRun
{
    internal sealed class TutorialGlow
    {
        private readonly RectTransform halo;
        private readonly CanvasGroup opacity;
        private readonly Action<float> setScale;

        internal TutorialGlow(RectTransform target, Color color, Action<float> setScale = null)
        {
            var image = GameCanvasView.Box(target, "Tutorial glow", color, 0, 0, 1, 1, false);
            MockupTheme.Glow(image);
            halo = image.rectTransform;
            halo.offsetMin = -Vector2.one * 5f;
            halo.offsetMax = Vector2.one * 5f;
            opacity = halo.gameObject.AddComponent<CanvasGroup>();
            opacity.blocksRaycasts = false;
            this.setScale = setScale;
            halo.gameObject.SetActive(false);
        }

        internal void Apply(float strength)
        {
            if (!halo.gameObject.activeSelf)
                halo.gameObject.SetActive(true);

            float alpha = Mathf.Lerp(0.44f, 1f, strength);
            if (opacity.alpha != alpha)
                opacity.alpha = alpha;

            ApplyScale(1f + 0.025f * strength);
        }

        internal void Clear()
        {
            halo.gameObject.SetActive(false);
            ApplyScale(1f);
        }

        private void ApplyScale(float scale)
        {
            if (setScale != null)
                setScale(scale);
            else
                halo.localScale = Vector3.one * scale;
        }
    }
}
