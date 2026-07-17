import os
import subprocess
import sys

# Ensure pillow is installed
try:
    from PIL import Image, ImageDraw
except ImportError:
    print("Installing Pillow...")
    subprocess.check_call([sys.executable, "-m", "pip", "install", "pillow"])
    from PIL import Image, ImageDraw

def draw_blocko_icon_high_res():
    # Render at a solid 512x512 size to get maximum geometric precision with 0% margin
    size = 512
    img = Image.new("RGBA", (size, size), (255, 255, 255, 0))
    draw = ImageDraw.Draw(img)
    
    # Yellow Rounded Square - Fills the entire canvas (0 margin)
    draw.rounded_rectangle(
        [0, 0, size, size],
        radius=115, # Proportional corner radius
        fill="#E8A020"
    )
    
    # White left vertical block
    # lx1 = 18/80 = 22.5%, ly1 = 16/80 = 20%, w = 12/80 = 15%, h = 48/80 = 60%
    lx1 = size * 0.225
    ly1 = size * 0.20
    lx2 = size * (0.225 + 0.15)
    ly2 = size * (0.20 + 0.60)
    lr = size * 0.075
    draw.rounded_rectangle([lx1, ly1, lx2, ly2], radius=lr, fill="#FFFFFF")
    
    # White right top block (hollow)
    # tx1 = 42/80 = 52.5%, ty1 = 20/80 = 25%, w = 18/80 = 22.5%, h = 14/80 = 17.5%, stroke = 6.5/80 = 8.125%
    tx1 = size * 0.525
    ty1 = size * 0.25
    tx2 = size * (0.525 + 0.225)
    ty2 = size * (0.25 + 0.175)
    tr = size * 0.05
    sw = size * 0.08125 # Stroke width
    
    draw.rounded_rectangle([tx1, ty1, tx2, ty2], radius=tr, fill="#FFFFFF")
    draw.rounded_rectangle([tx1 + sw, ty1 + sw, tx2 - sw, ty2 - sw], radius=max(1, tr - sw/2), fill="#E8A020")
    
    # White right bottom block (hollow)
    # bx1 = 42/80 = 52.5%, by1 = 46/80 = 57.5%, w = 18/80 = 22.5%, h = 14/80 = 17.5%
    bx1 = size * 0.525
    by1 = size * 0.575
    bx2 = size * (0.525 + 0.225)
    by2 = size * (0.575 + 0.175)
    br = size * 0.05
    
    draw.rounded_rectangle([bx1, by1, bx2, by2], radius=br, fill="#FFFFFF")
    draw.rounded_rectangle([bx1 + sw, by1 + sw, bx2 - sw, by2 - sw], radius=max(1, br - sw/2), fill="#E8A020")
    
    return img

# Output directory
out_dir = r"c:\Users\hamza\source\repos\Bolcko.Web\Bolcko.Web.App\wwwroot\favicons"
os.makedirs(out_dir, exist_ok=True)

# Generate master high-res image
master_img = draw_blocko_icon_high_res()

# Generate and resize sizes using LANCZOS filter for smooth downscaling
sizes = {
    "favicon-16x16.png": 16,
    "favicon-32x32.png": 32,
    "favicon-48x48.png": 48,
    "apple-touch-icon.png": 180,
    "android-chrome-192x192.png": 192,
    "android-chrome-512x512.png": 512,
}

for name, size in sizes.items():
    resized = master_img.resize((size, size), Image.Resampling.LANCZOS)
    resized.save(os.path.join(out_dir, name), "PNG")
    print(f"Generated {name} ({size}x{size})")

# Generate favicon.ico containing multiple resolutions
ico_path = os.path.join(out_dir, "favicon.ico")
master_img.save(ico_path, format="ICO", sizes=[(16, 16), (32, 32), (48, 48)])
print("Generated favicon.ico (multi-resolution)")
