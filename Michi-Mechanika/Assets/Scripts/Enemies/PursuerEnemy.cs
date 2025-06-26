using UnityEngine;

public class PursuerEnemy : Enemy
{
    private bool hasSeenPlayer = false;

    public void Activate()
    {
        hasSeenPlayer = true;
    }
}
