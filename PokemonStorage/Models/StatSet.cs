namespace PokemonStorage.Models;

public class StatSet {
    public Stat HP { get; private set; }
    public Stat Attack { get; private set; }
    public Stat Defense { get; private set; }
    public Stat Speed { get; private set; }
    public Stat SpecialAttack { get; private set; }
    public Stat SpecialDefense { get; private set; }

    public StatSet(StatHextuple iv, StatHextuple ev)
    {
        HP = new(iv.HP, (byte)ev.HP);
        Attack = new(iv.Attack, (byte)ev.Attack);
        Defense = new(iv.Defense, (byte)ev.Defense);
        Speed = new(iv.Speed, (byte)ev.Speed);
        SpecialAttack = new(iv.SpecialAttack, (byte)ev.SpecialAttack);
        SpecialDefense = new(iv.SpecialDefense, (byte)ev.SpecialDefense);
    }

    public void SetFinalValues(StatHextuple stats)
    {
        HP.Value = stats.HP;
        Attack.Value = stats.Attack;
        Defense.Value = stats.Defense;
        Speed.Value = stats.Speed;
        SpecialAttack.Value = stats.SpecialAttack;
        SpecialDefense.Value = stats.SpecialDefense;
    }
}