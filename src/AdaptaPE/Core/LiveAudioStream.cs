using System;
using System.IO;
using System.Threading;

namespace AdaptaPE.Core
{
    // SAPI consulta Length y Position incluso para audio en vivo sin búsqueda.
    public abstract class LiveAudioStream:Stream
    {
        long consumed;
        protected void Consumed(int bytes){Interlocked.Add(ref consumed,bytes);}
        public override bool CanSeek{get{return false;}}
        public override bool CanWrite{get{return false;}}
        public override long Length{get{return -1;}}
        public override long Position{get{return Interlocked.Read(ref consumed);}set{Seek(value,SeekOrigin.Begin);}}
        public override long Seek(long offset,SeekOrigin origin){long current=Position;if(origin==SeekOrigin.Current&&offset==0||origin==SeekOrigin.Begin&&offset==current)return current;throw new NotSupportedException("El audio en vivo no permite retroceder ni saltar muestras.");}
        public override void Flush(){}
        public override void SetLength(long value){throw new NotSupportedException();}
        public override void Write(byte[] buffer,int offset,int count){throw new NotSupportedException();}
    }
}
