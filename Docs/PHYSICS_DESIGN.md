# PALINODIA — Physics Prototype 0.1

These values form a *gameplay approximation*, not a physiological or thermodynamic simulation.

- A stage has one well-mixed ambient temperature and a finite equivalent thermal heat capacity, both stored in `StageEnvironmentState`.
- Each interactable body has its own thermal capacity in `J/K` and temperature in Celsius. Heat flow uses `Q = conductance * (Tbody - Tstage) * dt`. Stage receives the opposite heat transfer.
- Source extraction removes **Q** from source, credits `eta*Q` to storage and puts `(1-eta)*Q` into the ambient stage.
- Emission withdraws **Qin** from storage; grants `eta*Qin` of thermal or kinetic energy to nearby unobstructed targets; dissipates `(1-eta)*Qin` to stage. Every pulse distributes output among targets by normalized distance weighting; no energy duplication by multi-hits.
- Kinetic effect uses an energy-constrained impulse. Solve `0.5*m*Δv² + m*(v·dir)*Δv = E` for Δv >= 0, then `Rigidbody.AddForce(m*Δv*dir, Impulse)`.
- The reaction impulse is implicitly borne by the environment/ability-field, *not* simulated as recoil. This breaks total momentum conservation in the prototype: **pending model improvement**.
- Collision-shock and overheated machine module shutdown are functional status conditions, **not generic hit-point damage**. Heat transfer and rigidbody simulation are separate from shutdown conditions.
- Thermal events cannot draw infinite energy from the room: interactable sources have finite capacities and minimum extraction temperature. The room has its own finite heat capacity but is not currently a direct player resource.
- Drone heat-burst attacks draw from their own finite `EnergyReservoir` battery, not from an infinite ability reserve. Their attack loss warms the stage.
- Disabling machines is latched. This is a gameplay simplification; overheating in reality can cause both transient failures and permanent damage.

## Known limits

No thermal radiation, detailed pressure waves, conduction network, oxygen budget, electricity, structural fracture, anatomical physiology, contact-dependent impulse modes or full momentum bookkeeping. Values and ranges are tuned to make the test room playable; they are not assertions about real-world materials or injuries.
