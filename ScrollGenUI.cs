using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Reflection;
using System.Drawing.Text;
using System.Windows.Forms;
using System.IO.Compression;
using System.Drawing.Drawing2D;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Registry = Microsoft.Win32.Registry;
using RegistryKey = Microsoft.Win32.RegistryKey;
using PaintDotNet;
using PaintDotNet.AppModel;
using PaintDotNet.Effects;
using PaintDotNet.Clipboard;
using PaintDotNet.IndirectUI;
using PaintDotNet.Collections;
using PaintDotNet.PropertySystem;
using PaintDotNet.Rendering;
using ColorWheelControl = PaintDotNet.ColorBgra;
using AngleControl = System.Double;
using PanSliderControl = PaintDotNet.Rendering.Vector2Double;
using FolderControl = System.String;
using FilenameControl = System.String;
using ReseedButtonControl = System.Byte;
using RollControl = PaintDotNet.Rendering.Vector3Double;
using IntSliderControl = System.Int32;
using CheckboxControl = System.Boolean;
using TextboxControl = System.String;
using DoubleSliderControl = System.Double;
using ListBoxControl = System.Byte;
using RadioButtonControl = System.Byte;
using MultiLineTextboxControl = System.String;
using PaintDotNet.Direct2D1;
using static PaintDotNet.Direct2D1.SampleMapRenderer;

[assembly: AssemblyTitle("ScrollGenerator plugin for Paint.NET")]
[assembly: AssemblyDescription("Generate a scroll ornament pattern (branching spirals)")]
[assembly: AssemblyConfiguration("scroll swirl spiral curl vine arabesque meander")]
[assembly: AssemblyCompany("Anna Yudovin")]
[assembly: AssemblyProduct("ScrollGenerator")]
[assembly: AssemblyCopyright("Copyright ©2023 by Anna Yudovin")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: ComVisible(false)]
[assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyMetadata("BuiltByCodeLab", "Version=6.8.8422.38978")]
[assembly: SupportedOSPlatform("Windows")]

namespace ScrollGeneratorEffect
{
    public class PluginSupportInfo : IPluginSupportInfo
    {
        public string Author => base.GetType().Assembly.GetCustomAttribute<AssemblyCopyrightAttribute>().Copyright;
        public string Copyright => base.GetType().Assembly.GetCustomAttribute<AssemblyDescriptionAttribute>().Description;
        public string DisplayName => base.GetType().Assembly.GetCustomAttribute<AssemblyProductAttribute>().Product;
        public Version Version => base.GetType().Assembly.GetName().Version;
        public Uri WebsiteUri => new Uri("https://www.getpaint.net/redirect/plugins.html");
    }

    [PluginSupportInfo(typeof(PluginSupportInfo), DisplayName = "Scroll Ornament")]
    public class ScrollGeneratorEffectPlugin : PropertyBasedEffect
    {
        public static string StaticName => "Scroll Ornament";
        public static Image StaticIcon => new Bitmap(typeof(ScrollGeneratorEffectPlugin), "scroll_icon_r.png");
        //public static Bitmap StaticIcon
        //{
        //    get { return new Bitmap(typeof(ScrollGeneratorEffectPlugin), "scroll_icon_b.png"); }
        //}
        public static string SubmenuName => SubmenuNames.Render;

        public ScrollGeneratorEffectPlugin()
            : base(StaticName, StaticIcon, SubmenuName, new EffectOptions { Flags = EffectFlags.Configurable, RenderingSchedule = EffectRenderingSchedule.None })
        {
            instanceSeed = unchecked((int)DateTime.Now.Ticks);
        }

        public enum PropertyNames
        {
            TrkMaxNodes,
            ChkSetLeafNum,
            DrpLeafNum,
            ChkSetAng,
            OriginAngle,
            ChkTwin,
            ChkSetTwin,
            TrkTwinRatio,
            ChkResprout,
            ResproutAngle,
            ChkGrad,
            OptLeafSize,
            DrpStyle,
            TrkShiftFactor,
            TrkSpreadFactor,
            ChkRandLeafNum,
            OptRandLrgSz,
            ChkRandAng,
            ChkRandTwin,
            ChkRandColor,
            BtnReseed,
            ScrollColor,
            BackgroundColor,
            ChkThickerLine,
            TabContainer
        }

        public enum DrpLeafNumOptions
        {
            DrpLeafNumOption1,
            DrpLeafNumOption2,
            DrpLeafNumOption3,
            DrpLeafNumOption4,
            DrpLeafNumOption5,
            DrpLeafNumOption6
        }

        public enum OptLeafSizeOptions
        {
            OptLeafSizeOption1,
            OptLeafSizeOption2
        }

        public enum OptRandLrgSzOptions
        {
            OptRandLrgSzOption1,
            OptRandLrgSzOption2,
            OptRandLrgSzOption3
        }

        public enum DrpStyleOptions
        {
            DrpStyleOption1,
            DrpStyleOption2,
            DrpStyleOption3,
            DrpStyleOption4,
            DrpStyleOption5
        }

        private int instanceSeed;

        protected override PropertyCollection OnCreatePropertyCollection()
        {
            //List<int[]> colorPair = scrollTree.GetRGBRandomComplement();
            //scrollColor = ColorBgra.FromBgra((byte)colorPair[0][2], (byte)colorPair[0][1], (byte)colorPair[0][0], 255);
            //backgroundColor = ColorBgra.FromBgra((byte)colorPair[1][2], (byte)colorPair[1][1], (byte)colorPair[1][0], 255);

            scrollColor = ColorBgra.FromUInt32(unchecked((uint)EnvironmentParameters.PrimaryColor)).NewAlpha(255);
            backgroundColor = ColorBgra.FromUInt32(unchecked((uint)EnvironmentParameters.SecondaryColor)).NewAlpha(255);
            
            List<Property> props = new List<Property>();
            props.Add(new Int32Property(PropertyNames.TrkMaxNodes, 200, 0, 1000));
            props.Add(new BooleanProperty(PropertyNames.ChkSetLeafNum, true));
            props.Add(StaticListChoiceProperty.CreateForEnum<DrpLeafNumOptions>(PropertyNames.DrpLeafNum, (DrpLeafNumOptions)5, false));
            props.Add(new BooleanProperty(PropertyNames.ChkSetAng, true));
            props.Add(new DoubleProperty(PropertyNames.OriginAngle, 0, -180, 180));
            props.Add(new BooleanProperty(PropertyNames.ChkTwin, true));
            props.Add(new BooleanProperty(PropertyNames.ChkSetTwin, true));
            props.Add(new DoubleProperty(PropertyNames.TrkTwinRatio, 1, 0.6, 1));
            props.Add(new BooleanProperty(PropertyNames.ChkResprout, false));
            props.Add(new DoubleProperty(PropertyNames.ResproutAngle, 0, -25, 25));
            props.Add(new BooleanProperty(PropertyNames.ChkGrad, true));
            props.Add(StaticListChoiceProperty.CreateForEnum<OptLeafSizeOptions>(PropertyNames.OptLeafSize, 0, false));
            props.Add(StaticListChoiceProperty.CreateForEnum<DrpStyleOptions>(PropertyNames.DrpStyle, (DrpStyleOptions)2, false));
            props.Add(new Int32Property(PropertyNames.TrkShiftFactor, 0, -100, 100));
            props.Add(new Int32Property(PropertyNames.TrkSpreadFactor, 0, -100, 100));
            props.Add(new BooleanProperty(PropertyNames.ChkRandLeafNum, false));
            props.Add(StaticListChoiceProperty.CreateForEnum<OptRandLrgSzOptions>(PropertyNames.OptRandLrgSz, 0, false));
            props.Add(new BooleanProperty(PropertyNames.ChkRandAng, false));
            props.Add(new BooleanProperty(PropertyNames.ChkRandTwin, false));
            props.Add(new BooleanProperty(PropertyNames.ChkRandColor, false));
            props.Add(new Int32Property(PropertyNames.BtnReseed, 0, 0, 255));
            props.Add(new Int32Property(PropertyNames.ScrollColor, unchecked((int)scrollColor.Bgra), Int32.MinValue, Int32.MaxValue));
            props.Add(new Int32Property(PropertyNames.BackgroundColor, unchecked((int)backgroundColor.Bgra), Int32.MinValue, Int32.MaxValue));
            props.Add(new BooleanProperty(PropertyNames.ChkThickerLine, false));

            List<PropertyCollectionRule> propRules = new List<PropertyCollectionRule>();
            propRules.Add(new ReadOnlyBoundToBooleanRule(PropertyNames.DrpLeafNum, PropertyNames.ChkSetLeafNum, true));
            propRules.Add(new ReadOnlyBoundToBooleanRule(PropertyNames.OriginAngle, PropertyNames.ChkSetAng, true));
            propRules.Add(new ReadOnlyBoundToBooleanRule(PropertyNames.ResproutAngle, PropertyNames.ChkResprout, true));
            propRules.Add(new ReadOnlyBoundToBooleanRule(PropertyNames.OptLeafSize, PropertyNames.ChkGrad, true));
            propRules.Add(new ReadOnlyBoundToBooleanRule(PropertyNames.DrpStyle, PropertyNames.ChkGrad, true));
            propRules.Add(new ReadOnlyBoundToBooleanRule(PropertyNames.ChkRandTwin, PropertyNames.ChkTwin, true));
            propRules.Add(new ReadOnlyBoundToBooleanRule(PropertyNames.ChkSetTwin, PropertyNames.ChkTwin, true));
            propRules.Add(new ReadOnlyBoundToBooleanRule(PropertyNames.TrkTwinRatio, PropertyNames.ChkTwin, true));
            propRules.Add(new ReadOnlyBoundToBooleanRule(PropertyNames.TrkTwinRatio, PropertyNames.ChkSetTwin, true));

            propRules.Add(new ReadOnlyBoundToBooleanRule(PropertyNames.ScrollColor, PropertyNames.ChkRandColor, false));
            propRules.Add(new ReadOnlyBoundToBooleanRule(PropertyNames.BackgroundColor, PropertyNames.ChkRandColor, false));

            //for the following 12 lines: the point is to tie three pairs of checkboxes together and make them opposites of each other
            propRules.Add(new SetTargetWhenSourceEqualsAnyValueRule(PropertyNames.ChkRandLeafNum, false, PropertyNames.ChkSetLeafNum, true));
            propRules.Add(new SetTargetWhenSourceEqualsAnyValueRule(PropertyNames.ChkRandLeafNum, true, PropertyNames.ChkSetLeafNum, false));
            propRules.Add(new SetTargetWhenSourceEqualsAnyValueRule(PropertyNames.ChkSetLeafNum, false, PropertyNames.ChkRandLeafNum, true));
            propRules.Add(new SetTargetWhenSourceEqualsAnyValueRule(PropertyNames.ChkSetLeafNum, true, PropertyNames.ChkRandLeafNum, false));
            propRules.Add(new SetTargetWhenSourceEqualsAnyValueRule(PropertyNames.ChkRandAng, false, PropertyNames.ChkSetAng, true));
            propRules.Add(new SetTargetWhenSourceEqualsAnyValueRule(PropertyNames.ChkRandAng, true, PropertyNames.ChkSetAng, false));
            propRules.Add(new SetTargetWhenSourceEqualsAnyValueRule(PropertyNames.ChkSetAng, false, PropertyNames.ChkRandAng, true));
            propRules.Add(new SetTargetWhenSourceEqualsAnyValueRule(PropertyNames.ChkSetAng, true, PropertyNames.ChkRandAng, false));
            propRules.Add(new SetTargetWhenSourceEqualsAnyValueRule(PropertyNames.ChkRandTwin, false, PropertyNames.ChkSetTwin, true));
            propRules.Add(new SetTargetWhenSourceEqualsAnyValueRule(PropertyNames.ChkRandTwin, true, PropertyNames.ChkSetTwin, false));
            propRules.Add(new SetTargetWhenSourceEqualsAnyValueRule(PropertyNames.ChkSetTwin, false, PropertyNames.ChkRandTwin, true));
            propRules.Add(new SetTargetWhenSourceEqualsAnyValueRule(PropertyNames.ChkSetTwin, true, PropertyNames.ChkRandTwin, false));

            //the following two lines "work" in the sense of setting the actual values of the color wheels, 
            //but these values are not reflected by the visible state of the controls themselves
            //propRules.Add(new SetTargetWhenSourceEqualsAnyValueRule(PropertyNames.ScrollColor, unchecked((int)scrollColor.Bgra), PropertyNames.ChkRandColor, true));
            //propRules.Add(new SetTargetWhenSourceEqualsAnyValueRule(PropertyNames.BackgroundColor, unchecked((int)backgroundColor.Bgra), PropertyNames.ChkRandColor, true));
            
            return new PropertyCollection(props, propRules);
        }



        protected override ControlInfo OnCreateConfigUI(PropertyCollection props)
        {
            PanelControlInfo panelControlInfo = new PanelControlInfo();

            panelControlInfo.AddChildControl(PropertyControlInfo.CreateFor(props[PropertyNames.TrkMaxNodes]));
            panelControlInfo.SetPropertyControlValue(PropertyNames.TrkMaxNodes, ControlInfoPropertyNames.DisplayName, "Maximum Number of Nodes:");
            panelControlInfo.SetPropertyControlValue(PropertyNames.TrkMaxNodes, ControlInfoPropertyNames.ShowHeaderLine, false);

            TabContainerControlInfo configUI = new TabContainerControlInfo(props[PropertyNames.TabContainer]);

            TabPageControlInfo tabPage1 = new TabPageControlInfo();
            tabPage1.Text = "Style";
            tabPage1.AddChildControl(PropertyControlInfo.CreateFor(props[PropertyNames.ChkGrad]));
            tabPage1.AddChildControl(PropertyControlInfo.CreateFor(props[PropertyNames.OptLeafSize]));
            tabPage1.AddChildControl(PropertyControlInfo.CreateFor(props[PropertyNames.DrpStyle]));
            tabPage1.AddChildControl(PropertyControlInfo.CreateFor(props[PropertyNames.ChkSetLeafNum]));
            tabPage1.AddChildControl(PropertyControlInfo.CreateFor(props[PropertyNames.DrpLeafNum]));
            tabPage1.AddChildControl(PropertyControlInfo.CreateFor(props[PropertyNames.TrkShiftFactor]));
            tabPage1.AddChildControl(PropertyControlInfo.CreateFor(props[PropertyNames.TrkSpreadFactor]));
            configUI.AddTab(tabPage1);

            TabPageControlInfo tabPage2 = new TabPageControlInfo();
            tabPage2.Text = "Random";
            tabPage2.AddChildControl(PropertyControlInfo.CreateFor(props[PropertyNames.OptRandLrgSz]));
            tabPage2.AddChildControl(PropertyControlInfo.CreateFor(props[PropertyNames.ChkRandLeafNum]));
            tabPage2.AddChildControl(PropertyControlInfo.CreateFor(props[PropertyNames.ChkRandTwin]));
            tabPage2.AddChildControl(PropertyControlInfo.CreateFor(props[PropertyNames.ChkRandAng]));
            tabPage2.AddChildControl(PropertyControlInfo.CreateFor(props[PropertyNames.ChkRandColor]));
            tabPage2.AddChildControl(PropertyControlInfo.CreateFor(props[PropertyNames.BtnReseed]));
            configUI.AddTab(tabPage2);

            TabPageControlInfo tabPage3 = new TabPageControlInfo();
            tabPage3.Text = "Color";
            tabPage3.AddChildControl(PropertyControlInfo.CreateFor(props[PropertyNames.BackgroundColor]));
            tabPage3.AddChildControl(PropertyControlInfo.CreateFor(props[PropertyNames.ScrollColor]));
            tabPage3.AddChildControl(PropertyControlInfo.CreateFor(props[PropertyNames.ChkThickerLine]));
            configUI.AddTab(tabPage3);

            TabPageControlInfo tabPage4 = new TabPageControlInfo();
            tabPage4.Text = "More";
            tabPage4.AddChildControl(PropertyControlInfo.CreateFor(props[PropertyNames.ChkResprout]));
            tabPage4.AddChildControl(PropertyControlInfo.CreateFor(props[PropertyNames.ResproutAngle]));
            tabPage4.AddChildControl(PropertyControlInfo.CreateFor(props[PropertyNames.ChkTwin]));
            tabPage4.AddChildControl(PropertyControlInfo.CreateFor(props[PropertyNames.ChkSetTwin]));
            tabPage4.AddChildControl(PropertyControlInfo.CreateFor(props[PropertyNames.TrkTwinRatio]));
            tabPage4.AddChildControl(PropertyControlInfo.CreateFor(props[PropertyNames.ChkSetAng]));
            tabPage4.AddChildControl(PropertyControlInfo.CreateFor(props[PropertyNames.OriginAngle]));
            configUI.AddTab(tabPage4);

            //"Style" tab controls
            configUI.SetPropertyControlValue(PropertyNames.ChkGrad, ControlInfoPropertyNames.DisplayName, string.Empty);
            configUI.SetPropertyControlValue(PropertyNames.ChkGrad, ControlInfoPropertyNames.Description, "Leaf Size: Non-Uniform");
            configUI.SetPropertyControlValue(PropertyNames.ChkGrad, ControlInfoPropertyNames.ShowHeaderLine, true);
            configUI.SetPropertyControlValue(PropertyNames.OptLeafSize, ControlInfoPropertyNames.DisplayName, "Leaf Size Variation:");
            configUI.SetPropertyControlType(PropertyNames.OptLeafSize, PropertyControlType.RadioButton);
            PropertyControlInfo OptLeafSizeControl = configUI.FindControlForPropertyName(PropertyNames.OptLeafSize);
            OptLeafSizeControl.SetValueDisplayName(OptLeafSizeOptions.OptLeafSizeOption1, "Large-To-Small");
            OptLeafSizeControl.SetValueDisplayName(OptLeafSizeOptions.OptLeafSizeOption2, "Small-To-Large");
            configUI.SetPropertyControlValue(PropertyNames.OptLeafSize, ControlInfoPropertyNames.ShowHeaderLine, true);
            configUI.SetPropertyControlValue(PropertyNames.DrpStyle, ControlInfoPropertyNames.DisplayName, "Style Options:");
            PropertyControlInfo DrpStyleControl = configUI.FindControlForPropertyName(PropertyNames.DrpStyle);
            DrpStyleControl.SetValueDisplayName(DrpStyleOptions.DrpStyleOption1, "1");
            DrpStyleControl.SetValueDisplayName(DrpStyleOptions.DrpStyleOption2, "2");
            DrpStyleControl.SetValueDisplayName(DrpStyleOptions.DrpStyleOption3, "3");
            DrpStyleControl.SetValueDisplayName(DrpStyleOptions.DrpStyleOption4, "4");
            DrpStyleControl.SetValueDisplayName(DrpStyleOptions.DrpStyleOption5, "5");
            configUI.SetPropertyControlValue(PropertyNames.DrpStyle, ControlInfoPropertyNames.ShowHeaderLine, true);

            configUI.SetPropertyControlValue(PropertyNames.ChkSetLeafNum, ControlInfoPropertyNames.DisplayName, "Maximum Number of Leaves:");
            configUI.SetPropertyControlValue(PropertyNames.ChkSetLeafNum, ControlInfoPropertyNames.Description, "Specify (Non-Random)");
            configUI.SetPropertyControlValue(PropertyNames.ChkSetLeafNum, ControlInfoPropertyNames.ShowHeaderLine, true);
            configUI.SetPropertyControlValue(PropertyNames.DrpLeafNum, ControlInfoPropertyNames.DisplayName, "");
            PropertyControlInfo DrpLeafNumControl = configUI.FindControlForPropertyName(PropertyNames.DrpLeafNum);
            DrpLeafNumControl.SetValueDisplayName(DrpLeafNumOptions.DrpLeafNumOption1, "2");
            DrpLeafNumControl.SetValueDisplayName(DrpLeafNumOptions.DrpLeafNumOption2, "3");
            DrpLeafNumControl.SetValueDisplayName(DrpLeafNumOptions.DrpLeafNumOption3, "4");
            DrpLeafNumControl.SetValueDisplayName(DrpLeafNumOptions.DrpLeafNumOption4, "5");
            DrpLeafNumControl.SetValueDisplayName(DrpLeafNumOptions.DrpLeafNumOption5, "6");
            DrpLeafNumControl.SetValueDisplayName(DrpLeafNumOptions.DrpLeafNumOption6, "7");
            configUI.SetPropertyControlValue(PropertyNames.DrpLeafNum, ControlInfoPropertyNames.ShowHeaderLine, false);
            configUI.SetPropertyControlValue(PropertyNames.TrkShiftFactor, ControlInfoPropertyNames.DisplayName, "Adjust Look: Shift");
            configUI.SetPropertyControlValue(PropertyNames.TrkShiftFactor, ControlInfoPropertyNames.ShowHeaderLine, true);
            configUI.SetPropertyControlValue(PropertyNames.TrkSpreadFactor, ControlInfoPropertyNames.DisplayName, "Adjust Look: Spread");
            configUI.SetPropertyControlValue(PropertyNames.TrkSpreadFactor, ControlInfoPropertyNames.ShowHeaderLine, true);

            //"Random" tab controls
            configUI.SetPropertyControlValue(PropertyNames.OptRandLrgSz, ControlInfoPropertyNames.DisplayName, "Leaf Size: Randomness");
            configUI.SetPropertyControlType(PropertyNames.OptRandLrgSz, PropertyControlType.RadioButton);
            configUI.SetPropertyControlValue(PropertyNames.OptRandLrgSz, ControlInfoPropertyNames.ShowHeaderLine, false);
            PropertyControlInfo OptRandLrgSzControl = configUI.FindControlForPropertyName(PropertyNames.OptRandLrgSz);
            OptRandLrgSzControl.SetValueDisplayName(OptRandLrgSzOptions.OptRandLrgSzOption1, "None");
            OptRandLrgSzControl.SetValueDisplayName(OptRandLrgSzOptions.OptRandLrgSzOption2, "Some");
            OptRandLrgSzControl.SetValueDisplayName(OptRandLrgSzOptions.OptRandLrgSzOption3, "Lots");

            configUI.SetPropertyControlValue(PropertyNames.ChkRandLeafNum, ControlInfoPropertyNames.DisplayName, "Maximum Number of Leaves:");
            configUI.SetPropertyControlValue(PropertyNames.ChkRandLeafNum, ControlInfoPropertyNames.Description, "Random");
            configUI.SetPropertyControlValue(PropertyNames.ChkRandLeafNum, ControlInfoPropertyNames.ShowHeaderLine, true);
            configUI.SetPropertyControlValue(PropertyNames.ChkRandAng, ControlInfoPropertyNames.DisplayName, "Origin Angle:");
            configUI.SetPropertyControlValue(PropertyNames.ChkRandAng, ControlInfoPropertyNames.Description, "Random");
            configUI.SetPropertyControlValue(PropertyNames.ChkRandAng, ControlInfoPropertyNames.ShowHeaderLine, true);
            configUI.SetPropertyControlValue(PropertyNames.ChkRandTwin, ControlInfoPropertyNames.DisplayName, "Origin Curl Size Ratio:");
            configUI.SetPropertyControlValue(PropertyNames.ChkRandTwin, ControlInfoPropertyNames.Description, "Random");
            configUI.SetPropertyControlValue(PropertyNames.ChkRandTwin, ControlInfoPropertyNames.ShowHeaderLine, true);
            configUI.SetPropertyControlValue(PropertyNames.ChkRandColor, ControlInfoPropertyNames.DisplayName, "Color:");
            configUI.SetPropertyControlValue(PropertyNames.ChkRandColor, ControlInfoPropertyNames.Description, "Random Complement");
            configUI.SetPropertyControlValue(PropertyNames.ChkRandColor, ControlInfoPropertyNames.ShowHeaderLine, true);
            configUI.SetPropertyControlValue(PropertyNames.BtnReseed, ControlInfoPropertyNames.DisplayName, " ");
            configUI.SetPropertyControlType(PropertyNames.BtnReseed, PropertyControlType.IncrementButton);
            configUI.SetPropertyControlValue(PropertyNames.BtnReseed, ControlInfoPropertyNames.ButtonText, "Re-Seed");
            configUI.SetPropertyControlValue(PropertyNames.BtnReseed, ControlInfoPropertyNames.ShowHeaderLine, true);

            //"Color" tab controls
            configUI.SetPropertyControlValue(PropertyNames.BackgroundColor, ControlInfoPropertyNames.DisplayName, "Background Color");
            configUI.SetPropertyControlType(PropertyNames.BackgroundColor, PropertyControlType.ColorWheel);
            configUI.SetPropertyControlValue(PropertyNames.BackgroundColor, ControlInfoPropertyNames.ShowResetButton, true);
            configUI.SetPropertyControlValue(PropertyNames.BackgroundColor, ControlInfoPropertyNames.ShowHeaderLine, true);
            configUI.SetPropertyControlValue(PropertyNames.ScrollColor, ControlInfoPropertyNames.DisplayName, "Ornament Color");
            configUI.SetPropertyControlType(PropertyNames.ScrollColor, PropertyControlType.ColorWheel);
            configUI.SetPropertyControlValue(PropertyNames.ScrollColor, ControlInfoPropertyNames.ShowResetButton, true);
            configUI.SetPropertyControlValue(PropertyNames.ScrollColor, ControlInfoPropertyNames.ShowHeaderLine, true);
            configUI.SetPropertyControlValue(PropertyNames.ChkThickerLine, ControlInfoPropertyNames.DisplayName, " ");
            configUI.SetPropertyControlValue(PropertyNames.ChkThickerLine, ControlInfoPropertyNames.Description, "Thicker Line");
            configUI.SetPropertyControlValue(PropertyNames.ChkThickerLine, ControlInfoPropertyNames.ShowHeaderLine, true);
            //
            //"More" tab controls
            configUI.SetPropertyControlValue(PropertyNames.ChkResprout, ControlInfoPropertyNames.DisplayName, string.Empty);
            configUI.SetPropertyControlValue(PropertyNames.ChkResprout, ControlInfoPropertyNames.Description, "Resprout");
            configUI.SetPropertyControlValue(PropertyNames.ChkResprout, ControlInfoPropertyNames.ShowHeaderLine, false);
            configUI.SetPropertyControlValue(PropertyNames.ResproutAngle, ControlInfoPropertyNames.DisplayName, "Adjust Resprout Angle:");
            configUI.SetPropertyControlType(PropertyNames.ResproutAngle, PropertyControlType.AngleChooser);
            configUI.SetPropertyControlValue(PropertyNames.ResproutAngle, ControlInfoPropertyNames.DecimalPlaces, 3);
            configUI.SetPropertyControlValue(PropertyNames.ResproutAngle, ControlInfoPropertyNames.ShowHeaderLine, false);

            configUI.SetPropertyControlValue(PropertyNames.ChkTwin, ControlInfoPropertyNames.DisplayName, "Origin:");
            configUI.SetPropertyControlValue(PropertyNames.ChkTwin, ControlInfoPropertyNames.Description, "Twin");
            configUI.SetPropertyControlValue(PropertyNames.ChkTwin, ControlInfoPropertyNames.ShowHeaderLine, true);
            configUI.SetPropertyControlValue(PropertyNames.ChkSetTwin, ControlInfoPropertyNames.DisplayName, string.Empty);
            configUI.SetPropertyControlValue(PropertyNames.ChkSetTwin, ControlInfoPropertyNames.Description, "Twin Size Ratio: Select");
            configUI.SetPropertyControlValue(PropertyNames.ChkSetTwin, ControlInfoPropertyNames.ShowHeaderLine, false);
            configUI.SetPropertyControlValue(PropertyNames.TrkTwinRatio, ControlInfoPropertyNames.DisplayName, "");
            configUI.SetPropertyControlValue(PropertyNames.TrkTwinRatio, ControlInfoPropertyNames.SliderLargeChange, 0.1);
            configUI.SetPropertyControlValue(PropertyNames.TrkTwinRatio, ControlInfoPropertyNames.SliderSmallChange, 0.05);
            configUI.SetPropertyControlValue(PropertyNames.TrkTwinRatio, ControlInfoPropertyNames.UpDownIncrement, 0.01);
            configUI.SetPropertyControlValue(PropertyNames.TrkTwinRatio, ControlInfoPropertyNames.DecimalPlaces, 3);
            configUI.SetPropertyControlValue(PropertyNames.TrkTwinRatio, ControlInfoPropertyNames.ShowHeaderLine, false);
            configUI.SetPropertyControlValue(PropertyNames.ChkSetAng, ControlInfoPropertyNames.DisplayName, string.Empty);
            configUI.SetPropertyControlValue(PropertyNames.ChkSetAng, ControlInfoPropertyNames.Description, "Origin Angle: Select");
            configUI.SetPropertyControlValue(PropertyNames.ChkSetAng, ControlInfoPropertyNames.ShowHeaderLine, false);
            configUI.SetPropertyControlValue(PropertyNames.OriginAngle, ControlInfoPropertyNames.DisplayName, "");
            configUI.SetPropertyControlType(PropertyNames.OriginAngle, PropertyControlType.AngleChooser);
            configUI.SetPropertyControlValue(PropertyNames.OriginAngle, ControlInfoPropertyNames.DecimalPlaces, 3);
            configUI.SetPropertyControlValue(PropertyNames.OriginAngle, ControlInfoPropertyNames.ShowHeaderLine, false);

            panelControlInfo.AddChildControl(configUI);
            return panelControlInfo;
        }

        protected override void OnCustomizeConfigUIWindowProperties(PropertyCollection props)
        {
            // Change the effect's window title
            props[ControlInfoPropertyNames.WindowTitle].Value = "Scroll Ornament Generator";
            base.OnCustomizeConfigUIWindowProperties(props);
        }

        protected override void OnSetRenderInfo(PropertyBasedEffectConfigToken token, RenderArgs dstArgs, RenderArgs srcArgs)
        {
            trkMaxNodes = token.GetProperty<Int32Property>(PropertyNames.TrkMaxNodes).Value;
            chkSetLeafNum = token.GetProperty<BooleanProperty>(PropertyNames.ChkSetLeafNum).Value;
            drpLeafNum = (byte)(int)token.GetProperty<StaticListChoiceProperty>(PropertyNames.DrpLeafNum).Value;
            chkSetAng = token.GetProperty<BooleanProperty>(PropertyNames.ChkSetAng).Value;
            originAngle = token.GetProperty<DoubleProperty>(PropertyNames.OriginAngle).Value;
            chkTwin = token.GetProperty<BooleanProperty>(PropertyNames.ChkTwin).Value;
            chkSetTwin = token.GetProperty<BooleanProperty>(PropertyNames.ChkSetTwin).Value;
            trkTwinRatio = token.GetProperty<DoubleProperty>(PropertyNames.TrkTwinRatio).Value;
            chkResprout = token.GetProperty<BooleanProperty>(PropertyNames.ChkResprout).Value;
            resproutAngle = token.GetProperty<DoubleProperty>(PropertyNames.ResproutAngle).Value;
            chkGrad = token.GetProperty<BooleanProperty>(PropertyNames.ChkGrad).Value;
            optLeafSize = (byte)(int)token.GetProperty<StaticListChoiceProperty>(PropertyNames.OptLeafSize).Value;
            drpStyle = (byte)(int)token.GetProperty<StaticListChoiceProperty>(PropertyNames.DrpStyle).Value;
            trkShiftFactor = token.GetProperty<Int32Property>(PropertyNames.TrkShiftFactor).Value;
            trkSpreadFactor = token.GetProperty<Int32Property>(PropertyNames.TrkSpreadFactor).Value;
            chkRandLeafNum = token.GetProperty<BooleanProperty>(PropertyNames.ChkRandLeafNum).Value;
            optRandLrgSz = (byte)(int)token.GetProperty<StaticListChoiceProperty>(PropertyNames.OptRandLrgSz).Value;
            chkRandAng = token.GetProperty<BooleanProperty>(PropertyNames.ChkRandAng).Value;
            chkRandTwin = token.GetProperty<BooleanProperty>(PropertyNames.ChkRandTwin).Value;
            chkRandColor = token.GetProperty<BooleanProperty>(PropertyNames.ChkRandColor).Value;
            chkThickerLine = token.GetProperty<BooleanProperty>(PropertyNames.ChkThickerLine).Value;
            btnReseed = (byte)token.GetProperty<Int32Property>(PropertyNames.BtnReseed).Value;
            
            if (chkRandColor && scrollTree is not null)
            {
                List<int[]> colorPair = scrollTree.GetRGBRandomComplement();
                scrollColor = ColorBgra.FromBgra((byte)colorPair[0][2], (byte)colorPair[0][1], (byte)colorPair[0][0], 255);
                backgroundColor = ColorBgra.FromBgra((byte)colorPair[1][2], (byte)colorPair[1][1], (byte)colorPair[1][0], 255);

                //ColorBgra _scrollColor = ColorBgra.FromBgra((byte)colorPair[0][2], (byte)colorPair[0][1], (byte)colorPair[0][0], 255);
                //ColorBgra _backgroundColor = ColorBgra.FromBgra((byte)colorPair[1][2], (byte)colorPair[1][1], (byte)colorPair[1][0], 255);
                //token.SetPropertyValue(PropertyNames.ScrollColor, ColorBgra.ToOpaqueInt32(_scrollColor));
                //token.SetPropertyValue(PropertyNames.BackgroundColor, ColorBgra.ToOpaqueInt32(_backgroundColor));
            }
            else 
            { 
                scrollColor = ColorBgra.FromUInt32(unchecked((uint)token.GetProperty<Int32Property>(PropertyNames.ScrollColor).Value));
                backgroundColor = ColorBgra.FromUInt32(unchecked((uint)token.GetProperty<Int32Property>(PropertyNames.BackgroundColor).Value));
            }

            PreRender(dstArgs.Surface, srcArgs.Surface);
            base.OnSetRenderInfo(token, dstArgs, srcArgs);
        }

        protected override unsafe void OnRender(Rectangle[] rois, int startIndex, int length)
        {
            if (length == 0) return;
            for (int i = startIndex; i < startIndex + length; ++i)
            {
                Render(DstArgs.Surface,SrcArgs.Surface,rois[i]);
            }
        }

        #region User Entered Code
        // Name:
        // Submenu:
        // Author:
        // Title:
        // Version:
        // Desc:
        // Keywords:
        // URL:
        // Help:
        // Force Aliased Selection
        // Force Single Render Call
        #region UICode
        IntSliderControl trkMaxNodes = 200; // [0,1000] Number of Nodes (maximum)
        CheckboxControl chkSetLeafNum = true; // Max Number of Leaves: Specify
        ListBoxControl drpLeafNum = 0; // {chkSetLeafNum} |7|6|5|4|3|2
        CheckboxControl chkSetAng = true; // Origin Angle: Specify
        AngleControl originAngle = 0; // [-180,180] {chkSetAng} 
        CheckboxControl chkTwin = true; // Twin Origin
        CheckboxControl chkSetTwin = true; // Twin Curl Size Ratio: Select (Non-Random)
        DoubleSliderControl trkTwinRatio = 1; // [0.6,1] Twin Curl Size Ratio
        CheckboxControl chkResprout = true; // Resprout
        AngleControl resproutAngle = 0; // [-25,25] {chkResprout} Adjust Resprout Angle
        CheckboxControl chkGrad = true; // Leaf Size: Non-Uniform
        RadioButtonControl optLeafSize = 0; // {chkGrad} Leaf Size Variation|Large-To-Small|Small-To-Large
        ListBoxControl drpStyle = 0; // {chkGrad} Style Options|1|2|3|4|5
        IntSliderControl trkShiftFactor = 0; // [-100,100] Adjust Look: Shift
        IntSliderControl trkSpreadFactor = 0; // [-100,100] Adjust Look: Spread
        CheckboxControl chkRandLeafNum = false; // Max Number of Leaves: Random
        RadioButtonControl optRandLrgSz = 0; // Leaf Size Randomness|None|Some|Lots
        CheckboxControl chkRandAng = false; // Origin Angle: Random
        CheckboxControl chkRandTwin = false; // Twin Curl Size Ratio: Random
        CheckboxControl chkRandColor = false; // Color: Random Complement
        ReseedButtonControl btnReseed = 0; // Re-Seed
        ColorWheelControl scrollColor = ColorBgra.FromBgra(0, 0, 0, 255); // [PrimaryColor?!] Scroll Color
        ColorWheelControl backgroundColor = ColorBgra.FromBgra(255, 255, 255, 255); // [SecondaryColor?!] Background Color
        CheckboxControl chkThickerLine = false; // Thicker Line
        #endregion

        // Aux surface
        Surface aux = null;
        private SpiralTree scrollTree = new();
        private List<PointF[]> cachedTreePoints = new();
        private Polygon Boundry;
        private Graphics plotter;

        protected override void OnDispose(bool disposing)
        {
            if (disposing)
            {
                // Release any surfaces or effects you've created
                aux?.Dispose(); aux = null;
            }
        
            base.OnDispose(disposing);
        }
        
        // This single-threaded function is called after the UI changes and before the Render function is called
        // The purpose is to prepare anything you'll need in the Render function
        void PreRender(Surface dst, Surface src)
        {
            if (aux == null)
            {
                aux = new Surface(src.Size);
            }
            plotter = Graphics.FromImage(aux.CreateAliasedBitmap());
            plotter.SmoothingMode = SmoothingMode.AntiAlias;
            CreateTree();
        }


        private void CreateTree()
        {
            float ht = aux.Size.Height;
            float wd = aux.Size.Width;
            float[][] coordArry = new float[4][] { new float[2] { 0f, 0f },
                                                   new float[2] { 0f, ht },
                                                   new float[2] { wd, ht },
                                                   new float[2] { wd, 0f } };
            //set Configs values from interface
            Configs.maxNodes = trkMaxNodes;
            Configs.maxLeaves = drpLeafNum + 2;
            Configs.RANDNUM = chkRandLeafNum;

            Configs.GRAD = chkGrad;
            Configs.SMTOLG = (chkGrad && optLeafSize == 1) ? true : false;
            Configs.DIVOPTION = drpStyle + 1;
            Configs.RANDLRG = (optRandLrgSz == 1 || optRandLrgSz == 2) ? true : false;
            Configs.RANDSZ = (optRandLrgSz == 2) ? true : false;

            Configs.TWIN = chkTwin;
            if (chkSetTwin == true) { Configs.twinRatio = (float)trkTwinRatio; }
            else { Configs.twinRatio = 1f; }
            Configs.RANDTWIN = chkRandTwin;
            Configs.RANDANG = chkRandAng;
            Configs.rootAngle = (float)(originAngle * Math.PI) / 180f;

            Configs.GROWTINY = chkResprout;
            Configs.sproutAdjustment = (float)(resproutAngle * Math.PI) / 180f;

            Configs.spreadFactor = trkSpreadFactor * Configs.nodeHalo / 2f;
            Configs.shiftFactor = trkShiftFactor / 100f;

            Boundry = new Polygon(coordArry, true);
            scrollTree = new SpiralTree(Boundry);
            scrollTree.Grow();
            cachedTreePoints = scrollTree.PlotSpiralTreePoints();
        }


        // Here is the main multi-threaded render function
        // The dst canvas is broken up into rectangles and
        // your job is to write to each pixel of that rectangle
        void Render(Surface dst, Surface src, Rectangle rect)
        {
            Color drawColor = scrollColor.ToColor();
            Color bkgColor = backgroundColor.ToColor();

            plotter.Clear(bkgColor);
            aux.Fill(rect, bkgColor);
            float penWidth = 1.7f;
            if (chkThickerLine) { penWidth = 2.7f; }
            Pen pen = new(drawColor, penWidth);

            foreach (PointF[] _pArry in cachedTreePoints)
            {
                plotter.DrawCurve(pen, _pArry);
            }

            // Step through each row of the current rectangle
            for (int y = rect.Top; y < rect.Bottom; y++)
            {
                if (IsCancelRequested) return;
                // Step through each pixel on the current row of the rectangle
                for (int x = rect.Left; x < rect.Right; x++)
                {
                    dst[x, y] = aux[x, y];
                }
            }
        }
        #endregion
    }
}
