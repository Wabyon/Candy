using System.Threading.Tasks;
using System.Windows;
using Livet.Messaging;

namespace Candy.Client.Messaging
{
    /// <summary>
    /// 非同期的に入力と問い合わせる相互作用メッセージです。
    /// </summary>
    public class AsyncInputMessage : ResponsiveInteractionMessage<Task<string>>
    {
        public static readonly DependencyProperty CaptionProperty =
            DependencyProperty.Register("Caption", typeof(string), typeof(AsyncInputMessage), new PropertyMetadata(null));

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(AsyncInputMessage), new PropertyMetadata(null));

        public string Caption
        {
            get { return (string)GetValue(CaptionProperty); }
            set { SetValue(CaptionProperty, value); }
        }

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        protected override Freezable CreateInstanceCore()
        {
            return new AsyncInputMessage
            {
                Caption = Caption,
                Text = Text,
                MessageKey = MessageKey,
                Response = Response
            };
        }
    }
}