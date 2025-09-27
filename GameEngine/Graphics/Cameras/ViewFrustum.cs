using Silk.NET.Maths;

namespace Nexus.GameEngine.Graphics.Cameras;

/// <summary>
/// Represents a camera's view frustum for culling calculations.
/// </summary>
public struct ViewFrustum
{
    /// <summary>
    /// The six planes that define the view frustum.
    /// </summary>
    public Plane<float>[] Planes { get; set; }

    /// <summary>
    /// Check if a 3D bounding box intersects with this view frustum.
    /// </summary>
    /// <param name="boundingBox">The 3D bounding box to test</param>
    /// <returns>True if the bounding box is at least partially inside the frustum</returns>
    public bool Intersects(Box3D<float> boundingBox)
    {
        // Test each corner of the bounding box against all frustum planes
        var corners = new Vector3D<float>[]
        {
            new(boundingBox.Min.X, boundingBox.Min.Y, boundingBox.Min.Z),
            new(boundingBox.Max.X, boundingBox.Min.Y, boundingBox.Min.Z),
            new(boundingBox.Min.X, boundingBox.Max.Y, boundingBox.Min.Z),
            new(boundingBox.Max.X, boundingBox.Max.Y, boundingBox.Min.Z),
            new(boundingBox.Min.X, boundingBox.Min.Y, boundingBox.Max.Z),
            new(boundingBox.Max.X, boundingBox.Min.Y, boundingBox.Max.Z),
            new(boundingBox.Min.X, boundingBox.Max.Y, boundingBox.Max.Z),
            new(boundingBox.Max.X, boundingBox.Max.Y, boundingBox.Max.Z)
        };

        // For each plane, check if all corners are on the negative side
        foreach (var plane in Planes)
        {
            bool allOutside = true;
            foreach (var corner in corners)
            {
                // Calculate distance from point to plane using the standard plane equation
                // Distance = dot(normal, point) + distance
                var distance = Vector3D.Dot(plane.Normal, corner) + plane.Distance;
                if (distance >= 0)
                {
                    allOutside = false;
                    break;
                }
            }

            // If all corners are outside this plane, the box is completely outside the frustum
            if (allOutside)
                return false;
        }

        return true; // Box intersects or is inside the frustum
    }

    /// <summary>
    /// Check if a 2D bounding box intersects with this view frustum (projected to 2D).
    /// </summary>
    /// <param name="boundingBox">The 2D bounding box to test</param>
    /// <returns>True if the bounding box is at least partially inside the frustum</returns>
    public bool Intersects(Box2D<float> boundingBox)
    {
        // Convert 2D box to 3D box with minimal depth
        var box3D = new Box3D<float>(
            new Vector3D<float>(boundingBox.Min.X, boundingBox.Min.Y, -0.1f),
            new Vector3D<float>(boundingBox.Max.X, boundingBox.Max.Y, 0.1f)
        );

        return Intersects(box3D);
    }
}