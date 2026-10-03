using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using AdaptaPE.Core;
using AdaptaPE.Vision;
using AdaptaPE.Windows;
using Forms=System.Windows.Forms;

namespace AdaptaPE
{
    public sealed class MainWindow:Window
    {
        internal static string ProductVersion {get{return ((System.Reflection.AssemblyInformationalVersionAttribute)Attribute.GetCustomAttribute(typeof(MainWindow).Assembly,typeof(System.Reflection.AssemblyInformationalVersionAttribute))).InformationalVersion.Split('+')[0];}}
        readonly App app;readonly Preferences settings=Preferences.Load();readonly Voice voice;readonly Camera camera;readonly Filters filters=new Filters();readonly OptionalAI ai;readonly DesktopAccess desktop=new DesktopAccess();readonly Actions actions;
        Forms.NotifyIcon tray;Native.WinEventProc foregroundProc;IntPtr foregroundHook;HwndSource hwnd;FloatingWindow floating;HighlightWindow highlight;DwellWindow dwellWindow;
        readonly DispatcherTimer pointerTimer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(200)};
        readonly DispatcherTimer choiceTimer=new DispatcherTimer{Interval=TimeSpan.FromSeconds(1)};
        int pointerBusy,previewPending,pointerGeneration;bool suppress,suspended,quitting,cleaned;CancellationTokenSource pending=new CancellationTokenSource();
        bool restoreVoice,restoreCamera,restoreTalk;int restoreFilter;string lastPointerId,lastReadId;double hoverSince;string[] choices=new string[0];
        TextBlock status,result,transcript,voiceState,cameraState;CheckBox voiceToggle,cameraToggle,talkToggle,screenConsent,dwellToggle,tremorToggle;ComboBox filterChoice,modeChoice;ProgressBar dwellProgress,microphoneLevel;Image preview;TextBlock previewEmpty;TabControl tabs;WrapPanel choicePanel;
        volatile bool previewVisible;Slider speedSlider,amplitudeSlider;
        readonly byte[] previewPixels=new byte[Camera.PreviewWidth*Camera.PreviewHeight*3];WriteableBitmap livePreview;
        public MainWindow(App app) {
            this.app=app;voice=new Voice(settings);camera=new Camera(settings);ai=new OptionalAI(settings);actions=new Actions(desktop);
            Title="Adapta PE · Windows";Width=960;Height=800;MinWidth=760;MinHeight=620;Icon=Theme.Asset("AdaptaPE.ico");WindowStartupLocation=WindowStartupLocation.CenterScreen;
            FontFamily=new FontFamily("Segoe UI");FontSize=16;Background=new ImageBrush(Theme.Asset("background.png")){Stretch=Stretch.UniformToFill};Foreground=new SolidColorBrush(Color.FromRgb(27,39,44));
            Build();Closing+=(s,e)=>{if(!quitting){e.Cancel=true;Hide();if(settings.VoiceActive||settings.CameraActive)ShowFloating();}};
            SourceInitialized+=(s,e)=>{hwnd=HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);if(hwnd==null)throw new InvalidOperationException("No se pudo obtener la ventana nativa de Adapta PE.");hwnd.AddHook(WindowMessage);if(!Native.RegisterHotKey(hwnd.Handle,160,0x4000|0x0002|0x0004,0x53))Report("Atajo de emergencia Ctrl+Shift+S ocupado; Detener todo sigue disponible en bandeja y panel.");};
        }
        void Do(Action action){if(!cleaned&&!Dispatcher.HasShutdownStarted){if(Dispatcher.CheckAccess())action();else Dispatcher.BeginInvoke(action);}}
        void Build() {
            var backdrop=new Grid{Background=new SolidColorBrush(Color.FromArgb(85,0,20,52))};Content=backdrop;
            var root=new Grid{Margin=new Thickness(22)};root.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});root.RowDefinitions.Add(new RowDefinition());backdrop.Children.Add(root);
            var header=new DockPanel{Margin=new Thickness(0,0,0,18)};root.Children.Add(header);
            var panel=Button("Panel flotante · PiP",ShowFloating);DockPanel.SetDock(panel,Dock.Right);header.Children.Add(panel);header.Children.Add(Theme.Logo(64));
            var title=new StackPanel();var name=Text("Adapta PE",32,true);name.Foreground=Brushes.White;title.Children.Add(name);var subtitle=Text("Tu escritorio, a tu ritmo · Base "+ProductVersion,16);subtitle.Foreground=Brushes.White;title.Children.Add(subtitle);header.Children.Add(title);
            var card=new Border{Background=new SolidColorBrush(Color.FromRgb(248,250,252)),CornerRadius=new CornerRadius(20),Padding=new Thickness(20)};Grid.SetRow(card,1);root.Children.Add(card);
            var inside=new Grid();inside.RowDefinitions.Add(new RowDefinition());inside.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});card.Child=inside;
            tabs=new TabControl();inside.Children.Add(tabs);AddTab("Control",ControlPage());AddTab("Lectura y color",FilterPage());AddTab("IA opcional",AIPage());AddTab("Ayuda",HelpPage());
            tabs.SelectionChanged+=(s,e)=>RefreshPreviewVisibility();IsVisibleChanged+=(s,e)=>RefreshPreviewVisibility();
            var footer=new StackPanel{Margin=new Thickness(0,14,0,0)};Grid.SetRow(footer,1);inside.Children.Add(footer);
            status=Text("Todo apagado. Activa lo que necesitas.",14);
#if !NET461
            AutomationProperties.SetLiveSetting(status,AutomationLiveSetting.Polite);
#endif
            footer.Children.Add(status);result=Text("",14);result.MaxHeight=70;footer.Children.Add(result);choicePanel=new WrapPanel();footer.Children.Add(choicePanel);
            var bottom=new DockPanel();var stop=Button("Detener todo",()=>StopAll(true),true);DockPanel.SetDock(stop,Dock.Right);bottom.Children.Add(stop);bottom.Children.Add(Text("Cerrar ajustes mantiene asistencia y panel.",14));footer.Children.Add(bottom);
        }
        void RefreshPreviewVisibility(){previewVisible=(IsVisible&&tabs.SelectedIndex==0)||(floating!=null&&floating.IsVisible);}
        StackPanel ControlPage(){
            var p=Page();var columns=new Grid();columns.ColumnDefinitions.Add(new ColumnDefinition());columns.ColumnDefinitions.Add(new ColumnDefinition());p.Children.Add(columns);
            var voiceCard=new Border{Child=VoicePage(),Background=Brushes.White,CornerRadius=new CornerRadius(14),Padding=new Thickness(16),Margin=new Thickness(0,0,8,0)};columns.Children.Add(voiceCard);
            var cameraCard=new Border{Child=CameraPage(),Background=Brushes.White,CornerRadius=new CornerRadius(14),Padding=new Thickness(16),Margin=new Thickness(8,0,0,0)};Grid.SetColumn(cameraCard,1);columns.Children.Add(cameraCard);return p;
        }
        static Expander More(StackPanel parent,string title){var item=new Expander{Header=title,Margin=new Thickness(0,12,0,8),FontWeight=FontWeights.SemiBold,MinHeight=44};parent.Children.Add(item);return item;}
        static TextBlock Text(string text,double size=15,bool bold=false){return new TextBlock{Text=text,FontSize=size,FontWeight=bold?FontWeights.SemiBold:FontWeights.Normal,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,0,10)};}
        static Button Button(string text,Action click,bool danger=false){var b=new Button{Content=text,Padding=new Thickness(14,8,14,8),Margin=new Thickness(0,0,8,8),MinHeight=44};if(danger){b.Background=new SolidColorBrush(Color.FromRgb(151,32,49));b.Foreground=Brushes.White;}AutomationProperties.SetName(b,text);b.Click+=(s,e)=>click();return b;}
        static StackPanel Page(){return new StackPanel{Margin=new Thickness(6,10,6,8)};}
        void AddTab(string title,StackPanel page){tabs.Items.Add(new TabItem{Header=title,Content=new ScrollViewer{Content=page,VerticalScrollBarVisibility=ScrollBarVisibility.Auto}});}
        void Heading(StackPanel page,string title,string description){page.Children.Add(Text(title,23,true));page.Children.Add(Text(description,14));}
        CheckBox Check(StackPanel page,string text,bool value,Action<bool> change){var c=new CheckBox{Content=text,IsChecked=value,Margin=new Thickness(0,4,0,14),Padding=new Thickness(4),MinHeight=44};c.Checked+=(s,e)=>{if(!suppress)change(true);};c.Unchecked+=(s,e)=>{if(!suppress)change(false);};page.Children.Add(c);return c;}
        TextBox Input(StackPanel page,string label,string value,Action<string> change){page.Children.Add(Text(label,14,true));var box=new TextBox{Text=value??"",MinHeight=36,Padding=new Thickness(8),Margin=new Thickness(0,0,0,14)};AutomationProperties.SetName(box,label);box.LostKeyboardFocus+=(s,e)=>{if(!suppress)Try(()=>{change(box.Text);Save();});};page.Children.Add(box);return box;}
        ComboBox Combo(StackPanel page,string label,string[] values,int selected,Action<int> change){page.Children.Add(Text(label,14,true));var box=new ComboBox{ItemsSource=values,SelectedIndex=selected,MinHeight=36,Margin=new Thickness(0,0,0,14)};AutomationProperties.SetName(box,label);box.SelectionChanged+=(s,e)=>{if(!suppress&&box.SelectedIndex>=0)Try(()=>{change(box.SelectedIndex);Save();});};page.Children.Add(box);return box;}
        Slider Slider(StackPanel page,string label,double minimum,double maximum,double value,Action<double> change){var line=Text(label+": "+value.ToString("0.0"),14,true);page.Children.Add(line);var slider=new Slider{Minimum=minimum,Maximum=maximum,Value=value,TickFrequency=.1,Margin=new Thickness(0,0,0,16),IsSnapToTickEnabled=true};AutomationProperties.SetName(slider,label);slider.ValueChanged+=(s,e)=>{line.Text=label+": "+e.NewValue.ToString("0.0");change(e.NewValue);camera.Configure();};slider.LostMouseCapture+=(s,e)=>Save();slider.LostKeyboardFocus+=(s,e)=>Save();page.Children.Add(slider);return slider;}
        StackPanel VoicePage(){var p=Page();Heading(p,"Control por voz","Di «Computadora, abre Bloc de notas». Reconocimiento y audio permanecen en tu equipo.");
            voiceToggle=Check(p,"Activar voz",false,v=>Try(()=>SetVoice(v)));voiceState=Text("Micrófono apagado",14);p.Children.Add(voiceState);
            p.Children.Add(Text("Señal del micrófono",14,true));microphoneLevel=new ProgressBar{Minimum=0,Maximum=1,Height=10,Margin=new Thickness(0,0,0,12)};AutomationProperties.SetName(microphoneLevel,"Nivel de audio del micrófono");p.Children.Add(microphoneLevel);
            transcript=Text("Aquí verás lo que escuché",14);transcript.MinHeight=44;p.Children.Add(transcript);
            var preference=Page();More(p,"Dispositivos y reconocimiento").Content=preference;
            var microphones=WaveInput.Devices();Combo(preference,"Micrófono",microphones,Math.Min(microphones.Length-1,Math.Max(0,settings.Microphone+1)),i=>{ChangeVoice(()=>settings.Microphone=i-1);});
            Combo(preference,"Reconocimiento de voz",new[]{"Automático · SAPI o local incluido","Local incluido · español","SAPI instalado en Windows"},Math.Max(0,Array.IndexOf(new[]{"auto","local","sapi"},settings.SpeechEngine)),i=>{ChangeVoice(()=>settings.SpeechEngine=new[]{"auto","local","sapi"}[i]);});
            Input(preference,"Nombre para llamar al asistente",settings.WakeWord,v=>{if(string.IsNullOrWhiteSpace(v)||v.Length>40)throw new ArgumentException("Usa un nombre de 1 a 40 caracteres.");ChangeVoice(()=>settings.WakeWord=v.Trim());});
            preference.Children.Add(Button("Permiso del micrófono",()=>OpenSettings("ms-settings:privacy-microphone")));preference.Children.Add(Button("Dispositivos de sonido",()=>OpenSettings("ms-settings:sound")));
            preference.Children.Add(Text("Local incluido funciona sin instalar un reconocedor de Windows. SAPI necesita un motor español; una voz TTS no basta.",14));
            var typed=Page();More(p,"Escribir una orden").Content=typed;typed.Children.Add(Text("Enfoca primero la aplicación de destino.",14));var command=new TextBox{MinHeight=44,Padding=new Thickness(8),Margin=new Thickness(0,0,0,12)};AutomationProperties.SetName(command,"Orden para la aplicación externa");typed.Children.Add(command);
            Action execute=()=>{string text=command.Text;command.Clear();Run(text);};command.KeyDown+=(s,e)=>{if(e.Key==System.Windows.Input.Key.Enter){e.Handled=true;execute();}};typed.Children.Add(Button("Ejecutar orden",execute));return p;
        }
        void OpenSettings(string uri){Try(()=>Process.Start(new ProcessStartInfo(uri){UseShellExecute=true}));}
        StackPanel CameraPage(){var p=Page();Heading(p,"Cursor con cámara","Mantén una postura cómoda al calibrar. La vista muestra tu movimiento y la zona segura.");
            cameraToggle=Check(p,"Activar cursor con cámara",false,v=>Try(()=>SetCamera(v)));cameraState=Text("Cámara apagada",14);p.Children.Add(cameraState);
            preview=new Image{Height=210,Stretch=Stretch.Uniform};AutomationProperties.SetName(preview,"Vista local de cámara con zona segura y punto de movimiento");
            var surface=new Grid();previewEmpty=Text("Tu cámara aparece aquí\nActiva el cursor para empezar",14);previewEmpty.Foreground=Brushes.White;previewEmpty.TextAlignment=TextAlignment.Center;previewEmpty.HorizontalAlignment=HorizontalAlignment.Center;previewEmpty.VerticalAlignment=VerticalAlignment.Center;surface.Children.Add(previewEmpty);surface.Children.Add(preview);surface.Children.Add(new TextBlock{Text="Cámara local · sin grabación",Foreground=Brushes.White,Background=new SolidColorBrush(Color.FromArgb(150,0,0,0)),Padding=new Thickness(8),VerticalAlignment=VerticalAlignment.Bottom,FontSize=12});p.Children.Add(new Border{Child=surface,Background=new SolidColorBrush(Color.FromRgb(20,37,61)),CornerRadius=new CornerRadius(12),Padding=new Thickness(2),Margin=new Thickness(0,0,0,12)});
            dwellProgress=new ProgressBar{Minimum=0,Maximum=1,Height=10,Margin=new Thickness(0,0,0,8)};AutomationProperties.SetName(dwellProgress,"Progreso de clic por permanencia");p.Children.Add(dwellProgress);
            dwellToggle=Check(p,"Clic por permanencia · dwell",settings.DwellEnabled,v=>{settings.DwellEnabled=v;camera.Configure();camera.CancelClick();Save();});
            p.Children.Add(Text("Sal del anillo naranja y vuelve al verde. Mantén la postura para hacer un clic. El progreso aparece junto al cursor.",14));
            var row=new WrapPanel();row.Children.Add(Button("Calibrar",()=>camera.Calibrate()));row.Children.Add(Button("Pausar / reanudar",()=>camera.Pause(!camera.Paused)));p.Children.Add(row);
            var motion=Page();More(p,"Adaptar a mi movimiento").Content=motion;
            modeChoice=Combo(motion,"Control con",Modes,Math.Max(0,Array.IndexOf(Modes,settings.Mode)),i=>{settings.Mode=Modes[i];camera.Calibrate();});
            motion.Children.Add(Button("Perfil equilibrado",()=>{speedSlider.Value=1;amplitudeSlider.Value=.65;camera.Configure();camera.Calibrate();Save();Report("Perfil equilibrado: velocidad estable y control fino.");}));
            motion.Children.Add(Button("Perfil ágil",()=>{speedSlider.Value=1.5;amplitudeSlider.Value=.65;camera.Configure();camera.Calibrate();Save();Report("Perfil ágil: más velocidad. Ajusta a tu comodidad.");}));
            speedSlider=Slider(motion,"Velocidad",.1,3,settings.Sensitivity,v=>settings.Sensitivity=v);amplitudeSlider=Slider(motion,"Amplitud (menos: movimientos pequeños)",.1,2,settings.Amplitude,v=>settings.Amplitude=v);Slider(motion,"Espera de clic (segundos)",.4,5,settings.Dwell,v=>settings.Dwell=v);
            tremorToggle=Check(motion,"Reducir temblores",settings.Tremor,v=>{settings.Tremor=v;camera.Configure();camera.Calibrate();Save();});motion.Children.Add(Button("Siguiente clic derecho",()=>camera.RightNext()));
            var devices=Page();More(p,"Cámara y consumo").Content=devices;
            Combo(devices,"Cámara (índice)",Enumerable.Range(0,10).Select(i=>"Cámara "+(i+1)).ToArray(),Math.Max(0,Math.Min(9,settings.Camera)),i=>{if(camera.Active)SetCamera(false);settings.Camera=i;});
            Combo(devices,"Detecciones por segundo",new[]{"5 · mínimo consumo","8","12 · recomendado","15","20","30"},Array.IndexOf(new[]{5,8,12,15,20,30},settings.Fps),i=>settings.Fps=new[]{5,8,12,15,20,30}[i]);devices.Children.Add(Text("La vista de cámara usa hasta 24 FPS; el cursor se actualiza independientemente.",13));devices.Children.Add(Button("Permiso de cámara",()=>OpenSettings("ms-settings:privacy-webcam")));return p;
        }
        StackPanel FilterPage(){var p=Page();Heading(p,"Colores más cómodos","Siete matrices heredadas de Adapta PE. Son simulaciones de color, no una corrección médica.");
            filterChoice=Combo(p,"Filtro de pantalla",ColorMatrices.Titles,0,i=>{filters.Apply(i);settings.Filter=ColorMatrices.Names[i];Report(i==0?"Filtro retirado":ColorMatrices.Titles[i]+" activo");});
            p.Children.Add(Button("Quitar filtro",()=>Try(()=>SetFilter(0))));talkToggle=Check(p,"TalkBack: leer controles al señalar",false,v=>{settings.TalkBackActive=v;UpdatePointerTimer();Save();});
            p.Children.Add(Text("La lectura depende de los controles accesibles de cada aplicación. En Windows 7 el filtro usa una capa de magnificación a 10 FPS; requiere comprobación en el equipo.",14));return p;
        }
        StackPanel AIPage(){var p=Page();Heading(p,"IA sólo cuando la solicites","Funciones locales no necesitan cuenta, clave ni internet. Tu proveedor puede cobrar por consultas.");
            Check(p,"Activar integración de IA externa",settings.AIEnabled,v=>{settings.AIEnabled=v;if(!v)ai.Cancel();Save();});
            Input(p,"Endpoint HTTPS compatible con chat/completions",settings.Endpoint,v=>{ai.Cancel();settings.Endpoint=v;});Input(p,"Modelo del proveedor",settings.Model,v=>{ai.Cancel();settings.Model=v;});
            p.Children.Add(Text("API key personal (se protege con DPAPI para tu usuario)",14,true));var password=new PasswordBox{MinHeight=36,Padding=new Thickness(8),Margin=new Thickness(0,0,0,12)};AutomationProperties.SetName(password,"Clave API personal");p.Children.Add(password);
            p.Children.Add(Button("Guardar clave",()=>Try(()=>{Preferences.SaveKey(password.Password);password.Clear();Report("Clave protegida guardada");})));p.Children.Add(Button("Eliminar clave",()=>Try(()=>{Preferences.SaveKey("");Report("Clave eliminada");})));
            screenConsent=Check(p,"Permitir enviar pantalla al proveedor durante esta sesión",false,v=>{ai.ScreenConsent=v;if(!v)ai.Cancel();});p.Children.Add(Text("La captura explícita incluye la pantalla donde está el cursor y puede contener información privada. No se envía automáticamente.",14));return p;
        }
        StackPanel HelpPage(){var p=Page();Heading(p,"Empieza con una función","1. Enfoca una app. 2. Activa Voz. 3. Di «Computadora, muestra campos». 4. Elige un control. La cámara puede añadirse después.");
            p.Children.Add(Text("Órdenes habituales\n• Abre Bloc de notas / busca movilidad en Wikipedia\n• Escribe Hola, José / escribe Hola en el campo Nombre\n• Selecciona Contacto / pulsa lo seleccionado\n• Lee la pantalla / silencio\n• Nueva pestaña / guardar / presiona control shift N\n• Control cabeza / control brazo izquierdo / control zona libre\n• Calibrar cursor / activar temblor / pausar cursor\n• Filtro deuteranopía / quitar filtro\n• Pregunta a la IA … / analiza pantalla con IA\n• Detener todo",15));
            p.Children.Add(Text("Emergencia: Ctrl+Shift+S detiene asistencia. También disponible en bandeja, panel y esta ventana. Windows puede impedir controlar aplicaciones elevadas; no es necesario desactivar UAC.",14));
            Check(p,"Iniciar Adapta PE al entrar en Windows",settings.Login,v=>Try(()=>{Preferences.SetLogin(v,Process.GetCurrentProcess().MainModule.FileName);settings.Login=v;Save();}));
            Combo(p,"Esquina del panel",new[]{"Superior izquierda","Superior derecha","Inferior izquierda","Inferior derecha"},Math.Max(0,Array.IndexOf(new[]{"Superior izquierda","Superior derecha","Inferior izquierda","Inferior derecha"},settings.Corner)),i=>{settings.Corner=new[]{"Superior izquierda","Superior derecha","Inferior izquierda","Inferior derecha"}[i];if(floating!=null)floating.Position();});
            var screens=Forms.Screen.AllScreens;Combo(p,"Monitor del panel",screens.Select((s,i)=>(i+1)+". "+s.DeviceName).ToArray(),Math.Max(0,Array.FindIndex(screens,s=>s.DeviceName==settings.Monitor)),i=>{settings.Monitor=screens[i].DeviceName;if(floating!=null)floating.Position();});
            p.Children.Add(Button("Salir de Adapta PE",Exit));return p;
        }
        public void Initialize(){
            new WindowInteropHelper(this).EnsureHandle();Try(()=>filters.Recover());
            voice.Command=text=>Do(()=>{if(voice.Active)Run(text);});voice.Status=text=>Do(()=>{voiceState.Text=text;if(!voice.Active&&settings.VoiceActive){settings.VoiceActive=false;Save();}Report(text);Sync();});voice.Level=value=>Do(()=>microphoneLevel.Value=voice.Active?value:0);voice.Transcript=text=>Do(()=>{if(!voice.Active&&text.Length>0)return;transcript.Text=text;if(floating!=null)floating.Transcript.Text=text;});voice.FollowUp=()=>actions.PendingChoice;
            camera.Status=text=>Do(()=>{cameraState.Text=text;if(!camera.Active&&settings.CameraActive){camera.Stop();settings.CameraActive=false;UpdatePointerTimer();Save();}if(!camera.Active)Report(text);if(floating!=null)floating.CameraState.Text=text;Sync();});camera.Announce=text=>Do(()=>{if(camera.Active)Speak(text);});camera.Progress=value=>Do(()=>{double progress=camera.Active?value:0;dwellProgress.Value=progress;if(floating!=null)floating.Dwell.Value=progress;if(progress>0){if(dwellWindow==null)dwellWindow=new DwellWindow();dwellWindow.Update(progress);}else if(dwellWindow!=null)dwellWindow.Hide();});camera.PreviewEnabled=()=>previewVisible;
            camera.Preview=()=>{if(Interlocked.Exchange(ref previewPending,1)!=0)return;Do(()=>{try{if(!previewVisible||!camera.CopyPreview(previewPixels))return;if(livePreview==null)livePreview=new WriteableBitmap(Camera.PreviewWidth,Camera.PreviewHeight,96,96,PixelFormats.Bgr24,null);livePreview.WritePixels(new Int32Rect(0,0,Camera.PreviewWidth,Camera.PreviewHeight),previewPixels,Camera.PreviewWidth*3,0);preview.Source=livePreview;previewEmpty.Visibility=Visibility.Collapsed;if(floating!=null&&floating.IsVisible)floating.Preview.Source=livePreview;}finally{Interlocked.Exchange(ref previewPending,0);}});};
            desktop.ReadResponse=text=>Do(()=>Speak(text));desktop.Highlight=rect=>Do(()=>{if(!rect.HasValue){if(highlight!=null)highlight.Hide();return;}if(highlight==null)highlight=new HighlightWindow();highlight.ShowRect(rect.Value);});
            actions.Respond=text=>Do(()=>Speak(text));actions.ChoiceLabels=values=>Do(()=>ShowChoices(values));actions.Local=LocalCommand;
            foregroundProc=(h,evt,w,o,c,t,time)=>Do(()=>{actions.OnForeground(w);if(w!=IntPtr.Zero&&!Native.Own(w)&&voice.Active&&IsVisible)ShowFloating();});foregroundHook=Native.SetWinEventHook(3,3,IntPtr.Zero,foregroundProc,0,0,0x2);actions.OnForeground(Native.GetForegroundWindow());
            tray=new Forms.NotifyIcon{Icon=new System.Drawing.Icon(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"AdaptaPE.ico")),Text="Adapta PE",Visible=true};var menu=new Forms.ContextMenuStrip();menu.Items.Add("Abrir ajustes",null,(s,e)=>Do(ShowSettings));menu.Items.Add("Voz: activar / apagar",null,(s,e)=>Do(()=>Try(()=>SetVoice(!voice.Active))));menu.Items.Add("Cursor: activar / apagar",null,(s,e)=>Do(()=>Try(()=>SetCamera(!camera.Active))));menu.Items.Add("Panel flotante",null,(s,e)=>Do(ShowFloating));menu.Items.Add("Detener todo",null,(s,e)=>Do(()=>StopAll(true)));menu.Items.Add("Salir",null,(s,e)=>Do(Exit));tray.ContextMenuStrip=menu;tray.DoubleClick+=(s,e)=>Do(ShowSettings);
            pointerTimer.Tick+=PointerTick;choiceTimer.Tick+=(s,e)=>{if(choices.Length>0&&!actions.PendingChoice)actions.CancelChoices();if(!camera.Active&&pointerTimer.IsEnabled&&!settings.TalkBackActive)UpdatePointerTimer();};choiceTimer.Start();if(!string.IsNullOrEmpty(Preferences.Notice))Report(Preferences.Notice);
        }
        void Run(string text){if(string.IsNullOrWhiteSpace(text))return;transcript.Text="Escuché: "+text;result.Text=(Commands.Parse(text).Kind=="unknown"?"Buscando acción: ":"Orden detectada: ")+text;if(floating!=null){floating.Transcript.Text=transcript.Text;floating.Response.Text=result.Text;}_ = actions.Execute(text);}
        async Task<bool> LocalCommand(Command command){
            switch(command.Kind){
                case "stop":StopAll(true);return true;case "cancel":actions.Cancel();ai.Cancel();pending.Cancel();pending.Dispose();pending=new CancellationTokenSource();voice.Silence();camera.CancelClick();Report("Acción cancelada");return true;
                case "voice":SetVoice(command.Text=="on");return true;
                case "silence":voice.Silence();ai.Cancel();Report("Lectura detenida");return true;
                case "filter":int index=Array.IndexOf(ColorMatrices.Names,Commands.Normalize(command.Text));if(index<0)throw new ArgumentException("Filtro desconocido.");SetFilter(index);return true;
                case "talkback":settings.TalkBackActive=command.Text=="on";Sync();UpdatePointerTimer();Save();return true;
                case "calibrate":camera.Calibrate();return true;
                case "camera":SetCamera(command.Text=="on");return true;
                case "pause":camera.Pause(command.Text=="on");return true;
                case "cancelClick":camera.CancelClick();Speak("Clic pendiente cancelado");return true;
                case "nextRight":camera.RightNext();Speak("El siguiente clic será derecho");return true;
                case "mode":string mode=Modes.FirstOrDefault(m=>Commands.Normalize(m)==command.Text);if(mode==null)throw new ArgumentException("Modo desconocido. Usa cabeza, manos, torso, brazos o zona libre.");settings.Mode=mode;camera.Calibrate();Sync();Save();Speak("Control "+mode);return true;
                case "tremor":settings.Tremor=command.Text=="on";camera.Configure();camera.Calibrate();Sync();Save();return true;
                case "small":amplitudeSlider.Value=command.Text=="on"?.4:.65;camera.Configure();camera.Calibrate();Save();Speak("Amplitud ajustada");return true;
                case "speed":speedSlider.Value=Math.Max(.1,Math.Min(3,settings.Sensitivity*(command.Text=="down"?.8:1.25)));camera.Configure();Save();Speak(command.Text=="down"?"Cursor más lento":"Cursor más rápido");return true;
                case "cursorProfile":speedSlider.Value=command.Text=="preciso"?.65:1.5;amplitudeSlider.Value=.65;camera.Configure();camera.Calibrate();Save();Speak("Perfil "+command.Text+" activado");return true;
                case "dwell":settings.DwellEnabled=command.Text=="on";camera.Configure();camera.CancelClick();Sync();Save();Speak(settings.DwellEnabled?"Clic automático activado":"Clic automático apagado");return true;
                case "corner":string corner=Corners.FirstOrDefault(c=>Commands.Normalize(c)==command.Text);if(corner==null)throw new ArgumentException("Esquina no disponible.");settings.Corner=corner;ShowFloating();Save();return true;
                case "app":if(command.Text=="mostrar ajustes"||command.Text=="abrir adapta")ShowSettings();else if(command.Text=="modo flotante")ShowFloating();else if(command.Text=="salir de adapta")Exit();else{Hide();if(floating!=null)floating.Hide();}return true;
                case "ai":case "screenAI":Report("Consultando tu proveedor…");Speak(await ai.Ask(command.Text,command.Kind=="screenAI",pending.Token));return true;
                default:return false;
            }
        }
        static readonly string[] Modes={"Automático","Cabeza","Manos","Mano izquierda","Mano derecha","Torso","Brazos","Brazo izquierdo","Brazo derecho","Zona libre"},Corners={"Superior izquierda","Superior derecha","Inferior izquierda","Inferior derecha"};
        void ChangeVoice(Action change){change();try{voice.Restart();}finally{settings.VoiceActive=voice.Active;Sync();Save();}if(!voice.Active)Report("Preferencia de voz guardada. Activa Voz para usarla.");}
        void SetVoice(bool value){if(suspended&&value)throw new InvalidOperationException("Sesión suspendida.");try{if(value)voice.Start();else voice.Stop();}catch(Exception error){voiceState.Text=error.Message;throw;}settings.VoiceActive=voice.Active;Sync();Save();if(!value)Report("Voz apagada");}
        void SetCamera(bool value){if(suspended&&value)throw new InvalidOperationException("Sesión suspendida.");if(value)camera.Start();else{camera.Stop();preview.Source=null;previewEmpty.Visibility=Visibility.Visible;dwellProgress.Value=0;if(dwellWindow!=null)dwellWindow.Hide();if(floating!=null){floating.Preview.Source=null;floating.Dwell.Value=0;}}settings.CameraActive=camera.Active;Sync();UpdatePointerTimer();Save();Report(value?"Cámara local activada · calibrando":"Cámara apagada");}
        void SetFilter(int index){filters.Apply(index);settings.Filter=ColorMatrices.Names[index];Sync();Save();Report(index==0?"Filtro retirado":ColorMatrices.Titles[index]+" activo");}
        void Sync(){suppress=true;voiceToggle.IsChecked=voice.Active;cameraToggle.IsChecked=camera.Active;talkToggle.IsChecked=settings.TalkBackActive;dwellToggle.IsChecked=settings.DwellEnabled;tremorToggle.IsChecked=settings.Tremor;modeChoice.SelectedIndex=Math.Max(0,Array.IndexOf(Modes,settings.Mode));previewEmpty.Visibility=camera.Active&&preview.Source!=null?Visibility.Collapsed:Visibility.Visible;filterChoice.SelectedIndex=filters.Selected;suppress=false;if(floating!=null){floating.Preview.Visibility=camera.Active?Visibility.Visible:Visibility.Collapsed;floating.Dwell.Visibility=camera.Active?Visibility.Visible:Visibility.Collapsed;floating.Sync(voice.Active,camera.Active);}}
        void Save(){Try(()=>settings.Save());}
        void Try(Action action){try{action();}catch(Exception e){ReportError(e);}}
        public void ReportError(Exception error){Report(error.Message);Sync();}
        void Report(string text){status.Text=text;if(floating!=null)floating.State.Text=text;}
        void Speak(string text){result.Text=text;if(floating!=null)floating.Response.Text=text;try{voice.Say(text);}catch(Exception e){Report("Respuesta escrita disponible; salida de voz no disponible: "+e.Message);}}
        void ShowChoices(string[] values){choices=values;choicePanel.Children.Clear();for(int i=0;i<values.Length;i++){int index=i;choicePanel.Children.Add(Button(values[i],()=>Run("elige "+(index+1))));}if(floating!=null)floating.SetChoices(values,i=>Run("elige "+(i+1)));}
        public void DisplayChanged(){camera.Calibrate();if(floating!=null)floating.Position();if(highlight!=null)highlight.Hide();}
        public void ShowSettings(){Show();WindowState=WindowState.Normal;Activate();}
        void ShowFloating(){if(floating==null)floating=new FloatingWindow(settings,ShowSettings,()=>StopAll(true),RefreshPreviewVisibility,v=>Try(()=>SetVoice(v)),v=>Try(()=>SetCamera(v)));floating.SetChoices(choices,i=>Run("elige "+(i+1)));floating.Preview.Source=preview.Source;floating.Transcript.Text=transcript.Text;floating.Response.Text=result.Text;floating.CameraState.Text=cameraState.Text;floating.Position();floating.Show();RefreshPreviewVisibility();settings.Floating=true;Save();Sync();}
        void UpdatePointerTimer(){if(!suspended&&(camera.Active||settings.TalkBackActive))pointerTimer.Start();else{pointerTimer.Stop();camera.SetTarget(null);lastPointerId=null;lastReadId=null;}}
        async void PointerTick(object sender,EventArgs args){if(Interlocked.Exchange(ref pointerBusy,1)!=0)return;int version=pointerGeneration;IntPtr foreground=Native.GetForegroundWindow();try{if(suspended||!Native.DefaultDesktop())return;var target=await Task.Run(()=>DesktopAccess.AtCursor());double now=Stopwatch.GetTimestamp()/(double)Stopwatch.Frequency;
                if(suspended||version!=pointerGeneration||!pointerTimer.IsEnabled||Native.GetForegroundWindow()!=foreground)return;if(target==null){camera.SetTarget(null);lastPointerId=null;return;}
                
                camera.SetTarget(new CursorTarget{Id=target.Item1,Bounds=target.Item3,Time=now,Window=foreground,HitWindow=target.Item4});
                if(lastPointerId!=target.Item1){lastPointerId=target.Item1;hoverSince=now;}
                if(settings.TalkBackActive&&now-hoverSince>=.3&&lastReadId!=target.Item1&&!voice.Speaking&&!actions.PendingChoice){lastReadId=target.Item1;if(!string.IsNullOrWhiteSpace(target.Item2)){desktop.Highlight(target.Item3);Speak(target.Item2);}}
            }catch(Exception e){if(!(e is ElementNotAvailableException||e is InvalidOperationException||e is System.Runtime.InteropServices.COMException))ReportError(e);}finally{Interlocked.Exchange(ref pointerBusy,0);}}
        public void StopAll(bool remember){
            pointerGeneration++;pending.Cancel();pending.Dispose();pending=new CancellationTokenSource();actions.Cancel();voice.Stop();voice.Silence();camera.Stop();ai.Cancel();ai.ScreenConsent=false;screenConsent.IsChecked=false;pointerTimer.Stop();preview.Source=null;previewEmpty.Visibility=Visibility.Visible;if(dwellWindow!=null)dwellWindow.Hide();if(floating!=null){floating.Preview.Source=null;floating.Dwell.Value=0;}
            bool filterStopped=true;try{filters.Stop();}catch(Exception e){filterStopped=false;ReportError(e);}dwellProgress.Value=0;if(remember){restoreVoice=false;restoreCamera=false;restoreTalk=false;restoreFilter=0;settings.VoiceActive=false;settings.CameraActive=false;settings.TalkBackActive=false;settings.Filter=ColorMatrices.Names[filters.Selected];Save();}Sync();Report(filterStopped?"Asistencia detenida":"Voz y cámara detenidas; el filtro no se pudo restaurar. Reintenta Detener todo.");
        }
        public void Restore(){bool wantsCamera=settings.CameraActive,wantsVoice=settings.VoiceActive,wantsTalk=settings.TalkBackActive;string wantsFilter=settings.Filter;if(settings.Floating)ShowFloating();if(wantsVoice)Try(()=>SetVoice(true));if(wantsCamera)Try(()=>SetCamera(true));settings.TalkBackActive=wantsTalk;if(wantsTalk){Sync();UpdatePointerTimer();}int index=Array.IndexOf(ColorMatrices.Names,wantsFilter);if(index>0)Try(()=>SetFilter(index));}
        public void Suspend(){if(suspended)return;restoreVoice=voice.Active;restoreCamera=camera.Active;restoreTalk=settings.TalkBackActive;restoreFilter=filters.Selected;suspended=true;StopAll(false);Report("Sesión suspendida · entrada y sensores detenidos");}
        public async void Resume(){if(!suspended)return;await Task.Delay(750);if(!suspended||!Native.DefaultDesktop())return;suspended=false;settings.TalkBackActive=restoreTalk;if(restoreVoice)Try(()=>SetVoice(true));if(restoreCamera)Try(()=>SetCamera(true));if(restoreFilter>0)Try(()=>SetFilter(restoreFilter));UpdatePointerTimer();Sync();}
        IntPtr WindowMessage(IntPtr h,int message,IntPtr w,IntPtr l,ref bool handled){if(message==0x312&&w.ToInt32()==160){StopAll(true);handled=true;}return IntPtr.Zero;}
        void Exit(){quitting=true;Save();StopAll(false);app.Shutdown();}
        public void Cleanup(){if(cleaned)return;StopAll(false);cleaned=true;pointerTimer.Stop();choiceTimer.Stop();if(hwnd!=null)Native.UnregisterHotKey(hwnd.Handle,160);if(foregroundHook!=IntPtr.Zero)Native.UnhookWinEvent(foregroundHook);if(tray!=null){tray.Visible=false;tray.Dispose();}voice.Dispose();camera.Dispose();Try(()=>filters.Dispose());ai.Dispose();actions.Dispose();pending.Dispose();if(floating!=null){floating.AllowClose=true;floating.Close();}if(highlight!=null)highlight.Close();if(dwellWindow!=null)dwellWindow.Close();}
    }
    sealed class FloatingWindow:Window {
        public bool AllowClose;readonly Preferences settings;readonly WrapPanel choices=new WrapPanel();public readonly TextBlock State=new TextBlock(),Transcript=new TextBlock(),Response=new TextBlock();
        public readonly Image Preview=new Image{Height=190,Stretch=Stretch.Uniform,Margin=new Thickness(0,8,0,8),Visibility=Visibility.Collapsed};
        public readonly ProgressBar Dwell=new ProgressBar{Minimum=0,Maximum=1,Height=8,Margin=new Thickness(0,0,0,12),Visibility=Visibility.Collapsed};
        readonly CheckBox voiceToggle=new CheckBox{Content="Voz"},cameraToggle=new CheckBox{Content="Cursor con cámara"};bool syncing;
        public readonly TextBlock CameraState=new TextBlock{TextWrapping=TextWrapping.Wrap,FontSize=13,Foreground=new SolidColorBrush(Color.FromRgb(71,85,105)),Margin=new Thickness(0,0,0,8)};
        public FloatingWindow(Preferences settings,Action show,Action stop,Action visibility,Action<bool> voice,Action<bool> camera){this.settings=settings;Title="Adapta PE · panel";Icon=Theme.Asset("AdaptaPE.ico");Width=350;SizeToContent=SizeToContent.Height;ShowInTaskbar=false;Topmost=true;ShowActivated=false;ResizeMode=ResizeMode.NoResize;WindowStyle=WindowStyle.ToolWindow;MaxHeight=660;FontFamily=new FontFamily("Segoe UI");FontSize=15;Background=new SolidColorBrush(Color.FromRgb(248,250,252));Foreground=new SolidColorBrush(Color.FromRgb(27,39,44));
            var page=new StackPanel{Margin=new Thickness(18)};Content=new ScrollViewer{Content=page,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};
            var brand=new StackPanel{Orientation=Orientation.Horizontal,Margin=new Thickness(0,0,0,14)};brand.Children.Add(Theme.Logo(44));var heading=new StackPanel();heading.Children.Add(new TextBlock{Text="Adapta PE",FontSize=22,FontWeight=FontWeights.SemiBold});heading.Children.Add(new TextBlock{Text="A tu ritmo · Base "+MainWindow.ProductVersion,FontSize=12,Foreground=Theme.Blue});brand.Children.Add(heading);page.Children.Add(brand);
            State.Text="Activa sólo lo que necesitas";State.FontSize=13;State.Foreground=new SolidColorBrush(Color.FromRgb(71,85,105));State.TextWrapping=TextWrapping.Wrap;State.Margin=new Thickness(0,0,0,10);page.Children.Add(State);
            foreach(var toggle in new[]{voiceToggle,cameraToggle}){toggle.MinHeight=44;AutomationProperties.SetName(toggle,toggle.Content.ToString());page.Children.Add(toggle);}
            voiceToggle.Checked+=(s,e)=>{if(!syncing)voice(true);};voiceToggle.Unchecked+=(s,e)=>{if(!syncing)voice(false);};cameraToggle.Checked+=(s,e)=>{if(!syncing)camera(true);};cameraToggle.Unchecked+=(s,e)=>{if(!syncing)camera(false);};
            var heard=new Border{Background=Brushes.White,CornerRadius=new CornerRadius(12),Padding=new Thickness(12),Margin=new Thickness(0,10,0,8)};var responses=new StackPanel();heard.Child=responses;page.Children.Add(heard);
            foreach(var text in new[]{Transcript,Response}){text.TextWrapping=TextWrapping.Wrap;text.Margin=new Thickness(0,0,0,6);text.MaxHeight=78;responses.Children.Add(text);}Transcript.FontSize=13;Transcript.Foreground=new SolidColorBrush(Color.FromRgb(71,85,105));Response.FontWeight=FontWeights.SemiBold;
            page.Children.Add(Preview);page.Children.Add(CameraState);page.Children.Add(Dwell);page.Children.Add(choices);
            AutomationProperties.SetName(Preview,"Vista local de cámara en panel flotante");AutomationProperties.SetName(Dwell,"Progreso de dwell en panel flotante");IsVisibleChanged+=(s,e)=>visibility();
            var buttons=new Grid();buttons.ColumnDefinitions.Add(new ColumnDefinition());buttons.ColumnDefinitions.Add(new ColumnDefinition());var settingsButton=new Button{Content="Ajustes",Margin=new Thickness(0,0,6,0)};settingsButton.Click+=(s,e)=>show();buttons.Children.Add(settingsButton);var stopButton=new Button{Content="Detener todo",Margin=new Thickness(6,0,0,0),Background=new SolidColorBrush(Color.FromRgb(151,32,49)),Foreground=Brushes.White};stopButton.Click+=(s,e)=>stop();Grid.SetColumn(stopButton,1);buttons.Children.Add(stopButton);page.Children.Add(buttons);
            SourceInitialized+=(s,e)=>{var h=new WindowInteropHelper(this).Handle;var style=Native.GetWindowLongPtr(h,-20);Native.SetWindowLongPtr(h,-20,new IntPtr(style.ToInt64()|0x08000000));};SizeChanged+=(s,e)=>Position();
            Closing+=(s,e)=>{if(!AllowClose&&!Application.Current.Dispatcher.HasShutdownStarted&&Application.Current.Windows.OfType<MainWindow>().Any(w=>w.IsLoaded)){e.Cancel=true;Hide();settings.Floating=false;settings.Save();}};
        }
        public void Sync(bool voice,bool camera){syncing=true;voiceToggle.IsChecked=voice;cameraToggle.IsChecked=camera;syncing=false;CameraState.Visibility=camera||CameraState.Text.StartsWith("Cámara detenida:")?Visibility.Visible:Visibility.Collapsed;}
        public void SetChoices(string[] values,Action<int> choose){choices.Children.Clear();for(int i=0;i<values.Length;i++){int index=i;var b=new Button{Content=values[i],Padding=new Thickness(8),Margin=new Thickness(0,0,4,6),MaxWidth=270};b.Click+=(s,e)=>choose(index);choices.Children.Add(b);}}
        public void Position(){var screen=Forms.Screen.AllScreens.FirstOrDefault(s=>s.DeviceName==settings.Monitor)??Forms.Screen.PrimaryScreen;var b=screen.WorkingArea;var h=new WindowInteropHelper(this).Handle;if(h==IntPtr.Zero){Left=b.Left+16;Top=b.Top+16;return;}Native.Rect size;Native.GetWindowRect(h,out size);int width=size.Right-size.Left,height=Math.Min(size.Bottom-size.Top,b.Height-32);int x=settings.Corner.Contains("izquierda")?b.Left+16:b.Right-width-16;int y=settings.Corner.StartsWith("Superior")?b.Top+16:b.Bottom-height-16;Native.SetWindowPos(h,new IntPtr(-1),x,y,width,height,0x10);}
    }
    sealed class HighlightWindow:Window {
        public HighlightWindow(){WindowStyle=WindowStyle.None;AllowsTransparency=true;Background=Brushes.Transparent;ShowInTaskbar=false;ShowActivated=false;Topmost=true;IsHitTestVisible=false;ResizeMode=ResizeMode.NoResize;Content=new Border{BorderBrush=Brushes.DeepSkyBlue,BorderThickness=new Thickness(3),CornerRadius=new CornerRadius(3)};SourceInitialized+=(s,e)=>{var h=new WindowInteropHelper(this).Handle;Native.SetWindowLongPtr(h,-20,new IntPtr(Native.GetWindowLongPtr(h,-20).ToInt64()|0x08000000|0x20));};}
        public void ShowRect(Rect rect){if(rect.IsEmpty||rect.Width<=0||rect.Height<=0)return;Show();Native.SetWindowPos(new WindowInteropHelper(this).Handle,new IntPtr(-1),(int)rect.X,(int)rect.Y,(int)rect.Width,(int)rect.Height,0x10);}
    }
}
