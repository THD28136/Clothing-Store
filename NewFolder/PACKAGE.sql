-- ========================
-- V. PACKAGE 
-- ========================
GO

IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name='pkg_order')
EXEC('CREATE SCHEMA pkg_order');
GO

-- 1
IF OBJECT_ID('pkg_order.sp1','P') IS NOT NULL DROP PROC pkg_order.sp1;
GO
CREATE PROC pkg_order.sp1
AS
SELECT * FROM orders;
GO

-- 2
IF OBJECT_ID('pkg_order.sp2','P') IS NOT NULL DROP PROC pkg_order.sp2;
GO
CREATE PROC pkg_order.sp2
AS
SELECT * FROM order_details;
GO

-- 3
IF OBJECT_ID('pkg_order.sp3','P') IS NOT NULL DROP PROC pkg_order.sp3;
GO
CREATE PROC pkg_order.sp3
AS
SELECT * FROM products;
GO

-- 4
IF OBJECT_ID('pkg_order.sp4','P') IS NOT NULL DROP PROC pkg_order.sp4;
GO
CREATE PROC pkg_order.sp4
AS
SELECT * FROM users;
GO

-- 5
IF OBJECT_ID('pkg_order.sp5','P') IS NOT NULL DROP PROC pkg_order.sp5;
GO
CREATE PROC pkg_order.sp5
AS
SELECT * FROM categories;
GO

-- 6
IF OBJECT_ID('pkg_order.sp6','P') IS NOT NULL DROP PROC pkg_order.sp6;
GO
CREATE PROC pkg_order.sp6
AS
SELECT COUNT(*) AS total_orders FROM orders;
GO

-- 7
IF OBJECT_ID('pkg_order.sp7','P') IS NOT NULL DROP PROC pkg_order.sp7;
GO
CREATE PROC pkg_order.sp7
AS
SELECT COUNT(*) AS total_products FROM products;
GO

-- 8
IF OBJECT_ID('pkg_order.sp8','P') IS NOT NULL DROP PROC pkg_order.sp8;
GO
CREATE PROC pkg_order.sp8
AS
SELECT GETDATE() AS now_time;
GO

-- 9
IF OBJECT_ID('pkg_order.sp9','P') IS NOT NULL DROP PROC pkg_order.sp9;
GO
CREATE PROC pkg_order.sp9
AS
SELECT TOP 1 * FROM products;
GO

-- 10
IF OBJECT_ID('pkg_order.sp10','P') IS NOT NULL DROP PROC pkg_order.sp10;
GO
CREATE PROC pkg_order.sp10
AS
SELECT TOP 1 * FROM orders;
GO