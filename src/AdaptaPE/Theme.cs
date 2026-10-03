using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AdaptaPE
{
    static class Theme
    {
        public static readonly Brush Blue=new SolidColorBrush(Color.FromRgb(10,66,153));
        static readonly System.Collections.Generic.Dictionary<string,BitmapImage> assets=new System.Collections.Generic.Dictionary<string,BitmapImage>();
        public static BitmapImage Asset(string name){BitmapImage image;if(!assets.TryGetValue(name,out image)){image=new BitmapImage(new Uri("pack://application:,,,/AdaptaPE;component/brand/"+name));image.Freeze();assets.Add(name,image);}return image;}
        public static Image Logo(double size){return new Image{Source=Asset("logo.png"),Width=size,Height=size,Margin=new Thickness(0,0,14,0)};}
        public static void Apply(Application app){
            var dictionary=(ResourceDictionary)XamlReader.Parse(@"<ResourceDictionary xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
<Style TargetType='Button'>
 <Setter Property='Background' Value='White'/><Setter Property='Foreground' Value='#17365B'/><Setter Property='BorderBrush' Value='#D5DFEB'/><Setter Property='BorderThickness' Value='1'/><Setter Property='FontSize' Value='15'/><Setter Property='FontWeight' Value='SemiBold'/><Setter Property='MinHeight' Value='44'/><Setter Property='Padding' Value='14,8'/>
 <Setter Property='Template'><Setter.Value><ControlTemplate TargetType='Button'><Border x:Name='surface' Background='{TemplateBinding Background}' BorderBrush='{TemplateBinding BorderBrush}' BorderThickness='{TemplateBinding BorderThickness}' CornerRadius='10' Padding='{TemplateBinding Padding}'><ContentPresenter HorizontalAlignment='Center' VerticalAlignment='Center'/></Border><ControlTemplate.Triggers>
 <Trigger Property='IsMouseOver' Value='True'><Setter TargetName='surface' Property='Opacity' Value='0.9'/></Trigger><Trigger Property='IsPressed' Value='True'><Setter TargetName='surface' Property='Opacity' Value='0.75'/></Trigger><Trigger Property='IsKeyboardFocused' Value='True'><Setter Property='BorderThickness' Value='2'/><Setter Property='BorderBrush' Value='#0A4299'/></Trigger><Trigger Property='IsEnabled' Value='False'><Setter Property='Background' Value='#DCE1E7'/><Setter Property='Foreground' Value='#4B5563'/></Trigger>
 </ControlTemplate.Triggers></ControlTemplate></Setter.Value></Setter>
</Style>
<Style TargetType='CheckBox'>
 <Setter Property='MinHeight' Value='44'/><Setter Property='FontSize' Value='16'/><Setter Property='VerticalContentAlignment' Value='Center'/>
 <Setter Property='Template'><Setter.Value><ControlTemplate TargetType='CheckBox'><Grid><Grid.ColumnDefinitions><ColumnDefinition Width='*'/><ColumnDefinition Width='58'/></Grid.ColumnDefinitions><ContentPresenter VerticalAlignment='Center' Margin='0,0,12,0'/><Border x:Name='track' Grid.Column='1' Height='28' Width='50' Background='#7B8798' CornerRadius='14' BorderBrush='Transparent' BorderThickness='2'><Ellipse x:Name='thumb' Width='20' Height='20' Fill='White' HorizontalAlignment='Left' Margin='2,0'/></Border></Grid><ControlTemplate.Triggers><Trigger Property='IsChecked' Value='True'><Setter TargetName='track' Property='Background' Value='#0A4299'/><Setter TargetName='thumb' Property='HorizontalAlignment' Value='Right'/></Trigger><Trigger Property='IsKeyboardFocused' Value='True'><Setter TargetName='track' Property='BorderBrush' Value='#0A4299'/></Trigger><Trigger Property='IsEnabled' Value='False'><Setter TargetName='track' Property='Opacity' Value='0.4'/></Trigger></ControlTemplate.Triggers></ControlTemplate></Setter.Value></Setter>
</Style>
<Style TargetType='TabControl'><Setter Property='Background' Value='Transparent'/><Setter Property='BorderThickness' Value='0'/></Style>
<Style TargetType='TabItem'>
 <Setter Property='Padding' Value='16,10'/><Setter Property='MinHeight' Value='44'/><Setter Property='Foreground' Value='#475569'/><Setter Property='FontWeight' Value='SemiBold'/>
 <Setter Property='Template'><Setter.Value><ControlTemplate TargetType='TabItem'><Border x:Name='tab' Background='Transparent' CornerRadius='10' Padding='{TemplateBinding Padding}' Margin='0,0,6,8' BorderBrush='Transparent' BorderThickness='2'><ContentPresenter ContentSource='Header' VerticalAlignment='Center' HorizontalAlignment='Center'/></Border><ControlTemplate.Triggers><Trigger Property='IsSelected' Value='True'><Setter TargetName='tab' Property='Background' Value='#E5EEFA'/><Setter Property='Foreground' Value='#0A4299'/></Trigger><Trigger Property='IsKeyboardFocused' Value='True'><Setter TargetName='tab' Property='BorderThickness' Value='2'/><Setter TargetName='tab' Property='BorderBrush' Value='#0A4299'/></Trigger></ControlTemplate.Triggers></ControlTemplate></Setter.Value></Setter>
</Style>
<Style TargetType='TextBox'><Setter Property='BorderBrush' Value='#CBD5E1'/><Setter Property='Background' Value='#F8FAFC'/><Setter Property='FontSize' Value='16'/></Style>
<Style TargetType='ComboBox'><Setter Property='FontSize' Value='16'/><Setter Property='Padding' Value='8,6'/></Style>
<Style TargetType='ProgressBar'><Setter Property='Foreground' Value='#0A4299'/><Setter Property='Background' Value='#DDE6F1'/></Style>
</ResourceDictionary>");app.Resources.MergedDictionaries.Add(dictionary);
        }
    }
}
