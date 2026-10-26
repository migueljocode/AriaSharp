# Namespace Decision (M0)

## Decision
Console project namespace: **AriaSharp.App**

## Rationale
- The Console csproj already sets `<RootNamespace>AriaSharp.App</RootNamespace>`.
- Brief section 71 (Target architecture): "Namespace must match RootNamespace and must not be named Console."
- Keeping `AriaSharp.App` avoids renaming the csproj RootNamespace and matches the existing project declaration.
- All Console source files currently declare `namespace AriaSharp.Console`; they must be moved to `namespace AriaSharp.App`.

## Alternatives considered
- AriaSharp.Cli: would require changing RootNamespace in the csproj and every file; no benefit over AriaSharp.App which is already declared.

## Open for owner
Section 17 decision 2 lists "AriaSharp.App or AriaSharp.Cli" as open. This record captures the chosen default; the owner can override.
