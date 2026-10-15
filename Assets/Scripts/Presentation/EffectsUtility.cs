using UnityEngine;

namespace Palinodia.Presentation
{
    /// <summary>Runtime VFX primitives; no imported textures, shaders or render pipeline packages required.</summary>
    internal static class EffectsUtility
    {
        public static Material MakeGlowMaterial(Color color)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Legacy Shaders/Particles/Additive");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Standard");
            Material material = new Material(shader);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            return material;
        }

        public static LineRenderer MakeLine(Transform parent, string label, Material material,
            float width, bool loop = false)
        {
            GameObject go = new GameObject(label);
            go.transform.SetParent(parent, false);
            LineRenderer line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.loop = loop;
            line.sharedMaterial = material;
            line.widthMultiplier = width;
            line.numCapVertices = 5;
            line.numCornerVertices = 4;
            line.textureMode = LineTextureMode.Stretch;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            return line;
        }

        public static ParticleSystem MakeParticles(Transform parent, string label, Material material,
            float lifetime, float size)
        {
            GameObject go = new GameObject(label);
            go.transform.SetParent(parent, false);
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.duration = Mathf.Max(.01f, lifetime);
            main.startLifetime = lifetime;
            main.startSpeed = 0;
            main.startSize = size;
            main.maxParticles = 280;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = ps.emission;
            emission.enabled = false;
            var shape = ps.shape;
            shape.enabled = false;
            ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return ps;
        }

        public static void DrawRing(LineRenderer line, Vector3 center, float radius, float height,
            float phase, bool tilted = false)
        {
            const int segments = 64;
            line.positionCount = segments;
            for (int i = 0; i < segments; i++)
            {
                float angle = (i * Mathf.PI * 2 / segments) + phase;
                Vector3 delta = tilted
                    ? new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, Mathf.Sin(angle) * radius * .22f)
                    : new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle * 3 + phase) * height, Mathf.Sin(angle) * radius);
                line.SetPosition(i, center + delta);
            }
        }
    }
}
