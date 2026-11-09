"""Geometry for clean optional backdrops; never revive baked vanilla panels."""
import copy
from pathlib import Path
import forts_ui as ui

PANEL='root/HUD/HUDPanel'
RES=PANEL+'/ResourcePanel'
ITEMS=PANEL+'/HUDItems'
_original=None


def apply_chrome(globals_,placement,background,tactical):
    global _original
    if _original is None:
        g,_=ui.load(str(Path(__file__).resolve().parents[1]/'original/hud.lua'),ui.DEFAULT_GAME)
        _original=ui.unique_paths(ui.find_root(g))
    paths=ui.unique_paths(ui.find_root(globals_))
    def n(path):return paths[path][1]
    def c(path):return paths[path][2]
    def rgba(path,alpha,rgb=(255,255,255)):
        for key in ('Colour','ColourLower'):n(path).set(key,list(rgb)+[alpha])
    def text(path,pos,size,font_size,anchor=3):
        c(path).set('Position',pos);c(path).set('Size',size);c(path).set('Anchor',anchor)
        n(path).set('Size',font_size);n(path).set('Font','readout');rgba(path,255)
    alpha=round(255*background/100)
    # These textures contain baked screens, frames and readout dividers which
    # no longer coincide with the compact controls. Keep them transparent.
    for name in ('PanelCenter','PanelCenterL','PanelCenterR','ResourcePanel'):
        rgba(PANEL+'/'+name,0)
    neutral='ui/textures/HUD/custom/customization_panel.dds'
    n(PANEL+'/PanelLeft').set('Texture',neutral)
    c(PANEL+'/PanelLeft').set('Position',[0,496]);c(PANEL+'/PanelLeft').set('Size',[160,104]);rgba(PANEL+'/PanelLeft',alpha)
    n(ITEMS).set('Texture',neutral);rgba(ITEMS,0 if tactical else alpha)
    # A separate small backing follows the NEW fields. It is rendered first,
    # behind all existing buttons/text, and does not change their engine paths.
    frame=ui.Table()
    backdrop='ui/textures/HUD/custom/tactical_resource.dds' if tactical else neutral
    for key,value in {'Type':'Static','Name':'CustomizationBackdrop','Style':'Normal','Texture':backdrop,'Colour':[255,255,255,alpha],'ColourLower':[255,255,255,alpha]}.items():frame.set(key,value)
    control=ui.Table();control.set('Position',[60,0 if placement=='corner' else 80]);control.set('Size',[132,114 if placement=='corner' else 34]);frame.set('Control',control)
    children=c(RES).get('Children').array()
    c(RES).set('Children',ui.Table.of([frame]+children))
    # Start from the game's proven dense amount/maximum/rate layout in corner
    # mode. The large centre "amount / cap" boxes are kept for centre mode.
    for i,res in enumerate(('Metal','Energy')):
        box=RES+'/Readout'+res
        if placement=='corner':
            for path,(_,original_node,original_control) in _original.items():
                if path==box or path.startswith(box+'/'):
                    if path not in paths:continue
                    for key in ('Size','Font','Style','Text'):
                        v=original_node.get(key)
                        if v is not None:n(path).set(key,copy.deepcopy(v))
                    for key in ('Position','Size','Anchor','ScriptX','ScriptY'):
                        v=original_control.get(key)
                        if v is None:c(path).entries=[e for e in c(path).entries if e[1]!=key]
                        else:c(path).set(key,copy.deepcopy(v))
                    rgba(path,220 if path.endswith('Label') else 255)
            c(box).set('Position',[60,12+i*36]);c(box).set('Size',[132,30.49])
        n(box).set('Texture','ui/textures/HUD/custom/customization_readout.dds');rgba(box,alpha)
    # One tight row fits the same 132-unit column at the screen's right edge.
    for i,name in enumerate(('AssistantSettingsToggle','Grid','Autodoor','AutoCrossBrace','Pause','Menu')):
        path=RES+'/'+name
        if path not in paths:continue
        pos=c(path).get('Position').array();hidden=pos[1]>300
        c(path).set('Position',[60+i*21.5,84+(400 if hidden else 0)]);c(path).set('Size',[20,20])
    rgba(ITEMS+'/ResourcePanelEnemy',alpha)
    enemy=ITEMS+'/ResourcePanelEnemy'
    # Forts positions this control itself. Keep its native horizontal layout;
    # the engine's spy column starts 28 units to the right of ResourcePanel.
    c(enemy).entries=[e for e in c(enemy).entries if e[1]!='ScriptX']
    n(enemy).set('Texture','ui/textures/HUD/custom/'+('tactical_spy.dds' if tactical else 'customization_spy.dds'))
    for resource,y in (('Metal',61),('Energy',81)):
        text(enemy+'/'+resource,[72,y],[36,9],7.5)
        text(enemy+'/'+resource+'Cap',[114,y],[36,9],7.5)
        text(enemy+'/'+resource+'Rate',[160,y],[38,9],7.5)
    name=enemy+'/LastDeviceBox';c(name).set('Position',[36,34]);c(name).set('Size',[98,12]);rgba(name,0)
    last=name+'/LastDevice';text(last,[49,1],[96,9],7,1);c(last).set('ScriptX','ParentMiddleX')
    # Names arrive localized from the engine. Its normal text style resolves
    # the language font; the numeric readout atlas cannot render Cyrillic.
    n(last).entries=[e for e in n(last).entries if e[1]!='Font']
    c(enemy+'/SpyNext').set('Position',[142,29]);c(enemy+'/SpyNext').set('Size',[18,18])
    c(enemy+'/COBackground_Enemy').set('Position',[141,26])
    if not tactical:return
    n(PANEL+'/PanelLeft').set('Texture','ui/textures/HUD/custom/tactical_detail.dds')
    c(PANEL+'/PanelLeft').set('Position',[4,485]);c(PANEL+'/PanelLeft').set('Size',[160,115])
    c(PANEL+'/ItemDetail').set('Position',[12,507]);c(PANEL+'/ItemDetail').set('Size',[143,88])
    c(PANEL+'/ItemDetail/Help').set('Position',[0,400])
    for res in ('Metal','Energy'):
        box=RES+'/Readout'+res
        rgba(box,0 if placement=='corner' else alpha)
        if placement=='center':
            pos=c(box).get('Position').array()
            c(box).set('Position',[pos[0],pos[1]+37])
    # Retain the tab controls and their engine callbacks for keyboard category
    # selection, but place their header outside the visible game canvas.
    for path in (ITEMS+'/TabbedItemPanel/Header','root/HUD/ExpandedHUDItems/TabbedItemPanel/Header'):
        c(path).set('Position',[0,1000]);c(path).set('ScriptY','ScreenH + 400')
    # The complete HUDItems container is moved below, including its mouse hit
    # region. Moving only this child lets it draw outside the parent's input area.
    # Restore the vanilla weapon-direction controls to the detail panel. This
    # keeps their conditional visibility while removing the assistant popup.
    source=c(RES+'/AssistantSettingsPanel').get('Children')
    facing=next(e[2] for e in source.entries if e[3] and e[2].get('Name')=='WeaponLeft')
    c(RES+'/AssistantSettingsPanel').set('Children',ui.Table.of([e for e in source.array() if e is not facing]))
    detail=c(PANEL+'/ItemDetail')
    detail.set('Children',ui.Table.of(detail.get('Children').array()+[facing]))
    ui.control_part(facing).set('Position',[0,66])
    c(RES+'/AssistantSettingsToggle').set('Position',[0,1000])
    reactor=ITEMS+'/ReactorBars'
    pos=c(reactor).get('Position').array();c(reactor).set('Position',[pos[0]+4,pos[1]])
    for i in range(5):
        bar=reactor+'/ReactorBG'+str(i)
        n(bar).set('Texture','ui/textures/HUD/custom/tactical_reactor_track.dds');c(bar).set('Size',[160,22]);rgba(bar,alpha)
        n(bar+'/ReactorGauge'+str(i)).set('Texture','ui/textures/HUD/custom/tactical_reactor_fill.dds')
        button=bar+'/ReactorButton'+str(i)
        n(button).get('Static').set('Texture','ui/textures/HUD/custom/tactical_reactor_button.dds')
        c(button).set('Position',[3,3]);c(button).set('Size',[54,17])
        # The generated strip includes the red indicator; retain the engine's
        # flashing overlay at that indicator's new centre.
        c(bar+'/ReactorGlow'+str(i)).set('Position',[23,1])
    # Keep a small border margin on the screen edge, including the commander.
    pos=c(ITEMS+'/COBackground').get('Position').array()
    c(ITEMS+'/COBackground').set('Position',[pos[0]+8,pos[1]])
    pos=c(ITEMS).get('Position').array()
    c(ITEMS).set('Position',[pos[0],pos[1]+37])
    for path,(_,node,control) in paths.items():
        if path.startswith(ITEMS+'/') and path.count('/')==ITEMS.count('/')+1 and path!=ITEMS+'/TabbedItemPanel':
            pos=control.get('Position').array()
            control.set('Position',[pos[0],pos[1]-37])
