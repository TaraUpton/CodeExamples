
CREATE TABLE [nso].ProductAssignmentProduct (
	ProductAssignmentProductGuid [uniqueidentifier] NOT NULL PRIMARY KEY,
	ProductAssignmentGuid [uniqueidentifier] NOT NULL,
	ProductGuid [uniqueidentifier] NOT NULL,
    CONSTRAINT [FK_ProductAssignmentProduct_Product] FOREIGN KEY ([ProductGuid]) REFERENCES [nso].Product([ProductGuid]),
    CONSTRAINT [FK_ProductAssignmentProduct_ProductAssignment] FOREIGN KEY ([ProductAssignmentGuid]) REFERENCES [nso].ProductAssignment([ProductAssignmentGuid])  ON DELETE CASCADE,
) 
GO

CREATE UNIQUE INDEX [IX_ProductAssignmentProduct] ON [nso].ProductAssignmentProduct ([ProductAssignmentGuid],[ProductGuid])


