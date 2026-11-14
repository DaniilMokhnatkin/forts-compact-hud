"""Copy the installed game's original Forts wordmark into the ignored payload."""
import sys
from pathlib import Path
from build_layouts import open_image


def prepare(game, payload):
    source = Path(game) / "data/ui/textures/FortsLogo.dds"
    output = Path(payload) / "backgrounds/forts-logo.png"
    output.parent.mkdir(parents=True, exist_ok=True)
    open_image(str(source)).save(output)


if __name__ == "__main__":
    prepare(*sys.argv[1:])
