using UnityEngine;
using Palinodia.Simulation;

namespace Palinodia.Presentation
{
    /// <summary>Heat source remains unmistakable even before the player learns the controls.</summary>
    public sealed class EnergyCoreVisual : MonoBehaviour
    {
        private PhysicalBodyState source;
        private ParticleSystem embers;
        private LineRenderer lowerRing;
        private LineRenderer upperRing;
        private Material material;
        private Light radiance;
        private float phase;

        public void Initialize(PhysicalBodyState heatSource)
        {
            source = heatSource;
            material = EffectsUtility.MakeGlowMaterial(Color.white);
            lowerRing = EffectsUtility.MakeLine(transform, "VFX / thermal containment band lower", material, .065f, true);
            upperRing = EffectsUtility.MakeLine(transform, "VFX / thermal containment band upper", material, .042f, true);
            embers = EffectsUtility.MakeParticles(transform, "VFX / thermal embers", material, 1.2f, .18f);
            embers.Play();
            GameObject lamp = new GameObject("VFX / responsive core glow");
            lamp.transform.SetParent(transform, false);
            radiance = lamp.AddComponent<Light>();
            radiance.type = LightType.Point;
            radiance.color = new Color(1f, .37f, .09f);
            radiance.range = 6.5f;
            radiance.shadows = LightShadows.None;
        }

        private void Update()
        {
            if (source == null || embers == null) return;
            phase += Time.deltaTime;
            float heat = Mathf.Clamp01((float)((source.TemperatureC - source.MinExtractC) / 200.0));
            float visible = .25f + .75f * heat;
            Vector3 center = transform.position;
            EffectsUtility.DrawRing(lowerRing, center + Vector3.up * -.18f, 1.28f, .06f, phase * 1.3f);
            EffectsUtility.DrawRing(upperRing, center + Vector3.up * .32f, 1.2f, .07f, -phase);
            Color glow = new Color(1f, .45f + heat * .35f, .14f, .4f + visible * .6f);
            lowerRing.startColor = lowerRing.endColor = glow;
            upperRing.startColor = upperRing.endColor = glow;
            radiance.intensity = 1.3f + heat * 4f + Mathf.Sin(phase * 6f) * .22f;
            if (heat <= .02f || Time.frameCount % 2 != 0) return;
            var p = new ParticleSystem.EmitParams
            {
                position = center + new Vector3(Random.Range(-.8f, .8f), Random.Range(-.3f, .3f), Random.Range(-.8f, .8f)),
                velocity = new Vector3(Random.Range(-.3f, .3f), Random.Range(.5f, 1.5f), Random.Range(-.3f, .3f)),
                startLifetime = Random.Range(.55f, 1.3f),
                startSize = Random.Range(.09f, .25f) * visible,
                startColor = Color.Lerp(new Color(1f, .23f, .04f, .7f), new Color(1f, .9f, .4f, .9f), Random.value)
            };
            embers.Emit(p, Mathf.Max(1, Mathf.RoundToInt(4 * heat)));
        }

        private void OnDestroy()
        {
            if (material != null) Destroy(material);
        }
    }
}
