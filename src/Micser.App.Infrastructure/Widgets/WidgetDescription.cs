﻿using System;

namespace Micser.App.Infrastructure.Widgets
{
    public class WidgetDescription
    {
        public string Description { get; set; }
        public string Name { get; set; }
        public Type ViewModelType { get; set; }
        public Type ViewType { get; set; }
    }
}