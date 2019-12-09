# ThisNetWorks.OrchardCore.AdminTree
An Admin Tree Module for Orchard Core

## Getting Started

Currently this module is intended to be build using the Orchard Core `dev/deanmarcussen/tags` branch.

It requires an adapted [Orchard Core Admin Theme](https://github.com/ThisNetWorks/ThisNetWorks.OrchardCore.Themes)

As it progresses, they may be released to NuGet or MyGet, however this is likely to be after the release of Orchard Core 1.0

To use the module place the root of this repository in the `src/OrchardCore/OrchardCore.Modules/ThisNetWorks.OrchardCore.AdminTree` folder.

To use with the MyGet packages the `ProjectReference` sections in the `.csproj` files need to be changed to the equivalent `PackageReference` entries.

These are currently commented out.

To enable the module in Orchard Core select `Configuration -> Features -> ThisNetWorks AdminTree`

## Url Tree Menu

This presents all routable content items in tree menu, and is split on the Url Segements.

e.g.
```
home
│   support   
│
└───docs
│   └───getting-started
│   |   │   intro
│   |   │   setup
│   |   │   ...
│   │
│   └───more-features 
│       │   intro
│       │   setup
│       │   ...
│   
└───news
    │   welcome
    │   new-features
```

## Taxonomy Terms Menu

This presents a taxonomy and it's associated terms in a tree menu.

The primary purpose of this menu is intended to provide a way to manage complex taxonomies, in combination with
a navigation system that makes manages those terms, and content items easier.

It also provides a custom taxonomy field editor, called `Contained`, and a custom part called `TermContainedPart`

This allows the taxonomy tree to list items belonging to a taxonomy term, whether branch or leaf.

To get started 
- Create a term content type that will be used by the taxonomy.
- Add the `TermContainedPart` to the term content type.
- Create a taxonomy.
- To content types that this taxonomy can contain add a taxonomy field.
- Set the editor for this field to `Contained`
- Create terms for the taxonomy.
- When creating the term select what types of content that term may contain.

Note before the term can contain content types, those content types must 
- Have a taxonomy field set to this taxonomy
- Have the taxonomy field editor set to `Contained`

There is a `Contained` display mode which can be used to list branches or leafs, via AutoRoute.

There is a sample recipe which sets up a taxonomy with some default terms, editors, display modes and sample content items.

## Taxonomy Contents Menu

Still in early development, and currently disabled.

Intended to provide similar features to the `Taxonomy Terms Menu`, but may include more content items displayed in the menu tree.

