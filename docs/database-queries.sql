-- 1. Basic Retrieval
SELECT * FROM Equipment;

-- 2. Filtering Equipment by Category
SELECT * FROM Equipment WHERE Category = 'Laptop';

-- 3. Join Query (Active Borrowings with Student and Equipment details)
SELECT 
    s.Name AS StudentName,
    e.Name AS EquipmentName,
    b.BorrowDate,
    b.ReturnDate,
    b.Status
FROM Borrowings b
JOIN Students s ON b.StudentId = s.Id
JOIN Equipment e ON b.EquipmentId = e.Id
WHERE b.Status = 0; -- 0 = Active, 1 = Returned

-- 4. Aggregate Query (Active borrowings per student)
SELECT 
    s.Name,
    COUNT(b.Id) AS ActiveBorrowings
FROM Students s
LEFT JOIN Borrowings b ON s.Id = b.StudentId AND b.Status = 0
GROUP BY s.Id, s.Name;

-- 5. Update Statement (Mark borrowing as returned)
UPDATE Borrowings 
SET Status = 1, ReturnDate = CURRENT_TIMESTAMP 
WHERE Id = 1;