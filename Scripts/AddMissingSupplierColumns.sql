-- Add missing columns to Suppliers table
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Suppliers' AND COLUMN_NAME = 'Name')
    ALTER TABLE [Suppliers] ADD [Name] nvarchar(100) NOT NULL DEFAULT '';

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Suppliers' AND COLUMN_NAME = 'BankAccount')
    ALTER TABLE [Suppliers] ADD [BankAccount] nvarchar(50) NULL;

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Suppliers' AND COLUMN_NAME = 'BankName')
    ALTER TABLE [Suppliers] ADD [BankName] nvarchar(100) NULL;

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Suppliers' AND COLUMN_NAME = 'City')
    ALTER TABLE [Suppliers] ADD [City] nvarchar(100) NULL;

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Suppliers' AND COLUMN_NAME = 'ContactEmail')
    ALTER TABLE [Suppliers] ADD [ContactEmail] nvarchar(100) NULL;

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Suppliers' AND COLUMN_NAME = 'ContactPhone')
    ALTER TABLE [Suppliers] ADD [ContactPhone] nvarchar(20) NULL;

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Suppliers' AND COLUMN_NAME = 'Country')
    ALTER TABLE [Suppliers] ADD [Country] nvarchar(100) NULL;

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Suppliers' AND COLUMN_NAME = 'CurrentBalance')
    ALTER TABLE [Suppliers] ADD [CurrentBalance] decimal(18,2) NOT NULL DEFAULT 0;

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Suppliers' AND COLUMN_NAME = 'Notes')
    ALTER TABLE [Suppliers] ADD [Notes] nvarchar(1000) NULL;

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Suppliers' AND COLUMN_NAME = 'PostalCode')
    ALTER TABLE [Suppliers] ADD [PostalCode] nvarchar(10) NULL;

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Suppliers' AND COLUMN_NAME = 'Province')
    ALTER TABLE [Suppliers] ADD [Province] nvarchar(100) NULL;

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Suppliers' AND COLUMN_NAME = 'TaxNumber')
    ALTER TABLE [Suppliers] ADD [TaxNumber] nvarchar(50) NULL;

-- Add missing column to Branches table if needed
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Branches' AND COLUMN_NAME = 'SupplierId')
    ALTER TABLE [Branches] ADD [SupplierId] int NULL;

-- Add missing column to Users table if needed
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'Name')
    ALTER TABLE [Users] ADD [Name] nvarchar(100) NOT NULL DEFAULT '';

-- Add missing column to Sales table if needed  
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Sales' AND COLUMN_NAME = 'UserId')
    ALTER TABLE [Sales] ADD [UserId] int NOT NULL DEFAULT 1;

-- Create PurchaseOrders table if not exists
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'PurchaseOrders')
BEGIN
    CREATE TABLE [PurchaseOrders] (
        [Id] int IDENTITY(1,1) NOT NULL,
        [PurchaseOrderNumber] nvarchar(20) NOT NULL,
        [SupplierId] int NOT NULL,
        [BranchId] int NULL,
        [OrderDate] datetime2 NOT NULL,
        [DeliveryDate] datetime2 NULL,
        [TotalAmount] decimal(18,2) NOT NULL,
        [Status] int NOT NULL,
        [Notes] nvarchar(1000) NOT NULL DEFAULT '',
        [CreatedBy] int NOT NULL,
        [CreatedByUserId] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_PurchaseOrders] PRIMARY KEY ([Id])
    );
END;

-- Create StockMutations table if not exists
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'StockMutations')
BEGIN
    CREATE TABLE [StockMutations] (
        [Id] int IDENTITY(1,1) NOT NULL,
        [ProductId] int NOT NULL,
        [UserId] int NOT NULL,
        [BranchId] int NULL,
        [MutationType] int NOT NULL,
        [Quantity] int NOT NULL,
        [StockBefore] int NOT NULL,
        [StockAfter] int NOT NULL,
        [UnitCost] decimal(18,2) NULL,
        [TotalCost] decimal(18,2) NULL,
        [Notes] nvarchar(500) NOT NULL DEFAULT '',
        [ReferenceNumber] nvarchar(50) NULL,
        [SaleId] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_StockMutations] PRIMARY KEY ([Id])
    );
END;