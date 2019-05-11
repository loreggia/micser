namespace Micser.App.Infrastructure.Converter
{
    /// <summary>
    /// A value converter that inverses a boolean value.
    /// </summary>
    public class BooleanInvertConverter : BooleanConverter<bool>
    {
        /// <summary>
        /// Creates an instance of the <see cref="BooleanInvertConverter"/> class.
        /// </summary>
        public BooleanInvertConverter()
            : base(false, true)
        {
        }
    }
}