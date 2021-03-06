﻿using System;
using Terraria;
using Terraria.ModLoader;
using AnotherRpgMod.Utils;
using Microsoft.Xna.Framework;
namespace AnotherRpgMod.RPGModule.Entities
{
    class Utils
    {

        public static int GetBaseLevel(NPC npc)
        {
            
            int baselevel = (int)((npc.lifeMax / 25 + Mathf.Pow(npc.damage * 0.28f, 1.1f) + Mathf.Pow(npc.defense, 1.3f))*0.8f);
            if (npc.boss)
            {
                float health = npc.lifeMax;
                if (npc.aiStyle == 6)
                    health = health * 0.2f;
                if (Main.expertMode)
                {
                    baselevel = (int)(health / 140 + Mathf.Pow(npc.damage * 0.31f, 1.05f) + Mathf.Pow(npc.defense * 0.8f, 1.07f));
                }
                else
                {
                    baselevel = (int)(health / 100 + Mathf.Pow(npc.damage * 0.30f, 1.04f) + Mathf.Pow(npc.defense * 0.7f, 1.05f));
                }
                
            }
            
            if (Main.expertMode)
            {
                float levelmultiplier = Mathf.Clamp(baselevel / 50, 0.5f, 1);
                baselevel = (int)(baselevel * 0.6f);
            }
            
            return baselevel;
        }

        public static int GetTier(NPC npc,int baselevel)
        {
            int BonusLevel = WorldManager.GetWorldAdditionalLevel();

            int randomlevel = Mathf.RandomInt(-1, 3);
            if (BonusLevel*2 > baselevel)
            {
                return randomlevel = Mathf.Clamp(randomlevel + BonusLevel, randomlevel,BonusLevel*2 - baselevel) ;
            }
            return randomlevel;
        }
        public static int GetTierAlly(NPC npc, int baselevel)
        {
            return WorldManager.GetWorldAdditionalLevel();
        }
        public static int GetExp(NPC npc)
        {
            return Mathf.CeilInt(npc.lifeMax / 20 + npc.damage/2 + npc.defense);
        }
    }


    class ARPGGlobalProjectile : GlobalProjectile
    {
        bool init = false;
        
        public override bool InstancePerEntity
        {
            get
            {
                return true;
            }
        }

        public override void AI(Projectile projectile)
        {

            if (init)
                return;
            if (projectile.friendly)
                return;
            if (projectile.owner > Main.npc.Length)
                return;
            NPC owner = Main.npc[projectile.owner];
            if (owner.GivenName == "")
                return;
            ARPGGlobalNPC ownerGlobal = owner.GetGlobalNPC<ARPGGlobalNPC>();
            if (owner.boss)
                projectile.damage = Mathf.FloorInt(projectile.damage * 0.75f * Mathf.Pow(1 + ownerGlobal.getLevel * 0.08f + ownerGlobal.getTier * 0.1f, 0.93f));
            else
                projectile.damage = Mathf.FloorInt(projectile.damage *0.75f * Mathf.Pow(1 + ownerGlobal.getLevel * 0.09f + ownerGlobal.getTier * 0.11f, 0.93f));
            init = true;
        }
    }

    class ARPGGlobalNPC : GlobalNPC
    {

        private bool StatsCreated = false;
        private int level;
        public int getLevel { get { return level; } }
        private int tier;
        public int getTier { get { return tier; } }
        private int basehealth = 0;
        public int baseDamage = 0;
        int damage;
        public override bool InstancePerEntity
        {
            get
            {
                return true;
            }
        }

        private void SyncNpc(NPC npc) // only sync tier since it's the only random value, rest is calculed on client
        {
            if (Main.netMode == 2)
            {
                ModPacket packet = mod.GetPacket();
                packet.Write((byte)Message.SyncNPC);
                packet.Write(npc.whoAmI);
                packet.Write(tier+level);
                packet.Write(npc.lifeMax);
                packet.Write(npc.damage);
                packet.Write(npc.defense);
                packet.Send();
            }
        }

        public override void SetDefaults(NPC npc)
        {
            
            if ((npc.townNPC||(npc.friendly&&npc.lifeMax>10) )&& Main.netMode != 1)
            {
                baseDamage = npc.damage;
                level = Mathf.CeilInt(Utils.GetBaseLevel(npc) * ConfigFile.GetConfig.NpclevelMultiplier);
                tier = Mathf.CeilInt(Utils.GetTierAlly(npc, level) * ConfigFile.GetConfig.NpclevelMultiplier);
                npc.lifeMax = Mathf.Clamp( Mathf.FloorInt(npc.lifeMax * (1 + (tier + level) * 0.1f)),0,int.MaxValue);
                damage = Mathf.Clamp(Mathf.FloorInt(npc.damage * Mathf.Pow(1 + level * 0.08f + tier * 0.1f,0.93f)), 0, int.MaxValue);
                npc.defense = Mathf.Clamp(Mathf.FloorInt(npc.defense * (1 + level * 0.012f + tier * 0.02f)), 0, int.MaxValue);
                npc.life = Mathf.Clamp(Mathf.FloorInt(npc.lifeMax * (1 + (tier + level) * 0.1f)), 0, int.MaxValue);
            }
            else if (Main.netMode != 1)
            {
                level = Mathf.CeilInt(Utils.GetBaseLevel(npc) * ConfigFile.GetConfig.NpclevelMultiplier);
                if (ConfigFile.GetConfig.NpcProgress)
                    tier = Mathf.CeilInt(Utils.GetTier(npc, level) * ConfigFile.GetConfig.NpclevelMultiplier);

                if (npc.boss)
                {
                    damage = Mathf.FloorInt(npc.damage * Mathf.Pow(1 + level * 0.08f + tier * 0.2f, 0.93f));
                    npc.lifeMax = Mathf.FloorInt(npc.lifeMax * (1 + Mathf.Pow(level * 0.5f + tier * 0.7f,1.05f)));
                }
                else
                {
                    damage = Mathf.FloorInt(npc.damage * Mathf.Pow(1 + level * 0.09f + tier * 0.2f, 0.93f));
                    npc.lifeMax = Mathf.FloorInt(npc.lifeMax * (1 + Mathf.Pow(level * 0.1f,1.1f) + Mathf.Pow(tier * 0.25f,1.1f)));
                }
                npc.value = npc.value * (1 + (level + tier) * 0.01f);
                npc.defense = Mathf.FloorInt(npc.defense * (1 + level * 0.012f + tier * 0.02f));
            }
            base.SetDefaults(npc);
        }

        public override void PostAI(NPC npc)
        {
            
            if(npc.onFire)
            {
                npc.life -= Mathf.CeilInt(npc.lifeMax * 0.0002);
            }
            if (npc.onFire2)
            {
                npc.life -= Mathf.CeilInt(npc.lifeMax * 0.0005);
            }
            if (npc.onFrostBurn)
            {
                npc.life -= Mathf.CeilInt(npc.lifeMax * 0.001);
            }
            npc.damage = damage;
            if (npc.friendly) return;
            if (Main.netMode != 1) { 
                if (StatsCreated == false) {
                    StatsCreated = true;
                    npc.life = npc.lifeMax;

                    if (npc.GivenName == "")
                    {
                        npc.GivenName = ("Lvl. " + (level + tier) + " " + npc.TypeName);
                    }
                    else
                        npc.GivenName = ("Lvl. " + (level + tier) + " " + npc.GivenName);
                    SyncNpc(npc);
                }
            }
        }
        /*
        public override void PostAI(NPC npc)
        {


            if (npc.friendly) return;
            if (npc.townNPC) return;

                if (StatsCreated)
                    return;
                level = Mathf.CeilInt( Utils.GetBaseLevel(npc)*ConfigFile.GetConfig.NpclevelMultiplier);
                if (ConfigFile.GetConfig.NpcProgress)
                    tier = Mathf.CeilInt(Utils.GetTier(npc, level) * ConfigFile.GetConfig.NpclevelMultiplier);
                basehealth = npc.life;
                npc.lifeMax = Mathf.FloorInt(npc.lifeMax * (1 + level * 0.25f + tier * 0.4f));
                npc.life = Mathf.Clamp(Mathf.FloorInt(npc.life * (1 + level * 0.25f + tier * 0.4f)),0, basehealth*5);
                npc.damage = Mathf.FloorInt(npc.damage * (1 + level * 0.05f + tier * 0.08f));
                npc.defense = Mathf.FloorInt(npc.defense * (1 + level * 0.01f + tier * 0.025f));
                if (npc.GivenName == "")
                {
                    npc.GivenName = ("Lvl. " + (level + tier) + " " + npc.TypeName);
                }
                else
                    npc.GivenName = ("Lvl. " + (level + tier) + " " + npc.GivenName);

                //SyncNpc(npc);

                StatsCreated = true;

        }
        */

        public override void NPCLoot(NPC npc)
        {
            if (npc.friendly) return;
            if (npc.townNPC) return;

            Player player = Array.Find(Main.player, p => p.active);
            if (Main.netMode == 0) player = Main.LocalPlayer; //if local , well it's simple

            else if (Main.player[npc.target].active)
                player = Main.player[npc.target];
            else
                return;
            int XPToDrop = Utils.GetExp(npc);
            if (npc.rarity > 0)
            {
                XPToDrop = (int)(XPToDrop * 1.5f);
            }
            if (npc.GivenName.Contains("godly")|| npc.GivenName.Contains("Legendary"))
            {
                XPToDrop = XPToDrop * 2;
            }
            if (npc.boss)
            {
                XPToDrop = XPToDrop * 2;
                WorldManager.OnBossDefeated(npc);
            }

            XPToDrop = Mathf.CeilInt(XPToDrop * ConfigFile.GetConfig.XpMultiplier);

            int xplevel = level + tier;
            if (!ConfigFile.GetConfig.XPReduction)
            {
                xplevel = 99999;
            }
            if (Main.netMode == 2)
            {
                ModPacket packet = mod.GetPacket();
                packet.Write((byte)Message.AddXP);
                packet.Write(XPToDrop);
                packet.Write(xplevel);
                packet.Send();
            }
            else
                player.GetModPlayer<RPGPlayer>().AddXp(XPToDrop, xplevel);

        }
    }
}
