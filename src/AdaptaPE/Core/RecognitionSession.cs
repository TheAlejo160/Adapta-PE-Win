using System;
using System.Threading.Tasks;

namespace AdaptaPE.Core
{
    // La liberación nativa puede terminar después de apagar o cambiar el motor.
    public sealed class RecognitionSession
    {
        volatile int generation;
        public int Begin(){return ++generation;}
        public void Cancel(){generation++;}
        public bool Current(int token){return token==generation;}
        public async Task StartAfter(Task release,int token,Action start) {
            if(release!=null)await release;
            if(Current(token))start();
        }
    }
}
