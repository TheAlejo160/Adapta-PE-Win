using System;
using System.Linq;
using AdaptaPE.Core;

static class Program
{
    static int checks;
    sealed class AudioProbe:LiveAudioStream{public override bool CanRead{get{return true;}}public override int Read(byte[] buffer,int offset,int count){Consumed(count);return count;}}
    static void Check(bool valid,string reason){checks++;if(!valid)throw new Exception(reason);}
    static void Main(){
        var command=Commands.Parse("Computadora");Check(command.Kind=="unknown","Palabra sola no es una acción de escritorio");
        foreach(var phrase in new[]{"cierra Chrome","cerrar la aplicación Google Chrome.","por favor cierra el navegador crom","ciérrame la app Chrome","sierra Chrome","sal de Chrome","puedes cerrar Chrome por favor","por favor puedes cerrar Chrome","cierra por favor Chrome"}){
            command=Commands.Parse(phrase);Check(command.Kind=="closeApp"&&AppNames.Canonical(command.Text)=="google chrome","Cierre admite artículos, imperativo, puntuación y variantes de reconocimiento");
        }
        foreach(var pair in new[]{Tuple.Create("Bloc de notas","notepad"),Tuple.Create("Word","WINWORD"),Tuple.Create("Chrome","chrome"),Tuple.Create("Edge","msedge"),Tuple.Create("Calculadora","CalculatorApp"),Tuple.Create("VS Code","Code")})Check(AppNames.RunningRank(pair.Item1,pair.Item2,"Documento") == 3,"Identifica aplicación por proceso, no por título de documento");
        Check(AppNames.RunningRank("Word","chrome","Página — Microsoft Word")==0,"No cierra navegador por un título que menciona otra app");
        Check(AppNames.RunningRank("Word","wordpad","Microsoft Word")==0,"No confunde un proceso diferente con Word");
        Check(AppNames.RunningRank("Calculadora","ApplicationFrameHost","Calculadora")==2,"Reconoce ventana UWP con proceso contenedor");
        Check(AppNames.RunningRank("app","other","application") == 0&&AppNames.RunningRank("","notepad","Documento")==0,"Nombres vacíos o fragmentos de palabra no cierran ventanas");
        foreach(var phrase in new[]{"cursor más rápido","aumenta la velocidad del mouse","sube sensibilidad del ratón"})Check(Commands.Parse(phrase).Kind=="speed"&&Commands.Parse(phrase).Text=="up","Comandos para acelerar cursor");
        Check(Commands.Parse("modo preciso").Text=="preciso"&&Commands.Parse("modo rápido").Text=="agil","Perfiles por voz");
        Check(Commands.Parse("activar clic automático").Kind=="dwell"&&Commands.Parse("desactivar dwell").Text=="off","Dwell por voz");
        Check(Commands.Parse("activa el cursor").Text=="on"&&Commands.Parse("apaga el mouse cinético").Text=="off","Activación natural de cursor");
        Check(Commands.Parse("pausa el cursor").Text=="on"&&Commands.Parse("reanuda el cursor").Text=="off","Pausa natural de cursor");
        Check(Commands.Parse("calibra el cursor").Kind=="calibrate"&&Commands.Parse("activa el clic automático").Text=="on","Calibración y dwell en imperativo");
        Check(Commands.Parse("detén todo").Kind=="stop","Parada de emergencia en imperativo");
        Check(Commands.Parse("cerrar la ventana").Kind=="shortcut"&&Commands.Parse("cerrar la pestaña").Text=="CTRL+W","Distingue cierre actual de cierre por nombre");
        foreach(var pair in new[]{Tuple.Create("abre una nueva pestaña","CTRL+T"),Tuple.Create("cerrar esta ventana actual","ALT+F4"),Tuple.Create("refresca la página por favor.","CTRL+R"),Tuple.Create("regresa","ALT+LEFT"),Tuple.Create("cambia a la pestaña anterior","CTRL+SHIFT+TAB")})Check(Commands.Parse(pair.Item1).Kind=="shortcut"&&Commands.Parse(pair.Item1).Text==pair.Item2,"Navegación natural equivalente a extensión");
        Check(Commands.Parse("postura cómoda").Kind=="calibrate"&&Commands.Parse("control rostro").Text=="cabeza"&&Commands.Parse("control muñón").Text=="zona libre","Alias cinéticos de extensión");
        Check(Commands.Parse("descansar").Text=="on"&&Commands.Parse("continuar cursor").Text=="off"&&Commands.Parse("cancelar clic").Kind=="cancelClick","Descanso y cancelación de clic por voz");
        Check(Commands.Parse("escribe Ayúdame, por favor!").Text=="Ayúdame, por favor!","Cortesía forma parte del dictado literal");
        Check(Commands.Parse("no hagas clic derecho").Kind=="unknown","No ejecutar clic derecho desde frases negadas");
        command=Commands.Parse("Escribe Hola, José! en el campo Nombre");Check(command.Kind=="writeField"&&command.Text=="Hola, José!"&&command.Target=="Nombre","Dictado conserva Unicode y signos");
        command=Commands.Parse("busca arriba y abajo en Wikipedia");Check(command.Kind=="search"&&command.Text=="arriba y abajo"&&command.Target=="wikipedia","Búsqueda no se interpreta como scroll");
        command=Commands.Parse("abre You Tube en nueva ventana");Check(command.Kind=="open"&&command.Text=="You Tube"&&command.Destination=="ventana","Destino de navegación");
        Check(Commands.FindSite("equis").Id=="x"&&Commands.FindSite("Netflix").Id=="netflix","Alias exactos");Check(Commands.Sites.Length==28,"Catálogo de 28 sitios");
        Check(Commands.FindSite("instagram").Query("Hola José").Contains("site%3Awww.instagram.com"),"Búsqueda alternativa codificada");
        Check(KeyNames.TryParse("control shift N").SequenceEqual(new ushort[]{0x11,0x10,0x4E}),"Atajo nativo");Check(KeyNames.TryParse("cmd.exe /c del")==null,"No acepta shell dictado");
        Check(Commands.Ordinal(Commands.Parse("elige dos").Text)==1&&Commands.Ordinal("el sexto")==5,"Elección por números hablados y ordinales");
        command=Commands.Parse("pregunta  a la ia    ¿Qué es Perú?");Check(command.Kind=="ai"&&command.Text=="¿Qué es Perú?","Consulta conserva Unicode y espacios iniciales no alteran índices");
        bool beep;var wake=new WakeState();Check(wake.Consume("Computadora","Computadora",0,false,false,out beep)==null&&beep,"Activación con señal");Check(wake.Consume("Abre Chrome","Computadora",7,false,false,out beep)=="Abre Chrome","Espera de ocho segundos");
        wake.Consume("Computadora","Computadora",10,false,false,out beep);Check(wake.Consume("abre Chrome","Computadora",19,false,false,out beep)==null,"Activación caducada");Check(wake.Consume("Computadora abre Chrome","Computadora",20,true,false,out beep)==null,"Ignora acciones durante TTS");Check(wake.Consume("Computadora silencio","Computadora",21,true,false,out beep)=="silencio","Interrupción explícita");
        var matrix=ColorMatrices.Native(2);Check(Math.Abs(matrix[0]-.567)<.0001&&Math.Abs(matrix[5]-.433)<.0001&&Math.Abs(matrix[1]-.558)<.0001&&matrix[18]==1&&matrix[24]==1,"Transposición RGB a matriz nativa");
        using(var audio=new AudioProbe()){Check(audio.Length==-1&&!audio.CanSeek&&audio.Position==0,"Contrato SAPI de audio continuo sin longitud finita");Check(audio.Read(new byte[32],0,32)==32,"Captura informa bytes consumidos");Check(audio.Position==32&&audio.Seek(0,System.IO.SeekOrigin.Current)==32,"Posición acumulada permite consulta SAPI");bool rejected=false;try{audio.Position=0;}catch(NotSupportedException){rejected=true;}Check(rejected,"No promete rebobinar audio descartado");}
        var cursor=new Vec(100,100);for(int i=0;i<10;i++){cursor.X+=.2;cursor=cursor.ReconcilePixels(new Vec(Math.Round(cursor.X),100));}Check(Math.Abs(cursor.X-102)<.001,"Cursor conserva fracciones de píxel entre fotogramas");Check(new Vec(-200,100).ReconcilePixels(new Vec(0,100)).X==0,"Cursor respeta posiciones limitadas por Windows");
        foreach(bool partial in new[]{false,true}){var bytes=System.Text.Encoding.UTF8.GetBytes(partial?"{\"partial\":\"computadora\"}\0":"{\"text\":\"Hola, José\"}\0");var pointer=System.Runtime.InteropServices.Marshal.AllocHGlobal(bytes.Length);try{System.Runtime.InteropServices.Marshal.Copy(bytes,0,pointer,bytes.Length);Check(AdaptaPE.Windows.LocalSpeech.Text(pointer,partial)==(partial?"computadora":"Hola, José"),"Reconocimiento local decodifica finales y provisionales UTF-8");}finally{System.Runtime.InteropServices.Marshal.FreeHGlobal(pointer);}}
        foreach(int fps in new[]{5,8,12,15,30,60}) {
            var state=new KineticState();double t=0;bool calibrates=false;
            for(int i=0;i<fps*2;i++){var result=state.Update(new Vec(.5,.5),t,.75,"target");Check(!result.Click&&result.Delta.Length==0,"Calibrar no mueve ni hace clic");calibrates|=result.Calibrated;t+=1.0/fps;}
            Check(calibrates,"Calibración estable");int clicks=0;
            for(int i=0;i<fps;i++){var result=state.Update(new Vec(.8,.5),t,.75,"target");Check(!result.Click,"Movimiento no hace clic");t+=1.0/fps;}
            for(int i=0;i<fps*4;i++){var result=state.Update(new Vec(.5,.5),t,.75,"target");if(result.Click)clicks++;t+=1.0/fps;}
            Check(clicks==1,"Un clic por entrada");state.Lost(t+2);Check(!state.Origin.HasValue,"Pérdida prolongada recalibra");Check(!state.Update(new Vec(double.NaN,.5),t+3,.75,"target").Click,"Valores inválidos no hacen clic");
        }
        var interrupted=new KineticState();double moment=0;
        for(int i=0;i<40;i++){interrupted.Update(new Vec(.5,.5),moment,.75,"target");moment+=.05;}
        for(int i=0;i<20;i++){interrupted.Update(new Vec(.8,.5),moment,.75,"target");moment+=.05;}
        for(int i=0;i<15;i++){interrupted.Update(new Vec(.5,.5),moment,.75,"target");moment+=.05;}
        interrupted.Update(new Vec(.5,.5),moment,.75,null);moment+=.05;int resumedClicks=0;
        for(int i=0;i<50;i++){if(interrupted.Update(new Vec(.5,.5),moment,.75,"target").Click)resumedClicks++;moment+=.05;}
        Check(resumedClicks==1,"Dwell vuelve a progresar después de perder temporalmente el mismo objetivo");
        double referenceTravel=0;
        foreach(int fps in new[]{5,8,12,15,30,60}){
            var motionState=new KineticState{DwellEnabled=false};double t=0;
            for(int i=0;i<fps*2;i++){motionState.Update(new Vec(.5,.5),t,.75,null);t+=1.0/fps;}
            for(int i=0;i<fps*3;i++){motionState.Update(new Vec(.6,.5),t,.75,null);t+=1.0/fps;}
            double travel=0,min=double.MaxValue,max=0;
            for(int i=0;i<fps*2;i++){var motion=motionState.Update(new Vec(.6,.5),t,.75,null);travel+=motion.Delta.X;min=Math.Min(min,motion.Velocity.Length);max=Math.Max(max,motion.Velocity.Length);t+=1.0/fps;}
            Check(min>800&&max-min<1,"Postura constante mantiene velocidad útil y estable");
            if(referenceTravel==0)referenceTravel=travel;else Check(Math.Abs(travel-referenceTravel)/referenceTravel<.01,"Velocidad independiente de FPS dentro de 1 %");
            motionState.CancelClick();var stationary=motionState.Update(new Vec(.5,.5),t,.75,null);
            Check(stationary.Velocity.Length==0&&stationary.Delta.Length==0,"Volver a zona segura frena en la primera muestra");
            for(int i=0;i<fps*2;i++){t+=1.0/fps;stationary=motionState.Update(new Vec(.5,.5),t,.75,null);}
            Check(stationary.Velocity.Length==0&&!stationary.Click,"Postura segura frena sin clic pendiente");
        }
        foreach(var pair in Commands.NaturalShortcuts)Check(Commands.Parse(pair.Key).Kind=="shortcut"&&Commands.Parse(pair.Key).Text==pair.Value,"Atajo natural: "+pair.Key);
        foreach(var pair in Commands.NaturalActions)Check(Commands.Parse(pair.Key).Kind==pair.Value,"Acción natural: "+pair.Key);
        foreach(var prefix in new[]{"","por favor ","oye, ","me puedes ","podrías ","quiero que ","necesito ","hazme el favor de "})
        foreach(var name in new[]{"Chrome","Google","crom","google crom","Google Chrome"}){
            var close=Commands.Parse(prefix+"cerrar la app "+name+" por favor.");Check(close.Kind=="closeApp"&&AppNames.Canonical(close.Text)=="google chrome","Cierre coloquial: "+prefix+name);
            Check(AppNames.RunningRank(close.Text,"chrome","Documento — Google Chrome")==3,"Chrome por proceso incluso con alias coloquial");
        }
        foreach(var name in new[]{"Teams","Discord","Telegram","Zoom","VLC","OBS","Steam","Firefox","Brave","Recortes","Configuración"})
            Check(AppNames.MatchRank(name,AppNames.Canonical(name))==3,"Catálogo amplía alias: "+name);
        Check(AppNames.RunningRank("mi navegador","chrome","Documento")==1,"Navegador genérico exige elección");
        Check(Commands.Parse("abre la página de https://example.org/RutaMayúscula").Text=="https://example.org/RutaMayúscula","URL explícita conserva ruta y mayúsculas");
        Check(Commands.FindSite(AppNames.Clean(Commands.Parse("abre la página de Google").Text)).Id=="google","Sitio explícito conserva resolución real");
        Check(AppNames.RunningRank("chrom","chrome","Word — Google Chrome")==1,"Error aproximado de Chrome exige elección");
        Check(AppNames.RunningRank("Chrome","msedge","Google Chrome")==0,"Otro navegador con título Chrome nunca es Chrome");
        Check(AppNames.MatchRank("","Chrome")==0&&AppNames.MatchRank("cro","Microsoft Word")==0,"Nombres vacíos/cortos no producen parecidos");
        Check(Commands.Parse("cambia a Chrome").Kind=="switchApp"&&Commands.Parse("trae Chrome al frente").Text=="chrome","Enfocar app abierta");
        Check(Commands.Parse("abre la página de Google").Target=="site","Sitio explícito no se confunde con aplicación");
        Check(Commands.Parse("puedes escribir No cierres Chrome, por favor!").Text=="No cierres Chrome, por favor!","Dictado literal conserva cortesía y negación");
        foreach(var phrase in new[]{"no cierres Chrome","nunca cierres Chrome","no hagas un clic derecho","quiero que no leas la pantalla"})Check(Commands.Parse(phrase).Kind=="unknown","Negación no activa comando: "+phrase);
        Check(Commands.Parse("busca cómo cerrar Chrome en Google").Kind=="search","Consulta no ejecuta intención contenida");
        Check(Commands.SpeechPhrases.All(p=>Commands.Parse(p).Kind!="unknown"),"Gramática finita corresponde al parser");
        Check(KeyNames.TryParse("PAGEUP")[0]==0x21&&KeyNames.TryParse("PAGEDOWN")[0]==0x22,"Teclas de avance de página Windows");
        var session=new RecognitionSession();var release=new System.Threading.Tasks.TaskCompletionSource<bool>();int starts=0;
        var firstStart=session.StartAfter(release.Task,session.Begin(),()=>starts++);
        Check(starts==0,"No abre un segundo reconocedor mientras se libera el anterior");
        var secondStart=session.StartAfter(release.Task,session.Begin(),()=>starts+=10);
        release.SetResult(true);System.Threading.Tasks.Task.WhenAll(firstStart,secondStart).GetAwaiter().GetResult();
        Check(starts==10,"Sólo el último cambio de motor inicia reconocimiento");
        release=new System.Threading.Tasks.TaskCompletionSource<bool>();var stoppedStart=session.StartAfter(release.Task,session.Begin(),()=>starts++);
        session.Cancel();release.SetResult(true);stoppedStart.GetAwaiter().GetResult();Check(starts==10,"Detener durante liberación impide reactivación tardía");
        if(Environment.GetCommandLineArgs().Length>1)CheckPackage(Environment.GetCommandLineArgs()[1]);
        Console.WriteLine(checks+" comprobaciones de lógica aprobadas. No validan Windows, sensores ni rendimiento.");
    }
    static void CheckPackage(string folder){
        var version=System.Diagnostics.FileVersionInfo.GetVersionInfo(System.IO.Path.Combine(folder,"AdaptaPE.dll"));
        Check(version.ProductVersion=="1.0","Producto publicado muestra exactamente 1.0");
        Check(version.FileVersion=="1.0.3.0","Revisión de archivo conserva orden de actualización");
        Check(new Version(version.FileVersion)>new Version("1.0.2.0"),"Actualiza desde Base 1.0.2 y Lite 1.0.1 sin bajar la revisión");
    }
}
