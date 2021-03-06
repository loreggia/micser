﻿using System;
using System.Collections.Generic;

namespace Micser.Common.Settings
{
    /// <summary>
    /// Specifies the type/location where the setting is stored.
    /// </summary>
    public enum SettingStorageType
    {
        /// <summary>
        /// Stores the setting in the database.
        /// </summary>
        Internal,

        /// <summary>
        /// Doesn't store the setting; expects the setting to be stored a custom <see cref="ISettingHandler"/>.
        /// </summary>
        Custom,

        /// <summary>
        /// The setting is stored via API.
        /// </summary>
        Api
    }

    /// <summary>
    /// The type of the setting value.
    /// </summary>
    public enum SettingType
    {
        /// <summary>
        /// A <see cref="bool"/> value.
        /// </summary>
        Boolean,

        /// <summary>
        /// A <see cref="string"/> value.
        /// </summary>
        String,

        /// <summary>
        /// An integer value (<see cref="short"/>, <see cref="int"/>, <see cref="long"/>).
        /// </summary>
        Integer,

        /// <summary>
        /// A decimal value (<see cref="decimal"/>, <see cref="float"/>, <see cref="double"/>).
        /// </summary>
        Decimal,

        /// <summary>
        /// A list of values to choose from. Requires <see cref="SettingDefinition.List"/> to be set.
        /// </summary>
        List,

        /// <summary>
        /// A custom object.
        /// </summary>
        Object
    }

    /// <summary>
    /// Describes an application setting. This is used so it can be displayed in the settings view.
    /// </summary>
    public class SettingDefinition
    {
        /// <inheritdoc />
        public SettingDefinition()
        {
            IsAppliedInstantly = true;
            Type = SettingType.String;
            StorageType = SettingStorageType.Internal;
        }

        /// <summary>
        /// Gets or sets the default setting value.
        /// </summary>
        public object DefaultValue { get; set; }

        /// <summary>
        /// Gets or sets the description content that is shown as a help for this setting in the settings view.
        /// </summary>
        public object Description { get; set; }

        /// <summary>
        /// Gets or sets the type of a setting handler that is used to process loading/saving of the setting.
        /// This should be set if the <see cref="StorageType"/> is set to <see cref="SettingStorageType.Custom"/>.
        /// </summary>
        public Type HandlerType { get; set; }

        /// <summary>
        /// Gets or sets whether the setting will be applied and saved instantly when changing it in the UI or the user has to click an Apply button first. Default is true.
        /// </summary>
        public bool IsAppliedInstantly { get; set; }

        /// <summary>
        /// Gets or sets a function that is used to determine if this setting can be changed by the user.
        /// </summary>
        public Func<ISettingsService, bool> IsEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value that indicates whether this setting will not be shown in the settings view.
        /// </summary>
        public bool IsHidden { get; set; }

        /// <summary>
        /// Gets or sets the key that uniquely identifies this setting.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets a list of possible setting values. This is only used when the <see cref="Type"/> is set to <see cref="SettingType.List"/>.
        /// </summary>
        public IDictionary<object, string> List { get; set; }

        /// <summary>
        /// Gets or sets the name that is displayed for this setting.
        /// </summary>
        public object Name { get; set; }

        /// <summary>
        /// Gets or sets the storage type. Default is <see cref="SettingStorageType.Internal"/>.
        /// </summary>
        public SettingStorageType StorageType { get; set; }

        /// <summary>
        /// Gets or sets the setting type. Default is <see cref="SettingType.String"/>.
        /// </summary>
        public SettingType Type { get; set; }
    }
}