using System.Windows.Input;

namespace Micser.App.Infrastructure
{
    /// <summary>
    /// Connects an <see cref="ICommand"/> to a WPF <see cref="RoutedUICommand"/>.
    /// </summary>
    public class CommandBindingToCommand : CommandBinding
    {
        public CommandBindingToCommand(RoutedUICommand applicationCommand, ICommand boundCommand)
            : base(applicationCommand)
        {
            BoundCommand = boundCommand;

            Executed += OnExecuted;
            CanExecute += OnCanExecute;
        }

        public ICommand BoundCommand { get; }

        private void OnCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = BoundCommand?.CanExecute(e.Parameter) == true;
        }

        private void OnExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            BoundCommand?.Execute(e.Parameter);
        }
    }
}