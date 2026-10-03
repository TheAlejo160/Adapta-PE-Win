using System;
using System.Collections.Generic;
using System.Linq;

namespace AdaptaPE.Core
{
    public struct Vec
    {
        public double X,Y;
        public Vec(double x,double y){X=x;Y=y;}
        public double Length {get{return Math.Sqrt(X*X+Y*Y);}}
        public bool Finite {get{return !double.IsNaN(X)&&!double.IsNaN(Y)&&!double.IsInfinity(X)&&!double.IsInfinity(Y);}}
        public Vec ReconcilePixels(Vec actual){return Math.Abs(actual.X-X)>1||Math.Abs(actual.Y-Y)>1?actual:this;}
    }
    public sealed class MotionResult {public Vec Delta,Velocity;public bool Click,Calibrated;public double Progress;}
    public sealed class KineticState
    {
        public bool Tremor,DwellEnabled=true;
        public double Sensitivity=1,Amplitude=.65,DwellDuration=1;
        public Vec? Origin {get;private set;}
        public double Progress {get;private set;}
        public Vec ControlPoint {get{return smooth;}}
        public bool ClickArmed {get{return armed;}}
        public double DeadRadius {get{return Dead;}}
        public double AttractionRadius {get{return Attraction;}}
        Vec smooth=new Vec(.5,.5);
        readonly List<Tuple<double,Vec>> samples=new List<Tuple<double,Vec>>(30);
        readonly List<Vec> history=new List<Vec>(3);
        double? lastTime,armSince,dwellSince; bool armed,safe;string targetId;
        double Dead {get{return Tremor?.07:.045;}} double Attraction {get{return Dead+.02;}}
        static double Median(IEnumerable<double> values){var a=values.OrderBy(x=>x).ToArray();return a[a.Length/2];}
        public void CancelClick(){armSince=null;dwellSince=null;armed=false;Progress=0;targetId=null;safe=false;}
        public void Calibrate(){Origin=null;samples.Clear();history.Clear();lastTime=null;smooth=new Vec(.5,.5);CancelClick();}
        public void Lost(double time){CancelClick();samples.Clear();if(lastTime.HasValue&&time-lastTime.Value>=1.2)Calibrate();}
        public MotionResult Update(Vec point,double time,double aspect,string target)
        {
            var result=new MotionResult();
            if(!point.Finite||double.IsNaN(time)||double.IsInfinity(time)){Lost(time);return result;}
            double interval=lastTime.HasValue?time-lastTime.Value:0;
            if(interval>1.2)Calibrate();if(interval<0||interval>.5){CancelClick();history.Clear();}lastTime=time;
            if(!Origin.HasValue) {
                samples.Add(Tuple.Create(time,point));samples.RemoveAll(p=>time-p.Item1>1.2);
                if(samples.Count<4||time-samples[0].Item1<.9)return result;
                var center=new Vec(Median(samples.Select(p=>p.Item2.X)),Median(samples.Select(p=>p.Item2.Y)));
                var distances=samples.Select(p=>new Vec(p.Item2.X-center.X,(p.Item2.Y-center.Y)*aspect).Length).OrderBy(x=>x).ToArray();
                int third=Math.Max(1,samples.Count/3);
                double drift=new Vec(Median(samples.Take(third).Select(p=>p.Item2.X))-Median(samples.Skip(samples.Count-third).Select(p=>p.Item2.X)),
                    (Median(samples.Take(third).Select(p=>p.Item2.Y))-Median(samples.Skip(samples.Count-third).Select(p=>p.Item2.Y)))*aspect).Length;
                if(drift>.012||distances[Math.Min(distances.Length-1,(int)(distances.Length*.8))]>(Tremor?.045:.025))return result;
                Origin=center;history.Clear();history.Add(center);history.Add(center);samples.Clear();CancelClick();result.Calibrated=true;return result;
            }
            if(history.Count==3)history.RemoveAt(0);history.Add(point);if(history.Count<3)return result;
            var filtered=Tremor?new Vec(Median(history.Select(p=>p.X)),Median(history.Select(p=>p.Y))):point;
            double amplitude=Math.Max(.1,Math.Min(2,Amplitude));
            var raw=new Vec((filtered.X-Origin.Value.X)/amplitude,(filtered.Y-Origin.Value.Y)*aspect/amplitude);
            double distanceRaw=raw.Length,dt=Math.Min(.25,Math.Max(0,interval));
            double alpha=1-Math.Exp(-dt/(Tremor?.18:distanceRaw>Attraction+.025?.05:.10));
            smooth.X+=(.5+raw.X-smooth.X)*alpha;smooth.Y+=(.5+raw.Y-smooth.Y)*alpha;
            var d=new Vec(smooth.X-.5,smooth.Y-.5);double distance=d.Length,radius=Dead+(safe?.008:0);
            safe=distance<=radius && distanceRaw<=radius+.025;
            if(safe) {
                armSince=null;
                if(!armed||!DwellEnabled||target==null){dwellSince=null;targetId=null;Progress=0;return result;}
                if(targetId!=target){targetId=target;dwellSince=time;}
                Progress=Math.Min(1,(time-(dwellSince??time))/Math.Max(.4,DwellDuration));result.Progress=Progress;
                if(Progress>=1){CancelClick();result.Click=true;}return result;
            }
            dwellSince=null;Progress=0;targetId=null;
            if(distanceRaw>Attraction&&distance>Attraction){if(!armSince.HasValue)armSince=time;if(time-armSince.Value>=.18)armed=true;}else armSince=null;
            if(distance<=0||new Vec((point.X-Origin.Value.X)/amplitude,(point.Y-Origin.Value.Y)*aspect/amplitude).Length<=Dead)return result;
            double advance=Math.Max(0,distance-Dead);
            double speed=distance<=Attraction?100*Math.Pow(advance/(Attraction-Dead),2):100+1100*Math.Min(1,(distance-Attraction)/.12);
            speed=Math.Min(Tremor?1600:2400,speed*Math.Max(.1,Math.Min(3,Sensitivity)));
            if(dt>0){result.Velocity=new Vec(d.X/distance*speed,d.Y/distance*speed);result.Delta=new Vec(result.Velocity.X*dt,result.Velocity.Y*dt);}return result;
        }
    }
    public static class ColorMatrices
    {
        public static readonly string[] Names={"ninguno","protanomalia","protanopia","deuteranomalia","deuteranopia","tritanomalia","tritanopia","acromatopsia"};
        public static readonly string[] Titles={"Sin filtro","Protanomalía","Protanopía","Deuteranomalía","Deuteranopía","Tritanomalía","Tritanopía","Acromatopsia"};
        static readonly float[][] rgb={
            new float[]{1,0,0,0,1,0,0,0,1},new float[]{.817f,.183f,0,.333f,.667f,0,0,.125f,.875f},
            new float[]{.567f,.433f,0,.558f,.442f,0,0,.242f,.758f},new float[]{.8f,.2f,0,.258f,.742f,0,0,.142f,.858f},
            new float[]{.625f,.375f,0,.7f,.3f,0,0,.3f,.7f},new float[]{.967f,.033f,0,0,.733f,.267f,0,.183f,.817f},
            new float[]{.95f,.05f,0,0,.433f,.567f,0,0,.475f,.525f},new float[]{.299f,.587f,.114f,.299f,.587f,.114f,.299f,.587f,.114f}
        };
        public static float[] Native(int index) {
            if(index<0||index>=rgb.Length)throw new ArgumentOutOfRangeException("index");
            var m=new float[25];for(int output=0;output<3;output++)for(int input=0;input<3;input++)m[input*5+output]=rgb[index][output*3+input];m[18]=1;m[24]=1;return m;
        }
    }
}
