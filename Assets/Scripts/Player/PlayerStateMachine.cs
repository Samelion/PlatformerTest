using Assets.Scripts.StateMachine;

namespace Assets.Scripts.Player
{
    sealed class PlayerStateMachine
    {
        readonly PlayerController owner;
        
        public IState PreviousState { private set; get; }
        public IState CurrentState { private set; get; }

        public PlayerStateMachine(PlayerController player, IState startingState)
        {
            owner = player;
            ChangeState(startingState);
        }

        public void Tick()
        {
            var result = CurrentState.Execute(owner);
            if (result != null)
            {
                ChangeState(result);
            }
        }

        public void ChangeState(IState newState)
        {
            CurrentState?.Exit(owner);
            PreviousState = CurrentState;
            CurrentState = newState;
            CurrentState?.Enter(owner);
        }
    }
}
