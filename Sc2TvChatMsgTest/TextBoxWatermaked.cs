using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace Sc2TvChatMsgTest
{
    //http://stackoverflow.com/questions/833943/watermark-hint-text-placeholder-textbox-in-wpf
    class TextBoxWatermarked : TextBox
    {
        public static DependencyProperty WatermarkProperty = DependencyProperty.Register("Watermark",
                                                                                         typeof(string),
                                                                                         typeof(TextBoxWatermarked),
                                                                                         new PropertyMetadata(new PropertyChangedCallback(OnWatermarkChanged)));

        bool isWatermarked;
        Binding textBinding;

        public string Watermark
        {
            get { return (string)GetValue(WatermarkProperty); }
            set { SetValue(WatermarkProperty, value); }
        }

        //Constructor
        public TextBoxWatermarked()
        {
            Loaded += (s, ea) => ShowWatermark();
        }

        //Event callbacks
        protected override void OnGotFocus(RoutedEventArgs e)
        {
            base.OnGotFocus(e);
            HideWatermark();
        }

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);
            ShowWatermark();
        }

        static void OnWatermarkChanged(DependencyObject sender, DependencyPropertyChangedEventArgs ea)
        {
            TextBoxWatermarked box = sender as TextBoxWatermarked;
            if (box == null) return;
            box.ShowWatermark();
        }

        //Methods
        void ShowWatermark()
        {
            if (string.IsNullOrEmpty(base.Text))
            {
                isWatermarked = true;
                base.Foreground = new SolidColorBrush(Colors.Gray);
                BindingExpression bindingExp = GetBindingExpression(TextProperty);
                textBinding = bindingExp == null ? null : bindingExp.ParentBinding;
                if (bindingExp != null)
                    bindingExp.UpdateSource();
                BindingOperations.ClearBinding(this, TextProperty);
                base.Text = Watermark;
            }
        }

        void HideWatermark()
        {
            if (isWatermarked)
            {
                isWatermarked = false;
                ClearValue(ForegroundProperty);
                base.Text = "";
                if (textBinding != null)
                    SetBinding(TextProperty, textBinding);
                else
                    BindingOperations.ClearBinding(this, TextProperty);
            }
        }
    }
}
