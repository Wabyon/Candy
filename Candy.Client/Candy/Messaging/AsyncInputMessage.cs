using System.Threading.Tasks;
using System.Windows;
using Livet.Messaging;

namespace Candy.Client.Messaging
{
    /// <summary>
    /// �񓯊��I�ɓ��͂Ɩ₢���킹�鑊�ݍ�p���b�Z�[�W�ł��B
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