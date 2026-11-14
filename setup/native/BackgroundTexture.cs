using System;

namespace FortsSetup {
    // Forts draws menu backgrounds at (0,-37), size 1067 x 673.90, then clips
    // them to a 1067 x 600 screen. A raw 16:9 DDS is stretched vertically.
    // Keep editing/preview bytes intact and fit them into the visible portion
    // of the game's 1900 x 1200 texture canvas only at installation time.
    public static class BackgroundTexture {
        const int Width=1900,Height=1200;
        public static byte[] Prepare(byte[] source) {
            if(source==null)return null;
            Engine.ValidateBackground(source);
            int sourceWidth=BitConverter.ToInt32(source,16),sourceHeight=BitConverter.ToInt32(source,12);
            double scale=Math.Min(1067.0/sourceWidth,600.0/sourceHeight);
            double left=(1067-sourceWidth*scale)/2,top=(600-sourceHeight*scale)/2;
            byte[] result=new byte[128+Width*Height*4];Buffer.BlockCopy(source,0,result,0,128);
            Buffer.BlockCopy(BitConverter.GetBytes(Height),0,result,12,4);
            Buffer.BlockCopy(BitConverter.GetBytes(Width),0,result,16,4);
            Buffer.BlockCopy(BitConverter.GetBytes(Width*4),0,result,20,4);
            for(int y=0;y<Height;y++) {
                double screenY=(y+0.5)*673.90/Height-37;
                bool visibleY=screenY>=top&&screenY<top+sourceHeight*scale;
                double sy=Math.Max(0,Math.Min(sourceHeight-1,(screenY-top)/scale-0.5));
                int y0=(int)sy,y1=Math.Min(sourceHeight-1,y0+1);double fy=sy-y0;
                for(int x=0;x<Width;x++) {
                    int target=128+(y*Width+x)*4;result[target+3]=255;
                    double screenX=(x+0.5)*1067/Width;
                    if(!visibleY||screenX<left||screenX>=left+sourceWidth*scale)continue;
                    double sx=Math.Max(0,Math.Min(sourceWidth-1,(screenX-left)/scale-0.5));
                    int x0=(int)sx,x1=Math.Min(sourceWidth-1,x0+1);double fx=sx-x0;
                    int a=128+(y0*sourceWidth+x0)*4,b=128+(y0*sourceWidth+x1)*4;
                    int c=128+(y1*sourceWidth+x0)*4,d=128+(y1*sourceWidth+x1)*4;
                    for(int channel=0;channel<3;channel++) {
                        double upper=source[a+channel]*(1-fx)+source[b+channel]*fx;
                        double lower=source[c+channel]*(1-fx)+source[d+channel]*fx;
                        result[target+channel]=(byte)Math.Round(upper*(1-fy)+lower*fy);
                    }
                }
            }
            return result;
        }
    }
}
