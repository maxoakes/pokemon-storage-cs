using System.Data;
using Microsoft.Data.Sqlite;

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

    public int InsertIntoDatabase()
    {
        List<SqliteParameterPair> parameterPairs =
        [
            new SqliteParameterPair("sinnoh_champ", SqliteType.Integer, SinnohChamp),
            new SqliteParameterPair("ability", SqliteType.Integer, Ability),
            new SqliteParameterPair("great_ability", SqliteType.Integer, GreatAbility),
            new SqliteParameterPair("double_ability", SqliteType.Integer, DoubleAbility),
            new SqliteParameterPair("multi_ability", SqliteType.Integer, MultiAbility),
            new SqliteParameterPair("pair_ability", SqliteType.Integer, PairAbility),
            new SqliteParameterPair("world_ability", SqliteType.Integer, WorldAbility),
            new SqliteParameterPair("alert", SqliteType.Integer, Alert),
            new SqliteParameterPair("shock", SqliteType.Integer, Shock),
            new SqliteParameterPair("downcast", SqliteType.Integer, Downcast),
            new SqliteParameterPair("careless", SqliteType.Integer, Careless),
            new SqliteParameterPair("relax", SqliteType.Integer, Relax),
            new SqliteParameterPair("snooze", SqliteType.Integer, Snooze),
            new SqliteParameterPair("smile", SqliteType.Integer, Smile),
            new SqliteParameterPair("gorgeous", SqliteType.Integer, Gorgeous),
            new SqliteParameterPair("royal", SqliteType.Integer, Royal),
            new SqliteParameterPair("gorgeous_royal", SqliteType.Integer, GorgeousRoyal),
            new SqliteParameterPair("footprint", SqliteType.Integer, Footprint),
            new SqliteParameterPair("record", SqliteType.Integer, Record),
            new SqliteParameterPair("history", SqliteType.Integer, History),
            new SqliteParameterPair("legend", SqliteType.Integer, Legend),
            new SqliteParameterPair("red", SqliteType.Integer, Red),
            new SqliteParameterPair("green", SqliteType.Integer, Green),
            new SqliteParameterPair("blue", SqliteType.Integer, Blue),
            new SqliteParameterPair("festival", SqliteType.Integer, Festival),
            new SqliteParameterPair("carnival", SqliteType.Integer, Carnival),
            new SqliteParameterPair("classic", SqliteType.Integer, Classic),
            new SqliteParameterPair("premier", SqliteType.Integer, Premier),
            new SqliteParameterPair("sinnoh_cool", SqliteType.Integer, SinnohCool),
            new SqliteParameterPair("sinnoh_beauty", SqliteType.Integer, SinnohBeauty),
            new SqliteParameterPair("sinnoh_cute", SqliteType.Integer, SinnohCute),
            new SqliteParameterPair("sinnoh_smart", SqliteType.Integer, SinnohSmart),
            new SqliteParameterPair("sinnoh_tough", SqliteType.Integer, SinnohTough),
            new SqliteParameterPair("heonn_cool", SqliteType.Integer, HeonnCool),
            new SqliteParameterPair("heonn_beauty", SqliteType.Integer, HeonnBeauty),
            new SqliteParameterPair("heonn_cute", SqliteType.Integer, HeonnCute),
            new SqliteParameterPair("heonn_smart", SqliteType.Integer, HeonnSmart),
            new SqliteParameterPair("heonn_tough", SqliteType.Integer, HeonnTough),
            new SqliteParameterPair("champion", SqliteType.Integer, Champion),
            new SqliteParameterPair("winning", SqliteType.Integer, Winning),
            new SqliteParameterPair("victory", SqliteType.Integer, Victory),
            new SqliteParameterPair("artist", SqliteType.Integer, Artist),
            new SqliteParameterPair("effort", SqliteType.Integer, Effort),
            new SqliteParameterPair("marine", SqliteType.Integer, Marine),
            new SqliteParameterPair("land", SqliteType.Integer, Land),
            new SqliteParameterPair("sky", SqliteType.Integer, Sky),
            new SqliteParameterPair("country", SqliteType.Integer, Country),
            new SqliteParameterPair("national", SqliteType.Integer, National),
            new SqliteParameterPair("earth", SqliteType.Integer, Earth),
            new SqliteParameterPair("world", SqliteType.Integer, World)
        ];

        return DbInterface.InsertIntoDatabase("ribbon", parameterPairs, "storage");
    }

    public void LoadFromDatabase(int primaryKey)
    {
        List<SqliteParameter> parameters = [
            new SqliteParameter("Id", SqliteType.Integer) { Value = primaryKey }
        ];

        DataTable dataTable = DbInterface.RetrieveTable("SELECT * FROM ribbon WHERE id=@Id", "storage", parameters);

        if (dataTable.Rows.Count == 0)
        {
            throw new Exception($"No ribbons found with primary key {primaryKey}");
        }

        foreach (DataRow row in dataTable.Rows)
        {
            SinnohChamp =   row.Field<Int64>("sinnoh_champ") == 1;
            Ability =       row.Field<Int64>("ability") == 1;
            GreatAbility =  row.Field<Int64>("great_ability") == 1;
            DoubleAbility = row.Field<Int64>("double_ability") == 1;
            MultiAbility =  row.Field<Int64>("multi_ability") == 1;
            PairAbility =   row.Field<Int64>("pair_ability") == 1;
            WorldAbility =  row.Field<Int64>("world_ability") == 1;
            Alert =         row.Field<Int64>("alert") == 1;
            Shock =         row.Field<Int64>("shock") == 1;
            Downcast =      row.Field<Int64>("downcast") == 1;
            Careless =      row.Field<Int64>("careless") == 1;
            Relax =         row.Field<Int64>("relax") == 1;
            Snooze =        row.Field<Int64>("snooze") == 1;
            Smile =         row.Field<Int64>("smile") == 1;
            Gorgeous =      row.Field<Int64>("gorgeous") == 1;
            Royal =         row.Field<Int64>("royal") == 1;
            GorgeousRoyal = row.Field<Int64>("gorgeous_royal") == 1;
            Footprint =     row.Field<Int64>("footprint") == 1;
            Record =        row.Field<Int64>("record") == 1;
            History =       row.Field<Int64>("history") == 1;
            Legend =        row.Field<Int64>("legend") == 1;
            Red =           row.Field<Int64>("red") == 1;
            Green =         row.Field<Int64>("green") == 1;
            Blue =          row.Field<Int64>("blue") == 1;
            Festival =      row.Field<Int64>("festival") == 1;
            Carnival =      row.Field<Int64>("carnival") == 1;
            Classic =       row.Field<Int64>("classic") == 1;
            Premier =       row.Field<Int64>("premier") == 1;
            SinnohCool =    (byte)row.Field<Int64>("sinnoh_cool");
            SinnohBeauty =  (byte)row.Field<Int64>("sinnoh_beauty");
            SinnohCute =    (byte)row.Field<Int64>("sinnoh_cute");
            SinnohSmart =   (byte)row.Field<Int64>("sinnoh_smart");
            SinnohTough =   (byte)row.Field<Int64>("sinnoh_tough");
            HeonnCool =     (byte)row.Field<Int64>("heonn_cool");
            HeonnBeauty =   (byte)row.Field<Int64>("heonn_beauty");
            HeonnCute =     (byte)row.Field<Int64>("heonn_cute");
            HeonnSmart =    (byte)row.Field<Int64>("heonn_smart");
            HeonnTough =    (byte)row.Field<Int64>("heonn_tough");
            Champion =      row.Field<Int64>("champion") == 1;
            Winning =       row.Field<Int64>("winning") == 1;
            Victory =       row.Field<Int64>("victory") == 1;
            Artist =        row.Field<Int64>("artist") == 1;
            Effort =        row.Field<Int64>("effort") == 1;
            Marine =        row.Field<Int64>("marine") == 1;
            Land =          row.Field<Int64>("land") == 1;
            Sky =           row.Field<Int64>("sky") == 1;
            Country =       row.Field<Int64>("country") == 1;
            National =      row.Field<Int64>("national") == 1;
            Earth =         row.Field<Int64>("earth") == 1;
            World =         row.Field<Int64>("world") == 1;
        }
    }
        
    public override string ToString()
    {
        return "";
    }
}