IF EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[FK_TimingSlices_TimeSlices]') AND parent_object_id = OBJECT_ID(N'[TimingSlices]'))
	ALTER TABLE [TimingSlices] DROP CONSTRAINT [FK_TimingSlices_TimeSlices]

IF EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[FK_TimeSlices_WorkItems]') AND parent_object_id = OBJECT_ID(N'[TimeSlices]'))
	ALTER TABLE [TimeSlices] DROP CONSTRAINT [FK_TimeSlices_WorkItems]

IF EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[FK_TimeSlices_People]') AND parent_object_id = OBJECT_ID(N'[TimeSlices]'))
	ALTER TABLE [TimeSlices] DROP CONSTRAINT [FK_TimeSlices_People]

IF EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[FK_TextPropertyValues_WorkItems]') AND parent_object_id = OBJECT_ID(N'[TextPropertyValues]'))
	ALTER TABLE [TextPropertyValues] DROP CONSTRAINT [FK_TextPropertyValues_WorkItems]

IF EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[FK_TextPropertyValues_TextProperties]') AND parent_object_id = OBJECT_ID(N'[TextPropertyValues]'))
	ALTER TABLE [TextPropertyValues] DROP CONSTRAINT [FK_TextPropertyValues_TextProperties]

IF EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[FK_ListValues_ListValueProperties]') AND parent_object_id = OBJECT_ID(N'[ListValues]'))
	ALTER TABLE [ListValues] DROP CONSTRAINT [FK_ListValues_ListValueProperties]

IF EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[FK_ListValuePropertyValues_WorkItems]') AND parent_object_id = OBJECT_ID(N'[ListValuePropertyValues]'))
	ALTER TABLE [ListValuePropertyValues] DROP CONSTRAINT [FK_ListValuePropertyValues_WorkItems]

IF EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[FK_ListValuePropertyValues_ListValues]') AND parent_object_id = OBJECT_ID(N'[ListValuePropertyValues]'))
	ALTER TABLE [ListValuePropertyValues] DROP CONSTRAINT [FK_ListValuePropertyValues_ListValues]

IF EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[FK_ListValuePropertyValues_ListValueProperties]') AND parent_object_id = OBJECT_ID(N'[ListValuePropertyValues]'))
	ALTER TABLE [ListValuePropertyValues] DROP CONSTRAINT [FK_ListValuePropertyValues_ListValueProperties]

IF EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[FK_Assignments_WorkItems]') AND parent_object_id = OBJECT_ID(N'[Assignments]'))
	ALTER TABLE [Assignments] DROP CONSTRAINT [FK_Assignments_WorkItems]

IF EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[FK_Assignments_People]') AND parent_object_id = OBJECT_ID(N'[Assignments]'))
	ALTER TABLE [Assignments] DROP CONSTRAINT [FK_Assignments_People]

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[WorkItems]') AND type in (N'U'))
	DROP TABLE [WorkItems]

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[TimingSlices]') AND type in (N'U'))
	DROP TABLE [TimingSlices]

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[TimeSlices]') AND type in (N'U'))
	DROP TABLE [TimeSlices]

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[TextPropertyValues]') AND type in (N'U'))
	DROP TABLE [TextPropertyValues]

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[TextProperties]') AND type in (N'U'))
	DROP TABLE [TextProperties]

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[People]') AND type in (N'U'))
	DROP TABLE [People]

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[ListValues]') AND type in (N'U'))
	DROP TABLE [ListValues]

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[ListValuePropertyValues]') AND type in (N'U'))
	DROP TABLE [ListValuePropertyValues]

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[ListValueProperties]') AND type in (N'U'))
	DROP TABLE [ListValueProperties]

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[Assignments]') AND type in (N'U'))
	DROP TABLE [Assignments]

CREATE TABLE [Assignments] ([Id] [uniqueidentifier] NOT NULL, [WorkItemId] [uniqueidentifier] NOT NULL, [PersonId] [uniqueidentifier] NOT NULL, [Complete] [datetime2](7) NULL, [Due] [datetime2](7) NULL, [Estimate] [bigint] NULL, CONSTRAINT [PK_Assignments] PRIMARY KEY NONCLUSTERED ([Id] ASC))

CREATE TABLE [ListValueProperties] ([Id] [uniqueidentifier] NOT NULL, [Name] [nvarchar](max) NOT NULL, [Notes] [nvarchar](max) NULL, CONSTRAINT [PK_ListValueProperties] PRIMARY KEY NONCLUSTERED ([Id] ASC))

CREATE TABLE [ListValuePropertyValues] ([WorkItemId] [uniqueidentifier] NOT NULL, [ListValuePropertyId] [uniqueidentifier] NOT NULL, [ListValueId] [uniqueidentifier] NOT NULL, CONSTRAINT [PK_ListValuePropertyValues] PRIMARY KEY NONCLUSTERED ([WorkItemId] ASC, [ListValuePropertyId] ASC))

CREATE TABLE [ListValues] ([Id] [uniqueidentifier] NOT NULL, [ListValuePropertyId] [uniqueidentifier] NOT NULL, [Name] [nvarchar](max) NOT NULL, CONSTRAINT [PK_ListValues] PRIMARY KEY NONCLUSTERED ([Id] ASC))

CREATE TABLE [People] ([Id] [uniqueidentifier] NOT NULL, [Name] [nvarchar](max) NOT NULL, [WentAway] [datetime2](7) NULL, CONSTRAINT [PK_People] PRIMARY KEY NONCLUSTERED ([Id] ASC))

CREATE TABLE [TextProperties] ([Id] [uniqueidentifier] NOT NULL, [Name] [nvarchar](max) NOT NULL, [Notes] [nvarchar](max) NULL, CONSTRAINT [PK_TextProperties] PRIMARY KEY NONCLUSTERED ([Id] ASC))

CREATE TABLE [TextPropertyValues] ([WorkItemId] [uniqueidentifier] NOT NULL, [TextPropertyId] [uniqueidentifier] NOT NULL, [Text] [nvarchar](max) NOT NULL, CONSTRAINT [PK_TextPropertyValues] PRIMARY KEY NONCLUSTERED ([WorkItemId] ASC, [TextPropertyId] ASC))

CREATE TABLE [TimeSlices] ([Id] [uniqueidentifier] NOT NULL, [WorkItemId] [uniqueidentifier] NOT NULL, [PersonId] [uniqueidentifier] NOT NULL, [Start] [datetime2](7) NOT NULL, [End] [datetime2](7) NOT NULL, [Notes] [nvarchar](max) NULL, CONSTRAINT [PK_TimeSlices] PRIMARY KEY NONCLUSTERED ([Id] ASC))

CREATE TABLE [TimingSlices] ([Id] [uniqueidentifier] NOT NULL, CONSTRAINT [PK_TimingSlices] PRIMARY KEY NONCLUSTERED ([Id] ASC))

CREATE TABLE [WorkItems] ([Id] [uniqueidentifier] NOT NULL, [Name] [nvarchar](max) NOT NULL, [Notes] [nvarchar](max) NULL, CONSTRAINT [PK_WorkItems] PRIMARY KEY NONCLUSTERED ([Id] ASC))

ALTER TABLE [Assignments] WITH CHECK ADD CONSTRAINT [FK_Assignments_People] FOREIGN KEY ([PersonId]) REFERENCES [People] ([Id])
ALTER TABLE [Assignments] CHECK CONSTRAINT [FK_Assignments_People]

ALTER TABLE [Assignments] WITH CHECK ADD CONSTRAINT [FK_Assignments_WorkItems] FOREIGN KEY ([WorkItemId]) REFERENCES [WorkItems] ([Id])
ALTER TABLE [Assignments] CHECK CONSTRAINT [FK_Assignments_WorkItems]

ALTER TABLE [ListValuePropertyValues] WITH CHECK ADD CONSTRAINT [FK_ListValuePropertyValues_ListValueProperties] FOREIGN KEY ([ListValuePropertyId]) REFERENCES [ListValueProperties] ([Id])
ALTER TABLE [ListValuePropertyValues] CHECK CONSTRAINT [FK_ListValuePropertyValues_ListValueProperties]

ALTER TABLE [ListValuePropertyValues] WITH CHECK ADD CONSTRAINT [FK_ListValuePropertyValues_ListValues] FOREIGN KEY ([ListValueId]) REFERENCES [ListValues] ([Id])
ALTER TABLE [ListValuePropertyValues] CHECK CONSTRAINT [FK_ListValuePropertyValues_ListValues]

ALTER TABLE [ListValuePropertyValues] WITH CHECK ADD CONSTRAINT [FK_ListValuePropertyValues_WorkItems] FOREIGN KEY ([WorkItemId]) REFERENCES [WorkItems] ([Id])
ALTER TABLE [ListValuePropertyValues] CHECK CONSTRAINT [FK_ListValuePropertyValues_WorkItems]

ALTER TABLE [ListValues] WITH CHECK ADD CONSTRAINT [FK_ListValues_ListValueProperties] FOREIGN KEY ([ListValuePropertyId]) REFERENCES [ListValueProperties] ([Id])
ALTER TABLE [ListValues] CHECK CONSTRAINT [FK_ListValues_ListValueProperties]

ALTER TABLE [TextPropertyValues] WITH CHECK ADD CONSTRAINT [FK_TextPropertyValues_TextProperties] FOREIGN KEY ([TextPropertyId]) REFERENCES [TextProperties] ([Id])
ALTER TABLE [TextPropertyValues] CHECK CONSTRAINT [FK_TextPropertyValues_TextProperties]

ALTER TABLE [TextPropertyValues] WITH CHECK ADD CONSTRAINT [FK_TextPropertyValues_WorkItems] FOREIGN KEY ([WorkItemId]) REFERENCES [WorkItems] ([Id])
ALTER TABLE [TextPropertyValues] CHECK CONSTRAINT [FK_TextPropertyValues_WorkItems]

ALTER TABLE [TimeSlices] WITH CHECK ADD CONSTRAINT [FK_TimeSlices_People] FOREIGN KEY ([PersonId]) REFERENCES [People] ([Id])
ALTER TABLE [TimeSlices] CHECK CONSTRAINT [FK_TimeSlices_People]

ALTER TABLE [TimeSlices] WITH CHECK ADD CONSTRAINT [FK_TimeSlices_WorkItems] FOREIGN KEY ([WorkItemId]) REFERENCES [WorkItems] ([Id])
ALTER TABLE [TimeSlices] CHECK CONSTRAINT [FK_TimeSlices_WorkItems]

ALTER TABLE [TimingSlices] WITH CHECK ADD CONSTRAINT [FK_TimingSlices_TimeSlices] FOREIGN KEY ([Id]) REFERENCES [TimeSlices] ([Id])
ALTER TABLE [TimingSlices] CHECK CONSTRAINT [FK_TimingSlices_TimeSlices]