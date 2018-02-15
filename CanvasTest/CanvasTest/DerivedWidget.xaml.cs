namespace CanvasTest
{
    public partial class DerivedWidget
    {
        public DerivedWidget()
        {
            InitializeComponent();
            SetResourceReference(StyleProperty, typeof(Widget));
        }
    }
}