using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FortsSetup {
    // Decode once when selected: the preview and installation share the same bytes.
    public static class BackgroundImage {
        public const int MaxDimension=2048;
        public static readonly string[] PresetIds={"sunset","zors","grib"};
        public static string PresetFile(string assets,string id,bool screens) {
            string file;
            switch(id){
                case "sunset":file="sunset.png";break;
                case "zors":file=screens?"zors-screens.png":"zors-main.png";break;
                case "grib":file="grib-forest.png";break;
                default:throw new ArgumentException("Неизвестный набор фонов.");
            }
            return Path.Combine(assets,"backgrounds",file);
        }
        public static byte[] Encode(string path) {
            string ext=Path.GetExtension(path).ToLowerInvariant();
            if(ext!=".png"&&ext!=".jpg"&&ext!=".jpeg"&&ext!=".bmp")throw new IOException("Выбери PNG, JPG или BMP.");
            BitmapSource source;
            using(var stream=File.OpenRead(path)){
                if(stream.Length>64*1024*1024)throw new IOException("Изображение слишком большое: максимум 64 МБ.");
                var decoder=BitmapDecoder.Create(stream,BitmapCreateOptions.PreservePixelFormat,BitmapCacheOption.None);
                var frame=decoder.Frames[0];
                if(frame.PixelWidth>16384||frame.PixelHeight>16384||(long)frame.PixelWidth*frame.PixelHeight>40000000)throw new IOException("Изображение слишком большое: максимум 40 мегапикселей.");
                double scale=Math.Min(1.0,MaxDimension/(double)Math.Max(frame.PixelWidth,frame.PixelHeight));
                BitmapSource resized=scale<1?new TransformedBitmap(frame,new ScaleTransform(scale,scale)):(BitmapSource)frame;
                source=new FormatConvertedBitmap(resized,PixelFormats.Pbgra32,null,0);
                int width=source.PixelWidth,height=source.PixelHeight;
                // Fully materialize before closing the source stream.
                byte[] pixels=new byte[width*height*4];source.CopyPixels(pixels,width*4,0);
                for(int i=3;i<pixels.Length;i+=4)pixels[i]=255; // transparent areas become black
                byte[] data=new byte[128+pixels.Length];
                using(var writer=new BinaryWriter(new MemoryStream(data))){
                    writer.Write(0x20534444);writer.Write(124);writer.Write(0x100F);writer.Write(height);writer.Write(width);writer.Write(width*4);
                    writer.BaseStream.Position=76;writer.Write(32);writer.Write(0x41);writer.Write(0);writer.Write(32);
                    writer.Write(0x00FF0000);writer.Write(0x0000FF00);writer.Write(0x000000FF);writer.Write(unchecked((int)0xFF000000));writer.Write(0x1000);
                }
                Buffer.BlockCopy(pixels,0,data,128,pixels.Length);return data;
            }
        }
        public static BitmapSource Preview(byte[] data) {
            Engine.ValidateBackground(data);
            int width=BitConverter.ToInt32(data,16),height=BitConverter.ToInt32(data,12);
            var image=BitmapSource.Create(width,height,96,96,PixelFormats.Bgra32,null,Pixels(data),width*4);image.Freeze();return image;
        }
        public static byte[] AddLogo(byte[] background,string logoPath) {
            Engine.ValidateBackground(background);
            byte[] result=(byte[])background.Clone();
            int canvasWidth=BitConverter.ToInt32(background,16),canvasHeight=BitConverter.ToInt32(background,12);
            using(var file=File.OpenRead(logoPath)){
                var logo=BitmapDecoder.Create(file,BitmapCreateOptions.PreservePixelFormat,BitmapCacheOption.OnLoad).Frames[0];
                double scale=Math.Min(Math.Max(1,canvasWidth*0.25)/logo.PixelWidth,Math.Max(1,canvasHeight*0.14)/logo.PixelHeight);
                var resized=new TransformedBitmap(logo,new ScaleTransform(scale,scale));
                var source=new FormatConvertedBitmap(resized,PixelFormats.Pbgra32,null,0);
                int width=source.PixelWidth,height=source.PixelHeight;
                byte[] pixels=new byte[width*height*4];source.CopyPixels(pixels,width*4,0);
                for(int y=0;y<height;y++)for(int x=0;x<width;x++){
                    int from=(y*width+x)*4,to=128+((y+(int)(canvasHeight*0.10))*canvasWidth+x+(int)(canvasWidth*0.14))*4,alpha=pixels[from+3];
                    for(int channel=0;channel<3;channel++)result[to+channel]=(byte)Math.Min(255,pixels[from+channel]+(result[to+channel]*(255-alpha)+127)/255);
                }
            }
            return result;
        }
        static byte[] Pixels(byte[] data){byte[] pixels=new byte[data.Length-128];Buffer.BlockCopy(data,128,pixels,0,pixels.Length);return pixels;}
    }
}
