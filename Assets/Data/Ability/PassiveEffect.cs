using UnityEngine;

public abstract class PassiveEffect : ScriptableObject
{
    [TextArea] public string effectDescription;
    public abstract void OnApply(GameObject user);
    public abstract void OnRemove(GameObject user);

    // Virtual "Hooks" that complex passives can override
    public virtual void OnTurnStart(GameObject user) { }
    public virtual void OnMove(GameObject user, Vector2Int oldPos, Vector2Int newPos) { }
    public virtual void OnTakeDamage(GameObject user, int amount) { }

    // New Hook: Called whenever the actor's state might have changed
    public virtual void Refresh(GameObject user) { }

}