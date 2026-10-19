using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AdaptaPE.Core;

namespace AdaptaPE.Windows
{
    public sealed class Actions:IDisposable
    {
        readonly DesktopAccess desktop;readonly Applications apps=new Applications();readonly SemaphoreSlim busy=new SemaphoreSlim(1,1);
        CancellationTokenSource cancellation=new CancellationTokenSource();
        public IntPtr Target {get;private set;}
        IntPtr choiceWindow;Action<int> choose;string[] labels;int page;DateTime choiceExpires;
        public Action<string> Respond;public Action<string[]> ChoiceLabels;public Func<Command,Task<bool>> Local;
        public bool PendingChoice {get{return choose!=null&&DateTime.UtcNow<choiceExpires;}}
        public Actions(DesktopAccess desktop){this.desktop=desktop;desktop.Choices=(items,action)=>SetChoices(items,action,Target);}
        public void OnForeground(IntPtr window) {
            if(window==IntPtr.Zero||Native.Own(window))return;
            if(Target!=window){if(choiceWindow!=IntPtr.Zero)CancelChoices();desktop.CancelSelection();}Target=window;
        }
        public void CancelChoices(){choose=null;labels=null;page=0;if(ChoiceLabels!=null)ChoiceLabels(new string[0]);}
        public void Cancel(){cancellation.Cancel();cancellation.Dispose();cancellation=new CancellationTokenSource();CancelChoices();desktop.CancelSelection();}
        void SetChoices(string[] values,Action<int> action,IntPtr target) {
            choose=action;labels=values;page=0;choiceWindow=target;choiceExpires=DateTime.UtcNow.AddSeconds(45);PublishChoices();
        }
        void PublishChoices(){if(ChoiceLabels!=null)ChoiceLabels(labels.Skip(page*6).Take(6).Select((x,i)=>(i+1)+". "+x).ToArray());}
        string ChoiceSummary(){return string.Join(". ",labels.Skip(page*6).Take(6).Select((x,i)=>(i+1)+". "+x))+". Di el número, siguiente o anterior.";}
        void Choice(string text) {
            if(!PendingChoice){CancelChoices();throw new InvalidOperationException("No hay una elección pendiente.");}
            if(choiceWindow!=IntPtr.Zero&&Target!=choiceWindow){CancelChoices();throw new InvalidOperationException("La aplicación cambió; elección cancelada.");}
            string n=Commands.Normalize(text);
            if(n=="siguiente"||n=="anterior"){int next=page+(n=="siguiente"?1:-1);if(next<0||next*6>=labels.Length)throw new InvalidOperationException("No hay más opciones.");page=next;PublishChoices();if(Respond!=null)Respond(ChoiceSummary());return;}
            int index=Commands.Ordinal(n);if(index>=0){if(index>=6||page*6+index>=labels.Length)throw new InvalidOperationException("Elige un número de esta página.");index+=page*6;}
            else{var hits=labels.Select((label,i)=>new{label,i}).Where(e=>Commands.Normalize(e.label)==n).ToArray();if(hits.Length!=1)throw new InvalidOperationException("Di un número o un nombre único.");index=hits[0].i;}
            var action=choose;CancelChoices();action(index);
        }
        void PrepareTarget() {
            if(Target==IntPtr.Zero||!Native.IsWindow(Target))throw new InvalidOperationException("Abre una aplicación de destino antes del comando.");
            var foreground=Native.GetForegroundWindow();if(Native.Own(foreground)){Native.ShowWindow(Target,9);Native.SetForegroundWindow(Target);}
            if(!DesktopAccess.IsCurrent(Target))throw new InvalidOperationException("No se pudo activar la ventana. Enfoca la aplicación y repite.");
        }
        void PreparePointer(){PrepareTarget();if(Native.RootAtCursor()!=Target)throw new InvalidOperationException("Coloca el cursor sobre la aplicación de destino antes del clic.");}
        public async Task Execute(string text) {
            var command=Commands.Parse(text);
            if(command.Kind=="stop"||command.Kind=="cancel"||command.Kind=="silence"){if(Local!=null)await Local(command);return;}
            if(!await busy.WaitAsync(0)){if(Respond!=null)Respond("Hay una acción en curso. Puedes decir cancelar o detener todo.");return;}
            var token=cancellation.Token;IntPtr target=Target;
            try{
                if(Local!=null&&await Local(command))return;
                if(command.Kind=="choose") {if(choiceWindow!=IntPtr.Zero)PrepareTarget();await Task.Run(()=>Choice(command.Text),token);return;}
                if(PendingChoice){
                    var hits=labels.Where(x=>Commands.Normalize(x)==Commands.Normalize(text)).ToArray();
                    if(hits.Length==1){PrepareTarget();await Task.Run(()=>Choice(text),token);return;}CancelChoices();
                }
                string response;
                switch(command.Kind) {
                    case "listApps": {
                        var opened=await Task.Run(()=>Applications.OpenWindows().Take(60).ToArray(),token);token.ThrowIfCancellationRequested();
                        if(opened.Length==0)throw new InvalidOperationException("No encontré ventanas externas abiertas.");
                        SetChoices(opened.Select(w=>AppNames.Canonical(w.Process)+" · "+w.Title).ToArray(),i=>{token.ThrowIfCancellationRequested();Applications.Activate(opened[i].Handle,opened[i].Pid);},target);
                        response="Aplicaciones abiertas. Elige cuál enfocar.";break;
                    }
                    case "switchApp": {
                        var windows=await Task.Run(()=>Applications.FindRunning(command.Text),token);bool exact=windows.Length>0;
                        if(!exact)windows=await Task.Run(()=>Applications.FindRunning(command.Text,true),token);
                        token.ThrowIfCancellationRequested();if(windows.Length==0)throw new InvalidOperationException("No encontré esa aplicación abierta. Di muestra apps abiertas.");
                        var pids=windows.Select(w=>w.Pid).ToArray();
                        if(windows.Length>1||!exact){SetChoices(windows.Select(w=>w.Title).ToArray(),i=>{token.ThrowIfCancellationRequested();Applications.Activate(windows[i].Handle,pids[i]);},target);response="Elige la aplicación que quieres enfocar.";}
                        else{Applications.Activate(windows[0].Handle,pids[0]);response="Aplicación enfocada";}break;
                    }
                    case "open":case "appShortcut": {
                        string name=command.Kind=="open"?command.Text:command.Target;string n=Commands.Normalize(name);
                        var monitor=System.Text.RegularExpressions.Regex.Match(n,@"\s+en (?:el )?(?:monitor|pantalla) (uno|dos|tres|cuatro|\d+)$");int monitorIndex=-1;
                        if(monitor.Success){string v=monitor.Groups[1].Value;int number;if(!int.TryParse(v,out number))number=Array.IndexOf(new[]{"uno","dos","tres","cuatro"},v)+1;monitorIndex=number-1;name=name.Substring(0,monitor.Index);}
                        bool explicitSite=command.Target=="site";
                        if(!explicitSite&&command.Kind=="open"&&command.Destination=="actual"&&monitorIndex<0){
                            var running=await Task.Run(()=>Applications.FindRunning(name),token);token.ThrowIfCancellationRequested();
                            if(running.Length>0){var pids=running.Select(w=>w.Pid).ToArray();if(running.Length==1){Applications.Activate(running[0].Handle,pids[0]);response="Aplicación enfocada";}
                                else{SetChoices(running.Select(w=>w.Title).ToArray(),i=>{token.ThrowIfCancellationRequested();Applications.Activate(running[i].Handle,pids[i]);},target);response="Elige la ventana que quieres enfocar.";}break;}
                        }
                        var entries=explicitSite?new AppEntry[0]:await Task.Run(()=>apps.Match(name),token);token.ThrowIfCancellationRequested();
                        if(entries.Length==0){var site=Commands.FindSite(AppNames.Clean(name));Uri url;
                            if(site!=null){await OpenURL(site.Home,command.Destination,token);return;}
                            string candidate=name.Contains("://")?name:"https://"+name;
                            if(!name.Any(char.IsWhiteSpace)&&name.Contains(".")&&Uri.TryCreate(candidate,UriKind.Absolute,out url)&&new[]{"http","https"}.Contains(url.Scheme)&&string.IsNullOrEmpty(url.UserInfo)){await OpenURL(url.AbsoluteUri,command.Destination,token);return;}
                            if(explicitSite)throw new InvalidOperationException("Sitio desconocido. Usa su nombre del catálogo o una dirección HTTP/HTTPS.");
                            PrepareTarget();response=await Task.Run(()=>desktop.Run(new Command("press",name),target,token),token);break;
                        }
                        if(entries.Length>1||AppNames.MatchRank(name,entries[0].Name)<3){SetChoices(entries.Select(e=>e.Name).ToArray(),i=>{token.ThrowIfCancellationRequested();using(var launched=Applications.Launch(entries[i])){}},target);response="Encontré aplicaciones posibles. Elige una opción.";break;}
                        var app=entries[0];var process=await Task.Run(()=>Applications.Launch(app),token);var window=await Applications.Window(app,process,token);if(process!=null)process.Dispose();
                        token.ThrowIfCancellationRequested();if(window!=IntPtr.Zero){Native.ShowWindow(window,9);Native.SetForegroundWindow(window);OnForeground(window);
                            if(monitorIndex>=0){var screens=System.Windows.Forms.Screen.AllScreens;if(monitorIndex>=screens.Length)throw new InvalidOperationException("Monitor no disponible.");Native.Rect bounds;Native.GetWindowRect(window,out bounds);var area=screens[monitorIndex].WorkingArea;Native.SetWindowPos(window,IntPtr.Zero,area.X,area.Y,Math.Min(bounds.Right-bounds.Left,area.Width),Math.Min(bounds.Bottom-bounds.Top,area.Height),0x14);}
                            if(command.Kind=="appShortcut"){if(!DesktopAccess.IsCurrent(window))throw new InvalidOperationException("No se pudo enfocar la aplicación.");Native.Chord(KeyNames.TryParse(command.Text));}
                        }else if(command.Kind=="appShortcut"||monitorIndex>=0)throw new InvalidOperationException("Aplicación abierta; no encontré su ventana para completar el comando.");
                        response="Abriendo "+app.Name;break;
                    }
                    case "search": {
                        var site=Commands.FindSite(command.Target);if(site==null&&Native.Browser(Target)){PrepareTarget();site=await Task.Run(()=>CurrentSite(Target,token),token);}
                        await OpenURL((site??Commands.Sites[0]).Query(command.Text),command.Destination,token);return;
                    }
                    case "closeApp": {
                        var windows=await Task.Run(()=>Applications.FindRunning(command.Text),token);bool suggested=false;
                        if(windows.Length==0){windows=await Task.Run(()=>Applications.FindRunning(command.Text,true),token);suggested=windows.Length>0;}
                        if(windows.Length==0){windows=await Task.Run(()=>apps.Match(command.Text).SelectMany(e=>Applications.FindRunning(e.Name)).GroupBy(w=>w.Handle).Select(g=>g.First()).Take(12).ToArray(),token);suggested=windows.Length>0;}
                        if(windows.Length==0)throw new InvalidOperationException("No encontré esa aplicación abierta. Di cierra y su nombre, por ejemplo cierra Bloc de notas.");
                        token.ThrowIfCancellationRequested();var processes=windows.Select(w=>w.Pid).ToArray();
                        if(windows.Length>1||suggested){SetChoices(windows.Select(w=>w.Title).ToArray(),i=>{token.ThrowIfCancellationRequested();Applications.Close(windows[i].Handle,processes[i]);},target);response=suggested?"Encontré nombres parecidos. Confirma qué ventana quieres cerrar.":"Elige la ventana que quieres cerrar.";}
                        else{token.ThrowIfCancellationRequested();Applications.Close(windows[0].Handle,processes[0]);response="Cierre solicitado; la aplicación conserva su diálogo para guardar.";}break;
                    }
                    case "keys":case "shortcut":PrepareTarget();token.ThrowIfCancellationRequested();if(command.Text=="SCROLL_UP"||command.Text=="SCROLL_DOWN")Native.Scroll(command.Text=="SCROLL_UP"?6:-6);else Native.Chord(KeyNames.TryParse(command.Text));response="Atajo enviado";break;
                    case "click":PrepareTarget();token.ThrowIfCancellationRequested();if(desktop.HasSelection)response=await Task.Run(()=>desktop.Run(new Command("pressSelection"),target,token),token);else{PreparePointer();Native.Click();response="Clic";}break;
                    case "rightClick":PreparePointer();token.ThrowIfCancellationRequested();Native.Click(true);response="Clic derecho";break;
                    case "doubleClick":PreparePointer();token.ThrowIfCancellationRequested();Native.DoubleClick();response="Doble clic";break;
                    case "unknown":if(System.Text.RegularExpressions.Regex.IsMatch(Commands.Normalize(command.Text),@"^(?:no|nunca|jamas)\b"))throw new InvalidOperationException("Orden negada; no ejecuté ninguna acción.");PrepareTarget();response=await Task.Run(()=>desktop.Run(new Command("menu",command.Text),target,token),token);break;
                    default:PrepareTarget();response=await Task.Run(()=>desktop.Run(command,target,token),token);break;
                }
                token.ThrowIfCancellationRequested();if(Respond!=null)Respond(response+(PendingChoice?". "+ChoiceSummary():""));
            }catch(OperationCanceledException){}catch(Exception e){if(!token.IsCancellationRequested&&Respond!=null)Respond(e.Message);}finally{busy.Release();}
        }
        static Core.Site CurrentSite(IntPtr window,CancellationToken token) {
            token.ThrowIfCancellationRequested();try{
                var root=System.Windows.Automation.AutomationElement.FromHandle(window);var walker=System.Windows.Automation.TreeWalker.ControlViewWalker;var stack=new System.Collections.Generic.Stack<System.Windows.Automation.AutomationElement>();stack.Push(root);var time=System.Diagnostics.Stopwatch.StartNew();int nodes=0;
                while(stack.Count>0&&nodes++<250&&time.ElapsedMilliseconds<200){token.ThrowIfCancellationRequested();var e=stack.Pop();if(e.Current.IsPassword)continue;object pattern;
                    if(e.Current.ControlType==System.Windows.Automation.ControlType.Edit&&e.TryGetCurrentPattern(System.Windows.Automation.ValuePattern.Pattern,out pattern)) {
                        string value=((System.Windows.Automation.ValuePattern)pattern).Current.Value;Uri url;if(Uri.TryCreate(value.Contains("://")?value:"https://"+value,UriKind.Absolute,out url)){
                            var match=Commands.Sites.FirstOrDefault(s=>url.Host==new Uri(s.Home).Host||url.Host.EndsWith("."+new Uri(s.Home).Host,StringComparison.OrdinalIgnoreCase));if(match!=null)return match;
                        }
                    }
                    var child=walker.GetFirstChild(e);while(child!=null&&stack.Count<250&&time.ElapsedMilliseconds<200){stack.Push(child);child=walker.GetNextSibling(child);}
                }
            }catch(System.Windows.Automation.ElementNotAvailableException){}return null;
        }
        async Task OpenURL(string value,string destination,CancellationToken token) {
            Uri url;if(!Uri.TryCreate(value,UriKind.Absolute,out url)||!new[]{"http","https"}.Contains(url.Scheme)||!string.IsNullOrEmpty(url.UserInfo))throw new InvalidOperationException("URL no permitida.");
            token.ThrowIfCancellationRequested();
            if(Native.Browser(Target)) {
                PrepareTarget();IntPtr window=Target;
                if(destination=="ventana"||destination=="pestana"){Native.Chord(KeyNames.TryParse(destination=="ventana"?"CTRL+N":"CTRL+T"));await Task.Delay(300,token);var foreground=Native.GetForegroundWindow();if(!Native.Browser(foreground)||Native.Pid(foreground)!=Native.Pid(window))throw new InvalidOperationException("El navegador cambió; apertura detenida.");window=foreground;}
                await Task.Run(()=>{
                    token.ThrowIfCancellationRequested();
                    if(!DesktopAccess.IsCurrent(window))throw new InvalidOperationException("El foco cambió.");Native.Chord(KeyNames.TryParse("CTRL+L"));
                    var address=System.Windows.Automation.AutomationElement.FocusedElement;
                    if(address==null||address.Current.IsPassword||address.Current.ControlType!=System.Windows.Automation.ControlType.Edit||address.Current.ProcessId!=(int)Native.Pid(window))throw new InvalidOperationException("No se pudo enfocar la barra de direcciones del navegador.");
                    var addressId=address.GetRuntimeId();
                    Native.Unicode(url.AbsoluteUri,()=>{var focused=System.Windows.Automation.AutomationElement.FocusedElement;return DesktopAccess.IsCurrent(window)&&!token.IsCancellationRequested&&focused!=null&&!focused.Current.IsPassword&&focused.GetRuntimeId().SequenceEqual(addressId);});token.ThrowIfCancellationRequested();if(!DesktopAccess.IsCurrent(window))throw new InvalidOperationException("El foco cambió.");Native.Chord(KeyNames.TryParse("ENTER"));
                },token);
            }else{await Task.Run(()=>System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url.AbsoluteUri){UseShellExecute=true}),token);}
            if(Respond!=null)Respond("Destino abierto");
        }
        public void Dispose(){Cancel();cancellation.Dispose();}
    }
}
