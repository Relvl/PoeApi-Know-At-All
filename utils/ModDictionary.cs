using System;
using System.Collections.Generic;
using System.Linq;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.FilesInMemory;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;

namespace Know_At_All.utils;

public class ModDictionary(Mod mod)
{
    public ModInfo Get(Entity entity, ItemMod itemMod)
    {
        var fs = mod.GameController.Files;
        var baseItem = fs.BaseItemTypes.Translate(entity.Path);
        var record = fs.Mods.records[itemMod.RawName];
        var modInfo = new ModInfo
        {
            Record = record,
            Tier = -1,
            TotalTiers = 0,
            ValidTiers = []
        };
        if (!entity.TryGetComponent<Base>(out var baseComponent) || baseComponent is null)
            return modInfo;
        var realItemTags = baseComponent.Info.TagsDat.Select(t => t.Key).Concat([baseItem.ClassName.ToLower().Replace(' ', '_')]).ToArray();

        if (fs.Mods.recordsByTier.TryGetValue(Tuple.Create(modInfo.Record.Group, modInfo.Record.AffixType), out var allTiers))
        {
            var prevTierKey = "";
            foreach (var modRecord in allTiers)
            {
                if (ByAnyChance(modRecord, realItemTags, modInfo.Record))
                {
                    // todo No idea how to determine this shit by the other way...
                    // LocalIncreasedAttackSpeed2
                    // LocalIncreasedAttackSpeed2Royale____
                    if (prevTierKey.Length > 5 && modRecord.Key.StartsWith(prevTierKey))
                        continue;

                    modInfo.TotalTiers++;
                    modInfo.ValidTiers.Add(modRecord.Key);
                    if (modRecord.Equals(modInfo.Record))
                        modInfo.Tier = modInfo.TotalTiers;
                    
                    prevTierKey = modRecord.Key;
                }
            }

            if (modInfo.Tier == -1 && !string.IsNullOrEmpty(modInfo.Record.Tier))
                if (int.TryParse(new string(modInfo.Record.Tier.Where(char.IsDigit).ToArray()), out var parsedTier))
                    modInfo.Tier = parsedTier;
        }

        return modInfo;
    }

    private bool ByAnyChance(ModsDat.ModRecord modRecord, string[] itemTags, ModsDat.ModRecord itemRecord)
    {
        if (modRecord.Domain != itemRecord.Domain)
            return false;
        if (modRecord.TagChances is null || modRecord.TagChances.Count == 0)
            return false;
        foreach (var (tag, chance) in modRecord.TagChances)
            if (itemTags.Contains(tag))
                return chance > 0;
        return false;
    }

    public struct ModInfo
    {
        public ModsDat.ModRecord Record;
        public int Tier;
        public int TotalTiers;
        public List<string> ValidTiers;
    }
}