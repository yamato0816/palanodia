using UnityEngine;

namespace Palinodia.Simulation
{
    /// <summary>One finite-capacity, well-mixed thermal stage; no voxel/grid simulation.</summary>
    public sealed class StageEnvironmentState : MonoBehaviour
    {
        [SerializeField] private double temperatureC = 22.0;
        [SerializeField] private double pressurePa = 101325.0;
        [SerializeField] private double equivalentHeatCapacityJPerK = 2500000.0;
        private double netHeatJ;

        public double TemperatureC => temperatureC;
        public double PressurePa => pressurePa;
        public double NetHeatJ => netHeatJ;

        /// <summary>Positive Q warms the room, negative Q cools it. ΔT = Q / C.</summary>
        public void ExchangeHeat(double joules)
        {
            if (double.IsNaN(joules) || double.IsInfinity(joules)) return;
            double next = temperatureC + joules / equivalentHeatCapacityJPerK;
            if (next < -273.15) joules = (-273.15 - temperatureC) * equivalentHeatCapacityJPerK;
            temperatureC += joules / equivalentHeatCapacityJPerK;
            netHeatJ += joules;
        }
    }
}
