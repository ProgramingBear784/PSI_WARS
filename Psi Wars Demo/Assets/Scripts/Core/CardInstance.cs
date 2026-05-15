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
    public readonly Guid instanceId;  // Unique per instance so lists can hold duplicates of same card

    public CardInstance(CardData data)
    {
        this.data = data;
        instanceId = Guid.NewGuid();
    }

    // Effective stats include any attached equipment bonuses
    public int CyberStrength    => data.cyberStrength    + (equippedItem?.data.equipCyberBonus    ?? 0);
    public int PsionicStrength  => data.psionicStrength  + (equippedItem?.data.equipPsionicBonus  ?? 0);
    public int PhysicalStrength => data.physicalStrength + (equippedItem?.data.equipPhysicalBonus ?? 0);
}
