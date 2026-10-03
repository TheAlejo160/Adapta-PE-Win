using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AdaptaPE.Core;
using Microsoft.Win32;

namespace AdaptaPE.Windows
{
    public sealed class AppEntry {public string Name,Path;}
    public sealed class OpenWindow {public IntPtr Handle;public uint Pid;public string Process,Title;}
    public sealed class Applications
    {
        AppEntry[] cache;DateTime expiry;
        public AppEntry[] Catalog() {
            if(cache!=null&&DateTime.UtcNow<expiry)return cache;
            var items=new List<AppEntry>();
            foreach(string special in new[]{Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu)}) {
                if(!Directory.Exists(special))continue;var pending=new Stack<string>();pending.Push(special);
                while(pending.Count>0&&items.Count<1500){string folder=pending.Pop();try{
                    foreach(string link in Directory.EnumerateFiles(folder,"*.lnk"))items.Add(new AppEntry{Name=System.IO.Path.GetFileNameWithoutExtension(link),Path=link});
                    foreach(string child in Directory.EnumerateDirectories(folder))if((File.GetAttributes(child)&FileAttributes.ReparsePoint)==0)pending.Push(child);
                }catch(UnauthorizedAccessException){}catch(IOException){}}
            }
            foreach(var pair in new[]{Tuple.Create("Bloc de notas","notepad.exe"),Tuple.Create("Calculadora","calc.exe"),Tuple.Create("Paint","mspaint.exe"),Tuple.Create("Explorador de archivos","explorer.exe")}) {
                string file=System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),"System32",pair.Item2);
                if(File.Exists(file))items.Add(new AppEntry{Name=pair.Item1,Path=file});
            }
            foreach(var hive in new[]{RegistryHive.CurrentUser,RegistryHive.LocalMachine})foreach(var view in new[]{RegistryView.Registry64,RegistryView.Registry32}) {
                try{using(var root=RegistryKey.OpenBaseKey(hive,view))using(var paths=root.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\App Paths")) {
                    if(paths==null)continue;
                    foreach(var name in paths.GetSubKeyNames().Take(1500))using(var key=paths.OpenSubKey(name)) {
                        string file=(key==null?null:key.GetValue(null) as string);if(file==null)continue;file=Environment.ExpandEnvironmentVariables(file).Trim().Trim('"');
                        if(System.IO.Path.IsPathRooted(file)&&System.IO.Path.GetExtension(file).Equals(".exe",StringComparison.OrdinalIgnoreCase)&&File.Exists(file)&&Allowed(file)&&!items.Any(e=>AppNames.Canonical(e.Name)==AppNames.Canonical(System.IO.Path.GetFileNameWithoutExtension(name))))
                            items.Add(new AppEntry{Name=AppNames.Canonical(System.IO.Path.GetFileNameWithoutExtension(name)),Path=file});
                    }
                }}catch(System.Security.SecurityException){}catch(UnauthorizedAccessException){}catch(IOException){}
            }
            cache=items.GroupBy(e=>e.Path,StringComparer.OrdinalIgnoreCase).Select(g=>g.First()).OrderBy(e=>e.Name).ToArray();expiry=DateTime.UtcNow.AddMinutes(1);return cache;
        }
        public AppEntry[] Match(string name) {
            var matches=Catalog().Where(e=>Allowed(e.Path)&&Allowed(e.Name)).Select(e=>new{Entry=e,Rank=AppNames.MatchRank(name,e.Name)}).Where(e=>e.Rank>0).ToArray();
            int best=matches.Length==0?0:matches.Max(e=>e.Rank);
            return matches.Where(e=>e.Rank==best).Select(e=>e.Entry).ToArray();
        }
        static bool Allowed(string file){return !new[]{"cmd","powershell","pwsh","wscript","cscript","mshta","rundll32"}.Contains(Commands.Normalize(System.IO.Path.GetFileNameWithoutExtension(file)));}
        public static Process Launch(AppEntry app){if(!File.Exists(app.Path)||!Allowed(app.Path)||!Allowed(app.Name)||!new[]{".exe",".lnk"}.Contains(System.IO.Path.GetExtension(app.Path).ToLowerInvariant()))throw new InvalidOperationException("Aplicación no disponible.");return Process.Start(new ProcessStartInfo(app.Path){UseShellExecute=true});}
        public static OpenWindow[] OpenWindows(){
            var result=new List<OpenWindow>();var processes=new Dictionary<uint,string>();
            Native.EnumWindows((window,parameter)=>{
                if(!Native.IsWindowVisible(window)||Native.Own(window)||(Native.GetWindowLongPtr(window,-20).ToInt64()&0x80)!=0)return true;
                string title=Native.Title(window);if(title.Length==0)return true;uint pid=Native.Pid(window);string process;
                if(!processes.TryGetValue(pid,out process)){try{using(var p=Process.GetProcessById((int)pid))process=p.ProcessName;}catch(ArgumentException){return true;}catch(InvalidOperationException){return true;}catch(System.ComponentModel.Win32Exception){return true;}processes[pid]=process;}
                result.Add(new OpenWindow{Handle=window,Pid=pid,Process=process,Title=title});return true;
            },IntPtr.Zero);
            return result.ToArray();
        }
        public static OpenWindow[] FindRunning(string name,bool approximate=false){
            var result=OpenWindows().Select(w=>new{Window=w,Rank=AppNames.RunningRank(name,w.Process,w.Title)}).Where(w=>w.Rank>=(approximate?1:2)).ToArray();
            int best=result.Length==0?0:result.Max(w=>w.Rank);return result.Where(w=>w.Rank==best).Select(w=>w.Window).ToArray();
        }
        public static IntPtr[] Running(string name){return FindRunning(name).Select(w=>w.Handle).ToArray();}
        public static void Activate(IntPtr window,uint pid){
            if(!Native.DefaultDesktop()||!Native.IsWindow(window)||Native.Own(window)||Native.Pid(window)!=pid)throw new InvalidOperationException("La ventana cambió; activación cancelada.");
            Native.ShowWindow(window,9);if(!Native.SetForegroundWindow(window))throw new InvalidOperationException("Windows no permitió enfocar la aplicación. Enfócala manualmente.");
        }
        public static void Close(IntPtr window,uint pid){
            if(!Native.DefaultDesktop()||!Native.IsWindow(window)||Native.Own(window)||Native.Pid(window)!=pid)throw new InvalidOperationException("La ventana cambió; cierre cancelado.");
            if(!Native.PostMessage(window,0x10,IntPtr.Zero,IntPtr.Zero))throw new InvalidOperationException("Windows no permitió cerrar esa ventana. Puede estar ejecutándose como administrador.");
        }
        public static async Task<IntPtr> Window(AppEntry app,Process process,CancellationToken token) {
            for(int i=0;i<20;i++){token.ThrowIfCancellationRequested();if(process!=null){try{process.Refresh();if(process.MainWindowHandle!=IntPtr.Zero)return process.MainWindowHandle;if(process.HasExited)process=null;}catch(InvalidOperationException){process=null;}}
                var matches=Running(app.Name);if(matches.Length==1)return matches[0];await Task.Delay(100,token);
            }return IntPtr.Zero;
        }
    }
}
