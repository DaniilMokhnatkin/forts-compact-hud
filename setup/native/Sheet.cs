using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shell;

namespace FortsSetup {
    // Shared frame for secondary windows so they match the main Tahoe window:
    // system acrylic with a light tint, 36px caption strip, Display-font title,
    // inset grouped cards and capsule buttons.
    public static class Sheet {
        public static Brush Res(Window owner,string key){return (Brush)owner.FindResource(key);}
        public static Style Style(Window owner,string key){return (Style)owner.FindResource(key);}
        public static Grid Frame(Window w,Window owner,string title,string subtitle,string closeName,out Grid body,out Button close){
            w.Owner=owner;w.Title=title;w.WindowStyle=WindowStyle.None;w.ResizeMode=ResizeMode.NoResize;w.Background=Brushes.Transparent;w.FontFamily=owner.FontFamily;w.FontSize=13;w.Foreground=Res(owner,"Label");
            w.UseLayoutRounding=true;w.WindowStartupLocation=WindowStartupLocation.CenterOwner;w.MaxHeight=SystemParameters.WorkArea.Height-32;
            w.Resources.MergedDictionaries.Add(owner.Resources);
            WindowChrome.SetWindowChrome(w,new WindowChrome{CaptionHeight=36,GlassFrameThickness=new Thickness(-1),ResizeBorderThickness=new Thickness(0),UseAeroCaptionButtons=false,CornerRadius=new CornerRadius(0)});
            w.SourceInitialized+=(s,e)=>Glass.Apply(w);
            var root=new Grid();w.Content=root;
            var surface=new Border{Background=(Brush)new BrushConverter().ConvertFrom("#FCFCFE")};Grid.SetRowSpan(surface,2);root.Children.Add(surface);
            root.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});root.RowDefinitions.Add(new RowDefinition{Height=new GridLength(1,GridUnitType.Star)});
            var heading=new StackPanel{Margin=new Thickness(26,26,64,0)};
            heading.Children.Add(new TextBlock{Text=title,FontFamily=(FontFamily)owner.FindResource("Display"),FontSize=24,FontWeight=FontWeights.Bold});
            if(!String.IsNullOrEmpty(subtitle))heading.Children.Add(new TextBlock{Text=subtitle,Foreground=Res(owner,"SecondaryText"),TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,4,0,0)});
            root.Children.Add(heading);
            body=new Grid{Margin=new Thickness(26,18,26,22)};Grid.SetRow(body,1);root.Children.Add(body);
            close=new Button{Style=Style(owner,"Caption"),Content="",HorizontalAlignment=HorizontalAlignment.Right,VerticalAlignment=VerticalAlignment.Top};
            AutomationProperties.SetName(close,closeName);Grid.SetRowSpan(close,2);root.Children.Add(close);
            return root;
        }
        public static Border Group(Window owner,UIElement child,Thickness padding){return new Border{CornerRadius=new CornerRadius(14),Background=Res(owner,"Group"),BorderBrush=(Brush)new BrushConverter().ConvertFrom("#12000000"),BorderThickness=new Thickness(1),Padding=padding,Child=child};}
        public static Border Separator(Window owner){return new Border{Height=1,Background=Res(owner,"Separator"),VerticalAlignment=VerticalAlignment.Bottom,HorizontalAlignment=HorizontalAlignment.Stretch};}
        public static TextBlock Caption(Window owner,string text,Thickness margin){return new TextBlock{Text=text,Foreground=Res(owner,"SecondaryText"),FontSize=12,FontWeight=FontWeights.Medium,Margin=margin};}
    }
}
