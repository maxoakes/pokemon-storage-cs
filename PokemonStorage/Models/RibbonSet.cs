using System;
using System.Reflection;

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
    public bool SinnohCool { get; set; } = false;
    public bool SinnohCoolGreat { get; set; } = false;
    public bool SinnohCoolUltra { get; set; } = false;
    public bool SinnohCoolMaster { get; set; } = false;
    public bool SinnohBeauty { get; set; } = false;
    public bool SinnohBeautyGreat { get; set; } = false;
    public bool SinnohBeautyUltra { get; set; } = false;
    public bool SinnohBeautyMaster { get; set; } = false;
    public bool SinnohCute { get; set; } = false;
    public bool SinnohCuteGreat { get; set; } = false;
    public bool SinnohCuteUltra { get; set; } = false;
    public bool SinnohCuteMaster { get; set; } = false;
    public bool SinnohSmart { get; set; } = false;
    public bool SinnohSmartGreat { get; set; } = false;
    public bool SinnohSmartUltra { get; set; } = false;
    public bool SinnohSmartMaster { get; set; } = false;
    public bool SinnohTough { get; set; } = false;
    public bool SinnohToughGreat { get; set; } = false;
    public bool SinnohToughUltra { get; set; } = false;
    public bool SinnohToughMaster { get; set; } = false;

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

    public override string ToString()
    {
        return "";
    }
}