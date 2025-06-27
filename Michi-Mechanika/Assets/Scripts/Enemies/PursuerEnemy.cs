using UnityEngine;

public class PursuerEnemy : Enemy
{
    private bool hasSeenPlayer = false;

    public void Activate()
    {
        hasSeenPlayer = true;
    }

    public void Chase()
    {
        if (!hasSeenPlayer) return;
        
        
    }

    public void CheckForPlayer()
    {
        
    }
}
