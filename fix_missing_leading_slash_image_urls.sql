-- =====================================================================================
-- Data-fix script: add a leading "/" to Product/ProductVariant ImageUrl values that
-- were saved without one (root cause: Bulk Product Importer historically stored the
-- raw relative path returned by ImageService.SaveImageAsync()/DownloadAndCompressImageAsync(),
-- e.g. "products/abc123.webp", while the manual Admin upload flow always prepended "/",
-- e.g. "/images/products/abc123.webp"). This inconsistency is what made product images
-- disappear specifically on the Checkout "Order & Materials Summary" panel for
-- bulk-imported products, since that view used the raw value as <img src="..."> without
-- normalizing it (unlike the Cart page, which already normalized it defensively).
--
-- Scope: Products.ImageUrl and ProductVariants.ImageUrl only, as requested.
-- (ProductImages.Url — the additional/gallery images table — was NOT included here since
-- it wasn't part of the requested scope; flag separately if you want it covered too.)
--
-- This script only touches rows where ImageUrl:
--   - is not null/empty, AND
--   - does not already start with "/", AND
--   - does not start with "http" (external/absolute URLs are left untouched)
--
-- Review the SELECT preview output first. The UPDATEs run inside a transaction that is
-- NOT committed automatically — you must explicitly run COMMIT; (or ROLLBACK; to undo).
-- =====================================================================================

START TRANSACTION;

-- ---------------------------------------------------------------------------
-- 1) PREVIEW — run this first and review the rows before touching anything
-- ---------------------------------------------------------------------------
SELECT 'Products' AS "Table", "Id", "ImageUrl" AS "CurrentValue",
       '/' || "ImageUrl" AS "WouldBecome"
FROM "Products"
WHERE "ImageUrl" IS NOT NULL
  AND "ImageUrl" <> ''
  AND "ImageUrl" NOT LIKE '/%'
  AND "ImageUrl" NOT ILIKE 'http%';

SELECT 'ProductVariants' AS "Table", "Id", "ImageUrl" AS "CurrentValue",
       '/' || "ImageUrl" AS "WouldBecome"
FROM "ProductVariants"
WHERE "ImageUrl" IS NOT NULL
  AND "ImageUrl" <> ''
  AND "ImageUrl" NOT LIKE '/%'
  AND "ImageUrl" NOT ILIKE 'http%';

-- ---------------------------------------------------------------------------
-- 2) THE ACTUAL FIX — uncomment and run only after reviewing the preview above
-- ---------------------------------------------------------------------------

-- UPDATE "Products"
-- SET "ImageUrl" = '/' || "ImageUrl"
-- WHERE "ImageUrl" IS NOT NULL
--   AND "ImageUrl" <> ''
--   AND "ImageUrl" NOT LIKE '/%'
--   AND "ImageUrl" NOT ILIKE 'http%';

-- UPDATE "ProductVariants"
-- SET "ImageUrl" = '/' || "ImageUrl"
-- WHERE "ImageUrl" IS NOT NULL
--   AND "ImageUrl" <> ''
--   AND "ImageUrl" NOT LIKE '/%'
--   AND "ImageUrl" NOT ILIKE 'http%';

-- Verify the row counts / sample values look right, THEN:
-- COMMIT;

-- If anything looks wrong instead, run:
-- ROLLBACK;
