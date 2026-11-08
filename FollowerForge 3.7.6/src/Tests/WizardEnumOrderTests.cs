using System.Xml.Linq;
using FollowerForge.Domain;
using FollowerForge.Ui;

namespace FollowerForge.Tests;

/// <summary>
/// The wizard reads spec enums straight from ComboBox.SelectedIndex. That is only correct when
/// the XAML item order equals the enum's declaration order — the 3.7.1 relationship picker
/// shipped with the items in a different order, so picking "Ally" silently wrote Friend (and
/// the untouched default displayed Friend while writing Ally). These tests pin every
/// enum-backed combo to its expected sequence so the drift can never silently return.
/// </summary>
public sealed class WizardEnumOrderTests
{
    private static readonly XDocument Xaml = XDocument.Load(Path.Combine(
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..")),
        "Ui", "WizardWindow.axaml"));

    private static readonly XNamespace X = XNamespace.Get("http://schemas.microsoft.com/winfx/2006/xaml");

    private static XElement ComboElement(string comboName) =>
        Xaml.Descendants()
            .FirstOrDefault(e => (string?)e.Attribute(X + "Name") == comboName)
            ?? throw new InvalidOperationException($"ComboBox '{comboName}' not found in WizardWindow.axaml");

    private static string[] ComboItems(string comboName)
    {
        var box = ComboElement(comboName);

        // XAML compiles x:Name into the default namespace without prefix.
        if (box.Name.LocalName != "ComboBox")
            throw new InvalidOperationException($"'{comboName}' is a {box.Name.LocalName}, not a ComboBox");

        return box.Elements()
            .Select(e => (string?)e.Attribute("Content") ?? e.Value.Trim())
            .ToArray();
    }

    /// <summary>
    /// These combos show the raw enum names, so the XAML order must equal Enum.GetNames
    /// exactly — the cast SelectedIndex → enum has no other translation step.
    /// </summary>
    [Theory]
    [InlineData("RelationshipBox")]
    [InlineData("KinRankBox")]
    public void RelationshipCombos_ListEnumValuesInDeclarationOrder(string comboName)
    {
        Assert.Equal(Enum.GetNames<RelationshipRank>(), ComboItems(comboName));
    }

    /// <summary>
    /// Prose-labelled combos: the labels are human wording over enum positions. The sequences
    /// below were each verified against the enum declaration (IdleBehavior, TimeOfDay,
    /// LineEmotion, HostileCompany, TransformKind, FollowerStatPreset, UiTheme,
    /// DialogueTrigger) when the index-cast reads were audited; if a test fails here the
    /// XAML order drifted from the enum and every build from the wizard is mislabelled.
    /// </summary>
    [Theory]
    [InlineData("IdleBox",
        new[] { "Leave it to the game", "Keeps to her spot", "Uses the room she is in", "Settles wherever she is" })]
    [InlineData("LineTimeBox", new[] { "Any time", "Daytime", "At night" })]
    [InlineData("LineEmotionBox",
        new[] { "Neutral", "Anger", "Disgust", "Fear", "Sad", "Happy", "Surprise", "Puzzled" })]
    [InlineData("E2ACompanyBox", new[] { "Bandits", "Draugr", "Warlocks", "Beasts and creatures" })]
    [InlineData("TransformKindBox", new[] { "None", "Werewolf", "Custom race / spell" })]
    [InlineData("StatPresetBox",
        new[] { "Blank slate", "One-handed warrior", "Two-handed warrior", "Archer", "Pure mage",
                 "Spellsword", "Restoration support", "Rogue" })]
    [InlineData("ThemeBox",
        new[] { "Obsidian Gold", "Arcane Amethyst", "Nordic Frost", "Forge Teal", "Light" })]
    [InlineData("LineTriggerBox",
        new[] { "When you talk to her", "As you walk away", "Idle chatter", "A topic you can pick",
                 "A fight starts", "The fight is over", "Taunting an enemy", "Swinging a weapon",
                 "Power attack", "Blocking a blow", "Taking a hit", "Going down", "Dying" })]
    public void ProseCombos_KeepVerifiedOrder(string comboName, string[] expected)
    {
        Assert.Equal(expected, ComboItems(comboName));
    }

    /// <summary>
    /// The wizard's default selection must display what it writes. RelationshipBox defaults to
    /// Ally (BehaviorSpec default); KinRankBox defaults to Friend (NpcRelationship default).
    /// </summary>
    [Fact]
    public void RelationshipComboDefaults_MatchTheWrittenValues()
    {
        int DefaultOf(string comboName) =>
            int.Parse((string)(ComboElement(comboName).Attribute("SelectedIndex")
                ?? throw new InvalidOperationException(
                    $"{comboName} needs an explicit SelectedIndex")));

        var ranks = Enum.GetValues<RelationshipRank>();
        Assert.Equal(ranks.ToList().IndexOf(RelationshipRank.Ally), DefaultOf("RelationshipBox"));
        Assert.Equal(ranks.ToList().IndexOf(RelationshipRank.Friend), DefaultOf("KinRankBox"));
    }
}
