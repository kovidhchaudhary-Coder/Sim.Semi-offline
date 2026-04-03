# Sim.Semi-offline

## Beast-Nadi 6-DOF Physics Library (v0.9.5)

## Standalone Core (No Engine Imports)
- `Core/BeastEngine.Core.cs`
  - Namespace: `BeastPhysics`
  - Custom primitives: `BeastVector3`, `BeastQuaternion`, `BeastState`, `BeastInputs`
  - Deterministic 1000 Hz stepping (`EngineCore.DT = 0.001`)
  - RK4 integration for translation and rotation in `EngineCore.Step(...)`
  - Reinforced rectangular-prism inertia tensor with configurable scale
  - Distance-penalty joint reinforcement with stiffness hardening by `v^2`
  - Aero torque latch hysteresis (`1200 Nm` open / `800 Nm` close) with 400 Nm dead-zone
  - Headless boot audit (`RunBootAudit`) and raw string JSON export (`GetAuditJSON`)
  - Nadi visual-frequency state mapping (`7.90e14 Hz` violet shift when `V > 500000`)

## Unity Runtime Helpers (Optional Integration Layer)
- `Scripts/BeastMasterInitializer.cs`
- `Scripts/VehicleConfigValidator.cs`
- `Scripts/PhysicsAuditHarness.cs`
- `Scripts/TrunkLatchController.cs`
- `Scripts/BeastReinforcementController.cs`
- `Scripts/NadiVisualMapper.cs`
- `Scripts/BeastStressTestRunner.cs`

These scripts are optional adapters. The standalone core in `Core/` is the source-of-truth physics library.
