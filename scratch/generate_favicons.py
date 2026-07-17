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
    # Render at a solid 512x512 size to get maximum geometric precision with 0% margin and sharp square fill
    size = 512
    img = Image.new("RGBA", (size, size), (255, 255, 255, 0))
    draw = ImageDraw.Draw(img)
    
    # Yellow Rounded Square - Minimal corner radius to mimic LinkedIn/SoundCloud style
    draw.rounded_rectangle(
        [0, 0, size, size],
        radius=58, # Reduced corner radius for maximum fill area
        fill="#E8A020"
    )
    
    # White left vertical block (Thicker and wider)
    # lx1 = 14/80 = 17.5%, ly1 = 12/80 = 15%, w = 15/80 = 18.75%, h = 56/80 = 70%
    lx1 = size * 0.175
    ly1 = size * 0.15
    lx2 = size * (0.175 + 0.1875)
    ly2 = size * (0.15 + 0.70)
    lr = size * 0.0625
    draw.rounded_rectangle([lx1, ly1, lx2, ly2], radius=lr, fill="#FFFFFF")
    
    # White right top block (hollow) (Thicker border stroke)
    # tx1 = 38/80 = 47.5%, ty1 = 15/80 = 18.75%, w = 26/80 = 32.5%, h = 20/80 = 25%, stroke = 8.5/80 = 10.625%
    tx1 = size * 0.475
    ty1 = size * 0.1875
    tx2 = size * (0.475 + 0.325)
    ty2 = size * (0.1875 + 0.25)
    tr = size * 0.05
    sw = size * 0.10625 # Stroke width (bold)
    
    draw.rounded_rectangle([tx1, ty1, tx2, ty2], radius=tr, fill="#FFFFFF")
    draw.rounded_rectangle([tx1 + sw, ty1 + sw, tx2 - sw, ty2 - sw], radius=max(1, tr - sw/2), fill="#E8A020")
    
    # White right bottom block (hollow)
    # bx1 = 38/80 = 47.5%, by1 = 45/80 = 56.25%, w = 26/80 = 32.5%, h = 20/80 = 25%
    bx1 = size * 0.475
    by1 = size * 0.5625
    bx2 = size * (0.475 + 0.325)
    by2 = size * (0.5625 + 0.25)
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
