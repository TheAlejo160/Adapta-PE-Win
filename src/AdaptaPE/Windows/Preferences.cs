using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;

namespace AdaptaPE.Windows
{
    [DataContract]
    public sealed class Preferences
    {
        [DataMember] public string WakeWord="Computadora",Mode="Cabeza",Filter="ninguno",Endpoint="https://integrate.api.nvidia.com/v1/chat/completions",Model="",Corner="Inferior derecha",Monitor="";
        [DataMember] public string Mascot="ninguna",MascotMonitor="";
        [DataMember] public double MascotX=.75,MascotY=.8,PanelX,PanelY;
        [DataMember] public bool MascotLinked,PanelFree;
        [DataMember] public bool PanelHideCamera;

        [DataMember] public int Camera=0,Microphone=-1,Fps=12;
        [DataMember] public string SpeechEngine="auto";
        [DataMember] public double Sensitivity=1,Dwell=1,Amplitude=.65;
        [DataMember] public bool Tremor=false,DwellEnabled=true,VoiceActive=false,CameraActive=false,TalkBackActive=false,AIEnabled=false,Floating=false,Login=false;
        public static readonly string Folder=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"AdaptaPE");
        static readonly string FilePath=Path.Combine(Folder,"settings.json");
        public static string Notice="";
        public static Preferences Load(){return Load(FilePath);}
        internal static Preferences Load(string path) {
            try{if(!File.Exists(path))return new Preferences();using(var s=File.OpenRead(path)) {
                var p=(Preferences)new DataContractJsonSerializer(typeof(Preferences)).ReadObject(s);
                if(p==null)throw new InvalidDataException();
                p.Mode=AdaptaPE.Core.ControlModes.Normalize(p.Mode);
                if(!AdaptaPE.Core.MascotCommand.Names.Contains(p.Mascot))p.Mascot="ninguna";
                if(double.IsNaN(p.MascotX)||double.IsInfinity(p.MascotX)||p.MascotX<0||p.MascotX>1)p.MascotX=.75;
                if(double.IsNaN(p.MascotY)||double.IsInfinity(p.MascotY)||p.MascotY<0||p.MascotY>1)p.MascotY=.8;
                if(double.IsNaN(p.PanelX)||double.IsInfinity(p.PanelX)||p.PanelX<0||p.PanelX>1)p.PanelX=.5;
                if(double.IsNaN(p.PanelY)||double.IsInfinity(p.PanelY)||p.PanelY<0||p.PanelY>1)p.PanelY=.5;
                p.Monitor=p.Monitor??"";p.MascotMonitor=p.MascotMonitor??"";
                if(p.Corner!="Superior izquierda"&&p.Corner!="Superior derecha"&&p.Corner!="Inferior izquierda"&&p.Corner!="Inferior derecha")p.Corner="Inferior derecha";
                if(string.IsNullOrWhiteSpace(p.WakeWord))p.WakeWord="Computadora";
                if(p.WakeWord.Length>40)p.WakeWord="Computadora";
                if(p.Microphone < -1)p.Microphone=-1;
                if(p.SpeechEngine!="auto"&&p.SpeechEngine!="local"&&p.SpeechEngine!="sapi")p.SpeechEngine="auto";
                if(Array.IndexOf(AdaptaPE.Core.ColorMatrices.Names,p.Filter)<0)p.Filter="ninguno";
                p.Fps=Math.Max(5,Math.Min(30,p.Fps));p.Sensitivity=Math.Max(.1,Math.Min(3,p.Sensitivity));p.Amplitude=Math.Max(.1,Math.Min(2,p.Amplitude));p.Dwell=Math.Max(.4,Math.Min(5,p.Dwell));
                return p;
            }}catch(Exception e){if(!(e is IOException||e is SerializationException||e is ArgumentException||e is UnauthorizedAccessException))throw;Notice="Configuración dañada o inaccesible; se usan valores seguros.";return new Preferences();}
        }
        public void Save() {
            Directory.CreateDirectory(Folder);string temporary=FilePath+".tmp";
            using(var s=new FileStream(temporary,FileMode.Create,FileAccess.Write,FileShare.None)){new DataContractJsonSerializer(typeof(Preferences)).WriteObject(s,this);s.Flush(true);}
            if(File.Exists(FilePath))File.Replace(temporary,FilePath,null);else File.Move(temporary,FilePath);
        }
        public static void SaveKey(string key) {
            Directory.CreateDirectory(Folder);string path=Path.Combine(Folder,"provider.key");
            if(string.IsNullOrWhiteSpace(key)){if(File.Exists(path))File.Delete(path);return;}
            byte[] bytes=Encoding.UTF8.GetBytes(key.Trim());
            try{var encrypted=ProtectedData.Protect(bytes,null,DataProtectionScope.CurrentUser);File.WriteAllBytes(path+".tmp",encrypted);if(File.Exists(path))File.Replace(path+".tmp",path,null);else File.Move(path+".tmp",path);}
            finally{Array.Clear(bytes,0,bytes.Length);}
        }
        /// HTTPS obligatorio; HTTP sólo en loopback real (localhost, 127.0.0.0/8, ::1). Sin credenciales, consulta ni fragmento.
        public static Uri ProviderEndpoint(string text) {
            Uri uri;if(!Uri.TryCreate((text??"").Trim(),UriKind.Absolute,out uri)||string.IsNullOrEmpty(uri.Host)||uri.UserInfo.Length>0||uri.Query.Length>0||uri.Fragment.Length>0)return null;
            string host=uri.Host.ToLowerInvariant();string[] octets=host.Split('.');
            bool loopback=host=="localhost"||host=="[::1]"||octets.Length==4&&octets[0]=="127"&&octets.All(o=>o.Length>0&&o.Length<=3&&o.All(c=>c>='0'&&c<='9')&&int.TryParse(o,out int octet)&&octet<=255);
            return uri.Scheme==Uri.UriSchemeHttps||uri.Scheme==Uri.UriSchemeHttp&&loopback?uri:null;
        }
        /// Una clave nunca se traslada a otro destino: si no se puede borrar, se conserva el endpoint anterior.
        internal void ChangeEndpoint(string value,Action deleteKey=null) {
            if(deleteKey==null)deleteKey=()=>SaveKey("");
            try{deleteKey();}
            catch(Exception e) when(e is IOException||e is UnauthorizedAccessException){throw new InvalidOperationException("No se pudo borrar la clave anterior; se conserva el endpoint.",e);}
            Endpoint=value;
        }
        public static string ReadKey(){string path=Path.Combine(Folder,"provider.key");if(!File.Exists(path))return "";var bytes=ProtectedData.Unprotect(File.ReadAllBytes(path),null,DataProtectionScope.CurrentUser);try{return Encoding.UTF8.GetString(bytes);}finally{Array.Clear(bytes,0,bytes.Length);}}
        public static void SetLogin(bool enabled,string executable) {
            using(var key=Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run")) {
                if(enabled)key.SetValue("AdaptaPE","\""+executable+"\" --background");else key.DeleteValue("AdaptaPE",false);
            }
        }
    }
}
