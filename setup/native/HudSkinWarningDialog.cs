using System.Windows;
using System.Windows.Controls;
using System.Windows.Automation;
using System.Windows.Media;

namespace FortsSetup {
    public sealed class HudSkinWarningDialog : Window {
        public readonly Button Acknowledge;
        public static string Message(bool en,bool defaultSkin=true,string selectedSkin=null,bool menuDefault=false) {
            if(menuDefault)return en?
                "For Zors.o, set both options to Default in Forts:\n\n• Settings → Game → HUD Skin → Default\n• Settings → Game → Menu Skin (background) → Default\n\nOther skins can display the HUD or menu background incorrectly. Restart Forts after changing these settings.":
                "Для Zors.o включи обе настройки «По умолчанию» в Forts:\n\n• Настройки → Игра → Скин HUD → По умолчанию\n• Настройки → Игра → Скин меню (фон) → По умолчанию\n\nС другими скинами HUD или фон меню могут отображаться неправильно. После изменения настроек перезапусти Forts.";
            if(!defaultSkin)return en?
                "For Starkoff, the HUD skin selected in this installer must match the one used in Forts.\n\nSelected in this installer: "+selectedSkin+". Check Settings → Game → HUD Skin in Forts. If the game uses another skin, select that same skin in the installer and reinstall the HUD.":
                "Для Starkoff скин HUD в сетапе должен совпадать с выбранным в Forts.\n\nСейчас в сетапе: «"+selectedSkin+"». Проверь в Forts: Настройки → Игра → Скин HUD. Если в игре выбран другой скин, выбери такой же в сетапе и переустанови HUD.";
            return en?"In Forts, open Settings → Game → HUD Skin and select Default.\n\nOther skins can display this HUD's panels, buttons and icons incorrectly. Restart Forts after changing the skin.":
                "В Forts открой Настройки → Игра → Скин HUD и выбери «По умолчанию».\n\nС другими скинами панели, кнопки и значки этого HUD могут отображаться неправильно. После смены скина перезапусти Forts.";
        }
        public HudSkinWarningDialog(Window owner,bool en,bool defaultSkin=true,string selectedSkin=null,bool menuDefault=false) {
            Width=660;Height=menuDefault?450:defaultSkin?410:440;
            Grid body;Button close;
            string title=defaultSkin?(en?"Select the Default HUD skin":"Включи HUD «По умолчанию»"):(en?"Match the HUD skins":"Скины HUD должны совпадать");
            if(menuDefault)title=en?"Zors.o: Default HUD and menu":"Zors.o: HUD и меню «По умолчанию»";
            Sheet.Frame(this,owner,title,en?"Installation complete · required game setting":"Установка завершена · обязательная настройка в игре",en?"Close":"Закрыть",out body,out close);
            close.Click+=(s,e)=>Close();
            body.RowDefinitions.Add(new RowDefinition{Height=new GridLength(1,GridUnitType.Star)});
            body.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});
            var warning=new Border{Background=new SolidColorBrush(Color.FromRgb(255,244,215)),BorderBrush=new SolidColorBrush(Color.FromRgb(216,158,45)),BorderThickness=new Thickness(1.5),CornerRadius=new CornerRadius(12),Padding=new Thickness(18,14,18,14),Margin=new Thickness(0,0,0,18)};
            var text=new TextBlock{Text="⚠  "+Message(en,defaultSkin,selectedSkin,menuDefault),TextWrapping=TextWrapping.Wrap,Foreground=new SolidColorBrush(Color.FromRgb(100,61,10)),FontSize=15,LineHeight=22};
            AutomationProperties.SetName(text,Message(en,defaultSkin,selectedSkin,menuDefault));warning.Child=text;body.Children.Add(warning);
            Acknowledge=new Button{Content=en?"Understood":"Понятно",Style=Sheet.Style(owner,"Primary"),IsDefault=true,HorizontalAlignment=HorizontalAlignment.Right};
            AutomationProperties.SetAutomationId(Acknowledge,"AcknowledgeHudSkinWarning");Grid.SetRow(Acknowledge,1);body.Children.Add(Acknowledge);Acknowledge.Click+=(s,e)=>DialogResult=true;
        }
    }
}
