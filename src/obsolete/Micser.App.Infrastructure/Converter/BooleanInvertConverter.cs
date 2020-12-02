namespace Micser.App.Infrastructure.Converter
{
    /// <summary>
    /// A value converter that inverses a boolean value.
    /// </summary>
    public class BooleanInvertConverter : BooleanConverter<bool>
    {
        /// <inheritdoc />
        public BooleanInvertConverter()
            : base(false, true)
        {
        }
    }
}