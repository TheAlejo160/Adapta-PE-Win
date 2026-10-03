using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using AdaptaPE.Windows;

namespace AdaptaPE
{
    // Ventana pequeña sin foco ni entrada: permite ver dwell fuera de los ajustes.
    sealed class DwellWindow:Window
    {
        readonly System.Windows.Controls.Grid drawing=new System.Windows.Controls.Grid();
        readonly Path arc=new Path{Stroke=Theme.Blue,StrokeThickness=5};
        readonly System.Windows.Controls.TextBlock label=new System.Windows.Controls.TextBlock{FontSize=13,FontWeight=FontWeights.Bold,Foreground=Brushes.White,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center};
        public DwellWindow(){
            Width=64;Height=64;WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.NoResize;AllowsTransparency=true;Background=Brushes.Transparent;ShowInTaskbar=false;ShowActivated=false;Topmost=true;IsHitTestVisible=false;
            drawing.Children.Add(new Ellipse{Fill=new SolidColorBrush(Color.FromArgb(230,12,24,45)),Stroke=Brushes.White,StrokeThickness=1,Margin=new Thickness(3)});drawing.Children.Add(arc);drawing.Children.Add(label);Content=drawing;
            SourceInitialized+=(s,e)=>{var handle=new WindowInteropHelper(this).Handle;Native.SetWindowLongPtr(handle,-20,new IntPtr(Native.GetWindowLongPtr(handle,-20).ToInt64()|0x08000000|0x20));};
        }
        public void Update(double progress){
            if(progress<=0){Hide();return;}progress=Math.Min(1,progress);label.Text=((int)(progress*100))+"%";arc.Stroke=progress>=1?Brushes.LimeGreen:Brushes.DeepSkyBlue;
            var geometry=new StreamGeometry();double angle=Math.Min(progress,.9999)*Math.PI*2-Math.PI/2;
            using(var context=geometry.Open()){context.BeginFigure(new Point(32,5),false,false);context.ArcTo(new Point(32+27*Math.Cos(angle),32+27*Math.Sin(angle)),new Size(27,27),0,progress>.5,SweepDirection.Clockwise,true,false);}geometry.Freeze();arc.Data=geometry;
            Show();Native.Point cursor;if(!Native.GetCursorPos(out cursor))return;var area=System.Windows.Forms.Screen.FromPoint(new System.Drawing.Point(cursor.X,cursor.Y)).WorkingArea;var handle=new WindowInteropHelper(this).Handle;Native.Rect bounds;Native.GetWindowRect(handle,out bounds);int width=bounds.Right-bounds.Left,height=bounds.Bottom-bounds.Top;
            int x=cursor.X+22,y=cursor.Y+22;if(x+width>area.Right)x=cursor.X-width-22;if(y+height>area.Bottom)y=cursor.Y-height-22;
            Native.SetWindowPos(handle,new IntPtr(-1),Math.Max(area.Left,x),Math.Max(area.Top,y),width,height,0x10);
        }
    }
}
