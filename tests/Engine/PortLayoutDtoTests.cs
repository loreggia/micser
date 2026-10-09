using Micser.Audio;
using Micser.Engine.Contracts;
using NAudio.Wave;

namespace Micser.Engine.Tests;

public class PortLayoutDtoTests
{
    [Test]
    public async Task From_NamesTheSpeakers()
    {
        var dto = PortLayoutDto.From(ChannelLayout.Stereo);

        await Assert.That(dto.ChannelCount).IsEqualTo(2);
        await Assert.That(dto.Speakers).IsEquivalentTo(
            [SpeakerPosition.FrontLeft, SpeakerPosition.FrontRight],
            TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    public async Task From_ReservedSpeakerBits_HasNoSpeakers()
    {
        var dto = PortLayoutDto.From(new ChannelLayout(2, Speakers.FrontLeft | (Speakers)0x40000));

        await Assert.That(dto.ChannelCount).IsEqualTo(2);
        await Assert.That(dto.Speakers).IsNull();
    }
}
