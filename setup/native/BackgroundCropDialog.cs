using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace FortsSetup {
    public sealed class BackgroundCropDialog : Window {
        double zoom=1,horizontal=0.5,vertical=0.5;
        public readonly Button ZoomIn,ZoomOut;
        public readonly Border Viewport;
        public Int32Rect Frame {get;private set;}
        public bool UseOriginal {get;private set;}
        readonly BitmapSource original;
        readonly Image image=new Image{Stretch=Stretch.Uniform};
        readonly Canvas overlay=new Canvas{Width=576,Height=324,IsHitTestVisible=false};
        readonly System.Windows.Shapes.Path shade=new System.Windows.Shapes.Path{Fill=new SolidColorBrush(Color.FromArgb(155,0,0,0))};
        readonly Rectangle outline=new Rectangle{Stroke=Brushes.White,StrokeThickness=2,RadiusX=2,RadiusY=2};
        readonly Ellipse[] handles={new Ellipse(),new Ellipse(),new Ellipse(),new Ellipse()};
        Rect imageBounds,selectionBounds;int resizeCorner=-1;
        readonly TextBlock zoomText=new TextBlock{MinWidth=42,TextAlignment=TextAlignment.Center,VerticalAlignment=VerticalAlignment.Center};
        Point last;bool dragging;
        string T(string ru,string en){return english?en:ru;}
        readonly bool english;
        public BackgroundCropDialog(Window owner,byte[] data,bool en,Int32Rect? previous=null){
            english=en;original=BackgroundImage.Preview(data);Width=720;Height=620;
            Grid body;Button close;Sheet.Frame(this,owner,T("Обрезка изображения","Crop image"),T("Перетащи рамку или потяни за угол. Пропорции кадра — 16:9.","Drag the frame or a corner. The frame stays at 16:9."),T("Закрыть","Close"),out body,out close);
            close.Click+=(s,e)=>Close();
            body.RowDefinitions.Add(new RowDefinition{Height=new GridLength(1,GridUnitType.Star)});body.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});
            var scroll=new ScrollViewer{VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled};body.Children.Add(scroll);
            var content=new StackPanel();scroll.Content=content;
            var stage=new Grid();stage.Children.Add(image);stage.Children.Add(overlay);image.Source=original;
            overlay.Children.Add(shade);overlay.Children.Add(outline);foreach(var handle in handles){handle.Width=14;handle.Height=14;handle.Fill=Brushes.White;handle.Stroke=new SolidColorBrush(Color.FromRgb(22,115,255));handle.StrokeThickness=2;overlay.Children.Add(handle);}
            double viewScale=Math.Min(576.0/original.PixelWidth,324.0/original.PixelHeight);double imageWidth=original.PixelWidth*viewScale,imageHeight=original.PixelHeight*viewScale;
            imageBounds=new Rect((576-imageWidth)/2,(324-imageHeight)/2,imageWidth,imageHeight);
            Viewport=new Border{Width=576,Height=324,Background=Brushes.Black,Child=stage,ClipToBounds=true,CornerRadius=new CornerRadius(12),Clip=new RectangleGeometry(new Rect(0,0,576,324),12,12),Cursor=Cursors.SizeAll,Focusable=true};
            AutomationProperties.SetName(Viewport,T("Кадр изображения. Стрелки перемещают рамку.","Image frame. Arrow keys move the frame."));
            content.Children.Add(new Viewbox{Stretch=Stretch.Uniform,StretchDirection=StretchDirection.DownOnly,Child=Viewport,HorizontalAlignment=HorizontalAlignment.Center});
            if(previous.HasValue){var p=previous.Value;double fit=Math.Min(original.PixelWidth,original.PixelHeight*16.0/9);zoom=Math.Max(1,Math.Min(4,fit/p.Width));horizontal=original.PixelWidth==p.Width?0.5:p.X/(double)(original.PixelWidth-p.Width);vertical=original.PixelHeight==p.Height?0.5:p.Y/(double)(original.PixelHeight-p.Height);}
            var controls=new Grid{Margin=new Thickness(0,18,0,0)};controls.ColumnDefinitions.Add(new ColumnDefinition());controls.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});controls.ColumnDefinitions.Add(new ColumnDefinition());content.Children.Add(controls);
            var zoomGroup=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Center};Grid.SetColumn(zoomGroup,1);controls.Children.Add(zoomGroup);
            ZoomOut=SmallButton(owner,"−",T("Уменьшить масштаб","Zoom out"));zoomGroup.Children.Add(ZoomOut);zoomGroup.Children.Add(zoomText);
            ZoomIn=SmallButton(owner,"+",T("Увеличить масштаб","Zoom in"));zoomGroup.Children.Add(ZoomIn);
            ZoomOut.Click+=(s,e)=>SetZoom(zoom/1.2);ZoomIn.Click+=(s,e)=>SetZoom(zoom*1.2);
            var arrows=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right};Grid.SetColumn(arrows,2);controls.Children.Add(arrows);
            foreach(var item in new[]{Tuple.Create("←",T("Сдвинуть влево","Move left"),-1,0),Tuple.Create("↑",T("Сдвинуть вверх","Move up"),0,-1),Tuple.Create("↓",T("Сдвинуть вниз","Move down"),0,1),Tuple.Create("→",T("Сдвинуть вправо","Move right"),1,0)}){
                var direction=item;var button=SmallButton(owner,item.Item1,item.Item2);button.Margin=new Thickness(3,0,0,0);button.Click+=(s,e)=>MoveFrame(direction.Item3*30,direction.Item4*30);arrows.Children.Add(button);
            }
            Viewport.MouseLeftButtonDown+=(s,e)=>{last=e.GetPosition(Viewport);resizeCorner=HitCorner(last);if(resizeCorner<0&&!selectionBounds.Contains(last))return;dragging=Viewport.CaptureMouse();Viewport.Focus();e.Handled=true;};
            Viewport.MouseMove+=(s,e)=>{if(!dragging)return;Point p=e.GetPosition(Viewport);if(resizeCorner>=0)ResizeFrame(resizeCorner,p);else MoveFrame(p.X-last.X,p.Y-last.Y);last=p;};
            Viewport.MouseLeftButtonUp+=(s,e)=>{dragging=false;Viewport.ReleaseMouseCapture();};Viewport.LostMouseCapture+=(s,e)=>dragging=false;
            Viewport.MouseWheel+=(s,e)=>{SetZoom(zoom*(e.Delta>0?1.1:1/1.1));e.Handled=true;};
            Viewport.KeyDown+=(s,e)=>{double step=12;if(e.Key==Key.Left)MoveFrame(-step,0);else if(e.Key==Key.Right)MoveFrame(step,0);else if(e.Key==Key.Up)MoveFrame(0,-step);else if(e.Key==Key.Down)MoveFrame(0,step);else return;e.Handled=true;};
            var footer=new DockPanel{Margin=new Thickness(0,14,0,0)};Grid.SetRow(footer,1);body.Children.Add(footer);
            var full=new Button{Style=Sheet.Style(owner,"Secondary"),Content=T("Оставить целиком","Keep full image"),HorizontalAlignment=HorizontalAlignment.Left};AutomationProperties.SetAutomationId(full,"KeepFullImage");footer.Children.Add(full);
            var actions=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right};DockPanel.SetDock(actions,Dock.Right);footer.Children.Add(actions);
            var cancel=new Button{Style=Sheet.Style(owner,"Secondary"),Content=T("Отмена","Cancel"),IsCancel=true,Margin=new Thickness(0,0,10,0)};actions.Children.Add(cancel);
            var apply=new Button{Style=Sheet.Style(owner,"Primary"),Content=T("Применить кадр","Apply crop"),IsDefault=true};AutomationProperties.SetAutomationId(apply,"ApplyCrop");actions.Children.Add(apply);
            full.Click+=(s,e)=>{UseOriginal=true;DialogResult=true;};apply.Click+=(s,e)=>{UseOriginal=false;DialogResult=true;};
            RefreshFrame();
        }
        Button SmallButton(Window owner,string text,string label){
            var button=new Button{Style=Sheet.Style(owner,"Secondary"),Content=text,MinWidth=36,Height=34,Padding=new Thickness(8,0,8,0),ToolTip=label};AutomationProperties.SetName(button,label);return button;
        }
        internal void SetZoom(double value){zoom=Math.Max(1,Math.Min(4,value));RefreshFrame();}
        void RefreshFrame(){
            Frame=BackgroundImage.CropFrame(original.PixelWidth,original.PixelHeight,zoom,horizontal,vertical);
            selectionBounds=new Rect(imageBounds.X+Frame.X*imageBounds.Width/original.PixelWidth,imageBounds.Y+Frame.Y*imageBounds.Height/original.PixelHeight,Frame.Width*imageBounds.Width/original.PixelWidth,Frame.Height*imageBounds.Height/original.PixelHeight);
            var geometry=new GeometryGroup{FillRule=FillRule.EvenOdd};geometry.Children.Add(new RectangleGeometry(imageBounds));geometry.Children.Add(new RectangleGeometry(selectionBounds));shade.Data=geometry;
            Canvas.SetLeft(outline,selectionBounds.X);Canvas.SetTop(outline,selectionBounds.Y);outline.Width=selectionBounds.Width;outline.Height=selectionBounds.Height;
            var corners=new[]{selectionBounds.TopLeft,selectionBounds.TopRight,selectionBounds.BottomLeft,selectionBounds.BottomRight};for(int i=0;i<4;i++){Canvas.SetLeft(handles[i],corners[i].X-7);Canvas.SetTop(handles[i],corners[i].Y-7);}
            zoomText.Text=String.Format("{0:0}%",zoom*100);
        }
        int HitCorner(Point point){var corners=new[]{selectionBounds.TopLeft,selectionBounds.TopRight,selectionBounds.BottomLeft,selectionBounds.BottomRight};for(int i=0;i<corners.Length;i++)if(Math.Abs(point.X-corners[i].X)<=14&&Math.Abs(point.Y-corners[i].Y)<=14)return i;return -1;}
        internal void ResizeFrame(int corner,Point point){
            bool left=corner==0||corner==2,top=corner==0||corner==1;
            double anchorX=left?selectionBounds.Right:selectionBounds.Left,anchorY=top?selectionBounds.Bottom:selectionBounds.Top;
            double dx=left?anchorX-point.X:point.X-anchorX,dy=top?anchorY-point.Y:point.Y-anchorY;
            double requested=Math.Max(1,(dx+dy*16.0/9)/2);
            double fitWidth=Math.Min(original.PixelWidth,original.PixelHeight*16.0/9)*imageBounds.Width/original.PixelWidth;
            double boundedWidth=Math.Min(requested,Math.Min(left?anchorX-imageBounds.Left:imageBounds.Right-anchorX,(top?anchorY-imageBounds.Top:imageBounds.Bottom-anchorY)*16.0/9));
            zoom=Math.Max(1,Math.Min(4,fitWidth/Math.Max(1,boundedWidth)));
            var size=BackgroundImage.CropFrame(original.PixelWidth,original.PixelHeight,zoom,0,0);
            double x=(anchorX-imageBounds.X)*original.PixelWidth/imageBounds.Width-(left?size.Width:0);
            double y=(anchorY-imageBounds.Y)*original.PixelHeight/imageBounds.Height-(top?size.Height:0);
            horizontal=original.PixelWidth==size.Width?0.5:Math.Max(0,Math.Min(1,x/(original.PixelWidth-size.Width)));
            vertical=original.PixelHeight==size.Height?0.5:Math.Max(0,Math.Min(1,y/(original.PixelHeight-size.Height)));
            RefreshFrame();
        }
        internal void MoveFrame(double dx,double dy){
            int availableX=original.PixelWidth-Frame.Width,availableY=original.PixelHeight-Frame.Height;
            if(availableX>0)horizontal=Math.Max(0,Math.Min(1,horizontal+dx/imageBounds.Width*original.PixelWidth/availableX));
            if(availableY>0)vertical=Math.Max(0,Math.Min(1,vertical+dy/imageBounds.Height*original.PixelHeight/availableY));
            RefreshFrame();
        }
    }
}
