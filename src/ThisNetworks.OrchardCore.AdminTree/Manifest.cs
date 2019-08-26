using OrchardCore.Modules.Manifest;

[assembly: Module(
    Name = "ThisNetWorks.OrchardCore.AdminTree",
    Author = "ThisNetWorks",
    Website = "https://github.com/ThisNetWorks/ThisNetWorks.OrchardCore.AdminTree",
    Version = "2.0.0",
    Description = "ThisNetWorks Admin Tree module displays content items based on a url tree structure",
    Dependencies = new [] { "OrchardCore.ContentTypes" },
    Category = "Content Management"
)]
