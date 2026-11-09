using System;
using System.IO;

namespace FortsSetup {
    public sealed class HudCustomization {
        public string Resources="corner";
        public int Cards=80, Background=0, Hue=0;
        public bool Indicators=true;
        // Tactical only: replaces the coloured accents with white (instead of rotating the hue).
        public bool White=false;
        public string Key {get{return Resources+"-"+Cards+"-"+Background+"-"+(Indicators?1:0);}}
        public HudCustomization Clone(){return (HudCustomization)MemberwiseClone();}
        // Starkoff (compact) only lets the player move the resources; everything else is fixed.
        public HudCustomization For(string layout){var c=Clone();if(layout!="tactical"){c.Cards=80;c.Background=0;c.Hue=0;c.White=false;c.Indicators=true;}return c;}
        public void Validate(){
            if((Resources!="corner"&&Resources!="center")||(Cards!=65&&Cards!=80&&Cards!=100)||(Background!=0&&Background!=50&&Background!=100)||Hue< -180||Hue>180||Hue%15!=0)
                throw new ArgumentException("Недопустимые настройки HUD.");
        }
        public static byte[] ShiftHue(byte[] input,int degrees){
            if(degrees==0)return input;
            int start=BgraPixelOffset(input);
            byte[] data=(byte[])input.Clone();
            for(int i=start;i+3<data.Length;i+=4){
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
        static double Smooth(double a,double b,double x){double t=Math.Min(1,Math.Max(0,(x-a)/(b-a)));return t*t*(3-2*t);}
        static bool IsIconButton(string name){foreach(string p in new[]{"facing","menu","play","pause","grid","autodoors","crossbrace","hud-spy"})if(name.StartsWith(p,StringComparison.Ordinal))return true;return false;}
        // White HUD (Tactical). Icon buttons become white plates with dark icons: the plate level follows the
        // state (hover brightest, selected dimmer) and the old blue frame becomes a grey bezel. Disabled buttons
        // only lose their colour. Panels, tabs and the commander circles stay graphite with a white border so the
        // numbers and labels the game draws in white remain readable. Vivid gauge colours are left alone.
        public static byte[] Whiten(byte[] input,string fileName){
            string name=Path.GetFileName(fileName).ToLowerInvariant();
            bool button=IsIconButton(name)&&!name.EndsWith("-d.dds",StringComparison.Ordinal),plain=IsIconButton(name)&&!button,reactor=name.Contains("reactor");
            int start=BgraPixelOffset(input),width=(int)BitConverter.ToUInt32(input,16),height=(int)BitConverter.ToUInt32(input,12);
            byte[] data=(byte[])input.Clone();
            double face=0.25,plate=0.85;
            if(button){
                var levels=new System.Collections.Generic.List<double>();
                for(int i=start;i+3<data.Length;i+=4){
                    int pixel=(i-start)/4,x=pixel%width,y=pixel/width;
                    if(data[i+3]<200||Math.Min(Math.Min(x,y),Math.Min(width-1-x,height-1-y))<=2)continue;
                    levels.Add(0.299*data[i+2]/255.0+0.587*data[i+1]/255.0+0.114*data[i]/255.0);
                }
                if(levels.Count>0){levels.Sort();face=levels[levels.Count/2];}
                plate=Math.Min(0.97,0.80+(face-0.20)*1.2);
            }
            for(int i=start;i+3<data.Length;i+=4){
                if(data[i+3]==0)continue;
                double r=data[i+2]/255.0,g=data[i+1]/255.0,b=data[i]/255.0,d=Math.Max(r,Math.Max(g,b))-Math.Min(r,Math.Min(g,b)),l=0.299*r+0.587*g+0.114*b,o;
                if(button){
                    int pixel=(i-start)/4,x=pixel%width,y=pixel/width;
                    if(Math.Min(Math.Min(x,y),Math.Min(width-1-x,height-1-y))<=2)o=0.58;
                    else{double shade=plate+(l-face),icon=Smooth(0.14,0.22,Math.Abs(l-face));o=shade+(0.14-shade)*icon;}
                }
                else{
                    if(d<0.025||(reactor&&d>=0.35))continue;
                    o=plain?l:l+(0.96-l)*Smooth(0.38,0.5,l);
                }
                byte v=(byte)Math.Round(Math.Min(1,Math.Max(0,o))*255);
                data[i]=v;data[i+1]=v;data[i+2]=v;
            }
            return data;
        }
        public static int BgraPixelOffset(byte[] data){
            if(data.Length<128||BitConverter.ToInt32(data,0)!=0x20534444)throw new IOException("Некорректная текстура DDS.");
            int start=128;
            if(BitConverter.ToInt32(data,84)==0x30315844){
                // nutellafish uses uncompressed BGRA sRGB in a DDS DX10 header.
                if(data.Length<148||(BitConverter.ToInt32(data,128)!=87&&BitConverter.ToInt32(data,128)!=91))throw new IOException("Не поддерживается формат пикселей DDS DX10.");
                start=148;
            }else if((BitConverter.ToInt32(data,80)&0x40)==0||BitConverter.ToInt32(data,88)!=32)throw new IOException("Нужна несжатая текстура интерфейса DDS.");
            long width=BitConverter.ToUInt32(data,16),height=BitConverter.ToUInt32(data,12);
            if(width==0||height==0||width*height>(data.Length-start)/4)throw new IOException("Повреждена текстура DDS.");
            return start;
        }
    }
    public sealed class HudProfile {
        public string layout,hash;
        public HudCustomization customization;
    }
}
