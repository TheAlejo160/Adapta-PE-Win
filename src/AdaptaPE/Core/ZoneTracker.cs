using System;
using System.Collections.Generic;
namespace AdaptaPE.Core {
    // shortcut: textura local; una oclusión de 1,2 s exige nueva adquisición.
    public sealed class ZoneTracker {
        const int Width=160,Height=120,Radius=12;
        float[] previous,template;int x,y;double lostSince;public double Angle {get;private set;}public int Revision {get;private set;}
        struct Match {public int X,Y;public double Error,Angle;}
        public void Reset(){previous=null;template=null;lostSince=0;Angle=0;Revision++;}
        static float[] Patch(float[] image,int x,int y,out double contrast,out double detail){
            var values=new float[169];int i=0;double sum=0,energy=0,horizontal=0,vertical=0,cross=0;
            for(int row=-Radius;row<=Radius;row+=2)for(int col=-Radius;col<=Radius;col+=2){int p=(y+row)*Width+x+col;float v=image[p];values[i++]=v;sum+=v;energy+=v*v;double dx=(image[p+1]-image[p-1])/2,dy=(image[p+Width]-image[p-Width])/2;horizontal+=dx*dx;vertical+=dy*dy;cross+=dx*dy;}
            double mean=sum/values.Length;contrast=Math.Sqrt(Math.Max(0,energy/values.Length-mean*mean));detail=Math.Sqrt(Math.Max(0,(horizontal+vertical-Math.Sqrt((horizontal-vertical)*(horizontal-vertical)+4*cross*cross))/(2*values.Length)));
            for(i=0;i<values.Length;i++)values[i]=(float)((values[i]-mean)/Math.Max(1,contrast));return values;
        }
        Match Measure(float[] image,int px,int py,double angle){
            var result=new Match{X=px,Y=py,Angle=angle,Error=double.PositiveInfinity};if(px<13||py<13||px>=Width-13||py>=Height-13)return result;
            double sum=0,squares=0,dot=0,energy=0,cos=Math.Cos(angle),sin=Math.Sin(angle);int index=0;
            for(int row=-Radius;row<=Radius;row+=2)for(int col=-Radius;col<=Radius;col+=2){int sx=(int)Math.Round(px+col*cos-row*sin),sy=(int)Math.Round(py+col*sin+row*cos);if(sx<0||sy<0||sx>=Width||sy>=Height)return result;double v=image[sy*Width+sx],t=template[index++];sum+=v;squares+=v*v;dot+=v*t;energy+=t*t;}
            if(squares/169-sum*sum/(169*169)<36)return result;result.Error=1-dot/Math.Sqrt(energy*(squares-sum*sum/169));return result;
        }
        public Vec? Update(float[] image,double time){
            if(image==null||image.Length!=Width*Height)return null;
            if(template==null){
                if(previous==null){previous=(float[])image.Clone();return null;}
                double light=0;for(int i=0;i<image.Length;i++)light+=image[i]-previous[i];light/=image.Length;
                int count=0,sx=0,sy=0;for(int py=0;py<Height;py+=2)for(int px=0;px<Width;px+=2)if(Math.Abs(image[py*Width+px]-previous[py*Width+px]-light)>12){count++;sx+=px;sy+=py;}
                double best=0;float[] patch=null;int bx=0,by=0;
                if(count>=12&&count<Width*Height*.35/4){int cx=sx/count,cy=sy/count;for(int py=Math.Max(13,cy-24);py<Math.Min(Height-13,cy+25);py+=4)for(int px=Math.Max(13,cx-24);px<Math.Min(Width-13,cx+25);px+=4){double motion=Math.Abs(image[py*Width+px]-previous[py*Width+px]-light);if(motion<=12)continue;double contrast,detail;var p=Patch(image,px,py,out contrast,out detail);if(contrast>=6&&detail>=1&&detail*motion>best){best=detail*motion;patch=p;bx=px;by=py;}}}
                Array.Copy(image,previous,image.Length);if(patch==null)return null;template=patch;x=bx;y=by;Angle=0;Revision++;
            }
            var matches=new List<Match>(725);var bestMatch=new Match{Error=double.PositiveInfinity};
            for(int py=y-12;py<=y+12;py++)for(int px=x-12;px<=x+12;px++){var m=Measure(image,px,py,0);matches.Add(m);if(m.Error<bestMatch.Error)bestMatch=m;}
            var translation=bestMatch;
            if(translation.Error<=.4){foreach(double angle in new[]{-.24,-.12,.12,.24})for(int py=translation.Y-2;py<=translation.Y+2;py++)for(int px=translation.X-2;px<=translation.X+2;px++){var m=Measure(image,px,py,angle);matches.Add(m);if(m.Error<bestMatch.Error)bestMatch=m;}if(translation.Error-bestMatch.Error<.02)bestMatch=translation;}
            bool ambiguous=matches.Exists(m=>(m.X-bestMatch.X)*(m.X-bestMatch.X)+(m.Y-bestMatch.Y)*(m.Y-bestMatch.Y)>9&&m.Error<=bestMatch.Error*1.15+.005);
            if(bestMatch.Error>.15||ambiguous){if(lostSince==0)lostSince=time;if(time-lostSince>=1.2)Reset();return null;}
            lostSince=0;x=bestMatch.X;y=bestMatch.Y;Angle+=bestMatch.Angle;double c,d;var next=Patch(image,x,y,out c,out d);for(int i=0;i<template.Length;i++)template[i]=bestMatch.Angle!=0?next[i]:template[i]*.95f+next[i]*.05f;return new Vec(x/(double)Width,y/(double)Height);
        }
    }
}
