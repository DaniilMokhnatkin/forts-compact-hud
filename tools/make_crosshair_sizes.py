"""Build centred cursor textures and previews; installer needs no Python."""
from pathlib import Path
import sys
from PIL import Image, DdsImagePlugin


def build(payload: Path, assets: Path):
    for folder in payload.iterdir():
        if not folder.is_dir():
            continue
        if open(folder / 'mouse_target.dds', 'rb').read(88)[84:88] != b'\0\0\0\0':
            # Compressed art: the installer tints colours from an uncompressed copy.
            Image.open(folder / 'mouse_target.dds').convert('RGBA').save(folder / 'tint.dds', 'DDS')
        for percent in range(50, 201, 5):
            for name in ('mouse_target', 'mouse_target_painted'):
                source = Image.open(folder / f'{name}.dds').convert('RGBA')
                if percent > 110:
                    # The game scales its cursor quad for these values; keep DDS
                    # intact and render that geometry on a roomy preview canvas.
                    output = source.resize((round(64 * percent / 100),) * 2, Image.Resampling.BICUBIC)
                elif percent == 100:
                    output = source
                else:
                    # Transform about the texture centre, preserving its hotspot.
                    scale = percent / 100
                    centre = source.width / 2
                    output = source.transform(source.size, Image.Transform.AFFINE,
                        (1 / scale, 0, centre * (1 - 1 / scale),
                         0, 1 / scale, centre * (1 - 1 / scale)),
                        Image.Resampling.BICUBIC)
                    dest = folder / 'sizes' / str(percent)
                    dest.mkdir(parents=True, exist_ok=True)
                    output.save(dest / f'{name}.dds', 'DDS')
                if name == 'mouse_target':
                    preview = Image.new('RGBA', (128, 128))
                    preview.paste(output, ((128 - output.width) // 2, (128 - output.height) // 2))
                    preview.save(assets / f'crosshair_{folder.name}_{percent}.png')


if __name__ == '__main__':
    build(Path(sys.argv[1]), Path(sys.argv[2]))
