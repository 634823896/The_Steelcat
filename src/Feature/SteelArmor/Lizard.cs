using System;
using BepInEx;
using UnityEngine;
using SlugBase.Features;
using static SlugBase.Features.FeatureTypes;
using MonoMod;
using SteelCat.Hook;
using System.Runtime.InteropServices;
using Mono.Cecil;
using System.Collections.Generic;

namespace TheSteelcat.Feature.SteelArmor
{
    [BepInPlugin(MOD_ID, "steelcat", "0.0.1")]
    class Lizard : BaseUnityPlugin
    {
        private const string MOD_ID = "piao.steelcat";

        public static readonly PlayerFeature<int> Steelarmor = PlayerInt("steelcat/steel_armor");

        // Add hooks
        public void OnEnable()
        {
            On.RainWorld.OnModsInit += Extras.WrapInit(LoadResources);

            // Put your custom hooks here!
            On.Lizard.Bite += Lizard_Bite;
            On.Creature.Grab += Creature_Grab;
            On.Player.checkInput += Player_checkInput;
        }
        public static int YOUAREDYING = 5;
        public static bool BiteFLAG = false;
        public static bool InputYes = false;
        public global::Lizard GetLizardInstance(Room room)
        {
            foreach (List<PhysicalObject> list in room.physicalObjects)
            {
                foreach (PhysicalObject physicalObject in list)
                {
                    if (physicalObject is global::Lizard)
                    {
                        return physicalObject as global::Lizard;
                    }
                }
            }

            return null;
        }
        // Load any resources, such as sprites or sounds
        private void LoadResources(RainWorld rainWorld)
        {
        }
        public bool Creature_Grab(On.Creature.orig_Grab orig, Creature self, PhysicalObject obj, int graspUsed, int chunkGrabbed, Creature.Grasp.Shareability shareability, float dominance, bool overrideEquallyDominant, bool pacifying)
        {
            if (self.grasps == null || graspUsed < 0 || graspUsed > self.grasps.Length)
            {
                return false;
            }
            if (obj.slatedForDeletetion || obj is Creature && !(obj as Creature).CanBeGrabbed(self))
            {
                return false;
            }
            if (self.grasps[graspUsed] != null && self.grasps[graspUsed].grabbed == obj)
            {
                self.ReleaseGrasp(graspUsed);
                self.grasps[graspUsed] = new Creature.Grasp(self, obj, graspUsed, chunkGrabbed, shareability, dominance, true);
                obj.Grabbed(self.grasps[graspUsed]);
                new AbstractPhysicalObject.CreatureGripStick(self.abstractCreature, obj.abstractPhysicalObject, graspUsed, pacifying || obj.TotalMass < obj.TotalMass);
                return true;
            }
            foreach (Creature.Grasp grasp in obj.grabbedBy)
            {
                if (grasp.grabber == self || grasp.ShareabilityConflict(shareability) && (overrideEquallyDominant && grasp.dominance == dominance || grasp.dominance > dominance))
                {
                    return false;
                }
            }
            for (int i = obj.grabbedBy.Count - 1; i >= 0; i--)
            {
                if (obj.grabbedBy[i].ShareabilityConflict(shareability))
                {
                    obj.grabbedBy[i].Release();
                }
            }
            if (self.grasps[graspUsed] != null)
            {
                self.ReleaseGrasp(graspUsed);
            }
            self.grasps[graspUsed] = new Creature.Grasp(self, obj, graspUsed, chunkGrabbed, shareability, dominance, pacifying);
            //修改的部分
            if (YOUAREDYING >= 0)
            {
                BiteFLAG = true;
                Debug.Log("BiteFLAG = true");
                System.IO.File.AppendAllText(@"C:\Users\63482\Desktop\YOUAREDYING.txt", DateTime.Now.ToString() + "\n" + "Creature_Grab fix is running and set BiteFLAG = true\n");
            }
            else
            {
                obj.Grabbed(self.grasps[graspUsed]);
            }
            //修改的部分
            new AbstractPhysicalObject.CreatureGripStick(self.abstractCreature, obj.abstractPhysicalObject, graspUsed, pacifying || obj.TotalMass < obj.TotalMass);
            return true;
        }
        public void Player_checkInput(On.Player.orig_checkInput orig, Player self)
        {
            orig(self);
            if (self.input[0].pckp && self.input[0].jmp && BiteFLAG && YOUAREDYING > 0)
            {
                Debug.Log("按下技能键");
                var room = self.room;
                var pos = self.mainBodyChunk.pos;
                self.room.PlaySound(SoundID.Lizard_Head_Shield_Deflect, self.mainBodyChunk);
                YOUAREDYING--;
                Debug.Log("剩余盔甲：" + YOUAREDYING);
                InputYes = true;
                BiteFLAG = false;
                global::Lizard lizard = GetLizardInstance(self.room);
                if (lizard != null)
                {
                    lizard.Stun(100);
                    Debug.Log("蜥蜴被眩晕");
                }
            }
        }

        //public void Set(Player self)
        //{
        //    SteelArmor.TryGet(self, out int steelarmor);
        //    YOUAREDYING = steelarmor;
        //    System.IO.File.AppendAllText(@"C:\Users\63482\Desktop\YOUAREDYING.txt", DateTime.Now.ToString() + "\n" + YOUAREDYING.ToString() + "\n");
        //}
        //public void HurtBehavior(Player self)
        //{
        //    var room = self.room;
        //    var pos = self.mainBodyChunk.pos;
        //    room.PlaySound(SoundID.Bomb_Explode, pos);
        //    YOUAREDYING--;
        //    System.IO.File.AppendAllText(@"C:\Users\63482\Desktop\YOUAREDYING.txt", YOUAREDYING.ToString() + "\n\n");
        //}

        public void Lizard_Bite(On.Lizard.orig_Bite orig, global::Lizard self, BodyChunk chunk)
        {

            {
                if (ModManager.MSC && self.Template.type == MoreSlugcats.MoreSlugcatsEnums.CreatureTemplateType.TrainLizard && self.room != null)
                {
                    for (int i = 0; i < 16; i++)
                    {
                        Vector2 a = RWCustom.Custom.RNV();
                        self.room.AddObject(new Spark(self.firstChunk.pos + a * 40f, a * Mathf.Lerp(4f, 30f, UnityEngine.Random.value), Color.white, null, 8, 24));
                    }
                }
                if (self.grasps[0] != null && self.grabbedBy.Count == 0)
                {
                    return;
                }
                if (chunk != null && chunk.owner is Creature && (chunk.owner as Creature).newToRoomInvinsibility > 0)
                {
                    return;
                }
                self.biteControlReset = false;
                self.JawOpen = 0f;
                self.lastJawOpen = 0f;
                if (chunk == null)
                {
                    self.room.PlaySound(SoundID.Lizard_Jaws_Shut_Miss_Creature, self.mainBodyChunk);
                    return;
                }
                chunk.vel += self.mainBodyChunk.vel * Mathf.Lerp(self.mainBodyChunk.mass, 1.1f, 0.5f) / Mathf.Max(1f, chunk.mass);
                bool flag = false;
                if (chunk.owner is Creature && self.AI.DynamicRelationship((chunk.owner as Creature).abstractCreature).type == CreatureTemplate.Relationship.Type.Eats || UnityEngine.Random.value < 0.5f && chunk.owner.TotalMass < self.TotalMass * 1.2f || !(chunk.owner is Creature) && chunk.owner.TotalMass < self.TotalMass * 1.2f)
                {
                    flag = self.Grab(chunk.owner, 0, chunk.index, Creature.Grasp.Shareability.CanOnlyShareWithNonExclusive, self.lizardParams.biteDominance * UnityEngine.Random.value, true, true);
                }
                //if (flag)
                //{
                //    if (chunk.owner is Creature)
                //    {
                //        //if (self.Template.type == CreatureTemplate.Type.RedLizard)
                //        //{
                //        //    (chunk.owner as Creature).LoseAllGrasps();
                //        //}
                //        self.AI.BitCreature(chunk);
                //        if (ModManager.MMF)
                //        {
                //            if (chunk.owner is Player)
                //            {
                //                if (self.AI.friendTracker.friend != null && self.AI.friendTracker.friend == chunk.owner)
                //                {
                //                    (chunk.owner as Player).Violence(self.mainBodyChunk, new Vector2?(RWCustom.Custom.DirVec(self.mainBodyChunk.pos, chunk.pos) * 0.1f), chunk, null, Creature.DamageType.Bite, 0f, 0f);
                //                }
                //                else if (self.lizardParams.biteDamageChance > 0f && (self.lizardParams.biteDamageChance >= 1f || UnityEngine.Random.value < self.lizardParams.biteDamageChance * (chunk.owner as Player).DeathByBiteMultiplier()))
                //                {
                //                    (chunk.owner as Player).Violence(self.mainBodyChunk, new Vector2?(RWCustom.Custom.DirVec(self.mainBodyChunk.pos, chunk.pos) * 0.1f), chunk, null, Creature.DamageType.Bite, 0f, 0f);
                //                }
                //            }
                //            else if (UnityEngine.Random.value < self.lizardParams.biteDamageChance)
                //            {
                //                (chunk.owner as Creature).Violence(self.mainBodyChunk, new Vector2?(RWCustom.Custom.DirVec(self.mainBodyChunk.pos, chunk.pos) * 0.1f), chunk, null, Creature.DamageType.Bite, 0f, 0f);
                //            }
                //        }
                //        else if (UnityEngine.Random.value < self.lizardParams.biteDamageChance)
                //        {
                //            if (chunk.owner is Player)
                //            {
                //                (chunk.owner as Player).Violence(self.mainBodyChunk, new Vector2?(RWCustom.Custom.DirVec(self.mainBodyChunk.pos, chunk.pos) * 0.1f), chunk, null, Creature.DamageType.Bite, 0f, 0f);
                //            }
                //            else
                //            {
                //                (chunk.owner as Creature).Violence(self.mainBodyChunk, new Vector2?(RWCustom.Custom.DirVec(self.mainBodyChunk.pos, chunk.pos) * 0.1f), chunk, null, Creature.DamageType.Bite, 0f, 0f);
                //            }
                //        }
                //    }
                //    if (self.graphicsModule != null)
                //    {
                //        if (chunk.owner is IDrawable)
                //        {
                //            self.graphicsModule.AddObjectToInternalContainer(chunk.owner as IDrawable, 0);
                //        }
                //        else if (chunk.owner.graphicsModule != null)
                //        {
                //            self.graphicsModule.AddObjectToInternalContainer(chunk.owner.graphicsModule, 0);
                //        }
                //    }
                //    self.room.PlaySound((chunk.owner is Player) ? SoundID.Lizard_Jaws_Grab_Player : SoundID.Lizard_Jaws_Grab_NPC, self.mainBodyChunk);
                //    return;
                //}
                //if (UnityEngine.Random.value < 0.5f && chunk.owner is Creature && self.AI.DynamicRelationship((chunk.owner as Creature).abstractCreature).type == CreatureTemplate.Relationship.Type.Attacks)
                //{
                //    self.DamageAttack(chunk, 0.5f);
                //    return;
                //}
                //self.room.PlaySound(SoundID.Lizard_Jaws_Shut_Miss_Creature, self.mainBodyChunk);
                //for (int j = chunk.owner.grabbedBy.Count - 1; j >= 0; j--)
                //{
                //    if (chunk.owner.grabbedBy[j].grabber is Lizard)
                //    {
                //        chunk.owner.grabbedBy[j].Release();
                //    }
                //}
            }
            //else
            //{
            //    orig(self, chunk);
            //}
        }
    }
}