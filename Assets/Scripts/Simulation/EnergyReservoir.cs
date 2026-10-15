using System;
using UnityEngine;

namespace Palinodia.Simulation
{
    /// <summary>Energy units are joules. No auto-regeneration, negative balances or free overflow.</summary>
    [Serializable]
    public sealed class EnergyReservoir
    {
        [SerializeField] private double capacityJ = 150000.0;
        [SerializeField] private double storedJ = 17000.0;

        public double CapacityJ => capacityJ;
        public double StoredJ => storedJ;
        public double Fill => capacityJ <= 0 ? 0 : storedJ / capacityJ;

        public EnergyReservoir() { }

        public EnergyReservoir(double capacity, double initial)
        {
            capacityJ = Math.Max(0.0, capacity);
            storedJ = Math.Min(capacityJ, Math.Max(0.0, initial));
        }

        public double Deposit(double requestedJ)
        {
            if (requestedJ <= 0 || double.IsNaN(requestedJ) || double.IsInfinity(requestedJ)) return 0;
            double accepted = Math.Min(requestedJ, Math.Max(0.0, capacityJ - storedJ));
            storedJ += accepted;
            return accepted;
        }

        public double Withdraw(double requestedJ)
        {
            if (requestedJ <= 0 || double.IsNaN(requestedJ) || double.IsInfinity(requestedJ)) return 0;
            double withdrawn = Math.Min(requestedJ, Math.Max(0.0, storedJ));
            storedJ -= withdrawn;
            return withdrawn;
        }
    }
}
