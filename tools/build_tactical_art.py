"""Export ImageGen's empty tactical chrome and compose the existing repo glyph set."""
from pathlib import Path
import shutil
from PIL import Image, ImageEnhance
import draw_buttons as db
from build_layouts import open_image, vanilla_texture
from build_zors_hud import states, extra_glyphs
import forts_ui as ui

ROOT=Path(__file__).resolve().parents[1]


def main():
    source=Image.open(ROOT/'textures/tactical/source-sheet.png').convert('RGBA')
    pieces={'panel':(152,78,681,713),'button':(993,256,1295,557),
            'tab':(62,824,733,935),'readout':(804,824,1476,935)}
    art={k:source.crop(box) for k,box in pieces.items()}
    out=ROOT/'setup/payload/layouts/tactical/textures/HUD'
    def save(im,path):path.parent.mkdir(parents=True,exist_ok=True);im.save(path,'DDS')
    def fit(im,rel):
        original=open_image(vanilla_texture(ui.DEFAULT_GAME,'HUD/skin_base/'+rel))
        box=original.getchannel('A').point(lambda a:255 if a>24 else 0).getbbox()
        canvas=Image.new('RGBA',original.size)
        canvas.alpha_composite(im.resize((box[2]-box[0],box[3]-box[1]),Image.Resampling.LANCZOS),box[:2])
        return canvas
    shared=ROOT/'setup/payload/customization/textures/HUD/custom'
    save(art['panel'].resize((256,256),Image.Resampling.LANCZOS),shared/'customization_panel.dds')
    save(art['readout'].resize((256,64),Image.Resampling.LANCZOS),shared/'customization_readout.dds')
    for name in ('panel','readout'):
        save(art[name].resize((256,256) if name=='panel' else (256,64),Image.Resampling.LANCZOS),out/'custom'/f'tactical_{name}.dds')
    base=Image.new('RGBA',(64,64));base.alpha_composite(art['button'].resize((58,58),Image.Resampling.LANCZOS),(3,3))
    for folder,glyphs in [('custom',db.glyphs()),('skin_base',extra_glyphs())]:
        for name,shapes in glyphs.items():
            glyph=db.skin_glyph_layer(shapes,(32,32),((245,246,255),(211,217,245)),0.95)
            normal=Image.alpha_composite(base,glyph)
            for state,img in states(normal).items():save(img,out/folder/f'{name}-{state}.dds')
    for state,img in states(art['tab'].resize((256,64),Image.Resampling.LANCZOS)).items():save(img,out/'skin_base/tabs'/f'Button-Long-{state}.dds')
    fixes=Image.open(ROOT/'textures/tactical/corner-parts-v2.png').convert('RGBA')
    w,h=fixes.size
    def piece(box):
        im=fixes.crop(tuple(round(v) for v in (box[0]*w,box[1]*h,box[2]*w,box[3]*h)))
        bounds=im.getchannel('A').point(lambda a:255 if a>=128 else 0).getbbox()
        return im.crop(bounds)
    spy=piece((0,.08,.66,.45));magnifier=piece((.67,.08,.99,.45))
    circle=piece((.01,.47,.49,.94));star=piece((.51,.47,.98,.94))
    canvas=Image.new('RGBA',(512,256));canvas.alpha_composite(spy.resize((352,187),Image.Resampling.LANCZOS),(85,69))
    save(canvas,out/'custom/tactical_spy.dds');save(canvas,shared/'customization_spy.dds')
    for state,img in states(magnifier.resize((64,64),Image.Resampling.LANCZOS)).items():save(img,out/'skin_base'/f'HUD-Spy-{state}.dds')
    for name in ('COGauge_BG','COGauge_BG_enemy','COGauge_BG_HL','COGauge_BG_HL_enemy'):
        save(fit(circle,'commander/'+name+'.dds'),out/'skin_base/commander'/(name+'.dds'))
    for prefix in ('COGauge_Button','COGauge_Button_enemy'):
        for state,img in states(fit(star,'commander/'+prefix+'-A.dds')).items():save(img,out/'skin_base/commander'/f'{prefix}-{state}.dds')
    flat=Image.open(ROOT/'textures/tactical/flat-hud-v3.png').convert('RGBA')
    for name,box,size in (
        ('tactical_detail',(82,110,713,543),(320,230)),
        ('tactical_resource',(858,110,1454,543),(264,230)),
        ('tactical_reactor_track',(80,680,1456,762),(320,44)),
        ('tactical_reactor_fill',(350,860,1426,955),(256,32)),
    ):
        save(flat.crop(box).resize(size,Image.Resampling.LANCZOS),out/'custom'/(name+'.dds'))
    buttons=Image.open(ROOT/'textures/tactical/flat-buttons-v3.png').convert('RGBA')
    def button_piece(box):
        im=buttons.crop(tuple(round(v*s) for v,s in zip(box,(buttons.width,buttons.height,buttons.width,buttons.height))))
        return im.crop(im.getchannel('A').point(lambda a:255 if a>=128 else 0).getbbox())
    for name,box in (('FacingLeft',(0,0,.34,.59)),('FacingAuto',(.34,0,.66,.59)),('FacingRight',(.66,0,1,.59))):
        normal=button_piece(box).resize((64,64),Image.Resampling.LANCZOS)
        for state,img in states(normal).items():save(img,out/'custom'/f'{name}-{state}.dds')
    save(button_piece((0,.6,1,1)).resize((128,40),Image.Resampling.LANCZOS),out/'custom/tactical_reactor_button.dds')
    # Original live glyphs on the magnifier/reactor/commander are retained; they
    # come from the game's own textures rather than invented raster symbols.
    shutil.copy2(ROOT/'textures/tactical/reference.png',ROOT/'setup/app/assets/tactical_reference.png')
    art['panel'].save(ROOT/'setup/app/assets/tactical_panel.png')
    print('Tactical chrome exported from ImageGen source; existing functional glyphs retained')


if __name__=='__main__':main()
