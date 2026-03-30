using UnityEngine;

public class PlayerAttack : CharacterAttack
{
    public KeyCode attackKey = KeyCode.J; // Íæ¼Òµã»÷J¼ü¹¥»÷
     
    private void Update()
    {
        // ¹¥»÷¶¯»­²¥·Å
        if (Input.GetKeyDown(attackKey))
        {
            TriggerAttack();
        }
    }
}       