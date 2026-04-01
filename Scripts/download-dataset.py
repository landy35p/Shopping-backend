#!/usr/bin/env python3
"""
Download Electronics product data from HuggingFace Amazon Reviews 2023 dataset.
Falls back to mock data generation if the dataset is not accessible.

Usage:
    python Scripts/download-dataset.py                   # try HuggingFace first
    python Scripts/download-dataset.py --source mock     # force mock data
    python Scripts/download-dataset.py --sample 200      # set sample size

Output: Scripts/products.json
"""

import json
import random
import argparse
from pathlib import Path

SAMPLE_SIZE = 200
OUTPUT_FILE = Path(__file__).parent / "products.json"


def download_from_huggingface(sample_size: int) -> list[dict]:
    """Try to download Amazon Reviews Electronics metadata from HuggingFace."""
    try:
        from datasets import load_dataset
    except ImportError:
        raise RuntimeError("datasets library not installed. Run: pip install datasets")

    print("Loading dataset from HuggingFace (McAuley-Lab/Amazon-Reviews-2023)...")
    ds = load_dataset(
        "McAuley-Lab/Amazon-Reviews-2023",
        "raw_meta_Electronics",
        split="full",
        trust_remote_code=True,
    )

    total = len(ds)
    print(f"Dataset size: {total} records. Sampling {sample_size}...")
    indices = random.sample(range(total), min(sample_size, total))

    products = []
    for i, idx in enumerate(indices):
        row = ds[idx]
        title = (row.get("title") or "").strip()
        if not title:
            continue

        # Price
        price_raw = row.get("price", "") or ""
        try:
            price = float(str(price_raw).replace("$", "").replace(",", ""))
            if price <= 0:
                price = round(random.uniform(10, 500), 2)
        except Exception:
            price = round(random.uniform(10, 500), 2)

        # Rating
        avg_rating = row.get("average_rating", 0) or 0
        try:
            avg_rating = float(avg_rating)
        except Exception:
            avg_rating = 0.0
        if avg_rating <= 0:
            avg_rating = round(random.uniform(3.0, 5.0), 1)

        # Description
        description_list = row.get("description", []) or []
        if isinstance(description_list, list):
            description = " ".join(str(d) for d in description_list if d)
        else:
            description = str(description_list)
        if not description.strip():
            description = title

        # Image
        images = row.get("images", {}) or {}
        image_url = ""
        if isinstance(images, dict):
            large = images.get("large", [])
            if large:
                image_url = large[0]

        products.append({
            "id": row.get("parent_asin") or row.get("asin") or f"ASIN{i:06d}",
            "title": title[:200],
            "description": description[:1000],
            "price": price,
            "rating": round(min(5.0, max(0.0, avg_rating)), 1),
            "category": "Electronics",
            "imageUrl": image_url,
        })

        if (i + 1) % 20 == 0:
            print(f"  Processed {i + 1}/{sample_size} products...")

    return products


def generate_mock_data(sample_size: int) -> list[dict]:
    """Generate realistic mock Electronics products as fallback."""
    print("Generating mock Electronics product data...")

    templates = [
        # title_template, category, base_price, base_rating
        ("Sony WH-1000XM{n} Wireless Noise Cancelling Over-Ear Headphones Bluetooth 5.1", "Headphones", 279.99, 4.7),
        ("Bose QuietComfort {n}5 Wireless Bluetooth Headphones Active Noise Cancelling", "Headphones", 329.00, 4.6),
        ("Apple AirPods Pro (2nd Gen) with MagSafe USB-C Charging Case", "Earbuds", 249.00, 4.8),
        ("Sennheiser HD {n}50BT Wireless Headphones aptX Adaptive 30-Hour Battery", "Headphones", 149.95, 4.5),
        ("JBL Tune {n}30TWS True Wireless Earbuds Hands-free Call 40H Battery", "Earbuds", 49.95, 4.3),
        ("Sony Alpha a{n}100 Full-Frame Mirrorless Camera Body E-Mount", "Cameras", 1099.00, 4.7),
        ("Canon EOS R{n}50 Mirrorless Camera with RF-S 18-45mm f/4.5-6.3 IS STM Lens", "Cameras", 679.00, 4.6),
        ("Fujifilm X-T{n}0 APS-C Mirrorless Camera Black Compact Retro Design", "Cameras", 899.00, 4.5),
        ("DJI Osmo Pocket {n} 3-Axis Gimbal Stabilizer 4K Camera Creator Combo", "Cameras", 349.00, 4.4),
        ("GoPro HERO{n} Black Action Camera 5.3K Ultra HD Waterproof 27MP Photo", "Cameras", 399.00, 4.5),
        ("Apple MacBook Pro 14-inch M3 Pro Chip 18GB RAM 512GB SSD Space Black", "Laptops", 1999.00, 4.9),
        ("Dell XPS 1{n}5 Intel Core i7-13700H 1TB SSD 16GB RAM Windows 11", "Laptops", 1499.00, 4.5),
        ("Lenovo ThinkPad X1 Carbon Gen {n}1 Ultra-Thin Business Laptop 14\"", "Laptops", 1329.00, 4.6),
        ("ASUS ROG Strix G{n}6 Gaming Laptop RTX 4070 16\" 165Hz QHD Display", "Gaming Laptops", 1799.00, 4.4),
        ("Microsoft Surface Pro {n} 13\" Detachable Core i5 8GB 256GB", "Tablets", 999.00, 4.3),
        ("Anker PowerCore {n}0000mAh Portable Charger Slim PD 22.5W Output", "Accessories", 35.99, 4.6),
        ("USB-C to USB-C Cable {n}ft 240W Fast Charging Nylon Braided 2-Pack", "Accessories", 12.99, 4.4),
        ("Spigen Tough Armor MagFit iPhone 1{n} Pro Military-Grade Drop Protection", "Phone Cases", 19.99, 4.5),
        ("Belkin BoostCharge Pro 1{n}W MagSafe Wireless Charger Stand with PSU", "Chargers", 59.99, 4.3),
        ("Amazon Echo Dot (5th Gen) Smart Speaker Alexa Voice Control Charcoal", "Smart Home", 49.99, 4.5),
        ("Google Nest Mini (2nd Gen) Smart Speaker Google Assistant Chalk", "Smart Home", 29.99, 4.4),
        ("Sonos Era {n}00 Premium Wireless Smart Speaker Hi-Res Spatial Audio", "Speakers", 449.00, 4.7),
        ("Roku Streaming Stick {n}K Portable 4K HDR Device Voice Remote", "Streaming", 39.99, 4.4),
        ("Amazon Fire TV Stick 4K Max Wi-Fi 6E Alexa Voice Remote Pro", "Streaming", 59.99, 4.5),
        ("Razer BlackShark V2 Pro Wireless Gaming Headset 50mm Triforce Titanium", "Gaming Peripherals", 179.99, 4.5),
        ("Logitech G5{n}2 Hero Wireless Gaming Mouse 25600 DPI Ultra-Long Battery", "Gaming Peripherals", 139.99, 4.6),
        ("SteelSeries Arctis Nova {n} Wireless Gaming Headset Spatial Audio PC PS5", "Gaming Peripherals", 149.99, 4.4),
        ("ASUS TUF Gaming {n}7\" Monitor 165Hz IPS 1ms FreeSync Premium HDR", "Monitors", 279.99, 4.5),
        ("Apple Magic Keyboard with Touch ID Wireless Rechargeable for iPhone Mac", "Keyboards", 99.00, 4.6),
        ("Logitech MX Master 3S Advanced Wireless Mouse Silent 8K DPI Mac Windows", "Mice", 99.99, 4.7),
        ("Seagate Portable {n}TB External Hard Drive USB 3.0 for PC Mac Slim Black", "Storage", 54.99, 4.4),
        ("WD {n}TB My Passport Portable External Hard Drive USB-C Auto Backup Blue", "Storage", 64.99, 4.5),
        ("LG UltraWide 34\" 21:9 Curved IPS 100Hz FreeSync 1ms{n} HDR10 Monitor", "Monitors", 349.99, 4.4),
        ("Tile Mate (2024) Bluetooth Item Tracker 4-Pack Keys Wallet Luggage", "Accessories", 34.99, 4.3),
        ("Kindle Paperwhite (11th Gen) 6.8\" 300 PPI Waterproof 16GB Ads-Free", "E-Readers", 139.99, 4.7),
        ("iRobot Roomba j{n}+ Self-Emptying Robot Vacuum Wi-Fi Smart Mapping", "Smart Appliances", 449.00, 4.5),
        ("Ring Video Doorbell (2nd Gen) 1080p HD Wi-Fi Night Vision Motion Alert", "Security", 59.99, 4.3),
        ("TP-Link Archer AXE{n}000 Wi-Fi 6E Router Tri-Band Gigabit MU-MIMO", "Networking", 199.99, 4.5),
        ("Samsung 870 EVO {n}TB 2.5\" SATA III Internal SSD 560MB/s Read MZ-77E", "Storage", 84.99, 4.7),
        ("WD Blue SN{n}80 500GB M.2 NVMe Internal SSD up to 3500 MB/s", "Storage", 49.99, 4.6),
    ]

    category_descriptions = {
        "Headphones": "Experience superior audio immersion with advanced active noise cancellation technology, premium 40mm drivers delivering deep bass and crisp highs, and a comfortable over-ear design with memory foam cushions rated for 30+ hours of wireless playback.",
        "Earbuds": "True wireless earbuds with 6mm dynamic drivers, customizable EQ via app, IPX4 sweat and water resistance, and a total playtime of 36 hours with the compact charging case.",
        "Cameras": "Capture stunning images and 4K video with fast phase-detect autofocus, in-body image stabilisation, and a weather-sealed body designed for everyday shooting adventures.",
        "Gaming Laptops": "Dominate every session with a high-refresh-rate display, latest-gen GPU, and a cooling system engineered to sustain peak performance during extended gaming marathons.",
        "Laptops": "Ultra-portable powerhouse with all-day battery life, a vibrant high-resolution display, and fast NVMe SSD storage for seamless multitasking whether in the office or on the go.",
        "Tablets": "Versatile 2-in-1 device that transforms from tablet to laptop with a detachable keyboard cover, offering professional-grade software support and a vivid PixelSense display.",
        "Accessories": "Premium accessory crafted from durable materials to protect and enhance your devices with a perfect fit, reliable performance, and stylish finish.",
        "Phone Cases": "Military-grade drop protection for your smartphone with reinforced corners, precise cutouts, and a slim profile that fits standard wireless chargers.",
        "Chargers": "Intelligent multi-port charger supporting USB-C Power Delivery and MagSafe wireless charging, automatically detecting and delivering optimal wattage to each connected device.",
        "Smart Home": "Smart voice-controlled hub compatible with Alexa, Google Assistant, and HomeKit, enabling hands-free control of connected lights, locks, thermostats, and entertainment systems.",
        "Speakers": "Room-filling hi-res wireless audio with Trueplay acoustic tuning, seamless multi-room sync, and a minimalist design that complements any décor.",
        "Streaming": "Stream thousands of channels, apps, and Alexa skills in brilliant 4K HDR and Dolby Atmos sound, all delivered through a fast, responsive interface.",
        "Gaming Peripherals": "Professional gaming peripheral featuring ultra-low-latency wireless, a high-precision sensor, and full-spectrum RGB lighting with per-key customisation via companion software.",
        "Monitors": "High-fidelity monitor with wide colour gamut, HDR support, ergonomic height and tilt adjustment, and a rapid 1ms response time ideal for both creative workflows and competitive gaming.",
        "Keyboards": "Whisper-quiet scissor-switch keyboard with backlit keys, multi-device Bluetooth pairing, USB-C charging, and a slim aluminium chassis built to last.",
        "Mice": "Ergonomic wireless mouse with an ultra-precise laser sensor, customisable side buttons, and silent clicks engineered for productivity across multiple devices.",
        "Storage": "High-speed portable storage with USB 3.0 Gen 2 interface, automatic cloud backup integration, 256-bit hardware encryption, and a shock-resistant aluminium housing.",
        "E-Readers": "Read in any lighting with a glare-free Paperwhite display, IPX8 waterproofing, adjustable warm light, and weeks of battery life on a single charge.",
        "Smart Appliances": "AI-powered smart appliance that maps your home in 3D, avoids obstacles with precision, and empties its own dustbin — controlled entirely via the companion app or voice commands.",
        "Security": "Full HD 1080p security camera with colour night vision, two-way audio, customisable motion zones, and optional Ring Protect cloud subscription for 24/7 recording.",
        "Networking": "Next-generation Wi-Fi 6E tri-band router with 160 MHz channels, 8-stream MU-MIMO, WPA3 encryption, and easy setup through the companion app.",
    }

    random.seed(42)
    products = []

    for i in range(sample_size):
        tpl = templates[i % len(templates)]
        n = (i // len(templates)) + 1
        title_tpl, category, base_price, base_rating = tpl

        title = title_tpl.replace("{n}", str(n))
        price = round(base_price * random.uniform(0.85, 1.15), 2)
        rating = round(min(5.0, max(1.0, base_rating + random.uniform(-0.3, 0.1))), 1)
        description = category_descriptions.get(
            category,
            "Premium electronics product engineered for performance, durability, and ease of use in everyday and professional environments.",
        )

        products.append({
            "id": f"B{i:09d}",
            "title": title,
            "description": description,
            "price": price,
            "rating": rating,
            "category": category,
            "imageUrl": "",
        })

    return products


if __name__ == "__main__":
    parser = argparse.ArgumentParser(
        description="Download or generate Electronics product dataset"
    )
    parser.add_argument(
        "--source",
        choices=["huggingface", "mock"],
        default="huggingface",
        help="Data source: 'huggingface' (requires datasets library) or 'mock'",
    )
    parser.add_argument(
        "--sample",
        type=int,
        default=SAMPLE_SIZE,
        help=f"Number of products to sample (default: {SAMPLE_SIZE})",
    )
    args = parser.parse_args()

    print(f"Output: {OUTPUT_FILE}")

    if args.source == "mock":
        products = generate_mock_data(args.sample)
    else:
        try:
            products = download_from_huggingface(args.sample)
        except Exception as e:
            print(f"HuggingFace download failed ({e}). Falling back to mock data...")
            products = generate_mock_data(args.sample)

    with open(OUTPUT_FILE, "w", encoding="utf-8") as f:
        json.dump(products, f, ensure_ascii=False, indent=2)

    print(f"\n✅ Saved {len(products)} products → {OUTPUT_FILE}")
