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
    public sealed class Detection {public Vec Point;public string Source;public Point2f[] Landmarks;}
    internal sealed class LocalVision:IDisposable
    {
        readonly Dictionary<string,Net> models=new Dictionary<string,Net>();
        readonly string folder=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"models");
        static readonly Point2f[] personAnchors=Anchors(new[]{28,14,7},new[]{2,2,6}),palmAnchors=Anchors(new[]{24,12},new[]{2,6});
        Mat previousGray;Point2f[] features;Vec freePoint;int freeFrames,freeRevision;
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
            if(mode=="Zona libre")return Free(image);
            if(mode=="Cabeza")return Face(image);
            if(mode.StartsWith("Mano"))return Hand(image,mode,currentSource);
            if(mode!="Automático")return Body(image,mode,currentSource);
            if(!string.IsNullOrEmpty(currentSource)) {
                if(currentSource=="cabeza")return Face(image);
                if(currentSource.StartsWith("mano"))return Hand(image,"Manos",currentSource);
                return Body(image,"Torso");
            }
            return Hand(image,"Manos")??Face(image)??Body(image,"Torso");
        }
        Detection Face(Mat image) {
            var net=Model("face_detection_yunet_2023mar.onnx");
            using(var padded=new Mat()){Cv2.CopyMakeBorder(image,padded,0,256-image.Rows,0,0,BorderTypes.Constant,Scalar.Black);
                using(var blob=CvDnn.BlobFromImage(padded,1,new Size(320,256),Scalar.All(0),false,false)){
                    var outputs=Forward(net,blob);try{var names=net.GetUnconnectedOutLayersNames();float best=.65f;Point2f nose=new Point2f();Point2f[] landmarks=null;
                        foreach(int stride in new[]{8,16,32}) {
                            int c=Array.FindIndex(names,n=>n=="cls_"+stride),o=Array.FindIndex(names,n=>n=="obj_"+stride),k=Array.FindIndex(names,n=>n=="kps_"+stride);
                            if(c<0||o<0||k<0)throw new InvalidOperationException("Salidas YuNet incompatibles.");var cls=Floats(outputs[c]);var obj=Floats(outputs[o]);var kp=Floats(outputs[k]);int columns=320/stride;
                            for(int i=0;i<cls.Length;i++){float score=(float)Math.Sqrt(Math.Max(0,cls[i]*obj[i]));if(score<best)continue;int x=i%columns,y=i/columns;
                                var p=new Point2f[5];for(int j=0;j<5;j++)p[j]=new Point2f((x+kp[i*10+j*2])*stride,(y+kp[i*10+j*2+1])*stride);
                                if(p[2].X<0||p[2].X>=image.Cols||p[2].Y<0||p[2].Y>=image.Rows)continue;best=score;nose=p[2];landmarks=p;
                            }
                        }
                        return landmarks==null?null:new Detection{Point=new Vec(nose.X/image.Cols,nose.Y/image.Rows),Source="cabeza",Landmarks=landmarks};
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
                    for(int i=0;i<33;i++){p[i]=Unproject(inverse,data[i*5],data[i*5+1]);valid[i]=Sigmoid(data[i*5+3])>.55&&Sigmoid(data[i*5+4])>.55;}
                    int[] candidates;string source;
                    if(mode=="Brazo izquierdo"){candidates=new[]{15,13,11};source="brazo izquierdo";}else if(mode=="Brazo derecho"){candidates=new[]{16,14,12};source="brazo derecho";}else if(mode=="Brazos"){candidates=new[]{15,16,13,14,11,12};source="brazo";}else {candidates=new[]{11,12};source="torso";}
                    Point2f point;
                    if(source=="torso"){if(!valid[11]||!valid[12])return null;point=new Point2f((p[11].X+p[12].X)/2,(p[11].Y+p[12].Y)/2);}
                    else{int retained;int.TryParse((currentSource??"").Split(' ').Last(),out retained);int idx=retained>0&&candidates.Contains(retained)&&valid[retained]?retained:candidates.FirstOrDefault(i=>valid[i]);if(idx==0)return null;point=p[idx];source+=" "+idx;}
                    if(point.X<0||point.Y<0||point.X>image.Cols||point.Y>image.Rows)return null;
                    return new Detection{Point=new Vec(point.X/image.Cols,point.Y/image.Rows),Source=source,Landmarks=p.Where((v,i)=>valid[i]).ToArray()};
                }finally{foreach(var m in output)m.Dispose();}
            }
        }
        Detection Hand(Mat image,string mode,string source=null) {
            foreach(var d in DetectAnchors(image,true)) {
                double angle=Angle(d.Points[0],d.Points[2]);
                using(var rotation=Cv2.GetRotationMatrix2D(d.Points[0],angle,1)) {
                    var p=d.Points.Select(v=>Unproject(rotation,v.X,v.Y)).ToArray();float x1=p.Min(v=>v.X),y1=p.Min(v=>v.Y),x2=p.Max(v=>v.X),y2=p.Max(v=>v.Y);
                    double side=Math.Max(x2-x1,y2-y1)*3;if(side<20||side>image.Cols*3)continue;
                    using(var invRotation=new Mat()){Cv2.InvertAffineTransform(rotation,invRotation);var center=Unproject(invRotation,(x1+x2)/2,(float)((y1+y2)/2-.4*(y2-y1)));
                        using(var transform=Transform(center,angle,side,224))using(var inverse=new Mat())using(var blob=InputNHWC(image,transform,224)) {
                            Cv2.InvertAffineTransform(transform,inverse);var output=Forward(Model("handpose_estimation_mediapipe_2023feb.onnx"),blob);
                            try{var scalars=output.Where(m=>m.Total()==1).Select(Floats).ToArray();if(scalars.Length!=2||scalars[0][0]<.8)continue;bool right=scalars[1][0]>.5;string name=right?"mano derecha":"mano izquierda";
                                if(mode=="Mano izquierda"&&right||mode=="Mano derecha"&&!right||source!=null&&source!=name)continue;
                                var data=Floats(output.First(m=>m.Total()==63));var landmarks=new Point2f[21];for(int i=0;i<21;i++)landmarks[i]=Unproject(inverse,data[i*3],data[i*3+1]);
                                var centerPalm=new Point2f((landmarks[0].X+landmarks[5].X+landmarks[9].X+landmarks[13].X+landmarks[17].X)/5,(landmarks[0].Y+landmarks[5].Y+landmarks[9].Y+landmarks[13].Y+landmarks[17].Y)/5);
                                if(centerPalm.X<0||centerPalm.Y<0||centerPalm.X>image.Cols||centerPalm.Y>image.Rows)continue;
                                return new Detection{Point=new Vec(centerPalm.X/image.Cols,centerPalm.Y/image.Rows),Source=name,Landmarks=landmarks};
                            }finally{foreach(var m in output)m.Dispose();}
                        }
                    }
                }
            }return null;
        }
        public void ResetFree(){if(previousGray!=null)previousGray.Dispose();previousGray=null;features=null;freeFrames=0;}
        Detection Free(Mat image) {
            var gray=new Mat();Cv2.CvtColor(image,gray,ColorConversionCodes.BGR2GRAY);
            try{
                if(previousGray==null||features==null||features.Length<4) {
                    using(var mask=new Mat(gray.Size(),MatType.CV_8UC1,Scalar.All(0))){Cv2.Rectangle(mask,new Rect(image.Cols/4,image.Rows/4,image.Cols/2,image.Rows/2),Scalar.All(255),-1);features=Cv2.GoodFeaturesToTrack(gray,40,.02,8,mask,3,false,.04);}
                    freePoint=new Vec(.5,.5);freeFrames=0;freeRevision++;
                }else {
                    Point2f[] next=null;byte[] status;float[] error;Cv2.CalcOpticalFlowPyrLK(previousGray,gray,features,ref next,out status,out error,new Size(15,15),2,new TermCriteria(CriteriaTypes.Count|CriteriaTypes.Eps,20,.03),OpticalFlowFlags.None,.0001);
                    var good=Enumerable.Range(0,features.Length).Where(i=>status[i]!=0&&error[i]<20).ToArray();
                    if(good.Length<4){ResetFree();return null;}
                    var dx=good.Select(i=>(double)(next[i].X-features[i].X)).OrderBy(x=>x).ToArray();var dy=good.Select(i=>(double)(next[i].Y-features[i].Y)).OrderBy(x=>x).ToArray();
                    freePoint.X+=dx[dx.Length/2]/image.Cols;freePoint.Y+=dy[dy.Length/2]/image.Rows;features=good.Select(i=>next[i]).ToArray();freeFrames++;
                }
                if(previousGray!=null)previousGray.Dispose();previousGray=gray;gray=null;
                return features!=null&&features.Length>=4?new Detection{Point=freePoint,Source="zona libre "+freeRevision,Landmarks=features}:null;
            }finally{if(gray!=null)gray.Dispose();}
        }
        public void Dispose(){ResetFree();foreach(var model in models.Values)model.Dispose();models.Clear();}
    }
}
