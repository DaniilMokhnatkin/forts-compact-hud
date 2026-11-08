#!/usr/bin/env python3
"""Decompile, map and check Forts UI screen files.

Forts ships a few UI layouts (ui/screens/hud.lua, ui/ui.lua, ...) as LuaPlus
5.1 bytecode with 4-byte floats. These chunks only build data tables, so a
small VM that understands table construction recovers the exact source.
Text files are compiled with the game's own LuaPlusC.exe before reading, so
every command accepts either form.

  decompile IN [-o OUT]       bytecode -> Lua source
  map IN [-o OUT]             control tree with positions, sizes, layout scripts
  compare A B                 structural comparison of two layouts
  check ORIGINAL EDITED       syntax + every original control name still present
  compile IN OUT              compile with LuaPlusC (stripped, like the game)
"""
import argparse
import os
import struct
import subprocess
import sys
import tempfile

DEFAULT_GAME = r"C:\Program Files (x86)\Steam\steamapps\common\Forts"
FIELDS_PER_FLUSH = 50  # LFIELDS_PER_FLUSH in Lua 5.1

OP_MOVE, OP_LOADK, OP_LOADBOOL, OP_LOADNIL = 0, 1, 2, 3
OP_GETGLOBAL, OP_SETGLOBAL, OP_SETTABLE, OP_NEWTABLE = 5, 7, 9, 10
OP_CALL, OP_RETURN, OP_SETLIST = 28, 30, 34
OPNAMES = (
    "MOVE LOADK LOADBOOL LOADNIL GETUPVAL GETGLOBAL GETTABLE SETGLOBAL SETUPVAL "
    "SETTABLE NEWTABLE SELF ADD SUB MUL DIV MOD POW UNM NOT LEN CONCAT JMP EQ LT "
    "LE TEST TESTSET CALL TAILCALL RETURN FORLOOP FORPREP TFORLOOP SETLIST CLOSE "
    "CLOSURE VARARG"
).split()

LUA_KEYWORDS = set(
    "and break do else elseif end false for function if in local nil not or "
    "repeat return then true until while".split()
)


# ---------------------------------------------------------------- bytecode

class Table:
    """Lua table that remembers where each field started in the bytecode."""

    def __init__(self):
        self.entries = []  # (start_pc, key, value, is_array)

    def ordered(self):
        return sorted(self.entries, key=lambda e: e[0])

    def get(self, key, default=None):
        for _, k, v, _ in self.entries:
            if k == key:
                return v
        return default

    def set(self, key, value):
        """Replace a field in place, keeping its position in the source."""
        if isinstance(value, (list, tuple)):
            value = Table.of(value)
        elif isinstance(value, int) and not isinstance(value, bool):
            value = float(value)  # Lua numbers are floats
        for i, (start, k, _, is_array) in enumerate(self.entries):
            if k == key:
                self.entries[i] = (start, k, value, is_array)
                return
        self.entries.append((float("inf"), key, value, False))

    @staticmethod
    def of(items):
        t = Table()
        t.entries = [(i, i + 1, float(v) if isinstance(v, (int, float)) and not isinstance(v, bool) else v, True)
                     for i, v in enumerate(items)]
        return t

    def array(self):
        items = sorted((k, v) for _, k, v, is_array in self.entries if is_array)
        return [v for _, v in items]

    def hash_items(self):
        return [(k, v) for _, k, v, is_array in self.ordered() if not is_array]


class Global:
    def __init__(self, name):
        self.name = name


class Call:
    """Call to a global helper such as ButtonSprite(...), kept as source."""

    def __init__(self, func, args):
        self.func = func
        self.args = args


class Reader:
    def __init__(self, data):
        self.data = data
        self.pos = 0

    def take(self, n):
        chunk = self.data[self.pos:self.pos + n]
        if len(chunk) != n:
            raise ValueError("unexpected end of chunk")
        self.pos += n
        return chunk

    def byte(self):
        return self.take(1)[0]

    def header(self):
        if self.take(4) != b"\x1bLua":
            raise ValueError("not a Lua bytecode file")
        version, fmt, endian = self.byte(), self.byte(), self.byte()
        if version != 0x51 or fmt != 0 or endian != 1:
            raise ValueError("expected little-endian Lua 5.1 bytecode")
        self.int_size, self.size_t, self.instr_size, self.num_size = (self.byte() for _ in range(4))
        self.integral = self.byte()
        if self.integral or self.num_size not in (4, 8) or self.instr_size != 4:
            raise ValueError("unsupported number or instruction format")
        self.f32 = self.num_size == 4

    def int(self):
        return int.from_bytes(self.take(self.int_size), "little", signed=True)

    def string(self):
        n = int.from_bytes(self.take(self.size_t), "little")
        return None if n == 0 else self.take(n)[:-1].decode("latin-1")

    def number(self):
        return struct.unpack("<f" if self.f32 else "<d", self.take(self.num_size))[0]

    def function(self):
        fn = {"source": self.string()}
        self.int(), self.int()  # line defined / last line defined
        self.take(4)            # nups, numparams, is_vararg, maxstacksize
        fn["code"] = [int.from_bytes(self.take(4), "little") for _ in range(self.int())]
        consts = []
        for _ in range(self.int()):
            kind = self.byte()
            if kind == 0:
                consts.append(None)
            elif kind == 1:
                consts.append(self.byte() != 0)
            elif kind == 3:
                consts.append(self.number())
            elif kind == 4:
                consts.append(self.string())
            else:
                raise ValueError(f"unsupported constant type {kind}")
        fn["consts"] = consts
        fn["protos"] = [self.function() for _ in range(self.int())]
        self.take(self.int_size * self.int())  # line info
        for _ in range(self.int()):            # local variables
            self.string(), self.int(), self.int()
        for _ in range(self.int()):            # upvalue names
            self.string()
        return fn


def run_chunk(fn):
    """Execute a table-building main chunk and return its globals in order."""
    if fn["protos"]:
        raise ValueError("chunk defines functions; only data chunks are supported")
    code, K = fn["code"], fn["consts"]
    regs, start, globals_ = {}, {}, []
    top = 0  # first free register after an open (multi-result) call

    def rk(x, pc):
        if x & 0x100:
            return K[x & 0xFF], pc
        return regs[x], start[x]

    pc = 0
    while pc < len(code):
        ins = code[pc]
        op, a = ins & 0x3F, (ins >> 6) & 0xFF
        c, b, bx = (ins >> 14) & 0x1FF, (ins >> 23) & 0x1FF, (ins >> 14) & 0x3FFFF
        if op == OP_LOADK:
            regs[a], start[a] = K[bx], pc
        elif op == OP_LOADBOOL:
            regs[a], start[a] = b != 0, pc
            if c:
                pc += 1
        elif op == OP_LOADNIL:
            for r in range(a, b + 1):
                regs[r], start[r] = None, pc
        elif op == OP_MOVE:
            regs[a], start[a] = regs[b], start[b]
        elif op == OP_NEWTABLE:
            regs[a], start[a] = Table(), pc
        elif op == OP_SETTABLE:
            key, key_pc = rk(b, pc)
            value, value_pc = rk(c, pc)
            regs[a].entries.append((min(key_pc, value_pc), key, value, False))
        elif op == OP_GETGLOBAL:
            regs[a], start[a] = Global(K[bx]), pc
        elif op == OP_CALL:
            if not isinstance(regs[a], Global):
                raise ValueError(f"call of a non-global at instruction {pc + 1}")
            last = (top if b == 0 else a + b) - 1
            regs[a] = Call(regs[a].name, [regs[r] for r in range(a + 1, last + 1)])
            if c == 0:
                top = a + 1
            elif c == 1:
                globals_.append((None, regs[a]))  # statement such as dofile(...)
            elif c != 2:
                raise ValueError(f"call with {c - 1} results at instruction {pc + 1}")
        elif op == OP_SETLIST:
            if b == 0:
                b = top - a - 1
            if c == 0:
                pc += 1
                c = code[pc]
            base = (c - 1) * FIELDS_PER_FLUSH
            for j in range(1, b + 1):
                regs[a].entries.append((start[a + j], base + j, regs[a + j], True))
        elif op == OP_SETGLOBAL:
            globals_.append((K[bx], regs[a]))
        elif op == OP_RETURN:
            break
        else:
            raise ValueError(f"opcode {OPNAMES[op]} at instruction {pc + 1} is not supported")
        pc += 1
    return globals_


# ---------------------------------------------------------------- loading

def luac_path(game):
    path = os.path.join(game, "LuaPlusC.exe")
    if not os.path.isfile(path):
        sys.exit(f"LuaPlusC.exe not found in {game}")
    return path


def compile_lua(src, out, game):
    try:
        result = subprocess.run([luac_path(game), "-s", "-o", out, src],
                                capture_output=True, text=True)
    except OSError as err:
        sys.exit(f"cannot run LuaPlusC.exe: {err}")
    if result.returncode != 0:
        msg = (result.stderr or result.stdout).strip()
        raise SyntaxError(msg.replace(luac_path(game) + ": ", ""))


def load(path, game):
    """Return (globals, f32) for a bytecode or text Lua file."""
    with open(path, "rb") as fh:
        data = fh.read()
    if not data.startswith(b"\x1bLua"):
        fd, tmp = tempfile.mkstemp(suffix=".luac")
        os.close(fd)
        try:
            compile_lua(path, tmp, game)
            with open(tmp, "rb") as fh:
                data = fh.read()
        finally:
            os.remove(tmp)
    reader = Reader(data)
    reader.header()
    return run_chunk(reader.function()), reader.f32


# ---------------------------------------------------------------- source output

def fmt_number(v, f32):
    if v == int(v) and abs(v) < 1e15:
        return str(int(v))

    def same(text):
        x = float(text)
        if f32:
            x = struct.unpack("<f", struct.pack("<f", x))[0]
        return x == v

    for digits in range(1, 18):
        text = f"{v:.{digits}g}"
        if same(text):
            return text
    return repr(v)


def fmt_string(s):
    out = []
    for ch in s:
        o = ord(ch)
        if ch in '"\\':
            out.append("\\" + ch)
        elif ch == "\n":
            out.append("\\n")
        elif ch == "\r":
            out.append("\\r")
        elif ch == "\t":
            out.append("\\t")
        elif o < 32 or o == 127:
            out.append(f"\\{o:03d}")
        else:
            out.append(ch)
    return '"' + "".join(out) + '"'


def fmt_scalar(v, f32):
    if v is None:
        return "nil"
    if isinstance(v, bool):
        return "true" if v else "false"
    if isinstance(v, float):
        return fmt_number(v, f32)
    return fmt_string(v)


def fmt_key(k, f32):
    if isinstance(k, str) and k.isidentifier() and k.isascii() and k not in LUA_KEYWORDS:
        return k
    return f"[{fmt_scalar(k, f32)}]"


def is_inline(t):
    """Short lists of scalars, like Position = { 0, 0, }, stay on one line."""
    return len(t.entries) <= 8 and all(
        is_array and not isinstance(v, (Table, Call)) for _, _, v, is_array in t.entries)


def write_value(v, f32):
    """One-line form of any value: scalars, helper calls and inline tables."""
    if isinstance(v, Call):
        return f"{v.func}(" + ", ".join(write_value(x, f32) for x in v.args) + ")"
    if not isinstance(v, Table):
        return fmt_scalar(v, f32)
    if not v.entries:
        return "{ }"
    items = []
    for _, key, x, is_array in v.ordered():
        items.append(("" if is_array else fmt_key(key, f32) + " = ") + write_value(x, f32))
    return "{ " + "".join(item + ", " for item in items) + "}"


def write_table(t, indent, f32, out):
    """Multi-line table in the layout style of the game's text screens."""
    pad = "  " * indent
    out.append(pad + "{\n")
    expected = 1
    for _, key, value, is_array in t.ordered():
        prefix = ""
        if is_array:
            if key != expected:
                raise ValueError("array part is not sequential")
            expected += 1
        else:
            prefix = fmt_key(key, f32) + " ="
        if isinstance(value, Table) and not is_inline(value):
            if prefix:
                out.append(pad + "  " + prefix + "\n")
            write_table(value, indent + 1, f32, out)
        else:
            out.append(pad + "  " + (prefix + " " if prefix else "") + write_value(value, f32))
        out.append(",\n")
    out.append(pad + "}")


def to_source(globals_, f32):
    out = []
    for name, value in globals_:
        if name is None:
            out.append(write_value(value, f32))
        elif isinstance(value, Table) and not is_inline(value):
            out.append(f"{name} =\n")
            write_table(value, 0, f32, out)
        else:
            out.append(f"{name} = {write_value(value, f32)}")
        out.append("\n")
    return "".join(out)


# ---------------------------------------------------------------- control tree

GEOMETRY = ("Position", "Size", "ScriptX", "ScriptY", "ScriptW", "ScriptH", "Anchor", "Visible")


def visual_part(node):
    """Table holding Texture/Text: the node itself, or Static (Button, ListBox) / Text (TextButton)."""
    if isinstance(node.get("Control"), Table):
        return node
    for key in ("Static", "Text"):
        part = node.get(key)
        if isinstance(part, Table) and isinstance(part.get("Control"), Table):
            return part
    return node


def control_part(node):
    """Geometry and children live in a Control table, except on plain Control nodes."""
    ctl = visual_part(node).get("Control")
    return ctl if isinstance(ctl, Table) else node


def child_controls(t):
    """Controls nested anywhere below t: Children lists, ListBox Static parts, tab pages."""
    for _, _, value, _ in t.ordered():
        if isinstance(value, Table):
            if value.get("Type") is not None:
                yield value
            else:
                yield from child_controls(value)


def walk(node, path="", depth=0):
    ctl = control_part(node)
    name = node.get("Name", "?")
    here = f"{path}/{name}" if path else name
    yield here, depth, node, ctl
    for child in child_controls(node):
        yield from walk(child, here, depth + 1)


def unique_paths(root):
    paths, counts = {}, {}
    for path, depth, node, ctl in walk(root):
        counts[path] = counts.get(path, 0) + 1
        key = path if counts[path] == 1 else f"{path}#{counts[path]}"
        paths[key] = (depth, node, ctl)
    return paths


def short(v, f32):
    if isinstance(v, Table):
        return "{" + ", ".join(fmt_scalar(x, f32) for x in v.array()) + "}"
    return fmt_scalar(v, f32)


def find_root(globals_):
    for name, value in globals_:
        if name == "Root" and isinstance(value, Table):
            return value
    sys.exit("no Root table in file")


def map_lines(globals_, f32):
    lines = [
        "# Forts control tree. Units: virtual screen 600 high (1067 wide at 16:9).",
        "# pos/size are relative to the parent; X/Y/W/H are layout scripts that override them.",
        "",
    ]
    for path, (depth, node, ctl) in unique_paths(find_root(globals_)).items():
        parts = [f"{'  ' * depth}{node.get('Name', '?')} [{node.get('Type', '?')}]"]
        for key, label in (("Position", "pos"), ("Size", "size")):
            if ctl.get(key) is not None:
                parts.append(f"{label}={short(ctl.get(key), f32)}")
        for key in ("ScriptX", "ScriptY", "ScriptW", "ScriptH"):
            if ctl.get(key) is not None:
                parts.append(f"{key[-1]}={fmt_string(ctl.get(key))}")
        for key in ("Anchor", "Visible"):
            if ctl.get(key) is not None:
                parts.append(f"{key}={short(ctl.get(key), f32)}")
        visual = visual_part(node)
        for key in ("Texture", "Sprite", "Text", "Style", "Font"):
            value = node.get(key) if key == "Style" else visual.get(key)
            if value is not None and not isinstance(value, Table) and not (key == "Style" and value == "Normal"):
                parts.append(f"{key}={short(value, f32)}")
        lines.append("  ".join(parts))
    return lines


# ---------------------------------------------------------------- comparison

def plain(v):
    if isinstance(v, Call):
        return ("call", v.func, tuple(repr(plain(x)) for x in v.args))
    if isinstance(v, Table):
        return {"array": [plain(x) for x in v.array()],
                "hash": {k: plain(x) for k, x in v.hash_items()}}
    return v


def diff(a, b, path, out, limit=40):
    if len(out) >= limit:
        return
    if isinstance(a, dict) and isinstance(b, dict):
        diff(a["array"], b["array"], path, out, limit)
        for k in a["hash"].keys() | b["hash"].keys():
            if k not in b["hash"]:
                out.append(f"{path}.{k}: removed")
            elif k not in a["hash"]:
                out.append(f"{path}.{k}: added")
            else:
                diff(a["hash"][k], b["hash"][k], f"{path}.{k}", out, limit)
    elif isinstance(a, list) and isinstance(b, list):
        if len(a) != len(b):
            out.append(f"{path}: {len(a)} -> {len(b)} items")
        for i, (x, y) in enumerate(zip(a, b)):
            label = x.get("hash", {}).get("Name", i + 1) if isinstance(x, dict) else i + 1
            diff(x, y, f"{path}[{label}]", out, limit)
    elif a != b:
        out.append(f"{path}: {a!r} -> {b!r}")


# ---------------------------------------------------------------- commands

def write_out(text, out_path):
    if out_path:
        with open(out_path, "w", encoding="latin-1", newline="\n") as fh:
            fh.write(text)
    else:
        sys.stdout.write(text)


def cmd_decompile(args):
    globals_, f32 = load(args.input, args.game)
    write_out(to_source(globals_, f32), args.output)


def cmd_map(args):
    globals_, f32 = load(args.input, args.game)
    write_out("\n".join(map_lines(globals_, f32)) + "\n", args.output)


def cmd_compare(args):
    a, _ = load(args.a, args.game)
    b, _ = load(args.b, args.game)
    out = []
    diff(plain(find_root(a)), plain(find_root(b)), "Root", out)
    if out:
        print("\n".join(out))
        return 1
    print("identical")
    return 0


def cmd_check(args):
    original, f32 = load(args.original, args.game)
    try:
        edited, _ = load(args.edited, args.game)
    except SyntaxError as err:
        print(f"SYNTAX ERROR: {err}")
        return 2
    old = unique_paths(find_root(original))
    new = unique_paths(find_root(edited))
    old_names = {node.get("Name") for _, node, _ in old.values()}
    new_names = {node.get("Name") for _, node, _ in new.values()}
    missing = sorted(n for n in old_names - new_names if n)
    added = sorted(n for n in new_names - old_names if n)
    moved = [p for p in old if p not in new and old[p][1].get("Name") in new_names]
    changed = []
    for path in old.keys() & new.keys():
        o, n = old[path][2], new[path][2]
        for key in GEOMETRY:
            if short(o.get(key), f32) != short(n.get(key), f32):
                changed.append(f"  {path}: {key} {short(o.get(key), f32)} -> {short(n.get(key), f32)}")
    print(f"controls: {len(old)} original, {len(new)} edited")
    if changed:
        print(f"geometry changes ({len(changed)}):")
        print("\n".join(sorted(changed)))
    if moved:
        print(f"re-parented ({len(moved)}): " + ", ".join(moved[:20]))
    if added:
        print("new controls: " + ", ".join(added))
    if missing:
        print("MISSING controls (the engine looks these up by name; hide them instead): "
              + ", ".join(missing))
        return 1
    print("OK: syntax valid, every original control name is present")
    return 0


def cmd_compile(args):
    try:
        compile_lua(args.input, args.output, args.game)
    except SyntaxError as err:
        print(f"SYNTAX ERROR: {err}")
        return 2
    return 0


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--game", default=os.environ.get("FORTS_DIR", DEFAULT_GAME), help="Forts install folder")
    sub = parser.add_subparsers(dest="command", required=True)
    p = sub.add_parser("decompile")
    p.add_argument("input")
    p.add_argument("-o", "--output")
    p.set_defaults(func=cmd_decompile)
    p = sub.add_parser("map")
    p.add_argument("input")
    p.add_argument("-o", "--output")
    p.set_defaults(func=cmd_map)
    p = sub.add_parser("compare")
    p.add_argument("a")
    p.add_argument("b")
    p.set_defaults(func=cmd_compare)
    p = sub.add_parser("check")
    p.add_argument("original")
    p.add_argument("edited")
    p.set_defaults(func=cmd_check)
    p = sub.add_parser("compile")
    p.add_argument("input")
    p.add_argument("output")
    p.set_defaults(func=cmd_compile)
    args = parser.parse_args()
    sys.exit(args.func(args) or 0)


if __name__ == "__main__":
    main()
