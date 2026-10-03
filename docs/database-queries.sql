-- 1. Basic Retrieval
SELECT * FROM Equipment;

-- 2. Filtering (Available Equipment)
SELECT * FROM Equipment WHERE IsAvailable = 1;

-- 3. Join Query (Active Borrowings with Student and Equipment details)
SELECT 
    s.FullName AS Student,
    e.Name AS Equipment,
    b.BorrowedAt AS Borrowed,
    b.ExpectedReturnAt AS Due
FROM Borrowings b
JOIN Students s ON b.StudentId = s.Id
JOIN Equipment e ON b.EquipmentId = e.Id
WHERE b.Status = 'Active';

-- 4. Aggregate Query (Active borrowings per student)
SELECT 
    s.FullName, 
    COUNT(b.Id) AS ActiveBorrowings
FROM Students s
LEFT JOIN Borrowings b ON s.Id = b.StudentId AND b.Status = 'Active'
GROUP BY s.Id, s.FullName;

-- 5. Update Statement (Return equipment update example)
UPDATE Equipment SET IsAvailable = 1 WHERE Id = 1;