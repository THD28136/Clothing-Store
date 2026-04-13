-- ========================
-- IV. TRIGGER
-- ========================
GO

-- 1
IF OBJECT_ID('trg_UpdateStock','TR') IS NOT NULL DROP TRIGGER trg_UpdateStock;
GO
CREATE TRIGGER trg_UpdateStock ON order_details AFTER INSERT
AS UPDATE p SET stock=stock-i.quantity
FROM products p JOIN inserted i ON p.product_id=i.product_id;
GO

-- 2
IF OBJECT_ID('trg_CheckStock','TR') IS NOT NULL DROP TRIGGER trg_CheckStock;
GO
CREATE TRIGGER trg_CheckStock ON products FOR UPDATE
AS IF EXISTS(SELECT * FROM inserted WHERE stock<0)
BEGIN RAISERROR('Stock < 0',16,1); ROLLBACK; END;
GO

-- 3
IF OBJECT_ID('trg_SetDate','TR') IS NOT NULL DROP TRIGGER trg_SetDate;
GO
CREATE TRIGGER trg_SetDate ON orders INSTEAD OF INSERT
AS INSERT INTO orders(user_id,order_date)
SELECT user_id,GETDATE() FROM inserted;
GO

-- 4
IF OBJECT_ID('trg_NoDeleteCategory','TR') IS NOT NULL DROP TRIGGER trg_NoDeleteCategory;
GO
CREATE TRIGGER trg_NoDeleteCategory ON categories FOR DELETE
AS IF EXISTS(SELECT * FROM products p JOIN deleted d ON p.category_id=d.category_id)
BEGIN RAISERROR('Cannot delete',16,1); ROLLBACK; END;
GO

-- 5
CREATE TRIGGER trg_LogInsert ON products AFTER INSERT AS PRINT 'Insert OK';
GO

-- 6
CREATE TRIGGER trg_LogDelete ON products AFTER DELETE AS PRINT 'Delete OK';
GO

-- 7
CREATE TRIGGER trg_PriceCheck ON products FOR UPDATE
AS IF EXISTS(SELECT * FROM inserted WHERE price<=0) BEGIN ROLLBACK; END;
GO

-- 8
CREATE TRIGGER trg_UserDelete ON users FOR DELETE
AS IF EXISTS(SELECT * FROM orders o JOIN deleted d ON o.user_id=d.user_id)
BEGIN ROLLBACK; END;
GO

-- 9
CREATE TRIGGER trg_DefaultRole ON users AFTER INSERT
AS UPDATE users SET role='customer' WHERE role IS NULL;
GO

-- 10
CREATE TRIGGER trg_UpdateLog ON products AFTER UPDATE AS PRINT 'Updated';
GO