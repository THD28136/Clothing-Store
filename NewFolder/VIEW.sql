-- ========================
-- I. VIEW
-- ========================
GO

-- 1
IF OBJECT_ID('vw_ProductList','V') IS NOT NULL DROP VIEW vw_ProductList;
GO
CREATE VIEW vw_ProductList AS
SELECT p.*, c.name AS category_name
FROM products p JOIN categories c ON p.category_id = c.category_id;
GO

-- 2
IF OBJECT_ID('vw_InStockProducts','V') IS NOT NULL DROP VIEW vw_InStockProducts;
GO
CREATE VIEW vw_InStockProducts AS
SELECT * FROM products WHERE stock > 0;
GO

-- 3
IF OBJECT_ID('vw_FeaturedProducts','V') IS NOT NULL DROP VIEW vw_FeaturedProducts;
GO
CREATE VIEW vw_FeaturedProducts AS
SELECT * FROM products WHERE is_featured = 1;
GO

-- 4
IF OBJECT_ID('vw_OrderSummary','V') IS NOT NULL DROP VIEW vw_OrderSummary;
GO
CREATE VIEW vw_OrderSummary AS
SELECT o.order_id, u.name, o.order_date
FROM orders o JOIN users u ON o.user_id = u.user_id;
GO

-- 5
IF OBJECT_ID('vw_OrderDetailsFull','V') IS NOT NULL DROP VIEW vw_OrderDetailsFull;
GO
CREATE VIEW vw_OrderDetailsFull AS
SELECT od.*, p.name, p.price
FROM order_details od JOIN products p ON od.product_id = p.product_id;
GO

-- 6
IF OBJECT_ID('vw_TotalOrderAmount','V') IS NOT NULL DROP VIEW vw_TotalOrderAmount;
GO
CREATE VIEW vw_TotalOrderAmount AS
SELECT od.order_id, SUM(od.quantity * p.price) AS total
FROM order_details od JOIN products p ON od.product_id = p.product_id
GROUP BY od.order_id;
GO

-- 7
IF OBJECT_ID('vw_UserOrders','V') IS NOT NULL DROP VIEW vw_UserOrders;
GO
CREATE VIEW vw_UserOrders AS
SELECT u.name, o.order_id
FROM users u JOIN orders o ON u.user_id = o.user_id;
GO

-- 8
IF OBJECT_ID('vw_LowStock','V') IS NOT NULL DROP VIEW vw_LowStock;
GO
CREATE VIEW vw_LowStock AS
SELECT * FROM products WHERE stock < 10;
GO

-- 9
IF OBJECT_ID('vw_CategoryProductCount','V') IS NOT NULL DROP VIEW vw_CategoryProductCount;
GO
CREATE VIEW vw_CategoryProductCount AS
SELECT c.name, COUNT(p.product_id) AS total_products
FROM categories c LEFT JOIN products p ON c.category_id = p.category_id
GROUP BY c.name;
GO

-- 10
IF OBJECT_ID('vw_TopSellingProducts','V') IS NOT NULL DROP VIEW vw_TopSellingProducts;
GO
CREATE VIEW vw_TopSellingProducts AS
SELECT TOP 10 p.name, SUM(od.quantity) AS total_sold
FROM order_details od JOIN products p ON od.product_id = p.product_id
GROUP BY p.name
ORDER BY total_sold DESC;
GO
-- 11
CREATE VIEW vw_RecentOrders AS
SELECT * FROM orders
WHERE order_date >= DATEADD(DAY, -7, GETDATE());
GO