using System;
using System.Collections.Generic;
using UnityEngine;

public enum EnemyState
{
    Patrol,
    Chase,
    ReturnToSpawn,
    Attack
}

[DisallowMultipleComponent]
public class EnemyStatsManager : MonoBehaviour
{
    [SerializeField] private EnemyState initialState = EnemyState.Patrol;

    [Header("Chase Target")]
    public string playerTag = "Player";
    public Transform playerTarget;

    public EnemyState CurrentState { get; private set; }
    public Transform PlayerTarget => playerTarget;
    public bool HasPlayerInRange => playerRangeContacts.Count > 0;

    public event Action<EnemyState> OnStateChanged;

    private readonly Dictionary<Collider2D, int> playerRangeContacts = new Dictionary<Collider2D, int>();

    private void Awake()
    {
        ResetState();
        ResolvePlayerTargetIfMissing();
    }

    private void OnEnable()
    {
        ResetState();
        ResolvePlayerTargetIfMissing();
    }

    private void OnDisable()
    {
        playerRangeContacts.Clear();
    }

    public void ResetState()
    {
        CurrentState = initialState;
    }

    public void ResolvePlayerTargetIfMissing()
    {
        if (playerTarget != null || string.IsNullOrWhiteSpace(playerTag))
        {
            return;
        }

        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player != null)
        {
            playerTarget = player.transform;
        }
    }

    public void NotifyPlayerEnteredRange(Collider2D other)
    {
        Collider2D playerCollider = ResolvePlayerCollider(other);
        if (playerCollider == null)
        {
            return;
        }

        if (playerRangeContacts.TryGetValue(playerCollider, out int count))
        {
            playerRangeContacts[playerCollider] = count + 1;
        }
        else
        {
            playerRangeContacts[playerCollider] = 1;
        }

        if (playerTarget == null)
        {
            playerTarget = ResolvePlayerTransform(playerCollider);
        }

        EnterChaseState();
    }

    public void NotifyPlayerExitedRange(Collider2D other)
    {
        Collider2D playerCollider = ResolvePlayerCollider(other);
        if (playerCollider == null)
        {
            return;
        }

        if (playerRangeContacts.TryGetValue(playerCollider, out int count))
        {
            if (count <= 1)
            {
                playerRangeContacts.Remove(playerCollider);
            }
            else
            {
                playerRangeContacts[playerCollider] = count - 1;
            }
        }

        if (playerRangeContacts.Count == 0 &&
            (CurrentState == EnemyState.Chase || CurrentState == EnemyState.Attack))
        {
            EnterReturnToSpawnState();
        }
    }

    public void HandleMissingPlayerTarget()
    {
        playerRangeContacts.Clear();
        EnterReturnToSpawnState();
    }

    public void NotifyReturnedToSpawn()
    {
        if (CurrentState == EnemyState.ReturnToSpawn)
        {
            EnterPatrolState();
        }
    }

    public void EnterPatrolState()
    {
        SetState(EnemyState.Patrol);
    }

    public void EnterChaseState()
    {
        SetState(EnemyState.Chase);
    }

    public void EnterReturnToSpawnState()
    {
        SetState(EnemyState.ReturnToSpawn);
    }

    public void EnterAttackState()
    {
        SetState(EnemyState.Attack);
    }

    public bool IsInState(EnemyState state)
    {
        return CurrentState == state;
    }

    private void SetState(EnemyState newState)
    {
        if (CurrentState == newState)
        {
            return;
        }

        CurrentState = newState;
        OnStateChanged?.Invoke(CurrentState);
    }

    private Collider2D ResolvePlayerCollider(Collider2D other)
    {
        if (other == null)
        {
            return null;
        }

        if (other.CompareTag(playerTag))
        {
            return other;
        }

        if (other.attachedRigidbody != null && other.attachedRigidbody.CompareTag(playerTag))
        {
            return other;
        }

        Transform root = other.transform.root;
        if (root != null && root.CompareTag(playerTag))
        {
            return other;
        }

        return null;
    }

    private Transform ResolvePlayerTransform(Collider2D playerCollider)
    {
        if (playerCollider == null)
        {
            return null;
        }

        if (playerCollider.attachedRigidbody != null)
        {
            return playerCollider.attachedRigidbody.transform;
        }

        Transform root = playerCollider.transform.root;
        if (root != null && root.CompareTag(playerTag))
        {
            return root;
        }

        return playerCollider.transform;
    }
}
