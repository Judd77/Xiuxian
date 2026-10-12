using UnityEngine;
using UnityEngine.UI;
using WenXinJie.Narrative;

namespace WenXinJie.Presentation
{
    // Drawn at the UI scale so the observation stays sharp at every window size.
    [RequireComponent(typeof(CanvasRenderer))]
    internal sealed class ObservationInk : MaskableGraphic
    {
        private static readonly Color Jade = new(0.42f, 0.84f, 0.70f);
        private static readonly Color Gold = new(0.98f, 0.76f, 0.35f);
        private static readonly Color Ink = new(0.12f, 0.24f, 0.22f, 0.80f);
        private StoryObservation _observation;
        private float _elapsed;

        public void Present(StoryObservation observation)
        {
            _observation = observation;
            _elapsed = 0f;
            SetVerticesDirty();
        }

        private void Update()
        {
            if (_observation == null || _elapsed >= 2.2f)
            {
                return;
            }
            _elapsed = Mathf.Min(2.2f, _elapsed + Time.unscaledDeltaTime);
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (_observation == null)
            {
                return;
            }
            if (_observation.kind == "residue")
            {
                Sample(mesh, 46f, _observation.leftStrength);
                Sample(mesh, 528f, _observation.rightStrength);
                return;
            }

            var source = new Vector2(435f, 45f);
            var left = new Vector2(192f, 166f);
            var right = new Vector2(674f, 166f);
            var flow = _observation.flow;
            Path(mesh, source, left, new(0.42f, 0.65f, 0.58f, 0.32f));
            Path(mesh, source, right, new(0.42f, 0.65f, 0.58f, 0.32f));
            Ring(mesh, source, 30f, 2f, new(0.67f, 0.76f, 0.70f, 0.8f));
            Rune(mesh, source, 0.5f, Ink);
            var first = flow == "villageFirst" || flow == "villageWarning" || flow == "diverted";
            var leftPower = first ? Mathf.Clamp01((_elapsed - 0.25f) / 0.5f) : 0f;
            var rightPower = flow == "villageFirst" ? Mathf.Clamp01((_elapsed - 0.9f) / 0.5f) : 0f;
            Glow(mesh, left, leftPower, Gold);
            Glow(mesh, right, rightPower, Jade);
            Ring(mesh, left, 42f, 2f, Color.Lerp(new(0.44f, 0.60f, 0.55f), Gold, leftPower));
            Ring(mesh, right, 42f, 2f, Color.Lerp(new(0.44f, 0.60f, 0.55f), Jade, rightPower));
            Rune(mesh, left, 0.7f, Color.Lerp(Ink, Gold, leftPower));
            Rune(mesh, right, 0.7f, Color.Lerp(Ink, Jade, rightPower));
            if (first)
            {
                Spark(mesh, source, left, _elapsed / 0.75f, Gold);
            }
            if (flow == "villageFirst")
            {
                Spark(mesh, source, right, (_elapsed - 0.6f) / 0.75f, Jade);
            }
            if (flow == "closed")
            {
                Line(mesh, source + new Vector2(-17f, -17f), source + new Vector2(17f, 17f), 4f, Gold);
                Line(mesh, source + new Vector2(17f, -17f), source + new Vector2(-17f, 17f), 4f, Gold);
            }
        }

        private void Sample(VertexHelper mesh, float x, float strength)
        {
            Quad(mesh, new(x, 8f), new(x + 292f, 8f), new(x + 282f, 214f), new(x + 10f, 218f),
                new(0.88f, 0.89f, 0.77f, 0.98f));
            Line(mesh, new(x + 18f, 18f), new(x + 276f, 18f), 1.5f, new(0.58f, 0.53f, 0.34f, 0.65f));
            Line(mesh, new(x + 22f, 202f), new(x + 270f, 202f), 1.5f, new(0.58f, 0.53f, 0.34f, 0.65f));
            var center = new Vector2(x + 146f, 128f);
            if (_observation.sample == "ash")
            {
                for (var index = 0; index < 36; index++)
                {
                    var angle = index * 2.4f;
                    var point = center + new Vector2(Mathf.Cos(angle) * (12f + index),
                        Mathf.Sin(angle) * (9f + index * 0.45f));
                    Quad(mesh, point + new Vector2(-5f, -3f), point + new Vector2(3f, -5f),
                        point + new Vector2(6f, 3f), point + new Vector2(-4f, 4f),
                        new(0.20f, 0.23f, 0.21f, 0.72f));
                }
            }
            else
            {
                Rune(mesh, center, 1f, Ink);
            }
            var power = strength * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_elapsed / 0.85f));
            Glow(mesh, center, power, Gold);
            if (power > 0f)
            {
                Ring(mesh, center, 47f + 6f * power, 2f, new(0.85f, 0.47f, 0.12f, power));
                if (_observation.sample != "ash")
                {
                    Rune(mesh, center, 1f, new(0.70f, 0.34f, 0.08f, power));
                }
            }
        }

        private void Glow(VertexHelper mesh, Vector2 center, float power, Color tint)
        {
            if (power <= 0f)
            {
                return;
            }
            for (var ring = 0; ring < 4; ring++)
            {
                tint.a = power * (0.18f - ring * 0.035f);
                Ring(mesh, center, 28f + ring * 11f, 10f, tint);
            }
        }

        private void Rune(VertexHelper mesh, Vector2 center, float scale, Color tint)
        {
            Vector2 P(float x, float y) => center + new Vector2(x, y) * scale;
            Line(mesh, P(-25f, -25f), P(25f, -25f), 3f, tint);
            Line(mesh, P(0f, -38f), P(0f, 30f), 3f, tint);
            Line(mesh, P(-20f, -9f), P(22f, 4f), 3f, tint);
            Line(mesh, P(22f, 4f), P(-20f, 20f), 3f, tint);
            Line(mesh, P(-20f, 20f), P(17f, 36f), 3f, tint);
            Line(mesh, P(-32f, 34f), P(-19f, 6f), 2f, tint);
        }

        private void Path(VertexHelper mesh, Vector2 from, Vector2 to, Color tint)
        {
            var previous = from;
            for (var index = 1; index <= 24; index++)
            {
                var point = PathPoint(from, to, index / 24f);
                Line(mesh, previous, point, 3f, tint);
                previous = point;
            }
        }

        private static Vector2 PathPoint(Vector2 from, Vector2 to, float t)
        {
            var bend = new Vector2(to.x, from.y);
            return (1f - t) * (1f - t) * from + 2f * (1f - t) * t * bend + t * t * to;
        }

        private void Spark(VertexHelper mesh, Vector2 from, Vector2 to, float t, Color tint)
        {
            if (t < 0f || t > 1f)
            {
                return;
            }
            Ring(mesh, PathPoint(from, to, t), 7f, 5f, tint);
        }

        private void Ring(VertexHelper mesh, Vector2 center, float radius, float width, Color tint)
        {
            for (var index = 0; index < 48; index++)
            {
                var a = index * Mathf.PI * 2f / 48f;
                var b = (index + 1) * Mathf.PI * 2f / 48f;
                var inner = radius - width * 0.5f;
                var outer = radius + width * 0.5f;
                var first = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                var second = new Vector2(Mathf.Cos(b), Mathf.Sin(b));
                Quad(mesh, center + first * inner, center + first * outer,
                    center + second * outer, center + second * inner, tint);
            }
        }

        private void Line(VertexHelper mesh, Vector2 from, Vector2 to, float width, Color tint)
        {
            var delta = (to - from).normalized;
            var normal = new Vector2(-delta.y, delta.x) * width * 0.5f;
            Quad(mesh, from - normal, from + normal, to + normal, to - normal, tint);
        }

        private void Quad(VertexHelper mesh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color tint)
        {
            var index = mesh.currentVertCount;
            Vector2 Position(Vector2 point) => new(rectTransform.rect.xMin + point.x, rectTransform.rect.yMax - point.y);
            mesh.AddVert(Position(a), tint, Vector2.zero);
            mesh.AddVert(Position(b), tint, Vector2.zero);
            mesh.AddVert(Position(c), tint, Vector2.zero);
            mesh.AddVert(Position(d), tint, Vector2.zero);
            mesh.AddTriangle(index, index + 1, index + 2);
            mesh.AddTriangle(index, index + 2, index + 3);
        }
    }
}
