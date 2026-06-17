from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

OUT = Path(__file__).parent
W, H = 1440, 980

BG = "#17191d"
TOP = "#15171b"
NAV = "#1b1e23"
PANEL = "#202329"
PANEL2 = "#252a31"
PANEL3 = "#2b3038"
LINE = "#3a414b"
SOFT = "#303740"
TEXT = "#e9edf2"
MUTED = "#aab2bd"
QUIET = "#7d8793"
ACCENT = "#d8a35f"
ACCENT2 = "#b86b3d"
GREEN = "#78b678"
BLUE = "#6fa8dc"
RED = "#d06b6b"


def font(size, bold=False):
    names = ["segoeuib.ttf" if bold else "segoeui.ttf", "arialbd.ttf" if bold else "arial.ttf"]
    for name in names:
        try:
            return ImageFont.truetype(name, size)
        except OSError:
            pass
    return ImageFont.load_default()


F10 = font(10)
F11 = font(11)
F12 = font(12)
F13 = font(13)
F14 = font(14)
F16 = font(16, True)
F18 = font(18, True)
F22 = font(22, True)


def rounded(d, box, fill, outline=LINE, radius=7, width=1):
    d.rounded_rectangle(box, radius=radius, fill=fill, outline=outline, width=width)


def text(d, xy, s, fill=TEXT, f=F13):
    d.text(xy, s, fill=fill, font=f)


def pill(d, x, y, s, fill="#171a1f", outline=LINE):
    w = d.textlength(s, font=F11) + 18
    rounded(d, (x, y, x + w, y + 22), fill, outline, 11)
    text(d, (x + 9, y + 5), s, MUTED, F11)
    return x + w + 7


def button(d, box, s, primary=False):
    fill = ACCENT if primary else PANEL3
    outline = ACCENT if primary else LINE
    fg = "#15100c" if primary else TEXT
    rounded(d, box, fill, outline, 6)
    tw = d.textlength(s, font=F12)
    text(d, ((box[0] + box[2] - tw) / 2, box[1] + 9), s, fg, F12)


def input_box(d, box, label, value="", multiline=False):
    text(d, (box[0], box[1]), label.upper(), MUTED, F10)
    ib = (box[0], box[1] + 18, box[2], box[3])
    rounded(d, ib, TOP, LINE, 6)
    if multiline:
        lines = []
        for raw in value.splitlines():
            lines.extend(wrap(d, raw, ib[2] - ib[0] - 20, F13))
    else:
        lines = [value]
    for i, line in enumerate(lines[:5]):
        text(d, (ib[0] + 10, ib[1] + 9 + i * 18), line, TEXT if i == 0 else MUTED, F13)


def card(d, box, title, subtitle=None):
    rounded(d, box, "#1d2127", SOFT, 8)
    d.line((box[0], box[1] + 38, box[2], box[1] + 38), fill=SOFT)
    text(d, (box[0] + 12, box[1] + 11), title, TEXT, F13)
    if subtitle:
        pill(d, box[2] - d.textlength(subtitle, font=F11) - 35, box[1] + 8, subtitle)
    return (box[0] + 12, box[1] + 50, box[2] - 12, box[3] - 12)


def shell(title, subtitle, stage="Concept"):
    img = Image.new("RGB", (W, H), BG)
    d = ImageDraw.Draw(img)
    d.rectangle((0, 0, W, 42), fill=TOP)
    d.line((0, 42, W, 42), fill=LINE)
    d.polygon([(24, 21), (36, 9), (48, 21), (36, 33)], outline=ACCENT, fill=None)
    text(d, (60, 13), "FusionCanvas", TEXT, F16)
    rounded(d, (260, 7, 780, 35), "#1d2025", LINE, 6)
    text(d, (274, 15), "Search, open command, or create from current context", MUTED, F12)
    text(d, (1250, 15), "Plugins   Settings", MUTED, F12)

    d.rectangle((0, 42, 280, 952), fill=NAV)
    d.line((280, 42, 280, 952), fill=LINE)
    rounded(d, (12, 54, 268, 86), PANEL, LINE, 6)
    text(d, (24, 63), "Tomes of Virtue", TEXT, F13)
    rounded(d, (12, 98, 200, 128), TOP, LINE, 6)
    text(d, (24, 106), "Search tree", QUIET, F12)
    button(d, (208, 98, 238, 128), "+")
    button(d, (244, 98, 268, 128), "...")
    rows = [
        (0, "v", "Tabletop RPG"),
        (1, "v", "Dice and damage"),
        (2, "#", "Does a 17 hit?"),
        (2, "#", "Roll for initiative"),
        (1, "v", "Tavern humor"),
        (2, "#", "Ask your barkeep"),
        (0, "v", "Dark fantasy"),
        (1, ">", "Quest failures"),
        (0, "v", "Store setup"),
        (1, "#", "Mockup products"),
    ]
    y = 148
    for depth, icon, name in rows:
        if name == "Does a 17 hit?":
            rounded(d, (8, y - 2, 272, y + 26), "#34313a", "#654f34", 5)
            col = TEXT
        else:
            col = MUTED
        text(d, (18 + depth * 20, y + 4), icon, QUIET, F12)
        text(d, (40 + depth * 20, y + 3), name, col, F13)
        y += 30
    button(d, (12, 910, 132, 940), "New topic")
    button(d, (148, 910, 268, 940), "New item")

    d.rectangle((280, 42, W, 80), fill=TOP)
    for i, tab in enumerate(["Does a 17 hit?", "Tavern humor", "Mockup products"]):
        x = 290 + i * 172
        rounded(d, (x, 49, x + 166, 80), PANEL if i == 0 else "#1c2026", LINE if i == 0 else SOFT, 6)
        text(d, (x + 12, 59), tab, TEXT if i == 0 else MUTED, F12)
        text(d, (x + 145, 59), "x", QUIET, F12)

    d.rectangle((280, 80, W, 196), fill=PANEL)
    d.line((280, 196, W, 196), fill=LINE)
    text(d, (298, 96), "Tomes of Virtue / Tabletop RPG / Dice and damage / Does a 17 hit?", QUIET, F12)
    stages = ["Idea", "Concept", "Design", "Listing", "Archive"]
    metas = ["Captured", "Ready for refinement", "Draft variants", "Needs mockups", "Retain learning"]
    sx = 298
    for s, m in zip(stages, metas):
        sw = 150 if s != "Archive" else 105
        fill = "#302b27" if s == stage else PANEL2
        outline = ACCENT if s == stage else GREEN if s == "Idea" else LINE
        rounded(d, (sx, 118, sx + sw, 176), fill, outline, 7)
        text(d, (sx + 12, 130), s, TEXT, F13)
        text(d, (sx + 12, 153), m, QUIET, F11)
        sx += sw + 10
    rounded(d, (1218, 104, 1420, 182), "#1c2025", SOFT, 7)
    text(d, (1230, 116), "Active context follows", MUTED, F12)
    text(d, (1230, 136), "the selected tab.", MUTED, F12)
    text(d, (1230, 156), "Tool host sits below.", MUTED, F12)

    d.rectangle((280, 196, W, 952), fill=PANEL)
    text(d, (298, 214), title, TEXT, F22)
    text(d, (298, 244), subtitle, MUTED, F12)
    rounded(d, (1270, 211, 1420, 243), PANEL2, LINE, 6)
    text(d, (1282, 220), "Built-in tool", TEXT, F12)
    d.rectangle((0, 952, W, H), fill="#121418")
    text(d, (12, 960), "Local workspace: Tomes of Virtue", QUIET, F11)
    text(d, (1148, 960), "SQLite local-first model | plugins later", QUIET, F11)
    return img, d


def save(img, name):
    img.save(OUT / name)


def core():
    img, d = shell("FusionCanvas Workspace", "Core shell with navigation, tabs, horizontal workflow split, and stage tool host.", "Concept")
    b = card(d, (298, 270, 972, 910), "Horizontal Document Split", "Workflow above, tool below")
    x = b[0]
    for p in ["Navigation pane", "Tabbed view", "Stage boxes", "Tool selector"]:
        x = pill(d, x, b[1], p)
    rounded(d, (b[0], b[1] + 42, b[2], b[3]), TOP, LINE, 6)
    copy = [
        "The document area is intentionally split horizontally.",
        "",
        "Top: workflow stage navigator with Idea, Concept, Design, Listing, and optional Archive.",
        "",
        "Bottom: stage tool host. The active tab and selected stage determine which tool appears here.",
        "",
        "This keeps workflow orientation visible while preserving most of the screen for actual work.",
    ]
    for i, line in enumerate(copy):
        text(d, (b[0] + 14, b[1] + 60 + i * 24), line, TEXT if line else MUTED, F14)
    b = card(d, (988, 270, 1420, 910), "Current Item Snapshot")
    input_box(d, (b[0], b[1], b[2], b[1] + 62), "Current stage", "Concept")
    input_box(d, (b[0], b[1] + 78, b[2], b[1] + 140), "Concept readiness", "Strong phrase, graphic needs specificity")
    input_box(d, (b[0], b[1] + 156, b[2], b[1] + 218), "Next useful action", "Refine graphic direction before design")
    for i, (a, s) in enumerate([("Idea", "Done"), ("Concept", "Active"), ("Design", "Available"), ("Listing", "Pending")]):
        y = b[1] + 260 + i * 42
        d.line((b[0], y - 10, b[2], y - 10), fill=SOFT)
        text(d, (b[0], y), a, TEXT, F13)
        text(d, (b[2] - 120, y), s, GREEN if s == "Done" else MUTED, F13)
    save(img, "fusioncanvas-core-workspace.png")


def ideation():
    img, d = shell("Ideation Tool", "Capture ideas manually or generate candidates from the selected topic context.", "Idea")
    b = card(d, (298, 270, 822, 910), "Idea Capture", "Context-aware")
    input_box(d, (b[0], b[1], b[2], b[1] + 62), "Current scope", "Tomes of Virtue / Tabletop RPG / Dice and damage")
    input_box(d, (b[0], b[1] + 78, b[2], b[1] + 140), "Quick idea", "Tiny adventurer staring up at an impossible monster foot")
    input_box(d, (b[0], b[1] + 156, b[2], b[1] + 270), "Optional notes", "Readable thumbnail.\nDark fantasy joke about barely missing or surviving a roll.", True)
    button(d, (b[0], b[1] + 296, b[0] + 120, b[1] + 330), "Create idea", True)
    button(d, (b[0] + 132, b[1] + 296, b[0] + 270, b[1] + 330), "Attach reference")
    button(d, (b[0] + 282, b[1] + 296, b[0] + 432, b[1] + 330), "Generate 12 ideas")
    b = card(d, (838, 270, 1420, 910), "Generated Candidates")
    items = [
        ("Does a 17 hit?", "Tabletop anxiety, monster scale, simple phrase."),
        ("I have darkvision and regrets", "Works for dungeon delvers; maybe phrase-led."),
        ("Loot first, ethics later", "Tavern/shop crossover candidate."),
        ("Failed my stealth check", "Good visual gag, avoid duplicates."),
    ]
    for i, (a, s) in enumerate(items):
        y = b[1] + i * 78
        d.line((b[0], y + 68, b[2], y + 68), fill=SOFT)
        text(d, (b[0], y), a, TEXT, F14)
        text(d, (b[0], y + 24), s, QUIET, F12)
        button(d, (b[2] - 170, y + 10, b[2] - 92, y + 44), "Create")
        button(d, (b[2] - 82, y + 10, b[2] - 8, y + 44), "Reject")
    save(img, "fusioncanvas-ideation-tool.png")


def concept():
    img, d = shell("Concept Tool", "Refine the design triangle for a single existing item.", "Concept")
    b = card(d, (298, 270, 972, 910), "Design Triangle", "Click a node to improve")
    d.line((b[0] + 335, b[1] + 80, b[0] + 90, b[3] - 80), fill="#6b5436", width=2)
    d.line((b[0] + 335, b[1] + 80, b[2] - 90, b[3] - 80), fill="#6b5436", width=2)
    d.line((b[0] + 100, b[3] - 80, b[2] - 100, b[3] - 80), fill="#6b5436", width=2)
    nodes = [
        ((b[0] + 205, b[1] + 25, b[0] + 465, b[1] + 130), "Idea", "Comic tension of a player asking whether a mediocre roll survives a massive fantasy threat.", BLUE),
        ((b[0] + 40, b[3] - 160, b[0] + 300, b[3] - 45), "Phrase", "Does a 17 hit?", ACCENT),
        ((b[2] - 300, b[3] - 160, b[2] - 40, b[3] - 45), "Graphic", "Tiny adventurer facing only the huge clawed foot of a monster.", GREEN),
    ]
    for box, title, body, color in nodes:
        rounded(d, box, PANEL2, color, 8)
        text(d, (box[0] + 12, box[1] + 10), title.upper(), MUTED, F10)
        for j, line in enumerate(wrap(d, body, box[2] - box[0] - 24, F13)):
            text(d, (box[0] + 12, box[1] + 32 + j * 18), line, TEXT, F13)
    d.ellipse((b[0] + 274, b[1] + 250, b[0] + 400, b[1] + 376), fill=TOP, outline=ACCENT, width=2)
    text(d, (b[0] + 317, b[1] + 286), "82", ACCENT, F22)
    text(d, (b[0] + 294, b[1] + 318), "Graphic can sharpen", MUTED, F11)
    b = card(d, (988, 270, 1420, 910), "Concept History")
    input_box(d, (b[0], b[1], b[2], b[1] + 62), "Selected node", "Graphic")
    input_box(d, (b[0], b[1] + 78, b[2], b[1] + 220), "AI suggestion", "Use a low-angle silhouette: a tiny armored figure holding a bent sword, framed by the shadow of a clawed monster foot.", True)
    button(d, (b[0], b[1] + 246, b[0] + 86, b[1] + 280), "Accept", True)
    button(d, (b[0] + 98, b[1] + 246, b[0] + 200, b[1] + 280), "Regenerate")
    button(d, (b[0] + 212, b[1] + 246, b[0] + 330, b[1] + 280), "Save alternate")
    for i, row in enumerate(["Phrase accepted", "Graphic improved", "Score updated"]):
        y = b[1] + 330 + i * 42
        d.line((b[0], y - 10, b[2], y - 10), fill=SOFT)
        text(d, (b[0], y), f"14:0{2 + i * 3}", MUTED, F12)
        text(d, (b[0] + 70, y), row, TEXT, F13)
    save(img, "fusioncanvas-concept-tool.png")


def design():
    img, d = shell("Design Tool", "Import, generate, compare, and promote final design variants.", "Design")
    b1 = card(d, (298, 270, 610, 910), "Design Brief")
    input_box(d, (b1[0], b1[1], b1[2], b1[1] + 62), "Phrase", "Does a 17 hit?")
    input_box(d, (b1[0], b1[1] + 78, b1[2], b1[1] + 218), "Style", "Gritty dark fantasy, distressed shirt graphic, high contrast, readable at thumbnail size.", True)
    button(d, (b1[0], b1[1] + 250, b1[0] + 110, b1[1] + 284), "Import files")
    button(d, (b1[0] + 122, b1[1] + 250, b1[0] + 260, b1[1] + 284), "Generate variants")
    b2 = card(d, (626, 270, 1106, 910), "Variant Workspace", "Final selection explicit")
    for i, label in enumerate(["Variant A", "Variant B", "Dark shirt export"]):
        x = b2[0] + i * 150
        rounded(d, (x, b2[1], x + 138, b2[1] + 220), TOP, SOFT, 7)
        draw_art(d, (x + 10, b2[1] + 10, x + 128, b2[1] + 150), i)
        text(d, (x + 10, b2[1] + 164), label, TEXT, F13)
        pill(d, x + 10, b2[1] + 188, "final" if i == 2 else "draft")
    button(d, (b2[0], b2[1] + 250, b2[0] + 116, b2[1] + 284), "Promote final", True)
    button(d, (b2[0] + 128, b2[1] + 250, b2[0] + 216, b2[1] + 284), "Compare")
    b3 = card(d, (1122, 270, 1420, 910), "Variant Details")
    input_box(d, (b3[0], b3[1], b3[2], b3[1] + 62), "Status", "Selected final")
    text(d, (b3[0], b3[1] + 82), "TAGS", MUTED, F10)
    x = b3[0]
    for p in ["dark-shirts", "distressed", "final"]:
        x = pill(d, x, b3[1] + 104, p)
    input_box(d, (b3[0], b3[1] + 150, b3[2], b3[1] + 285), "Assets", "does-a-17-hit.afdesign\ndoes-a-17-hit-dark.png\ndoes-a-17-hit-light.png", True)
    save(img, "fusioncanvas-design-tool.png")


def listing():
    img, d = shell("Listing Tool", "Prepare metadata, mockups, price, and local marketplace-ready assets.", "Listing")
    b = card(d, (298, 270, 822, 910), "Listing Metadata")
    input_box(d, (b[0], b[1], b[2], b[1] + 62), "Title", "Does a 17 Hit? DnD Shirt")
    input_box(d, (b[0], b[1] + 78, b[2], b[1] + 210), "Description", "A dark fantasy tabletop RPG shirt for players who know the dread of asking the DM one small question.", True)
    input_box(d, (b[0], b[1] + 226, b[0] + 150, b[1] + 288), "Price", "24.99")
    input_box(d, (b[0] + 165, b[1] + 226, b[2], b[1] + 288), "Status", "Ready for manual Printify setup")
    button(d, (b[0], b[1] + 320, b[0] + 100, b[1] + 354), "Save listing", True)
    button(d, (b[0] + 112, b[1] + 320, b[0] + 226, b[1] + 354), "Generate text")
    b = card(d, (838, 270, 1420, 910), "Mockups and Readiness")
    for i, color in enumerate(["#222831", "#26344c", "#46484c"]):
        x = b[0] + i * 180
        rounded(d, (x, b[1], x + 160, b[1] + 220), TOP, SOFT, 7)
        draw_shirt(d, (x + 18, b[1] + 18, x + 142, b[1] + 155), color)
        text(d, (x + 18, b[1] + 172), ["Black front", "Navy front", "Dark Heather"][i], TEXT, F12)
    checks = [("Final design selected", "Dark shirt export"), ("Mockups generated", "3 files"), ("Price captured", "$24.99"), ("Provider setup", "Manual task remains")]
    for i, (a, s) in enumerate(checks):
        y = b[1] + 260 + i * 42
        d.line((b[0], y - 10, b[2], y - 10), fill=SOFT)
        text(d, (b[0], y), ("OK  " if i < 3 else "--  ") + a, GREEN if i < 3 else MUTED, F13)
        text(d, (b[2] - 170, y), s, MUTED, F13)
    save(img, "fusioncanvas-listing-tool.png")


def mockup():
    img, d = shell("Mockup Setup", "Store-level product, template, color, and placement configuration.", "Concept")
    b = card(d, (298, 270, 875, 910), "Mockup Product Settings")
    d.rectangle((b[0], b[1], b[0] + 235, b[3]), fill="#1a1e24")
    for i, row in enumerate(["SwiftPOD / Gildan 64000", "SwiftPOD / Bella Canvas 3001", "Printify / Mug 11oz"]):
        y = b[1] + 10 + i * 40
        if i == 0:
            rounded(d, (b[0] + 8, y - 4, b[0] + 225, y + 28), "#302b27", "#654f34", 6)
            col = TEXT
        else:
            col = MUTED
        text(d, (b[0] + 18, y + 4), row, col, F13)
    x0 = b[0] + 255
    input_box(d, (x0, b[1] + 5, b[2], b[1] + 67), "Vendor", "SwiftPOD")
    input_box(d, (x0, b[1] + 83, b[2], b[1] + 145), "Product", "Gildan 64000 T-Shirt")
    input_box(d, (x0, b[1] + 161, x0 + 140, b[1] + 223), "Design width", "3692")
    input_box(d, (x0 + 155, b[1] + 161, x0 + 295, b[1] + 223), "Design height", "4800")
    for i, row in enumerate(["Flat front shirt mockup   Front   Black, Navy, Dark Heather", "Lifestyle front mockup     Front   Black, Navy"]):
        y = b[1] + 270 + i * 44
        d.line((x0, y - 10, b[2], y - 10), fill=SOFT)
        text(d, (x0, y), row, TEXT, F12)
    b = card(d, (891, 270, 1420, 910), "Template Mapping")
    draw_shirt(d, (b[0] + 120, b[1] + 20, b[2] - 120, b[1] + 390), "#222831")
    input_box(d, (b[0], b[1] + 420, b[0] + 110, b[1] + 482), "X", "536")
    input_box(d, (b[0] + 125, b[1] + 420, b[0] + 235, b[1] + 482), "Y", "365")
    input_box(d, (b[0] + 250, b[1] + 420, b[0] + 360, b[1] + 482), "Width", "428")
    input_box(d, (b[0] + 375, b[1] + 420, b[0] + 485, b[1] + 482), "Rotation", "0")
    save(img, "fusioncanvas-mockup-setup.png")


def store():
    img, d = shell("Store and Niche Setup", "Maintain brand context, niche guidance, groups, and active work scope.", "Concept")
    b = card(d, (298, 270, 822, 910), "Store Setup")
    input_box(d, (b[0], b[1], b[2], b[1] + 62), "Store name", "Tomes of Virtue")
    input_box(d, (b[0], b[1] + 78, b[2], b[1] + 210), "Brand direction", "Dark fantasy merchandise for tabletop RPG players. Clever, gritty, readable, and not childish.", True)
    x = b[0]
    for p in ["Shopify later", "Printify manual setup", "AI context enabled"]:
        x = pill(d, x, b[1] + 232, p)
    button(d, (b[0], b[1] + 280, b[0] + 100, b[1] + 314), "Save store", True)
    button(d, (b[0] + 112, b[1] + 280, b[0] + 226, b[1] + 314), "Archive store")
    b = card(d, (838, 270, 1420, 910), "Niche Setup")
    input_box(d, (b[0], b[1], b[2], b[1] + 62), "Niche", "Tabletop RPG")
    input_box(d, (b[0], b[1] + 78, b[2], b[1] + 190), "Audience and humor", "Players and DMs who recognize table jokes, dice anxiety, tactical chaos, tavern problems, and campaign failures.", True)
    input_box(d, (b[0], b[1] + 206, b[2], b[1] + 318), "Style guidance", "Distressed badges, woodcut-inspired monsters, readable phrases, high contrast, minimal tiny details.", True)
    rows = [("Dice and damage", "12", "Core phrase-driven niche"), ("Tavern humor", "8", "Good for collections"), ("Quest failures", "5", "Needs more visual hooks")]
    for i, row in enumerate(rows):
        y = b[1] + 360 + i * 42
        d.line((b[0], y - 10, b[2], y - 10), fill=SOFT)
        text(d, (b[0], y), row[0], TEXT, F13)
        text(d, (b[0] + 210, y), row[1], MUTED, F13)
        text(d, (b[0] + 280, y), row[2], MUTED, F13)
    save(img, "fusioncanvas-store-niche-setup.png")


def draw_art(d, box, variant=0):
    rounded(d, box, "#15171b", LINE, 6)
    cx = (box[0] + box[2]) // 2
    cy = (box[1] + box[3]) // 2
    colors = [ACCENT, ACCENT2, "#d2c08f"]
    d.ellipse((cx - 22, cy - 38, cx + 22, cy + 6), fill=colors[variant], outline=None)
    d.polygon([(cx - 52, cy + 46), (cx, cy - 4), (cx + 52, cy + 46)], fill="#342a25", outline=ACCENT)
    text(d, (box[0] + 20, box[1] + 18), "17?", ACCENT, F18)


def draw_shirt(d, box, color):
    rounded(d, box, color, LINE, 7)
    d.rectangle((box[0] + 18, box[1], box[2] - 18, box[1] + 22), fill="#101216")
    d.ellipse((box[0] + 48, box[1] + 35, box[2] - 48, box[1] + 95), fill=ACCENT2)
    text(d, ((box[0] + box[2]) / 2 - 18, box[1] + 60), "17?", "#111111", F16)


def wrap(d, s, max_width, f):
    words = s.split()
    lines, line = [], ""
    for word in words:
        test = (line + " " + word).strip()
        if d.textlength(test, font=f) <= max_width:
            line = test
        else:
            if line:
                lines.append(line)
            line = word
    if line:
        lines.append(line)
    return lines


if __name__ == "__main__":
    core()
    ideation()
    concept()
    design()
    listing()
    mockup()
    store()
