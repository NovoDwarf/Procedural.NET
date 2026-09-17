# Procedural.NET

> [!WARNING]
> This project was created for educational purposes and is still under development. It may contain bugs, incomplete features, and architectural flaws. The code is not production-ready and should be used with caution.

> [!WARNING]
> Work in progress — currently non-functional.
> Procedural.NET was extracted from another project and requires a substantial redesign of its architecture and internal structure before it can become a working framework.

A modular .NET framework for procedural generation and node-based computational workflows.

Procedural.NET is currently focused on procedural terrain generation, with the long-term goal of becoming a general-purpose procedural toolkit for creating, processing, and combining procedural data through graph-based workflows.


## Projects

| Project | Description |
|---|---|
| `Procedural.NET.Core` | Core graph infrastructure, including graph documents, nodes, metadata, converters, and shared abstractions. |
| `Procedural.NET.Storage` | Storage and management of graph nodes and related data. |
| `Procedural.NET.Nodes` | Node implementations for noise generation, blending, display, logic, filters, and other procedural operations. |
| `Procedural.NET.Designer` | Avalonia-based visual graph editor for creating and editing procedural graphs. |

## Architecture

Procedural.NET is built around a graph-based architecture where nodes represent procedural operations and connections define the data flow between them.

```text
Procedural.NET
├── Procedural.NET.Core
│   └── Graph documents, nodes, metadata, converters
├── Procedural.NET.Storage
│   └── Node storage and management
├── Procedural.NET.Nodes
│   └── Noise, blending, display, logic, filters, ...
└── Procedural.NET.Designer
    └── Avalonia graph editor
```

## Roadmap

- Redesign the current architecture and project structure.
- Implement custom procedural nodes using [MathForge](https://github.com/NovoDwarf/MathForge).
- Introduce declarative node and graph definitions through [Grekov](https://github.com/NovoDwarf/Grekov).
- Develop GPU-accelerated node implementations using Slang shaders (for Godot → [NovoDwarf.Godot.Slang](https://github.com/NovoDwarf/NovoDwarf.Projects)).
- Integrate GPU execution into the procedural processing pipeline.
- Expand beyond terrain generation into a general-purpose procedural toolkit.

## License

[**Procedural.NET**]() is licensed under the [**MIT License**](), see [LICENSE](LICENSE) for more information.