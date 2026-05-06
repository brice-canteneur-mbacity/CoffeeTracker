using System.ComponentModel;

namespace CoffeeTracker.Models;

// ─────────────────────────────────────────────────────────────────────────────────────────
// Enums du domaine. Les valeurs entières sont stockées telles quelles dans IndexedDB :
// NE JAMAIS RÉORDONNER les membres existants. Les nouvelles valeurs doivent être ajoutées
// à la fin avec une valeur explicite. Le libellé d'affichage français vient du
// [Description(...)] et est résolu via EnumExtensions.GetLabel().
// ─────────────────────────────────────────────────────────────────────────────────────────

/// <summary>Process d'élaboration du café vert (lavage, fermentation, etc.).</summary>
public enum Process
{
    [Description("Lavé")] Washed,
    [Description("Nature")] Natural,
    [Description("Honey")] Honey,
    [Description("Anaérobie")] Anaerobic,
    [Description("Macération carbonique")] Carbonic,
    [Description("Autre")] Other
}

/// <summary>Niveau de torréfaction du café.</summary>
public enum RoastLevel
{
    [Description("Clair")] Light,
    [Description("Medium-clair")] MediumLight,
    [Description("Medium")] Medium,
    [Description("Medium-foncé")] MediumDark,
    [Description("Foncé")] Dark
}

/// <summary>Type de lait ajouté à un brew ou à une visite (null sur le modèle = sans lait).</summary>
public enum MilkType
{
    [Description("Entier")] Whole = 0,
    [Description("Demi-écrémé")] SemiSkimmed = 1,
    [Description("Écrémé")] Skimmed = 2,
    [Description("Sans lactose")] LactoseFree = 3,
    [Description("Avoine")] Oat = 4,
    [Description("Amande")] Almond = 5,
    [Description("Soja")] Soy = 6,
    [Description("Coco")] Coconut = 7,
    [Description("Riz")] Rice = 8,
    [Description("Autre")] Other = 9
}

/// <summary>
/// Méthode de décaféination. Renseignée seulement si <see cref="Coffee.IsDecaf"/> est true.
/// Les méthodes "Water" sont sans solvant chimique, EA est organique, MC est interdit dans certains pays.
/// </summary>
public enum DecafProcess
{
    [Description("Swiss Water (sans solvant)")] SwissWater = 0,
    [Description("Mountain Water (Mexique)")] MountainWater = 1,
    [Description("CO₂ supercritique")] Co2 = 2,
    [Description("EA / Sucre de canne")] EthylAcetate = 3,
    [Description("MC / Dichlorométhane")] MethyleneChloride = 4,
    [Description("Autre / Inconnu")] Other = 5
}

/// <summary>
/// Type de matériel. Mélange préparateurs (Espresso, V60…) et accessoires (Grinder, Kettle, Scale).
/// Utiliser <see cref="MachineTypeOrder.IsBrewer"/> pour distinguer.
/// </summary>
public enum MachineType
{
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

/// <summary>
/// Méthode de préparation d'un brew. Mappée vers un <see cref="MachineType"/> attendu via
/// <see cref="MachineTypeOrder.BrewerTypeFor"/> pour filtrer les machines proposées dans le formulaire.
/// </summary>
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

/// <summary>
/// Extensions de mapping pour récupérer le libellé localisé d'une valeur d'enum.
/// Si un <c>LocalizationService</c> a été enregistré via <see cref="SetLocalizer"/> (au démarrage
/// de l'app), <see cref="GetLabel"/> renvoie la traduction de la langue courante. Sinon
/// (avant init, ou clé manquante), fallback sur l'attribut <see cref="DescriptionAttribute"/>
/// posé sur la valeur d'enum (= la version française historique).
/// </summary>
public static class EnumExtensions
{
    /// <summary>
    /// Référence vers le service de localisation. Settée par <c>LocalizationService.InitializeAsync</c>
    /// au démarrage. Pattern statique acceptable ici : Blazor WASM est mono-utilisateur mono-onglet,
    /// pas de risque de course condition multi-tenant.
    /// </summary>
    private static object? _loc;

    /// <summary>Installe la passerelle vers le service de localisation (appelé au boot).</summary>
    internal static void SetLocalizer(object loc) => _loc = loc;

    /// <summary>
    /// Renvoie le libellé d'affichage d'une valeur d'enum, dans la langue courante si le
    /// localizer est initialisé, sinon en français via <see cref="DescriptionAttribute"/>.
    /// </summary>
    public static string GetLabel(this Enum value)
    {
        // L'appel au localizer est fait via reflection minimal pour éviter une dépendance
        // circulaire entre Models et Lib (ou pour découpler en cas de futur projet partagé).
        if (_loc is not null)
        {
            var method = _loc.GetType().GetMethod("TEnum");
            if (method?.Invoke(_loc, new object[] { value }) is string s && !string.IsNullOrEmpty(s))
                return s;
        }
        return GetDescriptionFallback(value);
    }

    /// <summary>Lit l'attribut <see cref="DescriptionAttribute"/> directement (utilisé comme fallback).</summary>
    public static string GetDescriptionFallback(Enum value)
    {
        var field = value.GetType().GetField(value.ToString());
        if (field is null) return value.ToString();
        var attr = (DescriptionAttribute?)Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute));
        return attr?.Description ?? value.ToString();
    }
}
