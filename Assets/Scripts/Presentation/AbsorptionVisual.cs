using UnityEngine;
using Palinodia.Gameplay;
using Palinodia.Simulation;

namespace Palinodia.Presentation
{
    /// <summary>
    /// A visible target halo, braided energy stream moving SOURCE -> PLAYER, and arrival sparks.
    /// No gameplay energy is generated here: visual flow requires a successful transfer receipt.
    /// </summary>
    public sealed class AbsorptionVisual : MonoBehaviour
    {
        private PlayerActor player;
        private PhysicalBodyState focused;
        private LineRenderer targetRing;
        private readonly LineRenderer[] streams = new LineRenderer[3];
        private ParticleSystem incoming;
        private LineRenderer chestHalo;
        private Material glowMaterial;
        private float flowUntil;
        private float flowAmount;
        private float ringPhase;
        private bool inRange;
        private float emitterAccumulator;

        public void Initialize(PlayerActor source)
        {
            player = source;
            glowMaterial = EffectsUtility.MakeGlowMaterial(Color.white);
            targetRing = EffectsUtility.MakeLine(transform, "VFX / absorbable object indicator", glowMaterial, .065f, true);
            for (int i = 0; i < streams.Length; i++)
            {
                streams[i] = EffectsUtility.MakeLine(transform, "VFX / energy stream " + i,
                    glowMaterial, i == 0 ? .115f : .045f);
                streams[i].positionCount = 28;
            }
            chestHalo = EffectsUtility.MakeLine(transform, "VFX / absorption halo at chest", glowMaterial, .055f, true);
            incoming = EffectsUtility.MakeParticles(transform, "VFX / travelling intake sparks", glowMaterial, .38f, .15f);
            incoming.Play();
            HideAll();
        }

        public void SetFocus(PhysicalBodyState body, bool eligible)
        {
            focused = body;
            inRange = eligible;
        }

        public void Transfer(PhysicalBodyState source, double creditedJ)
        {
            if (source == null || creditedJ <= 0) return;
            focused = source;
            inRange = true;
            flowUntil = Time.time + .14f;
            flowAmount = Mathf.Clamp01((float)creditedJ / 1000f);
        }

        private void LateUpdate()
        {
            if (player == null || targetRing == null) return;
            bool hasTarget = focused != null && focused.MaxRemovableHeatJ > 0;
            targetRing.enabled = hasTarget;
            if (hasTarget)
            {
                ringPhase += Time.deltaTime * (inRange ? 1.9f : .7f);
                Collider sourceCollider = focused.GetComponent<Collider>();
                Bounds bounds = sourceCollider != null ? sourceCollider.bounds : new Bounds(focused.transform.position, Vector3.one);
                Vector3 ringCenter = new Vector3(bounds.center.x, bounds.max.y + .2f, bounds.center.z);
                float radius = Mathf.Max(.65f, Mathf.Max(bounds.extents.x, bounds.extents.z) + .28f);
                Color marker = inRange ? new Color(.24f, 1f, .83f, .95f) : new Color(1f, .69f, .25f, .8f);
                targetRing.startColor = targetRing.endColor = marker;
                EffectsUtility.DrawRing(targetRing, ringCenter, radius, .045f, ringPhase);
            }

            bool streaming = hasTarget && inRange && Time.time < flowUntil;
            chestHalo.enabled = streaming;
            for (int i = 0; i < streams.Length; i++) streams[i].enabled = streaming;
            if (streaming)
            {
                chestHalo.startColor = chestHalo.endColor = new Color(.2f, .97f, 1f, .86f);
                EffectsUtility.DrawRing(chestHalo, player.ChestPosition, .55f, .04f, ringPhase * 3f, true);
            }
            if (!streaming) return;

            Vector3 from = focused.transform.position + Vector3.up * .16f;
            Vector3 to = player.ChestPosition;
            Vector3 axis = to - from;
            Vector3 tangent = Vector3.Cross(axis.normalized, Vector3.up);
            if (tangent.sqrMagnitude < .001f) tangent = Vector3.right;
            tangent.Normalize();
            Vector3 normal = Vector3.Cross(axis.normalized, tangent).normalized;
            for (int s = 0; s < streams.Length; s++)
            {
                LineRenderer line = streams[s];
                Color intakeColor = s == 0 ? new Color(.37f, .97f, 1f, .96f) :
                    new Color(1f, .68f, .3f, .78f);
                line.startColor = intakeColor;
                line.endColor = new Color(.24f, 1f, .97f, .95f);
                for (int i = 0; i < line.positionCount; i++)
                {
                    float t = i / (float)(line.positionCount - 1);
                    float phase = ringPhase * 13f + t * 15f + s * 2.094f;
                    float envelope = Mathf.Sin(Mathf.PI * t) * (.19f + flowAmount * .15f);
                    line.SetPosition(i, Vector3.Lerp(from, to, t) +
                        (tangent * Mathf.Sin(phase) + normal * Mathf.Cos(phase)) * envelope);
                }
            }
            emitterAccumulator += Time.deltaTime * 95f;
            while (emitterAccumulator >= 1f)
            {
                emitterAccumulator -= 1f;
                var particle = new ParticleSystem.EmitParams
                {
                    position = from + Random.insideUnitSphere * .3f,
                    velocity = (to - from) / .38f + Random.insideUnitSphere * .8f,
                    startLifetime = .38f,
                    startSize = Random.Range(.09f, .2f),
                    startColor = Color.Lerp(new Color(1f, .5f, .14f), new Color(.26f, .95f, 1f), Random.value)
                };
                incoming.Emit(particle, 1);
            }
        }

        private void HideAll()
        {
            if (targetRing != null) targetRing.enabled = false;
            if (chestHalo != null) chestHalo.enabled = false;
            foreach (LineRenderer line in streams) if (line != null) line.enabled = false;
        }

        private void OnDestroy()
        {
            if (glowMaterial != null) Destroy(glowMaterial);
        }
    }
}
