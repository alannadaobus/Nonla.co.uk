using System.Collections.Generic;
using UnityEngine;

namespace DanhCu.Data
{
    /// <summary>
    /// Global physics tuning. Applied to the engine once at boot by the runtime.
    /// Every field maps to a symbol in Docs/PHYSICS_SPEC.md; keep them in sync.
    /// </summary>
    [CreateAssetMenu(menuName = "Danh Cu/Physics Tuning", fileName = "PhysicsTuning_")]
    public sealed class PhysicsTuningProfile : DefinitionAsset
    {
        [Header("Engine (PhysX)")]
        [Tooltip("Fixed timestep in seconds. 1/120 by default (PHYSICS_SPEC §6).")]
        [Range(1f / 240f, 1f / 30f)] [SerializeField] private float fixedTimestep = 1f / 120f;

        [Tooltip("Custom spin-model sub-steps per fixed step (Δt_sim = fixedTimestep / substeps).")]
        [Range(1, 8)] [SerializeField] private int simulationSubsteps = 2;

        [SerializeField] private Vector3 gravity = new Vector3(0f, -9.81f, 0f);

        [Range(4, 32)] [SerializeField] private int solverIterations = 12;
        [Range(1, 16)] [SerializeField] private int solverVelocityIterations = 4;

        [Tooltip("rad/s. PhysX defaults to 7, far below a real top (~150-300). PHYSICS_SPEC §6.1.")]
        [Range(50f, 1000f)] [SerializeField] private float maxAngularSpeed = 450f;

        [Tooltip("Tops must never sleep while spinning slowly.")]
        [Min(0f)] [SerializeField] private float sleepThreshold = 0.0001f;

        [Min(0f)] [SerializeField] private float bounceThreshold = 0.5f;

        [Header("Spin model (PHYSICS_SPEC §4)")]
        [Tooltip("k_air: aerodynamic drag torque coefficient (N·m·s²).")]
        [Min(0f)] [SerializeField] private float airDragCoefficient = 2.0e-8f;

        [Tooltip("Spin (rad/s) below which the top starts to topple (ω_crit multiplier).")]
        [Range(0.5f, 3f)] [SerializeField] private float criticalSpinMultiplier = 1.0f;

        [Tooltip("How strongly tip friction rights a tilted fast top (sleeping-top effect).")]
        [Range(0f, 5f)] [SerializeField] private float rightingGain = 1.0f;

        [Tooltip("Fraction of relative tangential surface speed converted to spin transfer on collision.")]
        [Range(0f, 1f)] [SerializeField] private float collisionSpinTransfer = 0.35f;

        [Tooltip("Spin (rad/s) below which a top counts as stopped for rules.")]
        [Min(0f)] [SerializeField] private float stoppedSpinThreshold = 8f;

        public float FixedTimestep => fixedTimestep;

        public int SimulationSubsteps => simulationSubsteps;

        public Vector3 Gravity => gravity;

        public int SolverIterations => solverIterations;

        public int SolverVelocityIterations => solverVelocityIterations;

        public float MaxAngularSpeed => maxAngularSpeed;

        public float SleepThreshold => sleepThreshold;

        public float BounceThreshold => bounceThreshold;

        public float AirDragCoefficient => airDragCoefficient;

        public float CriticalSpinMultiplier => criticalSpinMultiplier;

        public float RightingGain => rightingGain;

        public float CollisionSpinTransfer => collisionSpinTransfer;

        public float StoppedSpinThreshold => stoppedSpinThreshold;

        public override void Validate(List<string> errors)
        {
            base.Validate(errors);
            Require(fixedTimestep > 0f, errors, "fixedTimestep must be positive.");
            Require(maxAngularSpeed >= 300f, errors, "maxAngularSpeed below 300 rad/s will clamp real launch speeds.");
            Require(gravity.y < 0f, errors, "gravity must point down (negative Y).");
        }
    }
}
