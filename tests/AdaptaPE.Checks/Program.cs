using System;
using System.Linq;
using AdaptaPE.Core;

static class Program
{
    static int checks;
    sealed class AudioProbe:LiveAudioStream{public override bool CanRead{get{return true;}}public override int Read(byte[] buffer,int offset,int count){Consumed(count);return count;}}
    static void Check(bool valid,string reason){checks++;if(!valid)throw new Exception(reason);}
    static void Main(){
        CheckPreferences();
        foreach(var word in new[]{"Computadora","Computadoras","Computador"}){
            bool signal;var activation=new WakeState();
            Check(activation.Consume(word+", por favor","computadora",0,false,false,out signal)==null&&signal,"Cortesía sola conserva espera: "+word);
            Check(activation.Consume("Abre Safari","computadora",7.99,false,false,out signal)=="Abre Safari","Espera disponible antes de ocho segundos");
            activation.Consume(word,"computadora",10,false,false,out signal);
            Check(activation.Consume("Abre Safari","computadora",18,false,false,out signal)==null,"Espera vence a los ocho segundos");
            Check(activation.Consume("oye   "+word+" escribe Hola,  Perú!","computadora",20,false,false,out signal)=="escribe Hola,  Perú!","Espacios antes de activación no recortan contenido");
        }
        foreach(var rejected in new[]{"compu","ordenador","supercomputadora","computadoramente"}){bool signal;Check(new WakeState().Consume(rejected+" abre Safari","computadora",0,false,false,out signal)==null&&!signal,"Rechaza activación parcial: "+rejected);}
        bool customSignal;var customWake=new WakeState();
        var invalidClock=new WakeState();invalidClock.Consume("Computadora","computadora",0,false,false,out customSignal);
        Check(invalidClock.Consume("Computadora abre Safari","computadora",double.NaN,false,false,out customSignal)==null&&invalidClock.Consume("abre Safari","computadora",1,false,false,out customSignal)==null,"Reloj inválido cancela la espera de activación");
        Check(customWake.Consume("oye   MA\u0301QUINA   AZUL escribe Hola,  Perú!","Máquina Azul",0,false,false,out customSignal)=="escribe Hola,  Perú!","Activación personal Unicode conserva índices originales");
        Check(new WakeState().Consume("Computadora abre Safari","Asistente",0,false,false,out customSignal)==null,"Palabra personal no activa aliases predeterminados");
        var boundedGesture=new GestureState();var fixedPoint=new Vec(.4,.4);
        boundedGesture.Update(fixedPoint,0,tilt:new Vec(double.NaN,double.PositiveInfinity));
        Check(boundedGesture.Update(fixedPoint,.05,tilt:new Vec(.2,0)).X==.5,"Orientación inválida no contamina el reposo");
        boundedGesture.Reset();boundedGesture.Update(fixedPoint,0,angle:double.PositiveInfinity);
        Check(boundedGesture.Update(fixedPoint,.05,angle:0).X==.5,"Ángulo infinito no bloquea seguimiento");
        boundedGesture.Reset();boundedGesture.Update(fixedPoint,0,angle:-double.MaxValue);
        Check(boundedGesture.Update(fixedPoint,.05,angle:double.MaxValue).Finite,"Ángulos extremos tienen cálculo acotado");
        boundedGesture.Reset();boundedGesture.Update(fixedPoint,0,angle:Math.PI-.01);
        Check(Math.Abs(boundedGesture.Update(fixedPoint,.05,angle:-Math.PI+.01).X-.518)<.000001,"Giro conserva continuidad al cruzar pi");
        foreach(var phrase in new[]{"ábrame Safari","abrí Safari","¿puedes abrir Safari?","podrías abrirme Safari por favor","quiero que abras Safari","me haces el favor de abrir Safari","ayúdame a abrir Safari","oye, porfa abre Safari, pe","oye, ¿puedes abrir Safari?"}){
            var prepared=Commands.Parse(phrase);Check(prepared.Kind=="open"&&prepared.Text=="Safari","Cortesía y conjugaciones: "+phrase);
        }
        Check(Commands.Parse("escribí Ya pe, abre YouTube.").Text=="Ya pe, abre YouTube.","Dictado conserva muletillas y signos");
        Check(Commands.Parse("escribe Hola,  José!").Text=="Hola,  José!","Dictado conserva espacios");
        Check(Commands.Parse("oye, búscame Ají de Gallina en YouTube porfa").Text=="Ají de Gallina","Búsqueda conserva consulta");
        Check(Commands.Parse("por favor no abras Safari").Kind=="unknown","Cortesía no oculta negación");
        foreach(var phrase in new[]{"¿puedes abrir Safari?","escribí Ya pe, abre YouTube.","oye, búscame Ají de Gallina en YouTube porfa","no abras Safari","buscar texto"})Check(Commands.Prepare(Commands.Prepare(phrase))==Commands.Prepare(phrase),"Preparación idempotente: "+phrase);
        Check(Commands.Parse("escribi\u0301 Hola, José!").Text=="Hola, José!","Conjugación con acento descompuesto");
        Check(Commands.Parse("seleccione el texto Hola, José!").Text=="Hola, José!","Selección literal conserva puntuación");
        Check(Commands.Parse("por favor quitar filtro").Kind=="filter"&&Commands.Parse("por favor quitar filtro").Text=="ninguno","Cortesía conserva retirada de filtro");
        Check(Commands.Parse("por favor ya no hables!").Kind=="silence","Cortesía conserva interrupción");
        Check(Commands.Parse("para la lectura").Kind=="silence","«Para la lectura» interrumpe");
        foreach(var off in new[]{"desactiva voz","apaga el micrófono"}){var parsed=Commands.Parse(off);Check(parsed.Kind=="voice"&&parsed.Text=="off","Apaga voz: "+off);bool offBeep;Check(new WakeState().Consume("Computadora, "+off,"Computadora",0,true,false,out offBeep)==off,"Apagar voz llega al ejecutor durante la lectura: "+off);}
        bool readingBeep;Check(new WakeState().Consume("Computadora, abre Chrome","Computadora",0,true,false,out readingBeep)==null,"Durante la lectura no se ejecutan otras órdenes");
        Check(Commands.Parse("pon el texto Hola, por favor.").Text=="Hola, por favor.","Prefijo de dictado histórico conserva signos");
        Check(Commands.Parse("abrir adapta").Kind=="app"&&Commands.Parse("abrir adapta").Text=="abrir adapta","Apertura del panel conserva su acción");
        Check(Commands.Parse("borrar campo").Kind=="clear","Borrado de campo conserva su acción");
        Check(Commands.Parse("busca Adapta PE").Text=="Adapta PE","Búsqueda conserva siglas finales");
        Check(Commands.Parse("busca qué significa pues").Text=="qué significa pues","Búsqueda conserva palabra final literal");
        Check(Commands.Parse("abre Adapta PE").Text=="Adapta PE","Apertura conserva nombre completo");
        Check(Commands.Prepare("oye, ¿puedes abrir Safari?")==Commands.Prepare(Commands.Prepare("oye, ¿puedes abrir Safari?")),"Puntuación interna conserva idempotencia");
        Check(Commands.Parse("oye, ¡detén todo!").Kind=="stop","Puntuación interna no bloquea emergencia");

        foreach(double fraction in new[]{-10.0,0,.5,1,10,double.NaN,double.PositiveInfinity}){
            int x=PanelPlacement.Position(-1920,1920,350,0,true,fraction,false),y=PanelPlacement.Position(-200,1080,600,164,true,fraction,false);Check(x>=-1920&&x+350<=0&&y>=-36&&y+600<=880,"Panel libre y mascota caben con coordenadas negativas");
        }
        Check(PanelPlacement.Position(0,900,600,164,false,0,false)==180&&PanelPlacement.Position(0,900,600,164,false,0,true)==284,"Esquinas reservan espacio para la mascota");
        var activity=new MascotActivity();activity.Touch(10);Check(!activity.ShouldSleep(99)&&activity.ShouldSleep(100),"Descanso sólo tras 90 segundos");activity.Speaking=true;Check(!activity.ShouldSleep(200),"No duerme durante lectura o respuesta");activity.Speaking=false;activity.Touch(200);Check(!activity.ShouldSleep(201),"Interacción reinicia descanso");activity.ManualSleep=true;Check(!activity.ShouldSleep(300),"Sueño manual exige despertar");
        Check(Commands.Parse("une mascota al panel").Text=="join"&&Commands.Parse("separa la mascota del panel").Text=="separate","Unión y separación por voz");Check(Commands.Parse("escribe une mascota al panel").Kind=="dictate","Unión no intercepta dictado");Check(Commands.Parse("mueve el panel a la esquina superior izquierda").Kind=="corner"&&Commands.Parse("panel esquina inferior derecha").Text=="inferior derecha","Posición del panel por voz");
        foreach(var pair in new[]{Tuple.Create("mascota Cu Chu","cuchu"),Tuple.Create("elige mascota tiiinku","tinku"),Tuple.Create("mascota Yaty","yati"),Tuple.Create("oculta la mascota","ninguna"),Tuple.Create("duerme mascota","sleep")})Check(Commands.Parse(pair.Item1).Kind=="mascot"&&Commands.Parse(pair.Item1).Text==pair.Item2,"Comandos de mascota: "+pair.Item1);
        Check(Commands.Parse("escribe mascota Cuchu").Kind=="dictate"&&Commands.Parse("escribe mascota Cuchu").Text=="mascota Cuchu","Mascota no altera dictado");
        Check(Commands.Parse("busca mascota Yati en Google").Kind=="search","Mascota no altera búsqueda");
        foreach(string name in new[]{"tinku","cuchu","yati"}){
            MascotAtlas atlas;using(var file=System.IO.File.OpenRead("assets/mascotas/mascota-"+name+".json"))atlas=(MascotAtlas)new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(MascotAtlas)).ReadObject(file);
            Check(atlas.Valid(1178,1370),"Atlas completo y referencias válidas: "+name);var animation=new MascotPlayback(atlas);animation.Set("wave");for(int i=0;i<100;i++)animation.Advance();Check(animation.State=="idle","Saludo termina en reposo");animation.Set("laptop");for(int i=0;i<100;i++)animation.Advance();Check(animation.State=="idle","Acción termina en reposo");animation.Set("sleep");for(int i=0;i<100;i++)animation.Advance();Check(animation.State=="sleep"&&animation.Resting,"Dormir termina animación y conserva sueño");animation.Set("wake");for(int i=0;i<100;i++)animation.Advance();Check(animation.State=="idle","Despertar termina en reposo");
        }

        foreach(string legacy in new[]{"Automático","Mano izquierda","Brazo derecho","Brazos"})Check(ControlModes.Normalize(legacy)=="Manos","Migra modo antiguo a manos");Check(ControlModes.Normalize("Torso")=="Cabeza","Torso migra a cabeza");
        foreach(double fps in new[]{5.0,15,30,60}){
            var gesture=new GestureState();double t=0;var neutral=new Vec(.2,.7);var a=gesture.Update(neutral,t);Check(a.X==.5&&a.Y==.5,"Gesto inicia centrado");t+=1/fps;a=gesture.Update(new Vec(.23,.7),t);Check(a.X>.65,"Desplazamiento pequeño controla gesto");for(int i=0;i<5;i++){t+=1/fps;a=gesture.Update(new Vec(.23,.7),t);}Check(a.X>.65,"Sostener gesto sostiene dirección");t+=1/fps;a=gesture.Update(neutral,t);Check(a.X==.5&&a.Y==.5,"Volver detiene gesto");a=gesture.Update(new Vec(.9,.9),t);Check(a.X==.5,"Fotograma repetido no mueve");t+=.3;a=gesture.Update(new Vec(.9,.9),t);Check(a.X==.5&&a.Y==.5&&gesture.ResetNow,"Salto temporal reinicia gesto y exige desarmar clic");
            var fixedState=new KineticState{FixedCenter=true};fixedState.Calibrate();Check(fixedState.Origin.HasValue,"Manos no aprende postura de cabeza");Check(!fixedState.Update(new Vec(.5,.5),0,1,"button").Click,"Centro inicial no hace clic");fixedState.Lost(2);Check(!fixedState.ClickArmed,"Pérdida desarma clic");
        }
        var attraction=CursorAttraction.Apply(new Vec(-100,50),new Vec(-80,50),0,.02);Check(attraction.X> -100&&attraction.X< -80,"Atracción local suave y monitores negativos");Check(CursorAttraction.Apply(new Vec(0,0),new Vec(29,0),0,.02).X==0,"No atrae controles lejanos");Check(CursorAttraction.Apply(new Vec(0,0),new Vec(20,0),121,.02).X==0,"Movimiento decidido sale de atracción");
        var slowGesture=new GestureState();slowGesture.Update(new Vec(.2,.7),0,maxGap:.3);Check(slowGesture.Update(new Vec(.23,.7),.23,maxGap:.3).X>.65,"Cinco detecciones tolera una imagen tardía sin reiniciar");Check(slowGesture.Update(new Vec(.25,.7),.6,maxGap:.3).X==.5&&slowGesture.ResetNow,"Pérdida a cinco detecciones reinicia y desarma");
        var zone=new ZoneTracker();var plain=Enumerable.Repeat(50f,19200).ToArray();Check(!zone.Update(plain,0).HasValue,"Zona necesita dos fotogramas");Check(!zone.Update(Enumerable.Repeat(80f,19200).ToArray(),.1).HasValue,"Cambio de luz no adquiere zona");
        var random=new Random(160);var patch=Enumerable.Range(0,900).Select(i=>(float)random.Next(20,230)).ToArray();Func<int,float[]> picture=offset=>{var pixels=(float[])plain.Clone();for(int y=0;y<30;y++)for(int x=0;x<30;x++)pixels[(40+y)*160+30+x+offset]=patch[y*30+x];return pixels;};
        zone.Reset();zone.Update(picture(0),0);var tracked=zone.Update(picture(2),.1);Check(tracked.HasValue,"Adquiere detalle local fuera del centro");var next=zone.Update(picture(4),.2);Check(next.HasValue&&Math.Abs(next.Value.X-tracked.Value.X-2.0/160)<.001,"Sigue la misma textura");Check(!zone.Update(plain,.3).HasValue,"Pérdida de textura detiene cursor");Check(!zone.Update(plain,1.6).HasValue,"Oclusión larga descarta textura");

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
            var state=new KineticState{MaxGap=fps==5?.3:.2};double t=0;bool calibrates=false;
            for(int i=0;i<fps*2;i++){var result=state.Update(new Vec(.5,.5),t,.75,"target");Check(!result.Click&&result.Delta.Length==0,"Calibrar no mueve ni hace clic");calibrates|=result.Calibrated;t+=1.0/fps;}
            Check(calibrates,"Calibración estable");int clicks=0;
            for(int i=0;i<fps;i++){var result=state.Update(new Vec(.8,.5),t,.75,"target");Check(!result.Click,"Movimiento no hace clic");t+=1.0/fps;}
            for(int i=0;i<fps*4;i++){var result=state.Update(new Vec(.5,.5),t,.75,"target");if(result.Click)clicks++;t+=1.0/fps;}
            Check(clicks==1,"Un clic por entrada");state.Lost(t+2);Check(!state.Origin.HasValue,"Pérdida prolongada recalibra");Check(!state.Update(new Vec(double.NaN,.5),t+3,.75,"target").Click,"Valores inválidos no hacen clic");
        }
        var jump=new KineticState();double jt=0;
        for(int i=0;i<60;i++){jump.Update(new Vec(.5,.5),jt,.75,"target");jt+=1.0/30;}
        for(int i=0;i<30;i++){jump.Update(new Vec(.8,.5),jt,.75,"target");jt+=1.0/30;}
        while(jump.Progress==0&&jt<10){jump.Update(new Vec(.5,.5),jt,.75,"target");jt+=1.0/30;}
        Check(jump.Progress>0,"Permanencia iniciada antes del salto");jt+=.25;Check(!jump.Update(new Vec(.5,.5),jt,.75,"target").Click&&jump.Progress==0,"Salto de 250 ms desarma la permanencia");
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
        Check(Commands.SpeechPhrases.All(p=>Commands.Parse(p).Kind!="unknown"),"Gramática finita corresponde al parser: "+string.Join(", ",Commands.SpeechPhrases.Where(p=>Commands.Parse(p).Kind=="unknown")));
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
    static void CheckPreferences(){
        // Endpoint del proveedor: HTTPS o loopback real; la clave nunca pasa a otro destino (sin red ni claves reales).
        foreach(var accepted in new[]{"https://ejemplo.test/v1/chat/completions","http://localhost:11434/v1/chat/completions","http://127.12.34.56:8080/v1","http://[::1]:8080/v1"})Check(AdaptaPE.Windows.Preferences.ProviderEndpoint(accepted)!=null,"Endpoint válido: "+accepted);
        foreach(var rejected in new[]{"http://ejemplo.test/v1","http://localhost.evil.com/v1","http://127.0.0.1.evil.com/v1","http://127.256.0.1/v1","http://128.0.0.1/v1","https://ejemplo.test/v1?key=x","https://usuario:clave@ejemplo.test/v1","ftp://localhost/v1"})Check(AdaptaPE.Windows.Preferences.ProviderEndpoint(rejected)==null,"Endpoint rechazado: "+rejected);
        var endpointPrefs=new AdaptaPE.Windows.Preferences();bool deleted=false;
        endpointPrefs.ChangeEndpoint("https://otro.test/v1",()=>deleted=true);Check(deleted&&endpointPrefs.Endpoint=="https://otro.test/v1","Cambiar endpoint borra la clave anterior");
        try{endpointPrefs.ChangeEndpoint("https://rechazado.test/v1",()=>{throw new System.IO.IOException("bloqueado");});Check(false,"Fallo al borrar la clave debe avisar");}catch(InvalidOperationException){}
        Check(endpointPrefs.Endpoint=="https://otro.test/v1","Si no se borra la clave se conserva el endpoint anterior");
        string path=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"AdaptaPE-checks-"+Guid.NewGuid().ToString("N")+".json");
        var serializer=new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(AdaptaPE.Windows.Preferences));
        try{
            Check(AdaptaPE.Windows.Preferences.Load(path).WakeWord=="Computadora","Preferencias ausentes usan valores iniciales");
            foreach(var wake in new[]{null,"", "   ",new string('a',41)}){
                var invalid=new AdaptaPE.Windows.Preferences{WakeWord=wake,Microphone=-2,SpeechEngine="inválido",Filter="inválido"};
                using(var file=System.IO.File.Create(path))serializer.WriteObject(file,invalid);
                var loaded=AdaptaPE.Windows.Preferences.Load(path);
                Check(loaded.WakeWord=="Computadora","Activación inválida se restaura sin excepción");
                Check(loaded.Microphone==-1,"Micrófono menor que -1 se restaura");
                Check(loaded.SpeechEngine=="auto","Motor desconocido usa selección automática");
                Check(loaded.Filter=="ninguno","Filtro desconocido no se activa");
            }
            System.IO.File.WriteAllText(path,"{}");
            var missing=AdaptaPE.Windows.Preferences.Load(path);
            Check(missing.WakeWord=="Computadora"&&missing.SpeechEngine=="auto"&&missing.Filter=="ninguno","Campos omitidos se validan tras deserializar");
            foreach(var engine in new[]{"auto","local","sapi"})foreach(var filter in ColorMatrices.Names){
                var valid=new AdaptaPE.Windows.Preferences{WakeWord=new string('á',40),Microphone=engine=="auto"?-1:2,SpeechEngine=engine,Filter=filter};
                using(var file=System.IO.File.Create(path))serializer.WriteObject(file,valid);
                var loaded=AdaptaPE.Windows.Preferences.Load(path);
                Check(loaded.WakeWord==valid.WakeWord&&loaded.Microphone==valid.Microphone&&loaded.SpeechEngine==engine&&loaded.Filter==filter,"Preferencias válidas se conservan: "+engine+" / "+filter);
            }
            System.IO.File.WriteAllText(path,"{inválido");
            Check(AdaptaPE.Windows.Preferences.Load(path).WakeWord=="Computadora","JSON dañado conserva recuperación segura");
        }finally{if(System.IO.File.Exists(path))System.IO.File.Delete(path);}
    }
    static void CheckPackage(string folder){
        var version=System.Diagnostics.FileVersionInfo.GetVersionInfo(System.IO.Path.Combine(folder,"AdaptaPE.dll"));
        Check(version.ProductVersion=="1.0","Producto publicado muestra exactamente 1.0");
        Check(version.FileVersion=="1.0.3.0","Revisión de archivo conserva orden de actualización");
        Check(new Version(version.FileVersion)>new Version("1.0.2.0"),"Actualiza desde Base 1.0.2 y Lite 1.0.1 sin bajar la revisión");
    }
}
