using UnityEngine;
using Palinodia.Simulation;

namespace Palinodia.Presentation
{
    /// <summary>Machine scan arcs, hover exhaust and one-time electrical failure shower.</summary>
    public sealed class DroneVisual : MonoBehaviour
    {
        private PhysicalBodyState machine;
        private LineRenderer scan;
        private ParticleSystem exhaust;
        private Material material;
        private float phase;
        private bool failed;

        public void Initialize(PhysicalBodyState body)
        {
            machine = body;
            material = EffectsUtility.MakeGlowMaterial(Color.white);
            scan = EffectsUtility.MakeLine(transform, "VFX / sentry scanner", material, .052f, true);
            exhaust = EffectsUtility.MakeParticles(transform, "VFX / sentry ion thruster", material, .6f, .12f);
            exhaust.Play();
        }

        private void Update()
        {
            if (machine == null || scan == null) return;
            if (!machine.Operational)
            {
                scan.enabled = false;
                if (failed) return;
                failed = true;
                for (int i = 0; i < 55; i++)
                {
                    var spark = new ParticleSystem.EmitParams
                    {
                        position = transform.position + Random.insideUnitSphere * .55f,
                        velocity = Random.onUnitSphere * Random.Range(2.5f, 8f),
                        startLifetime = Random.Range(.2f, .8f),
                        startSize = Random.Range(.07f, .19f),
                        startColor = new Color(.5f, .92f, 1f)
                    };
                    exhaust.Emit(spark, 1);
                }
                return;
            }
            phase += Time.deltaTime;
            scan.startColor = scan.endColor = new Color(1f, .25f, .19f, .8f);
            EffectsUtility.DrawRing(scan, transform.position, .93f, .075f, phase * 2.3f);
            if (Time.frameCount % 2 != 0) return;
            var p = new ParticleSystem.EmitParams
            {
                position = transform.position - Vector3.up * .67f + Random.insideUnitSphere * .15f,
                velocity = -Vector3.up * Random.Range(1f, 2.2f),
                startLifetime = Random.Range(.2f, .54f),
                startSize = Random.Range(.045f, .14f),
                startColor = new Color(.27f, .75f, 1f, .8f)
            };
            exhaust.Emit(p, 2);
        }

        private void OnDestroy()
        {
            if (material != null) Destroy(material);
        }
    }
}
