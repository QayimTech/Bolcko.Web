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
    # Render at a solid 512x512 size to get maximum geometric precision
    size = 512
    img = Image.new("RGBA", (size, size), (255, 255, 255, 0))
    draw = ImageDraw.Draw(img)
    
    # Yellow Rounded Square
    margin = 51.2   # 10%
    box_size = 409.6 # 80%
    r = 102.4       # Corner radius (20%)
    
    draw.rounded_rectangle(
        [margin, margin, margin + box_size, margin + box_size],
        radius=r,
        fill="#E8A020"
    )
    
    # White left vertical block
    # x=28%, y=26%, w=12%, h=48%
    lx1 = size * 0.28
    ly1 = size * 0.26
    lx2 = size * (0.28 + 0.12)
    ly2 = size * (0.26 + 0.48)
    lr = size * 0.06
    draw.rounded_rectangle([lx1, ly1, lx2, ly2], radius=lr, fill="#FFFFFF")
    
    # White right top block (hollow)
    # x=52%, y=30%, w=16%, h=12%, stroke=6%
    tx1 = size * 0.52
    ty1 = size * 0.30
    tx2 = size * (0.52 + 0.16)
    ty2 = size * (0.30 + 0.12)
    tr = size * 0.04
    sw = size * 0.06 # Stroke width (approx 30.7px)
    
    draw.rounded_rectangle([tx1, ty1, tx2, ty2], radius=tr, fill="#FFFFFF")
    draw.rounded_rectangle([tx1 + sw, ty1 + sw, tx2 - sw, ty2 - sw], radius=max(1, tr - sw/2), fill="#E8A020")
    
    # White right bottom block (hollow)
    # x=52%, y=58%, w=16%, h=12%, stroke=6%
    bx1 = size * 0.52
    by1 = size * 0.58
    bx2 = size * (0.52 + 0.16)
    by2 = size * (0.58 + 0.12)
    br = size * 0.04
    
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
