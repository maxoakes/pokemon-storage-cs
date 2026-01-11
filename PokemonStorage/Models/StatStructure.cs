namespace PokemonStorage.Models;

public class StatStructure
{
    public bool IsModernSystemByDefault { get; }
    private StatHextuple BaseStats { get; }
    public StatSet Modern { get; }
    public StatSet Old { get; }

    public StatStructure(bool isModern, StatHextuple iv, StatHextuple ev, int speciesId, int level, Nature? nature = null)
    {
        IsModernSystemByDefault = isModern;
        BaseStats = Lookup.GetBaseStats(speciesId);

        if (IsModernSystemByDefault) 
        {
            // Set modern stats as-is
            Modern = new(iv, ev);

            // Perform a conversion to set old stat system
            Old = new(
                new StatHextuple(
                    (byte)(Modern.HP.Iv >> 1),
                    (byte)(Modern.Attack.Iv >> 1),
                    (byte)(Modern.Defense.Iv >> 1),
                    (byte)(Modern.Speed.Iv >> 1),
                    (byte)(Modern.SpecialAttack.Iv >> 1),
                    (byte)(Modern.SpecialDefense.Iv >> 1)
            ),
                new StatHextuple(
                    (ushort)(Modern.HP.Ev * 66),
                    (ushort)(Modern.Attack.Ev * 70),
                    (ushort)(Modern.Defense.Ev * 69),
                    (ushort)(Modern.Speed.Ev * 66),
                    (ushort)(Modern.SpecialAttack.Ev * 66),
                    (ushort)(Modern.SpecialDefense.Ev * 68)
                )
            );
        }
        // If input is old system
        else 
        {
            // Set old stats as-is
            Old = new(iv, ev);

            // Perform conversion to set modern legal stat system
            StatHextuple modernIv = new()
            {
                HP = (byte)((Old.HP.Iv * 2) + 1),
                Attack = (byte)((Old.Attack.Iv * 2) + 1),
                Defense = (byte)((Old.Defense.Iv * 2) + 1),
                Speed = (byte)((Old.Speed.Iv * 2) + 1),
                SpecialAttack = (byte)((Old.SpecialAttack.Iv * 2) + 1),
                SpecialDefense = (byte)((Old.SpecialDefense.Iv * 2) + 1),
            };

            StatHextuple modernEv = new()
            {
                HP = (ushort)(Old.HP.Ev / 66),
                Attack = (ushort)(Old.Attack.Ev / 70),
                Defense = (ushort)(Old.Defense.Ev / 69),
                Speed = (ushort)(Old.Speed.Ev / 66),
                SpecialAttack = (ushort)(Old.SpecialAttack.Ev / 66),
                SpecialDefense = (ushort)(Old.SpecialDefense.Ev / 68)
            };
            int sum = modernEv.HP + modernEv.Attack + modernEv.Defense + modernEv.Speed + modernEv.SpecialAttack + modernEv.SpecialDefense;

            if (sum > 510)
            {
                float decreaseFactor = sum / 510f;
                modernEv.HP = (ushort)Math.Floor(modernEv.HP / decreaseFactor);
                modernEv.Attack = (ushort)Math.Floor(modernEv.Attack / decreaseFactor);
                modernEv.Defense = (ushort)Math.Floor(modernEv.Defense / decreaseFactor);
                modernEv.Speed = (ushort)Math.Floor(modernEv.Speed / decreaseFactor);
                modernEv.SpecialAttack = (ushort)Math.Floor(modernEv.SpecialAttack / decreaseFactor);
                modernEv.SpecialDefense = (ushort)Math.Floor(modernEv.SpecialDefense / decreaseFactor);
            }
            Modern = new StatSet(modernIv, modernEv);
        }

        // Set the calculated final stat values for each system
        SetFinalOldSystemValues(level);
        SetFinalModernSystemValues(level, nature);
    }

    public void SetFinalOldSystemValues(int level)
    {
        Old.SetFinalValues(
            new(
                (ushort)(Math.Floor(((BaseStats.HP + Old.HP.Iv) * 2 + Math.Floor(Math.Ceiling(Math.Sqrt(Old.HP.Ev)) / 4)) * level / 100) + level + 10),
                (ushort)(Math.Floor(((BaseStats.Attack + Old.Attack.Iv) * 2 + Math.Floor(Math.Ceiling(Math.Sqrt(Old.Attack.Ev)) / 4)) * level / 100) + 5),
                (ushort)(Math.Floor(((BaseStats.Defense + Old.Defense.Iv) * 2 + Math.Floor(Math.Ceiling(Math.Sqrt(Old.Defense.Ev)) / 4)) * level / 100) + 5),
                (ushort)(Math.Floor(((BaseStats.SpecialAttack + Old.SpecialAttack.Iv) * 2 + Math.Floor(Math.Ceiling(Math.Sqrt(Old.SpecialAttack.Ev)) / 4)) * level / 100) + 5),
                (ushort)(Math.Floor(((BaseStats.SpecialDefense + Old.SpecialDefense.Iv) * 2 + Math.Floor(Math.Ceiling(Math.Sqrt(Old.SpecialDefense.Ev)) / 4)) * level / 100) + 5),
                (ushort)(Math.Floor(((BaseStats.Speed + Old.Speed.Iv) * 2 + Math.Floor(Math.Ceiling(Math.Sqrt(Old.Speed.Ev)) / 4)) * level / 100) + 5)
            )
        );
    }

    public void SetFinalModernSystemValues(int level, Nature? nature = null)
    {
        double modifiedAttack = 1;
        double modifiedDefense = 1;
        double modifiedSpecialAttack = 1;
        double modifiedSpecialDefense = 1;
        double modifiedSpeed = 1;

        if (nature.HasValue)
        {
            switch (nature.Value.IncreaseId)
            {
                case 2:
                    modifiedAttack = 1.1;
                    break;
                case 3:
                    modifiedDefense = 1.1;
                    break;
                case 4:
                    modifiedSpecialAttack = 1.1;
                    break;
                case 5:
                    modifiedSpecialDefense = 1.1;
                    break;
                case 6:
                    modifiedSpeed = 1.1;
                    break;
                default:
                    break;
            }

            switch (nature.Value.DecreaseId)
            {
                case 2:
                    modifiedAttack = 0.9;
                    break;
                case 3:
                    modifiedDefense = 0.9;
                    break;
                case 4:
                    modifiedSpecialAttack = 0.9;
                    break;
                case 5:
                    modifiedSpecialDefense = 0.9;
                    break;
                case 6:
                    modifiedSpeed = 0.9;
                    break;
                default:
                    break;
            }
        }

        Modern.SetFinalValues(
            new(
                (ushort)(Math.Floor((2 * BaseStats.HP + Modern.HP.Iv + Math.Floor(Modern.HP.Ev / 4.0)) * level / 100.0) + level + 10),
                (ushort)Math.Floor((Math.Floor((2 * BaseStats.Attack + Modern.Attack.Iv + Math.Floor(Modern.Attack.Ev / 4.0)) * level / 100.0) + 5) * modifiedAttack),
                (ushort)Math.Floor((Math.Floor((2 * BaseStats.Defense + Modern.Defense.Iv + Math.Floor(Modern.Defense.Ev / 4.0)) * level / 100.0) + 5) * modifiedDefense),
                (ushort)Math.Floor((Math.Floor((2 * BaseStats.SpecialAttack + Modern.SpecialAttack.Iv + Math.Floor(Modern.SpecialAttack.Ev / 4.0)) * level / 100.0) + 5) * modifiedSpecialAttack),
                (ushort)Math.Floor((Math.Floor((2 * BaseStats.SpecialDefense + Modern.SpecialDefense.Iv + Math.Floor(Modern.SpecialDefense.Ev / 4.0)) * level / 100.0) + 5) * modifiedSpecialDefense),
                (ushort)Math.Floor((Math.Floor((2 * BaseStats.Speed + Modern.Speed.Iv + Math.Floor(Modern.Speed.Ev / 4.0)) * level / 100.0) + 5) * modifiedSpeed)
            )
        );
    }
}