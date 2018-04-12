﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;
using AnotherRpgMod.Utils;
using Terraria.ModLoader.IO;
using Terraria.GameInput;

namespace AnotherRpgMod.RPGModule
{
    

    public enum Stat : byte
    {
        Vit, //Vitality : Increase Health Points, Increase Health Regen, Increase defence (light)
        Foc, //Focus : Increase Mana, Increase Critical Rate, Increase Sumon Damage (light)
        Con, //Constitution : Increase Defences, Increase Health Regen, increase health (light)


        Str, //Strength : Increase Melee Damage, Increase Critical Damage, Increase Throw Damage (light)
        Agi, //Agility : Increase Ranged Damage, Increase Critical Damage, Increase Melee Damage (light)
        Dex, //Dexterity : Increase Throw Damage, Increase Critical Rate, Increase Ranged Damage (light)
        Int, //Intelect : Increase Magic Damage, Increase Mana Regen, Increase Mana (light)
        Spr //Spirit : Increase Summon Damage,  Increase Mana Regen, Increase Magic Damage (light)
    }


    namespace Entities {
        class StatData
        {
            private int natural;
            private int level;
            private int xp;

            public int AddLevel { get { return level; } }
            public int NaturalLevel { get { return natural; } }
            public int GetLevel { get { return level+ natural; } }
            public int GetXP { get { return xp; } }

            public int XpForLevel()
            {
                return Mathf.CeilInt(level * 0.04f) + 1;
            }
            public void AddXp(int _xp)
            {
                xp += _xp;
                while (xp >= XpForLevel())
                {
                    xp -= XpForLevel();
                    level=level +1;
                }
            }
            public StatData(int _natural, int _level = 0, int _xp = 0)
            {
                natural = _natural;
                xp = _xp;
                level = _level;
            }
            public void LevelUp()
            {
                natural++;
            }
            
        }

        class RpgStats
        {
            readonly int Default = 4;
            private Dictionary<Stat, StatData> ActualStat;

            public RpgStats()
            {
                ActualStat = new Dictionary<Stat, StatData>(8);
                for (int i = 0; i <= 7; i++)
                {
                    ActualStat.Add((Stat)i,new StatData(Default));
                }
            }

            public void SetStats(Stat _stat,int _natural,int _level,int _xp)
            {
                ActualStat[_stat] = new StatData(_natural, _level,_xp);
            }

            public int GetLevelStat(Stat a)
            {
                return ActualStat[a].AddLevel;
            }
            public int GetStat(Stat a)
            {
                return ActualStat[a].GetLevel;
            }
            public int GetNaturalStat(Stat a)
            {
                return ActualStat[a].NaturalLevel;
            }

            public void UpgradeStat(Stat statname,int value = 1)
            {
                ActualStat[statname].AddXp(value);
            }
            public int GetStatXP(Stat statname)
            {
                return ActualStat[statname].GetXP;
            }
            public int GetStatXPMax(Stat statname)
            {
                return ActualStat[statname].XpForLevel();
            }

            public void Reset(int level)
            {
                for (int i = 0; i <= 7; i++)
                {
                    ActualStat[(Stat)i] = new StatData(level + Default-1);
                }
            }

            public void OnLevelUp()
            {
                for (int i = 0; i <= 7; i++)
                {
                    ActualStat[(Stat)i].LevelUp();
                }
            }
        }

        class RPGPlayer : ModPlayer
        {
            private int Exp = 0;
            private RpgStats Stats;
            private SkillTree skilltree;
            public SkillTree GetskillTree => skilltree;

            private int level = 1;
            private int armor;
            public int BaseArmor { get { return armor; } }

            public long EquipedItemXp = 0;
            public long EquipedItemMaxXp = 1;

            public float statMultiplier = 1;

            public void SyncLevel(int _level) //only use for sync
            {
                level = _level;
            }
            public void SyncStat(StatData data ,Stat stat) //only use for sync
            {
                Stats.SetStats(stat,level, data.GetLevel, data.GetXP);
            }

            public int GetStatXP(Stat s)
            {
                return Stats.GetStatXP(s);
            }
            public int GetStatXPMax(Stat s)
            {
                return Stats.GetStatXPMax(s);
            }
            public int GetStat(Stat s)
            {
                return Stats.GetStat(s);
            }
            public int GetNaturalStat(Stat s)
            {
                return Stats.GetNaturalStat(s);
            }
            public int GetAddStat(Stat s)
            {
                return Stats.GetLevelStat(s);
            }
            public int GetLevel()
            {
                return level;
            }
            public int GetExp()
            {
                return Exp;
            }

            private int totalPoints = 0;
            private int freePoints = 0;


            public int FreePtns { get { return freePoints; } }
            public int TotalPtns { get { return totalPoints; } }

            private int BASEHEALTHPERHEART = 15;
            private int BASEMANAPERSTAR = 5;


            #region WEAPONXP
            private void AddWeaponXp(int damage)
            {
                if (damage < 0)
                    damage = -damage;
                if (player.HeldItem != null && player.HeldItem.damage > 0 && player.HeldItem.maxStack <= 1)
                {
                    Items.ItemUpdate Item = player.HeldItem.GetGlobalItem<Items.ItemUpdate>();
                    if (Item != null && Item.NeedsSaving(player.HeldItem))
                    {
                        Item.AddExp(Mathf.Ceillong(damage * 0.2f), player);
                    }
                }
            }

            public void Leech(int damage)
            {
                int lifeHeal = (int)(damage * GetLifeLeech());
                player.statLife = Mathf.Clamp(player.statLife + lifeHeal, 0, player.statLifeMax2);
                int manaHeal = (int)(player.statManaMax2 * GetManaLeech());
                player.statMana = Mathf.Clamp(player.statMana + manaHeal, 0, player.statManaMax2);
                if (lifeHeal > 0)
                    CombatText.NewText(player.getRect(), new Color(50, 255, 50), "+" + lifeHeal);
                if (manaHeal > 0)
                    CombatText.NewText(player.getRect(), new Color(50, 50, 255), "+" + manaHeal);
            }

            public override void ModifyHitNPC(Item item, NPC target, ref int damage, ref float knockback, ref bool crit)
            {
                if (crit)
                {
                    damage = (int)(0.5f * damage * GetCriticalDamage());
                }
                if (target.type != 488)
                    AddWeaponXp(damage);
                Leech(damage);

            }
            public override void ModifyHitNPCWithProj(Projectile proj, NPC target, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
            {
                
                if (crit)
                {
                    damage = (int)(0.5f * damage * GetCriticalDamage());
                }
                
                if (target.type != 488)
                    AddWeaponXp(damage/proj.penetrate);
                Leech(damage);


            }

            #endregion

            #region ARMORXP
            private void AddArmorXp(int damage)
            {
                damage = Mathf.Clamp(damage, 0, player.statLifeMax2);
                Item armorItem;
                for (int i = 0;i< 3; i++)
                {
                    armorItem = player.armor[i];
                    if (armorItem.Name!= "")
                    {
                        armorItem.GetGlobalItem<Items.ItemUpdate>().AddExp(Mathf.Ceillong(damage * 0.1f), player);
                    }
                    
                }
            }
            public override void OnHitByNPC(NPC npc, int damage, bool crit)
            {
                
                AddArmorXp(damage);
                base.OnHitByNPC(npc, damage, crit);
            }
            public override void OnHitByProjectile(Projectile proj, int damage, bool crit)
            {
                AddArmorXp(damage);
                base.OnHitByProjectile(proj, damage, crit);
            }

            #endregion



            public float GetDefenceMult()
            {
                    return (Stats.GetStat(Stat.Vit) * 0.02f + Stats.GetStat(Stat.Con) * 0.04f) * statMultiplier + 0.6f;
            }

            public float GetHealthPerHeart()
            {
                    return (Stats.GetStat(Stat.Vit) * 1.25f + Stats.GetStat(Stat.Con) * 0.625f)* statMultiplier;
            }
            public float GetManaPerStar()
            {
                    return (Stats.GetStat(Stat.Foc) * 0.2f + Stats.GetStat(Stat.Int) * 0.1f) * statMultiplier;
            }


            

            public override void PreUpdateBuffs()
            {
                if (player.onFire)
                {
                    player.statLife -= Mathf.CeilInt(player.statLifeMax2 * 0.003);
                }
                if (player.onFire2)
                {
                    player.statLife -= Mathf.CeilInt(player.statLifeMax2 * 0.005);
                }
                if (player.onFrostBurn)
                {
                    player.statLife -= Mathf.CeilInt(player.statLifeMax2 * 0.01);
                }

                if (Main.netMode != 2) { 
                    
                    player.meleeCrit = (int)(player.meleeCrit + GetCriticalChanceBonus());
                    player.thrownCrit = (int)(player.thrownCrit + GetCriticalChanceBonus());
                    player.magicCrit = (int)(player.magicCrit + GetCriticalChanceBonus());
                    player.rangedCrit = (int)(player.rangedCrit + GetCriticalChanceBonus());
                    

                    
                    if (Arpg.LoadedMods[SupportedMod.Thorium]) {
                        
                    }


                    if (player.HeldItem != null && player.HeldItem.damage>0&& player.HeldItem.maxStack <= 1) { 
                        Items.ItemUpdate Item = player.HeldItem.GetGlobalItem<Items.ItemUpdate>();
                        if (Item != null && Item.NeedsSaving(player.HeldItem))
                        {
                            EquipedItemXp = Item.GetXp;
                            EquipedItemMaxXp = Item.GetMaxXp;
                        }
                    
                    if (Arpg.LoadedMods[SupportedMod.Tremor]) {
                        
                    }


                    if (player.HeldItem != null && player.HeldItem.damage>0&& player.HeldItem.maxStack <= 1) { 
                        Items.ItemUpdate Item = player.HeldItem.GetGlobalItem<Items.ItemUpdate>();
                        if (Item != null && Item.NeedsSaving(player.HeldItem))
                        {
                            EquipedItemXp = Item.GetXp;
                            EquipedItemMaxXp = Item.GetMaxXp;
                        }
                    }
                }

            }
            private void UpdateThoriumDamage(Player player)
            {
                Mod Thorium = ModLoader.GetMod("Thorium");
                //player.GetModPlayer<Thorium.ThoriumPlayer>().symphonicDamage *= GetDamageMult(DamageType.Symphonic);
                //player.GetModPlayer<Thorium.ThoriumPlayer>().radiantBoost *= GetDamageMult(DamageType.Radiant);
            }
            private void UpdateTremorDamage(Player player)
            {
                Mod Tremor = ModLoader.GetMod("Tremor");
                //player.GetModPlayer<Thorium.ThoriumPlayer>().symphonicDamage *= GetDamageMult(DamageType.Symphonic);
                //player.GetModPlayer<Thorium.ThoriumPlayer>().radiantBoost *= GetDamageMult(DamageType.Radiant);
            }
            public override void PostUpdateEquips()
            {
                
                if (Main.netMode != 2) { 
                    armor = player.statDefense;
                    player.statLifeMax2 = (int)(player.statLifeMax2 * GetHealthPerHeart() / 20) + 10;
                    player.statManaMax2 = (int)(player.statManaMax2 * GetManaPerStar() / 20) + 4;
                    player.statDefense = (int)(player.statDefense * GetDefenceMult());
                    player.meleeDamage *= GetDamageMult(DamageType.Melee);
                    player.thrownDamage *= GetDamageMult(DamageType.Throw);
                    player.rangedDamage *= GetDamageMult(DamageType.Ranged);
                    player.magicDamage *= GetDamageMult(DamageType.Magic);
                    player.minionDamage *= GetDamageMult(DamageType.Summon);
                }
                player.lifeRegen *= Mathf.FloorInt(GetHealthRegen());
                player.manaRegenBonus *= Mathf.FloorInt(GetManaRegen());
            }




            public void SpendPoints(Stat _stat,int ammount)
            {
                ammount = Mathf.Clamp(ammount, 1, freePoints);
                Stats.UpgradeStat(_stat, ammount);
                freePoints -= ammount;
            }

            public void ResetStats()
            {
                Stats.Reset(level);
                freePoints = totalPoints;
            }

            public float GetCriticalChanceBonus()
            {
                float X = (Stats.GetStat(Stat.Foc) + Stats.GetStat(Stat.Dex))*0.05f;
                return X;
            }
            public float GetCriticalDamage()
            {
                float X = (Stats.GetStat(Stat.Agi) + Stats.GetStat(Stat.Str))*0.005f;
                return 1.4f+X;
            }

            public override void ProcessTriggers(TriggersSet triggersSet)
            {
                if (Arpg.StatsHotKey.JustPressed)
                {
                    Main.PlaySound(SoundID.MenuOpen);
                    UI.Stats.Instance.LoadChar();
                    UI.Stats.visible = !UI.Stats.visible;
                }
            }


            public float GetBonusHeal()
            {
                return GetHealthPerHeart() /20;
            }
            public float GetBonusHealMana()
            {
                return GetManaPerStar()/ 20;
            }

            public float GetHealthRegen()
            {
                return (Stats.GetStat(Stat.Vit) + Stats.GetStat(Stat.Con)) * 0.1f * statMultiplier;
            }
            public float GetManaRegen()
            {
                return (Stats.GetStat(Stat.Int) + Stats.GetStat(Stat.Spr)) * 0.1f * statMultiplier;
            }

            public float GetDamageMult(DamageType type)
            {

                switch (type)
                {
                    case DamageType.Magic:
                        return (Stats.GetStat(Stat.Int) * 0.03f + Stats.GetStat(Stat.Spr) * 0.03f) * statMultiplier + 0.4f;
                    case DamageType.Ranged:
                        return (Stats.GetStat(Stat.Agi) * 0.05f + Stats.GetStat(Stat.Dex) * 0.03f) * statMultiplier + 0.4f;
                    case DamageType.Summon:
                        return (Stats.GetStat(Stat.Spr) * 0.05f + Stats.GetStat(Stat.Foc) * 0.03f) * statMultiplier + 0.4f;
                    case DamageType.Throw:
                        return (Stats.GetStat(Stat.Dex) * 0.05f + Stats.GetStat(Stat.Str) * 0.03f) * statMultiplier + 0.4f;
                    case DamageType.Symphonic:
                        return (Stats.GetStat(Stat.Agi) * 0.025f + Stats.GetStat(Stat.Foc) * 0.025f) *statMultiplier + 0.4f;
                    case DamageType.Radiant:
                        return (Stats.GetStat(Stat.Int) * 0.025f + Stats.GetStat(Stat.Spr) * 0.025f) * statMultiplier + 0.4f;
                    case DamageType.Alchemic:
                        return (Stats.GetStat(Stat.Dex) * 0.05f + Stats.GetStat(Stat.Str) * 0.03f) * statMultiplier + 0.4f;
                    default:
                        return (Stats.GetStat(Stat.Str) * 0.05f + Stats.GetStat(Stat.Agi) * 0.03f) * statMultiplier + 0.4f;
                }
            }


            public void CheckExp()
            {
                while (this.Exp >= XPToNextLevel())
                {
                    this.Exp -= XPToNextLevel();
                    LevelUp();
                }
            }

            int ReduceExp(int xp, int _level)
            {
                int exp = xp;
                if (_level <= level - 5)
                {
                    float expMult = 1 - (level - _level) * 0.1f;
                    exp = (int)(exp * expMult);
                }

                if (exp < 1)
                    exp = 1;

                return exp;
            }
            public void AddXp(int exp,int _level)
            {
                exp = ReduceExp(exp, _level);
                if (exp >= XPToNextLevel() * 0.1f)
                {
                    CombatText.NewText(player.getRect(), new Color(50, 26, 255), exp + " XP !!");
                }
                else
                {
                    CombatText.NewText(player.getRect(), new Color(127, 159, 255), exp + " XP");
                }
                this.Exp += exp;
                CheckExp();
            }
            public void commandLevelup()
            {
                LevelUp();
            }
            private void SilentLevelUp()
            {
                int pointsToGain = 5 + Mathf.FloorInt(Mathf.Pow(level, 0.5f));
                totalPoints += pointsToGain;
                freePoints += pointsToGain;
                Stats.OnLevelUp();
                level++;
            }
            public void RecalculateStat()
            {
                int _level = level;
                level = 0;
                totalPoints = 0;
                freePoints = 0;
                Stats = new RpgStats();
                for (int i = 0; i< _level; i++)
                {
                    SilentLevelUp();
                }

            }

            public float GetLifeLeech()
            {
                if (player.HeldItem != null && player.HeldItem.damage > 0 && player.HeldItem.maxStack <= 1)
                {
                    Items.ItemUpdate Item = player.HeldItem.GetGlobalItem<Items.ItemUpdate>();
                    if (Item != null && Item.NeedsSaving(player.HeldItem))
                    {
                        return Item.GetLifeLeech*0.01f;
                    }
                }
                return 0;
            }
            public float GetManaLeech()
            {
                if (player.HeldItem != null && player.HeldItem.damage > 0 && player.HeldItem.maxStack <= 1)
                {
                    Items.ItemUpdate Item = player.HeldItem.GetGlobalItem<Items.ItemUpdate>();
                    if (Item != null && Item.NeedsSaving(player.HeldItem))
                    {
                        return Item.GetManaLeech * 0.01f;
                    }
                }
                return 0;
            }
            private void LevelUp()
            {
                int pointsToGain = 5 + Mathf.FloorInt(Mathf.Pow(level,0.5f));
                totalPoints += pointsToGain;
                freePoints += pointsToGain; 
                Stats.OnLevelUp();
                CombatText.NewText(player.getRect(), new Color(255, 25, 100), "LEVEL UP !!!!");
                
                level++;
                Main.NewText(player.name + " Is now level : " + level.ToString() + " .Congratulation !", 255, 223, 63);
                if (Main.netMode == 1)
                {
                    ModPacket packet = mod.GetPacket();
                    packet.Write((byte)Message.SyncLevel);
                    packet.Write(player.whoAmI);
                    packet.Write(level);
                    packet.Send();
                }
            }
            public int XPToNextLevel()
            {
                return 15 * level + 5 * Mathf.CeilInt(Mathf.Pow(level, 1.8f)) + 40;
            }


            public int[] ConvertStatToInt()
            {
                int[] convertedStats = new int[8];
                for (int i = 0; i < 8; i++)
                {
                    convertedStats[i] = GetAddStat((Stat)i);
                }
                return convertedStats;
            }

            public int[] ConvertStatXPToInt()
            {
                int[] convertedStats = new int[8];
                for (int i = 0; i < 8; i++)
                {
                    convertedStats[i] = GetStatXP((Stat)i);
                }
                return convertedStats;
            }

            void LoadStats(int[] _level, int[] _xp)
            {
                if (_xp.Length != 8) //if save is not correct , will try to port
                {

                    RecalculateStat();
                    if (_level.Length != 8) //if port don't work
                    {
                        return;
                    }
                    for (int i = 0; i < 8; i++)
                    {
                        Stats.UpgradeStat((Stat)i, _level[i]);
                    }
                }
                else
                {
                    for (int i = 0; i < 8; i++)
                    {
                        Stats.SetStats((Stat)i,level+3,_level[i], _xp[i]);
                    }
                }
                
            }


            public override TagCompound Save()
            {
                if (Stats == null)
                {
                    Stats = new RpgStats();
                }
                return new TagCompound {
                    {"Exp", Exp},
                    {"level", level},
                    {"Stats", ConvertStatToInt()},
                    {"StatsXP", ConvertStatXPToInt()},
                    {"totalPoints", totalPoints},
                    {"freePoints", freePoints},
                    {"AnRPGSaveVersion", 1}
                };
            }
            public override void Initialize()
            {
                Stats = new RpgStats();
            }

            public override void Load(TagCompound tag)
            {
                ErrorLogger.Log("Another Rpg Mod , Version " + mod.Version);
                Exp = tag.GetInt("Exp");
                level = tag.GetInt("level");
                LoadStats( tag.GetIntArray("Stats"), tag.GetIntArray("StatsXP"));
                totalPoints = tag.GetInt("totalPoints");
                freePoints = tag.GetInt("freePoints");
            }

            public override void PlayerConnect(Player player)
            {
                ModPacket packet = mod.GetPacket();
                packet.Write((byte)Message.SyncLevel);
                packet.Write(player.whoAmI);
                packet.Write(level);
                packet.Send();
            }

        }

        
        
        
    }
}
