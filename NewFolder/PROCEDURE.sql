-- ========================
-- II. PROCEDURE
-- ========================
GO

-- 1
IF OBJECT_ID('sp_AddProduct','P') IS NOT NULL DROP PROC sp_AddProduct;
GO
CREATE PROC sp_AddProduct
@name NVARCHAR(100), @price DECIMAL(10,2), @stock INT, @category_id INT
AS INSERT INTO products(name,price,stock,category_id)
VALUES (@name,@price,@stock,@category_id);
GO

-- 2
IF OBJECT_ID('sp_UpdateStock','P') IS NOT NULL DROP PROC sp_UpdateStock;
GO
CREATE PROC sp_UpdateStock
@id INT,@qty INT
AS UPDATE products SET stock = stock - @qty WHERE product_id = @id;
GO

-- 3
IF OBJECT_ID('sp_GetProductsByCategory','P') IS NOT NULL DROP PROC sp_GetProductsByCategory;
GO
CREATE PROC sp_GetProductsByCategory
@id INT
AS SELECT * FROM products WHERE category_id = @id;
GO

-- 4
IF OBJECT_ID('sp_CreateOrder','P') IS NOT NULL DROP PROC sp_CreateOrder;
GO
CREATE PROC sp_CreateOrder
@uid INT
AS INSERT INTO orders(user_id,order_date) VALUES(@uid,GETDATE());
GO

-- 5
IF OBJECT_ID('sp_AddOrderDetail','P') IS NOT NULL DROP PROC sp_AddOrderDetail;
GO
CREATE PROC sp_AddOrderDetail
@oid INT,@pid INT,@qty INT
AS INSERT INTO order_details VALUES(@oid,@pid,@qty);
GO

-- 6
IF OBJECT_ID('sp_DeleteProduct','P') IS NOT NULL DROP PROC sp_DeleteProduct;
GO
CREATE PROC sp_DeleteProduct
@id INT AS DELETE FROM products WHERE product_id=@id;
GO

-- 7
IF OBJECT_ID('sp_UpdatePrice','P') IS NOT NULL DROP PROC sp_UpdatePrice;
GO
CREATE PROC sp_UpdatePrice
@id INT,@price DECIMAL(10,2)
AS UPDATE products SET price=@price WHERE product_id=@id;
GO

-- 8
IF OBJECT_ID('sp_SearchProduct','P') IS NOT NULL DROP PROC sp_SearchProduct;
GO
CREATE PROC sp_SearchProduct
@key NVARCHAR(100)
AS SELECT * FROM products WHERE name LIKE '%'+@key+'%';
GO

-- 9
IF OBJECT_ID('sp_TotalRevenue','P') IS NOT NULL DROP PROC sp_TotalRevenue;
GO
CREATE PROC sp_TotalRevenue
AS SELECT SUM(od.quantity*p.price) revenue
FROM order_details od JOIN products p ON od.product_id=p.product_id;
GO

-- 10
IF OBJECT_ID('sp_TopProducts','P') IS NOT NULL DROP PROC sp_TopProducts;
GO
CREATE PROC sp_TopProducts
AS SELECT TOP 5 * FROM vw_TopSellingProducts;
GO
-- 11 
CREATE PROC sp_GetProductsByPriceRange
@min DECIMAL(10,2), @max DECIMAL(10,2)
AS
SELECT * FROM products WHERE price BETWEEN @min AND @max;
GO
-- 12
CREATE PROC sp_DeleteOrder
@id INT
AS
DELETE FROM orders WHERE order_id = @id;
GO