namespace PokemonStorageLibrary.Models;

public class StatSet {
    public Stat HP { get; private set; }
    public Stat Attack { get; private set; }
    public Stat Defense { get; private set; }
    public Stat Speed { get; private set; }
    public Stat SpecialAttack { get; private set; }
    public Stat SpecialDefense { get; private set; }

    public StatSet(StatHextuple iv, StatHextuple ev)
    {
        HP = new(ev.HP, (byte)iv.HP);
        Attack = new(ev.Attack, (byte)iv.Attack);
        Defense = new(ev.Defense, (byte)iv.Defense);
        Speed = new(ev.Speed, (byte)iv.Speed);
        SpecialAttack = new(ev.SpecialAttack, (byte)iv.SpecialAttack);
        SpecialDefense = new(ev.SpecialDefense, (byte)iv.SpecialDefense);
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