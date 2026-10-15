using UnityEngine;

namespace Palinodia.Presentation
{
    /// <summary>Concentric ionized shockfronts, directed sparks and short-lived glow for finite-energy pulses.</summary>
    public sealed class PulseVisual : MonoBehaviour
    {
        private LineRenderer outer;
        private LineRenderer inner;
        private Light flash;
        private Material sharedGlow;
        private float lifetime;
        private float radius;
        private Color color;

        public static void Spawn(Vector3 position, bool thermal, float size)
        {
            GameObject fx = new GameObject(thermal ? "VFX / thermal bloom" : "VFX / kinetic shockwave");
            fx.transform.position = position;
            PulseVisual pulse = fx.AddComponent<PulseVisual>();
            pulse.Configure(thermal, size);
            Destroy(fx, .6f);
        }

        private void Configure(bool thermal, float size)
        {
            radius = size;
            color = thermal ? new Color(1f, .48f, .1f, 1f) : new Color(.18f, .88f, 1f, 1f);
            sharedGlow = EffectsUtility.MakeGlowMaterial(Color.white);
            outer = EffectsUtility.MakeLine(transform, "Expanding shockfront", sharedGlow, thermal ? .15f : .11f, true);
            inner = EffectsUtility.MakeLine(transform, "Secondary plasma shockfront", sharedGlow, .045f, true);
            ParticleSystem sparks = EffectsUtility.MakeParticles(transform, "Radial spark shower", sharedGlow, .5f, .15f);
            sparks.Play();
            for (int i = 0; i < 72; i++)
            {
                Vector3 direction = Random.onUnitSphere;
                Vector3 displacement = (direction + Vector3.up * .12f).normalized;
                var p = new ParticleSystem.EmitParams
                {
                    position = transform.position + direction * .08f,
                    velocity = displacement * Random.Range(2.0f, thermal ? 8.5f : 12.0f),
                    startLifetime = Random.Range(.18f, .53f),
                    startSize = Random.Range(.055f, .2f),
                    startColor = Color.Lerp(color, Color.white, Random.Range(0f, .75f))
                };
                sparks.Emit(p, 1);
            }
            flash = gameObject.AddComponent<Light>();
            flash.type = LightType.Point;
            flash.color = color;
            flash.intensity = thermal ? 5f : 3.7f;
            flash.range = size * 3;
            flash.shadows = LightShadows.None;
        }

        private void Update()
        {
            lifetime += Time.deltaTime;
            float t = Mathf.Clamp01(lifetime / .47f);
            float expanded = Mathf.Lerp(.12f, radius, 1f - (1f - t) * (1f - t));
            EffectsUtility.DrawRing(outer, transform.position, expanded, .06f, 0f);
            EffectsUtility.DrawRing(inner, transform.position, expanded * .77f, 0f, .6f, true);
            Color fading = color;
            fading.a = (1f - t) * .9f;
            outer.startColor = outer.endColor = fading;
            fading.a *= .6f;
            inner.startColor = inner.endColor = fading;
            if (flash != null) flash.intensity = 4.5f * (1f - t) * (1f - t);
        }

        private void OnDestroy()
        {
            if (sharedGlow != null) Destroy(sharedGlow);
        }
    }
}
