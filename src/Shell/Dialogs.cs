using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Micser.Shell;

/// <summary>
/// Simple message dialogs: a text with OK, or with Yes and No.
/// </summary>
internal static class Dialogs
{
    /// <summary>
    /// Shows a message; returns true for OK or Yes. With an owner the dialog is modal to it.
    /// </summary>
    public static Task<bool> ShowAsync(Window? owner, string message, bool yesNo = false)
    {
        var result = new TaskCompletionSource<bool>();
        var dialog = new Window
        {
            Title = "Micser",
            SizeToContent = SizeToContent.WidthAndHeight,
            CanResize = false,
            ShowInTaskbar = owner == null,
            WindowStartupLocation =
                owner == null ? WindowStartupLocation.CenterScreen : WindowStartupLocation.CenterOwner,
        };

        ShellIcons.SetIcon(dialog);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8,
        };
        if (yesNo)
        {
            buttons.Children.Add(CreateButton(Strings.DialogYes, true, isDefault: true));
            buttons.Children.Add(CreateButton(Strings.DialogNo, false, isCancel: true));
        }
        else
        {
            buttons.Children.Add(CreateButton("OK", true, isDefault: true, isCancel: true));
        }

        dialog.Content = new StackPanel
        {
            Margin = new Thickness(24),
            Spacing = 20,
            Children =
            {
                new TextBlock
                {
                    Text = message,
                    TextWrapping = TextWrapping.Wrap,
                    MaxWidth = 420,
                },
                buttons,
            },
        };
        dialog.Closed += (_, _) => result.TrySetResult(false);

        if (owner != null)
        {
            _ = dialog.ShowDialog(owner);
        }
        else
        {
            dialog.Show();
        }

        return result.Task;

        Button CreateButton(string text, bool value, bool isDefault = false, bool isCancel = false)
        {
            var button = new Button
            {
                Content = text,
                MinWidth = 80,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                IsDefault = isDefault,
                IsCancel = isCancel,
            };
            button.Click += (_, _) =>
            {
                result.TrySetResult(value);
                dialog.Close();
            };
            return button;
        }
    }
}

/// <summary>
/// The Micser icon (the logo on a square), embedded in the shell.
/// </summary>
internal static class ShellIcons
{
    public static Stream OpenIcon()
    {
        return typeof(ShellIcons).Assembly.GetManifestResourceStream("Micser.Shell.icon.png")
            ?? throw new InvalidOperationException("The icon is missing.");
    }

    /// <summary>
    /// Sets the window's icon; on X11 once it's open (see <see cref="X11WindowIcon"/>).
    /// </summary>
    public static void SetIcon(Window window)
    {
        using (var icon = OpenIcon())
        {
            window.Icon = new WindowIcon(icon);
        }

        window.Opened += (_, _) =>
        {
            if (OperatingSystem.IsLinux())
            {
                X11WindowIcon.Apply(window);
            }
        };
    }
}
