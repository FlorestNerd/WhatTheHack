﻿using Harmony;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using UnityEngine;
using Verse;

namespace WhatTheHack.Harmony
{
    [HarmonyPatch(typeof(CompLongRangeMineralScanner), "FoundMinerals")]
    public static class CompLongRangeMineralScanner_Foundminerals
    {
        private const int MinDistance = 6;
        private const int MaxDistance = 22;
        private static readonly IntRange TimeoutDaysRange = new IntRange(min: 25, max: 50);

        static bool Prefix(CompLongRangeMineralScanner __instance)
        {
            if(Traverse.Create(__instance).Field("targetMineable").GetValue<ThingDef>() == WTH_DefOf.WTH_MechanoidParts)
            {
                if (!TileFinder.TryFindNewSiteTile(out int tile, MinDistance, MaxDistance, true, false))
                    return false;

                Site site = SiteMaker.MakeSite(WTH_DefOf.WTH_MechanoidTempleCore,
                                               WTH_DefOf.WTH_MechanoidTemplePart,
                                               tile, Faction.OfMechanoids, ifHostileThenMustRemainHostile: true);

                if (site == null)
                    return false;

                int randomInRange = TimeoutDaysRange.RandomInRange;

                site.Tile = tile;
                site.GetComponent<TimeoutComp>().StartTimeout(ticks: randomInRange * GenDate.TicksPerDay);
                site.SetFaction(Faction.OfMechanoids);

                site.customLabel = "TODO";
                Find.WorldObjects.Add(o: site);
                Find.LetterStack.ReceiveLetter(label: "TODO", text: "TODO", textLetterDef: LetterDefOf.PositiveEvent, lookTargets: site);

                return false;
            }
            return true;

        }


    }

    /** 
     * Adds extra option in long range mineral scanner so you can scan for mechanoid parts. 
     **/
   [HarmonyPatch]
   public static class CompLongRangeMineralScanner_CompGetGizmosExtra
   {
       static MethodBase TargetMethod()
       {
           var predicateClass = typeof(CompLongRangeMineralScanner).GetNestedTypes(AccessTools.all)
               .FirstOrDefault(t => t.FullName.Contains("c__Iterator0"));
           return predicateClass.GetMethods(AccessTools.all).FirstOrDefault(m => m.Name.Contains("m__0"));
       }
       static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
       {
           var instructionsList = new List<CodeInstruction>(instructions);
           for (var i = 0; i < instructionsList.Count; i++)
           {
               CodeInstruction instruction = instructionsList[i];

               if (instructionsList[i].operand == typeof(WindowStack).GetMethod("Add"))
               {
                   //yield return new CodeInstruction(OpCodes.Call, typeof(Pawn).GetMethod("CanTakeOrder"));//Injected code     
                   yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CompLongRangeMineralScanner_CompGetGizmosExtra), "AddMechPartsOption"));//Injected code     

               }
               else
               {
                   yield return instruction;
               }
           }
       }
       public static void AddMechPartsOption(WindowStack instance, FloatMenu menu)
        {
            List<FloatMenuOption> options = Traverse.Create(menu).Field("options").GetValue<List<FloatMenuOption>>();
            //options.Add(new FloatMenuOption());

            ThingDef mechanoidParts = WTH_DefOf.WTH_MechanoidParts;

            FloatMenuOption item = new FloatMenuOption("WTH_MechanoidParts_LabelShort".Translate(), delegate
            {
                foreach (object current2 in Find.Selector.SelectedObjects)
                {
                    Thing thing = current2 as Thing;
                    if (thing != null)
                    {
                        CompLongRangeMineralScanner compLongRangeMineralScanner = thing.TryGetComp<CompLongRangeMineralScanner>();
                        if (compLongRangeMineralScanner != null)
                        {
                            Traverse.Create(compLongRangeMineralScanner).Field("targetMineable").SetValue(mechanoidParts);
                            Log.Message("clicked mech parts minable");
                        }
                    }
                }
            }, MenuOptionPriority.Default, null, null, 29f, (Rect rect) => Widgets.InfoCardButton(rect.x + 5f, rect.y + (rect.height - 24f)/2, mechanoidParts.GetConcreteExample()), null);
            options.Add(item);
            Traverse.Create(menu).Field("options").SetValue(options);
            instance.Add(menu);
        }
   }
  
}
