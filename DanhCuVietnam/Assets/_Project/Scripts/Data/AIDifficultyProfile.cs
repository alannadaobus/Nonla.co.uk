using System.Collections.Generic;
using UnityEngine;

namespace DanhCu.Data
{
    /// <summary>
    /// Tuning for an AI opponent. The AI uses the same throw input model as the player,
    /// with error injected from these values, so it can never do what a human cannot.
    /// </summary>
    [CreateAssetMenu(menuName = "Danh Cu/AI Difficulty", fileName = "AI_")]
    public sealed class AIDifficultyProfile : DefinitionAsset
    {
        [Tooltip("Ordering for menus: 0 = easiest.")]
        [Min(0)] [SerializeField] private int tier;

        [Tooltip("Standard deviation of aim error, in degrees.")]
        [Range(0f, 20f)] [SerializeField] private float aimErrorDegrees = 6f;

        [Tooltip("Standard deviation of throw power error, as a fraction of max power.")]
        [Range(0f, 0.5f)] [SerializeField] private float powerError = 0.15f;

        [Tooltip("Delay before reacting to the opponent's throw, in seconds.")]
        [Range(0f, 3f)] [SerializeField] private float reactionTimeSeconds = 0.8f;

        [Tooltip("0 = always plays for longest spin, 1 = always tries to strike.")]
        [Range(0f, 1f)] [SerializeField] private float aggression = 0.5f;

        public int Tier => tier;

        public float AimErrorDegrees => aimErrorDegrees;

        public float PowerError => powerError;

        public float ReactionTimeSeconds => reactionTimeSeconds;

        public float Aggression => aggression;

        internal void Configure(int newTier, float newAimError, float newPowerError, float newReactionTime, float newAggression)
        {
            tier = newTier;
            aimErrorDegrees = newAimError;
            powerError = newPowerError;
            reactionTimeSeconds = newReactionTime;
            aggression = newAggression;
        }
    }
}
