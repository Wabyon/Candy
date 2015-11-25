using System.Windows;
using Livet.Behaviors.Messaging;
using Livet.Messaging;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;

namespace Candy.Client.Messaging.Behaviors
{
    /// <summary>
    /// <see cref="MetroWindow"/> 上に確認メッセージを表示するためのアクションを実行します。
    /// </summary>
    public class MetroConfirmationDialogInteractionMessageAction : InteractionMessageAction<FrameworkElement>
    {
        protected override void InvokeAction(InteractionMessage message)
        {
            var confirmation = message as AsyncConfirmationMessage;
            var window = Window.GetWindow(AssociatedObject) as MetroWindow;

            if (confirmation != null && window != null)
            {
                var task = window.ShowMessageAsync(confirmation.Caption, confirmation.Text, MessageDialogStyle.AffirmativeAndNegative);
                confirmation.Response = task.ContinueWith(t => ToBoolean(t.Result));
            }
        }
        private static bool? ToBoolean(MessageDialogResult result)
        {
            switch (result)
            {
                case MessageDialogResult.Affirmative:
                    return true;
                case MessageDialogResult.Negative:
                    return false;
                default:
                    return null;
            }
        }
    }
}