using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Automation;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Shell;
using System.Windows.Media.Effects;

namespace FortsSetup {
    public sealed class CustomizationDialog : Window {
        public HudCustomization Value {get;private set;}
        public readonly ComboBox ResourcesChoice,CardsChoice,BackgroundChoice;
        public readonly Slider HueSlider;
        public readonly CheckBox IndicatorsChoice;
        readonly Canvas preview=new Canvas{Width=720,Height=195,ClipToBounds=true};
        readonly TextBlock hueValue=new TextBlock{Width=82,VerticalAlignment=VerticalAlignment.Center,TextAlignment=TextAlignment.Right};
        readonly Ellipse hueSwatch=new Ellipse{Width=16,Height=16,Stroke=Brushes.White,StrokeThickness=1,Margin=new Thickness(8,0,0,0)};
        readonly string layout;
        readonly bool english;
        string L(string ru,string en){return english?en:ru;}
        public CustomizationDialog(Window owner,HudCustomization current,string layoutId,bool en) {
            Owner=owner;layout=layoutId;english=en;Value=current.Clone();
            Title=L("Настройка HUD","Customize HUD");Width=790;Height=715;MaxHeight=SystemParameters.WorkArea.Height-32;ResizeMode=ResizeMode.NoResize;WindowStartupLocation=WindowStartupLocation.CenterOwner;Background=Brushes.Transparent;FontFamily=owner.FontFamily;FontSize=13;WindowStyle=WindowStyle.None;
            Resources.MergedDictionaries.Add(owner.Resources);
            WindowChrome.SetWindowChrome(this,new WindowChrome{CaptionHeight=36,ResizeBorderThickness=new Thickness(0),CornerRadius=new CornerRadius(20),UseAeroCaptionButtons=false});
            SourceInitialized+=(s,e)=>Glass.Apply(this);
            var root=new Grid{Margin=new Thickness(26)};
            var shell=new Grid{Background=(Brush)new BrushConverter().ConvertFrom("#DDF4F4F7")};
            var card=new Border{Margin=new Thickness(8),CornerRadius=new CornerRadius(20),Background=(Brush)new BrushConverter().ConvertFrom("#F7FFFFFF"),BorderBrush=(Brush)new BrushConverter().ConvertFrom("#80FFFFFF"),BorderThickness=new Thickness(1),Child=root,Effect=new DropShadowEffect{BlurRadius=18,ShadowDepth=2,Opacity=0.10}};shell.Children.Add(card);Content=shell;
            var close=new Button{Style=(Style)owner.FindResource("Caption"),Content="\uE8BB",HorizontalAlignment=HorizontalAlignment.Right,VerticalAlignment=VerticalAlignment.Top,Margin=new Thickness(0,8,8,0)};AutomationProperties.SetName(close,L("Закрыть","Close"));close.Click+=(s,e)=>Close();shell.Children.Add(close);
            foreach(var height in new[]{GridLength.Auto,GridLength.Auto,new GridLength(1,GridUnitType.Star),GridLength.Auto})root.RowDefinitions.Add(new RowDefinition{Height=height});
            var heading=new StackPanel();heading.Children.Add(new TextBlock{Text=Title,FontSize=24,FontWeight=FontWeights.SemiBold});heading.Children.Add(new TextBlock{Text=L("Изменения применятся к игре после нажатия «Установить».","Changes apply to the game when you click Install."),Foreground=Brushes.DimGray,Margin=new Thickness(0,6,0,18)});root.Children.Add(heading);
            var controls=new Grid();controls.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(220)});controls.ColumnDefinitions.Add(new ColumnDefinition());
            var group=new Border{CornerRadius=new CornerRadius(14),Background=(Brush)owner.FindResource("Group"),Padding=new Thickness(16,4,16,4),Child=controls};Grid.SetRow(group,1);root.Children.Add(group);
            for(int i=0;i<5;i++)controls.RowDefinitions.Add(new RowDefinition{Height=new GridLength(46)});
            ResourcesChoice=Combo(new[]{L("Справа внизу","Bottom right"),L("Над панелью по центру","Above the centre strip")},"ResourcesPosition");
            CardsChoice=Combo(new[]{L("65% — маленькие","65% — small"),L("80% — компактные","80% — compact"),L("100% — ванильные","100% — original")},"BuildCardSize");
            BackgroundChoice=Combo(new[]{L("0% — без фона","0% — transparent"),L("50% — полупрозрачный","50% — translucent"),L("100% — сплошной","100% — opaque")},"PanelOpacity");
            var hue=new DockPanel();var colorLabel=new StackPanel{Orientation=Orientation.Horizontal,VerticalAlignment=VerticalAlignment.Center};colorLabel.Children.Add(hueValue);colorLabel.Children.Add(hueSwatch);hue.Children.Add(colorLabel);DockPanel.SetDock(colorLabel,Dock.Right);
            HueSlider=new Slider{Style=(Style)owner.FindResource("HueRange"),Minimum=-180,Maximum=180,TickFrequency=15,IsSnapToTickEnabled=true,SmallChange=15,LargeChange=30,VerticalAlignment=VerticalAlignment.Center};AutomationProperties.SetName(HueSlider,L("Оттенок интерфейса","Interface hue"));AutomationProperties.SetAutomationId(HueSlider,"InterfaceHue");
            var hueTrack=new Grid();var spectrum=new LinearGradientBrush{StartPoint=new Point(0,0),EndPoint=new Point(1,0)};for(int i=0;i<=12;i++)spectrum.GradientStops.Add(new GradientStop(Hue(((layout=="zors"?275:230)-180+i*30+360)%360),i/12.0));hueTrack.Children.Add(new Border{Height=7,CornerRadius=new CornerRadius(4),Background=spectrum,VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(10,0,10,0)});hueTrack.Children.Add(HueSlider);hue.Children.Add(hueTrack);
            IndicatorsChoice=new CheckBox{Style=(Style)owner.FindResource("Switch"),HorizontalAlignment=HorizontalAlignment.Right,VerticalAlignment=VerticalAlignment.Center};AutomationProperties.SetName(IndicatorsChoice,L("Сетка, двери и раскосы всегда видны","Always show grid, doors and cross bracing"));AutomationProperties.SetAutomationId(IndicatorsChoice,"ModeIndicators");
            string[] labels={L("Ресурсы","Resources"),L("Карточки строительства","Build cards"),L("Фон панелей","Panel background"),L("Оттенок интерфейса","Interface hue"),L("Сетка, двери и раскосы","Grid, doors and cross bracing")};
            UIElement[] fields={ResourcesChoice,CardsChoice,BackgroundChoice,hue,IndicatorsChoice};
            for(int i=0;i<5;i++){var label=new TextBlock{Text=labels[i],VerticalAlignment=VerticalAlignment.Center};Grid.SetRow(label,i);controls.Children.Add(label);Grid.SetRow(fields[i],i);Grid.SetColumn(fields[i],1);controls.Children.Add(fields[i]);}
            var visual=new StackPanel{Margin=new Thickness(0,16,0,10)};Grid.SetRow(visual,2);root.Children.Add(visual);
            visual.Children.Add(new TextBlock{Text=L("Схема расположения и масштаба","Layout and scale diagram"),FontWeight=FontWeights.SemiBold,Margin=new Thickness(0,0,0,8)});visual.Children.Add(new Border{CornerRadius=new CornerRadius(14),ClipToBounds=true,Child=new Viewbox{Stretch=Stretch.Uniform,Child=preview}});
            visual.Children.Add(new TextBlock{Text=L("Оттенок меняет панели и кнопки. Текст и сигнальные цвета сохраняются.","Hue changes panels and buttons. Text and signal colours are preserved."),Foreground=Brushes.DimGray,FontSize=12,Margin=new Thickness(0,7,0,0)});
            var footer=new DockPanel();Grid.SetRow(footer,3);root.Children.Add(footer);
            var reset=new Button{Style=(Style)owner.FindResource("Secondary"),Content=L("Сбросить","Reset"),HorizontalAlignment=HorizontalAlignment.Left};footer.Children.Add(reset);reset.Click+=(s,e)=>Set(new HudCustomization{Background=layout=="tactical"?100:0});
            var actions=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right};DockPanel.SetDock(actions,Dock.Right);footer.Children.Add(actions);
            var cancel=new Button{Style=(Style)owner.FindResource("Secondary"),Content=L("Отмена","Cancel"),IsCancel=true,Margin=new Thickness(0,0,10,0)};actions.Children.Add(cancel);
            var apply=new Button{Style=(Style)owner.FindResource("Primary"),Content=L("Готово","Done"),IsDefault=true};AutomationProperties.SetAutomationId(apply,"ApplyCustomization");actions.Children.Add(apply);apply.Click+=(s,e)=>{Read();Value.Validate();DialogResult=true;};
            Set(current);
            ResourcesChoice.SelectionChanged+=(s,e)=>Draw();CardsChoice.SelectionChanged+=(s,e)=>Draw();BackgroundChoice.SelectionChanged+=(s,e)=>Draw();HueSlider.ValueChanged+=(s,e)=>Draw();IndicatorsChoice.Checked+=(s,e)=>Draw();IndicatorsChoice.Unchecked+=(s,e)=>Draw();Draw();
        }
        ComboBox Combo(string[] labels,string id){var combo=new ComboBox{Style=(Style)FindResource("TahoeCombo"),ItemsSource=labels,Height=34,VerticalAlignment=VerticalAlignment.Center};AutomationProperties.SetName(combo,id);AutomationProperties.SetAutomationId(combo,id);return combo;}
        void Set(HudCustomization value){ResourcesChoice.SelectedIndex=value.Resources=="corner"?0:1;CardsChoice.SelectedIndex=Array.IndexOf(new[]{65,80,100},value.Cards);BackgroundChoice.SelectedIndex=value.Background/50;HueSlider.Value=value.Hue;IndicatorsChoice.IsChecked=value.Indicators;Draw();}
        void Read(){Value.Resources=ResourcesChoice.SelectedIndex==0?"corner":"center";Value.Cards=new[]{65,80,100}[Math.Max(0,CardsChoice.SelectedIndex)];Value.Background=Math.Max(0,BackgroundChoice.SelectedIndex)*50;Value.Hue=(int)HueSlider.Value;Value.Indicators=IndicatorsChoice.IsChecked==true;}
        void Box(double x,double y,double w,double h,Brush fill,string caption){var border=new Border{Width=w,Height=h,Background=fill,CornerRadius=new CornerRadius(3),BorderBrush=new SolidColorBrush(Color.FromArgb(100,170,180,210)),BorderThickness=new Thickness(1)};if(caption!=null)border.Child=new TextBlock{Text=caption,Foreground=Brushes.White,FontSize=10,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center};Canvas.SetLeft(border,x);Canvas.SetTop(border,y);preview.Children.Add(border);}
        void Draw(){
            if(ResourcesChoice==null||CardsChoice==null||BackgroundChoice==null||HueSlider==null||IndicatorsChoice==null)return;Read();preview.Children.Clear();preview.Background=new SolidColorBrush(Color.FromRgb(88,75,40));
            double hue=((layout=="zors"?275:230)+Value.Hue+360)%360;Color c=Hue(hue);var solid=new SolidColorBrush(c);var backing=new SolidColorBrush(Color.FromArgb((byte)(Value.Background*255/100),c.R,c.G,c.B));
            Box(6,99,112,90,backing,null);Box(598,96,116,93,backing,null);
            double card=36*Value.Cards/100.0;for(int i=0;i<7;i++)Box(236+i*(card+4),154-card,card,card,solid,null);
            Box(230,160,286,23,solid,L("Категории строительства","Build categories"));
            double rx=Value.Resources=="corner"?605:275,ry=Value.Resources=="corner"?103:91;
            Box(rx,ry,102,23,solid,"600 / 600");Box(rx+(Value.Resources=="center"?108:0),ry+(Value.Resources=="corner"?28:0),102,23,solid,"3000 / 3000");
            if(Value.Indicators)for(int i=0;i<3;i++)Box(603+i*26,163,22,20,solid,new[]{"#","|","×"}[i]);
            hueValue.Text=HueLabel(layout,Value.Hue,english);hueSwatch.Fill=solid;AutomationProperties.SetHelpText(HueSlider,hueValue.Text);
        }
        public static string HueLabel(string layout,int value,bool english){if(value==0)return english?"Original":"Исходный";double h=((layout=="zors"?275:230)+value+360)%360;string[] ru={"Красный","Оранжевый","Жёлтый","Зелёный","Бирюзовый","Синий","Фиолетовый","Розовый"},en={"Red","Orange","Yellow","Green","Cyan","Blue","Violet","Pink"};int i=h<15||h>=345?0:h<45?1:h<75?2:h<155?3:h<195?4:h<255?5:h<295?6:7;return (english?en:ru)[i];}
        static Color Hue(double h){double c=.38,x=c*(1-Math.Abs(h/60%2-1)),m=.16,r=0,g=0,b=0;if(h<60){r=c;g=x;}else if(h<120){r=x;g=c;}else if(h<180){g=c;b=x;}else if(h<240){g=x;b=c;}else if(h<300){r=x;b=c;}else{r=c;b=x;}return Color.FromRgb((byte)((r+m)*255),(byte)((g+m)*255),(byte)((b+m)*255));}
    }
}
