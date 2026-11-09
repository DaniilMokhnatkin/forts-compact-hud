"""Build validated Lua bytecode variants and uncompressed UI textures for native customization.
No compiler or scripts are needed on the player's machine.
"""
from pathlib import Path
from itertools import product
import sys
import shutil
from PIL import Image
import forts_ui as ui
from build_layouts import open_image, vanilla_texture
from hud_chrome import apply_chrome

ROOT = Path(__file__).resolve().parents[1]
PANEL = 'root/HUD/HUDPanel'
RES = PANEL + '/ResourcePanel'
ITEMS = PANEL + '/HUDItems'


def customize(globals_, placement, scale, background, indicators, tactical=False, plain=False):
    paths = ui.unique_paths(ui.find_root(globals_))
    def node(path): return paths[path][1]
    def ctl(path): return paths[path][2]
    def colour(path, alpha):
        for key in ('Colour', 'ColourLower'):
            old = node(path).get(key)
            rgb = old.array()[:3] if isinstance(old, ui.Table) else [255,255,255]
            node(path).set(key, rgb + [alpha])
    extra=round(71*(scale-80)/100)
    for i, resource in enumerate(('Metal','Energy')):
        path = RES + '/Readout' + resource
        # The engine anchors ResourcePanel to the right; positive local coordinates
        # therefore stay in the corner at every screen width.
        if placement == 'corner': ctl(path).set('Position', [42, i*40])
        else:
            p=ctl(path).get('Position').array()
            ctl(path).set('Position',[p[0],ctl(ITEMS).get('Position').array()[1]-extra-40-485])
        if not plain: colour(path, round(255*background/100))
        if tactical: node(path).set('Texture', 'ui/textures/HUD/custom/tactical_readout.dds')
    # Scale complete build cards, including costs, locks and alert anchors.
    for path, (_, n, c) in paths.items():
        if path.endswith('/PrototypeListItem/Button') and '/HUDItems/TabbedItemPanel/' in path:
            old = c.get('Size').array(); ratio = (53*scale/100)/old[0]
            c.set('Size', [round(v*ratio,2) for v in old[:2]])
            for child, (_, cn, cc) in paths.items():
                if child.startswith(path+'/') and child.count('/')==path.count('/')+1:
                    p=cc.get('Position')
                    if isinstance(p,ui.Table): cc.set('Position',[round(v*ratio,2) for v in p.array()[:2]])
                    if child.endswith('Cost') and isinstance(cn.get('Size'),(float,int)): cn.set('Size', max(5.5,7*scale/80))
            for sibling in ('Lock','Alert'):
                p=path.rsplit('/',1)[0]+'/'+sibling
                if p in paths:
                    v=ctl(p).get('Position')
                    if isinstance(v,ui.Table): ctl(p).set('Position',[round(x*ratio,2) for x in v.array()[:2]])
    # Give 100%-size cards the full vanilla strip height, and keep their bottom
    # edge fixed. Hidden-HUD anchors remain at their original, tested locations.
    for path in (ITEMS, ITEMS+'/TabbedItemPanel'):
        size=ctl(path).get('Size').array();ctl(path).set('Size',[size[0],max(1,size[1]+extra)])
    header=ctl(ITEMS+'/TabbedItemPanel/Header');pos=header.get('Position').array();header.set('Position',[pos[0],pos[1]+extra])
    for path, (_,n,c) in paths.items():
        if path.startswith(ITEMS+'/TabbedItemPanel/Panel') and c.get('Size') and not '/PrototypeListItem' in path:
            size=c.get('Size').array()
            if len(size)>=2: c.set('Size',[size[0],max(1,size[1]+extra)])
    if extra:
        p=ctl(ITEMS).get('Position').array();ctl(ITEMS).set('Position',[p[0],p[1]-extra])
        # Absolute corner overlays must not drift with the build strip.
        for name in ('COBackground','ReactorBars','ResourcePanelEnemy'):
            path=ITEMS+'/'+name
            if path in paths:
                p=ctl(path).get('Position').array();ctl(path).set('Position',[p[0],p[1]+extra])
    backgrounds=[PANEL+'/'+k for k in ('PanelLeft','PanelCenter','PanelCenterL','PanelCenterR')]+[RES,ITEMS+'/ResourcePanelEnemy']
    for path in backgrounds:
        if plain or path not in paths: continue
        alpha=round(255*background/100)
        # The broad centre backing is deliberately absent in the tactical theme.
        if tactical and '/PanelCenter' in path: alpha=0
        colour(path,alpha)
        if tactical and path in (PANEL+'/PanelLeft',RES): node(path).set('Texture','ui/textures/HUD/custom/tactical_panel.dds')
    if tactical:
        ctl(PANEL+'/ItemDetail').set('Position',[10,504])
        reactor=ctl(ITEMS+'/ReactorBars');pos=reactor.get('Position').array()
        reactor.set('Position',[pos[0],485-84-ctl(ITEMS).get('Position').array()[1]])
        # The original controls still supply live costs/weapon details on the left.
        # Do not synthesize numbers or move controls into a new engine path.
    enemy=ITEMS+'/ResourcePanelEnemy'
    if placement=='corner' and enemy in paths:
        pos=ctl(enemy).get('Position').array();h=ctl(enemy).get('Size').array()[1]
        ctl(enemy).set('Position',[pos[0],485-h-8-ctl(ITEMS).get('Position').array()[1]])
    for name in ('Grid','Autodoor','AutoCrossBrace'):
        path=RES+'/'+name
        if path in paths and not indicators:
            p=ctl(path).get('Position').array();ctl(path).set('Position',[p[0],p[1]+400])
    # Starkoff (compact) keeps its original panels; only Tactical gets the new chrome.
    if tactical: apply_chrome(globals_,placement,background,tactical)
    return globals_


def main(game):
    payload=ROOT/'setup'/'payload'
    target=payload/'customization'; target.mkdir(exist_ok=True)
    sources={'compact':ROOT/'build/hud.lua','tactical':ROOT/'build/hud.lua'}
    staging=ROOT/'build'/'customization';staging.mkdir(exist_ok=True)
    total=0
    for layout,source in sources.items():
        out=target/'variants'/layout;out.mkdir(parents=True,exist_ok=True)
        original,_=ui.load(str(source),game)
        original_names=set(ui.unique_paths(ui.find_root(original)))
        # Starkoff: only the resource position is configurable (original size/panels/colours).
        combos=[('corner',80,0,True),('center',80,0,True)] if layout=='compact' else product(('corner','center'),(65,80,100),(0,50,100),(False,True))
        for placement,scale,bg,indicators in combos:
            g,f32=ui.load(str(source),game)
            customize(g,placement,scale,bg,indicators,layout=='tactical',layout=='compact')
            paths=ui.unique_paths(ui.find_root(g))
            added=({RES+'/CustomizationStrip'}|({RES+'/CustomizationBackdrop'} if placement=='corner' else set())) if layout=='tactical' else set()
            expected=original_names|added
            if layout=='tactical':
                old=RES+'/AssistantSettingsPanel/WeaponLeft'
                expected={p.replace(old,PANEL+'/ItemDetail/WeaponLeft') if p==old or p.startswith(old+'/') else p for p in expected}
            assert set(paths)==expected, 'Customization changed unexpected engine control paths'
            for i,res in enumerate(('Metal','Energy')):
                pos=paths[RES+'/Readout'+res][2].get('Position').array()
                if placement=='corner': assert pos==([60,12+i*36] if layout=='tactical' else [42,i*40])
            for n in ('Grid','Autodoor','AutoCrossBrace'):
                p=RES+'/'+n
                if p in paths and indicators: assert paths[p][2].get('Position').array()[1]<120
            if layout=='tactical':
                strip=paths[RES+'/CustomizationStrip'][2];sx,sy=strip.get('Position').array()[:2];sw,sh=strip.get('Size').array()[:2]
                shown=[paths[RES+'/'+n][2] for n in ('Grid','Autodoor','AutoCrossBrace','Pause','Menu') if RES+'/'+n in paths and paths[RES+'/'+n][2].get('Position').array()[1]<300]
                assert shown, 'Pause and Menu must stay visible'
                for ic in shown:
                    ix,iy=ic.get('Position').array()[:2];iw,ih=ic.get('Size').array()[:2]
                    assert sx<=ix and ix+iw<=sx+sw and sy<=iy and iy+ih<=sy+sh, 'Icon outside its strip'
                # Icons are evenly spread and the strip hugs them vertically (3 units above and below).
                assert abs((shown[0].get('Position').array()[1]-sy)-(sy+sh-shown[0].get('Position').array()[1]-shown[0].get('Size').array()[1]))<0.01
                if placement=='corner':
                    assert sw>=paths[RES+'/ReadoutMetal'][2].get('Size').array()[0], 'Icon strip is shorter than the resource panel'
            if placement=='corner':
                enemy=paths[ITEMS+'/ResourcePanelEnemy'][2]
                assert paths[ITEMS][2].get('Position').array()[1]+enemy.get('Position').array()[1]+enemy.get('Size').array()[1]<=485-8
            else:
                readout=paths[RES+'/ReadoutMetal'][2]
                cards_top=paths[ITEMS][2].get('Position').array()[1]+paths[ITEMS+'/TabbedItemPanel'][2].get('Position').array()[1]
                assert 485+readout.get('Position').array()[1]+readout.get('Size').array()[1] <= cards_top
            name=f'{placement}-{scale}-{bg}-{int(indicators)}'
            lua=staging/(layout+'-'+name+'.lua');lua.write_text(ui.to_source(g,f32),encoding='latin-1')
            ui.compile_lua(str(lua),str(out/(name+'.lua')),game);total+=1
    tactical=payload/'layouts/tactical';tactical.mkdir(exist_ok=True)
    shutil.copy2(target/'variants/tactical/corner-80-100-1.lua',tactical/'hud.lua')
    for icon in ('extinguisher','under_construction','repair'):
        shutil.copy2(ROOT/f'setup/app/assets/icon_compact_{icon}.png',ROOT/f'setup/app/assets/icon_tactical_{icon}.png')
    shutil.copytree(payload/'icons/compact',payload/'icons/tactical',dirs_exist_ok=True)
    # Runtime hue rotation only needs UI chrome. Decode DDS here, at build time;
    # Warnings, reactor charge, characters, world icons and menu scenes are excluded.
    for skin in ('base','dlc1','dlc2','dlc3','pro','elite'):
        out=target/'skins'/skin;out.mkdir(parents=True,exist_ok=True)
        if skin=='base':
            folder=Path(game)/'data/ui/textures/HUD/skin_base'
            for p in folder.rglob('*.dds'):
                rel=p.relative_to(folder)
                # Not HUD chrome: the pause menu plate, end screens, achievements, fire alarms and the replay UI
                # keep their original colours.
                if any(x in p.name.lower() for x in ('reactor','radial','star','pausebg','ui-defeat','ui-victory','achievementspanel','firealarm')) or 'replay' in rel.parts: continue
                src=vanilla_texture(game,str(Path('HUD/skin_base')/rel))
                dest=out/'HUD/skin_base'/rel;dest.parent.mkdir(parents=True,exist_ok=True)
                open_image(src).save(dest,'DDS')
        folder=payload/'hud/data/ui/textures/HUD/custom' if skin=='dlc3' else payload/'skins'/skin
        for p in folder.glob('*.dds'):
            dest=out/'HUD/custom'/p.name;dest.parent.mkdir(parents=True,exist_ok=True)
            open_image(p).save(dest,'DDS')
    print(f'{total} validated/compiled HUD variants; all control paths preserved')


if __name__=='__main__': main(sys.argv[1] if len(sys.argv)>1 else ui.DEFAULT_GAME)
