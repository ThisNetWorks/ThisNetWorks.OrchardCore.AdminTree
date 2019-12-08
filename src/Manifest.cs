using OrchardCore.Modules.Manifest;

[assembly: Module(
    Name = "ThisNetWorks Admin Tree Menus",
    Author = "ThisNetWorks",
    Website = "https://github.com/ThisNetWorks/ThisNetWorks.OrchardCore.AdminTree",
    Version = "0.0.1",
    Description = "ThisNetWorks Admin Tree module displays content items based on a url tree, or taxonomy structure.",
    Dependencies = new [] { "OrchardCore.AdminMenu", "OrchardCore.Taxonomies", "OrchardCore.AutoRoute" },
    Category = "Content Management"
)]
