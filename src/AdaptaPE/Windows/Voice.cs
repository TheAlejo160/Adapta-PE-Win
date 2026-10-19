using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Speech.AudioFormat;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using System.Threading.Tasks;
using AdaptaPE.Core;

namespace AdaptaPE.Windows
{
    public sealed class Voice:IDisposable
    {
        readonly RecognitionSession session=new RecognitionSession();readonly Preferences settings;readonly WakeState wake=new WakeState();readonly Stopwatch time=Stopwatch.StartNew();
        SpeechRecognitionEngine engine;WaveInput input;SpeechSynthesizer synth;
        Task localWorker;Prompt currentPrompt;volatile bool active,speaking,starting;bool disposed;
        public bool Active {get{return active||starting;}}public bool Speaking {get{return speaking;}}
        public Action<bool> SpeechOutput;public Action Wake;
        public Func<bool> FollowUp;public Action<string> Command,Status,Transcript;public Action<double> Level;
        public Voice(Preferences settings){this.settings=settings;}
        public void Start() {
            if(Active||disposed)return;int token=session.Begin();starting=true;
            if(localWorker!=null&&!localWorker.IsCompleted){if(Status!=null)Status("Cambiando reconocimiento; liberando motor anterior…");_ = StartAfterRelease(token);return;}
            StartEngine(token);
        }
        async Task StartAfterRelease(int token) {
            try{await session.StartAfter(localWorker,token,()=>{try{StartEngine(token);}catch(Exception error){if(Status!=null)Status("Voz detenida: "+error.Message);}});}
            catch(Exception error){if(!session.Current(token))return;Stop();if(Status!=null)Status("Voz detenida: "+error.Message);}
        }
        public void Restart(){if(!Active||disposed)return;CloseRecognition(false);Start();}
        void StartEngine(int token) {
            try {
                RecognizerInfo recognizer=null;
                if(settings.SpeechEngine!="local")try{recognizer=SpeechRecognitionEngine.InstalledRecognizers().Where(r=>r.Culture.TwoLetterISOLanguageName=="es").OrderBy(r=>r.Culture.Name==System.Globalization.CultureInfo.CurrentUICulture.Name?0:r.Culture.Name==System.Globalization.CultureInfo.CurrentCulture.Name?1:r.Culture.Name=="es-ES"?2:3).FirstOrDefault();}catch{if(settings.SpeechEngine=="sapi")throw;}
                if(recognizer==null){if(settings.SpeechEngine=="sapi")throw new InvalidOperationException("No hay reconocedor SAPI español. Selecciona Automático o Local incluido; tener micrófono o una voz TTS no instala reconocimiento.");StartLocal(token);return;}
                engine=new SpeechRecognitionEngine(recognizer);engine.LoadGrammar(new DictationGrammar());
                // ponytail: máximo 600 nombres por sesión; ampliar sólo tras medir carga y precisión de SAPI.
                var names=AppNames.SpokenNames.Concat(new Applications().Catalog().Select(a=>a.Name)).Concat(Applications.OpenWindows().Select(w=>AppNames.Canonical(w.Process))).Distinct().Take(600).ToArray();
                var phrases=Commands.SpeechPhrases.Concat(names.SelectMany(name=>new[]{"abre "+name,"cierra "+name,"cerrar "+name,"cierres "+name,"abras "+name,"cambia a "+name})).Distinct().ToArray();
                var grammar=new GrammarBuilder{Culture=recognizer.Culture};grammar.Append(settings.WakeWord,0,1);grammar.Append(new GrammarBuilder(new Choices("por favor","porfa","puedes","podrías","me puedes","quiero que","necesito que")),0,2);grammar.Append(new Choices(phrases));grammar.Append(new GrammarBuilder(new Choices("por favor","porfa")),0,1);
                engine.LoadGrammar(new Grammar(grammar){Name="Comandos Adapta",Priority=1});
                var activation=new GrammarBuilder{Culture=recognizer.Culture};activation.Append(settings.WakeWord);engine.LoadGrammar(new Grammar(activation){Name="Activación Adapta",Priority=1});
                input=new WaveInput(settings.Microphone);input.Level=value=>{if(session.Current(token)&&Level!=null)Level(value);};engine.SetInputToAudioStream(input,new SpeechAudioFormatInfo(16000,AudioBitsPerSample.Sixteen,AudioChannel.Mono));
                engine.SpeechRecognized+=(s,e)=>{
                    if(!session.Current(token)||!active)return;
                    if(e.Result.Confidence<.45){System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(()=>{if(session.Current(token)&&active&&Status!=null)Status("No entendí con suficiente confianza. Repite la orden o escríbela.");}));return;}
                    Recognized(e.Result.Text,token);
                };
                engine.RecognizeCompleted+=(s,e)=>{
                    if(!session.Current(token)||!active)return;
                    System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(()=>Recover(e.Error??new IOException("SAPI terminó la escucha continua."),token)));
                };
                active=true;starting=false;engine.RecognizeAsync(RecognizeMode.Multiple);if(Status!=null)Status("SAPI · "+recognizer.Culture.DisplayName+" · di «"+settings.WakeWord+"»");
            }catch(Exception error){CloseRecognition(false);if(settings.SpeechEngine!="sapi"&&(error is NotSupportedException||error is COMException||error is InvalidOperationException)){StartLocal(session.Begin());return;}throw;}
        }
        void Recover(Exception error,int token){
            if(!session.Current(token)||!active)return;CloseRecognition(false);
            if(settings.SpeechEngine!="sapi"){try{StartLocal(session.Begin());return;}catch(Exception localError){error=localError;}}
            if(Status!=null)Status("Voz detenida: "+error.Message+" Selecciona Local incluido o revisa el dispositivo de audio.");
        }
        void Recognized(string text,int token){
            System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(()=>{
                if(!session.Current(token)||!active||string.IsNullOrWhiteSpace(text))return;if(Transcript!=null)Transcript(text);bool beep;
                string command=wake.Consume(text,settings.WakeWord,time.Elapsed.TotalSeconds,speaking,FollowUp!=null&&FollowUp(),out beep);
                if(beep){Silence();if(Wake!=null)Wake();System.Media.SystemSounds.Asterisk.Play();if(Status!=null)Status("Escuchando comando durante ocho segundos…");}
                else if(command!=null&&Command!=null)Command(command);
            }));
        }
        void StartLocal(int token){
            var stream=new WaveInput(settings.Microphone);input=stream;stream.Level=value=>{if(session.Current(token)&&Level!=null)Level(value);};starting=true;
            if(Status!=null)Status("Preparando reconocimiento español incluido…");
            localWorker=Task.Run(()=>{
                IntPtr model=IntPtr.Zero,recognizer=IntPtr.Zero;
                try{
                    model=LocalSpeech.Load();if(!session.Current(token))return;recognizer=LocalSpeech.vosk_recognizer_new(model,16000);
                    if(recognizer==IntPtr.Zero)throw new InvalidOperationException("No se pudo iniciar reconocimiento local.");if(!session.Current(token))return;
                    System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(()=>{if(!session.Current(token))return;active=true;starting=false;if(Status!=null)Status("Voz local incluida · di «"+settings.WakeWord+"»");}));
                    var bytes=new byte[4096];double lastPartial=0;string lastText="";
                    while(session.Current(token)){int length=stream.Read(bytes,0,bytes.Length);if(length==0||!session.Current(token))break;
                        int final=LocalSpeech.vosk_recognizer_accept_waveform(recognizer,bytes,length);if(final<0)throw new InvalidOperationException("No se pudo procesar audio local.");
                        if(final==1){Recognized(LocalSpeech.Text(LocalSpeech.vosk_recognizer_result(recognizer),false),token);lastText="";}
                        else if(time.Elapsed.TotalSeconds-lastPartial>=.25){lastPartial=time.Elapsed.TotalSeconds;string text=LocalSpeech.Text(LocalSpeech.vosk_recognizer_partial_result(recognizer),true);if(text!=lastText){lastText=text;System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(()=>{if(session.Current(token)&&active&&Transcript!=null)Transcript(text);}));}}
                    }
                }catch(Exception error){System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(()=>{if(!session.Current(token))return;Stop();if(Status!=null)Status("Voz detenida: "+error.Message);}));}
                finally{if(recognizer!=IntPtr.Zero)LocalSpeech.vosk_recognizer_free(recognizer);if(model!=IntPtr.Zero)LocalSpeech.vosk_model_free(model);stream.Dispose();}
            });
        }
        public void Stop(){CloseRecognition(true);}
        void CloseRecognition(bool cancelWake) {
            session.Cancel();active=false;starting=false;if(cancelWake)wake.Cancel();if(Level!=null)Level(0);
            var old=engine;engine=null;var stream=input;input=null;
            if(stream!=null)stream.Dispose();
            if(old!=null){try{old.RecognizeAsyncCancel();}catch(InvalidOperationException){}old.Dispose();}
            if(Transcript!=null)Transcript("");
        }
        void EnsureSynth() {
            if(synth!=null)return;synth=new SpeechSynthesizer();synth.SetOutputToDefaultAudioDevice();
            var voice=synth.GetInstalledVoices().FirstOrDefault(v=>v.Enabled&&v.VoiceInfo.Culture.TwoLetterISOLanguageName=="es");if(voice!=null)synth.SelectVoice(voice.VoiceInfo.Name);
            synth.SpeakCompleted+=(s,e)=>{var dispatcher=System.Windows.Application.Current?.Dispatcher;if(dispatcher!=null&&!dispatcher.HasShutdownStarted)dispatcher.BeginInvoke(new Action(()=>{if(ReferenceEquals(e.Prompt,currentPrompt)){speaking=false;currentPrompt=null;if(SpeechOutput!=null)SpeechOutput(false);}}));};
        }
        public void Say(string text) {
            if(disposed||string.IsNullOrWhiteSpace(text))return;EnsureSynth();synth.SpeakAsyncCancelAll();speaking=true;currentPrompt=new Prompt(text.Substring(0,Math.Min(6000,text.Length)));try{if(SpeechOutput!=null)SpeechOutput(true);synth.SpeakAsync(currentPrompt);}catch{Silence();throw;}
        }
        public void Silence(){currentPrompt=null;if(synth!=null)synth.SpeakAsyncCancelAll();speaking=false;if(SpeechOutput!=null)SpeechOutput(false);}
        public void Dispose(){disposed=true;Stop();Silence();if(synth!=null)synth.Dispose();}
    }
}
