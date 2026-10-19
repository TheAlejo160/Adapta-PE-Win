using System;
namespace AdaptaPE.Core {
    public sealed class GestureState {
        public bool ResetNow {get;private set;}
        Vec? reference,referenceTilt;double? lastTime,lastAngle;double rotation;Vec output=new Vec(.5,.5);
        public void Reset(){ResetNow=false;reference=null;referenceTilt=null;lastTime=null;lastAngle=null;rotation=0;output=new Vec(.5,.5);}
        public Vec Update(Vec point,double time,Vec? tilt=null,bool pointing=false,double? angle=null,double maxGap=.2){
            ResetNow=false;if(tilt.HasValue&&!tilt.Value.Finite)tilt=null;if(angle.HasValue&&(double.IsNaN(angle.Value)||double.IsInfinity(angle.Value)))angle=null;
            // Acotar el ángulo impide bucles de normalización con entradas extremas.
            if(angle.HasValue)angle=angle.Value%(2*Math.PI);
            if(!point.Finite||double.IsNaN(time)||double.IsInfinity(time)){Reset();ResetNow=true;return output;}
            if(lastTime.HasValue&&time<=lastTime.Value)return output;
            if(lastTime.HasValue&&time-lastTime.Value>maxGap+.0001){Reset();ResetNow=true;}
            if(!reference.HasValue){ResetNow=true;reference=point;referenceTilt=tilt;lastAngle=angle;lastTime=time;return output;}
            var delta=new Vec((point.X-reference.Value.X)*6,(point.Y-reference.Value.Y)*6);
            if(angle.HasValue&&lastAngle.HasValue){double step=angle.Value-lastAngle.Value;while(step>Math.PI)step-=2*Math.PI;while(step< -Math.PI)step+=2*Math.PI;rotation+=step;}lastAngle=angle;
            if(new Vec(point.X-reference.Value.X,point.Y-reference.Value.Y).Length<1.8/160){
                if(tilt.HasValue){var start=pointing?new Vec():referenceTilt??tilt.Value;delta=new Vec(tilt.Value.X-start.X,tilt.Value.Y-start.Y);}
                else if(angle.HasValue)delta=new Vec(rotation*.9,0);
            }
            lastTime=time;output=new Vec(.5+Math.Max(-.2,Math.Min(.2,delta.X)),.5+Math.Max(-.2,Math.Min(.2,delta.Y)));return output;
        }
    }
    public static class CursorAttraction {
        public static Vec Apply(Vec point,Vec center,double speed,double dt){
            double distance=new Vec(center.X-point.X,center.Y-point.Y).Length;if(!point.Finite||!center.Finite||distance>28||speed>120||dt<=0)return point;
            double alpha=1-Math.Exp(-Math.Min(.05,dt)/.12);return new Vec(point.X+(center.X-point.X)*alpha,point.Y+(center.Y-point.Y)*alpha);
        }
    }
    public static class ControlModes {
        public static readonly string[] Titles={"Cabeza","Manos","Zona libre"};
        public static string Normalize(string mode){string n=Commands.Normalize(mode);if(n=="cabeza"||n=="torso")return "Cabeza";return n.StartsWith("zona")?"Zona libre":"Manos";}
    }
}
