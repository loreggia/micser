﻿namespace Micser.App.Infrastructure.Themes
{
    public enum BrushThemeKey
    {
        Primary100,
        Primary090,
        Primary080,
        Primary070,
        Primary060,
        Primary050,
        Primary040,
        Primary030,
        Primary020,
        Primary010,
        Secondary100,
        Secondary090,
        Secondary080,
        Secondary070,
        Secondary060,
        Secondary050,
        Secondary040,
        Secondary030,
        Secondary020,
        Secondary010,
        Neutral100,
        Neutral090,
        Neutral080,
        Neutral070,
        Neutral060,
        Neutral050,
        Neutral040,
        Neutral030,
        Neutral020,
        Neutral010
    }

    public class BrushThemeKeyExtension : ThemeKeyExtension<BrushThemeKey>
    {
        public BrushThemeKeyExtension(BrushThemeKey key)
            : base(key)
        {
        }
    }
}