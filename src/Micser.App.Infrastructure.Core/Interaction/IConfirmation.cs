namespace Micser.App.Infrastructure.Interaction
{
    public interface IConfirmation : INotification
    {
        bool Confirmed { get; set; }
    }
}