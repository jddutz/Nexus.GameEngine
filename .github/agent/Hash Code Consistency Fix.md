# Critical Fix: Hash Code Consistency in DefaultBatchStrategy

## Problem Identified

The `GetHashCode(RenderState state)` and `GetHashCode(GL gl)` methods were producing **inconsistent hash codes** for equivalent states, which would break the batching system. The renderer uses:

1. `GetHashCode(GL gl)` - Once per frame to get the current GL state hash
2. `GetHashCode(RenderState state)` - For each render state to compare with current state

If these don't match for equivalent states, batch detection fails completely.

## Root Cause

### Original Implementation Issues:

1. **Different Texture Sampling**:

   - `GetHashCode(GL gl)`: Only sampled first 4 texture units
   - `GetHashCode(RenderState state)`: Included ALL bound textures (16 units)

2. **Different Hash Construction**:

   - Both methods constructed `HashCode` objects separately
   - No guarantee of identical hashing logic

3. **Potential Null Handling Differences**:
   - Risk of inconsistent null-to-uint conversion logic

## Solution Implemented

### 1. Shared Hash Computation Method ✅

Created `ComputeStateHash()` private method that both public methods now use:

```csharp
private static int ComputeStateHash(uint? framebuffer, uint? shaderProgram, uint? vertexArray, uint?[] boundTextures)
{
    var hash = new HashCode();
    hash.Add(framebuffer);
    hash.Add(shaderProgram);
    hash.Add(vertexArray);

    foreach (var texture in boundTextures)
    {
        hash.Add(texture);
    }

    return hash.ToHashCode();
}
```

### 2. Consistent GL State Querying ✅

Updated `GetHashCode(GL gl)` to:

- Query ALL 16 texture units (matching `RenderState.BoundTextures` array size)
- Use identical null handling logic (`0 == null`)
- Restore original active texture unit properly
- Call the shared `ComputeStateHash()` method

### 3. Simplified RenderState Hashing ✅

Updated `GetHashCode(RenderState state)` to:

- Simply pass state properties to `ComputeStateHash()`
- No local hash construction logic
- Guaranteed identical behavior

## Critical Benefits

### ✅ **Hash Consistency Guaranteed**

Both methods now produce identical hash codes for equivalent states because they:

- Use the same `ComputeStateHash()` implementation
- Sample the same number of texture units (16)
- Apply identical null handling logic

### ✅ **Batch Detection Fixed**

The renderer's batching logic now works correctly:

```csharp
int currentHashCode = BatchStrategy.GetHashCode(GL);  // Initial GL state
foreach (var targetState in sorted)
{
    int targetHashCode = BatchStrategy.GetHashCode(targetState);  // Target state
    if (currentHashCode != targetHashCode)
    {
        // Apply state changes - this now triggers correctly!
        ApplyRenderState(targetState, currentState);
        currentHashCode = targetHashCode;
    }
}
```

### ✅ **Performance Maintained**

- Still only queries GL state once per frame
- Efficient hash comparison for batch boundaries
- Proper texture unit restoration

## Test Verification

The fix ensures that:

1. If GL is in state X, `GetHashCode(GL)` returns hash H
2. If RenderState represents state X, `GetHashCode(RenderState)` returns hash H
3. Therefore: `GetHashCode(GL) == GetHashCode(RenderState)` when states match

This is **critical** for the batching system to function properly.

## Risk Mitigation

### Before Fix:

- Hash mismatches could cause state changes on every render state
- Batching would be completely ineffective
- Performance degradation due to excessive GL state switches

### After Fix:

- Hash codes are mathematically guaranteed to match for equivalent states
- Batching system works as designed
- Optimal performance with minimal state changes
