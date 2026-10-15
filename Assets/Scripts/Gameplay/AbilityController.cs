using System;
using System.Collections.Generic;
using UnityEngine;
using Palinodia.Simulation;
using Palinodia.Presentation;

namespace Palinodia.Gameplay
{
    /// <summary>Mouse aiming + finite energy conversions. Pulses use one shared budget across all hit bodies.</summary>
    public sealed class AbilityController : MonoBehaviour
    {
        private readonly EnergyReservoir reservoir = new EnergyReservoir(150000, 17000);
        private readonly List<PhysicalBodyState> eligible = new List<PhysicalBodyState>();
        private readonly List<float> weights = new List<float>();
        private readonly HashSet<PhysicalBodyState> unique = new HashSet<PhysicalBodyState>();
        private readonly List<PhysicalBodyState> knownBodies = new List<PhysicalBodyState>();
        private AbsorptionVisual absorptionVfx;
        private FollowCamera cameraRig;
        private PlayerActor actor;
        private StageEnvironmentState stage;
        private float cooldown;
        private string feedback = "Look at the orange core and hold E to harvest heat.";
        private float feedbackUntil;
        private float absorptionUntil;
        private float absorptionRateKJs;
        private const float AbsorbRange = 7.5f;

        public EnergyReservoir Reservoir => reservoir;
        public string Feedback => Time.time < feedbackUntil ? feedback : "";
        public PhysicalBodyState AimedBody { get; private set; }
        public float AimDistance { get; private set; }
        public PhysicalBodyState AbsorptionTarget { get; private set; }
        public PhysicalBodyState ScannedBody => AimedBody != null ? AimedBody : AbsorptionTarget;
        public bool CanAbsorbTarget { get; private set; }
        public bool IsAbsorbing => Time.time < absorptionUntil;
        public float AbsorptionRateKJs => IsAbsorbing ? absorptionRateKJs : 0f;
        public float AbsorptionDistance { get; private set; }

        public void Initialize(PlayerActor source, FollowCamera rig, StageEnvironmentState environment)
        {
            actor = source;
            cameraRig = rig;
            stage = environment;
            knownBodies.Clear();
            knownBodies.AddRange(FindObjectsByType<PhysicalBodyState>(FindObjectsSortMode.None));
            absorptionVfx = gameObject.AddComponent<AbsorptionVisual>();
            absorptionVfx.Initialize(actor);
        }

        private void Update()
        {
            if (actor == null || cameraRig == null || stage == null) return;
            cooldown -= Time.deltaTime;
            bool aimed = CastAim(out RaycastHit hit);
            AimedBody = aimed ? hit.collider.GetComponentInParent<PhysicalBodyState>() : null;
            AimDistance = aimed ? hit.distance : float.PositiveInfinity;

            SelectAbsorptionTarget();
            if (absorptionVfx != null) absorptionVfx.SetFocus(AbsorptionTarget, CanAbsorbTarget);
            if (!actor.Body.Operational || Cursor.lockState != CursorLockMode.Locked) return;
            if (Input.GetKey(KeyCode.E)) Absorb();
            if (Input.GetMouseButtonDown(0) && cooldown <= 0) Emit(false);
            else if (Input.GetMouseButtonDown(1) && cooldown <= 0) Emit(true);
        }

        private bool CastAim(out RaycastHit hit)
        {
            Ray ray = cameraRig.View.ViewportPointToRay(new Vector3(.5f, .5f, 0));
            RaycastHit[] results = Physics.RaycastAll(ray, 34f, ~0, QueryTriggerInteraction.Ignore);
            Array.Sort(results, (a, b) => a.distance.CompareTo(b.distance));
            foreach (RaycastHit candidate in results)
            {
                if (candidate.collider.transform.IsChildOf(actor.transform)) continue;
                hit = candidate;
                return true;
            }
            hit = default;
            return false;
        }

        /// <summary>
        /// Aim assist uses screen-space proximity, but extraction still requires player-side line of sight.
        /// In particular, decorative core rings/pedestals must not steal the camera's one ray hit.
        /// </summary>
        private void SelectAbsorptionTarget()
        {
            AbsorptionTarget = null;
            CanAbsorbTarget = false;
            AbsorptionDistance = float.PositiveInfinity;
            float bestScore = float.PositiveInfinity;
            Camera view = cameraRig.View;
            foreach (PhysicalBodyState body in knownBodies)
            {
                if (body == null || body == actor.Body || body.Category == BodyCategory.Living ||
                    body.MaxRemovableHeatJ < 1) continue;
                Collider collider = body.GetComponent<Collider>();
                Vector3 center = collider != null ? collider.bounds.center : body.transform.position;
                Vector3 viewport = view.WorldToViewportPoint(center);
                if (viewport.z <= 0) continue;
                float x = viewport.x - .5f;
                float y = viewport.y - .5f;
                float aimError = Mathf.Sqrt(x * x + y * y);
                // A core remains discoverable even when the crosshair is slightly off its visible shell.
                if (aimError > (body.Category == BodyCategory.Heater ? .55f : .28f)) continue;
                float distance = Vector3.Distance(actor.ChestPosition, collider != null ?
                    collider.ClosestPoint(actor.ChestPosition) : center);
                if (distance > 24f) continue;
                float score = aimError * 2.5f + distance * .018f;
                if (body == AimedBody) score -= .75f;
                if (body.Category == BodyCategory.Heater) score -= .27f;
                if (score >= bestScore) continue;
                if (!VisibleFromPlayer(body, center)) continue;
                bestScore = score;
                AbsorptionTarget = body;
                AbsorptionDistance = distance;
            }
            CanAbsorbTarget = AbsorptionTarget != null && AbsorptionDistance <= AbsorbRange &&
                reservoir.StoredJ < reservoir.CapacityJ - .001;
        }

        private bool VisibleFromPlayer(PhysicalBodyState body, Vector3 center)
        {
            Vector3 from = actor.ChestPosition;
            Vector3 path = center - from;
            float length = path.magnitude;
            if (length < .1f) return true;
            RaycastHit[] hits = Physics.RaycastAll(from, path / length, length,
                ~0, QueryTriggerInteraction.Ignore);
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.transform.IsChildOf(actor.transform) ||
                    hit.collider.transform.IsChildOf(body.transform)) continue;
                if (hit.distance < length - .08f) return false;
            }
            return true;
        }

        private void Absorb()
        {
            if (AbsorptionTarget == null)
            {
                if (Time.time >= feedbackUntil) Inform("Look toward the glowing HEAT CORE. Green ring = absorbable.");
                return;
            }
            if (AbsorptionDistance > AbsorbRange)
            {
                if (Time.time >= feedbackUntil) Inform("MOVE CLOSER to the highlighted heat source (within 7.5m).");
                return;
            }
            if (reservoir.StoredJ >= reservoir.CapacityJ - .001)
            {
                if (Time.time >= feedbackUntil) Inform("ENERGY STORAGE FULL — discharge before absorbing again.");
                return;
            }
            double requestedJ = 76000.0 * Time.deltaTime;
            TransferReceipt receipt = EnergyTransfer.Absorb(AbsorptionTarget, reservoir, stage, requestedJ, .8);
            if (receipt.CreditedJ > 0)
            {
                absorptionUntil = Time.time + .15f;
                absorptionRateKJs = (float)(receipt.CreditedJ / 1000.0 / Mathf.Max(Time.deltaTime, .0001f));
                if (absorptionVfx != null) absorptionVfx.Transfer(AbsorptionTarget, receipt.CreditedJ);
                feedback = "+" + (receipt.CreditedJ / 1000).ToString("F1") + " kJ absorbed   |   " +
                    (AbsorptionTarget.TemperatureC).ToString("F0") + " °C source   |   " +
                    (receipt.WasteHeatJ / 1000).ToString("F1") + " kJ dissipated";
                feedbackUntil = Time.time + .25f;
            }
            else if (Time.time >= feedbackUntil) Inform("Source has cooled below its usable temperature.");
        }

        private void Emit(bool thermal)
        {
            if (AimedBody == null || AimedBody == actor.Body || AimDistance > 28f)
            {
                Inform("Aim at a physical target within 28m.");
                return;
            }
            if (thermal && reservoir.StoredJ < 500) { Inform("No stored energy — hold E at a hot source."); return; }
            if (!thermal && reservoir.StoredJ < 500) { Inform("Not enough stored energy to launch a pulse."); return; }

            Collider[] around = Physics.OverlapSphere(AimedBody.transform.position, thermal ? 3.5f : 2.7f,
                ~0, QueryTriggerInteraction.Ignore);
            eligible.Clear(); weights.Clear(); unique.Clear();
            Vector3 origin = actor.ChestPosition + (AimedBody.transform.position - actor.ChestPosition).normalized * .65f;
            foreach (Collider c in around)
            {
                PhysicalBodyState candidate = c.GetComponentInParent<PhysicalBodyState>();
                if (candidate == null || candidate == actor.Body || !unique.Add(candidate)) continue;
                if (!thermal && (candidate.PhysicsBody == null || candidate.PhysicsBody.isKinematic)) continue;
                if (Blocked(origin, candidate)) continue;
                float separation = Vector3.Distance(AimedBody.transform.position, candidate.transform.position);
                float weight = 1f / (1f + separation * separation * .8f);
                eligible.Add(candidate);
                weights.Add(weight);
            }
            if (eligible.Count == 0) { Inform("No exposed physical target. Check line of sight."); return; }
            double debitedJ = reservoir.Withdraw(thermal ? 18500 : 13200);
            if (debitedJ <= 0) return;
            double efficiency = thermal ? .86 : .71;
            double deliveredJ = debitedJ * efficiency;
            stage.ExchangeHeat(debitedJ - deliveredJ);
            double weightTotal = 0;
            foreach (float weight in weights) weightTotal += weight;

            for (int i = 0; i < eligible.Count; i++)
            {
                PhysicalBodyState body = eligible[i];
                double shareJ = deliveredJ * weights[i] / weightTotal;
                if (thermal)
                {
                    body.ApplyHeat(shareJ);
                }
                else
                {
                    Rigidbody rb = body.PhysicsBody;
                    Vector3 direction = (body.transform.position - actor.transform.position).normalized;
                    if (direction.sqrMagnitude < .0001f) direction = cameraRig.transform.forward;
                    double beforeAlong = Vector3.Dot(rb.linearVelocity, direction);
                    double deltaV = EnergyTransfer.PositiveImpulseDeltaV(rb.mass, beforeAlong, shareJ);
                    rb.AddForce(direction * (float)(rb.mass * deltaV), ForceMode.Impulse);
                    body.ReportShock(shareJ);
                }
            }
            cooldown = thermal ? .34f : .42f;
            PulseVisual.Spawn(AimedBody.transform.position, thermal, thermal ? 3.5f : 2.7f);
            Inform((thermal ? "THERMAL" : "KINETIC") + "  " + (deliveredJ / 1000).ToString("F1") +
                " kJ / " + eligible.Count + " bodies  (lost " + ((debitedJ - deliveredJ) / 1000).ToString("F1") + " kJ)");
        }

        private bool Blocked(Vector3 origin, PhysicalBodyState target)
        {
            Vector3 delta = target.transform.position - origin;
            float distance = delta.magnitude;
            if (distance < .1f) return false;
            RaycastHit[] hits = Physics.RaycastAll(origin, delta / distance, distance,
                ~0, QueryTriggerInteraction.Ignore);
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.transform.IsChildOf(actor.transform)) continue;
                if (hit.collider.transform.IsChildOf(target.transform)) continue;
                if (hit.distance > .08f && hit.distance < distance - .1f) return true;
            }
            return false;
        }

        private void Inform(string message)
        {
            feedback = message;
            feedbackUntil = Time.time + 2.1f;
        }
    }
}
