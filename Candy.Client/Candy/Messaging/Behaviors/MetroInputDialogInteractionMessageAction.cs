using System.Windows;
using Livet.Behaviors.Messaging;
using Livet.Messaging;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;

namespace Candy.Client.Messaging.Behaviors
{
    /// <summary>
    /// <see cref="MetroWindow"/> 上に入力メッセージを表示するためのアクションを実行します。
    /// </summary>
    public class MetroInputDialogInteractionMessageAction : InteractionMessageAction<FrameworkElement>
    {
        protected override void InvokeAction(InteractionMessage message)
        {
            var information = message as AsyncInputMessage;
            var window = Window.GetWindow(AssociatedObject) as MetroWindow;

            if (information != null && window != null)
            {
                information.Response = window.ShowInputAsync(information.Caption, information.Text);
            }
        }
    }
}
