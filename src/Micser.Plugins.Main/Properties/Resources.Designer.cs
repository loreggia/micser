﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Micser.Plugins.Main.Properties {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "16.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    public class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Micser.Plugins.Main.Properties.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Amount.
        /// </summary>
        public static string CompressorAmount {
            get {
                return ResourceManager.GetString("CompressorAmount", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Attack.
        /// </summary>
        public static string CompressorAttack {
            get {
                return ResourceManager.GetString("CompressorAttack", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Advanced controls.
        /// </summary>
        public static string CompressorEnableAdvancedControls {
            get {
                return ResourceManager.GetString("CompressorEnableAdvancedControls", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Knee.
        /// </summary>
        public static string CompressorKnee {
            get {
                return ResourceManager.GetString("CompressorKnee", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Make up gain.
        /// </summary>
        public static string CompressorMakeUpGain {
            get {
                return ResourceManager.GetString("CompressorMakeUpGain", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Ratio.
        /// </summary>
        public static string CompressorRatio {
            get {
                return ResourceManager.GetString("CompressorRatio", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Release.
        /// </summary>
        public static string CompressorRelease {
            get {
                return ResourceManager.GetString("CompressorRelease", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Threshold.
        /// </summary>
        public static string CompressorThreshold {
            get {
                return ResourceManager.GetString("CompressorThreshold", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Type.
        /// </summary>
        public static string CompressorType {
            get {
                return ResourceManager.GetString("CompressorType", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Compresses the audio that is routed through this widget. In &quot;upward&quot; mode this means that silent sounds will get louder; in &quot;downward&quot; mode loud sounds are getting quieter..
        /// </summary>
        public static string CompressorWidgetDescription {
            get {
                return ResourceManager.GetString("CompressorWidgetDescription", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Compressor.
        /// </summary>
        public static string CompressorWidgetName {
            get {
                return ResourceManager.GetString("CompressorWidgetName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Device.
        /// </summary>
        public static string Device {
            get {
                return ResourceManager.GetString("Device", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Captures the audio from an input device (i.e. microphone)..
        /// </summary>
        public static string DeviceInputWidgetDescription {
            get {
                return ResourceManager.GetString("DeviceInputWidgetDescription", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Device Input.
        /// </summary>
        public static string DeviceInputWidgetName {
            get {
                return ResourceManager.GetString("DeviceInputWidgetName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Sends audio to an output device (i.e. speakers, headphones)..
        /// </summary>
        public static string DeviceOutputWidgetDescription {
            get {
                return ResourceManager.GetString("DeviceOutputWidgetDescription", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Device Output.
        /// </summary>
        public static string DeviceOutputWidgetName {
            get {
                return ResourceManager.GetString("DeviceOutputWidgetName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Gain.
        /// </summary>
        public static string Gain {
            get {
                return ResourceManager.GetString("Gain", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Changes the gain (in dB) of the audio that is routed through this widget, which allows amplifying the input volume. This can cause the audio to clip if it gets too loud..
        /// </summary>
        public static string GainWidgetDescription {
            get {
                return ResourceManager.GetString("GainWidgetDescription", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Gain.
        /// </summary>
        public static string GainWidgetName {
            get {
                return ResourceManager.GetString("GainWidgetName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Captures the audio that is sent to an output device (i.e. speakers, headphones)..
        /// </summary>
        public static string LoopbackDeviceInputWidgetDescription {
            get {
                return ResourceManager.GetString("LoopbackDeviceInputWidgetDescription", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Loopback Device Input.
        /// </summary>
        public static string LoopbackDeviceInputWidgetName {
            get {
                return ResourceManager.GetString("LoopbackDeviceInputWidgetName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Name.
        /// </summary>
        public static string Name {
            get {
                return ResourceManager.GetString("Name", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Visualizes the audio spectrum..
        /// </summary>
        public static string SpectrumWidgetDescription {
            get {
                return ResourceManager.GetString("SpectrumWidgetDescription", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Spectrum.
        /// </summary>
        public static string SpectrumWidgetName {
            get {
                return ResourceManager.GetString("SpectrumWidgetName", resourceCulture);
            }
        }
    }
}
