
using UnityEngine;

namespace RobotRun
{
    internal enum ButtonIconKind { Play, Retry }

    internal sealed class ButtonIcon : UnityEngine.UI.MaskableGraphic
    {
        private ButtonIconKind kind;

        internal void SetKind(ButtonIconKind icon)
        {
            kind = icon;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
        {
            mesh.Clear();
            var rect = GetPixelAdjustedRect();
            float size = Mathf.Min(rect.width, rect.height);
            if (size <= 0f)
                return;

            var center = rect.center;
            if (kind == ButtonIconKind.Play)
            {
                AddTriangle(mesh, center + new Vector2(-0.23f, 0.33f) * size,
                    center + new Vector2(0.33f, 0f) * size, center + new Vector2(-0.23f, -0.33f) * size);
                return;
            }

            const int segments = 20;
            for (int i = 0; i <= segments; i++)
            {
                var radial = Radial(Mathf.Lerp(174f, 480f, i / (float)segments));
                mesh.AddVert(center + radial * (0.35f * size), color, Vector2.zero);
                mesh.AddVert(center + radial * (0.26f * size), color, Vector2.zero);
                if (i == 0)
                    continue;

                int vertex = i * 2;
                mesh.AddTriangle(vertex - 2, vertex - 1, vertex);
                mesh.AddTriangle(vertex, vertex - 1, vertex + 1);
            }

            var end = Radial(480f);
            var tangent = new Vector2(-end.y, end.x);
            var arrow = center + end * (0.305f * size);
            var arrowBase = arrow - tangent * (0.09f * size);
            AddTriangle(mesh, arrow + tangent * (0.15f * size),
                arrowBase + end * (0.15f * size), arrowBase - end * (0.15f * size));
        }

        private static Vector2 Radial(float degrees)
        {
            float angle = degrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }

        private void AddTriangle(UnityEngine.UI.VertexHelper mesh, Vector2 a, Vector2 b, Vector2 c)
        {
            int vertex = mesh.currentVertCount;
            mesh.AddVert(a, color, Vector2.zero);
            mesh.AddVert(b, color, Vector2.zero);
            mesh.AddVert(c, color, Vector2.zero);
            mesh.AddTriangle(vertex, vertex + 1, vertex + 2);
        }
    }
}
