using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Automation;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace FortsSetup {
    public sealed class GalleryViewer : Window {
        public int Index {get;private set;}
        public readonly Image Picture=new Image{Stretch=Stretch.Uniform,Margin=new Thickness(20)};
        public readonly TextBlock Counter=new TextBlock{FontWeight=FontWeights.SemiBold,FontSize=14,VerticalAlignment=VerticalAlignment.Center,MinWidth=80,TextAlignment=TextAlignment.Center};
        readonly TextBlock caption=new TextBlock{VerticalAlignment=VerticalAlignment.Center,FontSize=13,Foreground=Brushes.DimGray,Margin=new Thickness(18,0,18,0),TextTrimming=TextTrimming.CharacterEllipsis};
        readonly string[] files;
        readonly bool noMotion;
        readonly Button previous,next;
        int revision;
        public GalleryViewer(Window owner,string[] paths,int index,string theme,bool english,bool reduced){
            Owner=owner;files=(string[])paths.Clone();Index=index;noMotion=reduced;Title=(english?"Screenshots — ":"Скриншоты — ")+theme;WindowState=WindowState.Maximized;Background=(Brush)new BrushConverter().ConvertFrom("#F5F5F7");FontFamily=owner.FontFamily;ShowInTaskbar=false;Resources.MergedDictionaries.Add(owner.Resources);
            var root=new Grid();Content=root;root.RowDefinitions.Add(new RowDefinition{Height=new GridLength(58)});root.RowDefinitions.Add(new RowDefinition());root.RowDefinitions.Add(new RowDefinition{Height=new GridLength(64)});
            var header=new Grid{Margin=new Thickness(24,0,24,0)};root.Children.Add(header);header.Children.Add(new TextBlock{Text=Title,FontSize=18,FontWeight=FontWeights.SemiBold,VerticalAlignment=VerticalAlignment.Center});
            var close=new Button{Style=(Style)owner.FindResource("Secondary"),Content=english?"Close · Esc":"Закрыть · Esc",HorizontalAlignment=HorizontalAlignment.Right,VerticalAlignment=VerticalAlignment.Center};AutomationProperties.SetAutomationId(close,"GalleryClose");close.Click+=(s,e)=>Close();header.Children.Add(close);
            var well=new Border{Background=(Brush)new BrushConverter().ConvertFrom("#17171B"),Child=Picture};Grid.SetRow(well,1);root.Children.Add(well);
            var toolbar=new DockPanel{Margin=new Thickness(24,10,24,10),LastChildFill=true};Grid.SetRow(toolbar,2);root.Children.Add(toolbar);
            var nav=new StackPanel{Orientation=Orientation.Horizontal};toolbar.Children.Add(nav);
            previous=new Button{Style=(Style)owner.FindResource("Secondary"),Content=english?"‹ Previous":"‹ Назад",IsEnabled=files.Length>1};AutomationProperties.SetAutomationId(previous,"GalleryPrevious");previous.Click+=(s,e)=>Step(-1);nav.Children.Add(previous);nav.Children.Add(Counter);
            next=new Button{Style=(Style)owner.FindResource("Secondary"),Content=english?"Next ›":"Далее ›",IsEnabled=files.Length>1};AutomationProperties.SetAutomationId(next,"GalleryNext");next.Click+=(s,e)=>Step(1);nav.Children.Add(next);
            var hint=new TextBlock{Text=english?"← → to browse":"← → для переключения",VerticalAlignment=VerticalAlignment.Center,Foreground=Brushes.DimGray,FontSize=12};DockPanel.SetDock(hint,Dock.Right);toolbar.Children.Add(hint);toolbar.Children.Add(caption);
            KeyDown+=(s,e)=>{if(e.Key==Key.Escape)Close();else if(e.Key==Key.Left){Step(-1);e.Handled=true;}else if(e.Key==Key.Right){Step(1);e.Handled=true;}};
            Load();
        }
        public void Step(int delta){if(files.Length==0)return;Index=(Index+delta+files.Length)%files.Length;Load();}
        async void Load(){if(files.Length==0)return;int token=++revision;string file=files[Index];Counter.Text=(Index+1)+" / "+files.Length;caption.Text=System.Text.RegularExpressions.Regex.Replace(Path.GetFileNameWithoutExtension(file),@"^\d+\s*","");try{
            var bitmap=await Task.Run(()=>WindowController.Bitmap(file));if(token!=revision)return;Picture.Source=bitmap;Picture.BeginAnimation(UIElement.OpacityProperty,null);Picture.Opacity=noMotion?1:0.35;Picture.BeginAnimation(UIElement.OpacityProperty,new DoubleAnimation(1,TimeSpan.FromMilliseconds(noMotion?0:180)),HandoffBehavior.SnapshotAndReplace);
        }catch(Exception e){caption.Text=e.Message;}}
    }
}
