using System.ComponentModel;

namespace CoffeeTracker.Models;

public enum Process
{
    [Description("Lavé")] Washed,
    [Description("Nature")] Natural,
    [Description("Honey")] Honey,
    [Description("Anaérobie")] Anaerobic,
    [Description("Macération carbonique")] Carbonic,
    [Description("Autre")] Other
}

public enum RoastLevel
{
    [Description("Clair")] Light,
    [Description("Medium-clair")] MediumLight,
    [Description("Medium")] Medium,
    [Description("Medium-foncé")] MediumDark,
    [Description("Foncé")] Dark
}

public enum MachineType
{
    // NOTE: ne pas réordonner — les valeurs entières sont stockées telles quelles dans IndexedDB.
    // Les nouvelles valeurs doivent être appended à la fin pour préserver les données existantes.
    [Description("Machine espresso")] Espresso = 0,
    [Description("Moulin")] Grinder = 1,
    [Description("Bouilloire")] Kettle = 2,
    [Description("Balance")] Scale = 3,
    [Description("Autre")] Other = 4,
    [Description("V60")] V60 = 5,
    [Description("AeroPress")] AeroPress = 6,
    [Description("Chemex")] Chemex = 7,
    [Description("French press")] FrenchPress = 8,
    [Description("Moka")] Moka = 9,
    [Description("Kalita")] Kalita = 10,
    [Description("Origami")] Origami = 11,
    [Description("Cold brew")] ColdBrew = 12
}

public static class MachineTypeOrder
{
    /// <summary>Ordre logique pour l'affichage : préparateurs d'abord, accessoires ensuite.</summary>
    public static readonly MachineType[] DisplayOrder = new[]
    {
        // Préparateurs
        MachineType.Espresso,
        MachineType.V60,
        MachineType.AeroPress,
        MachineType.Chemex,
        MachineType.FrenchPress,
        MachineType.Moka,
        MachineType.Kalita,
        MachineType.Origami,
        MachineType.ColdBrew,
        // Accessoires
        MachineType.Grinder,
        MachineType.Kettle,
        MachineType.Scale,
        MachineType.Other
    };

    /// <summary>True si ce type sert à préparer un café (vs accessoire de support).</summary>
    public static bool IsBrewer(this MachineType t) => t is not (
        MachineType.Grinder or MachineType.Kettle or MachineType.Scale or MachineType.Other);

    /// <summary>Mappe une méthode de brew vers son type de machine attendu (null = toutes les machines).</summary>
    public static MachineType? BrewerTypeFor(BrewMethod method) => method switch
    {
        BrewMethod.Espresso => MachineType.Espresso,
        BrewMethod.V60 => MachineType.V60,
        BrewMethod.AeroPress => MachineType.AeroPress,
        BrewMethod.Chemex => MachineType.Chemex,
        BrewMethod.FrenchPress => MachineType.FrenchPress,
        BrewMethod.Moka => MachineType.Moka,
        BrewMethod.Kalita => MachineType.Kalita,
        BrewMethod.Origami => MachineType.Origami,
        BrewMethod.ColdBrew => MachineType.ColdBrew,
        _ => null
    };
}

public enum BrewMethod
{
    [Description("Espresso")] Espresso,
    [Description("V60")] V60,
    [Description("AeroPress")] AeroPress,
    [Description("Chemex")] Chemex,
    [Description("French press")] FrenchPress,
    [Description("Moka")] Moka,
    [Description("Cold brew")] ColdBrew,
    [Description("Kalita")] Kalita,
    [Description("Origami")] Origami,
    [Description("Autre")] Other
}

public static class EnumExtensions
{
    public static string GetLabel(this Enum value)
    {
        var field = value.GetType().GetField(value.ToString());
        if (field is null) return value.ToString();
        var attr = (DescriptionAttribute?)Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute));
        return attr?.Description ?? value.ToString();
    }
}
