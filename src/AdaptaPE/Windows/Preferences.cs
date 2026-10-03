using System;
using System.IO;
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
        [DataMember] public int Camera=0,Microphone=-1,Fps=12;
        [DataMember] public string SpeechEngine="auto";
        [DataMember] public double Sensitivity=1,Dwell=1,Amplitude=.65;
        [DataMember] public bool Tremor=false,DwellEnabled=true,VoiceActive=false,CameraActive=false,TalkBackActive=false,AIEnabled=false,Floating=false,Login=false;
        public static readonly string Folder=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"AdaptaPE");
        static readonly string FilePath=Path.Combine(Folder,"settings.json");
        public static string Notice="";
        public static Preferences Load() {
            try{if(!File.Exists(FilePath))return new Preferences();using(var s=File.OpenRead(FilePath)) {
                var p=(Preferences)new DataContractJsonSerializer(typeof(Preferences)).ReadObject(s);
                if(p==null)throw new InvalidDataException();
                if(string.IsNullOrWhiteSpace(p.WakeWord))p.WakeWord="Computadora";
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
        public static string ReadKey(){string path=Path.Combine(Folder,"provider.key");if(!File.Exists(path))return "";var bytes=ProtectedData.Unprotect(File.ReadAllBytes(path),null,DataProtectionScope.CurrentUser);try{return Encoding.UTF8.GetString(bytes);}finally{Array.Clear(bytes,0,bytes.Length);}}
        public static void SetLogin(bool enabled,string executable) {
            using(var key=Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run")) {
                if(enabled)key.SetValue("AdaptaPE","\""+executable+"\" --background");else key.DeleteValue("AdaptaPE",false);
            }
        }
    }
}
