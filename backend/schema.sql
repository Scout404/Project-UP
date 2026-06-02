SET NAMES utf8mb4;

CREATE TABLE IF NOT EXISTS `Carts` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `UserId` INT NOT NULL,
    CONSTRAINT `PK_Carts` PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `Categories` (
    `CategoryId` INT NOT NULL AUTO_INCREMENT,
    `Name` LONGTEXT NOT NULL,
    CONSTRAINT `PK_Categories` PRIMARY KEY (`CategoryId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `Colors` (
    `ColorId` INT NOT NULL AUTO_INCREMENT,
    `Name` LONGTEXT NOT NULL,
    CONSTRAINT `PK_Colors` PRIMARY KEY (`ColorId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `Customer` (
    `CustomerId` INT NOT NULL AUTO_INCREMENT,
    `FirstName` LONGTEXT NULL,
    `LastName` LONGTEXT NULL,
    `Email` LONGTEXT NULL,
    `PasswordHash` LONGTEXT NULL,
    `Phone` LONGTEXT NULL,
    `IsMember` TINYINT(1) NOT NULL,
    `CreatedAt` DATETIME(6) NULL,
    CONSTRAINT `PK_Customer` PRIMARY KEY (`CustomerId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `Sizes` (
    `SizeId` INT NOT NULL AUTO_INCREMENT,
    `Name` LONGTEXT NOT NULL,
    CONSTRAINT `PK_Sizes` PRIMARY KEY (`SizeId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `Users` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `Username` LONGTEXT NOT NULL,
    `Password` LONGTEXT NOT NULL,
    `Email` LONGTEXT NOT NULL,
    `Role` LONGTEXT NOT NULL,
    `CreatedAt` DATETIME(6) NOT NULL,
    CONSTRAINT `PK_Users` PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `CartItems` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `VariantId` INT NOT NULL,
    `Name` LONGTEXT NOT NULL,
    `Price` DECIMAL(18,2) NOT NULL,
    `Quantity` INT NOT NULL,
    `CartId` INT NOT NULL,
    CONSTRAINT `PK_CartItems` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_CartItems_Carts_CartId`
        FOREIGN KEY (`CartId`) REFERENCES `Carts` (`Id`)
        ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `Products` (
    `ProductId` INT NOT NULL AUTO_INCREMENT,
    `Name` LONGTEXT NOT NULL,
    `Description` LONGTEXT NOT NULL,
    `CategoryId` INT NOT NULL,
    `Brand` LONGTEXT NOT NULL,
    `BasePrice` DECIMAL(18,2) NOT NULL,
    `IsActive` TINYINT(1) NOT NULL,
    `StockQuantity` INT NOT NULL,
    CONSTRAINT `PK_Products` PRIMARY KEY (`ProductId`),
    CONSTRAINT `FK_Products_Categories_CategoryId`
        FOREIGN KEY (`CategoryId`) REFERENCES `Categories` (`CategoryId`)
        ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `Address` (
    `AddressId` INT NOT NULL AUTO_INCREMENT,
    `CustomerId` INT NOT NULL,
    `Street` LONGTEXT NOT NULL,
    `City` LONGTEXT NOT NULL,
    `PostalCode` LONGTEXT NOT NULL,
    `Country` LONGTEXT NOT NULL,
    CONSTRAINT `PK_Address` PRIMARY KEY (`AddressId`),
    CONSTRAINT `FK_Address_Customer_CustomerId`
        FOREIGN KEY (`CustomerId`) REFERENCES `Customer` (`CustomerId`)
        ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `Orders` (
    `OrderId` INT NOT NULL AUTO_INCREMENT,
    `CustomerId` INT NOT NULL,
    `OrderDate` DATETIME(6) NOT NULL,
    `TotalPrice` DECIMAL(65,30) NOT NULL,
    `Status` INT NOT NULL,
    CONSTRAINT `PK_Orders` PRIMARY KEY (`OrderId`),
    CONSTRAINT `FK_Orders_Customer_CustomerId`
        FOREIGN KEY (`CustomerId`) REFERENCES `Customer` (`CustomerId`)
        ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `Wishlist` (
    `WishlistId` INT NOT NULL AUTO_INCREMENT,
    `CustomerId` INT NOT NULL,
    CONSTRAINT `PK_Wishlist` PRIMARY KEY (`WishlistId`),
    CONSTRAINT `FK_Wishlist_Customer_CustomerId`
        FOREIGN KEY (`CustomerId`) REFERENCES `Customer` (`CustomerId`)
        ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `ProductVariants` (
    `ProductVariantId` INT NOT NULL AUTO_INCREMENT,
    `ProductId` INT NOT NULL,
    `SizeId` INT NOT NULL,
    `ColorId` INT NOT NULL,
    `PictureUrl` LONGTEXT NOT NULL,
    `Stock` INT NOT NULL,
    `MinStock` INT NOT NULL,
    `MaxStock` INT NOT NULL,
    `ColorId1` INT NULL,
    `SizeId1` INT NULL,
    CONSTRAINT `PK_ProductVariants` PRIMARY KEY (`ProductVariantId`),
    CONSTRAINT `FK_ProductVariants_Colors_ColorId`
        FOREIGN KEY (`ColorId`) REFERENCES `Colors` (`ColorId`)
        ON DELETE RESTRICT,
    CONSTRAINT `FK_ProductVariants_Colors_ColorId1`
        FOREIGN KEY (`ColorId1`) REFERENCES `Colors` (`ColorId`),
    CONSTRAINT `FK_ProductVariants_Products_ProductId`
        FOREIGN KEY (`ProductId`) REFERENCES `Products` (`ProductId`)
        ON DELETE CASCADE,
    CONSTRAINT `FK_ProductVariants_Sizes_SizeId`
        FOREIGN KEY (`SizeId`) REFERENCES `Sizes` (`SizeId`)
        ON DELETE RESTRICT,
    CONSTRAINT `FK_ProductVariants_Sizes_SizeId1`
        FOREIGN KEY (`SizeId1`) REFERENCES `Sizes` (`SizeId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `Reviews` (
    `ReviewId` INT NOT NULL AUTO_INCREMENT,
    `ProductId` INT NOT NULL,
    `CustomerId` INT NOT NULL,
    `ReviewText` LONGTEXT NOT NULL,
    `Rating` INT NOT NULL,
    `CreatedAt` DATETIME(6) NULL,
    CONSTRAINT `PK_Reviews` PRIMARY KEY (`ReviewId`),
    CONSTRAINT `FK_Reviews_Customer_CustomerId`
        FOREIGN KEY (`CustomerId`) REFERENCES `Customer` (`CustomerId`)
        ON DELETE CASCADE,
    CONSTRAINT `FK_Reviews_Products_ProductId`
        FOREIGN KEY (`ProductId`) REFERENCES `Products` (`ProductId`)
        ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `OrderAddresses` (
    `OrderId` INT NOT NULL,
    `Street` LONGTEXT NOT NULL,
    `City` LONGTEXT NOT NULL,
    `PostalCode` LONGTEXT NOT NULL,
    `Country` LONGTEXT NOT NULL,
    CONSTRAINT `PK_OrderAddresses` PRIMARY KEY (`OrderId`),
    CONSTRAINT `FK_OrderAddresses_Orders_OrderId`
        FOREIGN KEY (`OrderId`) REFERENCES `Orders` (`OrderId`)
        ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `OrderItems` (
    `OrderItemId` INT NOT NULL AUTO_INCREMENT,
    `OrderId` INT NOT NULL,
    `VariantId` INT NOT NULL,
    `Quantity` INT NOT NULL,
    `Price` DECIMAL(18,2) NOT NULL,
    `OrdersOrderId` INT NULL,
    CONSTRAINT `PK_OrderItems` PRIMARY KEY (`OrderItemId`),
    CONSTRAINT `FK_OrderItems_Orders_OrderId`
        FOREIGN KEY (`OrderId`) REFERENCES `Orders` (`OrderId`)
        ON DELETE CASCADE,
    CONSTRAINT `FK_OrderItems_Orders_OrdersOrderId`
        FOREIGN KEY (`OrdersOrderId`) REFERENCES `Orders` (`OrderId`),
    CONSTRAINT `FK_OrderItems_ProductVariants_VariantId`
        FOREIGN KEY (`VariantId`) REFERENCES `ProductVariants` (`ProductVariantId`)
        ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `WishlistItems` (
    `WishlistItemId` INT NOT NULL AUTO_INCREMENT,
    `ProductVariantId` INT NOT NULL,
    `WishlistId` INT NOT NULL,
    CONSTRAINT `PK_WishlistItems` PRIMARY KEY (`WishlistItemId`),
    CONSTRAINT `FK_WishlistItems_ProductVariants_ProductVariantId`
        FOREIGN KEY (`ProductVariantId`) REFERENCES `ProductVariants` (`ProductVariantId`)
        ON DELETE CASCADE,
    CONSTRAINT `FK_WishlistItems_Wishlist_WishlistId`
        FOREIGN KEY (`WishlistId`) REFERENCES `Wishlist` (`WishlistId`)
        ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE INDEX `IX_Address_CustomerId` ON `Address` (`CustomerId`);
CREATE INDEX `IX_CartItems_CartId` ON `CartItems` (`CartId`);
CREATE INDEX `IX_Carts_UserId` ON `Carts` (`UserId`);
CREATE INDEX `IX_OrderItems_OrderId` ON `OrderItems` (`OrderId`);
CREATE INDEX `IX_OrderItems_OrdersOrderId` ON `OrderItems` (`OrdersOrderId`);
CREATE INDEX `IX_OrderItems_VariantId` ON `OrderItems` (`VariantId`);
CREATE INDEX `IX_Orders_CustomerId` ON `Orders` (`CustomerId`);
CREATE INDEX `IX_Products_CategoryId` ON `Products` (`CategoryId`);
CREATE INDEX `IX_ProductVariants_ColorId` ON `ProductVariants` (`ColorId`);
CREATE INDEX `IX_ProductVariants_ColorId1` ON `ProductVariants` (`ColorId1`);
CREATE INDEX `IX_ProductVariants_ProductId` ON `ProductVariants` (`ProductId`);
CREATE INDEX `IX_ProductVariants_SizeId` ON `ProductVariants` (`SizeId`);
CREATE INDEX `IX_ProductVariants_SizeId1` ON `ProductVariants` (`SizeId1`);
CREATE INDEX `IX_Reviews_CustomerId` ON `Reviews` (`CustomerId`);
CREATE INDEX `IX_Reviews_ProductId` ON `Reviews` (`ProductId`);
CREATE UNIQUE INDEX `IX_Wishlist_CustomerId` ON `Wishlist` (`CustomerId`);
CREATE INDEX `IX_WishlistItems_ProductVariantId` ON `WishlistItems` (`ProductVariantId`);
CREATE INDEX `IX_WishlistItems_WishlistId` ON `WishlistItems` (`WishlistId`);
