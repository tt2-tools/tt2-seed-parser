using System.Text.Json.Serialization;

namespace seedapi.Models;

public record SeedFile(
    [property: JsonPropertyName("valid_from")] DateTime ValidFrom,
    [property: JsonPropertyName("expire_at")] DateTime ExpireAt,
    [property: JsonPropertyName("raids")] List<Raid> Raids
);

public record Raid(
    [property: JsonPropertyName("tier")] int Tier,
    [property: JsonPropertyName("level")] int Level,
    [property: JsonPropertyName("spawn_sequence")] List<string> SpawnSequence,
    [property: JsonPropertyName("titans")] List<Titan> Titans,
    [property: JsonPropertyName("area_buffs")] List<Bonus>? AreaBuffs
);

public record Titan(
    [property: JsonPropertyName("enemy_id")] string EnemyId,
    [property: JsonPropertyName("enemy_name")] string EnemyName,
    [property: JsonPropertyName("current_hp")] double CurrentHp,
    [property: JsonPropertyName("total_hp")] double TotalHp,
    [property: JsonPropertyName("parts")] List<TitanPart> Parts,
    [property: JsonPropertyName("area_debuffs")] List<Bonus>? AreaDebuffs,
    [property: JsonPropertyName("cursed_debuffs")] List<Bonus>? CursedDebuffs
);

public record TitanPart(
    [property: JsonPropertyName("part_id")] string PartId,
    [property: JsonPropertyName("current_hp")] double CurrentHp,
    [property: JsonPropertyName("total_hp")] double TotalHp,
    [property: JsonPropertyName("cursed")] bool? Cursed
);

public record Bonus(
    [property: JsonPropertyName("bonus_type")] string BonusType,
    [property: JsonPropertyName("bonus_amount")] double BonusAmount
);
