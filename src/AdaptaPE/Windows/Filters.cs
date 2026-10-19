using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using AdaptaPE.Core;
using Forms=System.Windows.Forms;

namespace AdaptaPE.Windows
{
    public sealed class Filters:IDisposable
    {
        [StructLayout(LayoutKind.Sequential)]struct Effect {[MarshalAs(UnmanagedType.ByValArray,SizeConst=25)]public float[] Values;}
        [DllImport("Magnification.dll")]static extern bool MagInitialize();
        [DllImport("Magnification.dll")]static extern bool MagUninitialize();
        [DllImport("Magnification.dll")]static extern bool MagSetFullscreenColorEffect(ref Effect effect);
        [DllImport("Magnification.dll")]static extern bool MagGetFullscreenColorEffect(out Effect effect);
        [DllImport("Magnification.dll")]static extern bool MagSetColorEffect(IntPtr window,ref Effect effect);
        [DllImport("Magnification.dll")]static extern bool MagSetWindowSource(IntPtr window,Native.Rect rect);
        [DllImport("Magnification.dll")]static extern bool MagSetWindowFilterList(IntPtr window,uint mode,int count,IntPtr[] windows);
        [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern IntPtr CreateWindowEx(int ex,string cls,string title,int style,int x,int y,int width,int height,IntPtr parent,IntPtr menu,IntPtr instance,IntPtr param);
        readonly Mutex owner=new Mutex(false,@"Local\AdaptaPE.ColorFilter");
        bool owns,initialized;Effect previous,applied;LegacyWindow legacy;
        string recovery=Path.Combine(Preferences.Folder,"filter-recovery.bin");
        public int Selected {get;private set;}
        bool Global {get{return Environment.OSVersion.Version>=new Version(6,2);}}
        static bool Equal(Effect a,Effect b){return a.Values!=null&&b.Values!=null&&a.Values.Zip(b.Values,(x,y)=>Math.Abs(x-y)<.0001).All(v=>v);}
        public void Recover() {
            if(!Global||!File.Exists(recovery)||initialized)return;
            bool taken=false;
            try{try{taken=owner.WaitOne(0);}catch(AbandonedMutexException){taken=true;}
                if(!taken)return;
                if(!MagInitialize())throw new InvalidOperationException("No se pudo comprobar recuperación de filtro anterior.");
                try{Effect current;if(!MagGetFullscreenColorEffect(out current))throw new InvalidOperationException("No se pudo consultar filtro para recuperación.");
                    using(var reader=new BinaryReader(File.OpenRead(recovery))){var old=new Effect{Values=new float[25]};var last=new Effect{Values=new float[25]};
                        for(int i=0;i<25;i++)old.Values[i]=reader.ReadSingle();for(int i=0;i<25;i++)last.Values[i]=reader.ReadSingle();
                        if(Equal(current,last)&&!MagSetFullscreenColorEffect(ref old))throw new InvalidOperationException("No se pudo recuperar el filtro anterior.");
                    }File.Delete(recovery);
                }finally{MagUninitialize();}
            }finally{if(taken)owner.ReleaseMutex();}
        }
        public void Apply(int index) {
            if(index==0){Stop();return;}if(index<0||index>7)throw new ArgumentException("Filtro desconocido.");
            if(!initialized) {
                try{owns=owner.WaitOne(0);}catch(AbandonedMutexException){owns=true;}
                if(!owns)throw new InvalidOperationException("Otro Adapta PE utiliza los filtros.");
                if(!Environment.Is64BitProcess&&Environment.Is64BitOperatingSystem){Release();throw new InvalidOperationException("Filtro requiere aplicación x64 en Windows x64.");}
                if(!MagInitialize()){Release();throw new InvalidOperationException("Windows no pudo iniciar los filtros.");}initialized=true;
                if(Global) {
                    if(!MagGetFullscreenColorEffect(out previous)){Stop();throw new InvalidOperationException("No se pudo consultar el filtro actual de Windows.");}
                    if(File.Exists(recovery)) {
                        try{using(var reader=new BinaryReader(File.OpenRead(recovery))) {
                            var old=new Effect{Values=new float[25]};var last=new Effect{Values=new float[25]};
                            for(int i=0;i<25;i++)old.Values[i]=reader.ReadSingle();for(int i=0;i<25;i++)last.Values[i]=reader.ReadSingle();
                            if(Equal(previous,last)&&MagSetFullscreenColorEffect(ref old))previous=old;
                        }}catch(IOException){}
                    }
                }
            }
            var effect=new Effect{Values=ColorMatrices.Native(index)};
            if(Global) {
                if(Selected!=0){Effect current;if(!MagGetFullscreenColorEffect(out current)||!Equal(current,applied))throw new InvalidOperationException("Otra aplicación cambió el filtro. Retira el filtro de Adapta y vuelve a intentar.");}
                Directory.CreateDirectory(Preferences.Folder);
                using(var w=new BinaryWriter(File.Create(recovery))){foreach(float f in previous.Values)w.Write(f);foreach(float f in effect.Values)w.Write(f);}
                if(!MagSetFullscreenColorEffect(ref effect)){Stop();throw new InvalidOperationException("Windows rechazó el filtro global.");}applied=effect;
            }else {
                if(legacy==null){legacy=new LegacyWindow();legacy.Show();}
                if(!MagSetColorEffect(legacy.Magnifier,ref effect)){Stop();throw new InvalidOperationException("Filtro local no disponible en este equipo Windows 7.");}
            }
            Selected=index;
        }
        public void DisplayChanged(){if(legacy!=null)legacy.ResizeDisplay();}
        public void Stop() {
            if(legacy!=null){legacy.Close();legacy.Dispose();legacy=null;}
            if(initialized&&Global&&Selected!=0){Effect current;if(MagGetFullscreenColorEffect(out current)&&Equal(current,applied)) {
                if(!MagSetFullscreenColorEffect(ref previous))throw new InvalidOperationException("No se pudo restaurar el filtro. Vuelve a intentar Detener todo.");
            }}
            if(initialized){MagUninitialize();initialized=false;}Selected=0;if(File.Exists(recovery))File.Delete(recovery);Release();
        }
        void Release(){if(owns){owner.ReleaseMutex();owns=false;}}
        public void Dispose(){Stop();owner.Dispose();}
        sealed class LegacyWindow:Forms.Form {
            public IntPtr Magnifier;readonly Forms.Timer timer=new Forms.Timer{Interval=100};
            protected override bool ShowWithoutActivation {get{return true;}}
            protected override Forms.CreateParams CreateParams {get{var p=base.CreateParams;p.ExStyle|=0x08000000|0x20;p.ExStyle|=0x80000;return p;}}
            public LegacyWindow(){FormBorderStyle=Forms.FormBorderStyle.None;ShowInTaskbar=false;TopMost=true;Bounds=Forms.SystemInformation.VirtualScreen;BackColor=System.Drawing.Color.Black;Opacity=.999;}
            protected override void OnShown(EventArgs e){base.OnShown(e);Magnifier=CreateWindowEx(0,"Magnifier","",0x40000000|0x10000000,0,0,Width,Height,Handle,IntPtr.Zero,IntPtr.Zero,IntPtr.Zero);if(Magnifier==IntPtr.Zero)throw new InvalidOperationException("No se pudo crear filtro Windows 7.");MagSetWindowFilterList(Magnifier,0,1,new[]{Handle});timer.Tick+=(s,a)=>{var b=Forms.SystemInformation.VirtualScreen;MagSetWindowSource(Magnifier,new Native.Rect{Left=b.Left,Top=b.Top,Right=b.Right,Bottom=b.Bottom});};timer.Start();}
            public void ResizeDisplay(){Bounds=Forms.SystemInformation.VirtualScreen;if(Magnifier!=IntPtr.Zero)Native.SetWindowPos(Magnifier,IntPtr.Zero,0,0,Width,Height,0x14);}
            protected override void Dispose(bool disposing){if(disposing)timer.Dispose();base.Dispose(disposing);}
        }
    }
}
