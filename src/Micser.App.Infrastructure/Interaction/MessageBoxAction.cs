﻿using Prism.Interactivity.InteractionRequest;
using System;
using System.Windows;
using System.Windows.Interactivity;

namespace Micser.App.Infrastructure.Interaction
{
    public class MessageBoxAction : TriggerAction<FrameworkElement>
    {
        /// <inheritdoc />
        protected override void Invoke(object parameter)
        {
            if (!(parameter is InteractionRequestedEventArgs args))
            {
                return;
            }

            var contentString = args.Context.Content as string;
            var content = args.Context.Content as MessageBoxContent;

            var window = new MessageBoxWindow
            {
                DataContext = new MessageBoxViewModel
                {
                    Message = contentString ?? content?.Message,
                    Buttons = content?.Buttons ?? MessageBoxButton.OK,
                    Image = content?.Image ?? MessageBoxImage.Information,
                    Title = args.Context.Title,
                    Notification = args.Context
                },
                Owner = content?.ParentWindow,
                Icon = content?.ParentWindow?.Icon
            };

            void CloseHandler(object sender, EventArgs e)
            {
                window.Closed -= CloseHandler;
                args.Callback?.Invoke();
            }

            window.Closed += CloseHandler;

            if (content != null && content.IsModal)
            {
                window.ShowDialog();
            }
            else
            {
                window.Show();
            }
        }
    }
}