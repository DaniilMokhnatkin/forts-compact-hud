using System;
using System.IO;

namespace FortsSetup {
    public sealed class HudCustomization {
        public string Resources="corner";
        public int Cards=80, Background=0, Hue=0;
        public bool Indicators=true;
        public string Key {get{return Resources+"-"+Cards+"-"+Background+"-"+(Indicators?1:0);}}
        public HudCustomization Clone(){return (HudCustomization)MemberwiseClone();}
        public void Validate(){
            if((Resources!="corner"&&Resources!="center")||(Cards!=65&&Cards!=80&&Cards!=100)||(Background!=0&&Background!=50&&Background!=100)||Hue< -180||Hue>180||Hue%15!=0)
                throw new ArgumentException("Недопустимые настройки HUD.");
        }
        public static byte[] ShiftHue(byte[] input,int degrees){
            if(degrees==0)return input;
            if(input.Length<132||(BitConverter.ToInt32(input,80)&0x40)==0||BitConverter.ToInt32(input,88)!=32)throw new IOException("Нужна несжатая текстура интерфейса DDS.");
            byte[] data=(byte[])input.Clone();
            for(int i=128;i+3<data.Length;i+=4){
                double r=data[i+2]/255.0,g=data[i+1]/255.0,b=data[i]/255.0,max=Math.Max(r,Math.Max(g,b)),min=Math.Min(r,Math.Min(g,b)),d=max-min;
                if(d<0.025||data[i+3]==0)continue; // neutral metal and white glyphs stay neutral
                double h=max==r?(g-b)/d:max==g?2+(b-r)/d:4+(r-g)/d;
                h=((h*60+degrees)%360+360)%360;double c=d,x=c*(1-Math.Abs(h/60%2-1)),m=min;
                double nr=0,ng=0,nb=0;
                if(h<60){nr=c;ng=x;}else if(h<120){nr=x;ng=c;}else if(h<180){ng=c;nb=x;}else if(h<240){ng=x;nb=c;}else if(h<300){nr=x;nb=c;}else{nr=c;nb=x;}
                data[i+2]=(byte)Math.Round((nr+m)*255);data[i+1]=(byte)Math.Round((ng+m)*255);data[i]=(byte)Math.Round((nb+m)*255);
            }
            return data;
        }
    }
    public sealed class HudProfile {
        public string layout,hash;
        public HudCustomization customization;
    }
}
