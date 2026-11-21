using DanhCu.Data;
using UnityEngine;

namespace DanhCu.Runtime
{
    /// <summary>Pushes data-driven settings into engine globals. Called once at boot.</summary>
    internal static class EngineSettingsApplier
    {
        public static void ApplyPhysics(PhysicsTuningProfile profile)
        {
            if (profile == null)
            {
                Debug.LogWarning("[DanhCu] No PhysicsTuningProfile assigned; engine physics defaults are in use and tops will be clamped to 7 rad/s.");
                return;
            }

            Time.fixedDeltaTime = profile.FixedTimestep;
            Physics.gravity = profile.Gravity;
            Physics.defaultSolverIterations = profile.SolverIterations;
            Physics.defaultSolverVelocityIterations = profile.SolverVelocityIterations;
            Physics.defaultMaxAngularSpeed = profile.MaxAngularSpeed;
            Physics.sleepThreshold = profile.SleepThreshold;
            Physics.bounceThreshold = profile.BounceThreshold;
        }

        public static void ApplyPlatform(GameConfig config)
        {
            Application.targetFrameRate = Application.isMobilePlatform
                ? config.MobileTargetFrameRate
                : config.DesktopTargetFrameRate;

            // Prevent the phone from dimming during a long spin.
            if (Application.isMobilePlatform)
            {
                Screen.sleepTimeout = SleepTimeout.NeverSleep;
            }
        }
    }
}
