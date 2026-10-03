using System;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;

namespace AdaptaPE
{
    public sealed class App:Application
    {
        Mutex instance;MainWindow window;bool startupComplete;
        [STAThread]public static void Main(string[] args) {
            var app=new App();app.ShutdownMode=ShutdownMode.OnExplicitShutdown;app.Run();
        }
        protected override void OnStartup(StartupEventArgs e) {
            base.OnStartup(e);bool created;instance=new Mutex(true,@"Local\AdaptaPE.Base.Instance",out created);
            if(!created){MessageBox.Show("Adapta PE ya está abierto. Usa el icono de la bandeja del sistema.","Adapta PE");Shutdown();return;}
            DispatcherUnhandledException+=OnUnhandled;
            try {
                Theme.Apply(this);
                window=new MainWindow(this);window.Initialize();
                SystemEvents.SessionSwitch+=OnSession;SystemEvents.PowerModeChanged+=OnPower;SystemEvents.DisplaySettingsChanged+=OnDisplay;
                if(Array.IndexOf(e.Args,"--background")<0)window.Show();window.Restore();startupComplete=true;
            }catch(Exception error){MessageBox.Show("No se pudo iniciar Adapta PE.\n\n"+error.Message,"Adapta PE · error de arranque",MessageBoxButton.OK,MessageBoxImage.Error);Shutdown(1);}
        }
        void OnUnhandled(object sender,DispatcherUnhandledExceptionEventArgs e){e.Handled=true;if(!startupComplete){MessageBox.Show(e.Exception.Message,"Adapta PE · error de arranque",MessageBoxButton.OK,MessageBoxImage.Error);Shutdown(1);return;}if(window!=null){window.ReportError(e.Exception);if(!window.IsVisible)window.ShowSettings();}}
        void OnSession(object sender,SessionSwitchEventArgs e){if(window==null)return;Dispatcher.BeginInvoke(new Action(()=>{if(e.Reason==SessionSwitchReason.SessionLock||e.Reason==SessionSwitchReason.SessionLogoff||e.Reason==SessionSwitchReason.ConsoleDisconnect||e.Reason==SessionSwitchReason.RemoteDisconnect)window.Suspend();else if(e.Reason==SessionSwitchReason.SessionUnlock||e.Reason==SessionSwitchReason.ConsoleConnect||e.Reason==SessionSwitchReason.RemoteConnect)window.Resume();}));}
        void OnDisplay(object sender,EventArgs e){if(window!=null)Dispatcher.BeginInvoke(new Action(window.DisplayChanged));}
        void OnPower(object sender,PowerModeChangedEventArgs e){if(window==null)return;Dispatcher.BeginInvoke(new Action(()=>{if(e.Mode==PowerModes.Suspend)window.Suspend();else if(e.Mode==PowerModes.Resume)window.Resume();}));}
        protected override void OnExit(ExitEventArgs e){SystemEvents.SessionSwitch-=OnSession;SystemEvents.PowerModeChanged-=OnPower;SystemEvents.DisplaySettingsChanged-=OnDisplay;if(window!=null)window.Cleanup();if(instance!=null){instance.Dispose();instance=null;}base.OnExit(e);}
    }
}
