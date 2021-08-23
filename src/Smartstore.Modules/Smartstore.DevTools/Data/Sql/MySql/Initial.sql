
CREATE TABLE IF NOT EXISTS `DevToolsTestEntity` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Name` varchar(400) CHARACTER SET utf8mb4 NOT NULL,
    `Description` longtext CHARACTER SET utf8mb4 NULL,
    `PageSize` int NULL,
    `LimitedToStores` tinyint(1) NOT NULL,
    `SubjectToAcl` tinyint(1) NOT NULL,
    `Published` tinyint(1) NOT NULL,
    `Deleted` tinyint(1) NOT NULL,
    `DisplayOrder` int NOT NULL,
    `CreatedOnUtc` datetime(6) NOT NULL,
    `UpdatedOnUtc` datetime(6) NOT NULL,
    CONSTRAINT `PK_DevToolsTestEntity` PRIMARY KEY (`Id`),
    KEY `IX_Deleted` (`Deleted`),
    KEY `IX_DisplayOrder` (`DisplayOrder`),
    KEY `IX_LimitedToStores` (`LimitedToStores`),
    KEY `IX_SubjectToAcl` (`SubjectToAcl`)
);


