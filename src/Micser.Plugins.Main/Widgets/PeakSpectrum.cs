using System;
using System.Collections.Generic;
using System.Windows;
using Micser.App.Infrastructure.Controls;
using Micser.Common.Audio;
using Micser.Common.Extensions;
using Micser.Plugins.Main.Api;

namespace Micser.Plugins.Main.Widgets
{
    public class PeakSpectrum : LinePanel
    {
        public static readonly DependencyProperty SpectrumDataProperty = DependencyProperty.Register(
            nameof(SpectrumData), typeof(SpectrumData), typeof(PeakSpectrum), new PropertyMetadata(null, OnSpectrumDataPropertyChanged));

        public PeakSpectrum()
        {
            SetResourceReference(StyleProperty, typeof(LinePanel));
        }

        public SpectrumData SpectrumData
        {
            get => (SpectrumData)GetValue(SpectrumDataProperty);
            set => SetValue(SpectrumDataProperty, value);
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            UpdatePoints();
        }

        private static void OnSpectrumDataPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((PeakSpectrum)d).UpdatePoints();
        }

        private Point GetPoint(SpectrumData data, int i)
        {
            var width = ActualWidth;
            var height = ActualHeight;
            var valueCount = data.Values.Count;

            var maxFreqLog = Math.Log(valueCount);

            var log = Math.Log(i + 1);
            var x = width / maxFreqLog * log;

            var valueDb = AudioHelper.LinearToDb(data.Values[i].Value);
            MathExtensions.Clamp(ref valueDb, -90f, 12f);
            var y = height - height / 72d * (valueDb + 90d);

            return new Point(x, y);
        }

        private void UpdatePoints()
        {
            var data = SpectrumData;

            if (data == null)
            {
                return;
            }

            var valueCount = data.Values.Count;
            var points = new List<Point>();

            for (int i = 0; i < valueCount; i++)
            {
                points.Add(GetPoint(data, i));
            }

            Points = points;
        }
    }
}