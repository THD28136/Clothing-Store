-- ========================
-- III. FUNCTION
-- ========================
GO

-- 1
IF OBJECT_ID('fn_GetPrice') IS NOT NULL DROP FUNCTION fn_GetPrice;
GO
CREATE FUNCTION fn_GetPrice(@id INT)
RETURNS DECIMAL(10,2)
AS BEGIN RETURN (SELECT price FROM products WHERE product_id=@id) END;
GO

-- 2
IF OBJECT_ID('fn_TotalOrder') IS NOT NULL DROP FUNCTION fn_TotalOrder;
GO
CREATE FUNCTION fn_TotalOrder(@id INT)
RETURNS DECIMAL(10,2)
AS BEGIN RETURN (
SELECT SUM(quantity*price)
FROM order_details od JOIN products p ON od.product_id=p.product_id
WHERE order_id=@id) END;
GO

-- 3
IF OBJECT_ID('fn_CountProduct') IS NOT NULL DROP FUNCTION fn_CountProduct;
GO
CREATE FUNCTION fn_CountProduct(@cid INT)
RETURNS INT
AS BEGIN RETURN (SELECT COUNT(*) FROM products WHERE category_id=@cid) END;
GO

-- 4
IF OBJECT_ID('fn_CheckStock') IS NOT NULL DROP FUNCTION fn_CheckStock;
GO
CREATE FUNCTION fn_CheckStock(@id INT)
RETURNS INT
AS BEGIN RETURN (SELECT stock FROM products WHERE product_id=@id) END;
GO

-- 5
IF OBJECT_ID('fn_TotalOrdersUser') IS NOT NULL DROP FUNCTION fn_TotalOrdersUser;
GO
CREATE FUNCTION fn_TotalOrdersUser(@uid INT)
RETURNS INT
AS BEGIN RETURN (SELECT COUNT(*) FROM orders WHERE user_id=@uid) END;
GO

-- 6
IF OBJECT_ID('fn_MaxPrice') IS NOT NULL DROP FUNCTION fn_MaxPrice;
GO
CREATE FUNCTION fn_MaxPrice() RETURNS DECIMAL(10,2)
AS BEGIN RETURN (SELECT MAX(price) FROM products) END;
GO

-- 7
IF OBJECT_ID('fn_MinPrice') IS NOT NULL DROP FUNCTION fn_MinPrice;
GO
CREATE FUNCTION fn_MinPrice() RETURNS DECIMAL(10,2)
AS BEGIN RETURN (SELECT MIN(price) FROM products) END;
GO

-- 8
IF OBJECT_ID('fn_AvgPrice') IS NOT NULL DROP FUNCTION fn_AvgPrice;
GO
CREATE FUNCTION fn_AvgPrice() RETURNS DECIMAL(10,2)
AS BEGIN RETURN (SELECT AVG(price) FROM products) END;
GO

-- 9
IF OBJECT_ID('fn_TotalSold') IS NOT NULL DROP FUNCTION fn_TotalSold;
GO
CREATE FUNCTION fn_TotalSold(@id INT)
RETURNS INT
AS BEGIN RETURN (SELECT SUM(quantity) FROM order_details WHERE product_id=@id) END;
GO

-- 10
IF OBJECT_ID('fn_IsFeatured') IS NOT NULL DROP FUNCTION fn_IsFeatured;
GO
CREATE FUNCTION fn_IsFeatured(@id INT)
RETURNS BIT
AS BEGIN RETURN (SELECT is_featured FROM products WHERE product_id=@id) END;
GO
-- 11 
CREATE FUNCTION fn_TotalProducts()
RETURNS INT
AS
BEGIN
    RETURN (SELECT COUNT(*) FROM products);
END;
GO