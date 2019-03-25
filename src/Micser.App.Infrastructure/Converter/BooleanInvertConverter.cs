namespace Micser.App.Infrastructure.Converter
{
    public class BooleanInvertConverter : BooleanConverter<bool>
    {
        public BooleanInvertConverter()
            : base(false, true)
        {
        }
    }
}