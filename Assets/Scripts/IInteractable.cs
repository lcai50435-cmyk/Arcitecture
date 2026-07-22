using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Interaction interface
/// </summary>
public interface IInteractable 
{
    string InteractionTip { get; }  // Interaction prompt shown for the item

    void OnInteract(); // Runs when the item is interacted with
}
