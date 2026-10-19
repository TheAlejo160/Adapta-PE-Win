using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace AdaptaPE.Core
{
    [DataContract] public sealed class MascotAtlas {
        [DataContract] public sealed class Frame {
            [DataMember] public int x,y,w,h,duration;
        }
        [DataContract] public sealed class Animation {
            [DataMember] public string name;
            [DataMember] public int[] entry,loop,exit;
        }
        [DataMember] public Frame[] frames;
        [DataMember] public Animation[] animations;
        public Animation Find(string name){return animations.FirstOrDefault(a=>a.name==name)??animations.First(a=>a.name=="idle");}
        public bool Valid(int width,int height){return frames!=null&&frames.Length==144&&animations!=null&&animations.Any(a=>a.name=="idle"&&a.loop!=null&&a.loop.Length>0)&&animations.Any(a=>a.name=="look"&&a.loop!=null&&a.loop.Length==9)&&frames.All(f=>f!=null&&f.duration>0&&f.duration<=10000&&f.x>=0&&f.y>=0&&f.w>0&&f.h>0&&f.x<=width-f.w&&f.y<=height-f.h)&&animations.All(a=>a.entry!=null&&a.loop!=null&&a.exit!=null&&a.entry.Concat(a.loop).Concat(a.exit).All(i=>i>=0&&i<frames.Length));}
    }
    public sealed class MascotPlayback {
        public readonly MascotAtlas Atlas;
        public string State="idle";public int Frame;
        readonly Queue<int> pending=new Queue<int>();int index;
        public MascotPlayback(MascotAtlas atlas){Atlas=atlas;}
        public void Set(string state){if(State==state)return;pending.Clear();foreach(int i in Atlas.Find(State).exit.Concat(Atlas.Find(state).entry))pending.Enqueue(i);State=state;index=0;}
        public bool Resting {get{return State=="sleep"&&pending.Count==0&&index>=Atlas.Find(State).loop.Length;}}
        public void Advance(){if(pending.Count>0){Frame=pending.Dequeue();return;}var a=Atlas.Find(State);if((State=="wave"||State=="wake"||State=="phone"||State=="laptop"||State=="happy"||State=="confused")&&index>=a.loop.Length){foreach(int i in a.exit)pending.Enqueue(i);State="idle";index=0;Advance();return;}if(a.loop.Length==0){State="idle";index=0;Advance();return;}Frame=a.loop[index++%a.loop.Length];}
    }
    public static class PanelPlacement {
        public static int Position(int origin,int length,int size,int reserved,bool free,double fraction,bool far){
            int first=origin+reserved,last=Math.Max(first,origin+length-size);
            if(free){if(double.IsNaN(fraction)||double.IsInfinity(fraction))fraction=.5;return first+(int)(Math.Max(0,Math.Min(1,fraction))*(last-first));}
            return Math.Max(first,Math.Min(last,far?last-16:first+16));
        }
    }
    public sealed class MascotActivity {
        public double LastInteraction;public bool ManualSleep,Speaking;
        public void Touch(double now){LastInteraction=now;}
        public bool ShouldSleep(double now){return !ManualSleep&&!Speaking&&now-LastInteraction>=90;}
    }
    public static class MascotCommand {
        public static readonly string[] Names={"ninguna","tinku","cuchu","yati"};
        public static readonly string[] Titles={"Sin mascota","Tinku · gallito","Cuchu · cuy","Yati · llama"};
        public static string Parse(string text){
            string n=Commands.Normalize(text);
            if(n=="une mascota al panel"||n=="une la mascota al panel")return "join";
            if(n=="separa mascota del panel"||n=="separa la mascota del panel")return "separate";
            if(new[]{"oculta mascota","oculta la mascota","quita mascota","quita la mascota"}.Contains(n))return "ninguna";
            foreach(var pair in new[]{Tuple.Create("saluda","wave"),Tuple.Create("duerme","sleep"),Tuple.Create("despierta","wake")})if(n==pair.Item1+" mascota"||n==pair.Item1+" la mascota")return pair.Item2;
            var m=Regex.Match(n,@"^(?:(?:elige|cambia a)(?: la)? )?mascota\s+(.+)$",RegexOptions.None,TimeSpan.FromMilliseconds(100));if(!m.Success)return null;
            string name=Regex.Replace(m.Groups[1].Value,@"[\s.-]","");name=Regex.Replace(name,@"([a-z])\1+","$1");
            if(new[]{"tinku","tincu","tinko"}.Contains(name))return "tinku";
            if(new[]{"cuchu","kuchu","cucho","cuy"}.Contains(name))return "cuchu";
            return new[]{"yati","yaty","yate"}.Contains(name)?"yati":null;
        }
    }
}
