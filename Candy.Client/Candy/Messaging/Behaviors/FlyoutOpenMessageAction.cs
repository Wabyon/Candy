using System.Windows;
using Livet.Behaviors.Messaging;
using Livet.Messaging;
using MahApps.Metro.Controls;

namespace Candy.Client.Messaging.Behaviors
{
    /// <summary>
    /// <see cref="Flyout"/> コントロールを表示するためのアクションを実行します。
    /// </summary>
    public class FlyoutOpenMessageAction : InteractionMessageAction<FrameworkElement>
    {
        public static readonly DependencyProperty FlyoutProperty =
            DependencyProperty.Register("Flyout", typeof(FrameworkElement), typeof(FlyoutOpenMessageAction), new PropertyMetadata(null));

        public FrameworkElement Flyout
        {
            get { return (FrameworkElement)GetValue(FlyoutProperty); }
            set { SetValue(FlyoutProperty, value); }
        }

        protected override void InvokeAction(InteractionMessage message)
        {
            var flyout = Flyout as Flyout;

            if (flyout != null)
            {
                flyout.IsOpen = true;
            }
        }
    }
}