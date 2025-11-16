# API Contracts: Layout Alignment Refactor

This directory contains the contract definitions (interfaces and expected signatures) for the refactored layout components.

## Purpose

These contracts serve as:
1. **Design Documentation**: Clear specification of public API surface
2. **Implementation Guide**: Reference for implementing the refactored components
3. **Test Contracts**: Basis for unit and integration tests

## Files

- **HorizontalLayoutContract.cs**: Public API contract for HorizontalLayout component
- **VerticalLayoutContract.cs**: Public API contract for VerticalLayout component

## Notes

- These are conceptual contracts showing the intended API surface
- Actual implementations use partial classes with source-generated properties
- The `[ComponentProperty]` and `[TemplateProperty]` attributes trigger source generation for:
  - Public getter properties
  - Setter methods (e.g., `SetAlignment()`)
  - Deferred update queue management
  - PropertyChanged events
