using NUnit.Framework;
using UnityEngine;
using Palinodia.Simulation;

namespace Palinodia.Tests
{
    public sealed class EnergyAccountingTests
    {
        [Test]
        public void ReservoirClampsAllTransfers()
        {
            var r = new EnergyReservoir(100, 10);
            Assert.AreEqual(90d, r.Deposit(1000));
            Assert.AreEqual(100d, r.StoredJ);
            Assert.AreEqual(100d, r.Withdraw(250));
            Assert.AreEqual(0d, r.Withdraw(-5));
        }

        [Test]
        public void PositiveImpulseGivesRequestedKineticEnergyAtRest()
        {
            const double mass = 20;
            const double energy = 2500;
            double deltaV = EnergyTransfer.PositiveImpulseDeltaV(mass, 0, energy);
            Assert.That(.5 * mass * deltaV * deltaV, Is.EqualTo(energy).Within(1e-7));
        }

        [Test]
        public void PositiveImpulseWorksWhenBodyAlreadyMovingBackwards()
        {
            const double mass = 12;
            const double previous = -4;
            const double energy = 300;
            double delta = EnergyTransfer.PositiveImpulseDeltaV(mass, previous, energy);
            double deltaE = .5 * mass * ((previous + delta) * (previous + delta) - previous * previous);
            Assert.That(deltaE, Is.EqualTo(energy).Within(1e-7));
        }

        [Test]
        public void HeatAbsorptionConservesJoulesAcrossThreeBuckets()
        {
            GameObject envObj = new GameObject("test stage");
            GameObject hotObj = new GameObject("test source");
            try
            {
                StageEnvironmentState stage = envObj.AddComponent<StageEnvironmentState>();
                PhysicalBodyState source = hotObj.AddComponent<PhysicalBodyState>();
                source.Configure(stage, BodyCategory.Heater, "heater", 122, 100, 0, 22);
                var reserve = new EnergyReservoir(20000, 0);
                var receipt = EnergyTransfer.Absorb(source, reserve, stage, 1000, .8);
                Assert.That(receipt.DebitedJ, Is.EqualTo(1000).Within(1e-7));
                Assert.That(receipt.CreditedJ, Is.EqualTo(800).Within(1e-7));
                Assert.That(receipt.WasteHeatJ, Is.EqualTo(200).Within(1e-7));
                Assert.That(receipt.DebitedJ, Is.EqualTo(receipt.CreditedJ + receipt.WasteHeatJ).Within(1e-7));
                Assert.That(source.TemperatureC, Is.EqualTo(112).Within(1e-7));
            }
            finally
            {
                Object.DestroyImmediate(hotObj);
                Object.DestroyImmediate(envObj);
            }
        }
    }
}
