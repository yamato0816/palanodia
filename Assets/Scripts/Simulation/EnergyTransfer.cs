using System;

namespace Palinodia.Simulation
{
    public readonly struct TransferReceipt
    {
        public readonly double DebitedJ;
        public readonly double CreditedJ;
        public readonly double WasteHeatJ;
        public TransferReceipt(double debited, double credited, double waste)
        {
            DebitedJ = debited;
            CreditedJ = credited;
            WasteHeatJ = waste;
        }
    }

    /// <summary>Pure arithmetic for debiting a finite heat source and crediting finite storage.</summary>
    public static class EnergyTransfer
    {
        public static TransferReceipt Absorb(PhysicalBodyState source, EnergyReservoir store,
            StageEnvironmentState stage, double requestedSourceJ, double efficiency = 0.8)
        {
            if (source == null || store == null || stage == null || requestedSourceJ <= 0) return default;
            efficiency = Math.Max(0.001, Math.Min(1, efficiency));
            double sourceJ = Math.Min(requestedSourceJ, Math.Min(source.MaxRemovableHeatJ,
                (store.CapacityJ - store.StoredJ) / efficiency));
            if (sourceJ <= 0) return default;
            double removedJ = -source.ApplyHeat(-sourceJ);
            double credited = store.Deposit(removedJ * efficiency);
            double waste = removedJ - credited;
            stage.ExchangeHeat(waste);
            return new TransferReceipt(removedJ, credited, waste);
        }

        public static double PositiveImpulseDeltaV(double massKg, double velocityAlongDirectionMps, double gainedKineticJ)
        {
            if (massKg <= 0 || gainedKineticJ <= 0) return 0;
            // Solve ½m(Δv)^2 + m(v·n)Δv = ΔE. This accounts for preexisting velocity.
            return Math.Max(0, -velocityAlongDirectionMps +
                Math.Sqrt(velocityAlongDirectionMps * velocityAlongDirectionMps + 2 * gainedKineticJ / massKg));
        }
    }
}
