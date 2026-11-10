"""Live readouts aligned to the generated Grib wells, verified against game feedback."""
from PIL import Image, ImageDraw

# CO-panel-background / CO-panel-gauge UVs in Forts 1.39 uisprites.lua.
COMMANDER_UV = (.128, .14, .859, .855)


def centre_commander_fields(paths, texture_dir, items_path):
    for parent, frame_name, children in (
        (items_path + '/COBackground', 'COGauge_BG.dds', [('COActivate', 'COGauge_Button-A.dds'), ('COGauge', 'COGauge_Radial.dds'), ('COStar', 'COGauge_Star.dds')]),
        (items_path + '/ResourcePanelEnemy/COBackground_Enemy', 'COGauge_BG_enemy.dds', [('COActivate_Enemy', 'COGauge_Button_enemy-A.dds'), ('COGauge_Enemy', 'COGauge_Radial_enemy.dds')])):
        frame = Image.open(texture_dir / frame_name).convert('RGBA')
        hole = frame.getchannel('A').point(lambda a: 255 if a >= 24 else 0)
        seed = (frame.width // 2, frame.height // 2)
        if hole.getpixel(seed) != 0:
            raise ValueError('Commander frame has no transparent centre')
        ImageDraw.floodfill(hole, seed, 128)
        box = hole.point(lambda a: 255 if a == 128 else 0).getbbox()
        if not box or box[0] == 0 or box[1] == 0 or box[2] == frame.width or box[3] == frame.height:
            raise ValueError('Commander frame centre is not enclosed')
        fw, fh = paths[parent][2].get('Size').array()
        # Background anchor is bottom-left; the enemy frame is mirrored horizontally.
        left, top, right, bottom = COMMANDER_UV
        cx = fw * ((box[0] + box[2]) / 2 / frame.width - left) / (right - left)
        cy = -fh + fh * ((box[1] + box[3]) / 2 / frame.height - top) / (bottom - top)
        opening_w = abs(fw) * (box[2] - box[0]) / frame.width / (right - left)
        opening_h = fh * (box[3] - box[1]) / frame.height / (bottom - top)
        for child, name in children:
            image = Image.open(texture_dir / name).convert('RGBA')
            radial = child.startswith('COGauge')
            # The radial alpha is an angular charge ramp, not ordinary opacity.
            box = image.getchannel('A').point(lambda a: 255 if a >= (1 if radial else 128) else 0).getbbox()
            control = paths[parent + '/' + child][2]; w, h = control.get('Size').array()
            left, top, right, bottom = COMMANDER_UV if radial else (0, 0, 1, 1)
            if radial:
                fill_w = abs(w) * (box[2] - box[0]) / image.width / (right - left)
                fill_h = h * (box[3] - box[1]) / image.height / (bottom - top)
                scale = min((opening_w - 1) / fill_w, (opening_h - 1) / fill_h)
                w, h = round(w * scale, 3), round(h * scale, 3)
                control.set('Size', [w, h])
            control.set('Position', [round(cx - w * ((box[0] + box[2]) / 2 / image.width - left) / (right - left), 3),
                                     round(cy - h * ((box[1] + box[3]) / 2 / image.height - top) / (bottom - top), 3)])


def align_fields(paths, placement, resource_path, items_path):
    for resource in ('Metal', 'Energy'):
        box = resource_path + '/Readout' + resource
        rate_node, rate_control = paths[box + '/' + resource + 'Rate'][1:]
        # The old right edge sat on the ivory bezel; reserve a real dark gutter.
        rate_control.set('Position', [120, 12 if placement == 'corner' else 10])
        rate_control.set('Size', [52, 8])
        rate_node.set('Size', 8)
        for name in (resource, resource + 'Cap'):
            control = paths[box + '/' + name][2]
            control.set('Position', [control.get('Position').array()[0], 23 if placement == 'corner' else 20])
    spy = items_path + '/ResourcePanelEnemy'
    # PanelSpy has one header and two resource wells. Native y=40/64 placed
    # amounts in the header and on the divider of this new texture.
    for resource, y in (('Metal', 59), ('Energy', 79)):
        for suffix, x, width in (('', 70, 32), ('Cap', 105, 32), ('Rate', 144, 36)):
            node, control = paths[spy + '/' + resource + suffix][1:]
            control.set('Position', [x, y]); control.set('Size', [width, 9]); control.set('Anchor', 3)
            node.set('Size', 7); node.set('Font', 'readout')
    title = paths[spy + '/LastDeviceBox'][2]
    title.set('Position', [16, 28]); title.set('Size', [108, 12])
    node, control = paths[spy + '/LastDeviceBox/LastDevice'][1:]
    node.set('Size', 7.5)
    control.set('Position', [54, 1]); control.set('Size', [108, 10]); control.set('Anchor', 1)
    control.set('ScriptX', 'ParentMiddleX')
    button = paths[spy + '/SpyNext'][2]
    button.set('Position', [135, 28]); button.set('Size', [16, 16])
    for index in range(5):
        bar = items_path + '/ReactorBars/ReactorBG' + str(index)
        button = paths[bar + '/ReactorButton' + str(index)][2]
        gauge = paths[bar + '/ReactorGauge' + str(index)][2]
        x, y = gauge.get('Position').array(); w, h = gauge.get('Size').array()
        start = button.get('Position').array()[0] + button.get('Size').array()[0] + 4
        # Extend the live gauge into the old vanilla spacer, retaining its end.
        gauge.set('Position', [start, y]); gauge.set('Size', [x + w - start, h])
