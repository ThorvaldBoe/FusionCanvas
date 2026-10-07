from pathlib import Path
from PIL import Image, ImageDraw, ImageFont


OUT = Path(__file__).parent
W, H = 1600, 1000

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
GREEN = "#78b678"
BLUE = "#6fa8dc"
RED = "#d06b6b"
AMBER = "#d7a55a"


def font(size, bold=False):
    names = ["segoeuib.ttf" if bold else "segoeui.ttf", "arialbd.ttf" if bold else "arial.ttf"]
    for name in names:
        try:
            return ImageFont.truetype(name, size)
        except OSError:
            pass
    return ImageFont.load_default()


F10, F11, F12, F13 = font(10), font(11), font(12), font(13)
F15, F18, F22 = font(15, True), font(18, True), font(22, True)
F28 = font(28, True)


def rounded(draw, box, fill, outline=LINE, radius=8, width=1):
    draw.rounded_rectangle(box, radius=radius, fill=fill, outline=outline, width=width)


def text(draw, xy, value, fill=TEXT, face=F13):
    draw.text(xy, value, fill=fill, font=face)


def pill(draw, x, y, label, color=MUTED, outline=LINE, fill="#171a1f"):
    width = draw.textlength(label, font=F11) + 22
    rounded(draw, (x, y, x + width, y + 24), fill, outline, 12)
    text(draw, (x + 11, y + 6), label, color, F11)
    return x + width + 8


def button(draw, box, label, primary=False, disabled=False, danger=False):
    if disabled:
        fill, outline, fg = "#20242a", SOFT, QUIET
    elif danger:
        fill, outline, fg = "#352326", "#774349", RED
    elif primary:
        fill, outline, fg = ACCENT, ACCENT, "#15100c"
    else:
        fill, outline, fg = PANEL3, LINE, TEXT
    rounded(draw, box, fill, outline, 6)
    width = draw.textlength(label, font=F12)
    text(draw, ((box[0] + box[2] - width) / 2, box[1] + 10), label, fg, F12)


def shell(title, subtitle, state_label, state_color):
    image = Image.new("RGB", (W, H), BG)
    draw = ImageDraw.Draw(image)
    draw.rectangle((0, 0, W, 46), fill=TOP)
    draw.line((0, 46, W, 46), fill=LINE)
    draw.polygon([(24, 23), (38, 9), (52, 23), (38, 37)], outline=ACCENT)
    text(draw, (66, 14), "FusionCanvas", TEXT, F18)
    rounded(draw, (300, 8, 860, 38), "#1d2025", LINE, 6)
    text(draw, (316, 16), "Search, open command, or create from current context", MUTED, F12)
    text(draw, (1390, 17), "Settings", MUTED, F12)

    draw.rectangle((0, 46, 292, 972), fill=NAV)
    draw.line((292, 46, 292, 972), fill=LINE)
    rounded(draw, (14, 60, 278, 94), PANEL, LINE, 6)
    text(draw, (28, 70), "The Groan Zone", TEXT, F13)
    rounded(draw, (14, 108, 212, 138), TOP, LINE, 6)
    text(draw, (26, 116), "Search tree", QUIET, F12)
    button(draw, (220, 108, 248, 138), "+")
    button(draw, (254, 108, 278, 138), "...")
    rows = [
        (0, "v", "Dad jokes"),
        (1, "v", "Pun shirts"),
        (2, "#", "Dad Joke Loading..."),
        (2, "#", "World's Okayest Dad"),
        (1, "v", "One-liners"),
        (2, "#", "Classic dad jokes"),
        (0, "v", "Store setup"),
    ]
    y = 160
    for depth, icon, label in rows:
        if label == "Dad Joke Loading...":
            rounded(draw, (9, y - 4, 283, y + 27), "#34313a", "#654f34", 5)
            color = TEXT
        else:
            color = MUTED
        text(draw, (22 + depth * 20, y + 4), icon, QUIET, F12)
        text(draw, (44 + depth * 20, y + 3), label, color, F13)
        y += 34
    button(draw, (14, 918, 138, 948), "New topic")
    button(draw, (150, 918, 278, 948), "New item")

    draw.rectangle((292, 46, W, 86), fill=TOP)
    for i, label in enumerate(["Dad Joke Loading...", "World's Okayest Dad", "Store setup"]):
        x = 304 + i * 190
        rounded(draw, (x, 53, x + 182, 86), PANEL if i == 0 else "#1c2026", LINE if i == 0 else SOFT, 6)
        text(draw, (x + 12, 64), label, TEXT if i == 0 else MUTED, F12)
        text(draw, (x + 162, 64), "x", QUIET, F12)

    draw.rectangle((292, 86, W, 204), fill=PANEL)
    draw.line((292, 204, W, 204), fill=LINE)
    text(draw, (312, 103), "The Groan Zone / Dad jokes / Pun shirts / Dad Joke Loading...", QUIET, F12)
    stages = [("Idea", "Captured"), ("Concept", "Refined"), ("Design", "Final selected"), ("Listing", "Printify")]
    x = 312
    for label, meta in stages:
        active = label == "Listing"
        rounded(draw, (x, 128, x + 170, 184), "#302b27" if active else PANEL2, ACCENT if active else GREEN, 7)
        text(draw, (x + 12, 141), label, TEXT, F13)
        text(draw, (x + 12, 163), meta, QUIET, F11)
        x += 180
    rounded(draw, (1090, 118, 1565, 190), "#1c2025", SOFT, 7)
    text(draw, (1106, 131), "Listing / Printify", TEXT, F12)
    text(draw, (1106, 154), "Design remains the authority for colors and artwork.", MUTED, F11)

    draw.rectangle((292, 204, W, 972), fill=PANEL)
    text(draw, (312, 222), title, TEXT, F28)
    text(draw, (312, 260), subtitle, MUTED, F13)
    state_fill = "#1d2822" if state_color == GREEN else "#2d2520" if state_color == RED else "#202a33" if state_color == BLUE else "#2a271f"
    pill(draw, 1290, 220, state_label, state_color, state_color, state_fill)
    return image, draw


FORM_LEFT = 312
FORM_RIGHT = 1565
CONTENT_LEFT = 338
CONTROL_LEFT = 585
CONTROL_RIGHT = 1320


def form_surface(draw, top=300, bottom=934):
    rounded(draw, (FORM_LEFT, top, FORM_RIGHT, bottom), "#1d2127", SOFT, 7)
    draw.line((FORM_LEFT, top + 48, FORM_RIGHT, top + 48), fill=SOFT)
    text(draw, (CONTENT_LEFT, top + 15), "Printify listing", TEXT, F15)
    text(draw, (FORM_RIGHT - 160, top + 18), "Single-column form", QUIET, F11)


def section(draw, y, label):
    text(draw, (CONTENT_LEFT, y), label.upper(), QUIET, F10)
    return y + 24


def field(draw, y, label, value, editable=False, suffix=None, value_color=TEXT):
    text(draw, (CONTENT_LEFT, y + 10), label, MUTED, F12)
    fill = PANEL3 if editable else "#1a1e23"
    outline = ACCENT if editable else LINE
    rounded(draw, (CONTROL_LEFT, y, CONTROL_RIGHT, y + 38), fill, outline, 5)
    text(draw, (CONTROL_LEFT + 12, y + 10), value, value_color, F12)
    if suffix:
        text(draw, (CONTROL_RIGHT - 28, y + 10), suffix, QUIET, F12)
    return y + 42


def note(draw, y, value, color=MUTED):
    text(draw, (CONTROL_LEFT, y), value, color, F11)
    return y + 25


def action_row(draw, y, buttons):
    x = CONTENT_LEFT
    for label, kind, width in buttons:
        button(draw, (x, y, x + width, y + 38), label, primary=kind == "primary", disabled=kind == "disabled", danger=kind == "danger")
        x += width + 10


def standard_fields(draw, y):
    y = section(draw, y, "Listing content")
    y = field(draw, y, "Title", "Dad Joke Loading... – T-Shirt", editable=True)
    y = field(draw, y, "Description", "Optional dad-joke product description", editable=True)
    y += 8
    y = section(draw, y, "Printify selection")
    y = field(draw, y, "Blueprint", "Gildan 64000 T-Shirt")
    y = field(draw, y, "Provider", "SwiftPOD")
    y = field(draw, y, "Printify shop", "The Groan Zone")
    y = field(draw, y, "Colors", "Black · Navy · Dark Heather", value_color=MUTED)
    y = field(draw, y, "Artwork", "Front · 3692 × 4800 · full-width, top-aligned", value_color=MUTED)
    y += 8
    y = section(draw, y, "Defaults and pricing")
    y = field(draw, y, "Shipping profile", "Standard / General", editable=True, suffix="▾")
    y = field(draw, y, "Out-of-stock behavior", "Hide unavailable variants", editable=True, suffix="▾")
    y = field(draw, y, "Pricing", "Fixed profit amount · $12.00 / item", editable=True, suffix="▾")
    return y


def ready():
    image, draw = shell("Printify Tool", "Create a synchronized product from the current Item and Design state.", "Printify connected", GREEN)
    form_surface(draw)
    y = standard_fields(draw, 350)
    note(draw, y + 6, "Colors, variants, provider, and artwork are read from the Design stage and cannot be edited here.")
    action_row(draw, 884, [("Create in Printify", "primary", 172), ("Refresh connection", "secondary", 158), ("Open Design", "secondary", 128)])
    image.save(OUT / "printify-ready-to-create.png")


def draft():
    image, draw = shell("Printify Tool", "Manage the saved Printify product before publication.", "Draft saved", GREEN)
    form_surface(draw)
    y = standard_fields(draw, 350)
    note(draw, y + 6, "Printify product exists. Publishing is available because the Store uses Shopify + Printify.")
    action_row(draw, 884, [("Update Printify", "primary", 150), ("Publish to Shopify", "primary", 164), ("Archive locally", "secondary", 142), ("Delete draft", "danger", 128)])
    image.save(OUT / "printify-draft.png")


def standalone():
    image, draw = shell("Printify Tool", "Manage the Printify product without Shopify publication.", "Printify only", BLUE)
    form_surface(draw)
    y = standard_fields(draw, 350)
    note(draw, y + 6, "This Store uses standalone Printify. Shopify publication controls are intentionally unavailable.")
    action_row(draw, 884, [("Update Printify", "primary", 150), ("Refresh connection", "secondary", 158), ("Archive locally", "secondary", 142), ("Delete draft", "danger", 128)])
    image.save(OUT / "printify-standalone.png")


def conflict():
    image, draw = shell("Printify Tool", "Resolve remote changes before updating the mapped product.", "Remote changes", AMBER)
    form_surface(draw)
    y = 362
    y = section(draw, y, "Remote change requires a decision")
    rounded(draw, (CONTENT_LEFT, y, CONTROL_RIGHT, y + 56), "#2d271e", "#806335", 6)
    text(draw, (CONTENT_LEFT + 12, y + 11), "Printify changed the price outside Fusion Canvas.", AMBER, F13)
    text(draw, (CONTENT_LEFT + 12, y + 34), "No update will be sent until you choose which value to keep.", MUTED, F11)
    y += 74
    y = field(draw, y, "Title", "Dad Joke Loading... – T-Shirt", value_color=MUTED)
    y = field(draw, y, "Price in Fusion Canvas", "$30.00", value_color=ACCENT)
    y = field(draw, y, "Price in Printify", "$40.00", value_color=AMBER)
    y += 14
    y = section(draw, y, "Resolution")
    note(draw, y, "Accepting the remote value updates local integration data. Keeping the local value updates Printify.")
    action_row(draw, 884, [("Accept Printify $40", "secondary", 174), ("Keep Fusion Canvas $30", "primary", 202), ("Cancel", "secondary", 100)])
    image.save(OUT / "printify-remote-conflict.png")


def published():
    image, draw = shell("Printify Tool", "Manage the synchronized product and its Printify-mediated Shopify publication.", "Published", GREEN)
    form_surface(draw)
    y = 362
    y = section(draw, y, "Listing content")
    y = field(draw, y, "Title", "Dad Joke Loading... – T-Shirt", value_color=MUTED)
    y = field(draw, y, "Description", "Optional dad-joke product description", value_color=MUTED)
    y += 8
    y = section(draw, y, "Publication")
    y = field(draw, y, "Printify product", "5f7c…a91d", value_color=MUTED)
    y = field(draw, y, "Sales-channel product", "shopify-product-10492 · via Printify", value_color=MUTED)
    y = field(draw, y, "Last synchronization", "No drift detected · checked just now", value_color=GREEN)
    note(draw, y + 5, "No direct Shopify controls are shown. Product and publication operations use Printify only.")
    action_row(draw, 884, [("Update Printify", "primary", 150), ("Unpublish", "secondary", 120), ("Refresh", "secondary", 110), ("Archive locally", "secondary", 142), ("Delete remote", "disabled", 130)])
    image.save(OUT / "printify-published.png")


def unpublished():
    image, draw = shell("Printify Tool", "Manage the saved product before publishing it to Shopify.", "Unpublished", AMBER)
    form_surface(draw)
    y = 362
    y = section(draw, y, "Listing content")
    y = field(draw, y, "Title", "Dad Joke Loading... – T-Shirt", value_color=MUTED)
    y = field(draw, y, "Description", "Optional dad-joke product description", value_color=MUTED)
    y += 8
    y = section(draw, y, "Publication")
    y = field(draw, y, "Printify product", "5f7c…a91d", value_color=MUTED)
    y = field(draw, y, "Sales-channel product", "Not published", value_color=AMBER)
    y = field(draw, y, "Remote state", "Unpublished and unlocked", value_color=GREEN)
    note(draw, y + 5, "Remote deletion is available only because the product is verified as unpublished and unlocked.")
    action_row(draw, 884, [("Update Printify", "primary", 150), ("Publish to Shopify", "primary", 164), ("Refresh", "secondary", 110), ("Archive locally", "secondary", 142), ("Delete remote", "danger", 130)])
    image.save(OUT / "printify-unpublished.png")


def unavailable():
    image, draw = shell("Printify Tool", "Reconnect before performing remote work.", "Printify unavailable", RED)
    form_surface(draw)
    y = 362
    y = section(draw, y, "Connection status")
    rounded(draw, (CONTENT_LEFT, y, CONTROL_RIGHT, y + 52), "#2a2020", "#6e4040", 6)
    text(draw, (CONTENT_LEFT + 12, y + 10), "Printify cannot verify the API key or selected shop.", RED, F13)
    text(draw, (CONTENT_LEFT + 12, y + 32), "Remote actions are disabled; local listing data is preserved.", MUTED, F11)
    y += 70
    y = section(draw, y, "Local listing state")
    y = field(draw, y, "Title", "Dad Joke Loading... – T-Shirt", value_color=MUTED)
    y = field(draw, y, "Printify shop", "The Groan Zone · last known", value_color=AMBER)
    y = field(draw, y, "Product mapping", "5f7c…a91d retained locally", value_color=GREEN)
    y = field(draw, y, "Last verified state", "Published, currently unverified", value_color=AMBER)
    note(draw, y + 5, "Review local data or repair the Store connection. No local changes are lost while Printify is unavailable.")
    action_row(draw, 884, [("Verify connection", "primary", 164), ("Open Store settings", "secondary", 170), ("View Design", "secondary", 122)])
    image.save(OUT / "printify-connection-unavailable.png")


if __name__ == "__main__":
    ready()
    draft()
    standalone()
    conflict()
    published()
    unpublished()
    unavailable()
