using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowPlayer : MonoBehaviour
{
    public Transform player; // Assign your Player here

    private void OnEnable()
    {
        RegisterFollowTarget();
    }

    private void Start()
    {
        RegisterFollowTarget();
    }

    private void Update()
    {
        if (player == null)
        {
            RegisterFollowTarget();
        }
    }

    private void RegisterFollowTarget()
    {
        if (player == null && transform.parent != null && transform.parent.CompareTag("Player"))
        {
            player = transform.parent;
        }

        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }

        if (player != null)
        {
            RuntimeCameraController.EnsureInstance().BindFollowTarget(player);
        }
    }
}
