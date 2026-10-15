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
        private FollowCamera cameraRig;
        private PlayerActor actor;
        private StageEnvironmentState stage;
        private float cooldown;
        private string feedback = "Look at the orange core and hold E to harvest heat.";
        private float feedbackUntil;

        public EnergyReservoir Reservoir => reservoir;
        public string Feedback => Time.time < feedbackUntil ? feedback : "";
        public PhysicalBodyState AimedBody { get; private set; }
        public float AimDistance { get; private set; }

        public void Initialize(PlayerActor source, FollowCamera rig, StageEnvironmentState environment)
        {
            actor = source;
            cameraRig = rig;
            stage = environment;
        }

        private void Update()
        {
            if (actor == null || cameraRig == null || stage == null) return;
            cooldown -= Time.deltaTime;
            bool aimed = CastAim(out RaycastHit hit);
            AimedBody = aimed ? hit.collider.GetComponentInParent<PhysicalBodyState>() : null;
            AimDistance = aimed ? hit.distance : float.PositiveInfinity;

            if (!actor.Body.Operational) return;
            if (Input.GetKey(KeyCode.E)) Absorb();
            if (Cursor.lockState != CursorLockMode.Locked) return;
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

        private void Absorb()
        {
            if (AimedBody == null || AimedBody == actor.Body || AimDistance > 7.5f) return;
            if (AimedBody.Category == BodyCategory.Living) return;
            double rateJ = 76000.0 * Time.deltaTime;
            TransferReceipt receipt = EnergyTransfer.Absorb(AimedBody, reservoir, stage, rateJ, .8);
            if (receipt.CreditedJ > 0)
            {
                feedback = "+" + (receipt.CreditedJ / 1000).ToString("F1") + " kJ stored  |  loss " +
                    (receipt.WasteHeatJ / 1000).ToString("F1") + " kJ → room";
                feedbackUntil = Time.time + .3f;
            }
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
