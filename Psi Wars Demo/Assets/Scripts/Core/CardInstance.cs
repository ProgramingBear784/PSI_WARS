using System;

/// <summary>
/// Runtime wrapper around CardData. One instance per physical card in play.
/// </summary>
public class CardInstance
{
    public CardData data;
    public bool isDepleted;       // Grayscale — creation unit used this turn
    public bool isDisoriented;    // Lost Digital or Psionic battle — deals 0 Physical damage
    public CardInstance equippedItem; // Equipment attached to this unit (null if none)
    public CardInstance stackedWith;  // Partner in a Cyborg+non-Cyborg stack (null if not stacked)
    public readonly Guid instanceId;  // Unique per instance so lists can hold duplicates of same card

    public CardInstance(CardData data)
    {
        this.data = data;
        instanceId = Guid.NewGuid();
    }

    // Effective stats: base + equipment + stack partner (partner's stats pool into the stack)
    public int CyberStrength    => data.cyberStrength    + (equippedItem?.data.equipCyberBonus    ?? 0)
                                 + (stackedWith?.data.cyberStrength    ?? 0) + (stackedWith?.equippedItem?.data.equipCyberBonus    ?? 0);
    public int PsionicStrength  => data.psionicStrength  + (equippedItem?.data.equipPsionicBonus  ?? 0)
                                 + (stackedWith?.data.psionicStrength  ?? 0) + (stackedWith?.equippedItem?.data.equipPsionicBonus  ?? 0);
    public int PhysicalStrength => data.physicalStrength + (equippedItem?.data.equipPhysicalBonus ?? 0)
                                 + (stackedWith?.data.physicalStrength ?? 0) + (stackedWith?.equippedItem?.data.equipPhysicalBonus ?? 0);
}
