using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using AdaptaPE.Core;
using OpenCvSharp;
using OpenCvSharp.Dnn;

namespace AdaptaPE.Vision
{
    public sealed class Detection {public Vec Point;public string Source;public Point2f[] Landmarks;public Vec? Tilt;public bool Pointing;public double? Angle;public Detection Backup;}
    internal sealed class LocalVision:IDisposable
    {
        readonly Dictionary<string,Net> models=new Dictionary<string,Net>();
        readonly string folder=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"models");
        static readonly Point2f[] personAnchors=Anchors(new[]{28,14,7},new[]{2,2,6}),palmAnchors=Anchors(new[]{24,12},new[]{2,6});
        readonly Mat zoneSmall=new Mat(),zoneGray=new Mat(),zoneFloats=new Mat();
        readonly ZoneTracker zone=new ZoneTracker();readonly float[] grayPixels=new float[19200];Vec? previousHand;string previousHandSource;
        static Point2f[] Anchors(int[] grids,int[] repeats){var a=new List<Point2f>();for(int i=0;i<grids.Length;i++)for(int y=0;y<grids[i];y++)for(int x=0;x<grids[i];x++)for(int k=0;k<repeats[i];k++)a.Add(new Point2f((x+.5f)/grids[i],(y+.5f)/grids[i]));return a.ToArray();}
        Net Model(string name) {
            Net net;if(models.TryGetValue(name,out net))return net;
            string path=Path.Combine(folder,name);if(!File.Exists(path))throw new InvalidOperationException("Falta modelo local "+name+". Usa el paquete completo con la carpeta models.");
            string hash;if(!ModelHashes.Values.TryGetValue(name,out hash))throw new InvalidOperationException("Modelo desconocido.");
            using(var s=File.OpenRead(path))using(var sha=SHA256.Create())if(BitConverter.ToString(sha.ComputeHash(s)).Replace("-","").ToLowerInvariant()!=hash)throw new InvalidOperationException("Modelo corrupto: "+name);
            net=CvDnn.ReadNetFromOnnx(path);net.SetPreferableBackend(Backend.OPENCV);net.SetPreferableTarget(Target.CPU);models.Add(name,net);return net;
        }
        static float[] Floats(Mat mat){var a=new float[(int)(mat.Total()*mat.Channels())];Marshal.Copy(mat.Data,a,0,a.Length);return a;}
        static float Sigmoid(float x){return (float)(1/(1+Math.Exp(-Math.Max(-100,Math.Min(100,x)))));}
        static Mat[] Forward(Net net,Mat input){net.SetInput(input);var names=net.GetUnconnectedOutLayersNames();var output=names.Select(n=>new Mat()).ToArray();try{net.Forward(output,names);return output;}catch{foreach(var m in output)m.Dispose();throw;}}
        public Detection Detect(Mat image,string mode,string currentSource) {
            mode=ControlModes.Normalize(mode);
            if(mode=="Zona libre")return Free(image);
            if(mode=="Cabeza")return Face(image)??Body(image,"Cabeza",currentSource);
            var hand=Hand(image,"Manos",currentSource);if(hand!=null){if((currentSource??"").StartsWith("brazo"))hand.Backup=Body(image,"Manos",currentSource);return hand;}return Body(image,"Manos",currentSource);
        }
        Detection Face(Mat image) {
            var net=Model("face_detection_yunet_2023mar.onnx");
            using(var small=new Mat())using(var padded=new Mat()){Cv2.Resize(image,small,new Size(320,240));Cv2.CopyMakeBorder(small,padded,0,16,0,0,BorderTypes.Constant,Scalar.Black);
                using(var blob=CvDnn.BlobFromImage(padded,1,new Size(320,256),Scalar.All(0),false,false)){
                    var outputs=Forward(net,blob);try{var names=net.GetUnconnectedOutLayersNames();float best=.65f;Point2f nose=new Point2f();Point2f[] landmarks=null;
                        foreach(int stride in new[]{8,16,32}) {
                            int c=Array.FindIndex(names,n=>n=="cls_"+stride),o=Array.FindIndex(names,n=>n=="obj_"+stride),k=Array.FindIndex(names,n=>n=="kps_"+stride);
                            if(c<0||o<0||k<0)throw new InvalidOperationException("Salidas YuNet incompatibles.");var cls=Floats(outputs[c]);var obj=Floats(outputs[o]);var kp=Floats(outputs[k]);int columns=320/stride;
                            for(int i=0;i<cls.Length;i++){float score=(float)Math.Sqrt(Math.Max(0,cls[i]*obj[i]));if(score<best)continue;int x=i%columns,y=i/columns;
                                var p=new Point2f[5];for(int j=0;j<5;j++)p[j]=new Point2f((x+kp[i*10+j*2])*stride,(y+kp[i*10+j*2+1])*stride);
                                if(p[2].X<0||p[2].X>=320||p[2].Y<0||p[2].Y>=240)continue;best=score;nose=p[2];landmarks=p;
                            }
                        }
                        if(landmarks==null)return null;double eyeWidth=Distance(landmarks[0],landmarks[1]);
                        double yaw=eyeWidth>=8?(nose.X-(landmarks[0].X+landmarks[1].X)/2)/eyeWidth:0;
                        return new Detection{Point=new Vec(nose.X/320+.12*yaw,nose.Y/240),Source="cabeza",Landmarks=landmarks.Select(v=>new Point2f(v.X*image.Cols/320,v.Y*image.Rows/240)).ToArray()};
                    }finally{foreach(var m in outputs)m.Dispose();}
                }
            }
        }
        sealed class AnchorDetection {public float Score;public Point2f[] Points;}
        List<AnchorDetection> DetectAnchors(Mat image,bool palm) {
            int size=palm?192:224;var anchors=palm?palmAnchors:personAnchors;var net=Model(palm?"palm_detection_mediapipe_2023feb.onnx":"person_detection_mediapipe_2023mar.onnx");
            double ratio=(double)size/Math.Max(image.Cols,image.Rows);int w=(int)(image.Cols*ratio),h=(int)(image.Rows*ratio),left=(size-w)/2,top=(size-h)/2;
            using(var resized=new Mat())using(var padded=new Mat())using(var rgb=new Mat())using(var floats=new Mat()) {
                Cv2.Resize(image,resized,new Size(w,h));Cv2.CopyMakeBorder(resized,padded,top,size-h-top,left,size-w-left,BorderTypes.Constant,palm?Scalar.Black:Scalar.All(127.5));Cv2.CvtColor(padded,rgb,ColorConversionCodes.BGR2RGB);
                Mat blob=null;
                if(palm){rgb.ConvertTo(floats,MatType.CV_32FC3,1.0/255);blob=floats.Reshape(1,new[]{1,size,size,3});}
                else blob=CvDnn.BlobFromImage(padded,1.0/127.5,new Size(size,size),Scalar.All(127.5),true,false);
                using(blob){var output=Forward(net,blob);try {
                    Mat scoreMat=output.First(m=>m.Total()==anchors.Length);Mat deltaMat=output.First(m=>m.Total()==anchors.Length*(palm?18:12));
                    var scores=Floats(scoreMat);var deltas=Floats(deltaMat);int row=palm?18:12,landmarkCount=palm?7:4;var result=new List<AnchorDetection>();
                    for(int i=0;i<anchors.Length;i++){float score=Sigmoid(scores[i]);if(score<.6)continue;var points=new Point2f[landmarkCount];
                        for(int j=0;j<points.Length;j++)points[j]=new Point2f((float)(((deltas[i*row+4+j*2]/size+anchors[i].X)*size-left)/ratio),(float)(((deltas[i*row+5+j*2]/size+anchors[i].Y)*size-top)/ratio));
                        result.Add(new AnchorDetection{Score=score,Points=points});
                    }
                    var kept=new List<AnchorDetection>();foreach(var d in result.OrderByDescending(x=>x.Score)){
                        if(kept.All(k=>Distance(k.Points[0],d.Points[0])>Math.Max(15,Distance(d.Points[0],d.Points[Math.Min(2,d.Points.Length-1)]))))kept.Add(d);
                        if(kept.Count==(palm?2:1))break;
                    }return kept;
                }finally{foreach(var m in output)m.Dispose();}}
            }
        }
        static double Distance(Point2f a,Point2f b){return Math.Sqrt((a.X-b.X)*(a.X-b.X)+(a.Y-b.Y)*(a.Y-b.Y));}
        static double Angle(Point2f a,Point2f b){double angle=90-Math.Atan2(-(b.Y-a.Y),b.X-a.X)*180/Math.PI;while(angle>180)angle-=360;while(angle< -180)angle+=360;return angle;}
        static Mat Transform(Point2f center,double angle,double side,int size) {
            var m=Cv2.GetRotationMatrix2D(center,angle,size/side);m.Set(0,2,m.At<double>(0,2)+size*.5-center.X);m.Set(1,2,m.At<double>(1,2)+size*.5-center.Y);return m;
        }
        static Point2f Unproject(Mat inverse,float x,float y){return new Point2f((float)(inverse.At<double>(0,0)*x+inverse.At<double>(0,1)*y+inverse.At<double>(0,2)),(float)(inverse.At<double>(1,0)*x+inverse.At<double>(1,1)*y+inverse.At<double>(1,2)));}
        static Mat InputNHWC(Mat image,Mat transform,int size) {
            using(var crop=new Mat())using(var rgb=new Mat())using(var floats=new Mat()){
                Cv2.WarpAffine(image,crop,transform,new Size(size,size),InterpolationFlags.Linear,BorderTypes.Constant,Scalar.Black);Cv2.CvtColor(crop,rgb,ColorConversionCodes.BGR2RGB);rgb.ConvertTo(floats,MatType.CV_32FC3,1.0/255);return floats.Reshape(1,new[]{1,size,size,3});
            }
        }
        Detection Body(Mat image,string mode,string currentSource=null) {
            var detections=DetectAnchors(image,false);if(detections.Count==0)return null;var d=detections[0];double side=2*Distance(d.Points[0],d.Points[1]);if(side<20||side>image.Cols*8)return null;
            using(var transform=Transform(d.Points[0],Angle(d.Points[0],d.Points[1]),side,256))using(var inverse=new Mat())using(var blob=InputNHWC(image,transform,256)){
                Cv2.InvertAffineTransform(transform,inverse);var output=Forward(Model("pose_estimation_mediapipe_2023mar.onnx"),blob);
                try{var confidence=output.First(m=>m.Total()==1);if(Floats(confidence)[0]<.6)return null;var data=Floats(output.First(m=>m.Total()==195));var p=new Point2f[33];var valid=new bool[33];
                    for(int i=0;i<33;i++){p[i]=Unproject(inverse,data[i*5],data[i*5+1]);valid[i]=Sigmoid(data[i*5+3])>(mode=="Manos"?.5:.65)&&Sigmoid(data[i*5+4])>(mode=="Manos"?.5:.65);}
                    Point2f point;string source;
                    if(mode=="Cabeza"){
                        if(valid[0]){point=p[0];source="cabeza corporal";}
                        else if(valid[11]&&valid[12]){point=new Point2f((p[11].X+p[12].X)/2,(p[11].Y+p[12].Y)/2);source="torso";}else return null;
                    }else{
                        if((currentSource??"").StartsWith("mano")&&previousHand.HasValue){
                            var near=new[]{15,16}.Where(i=>valid[i]).OrderBy(i=>new Vec(p[i].X/image.Cols-previousHand.Value.X,p[i].Y/image.Rows-previousHand.Value.Y).Length).ToArray();
                            if(near.Length==0||new Vec(p[near[0]].X/image.Cols-previousHand.Value.X,p[near[0]].Y/image.Rows-previousHand.Value.Y).Length>.2)return null;
                            point=p[near[0]];source=currentSource;
                        }else{
                            var candidates=new[]{15,16,13,14};int retained;int.TryParse((currentSource??"").Split(' ').Last(),out retained);
                            int idx=retained>0&&candidates.Contains(retained)&&valid[retained]?retained:candidates.FirstOrDefault(i=>valid[i]);if(idx==0)return null;point=p[idx];source="brazo "+idx;
                        }
                    }
                    if(point.X<0||point.Y<0||point.X>image.Cols||point.Y>image.Rows)return null;
                    if(source.StartsWith("mano"))previousHand=new Vec(point.X/image.Cols,point.Y/image.Rows);
                    return new Detection{Point=new Vec(point.X/image.Cols,point.Y/image.Rows),Source=source,Landmarks=p.Where((v,i)=>valid[i]).ToArray()};
                }finally{foreach(var m in output)m.Dispose();}
            }
        }
        Detection Hand(Mat image,string mode,string source=null) {
            Detection best=null;double nearest=double.PositiveInfinity;
            foreach(var d in DetectAnchors(image,true)) {
                double angle=Angle(d.Points[0],d.Points[2]);
                using(var rotation=Cv2.GetRotationMatrix2D(d.Points[0],angle,1)) {
                    var p=d.Points.Select(v=>Unproject(rotation,v.X,v.Y)).ToArray();float x1=p.Min(v=>v.X),y1=p.Min(v=>v.Y),x2=p.Max(v=>v.X),y2=p.Max(v=>v.Y);
                    double side=Math.Max(x2-x1,y2-y1)*3;if(side<20||side>image.Cols*3)continue;
                    using(var invRotation=new Mat()){Cv2.InvertAffineTransform(rotation,invRotation);var center=Unproject(invRotation,(x1+x2)/2,(float)((y1+y2)/2-.4*(y2-y1)));
                        using(var transform=Transform(center,angle,side,224))using(var inverse=new Mat())using(var blob=InputNHWC(image,transform,224)) {
                            Cv2.InvertAffineTransform(transform,inverse);var output=Forward(Model("handpose_estimation_mediapipe_2023feb.onnx"),blob);
                            try{var scalars=output.Where(m=>m.Total()==1).Select(Floats).ToArray();if(scalars.Length!=2||scalars[0][0]<.8)continue;bool right=scalars[1][0]>.5;string name=right?"mano derecha":"mano izquierda";

                                var data=Floats(output.First(m=>m.Total()==63));var landmarks=new Point2f[21];for(int i=0;i<21;i++)landmarks[i]=Unproject(inverse,data[i*3],data[i*3+1]);
                                var wrist=new Vec(landmarks[0].X/image.Cols,landmarks[0].Y/image.Rows);
                                if(!wrist.Finite||wrist.X<0||wrist.Y<0||wrist.X>1||wrist.Y>1)continue;
                                double distance=previousHand.HasValue?new Vec(wrist.X-previousHand.Value.X,wrist.Y-previousHand.Value.Y).Length:0;
                                if((source??"").StartsWith("mano")&&distance>.2)continue;
                                if(best!=null&&distance>=nearest)continue;
                                var tilt=HandTilt(data,inverse,side,image.Cols,image.Rows);nearest=distance;
                                best=new Detection{Point=wrist,Source=(source??"").StartsWith("mano")?source:previousHand.HasValue&&distance<=.2?previousHandSource??name:name,Landmarks=landmarks,Tilt=tilt.Item1,Pointing=tilt.Item2};
                            }finally{foreach(var m in output)m.Dispose();}
                        }
                    }
                }
            }if(best!=null){previousHand=best.Point;previousHandSource=best.Source;}return best;
        }
        static Tuple<Vec,bool> HandTilt(float[] data,Mat inverse,double side,int width,int height){
            var wrist=Unproject(inverse,data[0],data[1]);
            Func<int,double[]> vector=i=>{var p=Unproject(inverse,data[i*3],data[i*3+1]);return new[]{(p.X-wrist.X)/width,(p.Y-wrist.Y)/height*.75,(data[i*3+2]-data[2])*side/224/width};};
            Func<double[],double> length=v=>Math.Sqrt(v.Sum(n=>n*n));var extended=new List<double[]>();
            foreach(int b in new[]{5,9,13,17}){var v=vector(b+3);if(length(v)>length(vector(b))*1.25)extended.Add(v);}
            bool pointing=extended.Count==1||extended.Count==2&&extended[0].Zip(extended[1],(a,b)=>a*b).Sum()/Math.Max(.000001,length(extended[0])*length(extended[1]))>.85;
            double[] direction;
            if(pointing)direction=Enumerable.Range(0,3).Select(i=>extended.Sum(v=>v[i])).ToArray();
            else{var a=vector(5);var b=vector(17);direction=new[]{a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0]};if(direction[2]<0)for(int i=0;i<3;i++)direction[i]=-direction[i];}
            double norm=Math.Max(.000001,length(direction));return Tuple.Create(new Vec(direction[0]/norm*.2,direction[1]/norm*.2),pointing);
        }
        public void ResetFree(){zone.Reset();previousHand=null;previousHandSource=null;}
        Detection Free(Mat image){
            Cv2.Resize(image,zoneSmall,new Size(160,120));Cv2.CvtColor(zoneSmall,zoneGray,ColorConversionCodes.BGR2GRAY);zoneGray.ConvertTo(zoneFloats,MatType.CV_32FC1);Marshal.Copy(zoneFloats.Data,grayPixels,0,grayPixels.Length);
            var point=zone.Update(grayPixels,System.Diagnostics.Stopwatch.GetTimestamp()/(double)System.Diagnostics.Stopwatch.Frequency);
            return point.HasValue?new Detection{Point=point.Value,Source="zona libre "+zone.Revision,Angle=zone.Angle}:null;
        }
        public void Dispose(){ResetFree();zoneSmall.Dispose();zoneGray.Dispose();zoneFloats.Dispose();foreach(var model in models.Values)model.Dispose();models.Clear();}
    }
}
