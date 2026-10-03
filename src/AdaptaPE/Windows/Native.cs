using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using AdaptaPE.Core;

namespace AdaptaPE.Windows
{
    internal static class Native
    {
        public static readonly IntPtr Tag=new IntPtr(0x41445045);
        [StructLayout(LayoutKind.Sequential)] public struct Point {public int X,Y;}
        [StructLayout(LayoutKind.Sequential)] public struct Rect {public int Left,Top,Right,Bottom;}
        [StructLayout(LayoutKind.Sequential)] struct Input {public uint Type;public InputUnion Data;}
        [StructLayout(LayoutKind.Explicit)] struct InputUnion {[FieldOffset(0)]public MouseInput Mouse;[FieldOffset(0)]public KeyInput Key;}
        [StructLayout(LayoutKind.Sequential)] struct MouseInput {public int X,Y;public uint MouseData,Flags,Time;public IntPtr Extra;}
        [StructLayout(LayoutKind.Sequential)] struct KeyInput {public ushort VirtualKey,Scan;public uint Flags,Time;public IntPtr Extra;}
        [StructLayout(LayoutKind.Sequential)] public struct MouseHook {public Point Point;public uint MouseData,Flags,Time;public IntPtr Extra;}
        public delegate IntPtr HookProc(int code,IntPtr message,IntPtr data);
        public delegate void WinEventProc(IntPtr hook,uint evt,IntPtr window,int obj,int child,uint thread,uint time);
        [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")]public static extern uint GetWindowThreadProcessId(IntPtr window,out uint pid);
        [DllImport("user32.dll")]public static extern bool IsWindow(IntPtr window);
        [DllImport("user32.dll")]public static extern bool IsWindowVisible(IntPtr window);
        public delegate bool EnumWindowProc(IntPtr window,IntPtr parameter);
        [DllImport("user32.dll")]public static extern bool EnumWindows(EnumWindowProc callback,IntPtr parameter);
        [DllImport("user32.dll")]public static extern bool SetForegroundWindow(IntPtr window);
        [DllImport("user32.dll")]public static extern bool ShowWindow(IntPtr window,int command);
        [DllImport("user32.dll")] public static extern bool GetCursorPos(out Point point);
        [DllImport("user32.dll")]public static extern IntPtr WindowFromPoint(Point point);
        [DllImport("user32.dll")]public static extern IntPtr GetAncestor(IntPtr window,uint flags);
        public static IntPtr RootAtCursor(){Point p;if(!GetCursorPos(out p))return IntPtr.Zero;return GetAncestor(WindowFromPoint(p),2);}
        [DllImport("user32.dll")]public static extern int GetSystemMetrics(int index);
        [DllImport("user32.dll",SetLastError=true)]static extern uint SendInput(uint count,Input[] inputs,int size);
        [DllImport("user32.dll")]public static extern bool GetWindowRect(IntPtr window,out Rect rect);
        [DllImport("user32.dll")]public static extern bool SetWindowPos(IntPtr window,IntPtr after,int x,int y,int width,int height,uint flags);
        [DllImport("user32.dll")]public static extern IntPtr SetWinEventHook(uint min,uint max,IntPtr module,WinEventProc callback,uint pid,uint thread,uint flags);
        [DllImport("user32.dll")]public static extern bool UnhookWinEvent(IntPtr hook);
        [DllImport("user32.dll",SetLastError=true)]public static extern IntPtr SetWindowsHookEx(int id,HookProc proc,IntPtr module,uint thread);
        [DllImport("user32.dll")]public static extern bool UnhookWindowsHookEx(IntPtr hook);
        [DllImport("user32.dll")]public static extern IntPtr CallNextHookEx(IntPtr hook,int code,IntPtr message,IntPtr data);
        [DllImport("kernel32.dll",CharSet=CharSet.Unicode)]public static extern IntPtr GetModuleHandle(string name);
        [DllImport("user32.dll")]public static extern bool PostMessage(IntPtr window,uint message,IntPtr w,IntPtr l);
        [DllImport("user32.dll",CharSet=CharSet.Unicode)]public static extern int GetWindowText(IntPtr window,StringBuilder text,int length);
        [DllImport("user32.dll",SetLastError=true)]public static extern bool RegisterHotKey(IntPtr window,int id,uint modifiers,uint key);
        [DllImport("user32.dll")]public static extern bool UnregisterHotKey(IntPtr window,int id);
        [DllImport("user32.dll",EntryPoint="GetWindowLongPtrW")]static extern IntPtr GetWindowLong64(IntPtr hwnd,int index);
        [DllImport("user32.dll",EntryPoint="GetWindowLongW")]static extern int GetWindowLong32(IntPtr hwnd,int index);
        public static IntPtr GetWindowLongPtr(IntPtr hwnd,int index){return IntPtr.Size==8?GetWindowLong64(hwnd,index):new IntPtr(GetWindowLong32(hwnd,index));}
        [DllImport("user32.dll",EntryPoint="SetWindowLongPtrW")]static extern IntPtr SetWindowLong64(IntPtr hwnd,int index,IntPtr value);
        [DllImport("user32.dll",EntryPoint="SetWindowLongW")]static extern int SetWindowLong32(IntPtr hwnd,int index,int value);
        public static IntPtr SetWindowLongPtr(IntPtr hwnd,int index,IntPtr value){return IntPtr.Size==8?SetWindowLong64(hwnd,index,value):new IntPtr(SetWindowLong32(hwnd,index,value.ToInt32()));}
        [DllImport("user32.dll",SetLastError=true)]static extern IntPtr OpenInputDesktop(uint flags,bool inherit,uint access);
        [DllImport("user32.dll")]static extern bool CloseDesktop(IntPtr desktop);
        [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern bool GetUserObjectInformation(IntPtr obj,int index,StringBuilder buffer,uint length,out uint needed);
        public static bool DefaultDesktop() {
            var desktop=OpenInputDesktop(0,false,1);if(desktop==IntPtr.Zero)return false;
            try{var name=new StringBuilder(128);uint needed;return GetUserObjectInformation(desktop,2,name,256,out needed)&&name.ToString()=="Default";}finally{CloseDesktop(desktop);}
        }
        public static uint Pid(IntPtr window){uint p;GetWindowThreadProcessId(window,out p);return p;}
        public static bool Own(IntPtr window){return window!=IntPtr.Zero&&Pid(window)==(uint)Process.GetCurrentProcess().Id;}
        public static string Title(IntPtr window){var s=new StringBuilder(300);GetWindowText(window,s,s.Capacity);return s.ToString();}
        public static bool Browser(IntPtr window) {
            try{string n=Process.GetProcessById((int)Pid(window)).ProcessName.ToLowerInvariant();return new[]{"chrome","msedge","brave","firefox","opera","vivaldi"}.Contains(n);}catch(ArgumentException){return false;}
        }
        static void Post(Input[] inputs) {
            if(!DefaultDesktop())throw new InvalidOperationException("Entrada detenida fuera del escritorio de usuario.");
            uint count=SendInput((uint)inputs.Length,inputs,Marshal.SizeOf(typeof(Input)));
            if(count!=inputs.Length)throw new InvalidOperationException("Windows bloqueó la entrada. Comprueba foco y nivel de permisos (UIPI).");
        }
        static Input Key(ushort key,bool up,ushort scan=0,bool unicode=false){return new Input{Type=1,Data=new InputUnion{Key=new KeyInput{VirtualKey=unicode?(ushort)0:key,Scan=scan,Flags=(uint)((up?2:0)|(unicode?4:0)),Extra=Tag}}};}
        public static void Chord(ushort[] keys) {
            if(keys==null||keys.Length==0)throw new ArgumentException("Atajo no reconocido.");
            var inputs=keys.Select(k=>Key(k,false)).Concat(keys.Reverse().Select(k=>Key(k,true))).ToArray();
            try{Post(inputs);}catch{try{Post(keys.Reverse().Select(k=>Key(k,true)).ToArray());}catch{}throw;}
        }
        public static void Unicode(string text,Func<bool> canAct) {
            for(int offset=0;offset<text.Length;offset+=64) {
                if(!canAct())throw new InvalidOperationException("El foco cambió; dictado detenido.");
                var b=text.Substring(offset,Math.Min(64,text.Length-offset));
                Post(b.SelectMany(c=>new[]{Key(0,false,c,true),Key(0,true,c,true)}).ToArray());
            }
        }
        public static void Move(double x,double y) {
            int left=GetSystemMetrics(76),top=GetSystemMetrics(77),width=GetSystemMetrics(78),height=GetSystemMetrics(79);
            x=Math.Max(left,Math.Min(left+width-1,x));y=Math.Max(top,Math.Min(top+height-1,y));
            Post(new[]{new Input{Type=0,Data=new InputUnion{Mouse=new MouseInput{X=(int)Math.Round((x-left)*65535/Math.Max(1,width-1)),Y=(int)Math.Round((y-top)*65535/Math.Max(1,height-1)),Flags=0x8000|0x4000|1,Extra=Tag}}}});
        }
        public static void Click(bool right=false){ButtonClick(right,1);}
        public static void DoubleClick(){ButtonClick(false,2);}
        static void ButtonClick(bool right,int clicks){
            uint down=right?8u:2u,up=right?16u:4u;
            var inputs=Enumerable.Range(0,clicks*2).Select(i=>new Input{Data=new InputUnion{Mouse=new MouseInput{Flags=i%2==0?down:up,Extra=Tag}}}).ToArray();
            try{Post(inputs);}catch{try{Post(new[]{new Input{Data=new InputUnion{Mouse=new MouseInput{Flags=up,Extra=Tag}}}});}catch{}throw;}
        }
        public static void Scroll(int lines){Post(new[]{new Input{Data=new InputUnion{Mouse=new MouseInput{MouseData=unchecked((uint)(lines*120)),Flags=0x0800,Extra=Tag}}}});}
    }
}
