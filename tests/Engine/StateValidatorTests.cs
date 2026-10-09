using System.ComponentModel.DataAnnotations;
using Micser.Engine.Modules;

namespace Micser.Engine.Tests;

public class StateValidatorTests
{
    [Test]
    public async Task Validate_AppliesAttributesOnRecordParameters()
    {
        var errors = StateValidator.Validate(new ParameterRecord(42, null!));

        await Assert.That(errors.Keys).IsEquivalentTo(["value", "name"]);
    }

    [Test]
    public async Task Validate_AppliesAttributesOnProperties()
    {
        var errors = StateValidator.Validate(new PropertyClass { Value = 42 });

        await Assert.That(errors.Keys).IsEquivalentTo(["value"]);
    }

    [Test]
    public async Task Validate_RecursesIntoPropertiesAndCollections()
    {
        var errors = StateValidator.Validate(
            new Container(new ParameterRecord(1, "ok"), [new ParameterRecord(1, "ok"), new ParameterRecord(42, "ok")]),
            "state"
        );

        await Assert.That(errors.Keys).IsEquivalentTo(["state.items[1].value"]);
    }

    [Test]
    public async Task Validate_ValidObject_ReturnsNoErrors()
    {
        var errors = StateValidator.Validate(new ParameterRecord(5, "ok"));

        await Assert.That(errors).IsEmpty();
    }

    private sealed record Container(ParameterRecord Single, IReadOnlyList<ParameterRecord> Items);

    private sealed record ParameterRecord([Range(0, 10)] int Value, [Required] string Name);

    private sealed class PropertyClass
    {
        [Range(0, 10)]
        public int Value { get; init; }
    }
}
