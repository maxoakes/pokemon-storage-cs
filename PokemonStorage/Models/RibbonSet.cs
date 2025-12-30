using System;
using System.Reflection;
using Microsoft.Data.Sqlite;
using PokemonStorage.DatabaseIO;

namespace PokemonStorage.Models;

public class RibbonSet
{
    // Sinnoh Set 1
    public bool SinnohChamp { get; set; } = false;
    public bool Ability { get; set; } = false;
    public bool GreatAbility { get; set; } = false;
    public bool DoubleAbility { get; set; } = false;
    public bool MultiAbility { get; set; } = false;
    public bool PairAbility { get; set; } = false;
    public bool WorldAbility { get; set; } = false;
    public bool Alert { get; set; } = false;
    public bool Shock { get; set; } = false;
    public bool Downcast { get; set; } = false;
    public bool Careless { get; set; } = false;
    public bool Relax { get; set; } = false;
    public bool Snooze { get; set; } = false;
    public bool Smile { get; set; } = false;
    public bool Gorgeous { get; set; } = false;
    public bool Royal { get; set; } = false;
    public bool GorgeousRoyal { get; set; } = false;
    public bool Footprint { get; set; } = false;
    public bool Record { get; set; } = false;
    public bool History { get; set; } = false;
    public bool Legend { get; set; } = false;
    public bool Red { get; set; } = false;
    public bool Green { get; set; } = false;
    public bool Blue { get; set; } = false;
    public bool Festival { get; set; } = false;
    public bool Carnival { get; set; } = false;
    public bool Classic { get; set; } = false;
    public bool Premier { get; set; } = false;

    // Sinnoh Set 2
    public byte SinnohCool { get; set; } = 0;
    public byte SinnohBeauty { get; set; } = 0;
    public byte SinnohCute { get; set; } = 0;
    public byte SinnohSmart { get; set; } = 0;
    public byte SinnohTough { get; set; } = 0;

    // Hoenn Set
    public byte HeonnCool { get; set; } = 0;
    public byte HeonnBeauty { get; set; } = 0;
    public byte HeonnCute { get; set; } = 0;
    public byte HeonnSmart { get; set; } = 0;
    public byte HeonnTough { get; set; } = 0;
    public bool Champion { get; set; } = false;
    public bool Winning { get; set; } = false;
    public bool Victory { get; set; } = false;
    public bool Artist { get; set; } = false;
    public bool Effort { get; set; } = false;
    public bool Marine { get; set; } = false;
    public bool Land { get; set; } = false;
    public bool Sky { get; set; } = false;
    public bool Country { get; set; } = false;
    public bool National { get; set; } = false;
    public bool Earth { get; set; } = false;
    public bool World { get; set; } = false;

    public RibbonSet()
    {

    }

    /// <summary>
    /// Parse a gen 4 ribbon set data value. Modes: 0=HoennSet, 1=SinnohSet1, 2=SinnohSet2
    /// </summary>
    /// <param name="mode"></param>
    public void ParseRibbonSet(int mode, byte[] data)
    {
        switch (mode)
        {
            case 0:
                HeonnCool =     (byte)(data[0] & 0xF);
                HeonnBeauty =   (byte)(data[0] >> 4 & 0xF);
                HeonnCute =     (byte)(data[1] & 0xF);
                HeonnSmart =    (byte)(data[1] >> 4 & 0xF);
                HeonnTough =    (byte)(data[2] & 0xF);
                Champion =  (data[2] & 0x10) != 0;
                Winning =   (data[2] & 0x20) != 0;
                Victory =   (data[2] & 0x40) != 0;
                Artist =    (data[2] & 0x80) != 0;
                Effort =    (data[3] & 0x01) != 0;
                Marine =    (data[3] & 0x02) != 0;
                Land =      (data[3] & 0x04) != 0;
                Sky =       (data[3] & 0x08) != 0;
                Country =   (data[3] & 0x10) != 0;
                National =  (data[3] & 0x20) != 0;
                Earth =     (data[3] & 0x40) != 0;
                World =     (data[3] & 0x80) != 0;
                break;
            case 1:
                SinnohChamp =   (data[0] & 0x01) != 0;
                Ability =       (data[0] & 0x02) != 0;
                GreatAbility =  (data[0] & 0x04) != 0;
                DoubleAbility = (data[0] & 0x08) != 0;
                MultiAbility =  (data[0] & 0x10) != 0;
                PairAbility =   (data[0] & 0x20) != 0;
                WorldAbility =  (data[0] & 0x40) != 0;
                Alert =         (data[0] & 0x80) != 0;
                Shock =         (data[1] & 0x01) != 0;
                Downcast =      (data[1] & 0x02) != 0;
                Careless =      (data[1] & 0x04) != 0;
                Relax =         (data[1] & 0x08) != 0;
                Snooze =        (data[1] & 0x10) != 0;
                Smile =         (data[1] & 0x20) != 0;
                Gorgeous =      (data[1] & 0x40) != 0;
                Royal =         (data[1] & 0x80) != 0;
                GorgeousRoyal = (data[2] & 0x01) != 0;
                Footprint =     (data[2] & 0x02) != 0;
                Record =        (data[2] & 0x04) != 0;
                History =       (data[2] & 0x08) != 0;
                Legend =        (data[2] & 0x10) != 0;
                Red =           (data[2] & 0x20) != 0;
                Green =         (data[2] & 0x40) != 0;
                Blue =          (data[2] & 0x80) != 0;
                Festival =      (data[3] & 0x01) != 0;
                Carnival =      (data[3] & 0x02) != 0;
                Classic =       (data[3] & 0x04) != 0;
                Premier =       (data[3] & 0x08) != 0;
                break;
            case 2:
                SinnohCool =    (byte)(data[0] & 0xF);
                SinnohBeauty =  (byte)(data[0] >> 4 & 0xF);
                SinnohCute =    (byte)(data[1] & 0xF);
                SinnohSmart =   (byte)(data[1] >> 4 & 0xF);
                SinnohTough =   (byte)(data[2] & 0xF);
                break;
            default:
                break;
        }
    }

    public List<SqliteParameter> GetSqliteParameters()
    {
        var parameters = new List<SqliteParameter>();
        foreach (PropertyInfo prop in typeof(RibbonSet).GetProperties())
        {
            parameters.Add(new SqliteParameter(prop.Name, SqliteType.Integer) { Value = prop.GetValue(this) });
        }
        return parameters;
    }

    public int InsertIntoDatabase()
    {
        return (int)DbInterface.RetrieveScalar("""
        INSERT INTO ribbon (
            sinnoh_champ,
            ability,
            great_ability,
            double_ability,
            multi_ability,
            pair_ability,
            world_ability,
            alert,
            shock,
            downcast,
            careless,
            relax,
            snooze,
            smile,
            gorgeous,
            royal,
            gorgeous_royal,
            footprint,
            record,
            history,
            legend,
            red,
            green,
            blue,
            festival,
            carnival,
            classic,
            premier,
            sinnoh_cool,
            sinnoh_beauty,
            sinnoh_cute,
            sinnoh_smart,
            sinnoh_tough,
            heonn_cool,
            heonn_beauty,
            heonn_cute,
            heonn_smart,
            heonn_tough,
            champion,
            winning,
            victory,
            artist,
            effort,
            marine,
            land,
            sky,
            country,
            national,
            earth,
            world
        ) VALUES (
            @SinnohChamp
            @Ability
            @GreatAbility
            @DoubleAbility
            @MultiAbility
            @PairAbility
            @WorldAbility
            @Alert
            @Shock
            @Downcast
            @Careless
            @Relax
            @Snooze
            @Smile
            @Gorgeous
            @Royal
            @GorgeousRoyal
            @Footprint
            @Record
            @History
            @Legend
            @Red
            @Green
            @Blue
            @Festival
            @Carnival
            @Classic
            @Premier
            @SinnohCool
            @SinnohBeauty
            @SinnohCute
            @SinnohSmart
            @SinnohTough
            @HeonnCool
            @HeonnBeauty
            @HeonnCute
            @HeonnSmart
            @HeonnTough
            @Champion
            @Winning
            @Victory
            @Artist
            @Effort
            @Marine
            @Land
            @Sky
            @Country
            @National
            @Earth
            @World
        ); SELECT last_insert_rowid();
        """, "storage", GetSqliteParameters());
    }

    public override string ToString()
    {
        return "";
    }
}