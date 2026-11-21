namespace DanhCu.Core.StateMachines
{
    /// <summary>
    /// A single state in a <see cref="StateMachine{TId}"/>.
    /// Implementations must not assume they are ticked every frame while inactive.
    /// </summary>
    public interface IState
    {
        /// <summary>Called once when the state becomes active.</summary>
        void Enter();

        /// <summary>Called every frame while the state is active.</summary>
        /// <param name="deltaTime">Unscaled frame time in seconds.</param>
        void Tick(float deltaTime);

        /// <summary>Called once when the state stops being active.</summary>
        void Exit();
    }
}
