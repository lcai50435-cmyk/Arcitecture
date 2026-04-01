using UnityEngine;

public class PlayerAttack : CharacterAttack
{
    private KeyCode attackKey = KeyCode.Mouse0; // Ä¬ÈÏÊó±ê×ó¼ü

    private void Awake()
    {

    }

    private void Update()
    {
        // ¹¥»÷¶¯»­²¥·Å
        if (Input.GetKeyDown(attackKey) && !isAttacking)
        {          
            TriggerAttack();
        }
    }
}       