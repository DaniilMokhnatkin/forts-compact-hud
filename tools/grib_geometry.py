"""Live readouts aligned to the generated Grib wells, verified against game feedback."""
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
