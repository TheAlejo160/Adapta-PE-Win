using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using AdaptaPE.Core;

namespace AdaptaPE.Windows
{
    internal sealed class WaveInput:LiveAudioStream
    {
        [StructLayout(LayoutKind.Sequential)]struct WaveFormat {public ushort Tag,Channels;public uint Rate,BytesPerSec;public ushort Align,Bits,Extra;}
        [StructLayout(LayoutKind.Sequential)]struct Header {public IntPtr Data;public uint Length,Recorded;public IntPtr User;public uint Flags,Loops;public IntPtr Next,Reserved;}
        [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]struct Caps {public ushort Manufacturer,Product;public uint Version;[MarshalAs(UnmanagedType.ByValTStr,SizeConst=32)]public string Name;public uint Formats;public ushort Channels,Reserved;}
        [DllImport("winmm.dll")]static extern uint waveInOpen(out IntPtr device,uint id,ref WaveFormat format,IntPtr ready,IntPtr instance,uint flags);
        [DllImport("winmm.dll")]static extern uint waveInPrepareHeader(IntPtr device,IntPtr header,uint size);
        [DllImport("winmm.dll")]static extern uint waveInUnprepareHeader(IntPtr device,IntPtr header,uint size);
        [DllImport("winmm.dll")]static extern uint waveInAddBuffer(IntPtr device,IntPtr header,uint size);
        [DllImport("winmm.dll")]static extern uint waveInStart(IntPtr device);
        [DllImport("winmm.dll")]static extern uint waveInStop(IntPtr device);
        [DllImport("winmm.dll")]static extern uint waveInReset(IntPtr device);
        [DllImport("winmm.dll")]static extern uint waveInClose(IntPtr device);
        [DllImport("winmm.dll")]static extern uint waveInGetNumDevs();
        [DllImport("winmm.dll",CharSet=CharSet.Unicode)]static extern uint waveInGetDevCaps(UIntPtr id,out Caps caps,uint size);
        readonly object sync=new object();readonly byte[] ring=new byte[32768];readonly List<IntPtr> headers=new List<IntPtr>();readonly List<GCHandle> pins=new List<GCHandle>();
        readonly AutoResetEvent ready=new AutoResetEvent(false);Thread capture;IntPtr device;volatile bool closed;int head,count,nextBuffer;long lastAudio=Stopwatch.GetTimestamp();double lastLevel;Exception captureError;
        public Action<double> Level;
        public static string[] Devices(){var names=new List<string>{"Micrófono predeterminado de Windows"};for(uint i=0;i<waveInGetNumDevs();i++){Caps c;names.Add(i+": "+(waveInGetDevCaps(new UIntPtr(i),out c,(uint)Marshal.SizeOf(typeof(Caps)))==0?c.Name:"Nombre no disponible"));}return names.ToArray();}
        public WaveInput(int index) {
            var format=new WaveFormat{Tag=1,Channels=1,Rate=16000,BytesPerSec=32000,Align=2,Bits=16};
            // CALLBACK_EVENT: ninguna llamada WinMM ocurre dentro de un callback del driver.
            uint error=waveInOpen(out device,index<0?uint.MaxValue:(uint)index,ref format,ready.SafeWaitHandle.DangerousGetHandle(),IntPtr.Zero,0x50000);
            if(error!=0){ready.Dispose();throw new InvalidOperationException("No se pudo abrir micrófono PCM 16 kHz. Revisa dispositivo y permisos de Windows (error "+error+").");}
            try{for(int i=0;i<4;i++){
                var pin=GCHandle.Alloc(new byte[4096],GCHandleType.Pinned);pins.Add(pin);var p=Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Header)));headers.Add(p);
                Marshal.StructureToPtr(new Header{Data=pin.AddrOfPinnedObject(),Length=4096},p,false);
                if(waveInPrepareHeader(device,p,(uint)Marshal.SizeOf(typeof(Header)))!=0||waveInAddBuffer(device,p,(uint)Marshal.SizeOf(typeof(Header)))!=0)throw new InvalidOperationException("No se pudo preparar captura de audio.");
            }if(waveInStart(device)!=0)throw new InvalidOperationException("No se pudo iniciar micrófono.");capture=new Thread(Capture){IsBackground=true,Name="Adapta PE · micrófono"};capture.Start();}catch{Dispose();throw;}
        }
        void Capture(){
            try{while(!closed){ready.WaitOne(500);if(closed)return;for(int i=0;i<headers.Count;i++){
                var pointer=headers[nextBuffer];var h=(Header)Marshal.PtrToStructure(pointer,typeof(Header));if((h.Flags&1)==0)break;
                Push(h);if(closed)return;uint error=waveInAddBuffer(device,pointer,(uint)Marshal.SizeOf(typeof(Header)));
                if(error!=0)throw new IOException("Captura de audio interrumpida (WinMM "+error+").");
                nextBuffer=(nextBuffer+1)%headers.Count;
            }}}catch(Exception error){lock(sync){captureError=error;Monitor.PulseAll(sync);}}
        }
        void Push(Header h) {
            Action<double> level=null;double peak=0;
            lock(sync){if(closed)return;int n=(int)Math.Min(4096u,h.Recorded);if(n>0){
                double now=Stopwatch.GetTimestamp()/(double)Stopwatch.Frequency;if(Level!=null&&now-lastLevel>=.1){lastLevel=now;for(int i=0;i+1<n;i+=2)peak=Math.Max(peak,Math.Abs((int)Marshal.ReadInt16(h.Data,i))/32768.0);level=Level;}
                if(count+n>ring.Length){int drop=count+n-ring.Length;head=(head+drop)%ring.Length;count-=drop;}
                lastAudio=Stopwatch.GetTimestamp();int tail=(head+count)%ring.Length,first=Math.Min(n,ring.Length-tail);Marshal.Copy(h.Data,ring,tail,first);if(n>first)Marshal.Copy(IntPtr.Add(h.Data,first),ring,0,n-first);count+=n;Monitor.PulseAll(sync);
            }}if(level!=null)level(Math.Min(1,peak*3));
        }
        public override int Read(byte[] buffer,int offset,int length){if(buffer==null)throw new ArgumentNullException("buffer");if(offset<0||length<0||offset+length>buffer.Length)throw new ArgumentOutOfRangeException();if(length==0)return 0;
            lock(sync){while(count==0&&!closed){if(captureError!=null)throw new IOException("Falló captura del micrófono: "+captureError.Message,captureError);Monitor.Wait(sync,500);if(!closed&&(Stopwatch.GetTimestamp()-lastAudio)/(double)Stopwatch.Frequency>6)throw new IOException("Micrófono sin muestras durante seis segundos.");}if(closed)return 0;int n=Math.Min(length,count),first=Math.Min(n,ring.Length-head);Buffer.BlockCopy(ring,head,buffer,offset,first);if(n>first)Buffer.BlockCopy(ring,0,buffer,offset+first,n-first);head=(head+n)%ring.Length;count-=n;Consumed(n);return n;}}
        protected override void Dispose(bool disposing){lock(sync){if(closed)return;closed=true;Monitor.PulseAll(sync);}ready.Set();if(capture!=null&&capture.IsAlive)capture.Join();if(device!=IntPtr.Zero){waveInStop(device);waveInReset(device);foreach(var p in headers)waveInUnprepareHeader(device,p,(uint)Marshal.SizeOf(typeof(Header)));waveInClose(device);device=IntPtr.Zero;}foreach(var p in headers)Marshal.FreeHGlobal(p);foreach(var pin in pins)pin.Free();ready.Dispose();Array.Clear(ring,0,ring.Length);base.Dispose(disposing);}
        public override bool CanRead{get{return !closed;}}
    }
}
