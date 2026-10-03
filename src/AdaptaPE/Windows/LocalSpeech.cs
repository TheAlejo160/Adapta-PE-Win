using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace AdaptaPE.Windows
{
    // API C oficial: no requiere .NET Standard, servicios ni un runtime de otro lenguaje.
    static class LocalSpeech
    {
        const string Library="libvosk.dll";
        [DllImport(Library,CallingConvention=CallingConvention.Cdecl)]public static extern void vosk_set_log_level(int level);
        [DllImport(Library,CallingConvention=CallingConvention.Cdecl)]static extern IntPtr vosk_model_new(byte[] path);
        [DllImport(Library,CallingConvention=CallingConvention.Cdecl)]public static extern void vosk_model_free(IntPtr model);
        [DllImport(Library,CallingConvention=CallingConvention.Cdecl)]public static extern IntPtr vosk_recognizer_new(IntPtr model,float rate);
        [DllImport(Library,CallingConvention=CallingConvention.Cdecl)]public static extern void vosk_recognizer_free(IntPtr recognizer);
        [DllImport(Library,CallingConvention=CallingConvention.Cdecl)]public static extern int vosk_recognizer_accept_waveform(IntPtr recognizer,byte[] data,int length);
        [DllImport(Library,CallingConvention=CallingConvention.Cdecl)]public static extern IntPtr vosk_recognizer_result(IntPtr recognizer);
        [DllImport(Library,CallingConvention=CallingConvention.Cdecl)]public static extern IntPtr vosk_recognizer_partial_result(IntPtr recognizer);
        public static IntPtr Load(){
            if(!Environment.Is64BitProcess)throw new InvalidOperationException("Reconocimiento incluido requiere la edición x64. En x86 selecciona SAPI.");
            string folder=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"models","speech","es");
            if(!File.Exists(Path.Combine(folder,"am","final.mdl")))throw new InvalidOperationException("Falta modelo de voz. Conserva toda la carpeta del paquete, incluido models/speech/es.");
            vosk_set_log_level(-1);var model=vosk_model_new(Encoding.UTF8.GetBytes(folder+"\0"));
            if(model==IntPtr.Zero)throw new InvalidOperationException("No se pudo cargar el modelo español incluido. Comprueba los archivos del paquete.");return model;
        }
        [DataContract]sealed class Result{[DataMember(Name="text")]public string Text{get;set;}[DataMember(Name="partial")]public string Partial{get;set;}}
        public static string Text(IntPtr pointer,bool partial){
            if(pointer==IntPtr.Zero)return "";int length=0;while(length<65536&&Marshal.ReadByte(pointer,length)!=0)length++;
            if(length>=65536)throw new InvalidDataException("Respuesta local de voz demasiado grande.");var bytes=new byte[length];Marshal.Copy(pointer,bytes,0,length);
            using(var stream=new MemoryStream(bytes)){var result=(Result)new DataContractJsonSerializer(typeof(Result)).ReadObject(stream);return (partial?result.Partial:result.Text)??"";}
        }
    }
}
