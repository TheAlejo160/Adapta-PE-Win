using System;
using System.IO;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using AdaptaPE.Core;
using Forms=System.Windows.Forms;

namespace AdaptaPE.Windows
{
    public sealed class Mascot:IDisposable {
        readonly Preferences settings;readonly Action save;readonly DispatcherTimer timer=new DispatcherTimer(DispatcherPriority.Background);
        readonly DispatcherTimer inactivity=new DispatcherTimer(DispatcherPriority.Background){Interval=TimeSpan.FromSeconds(90)};
        MascotActivity activity=new MascotActivity();FloatingWindow panel;
        public Action LayoutChanged;
        Window sprite,handle;Image image;BitmapSource[] frames;MascotPlayback playback;
        bool sleeping,suspended,clearing,disposed;string speechState="talk";System.Drawing.Point drag;
        public Action<string> Status;
        public Mascot(Preferences settings,Action save){this.settings=settings;this.save=save;SystemParameters.StaticPropertyChanged+=AnimationChanged;timer.Tick+=(s,e)=>{timer.Stop();Draw();Schedule();};inactivity.Tick+=(s,e)=>{inactivity.Stop();if(playback!=null&&activity.ShouldSleep(Now)){sleeping=true;playback.Set("sleep");Draw();Schedule();Report(settings.Mascot+" · descansando por inactividad");}};}
        public void Choose(string name){
            if(disposed)return;Clear();settings.Mascot=MascotCommand.Names.Contains(name)?name:"ninguna";sleeping=false;bool speaking=activity.Speaking;activity=new MascotActivity{Speaking=speaking};
            if(settings.Mascot=="ninguna"){Report("Sin mascota");if(LayoutChanged!=null)LayoutChanged();return;}
            try{
                string prefix=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"mascotas","mascota-"+settings.Mascot);
                MascotAtlas atlas;using(var file=File.OpenRead(prefix+".json"))atlas=(MascotAtlas)new DataContractJsonSerializer(typeof(MascotAtlas)).ReadObject(file);
                var bitmap=new BitmapImage();bitmap.BeginInit();bitmap.CacheOption=BitmapCacheOption.OnLoad;bitmap.UriSource=new Uri(prefix+".png");bitmap.EndInit();bitmap.Freeze();
                if(atlas==null||!atlas.Valid(bitmap.PixelWidth,bitmap.PixelHeight))throw new InvalidDataException("Atlas inválido.");
                frames=atlas.frames.Select(f=>{var crop=new CroppedBitmap(bitmap,new Int32Rect(f.x,f.y,f.w,f.h));crop.Freeze();return (BitmapSource)crop;}).ToArray();playback=new MascotPlayback(atlas);
                image=new Image{Stretch=Stretch.Fill};sprite=Panel(96,112);sprite.Content=image;sprite.IsHitTestVisible=false;
                sprite.SourceInitialized+=(s,e)=>{var h=new WindowInteropHelper(sprite).Handle;Native.SetWindowLongPtr(h,-20,new IntPtr(Native.GetWindowLongPtr(h,-20).ToInt64()|0x20|0x08000000));};
                handle=Panel(96,44);handle.SourceInitialized+=(s,e)=>HwndSource.FromHwnd(new WindowInteropHelper(handle).Handle).AddHook(DisplayMessage);var button=new Button{Content=settings.Mascot,MinHeight=44,ToolTip="Arrastra a otro monitor o usa las flechas"};AutomationProperties.SetName(button,"Mover "+settings.Mascot+": arrastra o usa las flechas");handle.Content=button;
                button.PreviewMouseLeftButtonDown+=(s,e)=>{drag=Forms.Cursor.Position;button.CaptureMouse();button.Focus();e.Handled=true;};
                button.PreviewMouseMove+=(s,e)=>{if(!button.IsMouseCaptured)return;var p=Forms.Cursor.Position;Move(p.X-drag.X,p.Y-drag.Y);drag=p;};
                button.PreviewMouseLeftButtonUp+=(s,e)=>{button.ReleaseMouseCapture();SavePosition();e.Handled=true;};
                button.PreviewKeyDown+=(s,e)=>{if(e.Key==Key.Escape){Application.Current.Windows.OfType<AdaptaPE.MainWindow>().FirstOrDefault()?.Activate();e.Handled=true;return;}int x=e.Key==Key.Left?-24:e.Key==Key.Right?24:0,y=e.Key==Key.Up?-24:e.Key==Key.Down?24:0;if(x!=0||y!=0){Move(x,y);SavePosition();e.Handled=true;}};
                if(!suspended){sprite.Show();handle.Show();}Position();Draw();Schedule();Touch();if(activity.Speaking)React(speechState);if(LayoutChanged!=null)LayoutChanged();Report(settings.Mascot+" visible · arrastra su nombre o usa las flechas");
            }catch(Exception error){Clear();settings.Mascot="ninguna";Report("No se cargó la mascota: "+error.Message);}
        }
        Window Panel(int width,int height){var panel=new Window{Width=width,Height=height,WindowStyle=WindowStyle.None,ResizeMode=ResizeMode.NoResize,AllowsTransparency=true,Background=Brushes.Transparent,ShowInTaskbar=false,ShowActivated=false,Topmost=true};panel.Closing+=(s,e)=>{if(!clearing){e.Cancel=true;panel.Dispatcher.BeginInvoke(new Action(()=>{Choose("ninguna");save();}));}};return panel;}
        internal void Attach(FloatingWindow value){panel=settings.MascotLinked&&value!=null&&value.IsVisible?value:null;Position();}
        public void Link(bool linked){settings.MascotLinked=linked;if(LayoutChanged!=null)LayoutChanged();save();Report(linked?"Mascota unida al panel":"Mascota independiente");}
        public void Position(){if(handle==null)return;if(panel!=null){Native.Rect r;Native.GetWindowRect(new WindowInteropHelper(panel).Handle,out r);Place((r.Left+r.Right-WidthPixels)/2.0,r.Top-HeightPixels-(int)(8*Scale),Forms.Screen.FromHandle(new WindowInteropHelper(panel).Handle));return;}var screen=Forms.Screen.AllScreens.FirstOrDefault(s=>s.DeviceName==settings.MascotMonitor)??Forms.Screen.PrimaryScreen;var b=screen.WorkingArea;Place(b.Left+settings.MascotX*Math.Max(0,b.Width-WidthPixels),b.Top+settings.MascotY*Math.Max(0,b.Height-HeightPixels),screen);}
        double Scale {get{var source=handle==null?null:PresentationSource.FromVisual(handle);return source!=null&&source.CompositionTarget!=null?source.CompositionTarget.TransformToDevice.M11:1;}}
        int WidthPixels {get{return (int)Math.Round(96*Scale);}}int LabelPixels {get{return (int)Math.Round(44*Scale);}}int HeightPixels {get{return (int)Math.Round(156*Scale);}}
        IntPtr DisplayMessage(IntPtr h,int message,IntPtr w,IntPtr l,ref bool handled){if(message==0x2E0)Application.Current.Dispatcher.BeginInvoke(new Action(()=>{if(handle!=null)Move(0,0);}));return IntPtr.Zero;}
        void Move(int dx,int dy){Touch();if(panel!=null){panel.MoveBy(dx,dy);return;}if(handle==null)return;Native.Rect rect;Native.GetWindowRect(new WindowInteropHelper(handle).Handle,out rect);double x=rect.Left+dx,y=rect.Top+dy;Place(x,y,Forms.Screen.FromPoint(Mouse.LeftButton==MouseButtonState.Pressed?Forms.Cursor.Position:new System.Drawing.Point((int)x+WidthPixels/2,(int)y+LabelPixels/2)));}
        void Place(double x,double y,Forms.Screen screen){var b=screen.WorkingArea;int width=WidthPixels,label=LabelPixels,height=HeightPixels;x=Math.Max(b.Left,Math.Min(b.Right-width,x));y=Math.Max(b.Top,Math.Min(b.Bottom-height,y));new WindowInteropHelper(handle).EnsureHandle();new WindowInteropHelper(sprite).EnsureHandle();Native.SetWindowPos(new WindowInteropHelper(handle).Handle,new IntPtr(-1),(int)x,(int)y,width,label,0x10);Native.SetWindowPos(new WindowInteropHelper(sprite).Handle,new IntPtr(-1),(int)x,(int)y+label,width,height-label,0x10);}
        void SavePosition(){if(panel!=null){panel.SavePosition();return;}Native.Rect rect;Native.GetWindowRect(new WindowInteropHelper(handle).Handle,out rect);var screen=Forms.Screen.FromPoint(new System.Drawing.Point(rect.Left+WidthPixels/2,rect.Top+LabelPixels/2));var b=screen.WorkingArea;settings.MascotMonitor=screen.DeviceName;settings.MascotX=(rect.Left-b.Left)/(double)Math.Max(1,b.Width-WidthPixels);settings.MascotY=(rect.Top-b.Top)/(double)Math.Max(1,b.Height-HeightPixels);save();}
        static double Now {get{return System.Diagnostics.Stopwatch.GetTimestamp()/(double)System.Diagnostics.Stopwatch.Frequency;}}
        public void Touch(){if(sleeping&&!activity.ManualSleep){sleeping=false;playback.Set("wake");Draw();Schedule();}activity.Touch(Now);inactivity.Stop();if(playback!=null&&!suspended&&!activity.ManualSleep&&!activity.Speaking)inactivity.Start();}
        public void PrepareSpeech(bool reading){speechState=reading?"read":"talk";if(!reading)React("laptop");}
        public void Speaking(bool active){activity.Speaking=active;React(active?speechState:"idle");Touch();}
        public void React(string state){if(activity.ManualSleep||playback==null)return;if(state!="idle"){sleeping=false;Touch();}if(!sleeping&&playback.State!=state){playback.Set(state);Draw();Schedule();Report(settings.Mascot+" · "+(state=="read"?"leyendo":state=="talk"?"hablando":state=="phone"?"escuchando":state=="laptop"?"realizando tu acción":"visible"));}}
        public void Gesture(string state){if(playback==null){Report("Elige una mascota primero");return;}if(state=="join"||state=="separate"){Link(state=="join");return;}if(activity.ManualSleep&&state!="wake"&&state!="sleep"){Report(settings.Mascot+" duerme · pulsa Despertar");return;}activity.ManualSleep=state=="sleep";sleeping=activity.ManualSleep;Touch();playback.Set(state);Draw();Schedule();Report(settings.Mascot+" · "+(state=="sleep"?"durmiendo":state=="wake"?"despertando":"saludando"));}
        public void FocusHandle(){if(handle==null){Report("Elige una mascota primero");return;}handle.Activate();((Button)handle.Content).Focus();}
        public void NextScreen(){if(panel!=null){panel.NextScreen();return;}if(handle==null){Report("Elige una mascota primero");return;}var screens=Forms.Screen.AllScreens;if(screens.Length<2){Report("Sólo hay una pantalla conectada");return;}int index=Array.FindIndex(screens,s=>s.DeviceName==settings.MascotMonitor);settings.MascotMonitor=screens[(index+1)%screens.Length].DeviceName;Position();save();Report(settings.Mascot+" · "+settings.MascotMonitor);}
        public void Suspend(bool value){suspended=value;inactivity.Stop();if(!value)Touch();if(sprite!=null){if(value){sprite.Hide();handle.Hide();}else{sprite.Show();handle.Show();Position();}}Schedule();}
        void Draw(){if(playback==null||image==null)return;string previous=playback.State;playback.Advance();if(previous!="idle"&&playback.State=="idle")Report(settings.Mascot+" visible");int index=playback.Frame;if(playback.State=="idle"&&handle!=null){Native.Rect rect;Native.GetWindowRect(new WindowInteropHelper(sprite).Handle,out rect);var p=Forms.Cursor.Position;double dx=p.X-(rect.Left+(rect.Right-rect.Left)/2),dy=p.Y-(rect.Top+(rect.Bottom-rect.Top)/2);if(dx*dx+dy*dy<250000){int x=dx< -40?0:dx>40?2:1,y=dy< -40?0:dy>40?2:1;index=playback.Atlas.Find("look").loop[y*3+x];}}if(!SystemParameters.ClientAreaAnimation)index=playback.Atlas.Find(playback.State).loop.FirstOrDefault();image.Source=frames[index];}
        void Schedule(){timer.Stop();if(playback==null)return;if(!SystemParameters.ClientAreaAnimation){if(playback.State!="idle"&&playback.State!="sleep"&&playback.State!="read"&&playback.State!="talk"){playback.Set("idle");Draw();}return;}if(suspended||playback.Resting)return;timer.Interval=TimeSpan.FromMilliseconds(playback.Atlas.frames[playback.Frame].duration);timer.Start();}
        void Report(string text){if(Status!=null)Status(text);}
        void AnimationChanged(object sender,PropertyChangedEventArgs e){if(e.PropertyName=="ClientAreaAnimation"&&!disposed)timer.Dispatcher.BeginInvoke(new Action(Schedule));}
        void Clear(){clearing=true;inactivity.Stop();timer.Stop();if(sprite!=null)sprite.Close();if(handle!=null)handle.Close();sprite=null;handle=null;image=null;frames=null;playback=null;clearing=false;}
        public void Dispose(){disposed=true;SystemParameters.StaticPropertyChanged-=AnimationChanged;Clear();}
    }
}
