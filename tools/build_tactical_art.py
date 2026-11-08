"""Export ImageGen's empty tactical chrome and compose the existing repo glyph set."""
from pathlib import Path
import shutil
from PIL import Image, ImageEnhance
import draw_buttons as db
from build_layouts import open_image
from build_zors_hud import states, extra_glyphs

ROOT=Path(__file__).resolve().parents[1]


def main():
    source=Image.open(ROOT/'textures/tactical/source-sheet.png').convert('RGBA')
    pieces={'panel':(152,78,681,713),'button':(993,256,1295,557),
            'tab':(62,824,733,935),'readout':(804,824,1476,935)}
    art={k:source.crop(box) for k,box in pieces.items()}
    out=ROOT/'setup/payload/layouts/tactical/textures/HUD'
    def save(im,path):path.parent.mkdir(parents=True,exist_ok=True);im.save(path,'DDS')
    for name in ('panel','readout'):
        save(art[name].resize((256,256) if name=='panel' else (256,64),Image.Resampling.LANCZOS),out/'custom'/f'tactical_{name}.dds')
    base=Image.new('RGBA',(64,64));base.alpha_composite(art['button'].resize((58,58),Image.Resampling.LANCZOS),(3,3))
    for folder,glyphs in [('custom',db.glyphs()),('skin_base',extra_glyphs())]:
        for name,shapes in glyphs.items():
            glyph=db.skin_glyph_layer(shapes,(32,32),((245,246,255),(211,217,245)),0.95)
            normal=Image.alpha_composite(base,glyph)
            for state,img in states(normal).items():save(img,out/folder/f'{name}-{state}.dds')
    for state,img in states(art['tab'].resize((256,64),Image.Resampling.LANCZOS)).items():save(img,out/'skin_base/tabs'/f'Button-Long-{state}.dds')
    # Original live glyphs on the magnifier/reactor/commander are retained; they
    # come from the game's own textures rather than invented raster symbols.
    shutil.copy2(ROOT/'textures/tactical/reference.png',ROOT/'setup/app/assets/tactical_reference.png')
    art['panel'].save(ROOT/'setup/app/assets/tactical_panel.png')
    print('Tactical chrome exported from ImageGen source; existing functional glyphs retained')


if __name__=='__main__':main()
