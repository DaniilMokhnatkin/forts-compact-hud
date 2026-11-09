using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Automation;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shell;
using Microsoft.Win32;

namespace FortsSetup {
    public static class FeedbackClient {
        public const int MaxImage=8*1024*1024;
        public static string Endpoint(string root){
            foreach(string file in new[]{Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),"Forts-Feedback.json"),Path.Combine(root,"app","feedback.json")}){
                if(!File.Exists(file))continue;
                try{var data=new JavaScriptSerializer().Deserialize<Dictionary<string,string>>(File.ReadAllText(file));Uri uri;string value;
                    if(data.TryGetValue("endpoint",out value)&&Uri.TryCreate(value,UriKind.Absolute,out uri)&&uri.Scheme=="https"&&String.IsNullOrEmpty(uri.UserInfo)&&uri.Host.EndsWith(".workers.dev",StringComparison.OrdinalIgnoreCase))return uri.GetLeftPart(UriPartial.Authority)+"/report";
                }catch{}
            }return null;
        }
        public static byte[] ImageBytes(string path){
            var info=new FileInfo(path);if(!info.Exists||info.Length<8||info.Length>MaxImage)throw new InvalidDataException("PNG, JPG или WebP до 8 МБ.");
            byte[] b=File.ReadAllBytes(path);if(b.Length>MaxImage)throw new InvalidDataException("PNG, JPG или WebP до 8 МБ.");
            Mime(b);return b;
        }
        public static string Mime(byte[] b){
            if(b.Length>=8&&b[0]==137&&b[1]==80&&b[2]==78&&b[3]==71&&b[4]==13&&b[5]==10&&b[6]==26&&b[7]==10)return "image/png";
            if(b.Length>=3&&b[0]==255&&b[1]==216&&b[2]==255)return "image/jpeg";
            if(b.Length>=12&&System.Text.Encoding.ASCII.GetString(b,0,4)=="RIFF"&&System.Text.Encoding.ASCII.GetString(b,8,4)=="WEBP")return "image/webp";
            throw new InvalidDataException("PNG, JPG или WebP до 8 МБ.");
        }
        public static MultipartFormDataContent BuildBody(string description,string contact,string details,byte[] image){
            var body=new MultipartFormDataContent();
                body.Add(new StringContent(description),"description");body.Add(new StringContent(contact),"contact");body.Add(new StringContent(details),"details");
                if(image!=null){var content=new ByteArrayContent(image);content.Headers.ContentType=new MediaTypeHeaderValue(Mime(image));body.Add(content,"screenshot","screenshot"+(Mime(image)=="image/png"?".png":Mime(image)=="image/jpeg"?".jpg":".webp"));}
                // .NET Framework omits quotes for token-safe names; workerd's
                // multipart parser requires quoted Content-Disposition values.
                foreach(var part in body){var disposition=part.Headers.ContentDisposition;disposition.Name="\""+disposition.Name.Trim('"')+"\"";if(disposition.FileName!=null){disposition.FileName="\""+disposition.FileName.Trim('"')+"\"";disposition.FileNameStar=null;}}
                return body;
        }
        public static async Task Send(string endpoint,string description,string contact,string details,byte[] image){
            using(var http=new HttpClient{Timeout=TimeSpan.FromSeconds(50)})using(var body=BuildBody(description,contact,details,image)){
                using(var result=await http.PostAsync(endpoint,body)){
                    if(result.IsSuccessStatusCode)return;
                    string response=await result.Content.ReadAsStringAsync();
                    if(response.Contains("partial_delivery"))throw new IOException("Скриншот доставлен, но текст не отправился. Не отправляйте отчёт повторно целиком.");
                    if((int)result.StatusCode==429)throw new IOException("Слишком много отчётов. Подождите минуту.");
                    if(response.Contains("invalid_form"))throw new IOException("Сервер не принял формат отчёта. Обновите установщик.");
                    if(response.Contains("not_configured"))throw new IOException("Отправка временно недоступна у разработчика.");
                    throw new IOException("Не удалось доставить отчёт. Проверьте подключение и повторите позже.");
                }
            }
        }
    }
    public sealed class FeedbackDialog:Window {
        public readonly TextBox Description,Contact;
        public readonly Button SendButton;
        readonly TextBlock status,attachment;
        readonly Image preview;
        byte[] image;
        bool sending;
        string L(bool en,string ru,string eng){return en?eng:ru;}
        public FeedbackDialog(Window owner,string root,string details,bool en){
            Grid body;Button close;Sheet.Frame(this,owner,L(en,"Сообщить об ошибке","Report a bug"),L(en,"Отчёт и вложение будут отправлены разработчику.","Your report and attachment will be sent to the developer."),L(en,"Закрыть","Close"),out body,out close);
            Width=520;SizeToContent=SizeToContent.Height;close.Click+=(s,e)=>Close();
            var content=new StackPanel();body.Children.Add(content);
            var fields=new StackPanel();
            fields.Children.Add(Sheet.Caption(owner,L(en,"Что произошло и как повторить?","What happened and how can we reproduce it?"),new Thickness(2,0,0,6)));
            Description=new TextBox{Style=Sheet.Style(owner,"TahoeField"),Height=120,AcceptsReturn=true,TextWrapping=TextWrapping.Wrap,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,MaxLength=2500};AutomationProperties.SetName(Description,"Bug description");fields.Children.Add(Description);
            fields.Children.Add(Sheet.Caption(owner,L(en,"Контакт для ответа — необязательно","Reply contact — optional"),new Thickness(2,14,0,6)));
            Contact=new TextBox{Style=Sheet.Style(owner,"TahoeField"),MaxLength=120,Padding=new Thickness(11,7,11,7)};AutomationProperties.SetName(Contact,"Reply contact");fields.Children.Add(Contact);
            content.Children.Add(Sheet.Group(owner,fields,new Thickness(16,14,16,16)));
            var row=new Grid();row.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});row.ColumnDefinitions.Add(new ColumnDefinition());row.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});
            preview=new Image{Stretch=Stretch.UniformToFill};
            var thumb=new Border{Width=44,Height=44,CornerRadius=new CornerRadius(10),Background=Brushes.White,BorderBrush=(Brush)new BrushConverter().ConvertFrom("#1F000000"),BorderThickness=new Thickness(1),ClipToBounds=true,Margin=new Thickness(0,0,12,0)};
            var glyph=new TextBlock{Text="",FontFamily=new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"),FontSize=18,Foreground=Sheet.Res(owner,"Tertiary"),HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center};
            var thumbHost=new Grid();thumbHost.Children.Add(glyph);thumbHost.Children.Add(preview);thumb.Child=thumbHost;row.Children.Add(thumb);
            var info=new StackPanel{VerticalAlignment=VerticalAlignment.Center};Grid.SetColumn(info,1);row.Children.Add(info);
            info.Children.Add(new TextBlock{Text=L(en,"Скриншот","Screenshot"),FontWeight=FontWeights.Medium});
            attachment=new TextBlock{Text=L(en,"PNG, JPG или WebP до 8 МБ","PNG, JPG or WebP up to 8 MB"),Foreground=Sheet.Res(owner,"SecondaryText"),FontSize=12,TextTrimming=TextTrimming.CharacterEllipsis};info.Children.Add(attachment);
            var actions=new StackPanel{Orientation=Orientation.Horizontal,VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(10,0,0,0)};Grid.SetColumn(actions,2);row.Children.Add(actions);
            var remove=new Button{Content=L(en,"Убрать","Remove"),Style=Sheet.Style(owner,"Link"),Margin=new Thickness(0,0,14,0),Visibility=Visibility.Collapsed};actions.Children.Add(remove);
            var add=new Button{Content=L(en,"Прикрепить","Attach"),Style=Sheet.Style(owner,"Secondary")};actions.Children.Add(add);
            var attachGroup=Sheet.Group(owner,row,new Thickness(14,12,14,12));attachGroup.Margin=new Thickness(0,12,0,0);content.Children.Add(attachGroup);
            remove.Click+=(s,e)=>{image=null;preview.Source=null;glyph.Visibility=Visibility.Visible;remove.Visibility=Visibility.Collapsed;attachment.Text=L(en,"PNG, JPG или WebP до 8 МБ","PNG, JPG or WebP up to 8 MB");};
            add.Click+=(s,e)=>{var picker=new OpenFileDialog{Filter="Images|*.png;*.jpg;*.jpeg;*.webp",Multiselect=false};if(picker.ShowDialog(this)!=true)return;try{image=FeedbackClient.ImageBytes(picker.FileName);attachment.Text=Path.GetFileName(picker.FileName);remove.Visibility=Visibility.Visible;try{var b=new BitmapImage();b.BeginInit();b.CacheOption=BitmapCacheOption.OnLoad;b.StreamSource=new MemoryStream(image,false);b.EndInit();b.Freeze();preview.Source=b;glyph.Visibility=Visibility.Collapsed;}catch{preview.Source=null;glyph.Visibility=Visibility.Visible;}}catch{Status(L(en,"Выберите PNG, JPG или WebP до 8 МБ.","Select a PNG, JPG or WebP up to 8 MB."),true);}};
            string endpoint=FeedbackClient.Endpoint(root);
            status=new TextBlock{TextWrapping=TextWrapping.Wrap,Margin=new Thickness(2,12,0,0),FontSize=12};content.Children.Add(status);
            if(endpoint==null)Status(L(en,"Автор сборки ещё не подключил отправку отчётов.","The build author has not enabled report delivery yet."),true);else status.Visibility=Visibility.Collapsed;
            var footer=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right,Margin=new Thickness(0,16,0,0)};content.Children.Add(footer);
            var cancel=new Button{Content=L(en,"Отмена","Cancel"),Style=Sheet.Style(owner,"Secondary"),IsCancel=true,Margin=new Thickness(0,0,10,0)};footer.Children.Add(cancel);cancel.Click+=(s,e)=>Close();
            SendButton=new Button{Content=L(en,"Отправить","Send"),Style=Sheet.Style(owner,"Primary"),IsEnabled=false};footer.Children.Add(SendButton);Description.TextChanged+=(s,e)=>SendButton.IsEnabled=!sending&&endpoint!=null&&Description.Text.Trim().Length>=5;
            Closing+=(s,e)=>{if(sending)e.Cancel=true;};
            SendButton.Click+=async(s,e)=>{sending=true;SendButton.IsEnabled=false;add.IsEnabled=remove.IsEnabled=close.IsEnabled=cancel.IsEnabled=false;Description.IsReadOnly=Contact.IsReadOnly=true;Status(L(en,"Отправляю…","Sending…"),false);try{await FeedbackClient.Send(endpoint,Description.Text.Trim(),Contact.Text.Trim(),details,image);Status(L(en,"Отчёт отправлен. Спасибо!","Report sent. Thank you!"),false);SendButton.Visibility=Visibility.Collapsed;cancel.Content=L(en,"Закрыть","Close");}catch(IOException ex){Status(en?"Delivery failed. Try later. If the screenshot was already delivered, do not resend it.":ex.Message,true);}catch{Status(L(en,"Не удалось отправить. Проверьте сеть и повторите позже.","Could not send. Check your connection and try later."),true);}finally{sending=false;add.IsEnabled=remove.IsEnabled=close.IsEnabled=cancel.IsEnabled=true;Description.IsReadOnly=Contact.IsReadOnly=false;SendButton.IsEnabled=SendButton.Visibility==Visibility.Visible&&Description.Text.Trim().Length>=5;}};
        }
        void Status(string text,bool error){status.Text=text;status.Visibility=Visibility.Visible;status.Foreground=(Brush)FindResource(error?"Danger":"SecondaryText");}
    }
}
