using System.Buffers.Binary;

namespace Micser.Audio.Devices.PipeWire;

/// <summary>
/// Builds and reads the few SPA PODs the backend needs; the C helpers for them are inline functions. A POD is a little-endian
/// <c>{ uint32 size, uint32 type }</c> header and a body of <c>size</c> bytes, padded to 8 bytes.
/// </summary>
internal static class SpaPod
{
    public const uint TypeBool = 2;
    public const uint TypeId = 3;
    public const uint TypeInt = 4;
    public const uint TypeFloat = 6;
    public const uint TypeArray = 13;
    public const uint TypeObject = 15;
    public const uint TypeChoice = 19;

    public const uint ObjectProps = 0x40002;
    public const uint ObjectFormat = 0x40003;

    public const uint FormatMediaType = 1;
    public const uint FormatMediaSubtype = 2;
    public const uint FormatAudioFormat = 0x10001;
    public const uint FormatAudioRate = 0x10003;
    public const uint FormatAudioChannels = 0x10004;
    public const uint FormatAudioPosition = 0x10005;

    public const uint MediaTypeAudio = 1;
    public const uint MediaSubtypeRaw = 1;
    public const uint AudioFormatF32 = 0x11B;

    public const uint PropMute = 0x10004;
    public const uint PropChannelVolumes = 0x10008;

    /// <summary>
    /// A raw audio format object (<c>SPA_TYPE_OBJECT_Format</c>) of interleaved 32-bit floats with the rate and positions.
    /// </summary>
    /// <param name="paramId">E.g. <c>SPA_PARAM_EnumFormat</c>.</param>
    public static byte[] AudioFormat(uint paramId, int rate, ReadOnlySpan<uint> positions)
    {
        var writer = new Writer();
        var objectStart = writer.BeginObject(ObjectFormat, paramId);
        writer.PropertyId(FormatMediaType, MediaTypeAudio);
        writer.PropertyId(FormatMediaSubtype, MediaSubtypeRaw);
        writer.PropertyId(FormatAudioFormat, AudioFormatF32);
        writer.PropertyInt(FormatAudioRate, rate);
        writer.PropertyInt(FormatAudioChannels, positions.Length);
        writer.PropertyIdArray(FormatAudioPosition, positions);
        writer.End(objectStart);
        return writer.ToArray();
    }

    /// <summary>
    /// The size of the POD whose header starts <paramref name="header"/>, header included.
    /// </summary>
    public static int GetSize(ReadOnlySpan<byte> header)
    {
        return 8 + (int)BinaryPrimitives.ReadUInt32LittleEndian(header);
    }

    /// <summary>
    /// Reads the volume and mute of a <c>Props</c> object, e.g. a node's <c>SPA_PARAM_Props</c>. Missing values are null.
    /// </summary>
    public static (float[]? ChannelVolumes, bool? Mute) ReadVolume(ReadOnlySpan<byte> pod)
    {
        float[]? volumes = null;
        bool? mute = null;
        if (pod.Length < 16 || ReadUInt(pod, 4) != TypeObject || ReadUInt(pod, 8) != ObjectProps)
        {
            return (volumes, mute);
        }

        var end = Math.Min(pod.Length, GetSize(pod));
        var offset = 16;
        while (offset + 16 <= end)
        {
            var key = ReadUInt(pod, offset);
            var size = (int)ReadUInt(pod, offset + 8);
            var type = ReadUInt(pod, offset + 12);
            var body = offset + 16;
            if (body + size > end)
            {
                break;
            }

            var value = pod.Slice(body, size);
            if (key == PropMute && type == TypeBool && size >= 4)
            {
                mute = BinaryPrimitives.ReadInt32LittleEndian(value) != 0;
            }
            else if (key == PropChannelVolumes && type == TypeArray && size >= 8)
            {
                volumes = ReadFloatArray(value);
            }

            offset = body + Pad(size);
        }

        return (volumes, mute);
    }

    private static int Pad(int size)
    {
        return (size + 7) & ~7;
    }

    private static float[]? ReadFloatArray(ReadOnlySpan<byte> arrayBody)
    {
        var childSize = (int)ReadUInt(arrayBody, 0);
        if (ReadUInt(arrayBody, 4) != TypeFloat || childSize != 4)
        {
            return null;
        }

        var values = new float[(arrayBody.Length - 8) / 4];
        for (var i = 0; i < values.Length; i++)
        {
            values[i] = BinaryPrimitives.ReadSingleLittleEndian(arrayBody[(8 + (i * 4))..]);
        }

        return values;
    }

    private static uint ReadUInt(ReadOnlySpan<byte> data, int offset)
    {
        return BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
    }

    private sealed class Writer
    {
        private readonly List<byte> _data = [];

        /// <summary>
        /// Starts an object; returns the offset to pass to <see cref="End"/>, which sets its size.
        /// </summary>
        public int BeginObject(uint objectType, uint id)
        {
            var start = _data.Count;
            UInt(0);
            UInt(TypeObject);
            UInt(objectType);
            UInt(id);
            return start;
        }

        public void End(int start)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(
                System.Runtime.InteropServices.CollectionsMarshal.AsSpan(_data)[start..],
                (uint)(_data.Count - start - 8)
            );
        }

        public void PropertyId(uint key, uint value)
        {
            Property(key, TypeId, 4);
            UInt(value);
            UInt(0);
        }

        public void PropertyInt(uint key, int value)
        {
            Property(key, TypeInt, 4);
            UInt((uint)value);
            UInt(0);
        }

        public void PropertyIdArray(uint key, ReadOnlySpan<uint> values)
        {
            var size = 8 + (values.Length * 4);
            Property(key, TypeArray, size);
            UInt(4);
            UInt(TypeId);
            foreach (var value in values)
            {
                UInt(value);
            }

            for (var i = size; i < Pad(size); i++)
            {
                _data.Add(0);
            }
        }

        public byte[] ToArray()
        {
            return [.. _data];
        }

        private void Property(uint key, uint type, int size)
        {
            UInt(key);
            UInt(0);
            UInt((uint)size);
            UInt(type);
        }

        private void UInt(uint value)
        {
            Span<byte> bytes = stackalloc byte[4];
            BinaryPrimitives.WriteUInt32LittleEndian(bytes, value);
            _data.AddRange(bytes);
        }
    }
}
