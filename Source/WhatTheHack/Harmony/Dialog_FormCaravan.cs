﻿using Harmony;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using Verse;
using WhatTheHack.Buildings;

namespace WhatTheHack.Harmony
{
    
    [HarmonyPatch(typeof(Dialog_FormCaravan), "DoBottomButtons")]
    class Dialog_FormCaravan_DoBottomButtons
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            for (var i = 0; i < instructionsList.Count; i++)
            {
                CodeInstruction instruction = instructionsList[i];
                if (i < instructionsList.Count - 2 && instructionsList[i + 1].opcode == OpCodes.Call && instructionsList[i + 1].operand == AccessTools.Method(typeof(Dialog_FormCaravan), "get_DaysWorthOfFood"))
                {
                    Log.Message("Found instruction");
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldloca_S, 4);
                    yield return new CodeInstruction(OpCodes.Call, typeof(Dialog_FormCaravan_DoBottomButtons).GetMethod("AddWarnings"));
                }
                yield return instruction;
            }
        }

        public static void AddWarnings(Dialog_FormCaravan instance, ref List<string> warnings){
            Log.Message("calling AddWarnings");
            int numMechanoids = 0;
            int numPlatforms = 0;
            foreach (TransferableOneWay tow in instance.transferables)
            {
                if (tow.ThingDef.race != null && tow.ThingDef.race.IsMechanoid)
                {
                    numMechanoids += tow.CountToTransfer;
                }
                if (tow.ThingDef == ThingDefOf.MinifiedThing)
                {
                    if (tow.things[0].GetInnerIfMinified().def == WTH_DefOf.WTH_PortableChargingPlatform)
                    {
                        numPlatforms += tow.CountToTransfer;
                    }
                }
            }
            if(numMechanoids == 0)
            {
                return;
            }
            Log.Message("Mech count: " + numMechanoids);
            Log.Message("platform count: " + numPlatforms);
            if(numPlatforms < numMechanoids)
            {
                warnings.Add("WTH_Warning_NotEnoughPlatforms");
            }
            else if (Base.Instance.daysOfFuel < Traverse.Create(instance).Field("MaxDaysWorthOfFoodToShowWarningDialog").GetValue<float>())
            {
                warnings.Add("WTH_Warning_DaysOfFuel".Translate(new Object[]{Base.Instance.daysOfFuel.ToString("0.#") }));
            }
        }
    }

    [HarmonyPatch(typeof(Dialog_FormCaravan), "CountToTransferChanged")]
    class Dialog_FormCaravan_CountToTransferChanged
    {
        static void Postfix(Dialog_FormCaravan __instance)
        {
            int numMechanoids = 0;
            float fuelAmount = 0f;
            float fuelConsumption = 0f;
            int numPlatforms = 0;
            float daysOfFuel = 0;
            StringBuilder daysOfFuelReason = new StringBuilder();

            foreach (TransferableOneWay tow in __instance.transferables)
            {
                if(tow.ThingDef.race != null && tow.ThingDef.race.IsMechanoid)
                {
                    numMechanoids += tow.CountToTransfer;
                }
                if(tow.ThingDef == ThingDefOf.Chemfuel)
                {
                    fuelAmount += tow.CountToTransfer;
                }
                if(tow.ThingDef == ThingDefOf.MinifiedThing)
                {
                    if (tow.things[0].GetInnerIfMinified().def == WTH_DefOf.WTH_PortableChargingPlatform)
                    {
                        numPlatforms += tow.CountToTransfer;
                    }

                }
            }
            if (numMechanoids == 0)
            {
                return;
            }
            daysOfFuelReason.AppendLine("WTH_Explanation_NumMechs".Translate() + ": " + numMechanoids);
            daysOfFuelReason.AppendLine("WTH_Explanation_NumPlatforms".Translate() + ": " + numPlatforms);
            if (numPlatforms >= numMechanoids)
            {
                fuelConsumption = numMechanoids * WTH_DefOf.WTH_PortableChargingPlatform.GetCompProperties<CompProperties_Refuelable>().fuelConsumptionRate;
                daysOfFuel = fuelAmount / fuelConsumption;
                daysOfFuelReason.AppendLine("WTH_Explanation_FuelConsumption".Translate() + ": " + fuelConsumption.ToString("0.#"));
                daysOfFuelReason.AppendLine("WTH_Explanation_TotalFuel".Translate() + ": " + fuelAmount.ToString("0.#"));
                daysOfFuelReason.AppendLine("WTH_Explanation_DaysOfFuel".Translate() + ": " + fuelAmount.ToString("0.#") + " /( " + numMechanoids + " * " + fuelConsumption + ") = " + daysOfFuel.ToString("0.#"));
            }
            else
            {
                daysOfFuelReason.AppendLine("WTH_Explanation_NotEnoughPlatforms".Translate());
            }
            Base.Instance.daysOfFuel = daysOfFuel;
            Base.Instance.daysOfFuelReason = daysOfFuelReason.ToString();

        }

    }

}