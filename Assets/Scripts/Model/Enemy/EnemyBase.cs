using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy 
{
    public float health;
    public float speed;
    public float attack;

    public Enemy(float health, float speed, float attack)
    {
        this.health = health;
        this.speed = speed;
    }
}
