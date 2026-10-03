using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Automation;
using AdaptaPE.Core;

namespace AdaptaPE.Windows
{
    public sealed class DesktopAccess
    {
        public Action<Rect?> Highlight;
        public Action<string> ReadResponse;
        public bool HasSelection {get{return selected!=null;}}
        public Action<string[],Action<int>> Choices;
        AutomationElement selected;IntPtr selectionWindow;
        public void CancelSelection(){selected=null;selectionWindow=IntPtr.Zero;if(Highlight!=null)Highlight(null);}
        static bool Safe(AutomationElement e){try{return e!=null&&!e.Current.IsPassword&&e.Current.IsEnabled&&!e.Current.IsOffscreen&&e.Current.ProcessId!=Process.GetCurrentProcess().Id;}catch(ElementNotAvailableException){return false;}}
        static string Label(AutomationElement e) {
            if(!Safe(e))return "";string n=e.Current.Name??"";
            if(string.IsNullOrWhiteSpace(n)){var label=e.Current.LabeledBy;if(label!=null&&!label.Current.IsPassword)n=label.Current.Name;}
            if(string.IsNullOrWhiteSpace(n))n=e.Current.AutomationId??"";
            return n.Length>300?n.Substring(0,300):n;
        }
        public static bool IsCurrent(IntPtr window){return window!=IntPtr.Zero&&Native.IsWindow(window)&&Native.GetForegroundWindow()==window&&Native.DefaultDesktop();}
        static void Require(IntPtr window,CancellationToken token){token.ThrowIfCancellationRequested();if(!IsCurrent(window))throw new InvalidOperationException("La ventana cambió; acción cancelada.");}
        List<AutomationElement> Controls(IntPtr window,CancellationToken token) {
            Require(window,token);var root=AutomationElement.FromHandle(window);var walker=TreeWalker.ControlViewWalker;
            var pending=new Stack<AutomationElement>();var results=new List<AutomationElement>();pending.Push(root);var time=Stopwatch.StartNew();int count=0;
            // ponytail: recorrido limitado; aislar proveedor UIA en proceso separado si sus llamadas COM se bloquean.
            while(pending.Count>0&&count++<1800&&time.ElapsedMilliseconds<800){token.ThrowIfCancellationRequested();var e=pending.Pop();try {
                if(e.Current.IsPassword)continue;if(Safe(e))results.Add(e);
                var child=walker.GetFirstChild(e);while(child!=null&&pending.Count<1800&&time.ElapsedMilliseconds<800){pending.Push(child);child=walker.GetNextSibling(child);}
            }catch(ElementNotAvailableException){}}
            Require(window,token);return results;
        }
        static bool Field(AutomationElement e){object p;return Safe(e)&&(e.TryGetCurrentPattern(ValuePattern.Pattern,out p)&&!((ValuePattern)p).Current.IsReadOnly||e.Current.ControlType==ControlType.Edit);}
        static bool Invokable(AutomationElement e){object p;return Safe(e)&&(e.TryGetCurrentPattern(InvokePattern.Pattern,out p)||e.TryGetCurrentPattern(SelectionItemPattern.Pattern,out p)||e.TryGetCurrentPattern(TogglePattern.Pattern,out p)||e.TryGetCurrentPattern(ExpandCollapsePattern.Pattern,out p));}
        static List<AutomationElement> Matches(IEnumerable<AutomationElement> elements,string name) {
            string n=Commands.Normalize(name);var a=elements.Where(Safe).ToList();var exact=a.Where(e=>Commands.Normalize(Label(e))==n).ToList();
            return exact.Count>0?exact:a.Where(e=>n.Length>0&&Commands.Normalize(Label(e)).Contains(n)).ToList();
        }
        string Pick(List<AutomationElement> elements,IntPtr window,CancellationToken token,Action<AutomationElement> action,bool force=false) {
            if(elements.Count==0)throw new InvalidOperationException("No encontré un control accesible. Usa muestra campos o un atajo conocido.");
            if(elements.Count==1&&!force){Require(window,token);action(elements[0]);return "Listo";}
            var choices=elements.Where(e=>!string.IsNullOrWhiteSpace(Label(e))).Take(60).ToArray();
            if(choices.Length==0)throw new InvalidOperationException("Los controles no publican nombres accesibles.");
            if(Choices!=null)Choices(choices.Select(Label).ToArray(),i=>{Require(window,token);if(i<0||i>=choices.Length)throw new ArgumentException("Opción no disponible.");action(choices[i]);});
            return "Elige un número o un nombre. Di siguiente para ver más opciones.";
        }
        public string Run(Command command,IntPtr window,CancellationToken token) {
            Require(window,token);
            if(command.Kind=="pressSelection") {
                if(selected==null||selectionWindow!=window||!Safe(selected))throw new InvalidOperationException("La selección ya no está disponible.");
                var e=selected;CancelSelection();Invoke(e,window,token);return "Control activado";
            }
            if(command.Kind=="dictate"||command.Kind=="clear"||command.Kind=="selectText") {
                var focused=AutomationElement.FocusedElement;
                if(!Field(focused)||focused.Current.ProcessId!=(int)Native.Pid(window))throw new InvalidOperationException("Enfoca un campo editable no protegido.");
                if(command.Kind=="selectText") {
                    object p;if(!focused.TryGetCurrentPattern(TextPattern.Pattern,out p))throw new InvalidOperationException("Este campo no permite seleccionar texto mediante accesibilidad.");
                    var range=((TextPattern)p).DocumentRange.FindText(command.Text,false,true);if(range==null)throw new InvalidOperationException("No encontré ese texto.");Require(window,token);range.Select();return "Texto seleccionado";
                }
                Write(focused,command.Kind=="clear"?"":command.Text,window,token,command.Kind=="clear");return command.Kind=="clear"?"Campo borrado":"Texto insertado";
            }
            var all=Controls(window,token);
            if(command.Kind=="readScreen") {
                var text=new List<string>();int chars=0;
                foreach(var e in all){token.ThrowIfCancellationRequested();if(!Safe(e))continue;string line=Label(e);object p;
                    if(Field(e)&&e.TryGetCurrentPattern(ValuePattern.Pattern,out p))line+=" "+((ValuePattern)p).Current.Value;
                    if(line.Length>0&&!text.Contains(line)){line=line.Substring(0,Math.Min(500,line.Length));text.Add(line);chars+=line.Length;}if(chars>=6000)break;
                }
                return text.Count>0?string.Join(". ",text):"La aplicación no publica contenido legible por accesibilidad.";
            }
            if(command.Kind=="fields")return Pick(all.Where(Field).ToList(),window,token,e=>Focus(e,window,token),true);
            if(command.Kind=="actions")return Pick(all.Where(Invokable).ToList(),window,token,e=>Invoke(e,window,token),true);
            if(command.Kind=="media"){var candidates=all.Where(Invokable).Where(e=>new[]{"play","reproducir","resume","continuar","pause","pausa","pausar"}.Any(n=>Commands.Normalize(Label(e)).Contains(n))).Where(e=>command.Text=="pausar"?Commands.Normalize(Label(e)).Contains("paus"):!Commands.Normalize(Label(e)).Contains("paus")).ToList();return Pick(candidates,window,token,e=>Invoke(e,window,token));}
            var items=Matches(command.Kind=="writeField"||command.Kind=="focus"?all.Where(Field):command.Kind=="menu"?all.Where(e=>e.Current.ControlType==ControlType.MenuItem):all,command.Kind=="writeField"?command.Target:command.Text);
            string picked=Pick(items,window,token,e=>{
                if(command.Kind=="select"){selected=e;selectionWindow=window;if(Highlight!=null)Highlight(e.Current.BoundingRectangle);return;}
                if(command.Kind=="focus"){Focus(e,window,token);return;}
                if(command.Kind=="writeField"){Focus(e,window,token);Write(e,command.Text,window,token,false);return;}
                if(command.Kind=="read"){if(items.Count>1&&ReadResponse!=null)ReadResponse(Read(e));return;}
                Invoke(e,window,token);
            });
            if(items.Count==1&&command.Kind=="read")return Read(items[0]);
            if(items.Count==1&&command.Kind=="select")return "Seleccionado "+Label(items[0]);
            return picked;
        }
        static string Read(AutomationElement e){if(!Safe(e))return "Control no disponible";object p;string text=Label(e);if(e.TryGetCurrentPattern(ValuePattern.Pattern,out p))text+=" "+((ValuePattern)p).Current.Value;return text;}
        static void Focus(AutomationElement e,IntPtr window,CancellationToken token){Require(window,token);if(!Safe(e)||!e.Current.IsKeyboardFocusable)throw new InvalidOperationException("Este control no admite foco.");e.SetFocus();}
        static void Write(AutomationElement e,string text,IntPtr window,CancellationToken token,bool clear) {
            Require(window,token);if(!Field(e))throw new InvalidOperationException("Campo protegido o no editable.");
            if(clear){object p;if(e.TryGetCurrentPattern(ValuePattern.Pattern,out p)){((ValuePattern)p).SetValue("");return;}Focus(e,window,token);Native.Chord(KeyNames.TryParse("CTRL+A"));Require(window,token);Native.Chord(KeyNames.TryParse("DELETE"));return;}
            Focus(e,window,token);var id=e.GetRuntimeId();
            Native.Unicode(text,()=>{token.ThrowIfCancellationRequested();var f=AutomationElement.FocusedElement;return IsCurrent(window)&&Safe(f)&&f.GetRuntimeId().SequenceEqual(id);});
        }
        static void Invoke(AutomationElement e,IntPtr window,CancellationToken token) {
            Require(window,token);if(!Safe(e)||e.Current.ProcessId!=(int)Native.Pid(window))throw new InvalidOperationException("Control no disponible en la ventana de destino.");object p;
            if(e.TryGetCurrentPattern(InvokePattern.Pattern,out p)){((InvokePattern)p).Invoke();return;}
            if(e.TryGetCurrentPattern(SelectionItemPattern.Pattern,out p)){((SelectionItemPattern)p).Select();return;}
            if(e.TryGetCurrentPattern(TogglePattern.Pattern,out p)){((TogglePattern)p).Toggle();return;}
            if(e.TryGetCurrentPattern(ExpandCollapsePattern.Pattern,out p)){((ExpandCollapsePattern)p).Expand();return;}
            throw new InvalidOperationException("El control no admite activación accesible. Usa cursor o atajo.");
        }
        public static Tuple<string,string,Rect,IntPtr> AtCursor() {
            Native.Point p;if(!Native.GetCursorPos(out p))return null;var window=Native.RootAtCursor();if(window==IntPtr.Zero||!Native.DefaultDesktop())return null;
            // Un clic por postura también puede apuntar a superficies sin semántica UIA.
            var cell=new Vec(Math.Floor(p.X/24.0)*24,Math.Floor(p.Y/24.0)*24);
            var fallback=Tuple.Create("pixel:"+window+":"+cell.X+":"+cell.Y,"",new Rect(cell.X,cell.Y,24,24),window);
            try{var e=AutomationElement.FromPoint(new Point(p.X,p.Y));if(e==null||!e.Current.IsEnabled||e.Current.IsOffscreen)return null;var bounds=e.Current.BoundingRectangle;
                if(bounds.IsEmpty||!bounds.Contains(p.X,p.Y))return fallback;
                string id=e.Current.ProcessId+":"+string.Join(".",e.GetRuntimeId());string text="";
                if(Safe(e))try{text=Read(e);}catch(System.Runtime.InteropServices.COMException){}
                return Tuple.Create(id,text,bounds,window);
            }catch(ElementNotAvailableException){return fallback;}catch(InvalidOperationException){return fallback;}catch(System.Runtime.InteropServices.COMException){return fallback;}
        }
    }
}
