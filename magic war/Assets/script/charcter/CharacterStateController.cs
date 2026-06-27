using UnityEngine;

public enum CharacterState
{
    Idle,
    Moving,
    Casting,
    Recovery,
    Knocked,
    Dead
}

public class CharacterStateController : MonoBehaviour
{
    public CharacterState currentState = CharacterState.Idle;

    public bool CanMove =>
        currentState != CharacterState.Casting &&
        currentState != CharacterState.Recovery &&
        currentState != CharacterState.Knocked &&
        currentState != CharacterState.Dead;

    public bool CanCast =>
        currentState == CharacterState.Idle ||
        currentState == CharacterState.Moving;

    public bool IsBusy =>
        currentState == CharacterState.Casting ||
        currentState == CharacterState.Recovery ||
        currentState == CharacterState.Knocked;

    public void SetState(CharacterState state)
    {
        currentState = state;
    }

    public void SetMoving(bool isMoving)
    {
        if (!CanMove)
            return;

        currentState = isMoving ? CharacterState.Moving : CharacterState.Idle;
    }

    public void ReturnToIdleIfNotDead()
    {
        if (currentState != CharacterState.Dead)
            currentState = CharacterState.Idle;
    }

    public void ResetRuntimeState()
    {
        currentState = CharacterState.Idle;
    }
}
