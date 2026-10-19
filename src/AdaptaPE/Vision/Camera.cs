using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using AdaptaPE.Core;
using AdaptaPE.Windows;
using OpenCvSharp;

namespace AdaptaPE.Vision
{
    public sealed class CursorTarget {public bool Attract;public string Id;public System.Windows.Rect Bounds;public double Time;public IntPtr Window,HitWindow;}
    public sealed class Camera:IDisposable
    {
        public const int PreviewWidth=320,PreviewHeight=240,PreviewFps=24;
        readonly GestureState gesture=new GestureState();
        readonly Preferences settings;readonly KineticState state=new KineticState();readonly object gate=new object(),previewGate=new object();
        readonly byte[] previewPixels=new byte[PreviewWidth*PreviewHeight*3];bool previewReady;
        CancellationTokenSource cancellation;Task worker;volatile int generation;volatile bool active,paused,nextRight;bool disposed;
        Native.HookProc hookProc;IntPtr hook;double pauseUntil,velocityTime;Vec velocity;volatile CursorTarget target;volatile Detection lastDetection;
        public Action<string> Status;public Action Preview;public Action<double> Progress;public Action<string> Announce;
        public Func<bool> PreviewEnabled;public bool Active{get{return active;}}public bool Paused{get{return paused;}}
        static double Now{get{return Stopwatch.GetTimestamp()/(double)Stopwatch.Frequency;}}
        public Camera(Preferences settings){this.settings=settings;}
        public void SetTarget(CursorTarget value){target=value;}
        public void Calibrate(){lock(gate){state.FixedCenter=ControlModes.Normalize(settings.Mode)!="Cabeza";state.Calibrate();gesture.Reset();velocity=new Vec();velocityTime=0;}if(Status!=null)Status(state.FixedCenter?"Centro fijo listo · mueve ligeramente la zona visible":"Mantén postura cómoda durante un segundo…");}
        public void Configure(){lock(gate){state.Sensitivity=settings.Sensitivity;state.Amplitude=settings.Amplitude;state.DwellDuration=settings.Dwell;state.Tremor=settings.Tremor;state.DwellEnabled=settings.DwellEnabled;}}
        public void Pause(bool value){paused=value;Calibrate();}
        public void RightNext(){nextRight=true;}
        public void CancelClick(){nextRight=false;lock(gate)state.CancelClick();}
        bool PhysicalPause{get{return Now<Volatile.Read(ref pauseUntil);}}
        public bool CopyPreview(byte[] pixels){lock(previewGate){if(!active||!previewReady)return false;Buffer.BlockCopy(previewPixels,0,pixels,0,previewPixels.Length);return true;}}
        public void Start() {
            if(active||disposed)return;if(worker!=null&&!worker.IsCompleted)throw new InvalidOperationException("La cámara anterior sigue cerrándose. Espera y vuelve a activar.");
            int token=++generation;Configure();Calibrate();paused=false;lastDetection=null;active=true;
            var run=new CancellationTokenSource();cancellation=run;var cancel=run.Token;
            hookProc=(code,message,data)=>{if(code>=0){var m=(Native.MouseHook)Marshal.PtrToStructure(data,typeof(Native.MouseHook));if((m.Flags&1)==0&&m.Extra!=Native.Tag)Volatile.Write(ref pauseUntil,Now+2);}return Native.CallNextHookEx(hook,code,message,data);};
            hook=Native.SetWindowsHookEx(14,hookProc,Native.GetModuleHandle(null),0);
            if(hook==IntPtr.Zero){active=false;run.Dispose();cancellation=null;throw new InvalidOperationException("No se pudo registrar pausa por ratón físico.");}
            worker=Task.Run(async()=>{
                Task captureWorker=null,pointerWorker=null;Mat newest=null;var frames=new object();int revision=0;double capturedAt=0;
                try {
                    Cv2.SetNumThreads(1);newest=new Mat();
                    // Una captura y un solo frame pendiente. La preview no espera al detector.
                    captureWorker=Task.Run(async()=>{
                        using(var capture=new VideoCapture())using(var frame=new Mat())using(var image=new Mat())using(var display=new Mat()){
                            if(!capture.Open(settings.Camera,VideoCaptureAPIs.MSMF)&&!capture.Open(settings.Camera,VideoCaptureAPIs.DSHOW))throw new InvalidOperationException("No se pudo abrir cámara. Revisa índice, privacidad y otras aplicaciones.");
                            capture.Set(VideoCaptureProperties.FrameWidth,640);capture.Set(VideoCaptureProperties.FrameHeight,480);capture.Set(VideoCaptureProperties.Fps,30);capture.Set(VideoCaptureProperties.BufferSize,1);
                            double lastPreview=0;
                            while(!cancel.IsCancellationRequested&&token==generation){
                                double start=Now;if(!Native.DefaultDesktop()){await Task.Delay(150,cancel);continue;}
                                if(!capture.Read(frame)||frame.Empty())throw new InvalidOperationException("Cámara desconectada. Reactiva después de reconectarla.");
                                Cv2.Resize(frame,image,new Size(640,480));Cv2.Flip(image,image,FlipMode.Y);
                                if(image.Channels()==4)Cv2.CvtColor(image,image,ColorConversionCodes.BGRA2BGR);else if(image.Channels()==1)Cv2.CvtColor(image,image,ColorConversionCodes.GRAY2BGR);
                                if(image.Type()!=MatType.CV_8UC3)throw new InvalidOperationException("La cámara no entrega un formato de imagen compatible.");
                                lock(frames){image.CopyTo(newest);capturedAt=Now;revision++;}
                                if(Preview!=null&&PreviewEnabled!=null&&PreviewEnabled()&&Now-lastPreview>=1.0/PreviewFps){
                                    lastPreview=Math.Max(lastPreview+1.0/PreviewFps,Now-1.0/PreviewFps);Cv2.Resize(image,display,new Size(PreviewWidth,PreviewHeight));DrawPreview(display,lastDetection);
                                    lock(previewGate){if(token==generation){Marshal.Copy(display.Data,previewPixels,0,previewPixels.Length);previewReady=true;}}
                                    if(token==generation)Preview();
                                }
                                await Task.Delay(Math.Max(1,(int)(1000.0/30-(Now-start)*1000)),cancel);
                            }
                        }
                    });
                    pointerWorker=Task.Run(()=>MovePointer(token,cancel));
                    using(var image=new Mat())using(var vision=new LocalVision()){
                        string source=null,candidate=null,mode=null;double candidateSince=0,lostSince=0,lastStatus=0,lastProgress=0;int candidateCount=0,usedRevision=0;bool wasPhysicalPause=false;
                        while(!cancel.IsCancellationRequested&&token==generation){
                            if(captureWorker.IsCompleted)await captureWorker;if(pointerWorker.IsCompleted)await pointerWorker;
                            double start=Now,frameTime=0;bool fresh=false;
                            lock(frames){if(revision!=usedRevision){newest.CopyTo(image);usedRevision=revision;frameTime=capturedAt;fresh=true;}}
                            if(!fresh){await Task.Delay(10,cancel);continue;}
                            if(paused||PhysicalPause||!Native.DefaultDesktop()){
                                lock(gate){state.Calibrate();gesture.Reset();velocity=new Vec();velocityTime=0;}wasPhysicalPause=true;lastDetection=null;
                                if(start-lastStatus>.5){lastStatus=start;Emit(token,paused?"Cursor pausado · cámara visible":"Pausa por ratón físico · 2 segundos");}if(Progress!=null)Progress(0);await Task.Delay(50,cancel);continue;
                            }
                            if(wasPhysicalPause){wasPhysicalPause=false;source=null;Calibrate();}
                            if(mode!=settings.Mode){mode=settings.Mode;source=null;candidate=null;vision.ResetFree();Calibrate();}
                            var detection=vision.Detect(image,mode,source);double now=Now;if(mode!=settings.Mode){lock(gate){velocity=new Vec();velocityTime=0;state.CancelClick();gesture.Reset();}continue;}if(cancel.IsCancellationRequested||token!=generation)break;if(captureWorker.IsFaulted)await captureWorker;
                            lastDetection=detection;
                            if(detection==null||now-frameTime>.35){
                                lock(gate){state.Lost(now);gesture.Reset();velocity=new Vec();velocityTime=0;}lastDetection=null;if(Progress!=null)Progress(0);
                                candidate=null;candidateCount=0;if(lostSince==0)lostSince=now;if(now-lostSince>=1.2){source=null;candidate=null;vision.ResetFree();}
                                if(now-lastStatus>.5){lastStatus=now;Emit(token,"Sin detección estable · cursor detenido");}
                            }else{
                                lostSince=0;
                                if(detection.Source!=source){
                                    lock(gate){velocity=new Vec();velocityTime=0;state.CancelClick();}
                                    if(candidate!=detection.Source){candidate=detection.Source;candidateSince=now;candidateCount=0;}candidateCount++;
                                    if(candidateCount<3||now-candidateSince<.15){
                                        if(detection.Backup!=null&&detection.Backup.Source==source)detection=detection.Backup;
                                        else{if(now-lastStatus>.5){lastStatus=now;Emit(token,"Confirmando "+detection.Source+" · cursor detenido");}await Task.Delay(Math.Max(1,(int)(1000.0/settings.Fps-(Now-start)*1000)),cancel);continue;}
                                    }else{source=detection.Source;Calibrate();}
                                }else{candidate=null;candidateCount=0;}
                                Native.Point position;var current=target;
                                string id=current!=null&&Native.GetCursorPos(out position)&&now-current.Time<.75&&current.Bounds.Contains(position.X,position.Y)&&Native.GetForegroundWindow()==current.Window&&Native.RootAtCursor()==current.HitWindow?current.Id:null;
                                MotionResult motion;bool calibrated,armed;lock(gate){var point=state.FixedCenter?gesture.Update(detection.Point,frameTime,detection.Tilt,detection.Pointing,detection.Angle,settings.Fps==5?.3:.2):detection.Point;if(state.FixedCenter&&gesture.ResetNow)state.CancelClick();state.MaxGap=settings.Fps==5?.3:.2;motion=state.Update(point,now,state.FixedCenter?1:.75,id);velocity=motion.Velocity;velocityTime=state.FixedCenter&&gesture.ResetNow?0:frameTime;calibrated=state.Origin.HasValue;armed=state.ClickArmed;}
                                if(motion.Calibrated&&Announce!=null)Announce("Postura calibrada");
                                if(token!=generation||cancel.IsCancellationRequested||paused||PhysicalPause)continue;
                                if(motion.Click&&current!=null&&id!=null&&Native.DefaultDesktop()&&Native.GetForegroundWindow()==current.Window&&Native.RootAtCursor()==current.HitWindow&&Native.GetCursorPos(out position)&&current.Bounds.Contains(position.X,position.Y)){Native.Click(nextRight);nextRight=false;}
                                if(Progress!=null&&(now-lastProgress>=.1||motion.Click)){lastProgress=now;Progress(motion.Progress);}
                                if(now-lastStatus>.4){lastStatus=now;Emit(token,!calibrated?"Calibrando postura · mantente cómodo":motion.Progress>0?"Clic por permanencia · "+(int)(motion.Progress*100)+" %":motion.Velocity.Length>0?"Moviendo · "+source:armed?"Postura estable · esperando clic":"Zona segura · sal del anillo y vuelve para habilitar clic");}
                            }
                            await Task.Delay(Math.Max(1,(int)(1000.0/settings.Fps-(Now-start)*1000)),cancel);
                        }
                    }
                }catch(OperationCanceledException){}catch(Exception error){if(token==generation){active=false;Emit(token,"Cámara detenida: "+error.Message);}}
                finally{
                    run.Cancel();lock(gate)velocity=new Vec();
                    if(pointerWorker!=null)try{await pointerWorker;}catch(Exception){}
                    if(captureWorker!=null)try{await captureWorker;}catch(Exception){}
                    if(newest!=null)newest.Dispose();Interlocked.CompareExchange(ref cancellation,null,run);run.Dispose();
                }
            });
        }
        async Task MovePointer(int token,CancellationToken cancel){
            Vec cursor=new Vec();Native.Point physical;bool initialized=false;double previous=Now;
            while(!cancel.IsCancellationRequested&&token==generation){
                double now=Now,dt=Math.Min(.05,Math.Max(0,now-previous));previous=now;Vec speed;double updated;bool ready;lock(gate){speed=velocity;updated=velocityTime;ready=state.Origin.HasValue;}
                if(paused||PhysicalPause||!Native.DefaultDesktop()||now-updated>.35||!ready){if(Native.GetCursorPos(out physical)){cursor=new Vec(physical.X,physical.Y);initialized=true;}}
                else{
                    if(!initialized&&Native.GetCursorPos(out physical)){cursor=new Vec(physical.X,physical.Y);initialized=true;}
                    if(initialized&&token==generation&&!cancel.IsCancellationRequested&&!paused&&!PhysicalPause){cursor.X+=speed.X*dt;cursor.Y+=speed.Y*dt;var current=target;if(current!=null&&current.Attract&&now-current.Time<.35&&Native.GetForegroundWindow()==current.Window&&Native.RootAtCursor()==current.HitWindow)cursor=CursorAttraction.Apply(cursor,new Vec(current.Bounds.X+current.Bounds.Width/2,current.Bounds.Y+current.Bounds.Height/2),speed.Length,dt);if(Native.GetCursorPos(out physical)&&(Math.Abs(cursor.X-physical.X)>.5||Math.Abs(cursor.Y-physical.Y)>.5))Native.Move(cursor.X,cursor.Y);if(Native.GetCursorPos(out physical))cursor=cursor.ReconcilePixels(new Vec(physical.X,physical.Y));}
                }
                await Task.Delay(15,cancel);
            }
        }
        void DrawPreview(Mat display,Detection detection){
            if(detection!=null&&detection.Landmarks!=null)foreach(var p in detection.Landmarks)Cv2.Circle(display,(int)(p.X*display.Cols/640),(int)(p.Y*display.Rows/480),2,Scalar.LimeGreen,-1);
            Vec point;double dead,attraction;bool calibrated;lock(gate){point=state.ControlPoint;dead=state.DeadRadius;attraction=state.AttractionRadius;calibrated=state.Origin.HasValue;}
            var center=new OpenCvSharp.Point(display.Cols/2,display.Rows/2);Cv2.Circle(display,center,(int)(dead*display.Cols),Scalar.LimeGreen,1);Cv2.Circle(display,center,(int)(attraction*display.Cols),new Scalar(0,175,255),1);
            if(calibrated)Cv2.Circle(display,(int)(center.X+(point.X-.5)*display.Cols),(int)(center.Y+(point.Y-.5)*display.Cols),4,Scalar.White,-1);
        }
        void Emit(int token,string message){if(token==generation&&Status!=null)Status(message);}
        public void Stop(){generation++;active=false;nextRight=false;target=null;var run=Interlocked.Exchange(ref cancellation,null);if(run!=null)try{run.Cancel();}catch(ObjectDisposedException){}if(hook!=IntPtr.Zero){Native.UnhookWindowsHookEx(hook);hook=IntPtr.Zero;}lock(gate){state.Calibrate();velocity=new Vec();}lock(previewGate)previewReady=false;}
        public void Dispose(){disposed=true;Stop();}
    }
}
