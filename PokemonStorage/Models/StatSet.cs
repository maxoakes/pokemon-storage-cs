namespace PokemonStorage.Models;

public class StatSet
{
    public bool IsModernSystem;
    public Stat HP { get; private set; }
    public Stat Attack { get; private set; }
    public Stat Defense { get; private set; }
    public Stat Speed { get; private set; }
    public Stat SpecialAttack { get; private set; }
    public Stat SpecialDefense { get; private set; }

    public StatSet(bool isModern, StatHextuple iv, StatHextuple ev)
    {
        IsModernSystem = isModern;
        HP = new(0, iv.HP, ev.HP);
        Attack = new(0, iv.Attack, ev.Attack);
        Defense = new(0, iv.Defense, ev.Defense);
        Speed = new(0, iv.Speed, ev.Speed);
        SpecialAttack = new(0, iv.SpecialAttack, ev.SpecialAttack);
        SpecialDefense = new(0, iv.SpecialDefense, ev.SpecialDefense);
    }

    public StatSet AsOldSystem()
    {
        if (!IsModernSystem) return this; 

        StatSet oldSystem = new(false, new StatHextuple(
            (byte)(HP.Iv >> 1),
            (byte)(Attack.Iv >> 1),
            (byte)(Defense.Iv >> 1),
            (byte)(Speed.Iv >> 1),
            (byte)(SpecialAttack.Iv >> 1),
            (byte)(SpecialDefense.Iv >> 1)
        ),
        new StatHextuple(
            (ushort)(HP.Ev * 66),
            (ushort)(Attack.Ev * 70),
            (ushort)(Defense.Ev * 69),
            (ushort)(Speed.Ev * 66),
            (ushort)(SpecialAttack.Ev * 66),
            (ushort)(SpecialDefense.Ev * 68)
        ));
        return oldSystem;
    }

    public StatSet AsModernSystem(StatHextuple baseStats, byte level, Nature nature)
    {
        StatSet modernSet;
        if (IsModernSystem)
        {
            modernSet = this;
        }
        else
        {
            StatHextuple iv = new()
            {
                HP = (byte)((HP.Iv * 2) + 1),
                Attack = (byte)((Attack.Iv * 2) + 1),
                Defense = (byte)((Defense.Iv * 2) + 1),
                Speed = (byte)((Speed.Iv * 2) + 1),
                SpecialAttack = (byte)((SpecialAttack.Iv * 2) + 1),
                SpecialDefense = (byte)((SpecialDefense.Iv * 2) + 1),
            };

            StatHextuple ev = new()
            {
                HP = (ushort)(HP.Ev / 66),
                Attack = (ushort)(Attack.Ev / 70),
                Defense = (ushort)(Defense.Ev / 69),
                Speed = (ushort)(Speed.Ev / 66),
                SpecialAttack = (ushort)(SpecialAttack.Ev / 66),
                SpecialDefense = (ushort)(SpecialDefense.Ev / 68)
            };
            int sum = ev.HP + ev.Attack + ev.Defense + ev.Speed + ev.SpecialAttack + ev.SpecialDefense;

            if (sum > 510)
            {
                float decreaseFactor = sum / 510f;
                ev.HP = (ushort)Math.Floor(ev.HP / decreaseFactor);
                ev.Attack = (ushort)Math.Floor(ev.Attack / decreaseFactor);
                ev.Defense = (ushort)Math.Floor(ev.Defense / decreaseFactor);
                ev.Speed = (ushort)Math.Floor(ev.Speed / decreaseFactor);
                ev.SpecialAttack = (ushort)Math.Floor(ev.SpecialAttack / decreaseFactor);
                ev.SpecialDefense = (ushort)Math.Floor(ev.SpecialDefense / decreaseFactor);
            }
            modernSet = new StatSet(true, iv, ev);
        }

        modernSet.SetFinalStatValues(baseStats, level, nature);
        return modernSet;
    }

    private void SetFinalStatValues(StatHextuple baseStats, byte level, Nature nature)
    {
        if (IsModernSystem)
        {
            double modifiedAttack = 1;
            double modifiedDefense = 1;
            double modifiedSpecialAttack = 1;
            double modifiedSpecialDefense = 1;
            double modifiedSpeed = 1;

            switch (nature.IncreaseId)
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

            switch (nature.DecreaseId)
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

            HP.Value = (ushort)(Math.Floor((2 * baseStats.HP + HP.Iv + Math.Floor(HP.Ev / 4.0)) * level / 100.0) + level + 10);
            Attack.Value = (ushort)Math.Floor((Math.Floor((2 * baseStats.Attack + Attack.Iv + Math.Floor(Attack.Ev / 4.0)) * level / 100.0) + 5) * modifiedAttack);
            Defense.Value = (ushort)Math.Floor((Math.Floor((2 * baseStats.Defense + Defense.Iv + Math.Floor(Defense.Ev / 4.0)) * level / 100.0) + 5) * modifiedDefense);
            SpecialAttack.Value = (ushort)Math.Floor((Math.Floor((2 * baseStats.SpecialAttack + SpecialAttack.Iv + Math.Floor(SpecialAttack.Ev / 4.0)) * level / 100.0) + 5) * modifiedSpecialAttack);
            SpecialDefense.Value = (ushort)Math.Floor((Math.Floor((2 * baseStats.SpecialDefense + SpecialDefense.Iv + Math.Floor(SpecialDefense.Ev / 4.0)) * level / 100.0) + 5) * modifiedSpecialDefense);
            Speed.Value = (ushort)Math.Floor((Math.Floor((2 * baseStats.Speed + Speed.Iv + Math.Floor(Speed.Ev / 4.0)) * level / 100.0) + 5) * modifiedSpeed);
        }
        else
        {
            HP.Value = (ushort)(Math.Floor(((baseStats.HP + HP.Iv) * 2 + Math.Floor(Math.Ceiling(Math.Sqrt(HP.Ev)) / 4)) * level / 100) + level + 10);
            Attack.Value = (ushort)(Math.Floor(((baseStats.Attack + Attack.Iv) * 2 + Math.Floor(Math.Ceiling(Math.Sqrt(Attack.Ev)) / 4)) * level / 100) + 5);
            Defense.Value = (ushort)(Math.Floor(((baseStats.Defense + Defense.Iv) * 2 + Math.Floor(Math.Ceiling(Math.Sqrt(Defense.Ev)) / 4)) * level / 100) + 5);
            SpecialAttack.Value = (ushort)(Math.Floor(((baseStats.SpecialAttack + SpecialAttack.Iv) * 2 + Math.Floor(Math.Ceiling(Math.Sqrt(SpecialAttack.Ev)) / 4)) * level / 100) + 5);
            SpecialDefense.Value = (ushort)(Math.Floor(((baseStats.SpecialDefense + SpecialDefense.Iv) * 2 + Math.Floor(Math.Ceiling(Math.Sqrt(SpecialDefense.Ev)) / 4)) * level / 100) + 5);
            Speed.Value = (ushort)(Math.Floor(((baseStats.Speed + Speed.Iv) * 2 + Math.Floor(Math.Ceiling(Math.Sqrt(Speed.Ev)) / 4)) * level / 100) + 5);
        }
    }
}