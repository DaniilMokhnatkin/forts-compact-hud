# Встроенные фоны меню

В `setup/app/assets/backgrounds/` хранятся исходники встроенных наборов.
Папка автоматически входит в EXE при `tools/build_native_setup.ps1`.
Готовые наборы не требуют сети или выбора файлов.

- `sunset.png`: исходное изображение, приложенное пользователем 2026-11-14;
  копия без изменений. SHA-256: `7943A61AC710910E8C05FBEDEED5588D95C06D7161CCC5DFB62E466B92247559`.
- `zors-main.png`, `zors-screens.png`: соответствующие `FE-MainMenu-BG.dds`
  и `FE-AllScreens-BG.dds` из имеющегося оформления Zors.o, без художественных
  изменений; DDS декодированы в PNG штатным инструментарием проекта (Pillow).
  Автор оформления: [Zors.o](https://steamcommunity.com/profiles/76561199099877561).
- `grib-forest.png`: новая иллюстрация, созданная встроенным `image_gen`.
  Референсы: манера рисовки пользовательского пейзажа и палитра `theme_grib.png`.
  По уточнению пользователя сцена самостоятельная, без гор и закатной композиции.

При выборе набора `BackgroundImage.Encode` создаёт текстуру без обрезки тем же
путём, что и при загрузке своего файла. У Zors.o сохраняется отдельная картинка
для остальных экранов. У двух других наборов одна иллюстрация для обоих слотов.

## Финальный промпт для грибного леса

Create one NEW widescreen 16:9 illustrated game menu background. Image1 is a DRAWING STYLE reference ONLY: crisp angular polygon-like hand-painted planes, clean confident shapes, controlled graphic detail, dramatic light, atmospheric layering. DO NOT reproduce its scene, its snowy mountains, its sunset sky or its winding path. Completely different subject and composition. Image2 is the red-and-ivory mushroom HUD palette reference. Scene: an ancient enchanted MUSHROOM FOREST viewed from ground level inside a secluded rocky grotto. Monumental scarlet fly agaric mushrooms with broad red caps and large ivory spots form a sheltering canopy overhead on the RIGHT and upper edge. Their cream stems grow among moss-covered broken stone terraces, gnarled roots, ochre ferns, tiny russet mushrooms and a shallow still stream reflecting warm amber light. Sunbeams from a small opening high at the back-right illuminate suspended spores and the undersides of giant mushroom gills, creating rich warm ivory highlights against deep burgundy, forest green, dark teal and earthy purple shadows. Organic asymmetrical composition: layered mushroom silhouettes recede into mysterious forest depth in centre-right; LEFT third is a calm dark mossy rock face with sparse foliage and subtle texture, deliberately empty enough for the game's menu labels. Build convincing depth and grand scale. Mature sophisticated fantasy game illustration, cozy and mysterious rather than childish. The visual language is angular painted facets, NOT photorealism and not a 3D render; preserve the reference's drawing style but invent the entire environment. Distinct mushroom forest, absolutely NO snowy peaks, NO alpine panorama, NO red-orange sunset horizon, NO copy of reference layout. No characters, no faces, no houses, no buildings, no writing, no logos, no interface, no HUD, no buttons, no border. Full bleed high-quality finished artwork.
