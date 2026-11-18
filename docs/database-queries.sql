-- PostgreSQL read-only verification. Connect to the intended disposable database first.
SELECT "MigrationId", "ProductVersion" FROM "__EFMigrationsHistory" ORDER BY "MigrationId";
SELECT "Id", "StudentNumber", "NormalizedNumber", "Name", "Contact", "CanBorrow", "Version" FROM "Students" ORDER BY "Id";
SELECT "Id", "Identifier", "NormalizedIdentifier", "Name", "Description", "IsAvailable", "Version" FROM "Equipments" ORDER BY "Id";
SELECT b."Id", s."StudentNumber", s."Name" AS "Student", e."Identifier", e."Name" AS "Equipment",
       b."DateBorrowed", b."ExpectedReturnDate", b."ActualReturnDate",
       CASE WHEN b."ActualReturnDate" IS NULL THEN 'Active' ELSE 'Returned' END AS "Status"
FROM "Borrowings" b JOIN "Students" s ON s."Id" = b."StudentId"
JOIN "Equipments" e ON e."Id" = b."EquipmentId" ORDER BY b."Id";
-- Expected: no rows for either anomaly query.
SELECT "EquipmentId", count(*) FROM "Borrowings" WHERE "ActualReturnDate" IS NULL
GROUP BY "EquipmentId" HAVING count(*) > 1;
SELECT "StudentId", count(*) FROM "Borrowings" WHERE "ActualReturnDate" IS NULL
GROUP BY "StudentId" HAVING count(*) > 3;
SELECT e."Id", e."IsAvailable" FROM "Equipments" e
WHERE e."IsAvailable" = EXISTS (SELECT 1 FROM "Borrowings" b WHERE b."EquipmentId" = e."Id" AND b."ActualReturnDate" IS NULL);
-- Inspect actual database-enforced keys, FKs, CHECKs and indexes.
SELECT conrelid::regclass AS "Table", conname, pg_get_constraintdef(oid)
FROM pg_constraint WHERE connamespace = 'public'::regnamespace ORDER BY conrelid::regclass::text, conname;
SELECT tablename, indexname, indexdef FROM pg_indexes WHERE schemaname = 'public' ORDER BY tablename, indexname;
