using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Status Effect")]
public class StatusEffectData : ScriptableObject
{
    public string effectName;
    [TextArea] public string description;
    
    // Poison (purple)
    //Sleep (zzz), Sneeze
    //Accuracy down (crosshare symbol)
    //Flower Trap (flower symbol)
    //Shield of Leaves (shield or leaf symbol)
    //Taunt (middle finger or megaphone)
    //Heartwood Pulse (thorn absorbing half of incoming dmg/healing, could be magnet symbol)
    //Defense up (shield with arrow upwards or plus symbol)


    public Sprite icon;

    public int maxStacks = 1;
    public bool isDebuff = true;

    public virtual void OnApply(CharacterCombat target) { }
    public virtual void OnTurnStart(CharacterCombat target, ref int stacks) { }
    public virtual void OnTurnEnd(CharacterCombat target, ref int stacks) { }
    public virtual void OnRemove(CharacterCombat target) { }
}