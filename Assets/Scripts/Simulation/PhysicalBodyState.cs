using System;
using UnityEngine;
using Palinodia.Gameplay;

namespace Palinodia.Simulation
{
    public enum BodyCategory { Metal, Heater, Machine, Living }

    /// <summary>Local material/functional state. No universal HP or attack damage number.</summary>
    public sealed class PhysicalBodyState : MonoBehaviour
    {
        [SerializeField] private BodyCategory category;
        [SerializeField] private double temperatureC = 22;
        [SerializeField] private double heatCapacityJPerK = 1000;
        [SerializeField] private double conductanceWPerK = 1.5;
        [SerializeField] private double minExtractC = 22;
        [SerializeField] private double shutdownTempC = 105;
        [SerializeField] private double shockLimitJ = 9000;
        [SerializeField] private bool operational = true;
        [SerializeField] private string displayName = "Physical object";

        private StageEnvironmentState stage;
        private Rigidbody physicsBody;
        private bool configured;
        private Renderer[] renderers;
        private MaterialPropertyBlock materialBlock;
        private float visualTimer;

        public BodyCategory Category => category;
        public double TemperatureC => temperatureC;
        public double HeatCapacityJPerK => heatCapacityJPerK;
        public double MinExtractC => minExtractC;
        public double MaxRemovableHeatJ => Math.Max(0, (temperatureC - minExtractC) * heatCapacityJPerK);
        public bool Operational => operational;
        public Rigidbody PhysicsBody => physicsBody;
        public string DisplayName => displayName;
        public double ShutdownTempC => shutdownTempC;

        public void Configure(StageEnvironmentState environment, BodyCategory type, string label,
            double initialC, double capacityJPerK, double exchangeWPerK, double extractionFloorC = 22,
            double failureTempC = 105, double failureShockJ = 9000)
        {
            stage = environment;
            category = type;
            displayName = label;
            temperatureC = initialC;
            heatCapacityJPerK = Math.Max(1, capacityJPerK);
            conductanceWPerK = Math.Max(0, exchangeWPerK);
            minExtractC = extractionFloorC;
            shutdownTempC = failureTempC;
            shockLimitJ = failureShockJ;
            physicsBody = GetComponent<Rigidbody>();
            renderers = GetComponentsInChildren<Renderer>();
            materialBlock = new MaterialPropertyBlock();
            operational = true;
            configured = true;
        }

        public double ApplyHeat(double joules)
        {
            if (!configured || double.IsNaN(joules) || double.IsInfinity(joules)) return 0;
            double accepted = Math.Max(joules, (-273.15 - temperatureC) * heatCapacityJPerK);
            temperatureC += accepted / heatCapacityJPerK;
            EvaluateFunction();
            return accepted;
        }

        public void ReportShock(double joules)
        {
            if (!operational || category != BodyCategory.Machine) return;
            if (joules >= shockLimitJ) Disable();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!configured || category != BodyCategory.Machine || !operational || physicsBody == null) return;
            double estimatedImpactJ = 0.5 * physicsBody.mass * collision.relativeVelocity.sqrMagnitude;
            // An estimated *stress indicator*, not extra energy added to the body.
            ReportShock(estimatedImpactJ * 0.35);
        }

        private void FixedUpdate()
        {
            if (!configured || stage == null) return;
            double deltaJ = (temperatureC - stage.TemperatureC) * conductanceWPerK * Time.fixedDeltaTime;
            deltaJ = Math.Min(Math.Max(deltaJ, -Math.Abs(temperatureC - stage.TemperatureC) * heatCapacityJPerK),
                Math.Abs(temperatureC - stage.TemperatureC) * heatCapacityJPerK);
            double actual = ApplyHeat(-deltaJ);
            stage.ExchangeHeat(-actual);
            EvaluateFunction();
        }

        private void EvaluateFunction()
        {
            if (!operational) return;
            if (category == BodyCategory.Machine && temperatureC >= shutdownTempC) Disable();
            if (category == BodyCategory.Living && (temperatureC >= 44 || temperatureC <= 28)) Disable();
        }

        private void Disable()
        {
            if (!operational) return;
            operational = false;
            visualTimer = 0;
            if (category == BodyCategory.Machine && physicsBody != null)
            {
                physicsBody.useGravity = true;
                physicsBody.linearDamping = 0.2f;
            }
            DroneAgent drone = GetComponent<DroneAgent>();
            if (drone != null) drone.OnDisabled();
        }

        private void Update()
        {
            if (operational || category != BodyCategory.Machine || renderers == null) return;
            visualTimer += Time.deltaTime;
            if (visualTimer < 0.05f) return;
            // Renderer shared materials are unchanged; only this disabled machine is tinted.
            foreach (Renderer r in renderers)
            {
                r.GetPropertyBlock(materialBlock);
                materialBlock.SetColor("_Color", new Color(0.24f, 0.26f, 0.29f));
                materialBlock.SetColor("_BaseColor", new Color(0.24f, 0.26f, 0.29f));
                r.SetPropertyBlock(materialBlock);
            }
        }
    }
}
