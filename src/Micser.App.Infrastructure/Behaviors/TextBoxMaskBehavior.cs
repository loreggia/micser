using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Micser.App.Infrastructure.Behaviors
{
    /// <summary>
    /// The type of masking to apply to the text box input.
    /// </summary>
    public enum MaskType
    {
        /// <summary>
        /// A custom mask.
        /// </summary>
        Any,

        /// <summary>
        /// Only allows integer numbers.
        /// </summary>
        Integer,

        /// <summary>
        /// Allows integer and decimal numbers.
        /// </summary>
        Decimal
    }

    /// <summary>
    ///     WPF Maskable TextBox class. Just specify the TextBoxMaskBehavior.Mask attached property to a TextBox.
    ///     It protect your TextBox from unwanted non numeric symbols and make it easy to modify your numbers.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Class Information:
    ///         <list type="bullet">
    ///             <item name="authors">Authors: Ruben Hakopian</item>
    ///             <item name="date">February 2009</item>
    ///             <item name="originalURL">http://www.rubenhak.com/?p=8</item>
    ///         </list>
    ///     </para>
    /// </remarks>
    public class TextBoxMaskBehavior
    {
        #region MinimumValue Property

        /// <summary>
        /// The minimum allowed value for <see cref="MaskType.Integer"/> and <see cref="MaskType.Decimal"/> masks.
        /// </summary>
        public static readonly DependencyProperty MinimumValueProperty =
            DependencyProperty.RegisterAttached(
                "MinimumValue",
                typeof(double),
                typeof(TextBoxMaskBehavior),
                new FrameworkPropertyMetadata(double.NaN, MinimumValueChangedCallback)
            );

        /// <summary>
        /// Gets the minimum allowed value for <see cref="MaskType.Integer"/> and <see cref="MaskType.Decimal"/> masks.
        /// </summary>
        public static double GetMinimumValue(DependencyObject obj)
        {
            return (double)obj.GetValue(MinimumValueProperty);
        }

        /// <summary>
        /// Sets the minimum allowed value for <see cref="MaskType.Integer"/> and <see cref="MaskType.Decimal"/> masks.
        /// </summary>
        public static void SetMinimumValue(DependencyObject obj, double value)
        {
            obj.SetValue(MinimumValueProperty, value);
        }

        private static void MinimumValueChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var _this = d as TextBox;
            ValidateTextBox(_this);
        }

        #endregion MinimumValue Property

        #region MaximumValue Property

        /// <summary>
        /// The maximum allowed value for <see cref="MaskType.Integer"/> and <see cref="MaskType.Decimal"/> masks.
        /// </summary>
        public static readonly DependencyProperty MaximumValueProperty =
            DependencyProperty.RegisterAttached(
                "MaximumValue",
                typeof(double),
                typeof(TextBoxMaskBehavior),
                new FrameworkPropertyMetadata(double.NaN, MaximumValueChangedCallback)
            );

        /// <summary>
        /// Gets the maximum allowed value for <see cref="MaskType.Integer"/> and <see cref="MaskType.Decimal"/> masks.
        /// </summary>
        public static double GetMaximumValue(DependencyObject obj)
        {
            return (double)obj.GetValue(MaximumValueProperty);
        }

        /// <summary>
        /// Sets the maximum allowed value for <see cref="MaskType.Integer"/> and <see cref="MaskType.Decimal"/> masks.
        /// </summary>
        public static void SetMaximumValue(DependencyObject obj, double value)
        {
            obj.SetValue(MaximumValueProperty, value);
        }

        private static void MaximumValueChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var _this = d as TextBox;
            ValidateTextBox(_this);
        }

        #endregion MaximumValue Property

        #region Mask Property

        /// <summary>
        /// The input mask type.
        /// </summary>
        public static readonly DependencyProperty MaskProperty =
            DependencyProperty.RegisterAttached(
                "Mask",
                typeof(MaskType),
                typeof(TextBoxMaskBehavior),
                new FrameworkPropertyMetadata(MaskChangedCallback)
            );

        /// <summary>
        /// Gets the input mask type attached property.
        /// </summary>
        public static MaskType GetMask(DependencyObject obj)
        {
            return (MaskType)obj.GetValue(MaskProperty);
        }

        /// <summary>
        /// Sets the input mask type attached property.
        /// </summary>
        public static void SetMask(DependencyObject obj, MaskType value)
        {
            obj.SetValue(MaskProperty, value);
        }

        private static void MaskChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is TextBox textBox)
            {
                textBox.PreviewTextInput -= TextBox_PreviewTextInput;
                textBox.TextChanged -= TextBox_TextChanged;
                DataObject.RemovePastingHandler(textBox, TextBoxPastingEventHandler);
            }

            if (!(d is TextBox _this))
            {
                return;
            }

            if ((MaskType)e.NewValue != MaskType.Any)
            {
                _this.PreviewTextInput += TextBox_PreviewTextInput;
                _this.TextChanged += TextBox_TextChanged;
                DataObject.AddPastingHandler(_this, TextBoxPastingEventHandler);
            }

            ValidateTextBox(_this);
        }

        #endregion Mask Property

        #region Decimals Property

        /// <summary>
        /// The number of decimals for <see cref="MaskType.Decimal"/> masks.
        /// </summary>
        public static readonly DependencyProperty DecimalsProperty = DependencyProperty.RegisterAttached(
            "Decimals", typeof(int), typeof(TextBoxMaskBehavior), new PropertyMetadata(2));

        /// <summary>
        /// Gets the number of decimals for <see cref="MaskType.Decimal"/> masks.
        /// </summary>
        public static int GetDecimals(DependencyObject element)
        {
            return (int)element.GetValue(DecimalsProperty);
        }

        /// <summary>
        /// Sets the number of decimals for <see cref="MaskType.Decimal"/> masks.
        /// </summary>
        public static void SetDecimals(DependencyObject element, int value)
        {
            element.SetValue(DecimalsProperty, value);
        }

        #endregion Decimals Property

        #region Private Static Methods

        private static bool IsSymbolValid(MaskType mask, string str)
        {
            switch (mask)
            {
                case MaskType.Any:
                    return true;

                case MaskType.Integer:
                    if (str == NumberFormatInfo.CurrentInfo.NegativeSign)
                    {
                        return true;
                    }

                    break;

                case MaskType.Decimal:
                    if (str == NumberFormatInfo.CurrentInfo.NumberDecimalSeparator ||
                        str == NumberFormatInfo.CurrentInfo.NegativeSign)
                    {
                        return true;
                    }

                    break;
            }

            if (mask.Equals(MaskType.Integer) || mask.Equals(MaskType.Decimal))
            {
                foreach (var ch in str)
                {
                    if (!char.IsDigit(ch))
                    {
                        return false;
                    }
                }

                return true;
            }

            return false;
        }

        private static void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var textBox = (TextBox)sender;
            var isValid = IsSymbolValid(GetMask(textBox), e.Text);
            e.Handled = !isValid;
            if (isValid)
            {
                var caret = textBox.CaretIndex;
                var text = textBox.Text;
                var textInserted = false;
                var selectionLength = 0;

                if (textBox.SelectionLength > 0)
                {
                    text = text.Substring(0, textBox.SelectionStart) +
                           text.Substring(textBox.SelectionStart + textBox.SelectionLength);
                    caret = textBox.SelectionStart;
                }

                if (e.Text == NumberFormatInfo.CurrentInfo.NumberDecimalSeparator)
                {
                    while (true)
                    {
                        var ind = text.IndexOf(NumberFormatInfo.CurrentInfo.NumberDecimalSeparator);
                        if (ind == -1)
                        {
                            break;
                        }

                        text = text.Substring(0, ind) + text.Substring(ind + 1);
                        if (caret > ind)
                        {
                            caret--;
                        }
                    }

                    if (caret == 0)
                    {
                        text = "0" + text;
                        caret++;
                    }
                    else
                    {
                        if (caret == 1 && string.Empty + text[0] == NumberFormatInfo.CurrentInfo.NegativeSign)
                        {
                            text = NumberFormatInfo.CurrentInfo.NegativeSign + "0" + text.Substring(1);
                            caret++;
                        }
                    }

                    if (caret == text.Length)
                    {
                        selectionLength = 1;
                        textInserted = true;
                        text = text + NumberFormatInfo.CurrentInfo.NumberDecimalSeparator + "0";
                        caret++;
                    }
                }
                else if (e.Text == NumberFormatInfo.CurrentInfo.NegativeSign)
                {
                    textInserted = true;
                    if (textBox.Text.Contains(NumberFormatInfo.CurrentInfo.NegativeSign))
                    {
                        text = text.Replace(NumberFormatInfo.CurrentInfo.NegativeSign, string.Empty);
                        if (caret != 0)
                        {
                            caret--;
                        }
                    }
                    else
                    {
                        text = NumberFormatInfo.CurrentInfo.NegativeSign + textBox.Text;
                        caret++;
                    }
                }

                if (!textInserted)
                {
                    text = text.Substring(0, caret) + e.Text +
                           (caret < textBox.Text.Length ? text.Substring(caret) : string.Empty);

                    caret++;
                }

                try
                {
                    var val = Convert.ToDouble(text);
                    var newVal = ValidateLimits(GetMinimumValue(textBox), GetMaximumValue(textBox), val);
                    if (val != newVal)
                    {
                        text = newVal.ToString();
                    }
                    else if (val == 0)
                    {
                        if (!text.Contains(NumberFormatInfo.CurrentInfo.NumberDecimalSeparator))
                        {
                            text = "0";
                        }
                    }
                }
                catch
                {
                    text = "0";
                }

                while (text.Length > 1 && text[0] == '0' && string.Empty + text[1] != NumberFormatInfo.CurrentInfo.NumberDecimalSeparator)
                {
                    text = text.Substring(1);
                    if (caret > 0)
                    {
                        caret--;
                    }
                }

                while (text.Length > 2 && string.Empty + text[0] == NumberFormatInfo.CurrentInfo.NegativeSign && text[1] == '0' &&
                       string.Empty + text[2] != NumberFormatInfo.CurrentInfo.NumberDecimalSeparator)
                {
                    text = NumberFormatInfo.CurrentInfo.NegativeSign + text.Substring(2);
                    if (caret > 1)
                    {
                        caret--;
                    }
                }

                if (caret > text.Length)
                {
                    caret = text.Length;
                }

                textBox.Text = text;
                textBox.CaretIndex = caret;
                textBox.SelectionStart = caret;
                textBox.SelectionLength = selectionLength;
                e.Handled = true;
            }
        }

        private static void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var _this = (TextBox)sender;
            var text = _this.Text;
            var decimals = GetDecimals(_this);

            if (decimals < 0)
            {
                return;
            }

            var decimalIndex = text.IndexOf(NumberFormatInfo.CurrentInfo.NumberDecimalSeparator, StringComparison.Ordinal);
            if (decimalIndex >= 0 && text.Length > decimalIndex + decimals)
            {
                text = text.Substring(0, decimalIndex + 1 + decimals);
            }

            if (_this.CaretIndex > text.Length)
            {
                _this.CaretIndex = text.Length;
            }

            _this.Text = text;
        }

        private static void TextBoxPastingEventHandler(object sender, DataObjectPastingEventArgs e)
        {
            var textBox = (TextBox)sender;
            var clipboard = e.DataObject.GetData(typeof(string)) as string;
            clipboard = ValidateValue(GetMask(textBox), clipboard);
            if (!string.IsNullOrEmpty(clipboard))
            {
                textBox.Text = clipboard;
            }

            e.CancelCommand();
            e.Handled = true;
        }

        private static double ValidateLimits(double min, double max, double value)
        {
            if (!min.Equals(double.NaN))
            {
                if (value < min)
                {
                    return min;
                }
            }

            if (!max.Equals(double.NaN))
            {
                if (value > max)
                {
                    return max;
                }
            }

            return value;
        }

        private static void ValidateTextBox(TextBox _this)
        {
            if (GetMask(_this) != MaskType.Any)
            {
                _this.Text = ValidateValue(GetMask(_this), _this.Text);
            }
        }

        private static string ValidateValue(MaskType mask, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            value = value.Trim();
            switch (mask)
            {
                case MaskType.Integer:
                    try
                    {
                        // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
                        Convert.ToInt64(value);
                        return value;
                    }
                    catch
                    {
                        return string.Empty;
                    }

                case MaskType.Decimal:
                    try
                    {
                        // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
                        Convert.ToDouble(value);
                        return value;
                    }
                    catch
                    {
                        return string.Empty;
                    }
            }

            return value;
        }

        #endregion Private Static Methods
    }
}