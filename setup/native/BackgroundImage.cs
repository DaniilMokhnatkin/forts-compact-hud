using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FortsSetup {
    // Decode once when selected: the preview and installation share the same bytes.
    public static class BackgroundImage {
        public const int Width=1920, Height=1080;
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
                double scale=Math.Max(Width/(double)frame.PixelWidth,Height/(double)frame.PixelHeight);
                var resized=new TransformedBitmap(frame,new ScaleTransform(scale,scale));
                var cropped=new CroppedBitmap(resized,new Int32Rect((resized.PixelWidth-Width)/2,(resized.PixelHeight-Height)/2,Width,Height));
                source=new FormatConvertedBitmap(cropped,PixelFormats.Pbgra32,null,0);
                // Fully materialize before closing the source stream.
                byte[] pixels=new byte[Width*Height*4];source.CopyPixels(pixels,Width*4,0);
                for(int i=3;i<pixels.Length;i+=4)pixels[i]=255; // transparent areas become black
                byte[] data=new byte[128+pixels.Length];
                using(var writer=new BinaryWriter(new MemoryStream(data))){
                    writer.Write(0x20534444);writer.Write(124);writer.Write(0x100F);writer.Write(Height);writer.Write(Width);writer.Write(Width*4);
                    writer.BaseStream.Position=76;writer.Write(32);writer.Write(0x41);writer.Write(0);writer.Write(32);
                    writer.Write(0x00FF0000);writer.Write(0x0000FF00);writer.Write(0x000000FF);writer.Write(unchecked((int)0xFF000000));writer.Write(0x1000);
                }
                Buffer.BlockCopy(pixels,0,data,128,pixels.Length);return data;
            }
        }
        public static BitmapSource Preview(byte[] data) {
            Engine.ValidateBackground(data);
            var image=BitmapSource.Create(Width,Height,96,96,PixelFormats.Bgra32,null,Pixels(data),Width*4);image.Freeze();return image;
        }
        public static byte[] AddLogo(byte[] background,string logoPath) {
            Engine.ValidateBackground(background);
            byte[] result=(byte[])background.Clone();
            using(var file=File.OpenRead(logoPath)){
                var logo=BitmapDecoder.Create(file,BitmapCreateOptions.PreservePixelFormat,BitmapCacheOption.OnLoad).Frames[0];
                double scale=Math.Min(640.0/logo.PixelWidth,216.0/logo.PixelHeight);
                var resized=new TransformedBitmap(logo,new ScaleTransform(scale,scale));
                var source=new FormatConvertedBitmap(resized,PixelFormats.Pbgra32,null,0);
                int width=source.PixelWidth,height=source.PixelHeight;
                byte[] pixels=new byte[width*height*4];source.CopyPixels(pixels,width*4,0);
                for(int y=0;y<height;y++)for(int x=0;x<width;x++){
                    int from=(y*width+x)*4,to=128+((y+26)*Width+x+150)*4,alpha=pixels[from+3];
                    for(int channel=0;channel<3;channel++)result[to+channel]=(byte)Math.Min(255,pixels[from+channel]+(result[to+channel]*(255-alpha)+127)/255);
                }
            }
            return result;
        }
        static byte[] Pixels(byte[] data){byte[] pixels=new byte[data.Length-128];Buffer.BlockCopy(data,128,pixels,0,pixels.Length);return pixels;}
    }
}
