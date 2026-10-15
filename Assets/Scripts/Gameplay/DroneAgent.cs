using UnityEngine;
using Palinodia.Simulation;
using Palinodia.Presentation;

namespace Palinodia.Gameplay
{
    /// <summary>Small physically-driven hostile machine; no humanoid magic users in past-era test arena.</summary>
    [RequireComponent(typeof(Rigidbody), typeof(PhysicalBodyState))]
    public sealed class DroneAgent : MonoBehaviour
    {
        private Transform target;
        private PhysicalBodyState machine;
        private PhysicalBodyState targetBody;
        private Rigidbody rb;
        private StageEnvironmentState stage;
        private readonly EnergyReservoir battery = new EnergyReservoir(55000, 55000);
        private float cooldown;
        private float phase;
        private bool configured;

        public void Initialize(PlayerActor player, StageEnvironmentState environment, float offset)
        {
            target = player.transform;
            targetBody = player.Body;
            stage = environment;
            phase = offset;
            machine = GetComponent<PhysicalBodyState>();
            rb = GetComponent<Rigidbody>();
            rb.useGravity = false;
            rb.linearDamping = 1.9f;
            rb.angularDamping = 1.5f;
            configured = true;
        }

        public void OnDisabled()
        {
            if (rb == null) return;
            rb.useGravity = true;
            rb.linearDamping = .15f;
            rb.AddTorque(transform.right * 5, ForceMode.Impulse);
        }

        private void FixedUpdate()
        {
            if (!configured || !machine.Operational || target == null) return;
            Vector3 toTarget = target.position - transform.position;
            Vector3 horizontal = Vector3.ProjectOnPlane(toTarget, Vector3.up);
            float distance = horizontal.magnitude;
            Vector3 desiredVelocity = distance > 2.5f ? horizontal.normalized * 3.4f : Vector3.zero;
            Vector3 current = rb.linearVelocity;
            Vector3 steer = desiredVelocity - new Vector3(current.x, 0, current.z);
            rb.AddForce(steer * 5f, ForceMode.Acceleration);
            float hoverHeight = 1.55f + Mathf.Sin(Time.time * 2.0f + phase) * .16f;
            rb.AddForce(Vector3.up * ((hoverHeight - transform.position.y) * 18 - rb.linearVelocity.y * 7), ForceMode.Acceleration);
            if (distance > .5f) rb.MoveRotation(Quaternion.Slerp(rb.rotation, Quaternion.LookRotation(horizontal), Time.fixedDeltaTime * 3f));
            if (distance > 15) rb.AddForce(horizontal.normalized * 11, ForceMode.Acceleration);
        }

        private void Update()
        {
            if (!configured || !machine.Operational || targetBody == null || !targetBody.Operational) return;
            cooldown -= Time.deltaTime;
            if (cooldown > 0 || Vector3.Distance(transform.position, target.position) > 3.6f) return;
            if (!CanSeePlayer()) return;
            cooldown = 2.4f;
            // Finite battery energy: each attack drains the machine's internal store.
            // The ability does not synthesize heat out of nothing.
            const double burstJ = 5600;
            double takenJ = battery.Withdraw(burstJ);
            targetBody.ApplyHeat(takenJ * .75);
            stage.ExchangeHeat(takenJ * .25);
            if (takenJ > 0) PulseVisual.Spawn(target.position + Vector3.up * 1.15f, true, .55f);
        }
        private bool CanSeePlayer()
        {
            Vector3 origin = transform.position;
            Vector3 destination = target.position + Vector3.up * 1.2f;
            Vector3 delta = destination - origin;
            float distance = delta.magnitude;
            foreach (RaycastHit hit in Physics.RaycastAll(origin, delta.normalized, distance,
                ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.transform.IsChildOf(transform)) continue;
                if (hit.collider.transform.IsChildOf(target)) continue;
                if (hit.distance < distance - .1f) return false;
            }
            return true;
        }
    }
}
