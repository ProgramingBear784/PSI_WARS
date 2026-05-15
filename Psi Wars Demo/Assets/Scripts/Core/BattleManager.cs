using System.Collections.Generic;
using System.Text;

/// <summary>
/// Pure battle resolution. Handles one attacker vs one defender at a time.
/// Called by GameUI during Battle Phase.
/// </summary>
public static class BattleManager
{
    public class BattleResult
    {
        public CardInstance attacker;
        public CardInstance defender;   // null = unblocked

        public int atkCyberTotal, defCyberTotal;
        public int atkPsionicTotal, defPsionicTotal;
        public int atkPhysicalTotal, defPhysicalTotal;

        public bool attackerDisoriented;
        public bool defenderDisoriented;
        public bool attackerDestroyed;
        public bool defenderDestroyed;

        public bool isUnblocked;
        public int  labDamage;          // Damage dealt to opponent Lab if unblocked

        public string log;
    }

    // ── Entry points ──────────────────────────────────────────────────────────

    /// <summary>Resolve an unblocked attacker — deals Physical damage to opponent Lab.</summary>
    public static BattleResult ResolveUnblocked(CardInstance attacker, PlayerState opponent)
    {
        int damage = attacker.PhysicalStrength;
        opponent.labHP -= damage;

        return new BattleResult
        {
            attacker   = attacker,
            isUnblocked = true,
            labDamage  = damage,
            log        = $"{attacker.data.cardName} is UNBLOCKED — deals {damage} damage to Lab!"
        };
    }

    /// <summary>Resolve full combat between an attacker and a defender.</summary>
    public static BattleResult ResolveVs(CardInstance atk, CardInstance def)
    {
        var result = new BattleResult { attacker = atk, defender = def };
        var log    = new StringBuilder();

        bool atkCyberDie    = true;
        bool defCyberDie    = true;
        bool atkPsionicDie  = true;
        bool defPsionicDie  = true;

        int atkExtraPhys = 0;
        int defExtraPhys = 0;
        bool atkCanReroll = false;
        bool defCanReroll = false;

        // ── Digital Battle ────────────────────────────────────────────────────
        if (atk.CyberStrength >= 1 && def.CyberStrength >= 1)
        {
            var atkR = GameManager.ResolveCyberDie(GameManager.RollCyber());
            var defR = GameManager.ResolveCyberDie(GameManager.RollCyber());

            log.Append($"[Cyber] Atk rolled {atkR.roll}, Def rolled {defR.roll}. ");

            // Obliteration: if both → both dice removed; if one → wins and removes other's die
            if (atkR.obliteration && defR.obliteration)
            {
                atkCyberDie = defCyberDie = false;
                log.Append("Both Obliterate — both Cyber dice removed. ");
            }
            else if (atkR.obliteration)
            {
                defCyberDie = false;
                log.Append($"Attacker Obliterates — Defender Cyber die removed! ");
            }
            else if (defR.obliteration)
            {
                atkCyberDie = false;
                log.Append($"Defender Obliterates — Attacker Cyber die removed! ");
            }

            int atkCyberBonus = atkCyberDie ? atkR.cyberBonus : 0;
            int defCyberBonus = defCyberDie ? defR.cyberBonus : 0;

            if (atkCyberDie && atkR.canRerollPhysical) atkCanReroll = true;
            if (defCyberDie && defR.canRerollPhysical) defCanReroll = true;
            if (atkCyberDie) atkExtraPhys += atkR.physicalBonus;
            if (defCyberDie) defExtraPhys += defR.physicalBonus;

            result.atkCyberTotal = atk.CyberStrength + atkCyberBonus;
            result.defCyberTotal = def.CyberStrength + defCyberBonus;

            log.Append($"Totals: Atk {result.atkCyberTotal} vs Def {result.defCyberTotal}. ");
            ApplyDisorientation(result, log, result.atkCyberTotal, result.defCyberTotal, "Cyber");
        }

        // ── Psionic Battle ────────────────────────────────────────────────────
        if (atk.PsionicStrength >= 1 && def.PsionicStrength >= 1)
        {
            var atkR = GameManager.ResolvePsionicDie(GameManager.RollPsionic());
            var defR = GameManager.ResolvePsionicDie(GameManager.RollPsionic());

            log.Append($"[Psionic] Atk rolled {atkR.roll}, Def rolled {defR.roll}. ");

            if (atkR.obliteration && defR.obliteration)
            {
                atkPsionicDie = defPsionicDie = false;
                log.Append("Both Obliterate — both Psionic dice removed. ");
            }
            else if (atkR.obliteration)
            {
                defPsionicDie = false;
                log.Append("Attacker Obliterates — Defender Psionic die removed! ");
            }
            else if (defR.obliteration)
            {
                atkPsionicDie = false;
                log.Append("Defender Obliterates — Attacker Psionic die removed! ");
            }

            int atkPsioBonus = atkPsionicDie ? atkR.psionicBonus : 0;
            int defPsioBonus = defPsionicDie ? defR.psionicBonus : 0;

            if (atkPsionicDie && atkR.canRerollPhysical) atkCanReroll = true;
            if (defPsionicDie && defR.canRerollPhysical) defCanReroll = true;
            if (atkPsionicDie) atkExtraPhys += atkR.physicalBonus;
            if (defPsionicDie) defExtraPhys += defR.physicalBonus;

            result.atkPsionicTotal = atk.PsionicStrength + atkPsioBonus;
            result.defPsionicTotal = def.PsionicStrength + defPsioBonus;

            log.Append($"Totals: Atk {result.atkPsionicTotal} vs Def {result.defPsionicTotal}. ");
            ApplyDisorientation(result, log, result.atkPsionicTotal, result.defPsionicTotal, "Psionic");
        }

        // ── Physical Battle ───────────────────────────────────────────────────
        {
            int atkRoll = GameManager.RollPhysical();
            int defRoll = GameManager.RollPhysical();

            // Optional rerolls from Cyber/Psionic die results
            if (atkCanReroll) { int r2 = GameManager.RollPhysical(); if (r2 > atkRoll) atkRoll = r2; }
            if (defCanReroll) { int r2 = GameManager.RollPhysical(); if (r2 > defRoll) defRoll = r2; }

            var atkPR = GameManager.ResolvePhysicalDie(atkRoll);
            var defPR = GameManager.ResolvePhysicalDie(defRoll);

            log.Append($"[Physical] Atk rolled {atkRoll}, Def rolled {defRoll}. ");

            // Obliteration on physical die
            if (atkPR.obliteration && defPR.obliteration)
                log.Append("Both Physical Obliterate — cancel out. ");
            else if (atkPR.obliteration)
                log.Append("Attacker Physical Obliterates — Defender Physical die removed! ");
            else if (defPR.obliteration)
                log.Append("Defender Physical Obliterates — Attacker Physical die removed! ");

            int atkPhysDieBonus = atkPR.obliteration && defPR.obliteration ? 1 : atkPR.physicalBonus;
            int defPhysDieBonus = atkPR.obliteration && defPR.obliteration ? 1 : defPR.physicalBonus;
            if (atkPR.obliteration && !defPR.obliteration) { defPhysDieBonus = 0; }
            if (defPR.obliteration && !atkPR.obliteration) { atkPhysDieBonus = 0; }

            // Disoriented units deal 0 physical damage
            result.atkPhysicalTotal = result.attackerDisoriented
                ? 0 : atk.PhysicalStrength + atkPhysDieBonus + atkExtraPhys;
            result.defPhysicalTotal = result.defenderDisoriented
                ? 0 : def.PhysicalStrength + defPhysDieBonus + defExtraPhys;

            log.Append($"Totals: Atk {result.atkPhysicalTotal} vs Def {result.defPhysicalTotal}. ");

            if (result.atkPhysicalTotal > result.defPhysicalTotal)
            {
                result.defenderDestroyed = true;
                log.Append("DEFENDER DESTROYED!");
            }
            else if (result.defPhysicalTotal > result.atkPhysicalTotal)
            {
                result.attackerDestroyed = true;
                log.Append("ATTACKER DESTROYED!");
            }
            else
            {
                log.Append("Tie — no effect.");
            }
        }

        result.log = log.ToString();
        return result;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void ApplyDisorientation(BattleResult r, StringBuilder log, int atkTotal, int defTotal, string type)
    {
        if (atkTotal > defTotal)
        {
            r.defenderDisoriented = true;
            r.defender.isDisoriented = true;
            log.Append($"Defender DISORIENTED ({type}). ");
        }
        else if (defTotal > atkTotal)
        {
            r.attackerDisoriented = true;
            r.attacker.isDisoriented = true;
            log.Append($"Attacker DISORIENTED ({type}). ");
        }
    }

    /// <summary>Apply destruction results to player states.</summary>
    public static void ApplyResults(List<BattleResult> results, PlayerState attacker, PlayerState defender)
    {
        foreach (var r in results)
        {
            if (r.attackerDestroyed && r.attacker != null) attacker.DestroyUnit(r.attacker);
            if (r.defenderDestroyed && r.defender != null) defender.DestroyUnit(r.defender);
        }
    }
}
