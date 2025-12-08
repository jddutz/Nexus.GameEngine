using Nexus.GameEngine.Components;
using System.Numerics;

namespace Nexus.GameEngine.Components;

public partial class RectTransform : Component, IRectTransform
{
    [ComponentProperty]
    [TemplateProperty]
    protected Vector2D<float> _position;

    [ComponentProperty]
    [TemplateProperty]
    protected Vector2D<float> _size;

    [ComponentProperty]
    [TemplateProperty]
    protected float _rotation;

    [ComponentProperty]
    [TemplateProperty]
    protected Vector2D<float> _scale = Vector2D<float>.One;

    [ComponentProperty]
    [TemplateProperty]
    protected Vector2D<float> _pivot = Vector2D<float>.Zero; // Top-Left default

    private Matrix4X4<float> _localMatrix = Matrix4X4<float>.Identity;
    private Matrix4X4<float> _worldMatrix = Matrix4X4<float>.Identity;
    private bool _isWorldMatrixValid = false;
    private Rectangle<int> _bounds;

    public Matrix4X4<float> LocalMatrix
    {
        get
        {
            if (!_isWorldMatrixValid) UpdateTransforms();
            return _localMatrix;
        }
    }

    public Matrix4X4<float> WorldMatrix
    {
        get
        {
            if (!_isWorldMatrixValid) UpdateTransforms();
            return _worldMatrix;
        }
    }

    public Rectangle<int> Bounds
    {
        get
        {
            if (!_isWorldMatrixValid)
            {
                UpdateTransforms();
            }
            return _bounds;
        }
    }

    private void InvalidateWorldMatrix()
    {
        _isWorldMatrixValid = false;
        
        // Invalidate all children recursively
        foreach (var child in Children)
        {
            if (child is RectTransform rectTransform)
            {
                rectTransform.InvalidateWorldMatrix();
            }
        }
    }

    private void UpdateTransforms()
    {
        // Calculate Local Matrix with pivot support
        // The pivot determines which point of the element is positioned at _position
        // For pivot (0,0) = top-left, (0.5,0.5) = center, (1,1) = bottom-right
        
        // Calculate pivot offset in element's local space
        var pivotOffset = _pivot * _size;
        
        // Transform order (right to left): 
        // 1. Translate by -pivot (move pivot to origin)
        // 2. Scale and Rotate around origin
        // 3. Translate by +pivot (move pivot back)
        // 4. Translate by position (move to final location)
        var scaleMatrix = Matrix4X4.CreateScale(new Vector3D<float>(_scale.X, _scale.Y, 1.0f));
        var rotationMatrix = Matrix4X4.CreateRotationZ(_rotation);
        var pivotTranslate = Matrix4X4.CreateTranslation(new Vector3D<float>(-pivotOffset.X, -pivotOffset.Y, 0.0f));
        var unpivotTranslate = Matrix4X4.CreateTranslation(new Vector3D<float>(pivotOffset.X, pivotOffset.Y, 0.0f));
        var positionTranslate = Matrix4X4.CreateTranslation(new Vector3D<float>(_position.X, _position.Y, 0.0f));

        // Applied right to left: position * unpivot * rotate * scale * pivot
        _localMatrix = positionTranslate * unpivotTranslate * rotationMatrix * scaleMatrix * pivotTranslate;

        // Calculate World Matrix        
        var parent = GetParent<IRectTransform>();
        if (parent == null)
        {
            _worldMatrix = _localMatrix;
        }
        else
        {
            _worldMatrix = _localMatrix * parent.WorldMatrix;
        }

        // Calculate bounds at the same time
        UpdateBounds();
        
        _isWorldMatrixValid = true;
    }

    private void UpdateBounds()
    {
        // Calculate corners relative to Pivot
        // TL: -Pivot * Size
        // BR: (1 - Pivot) * Size
        
        var tl = -_pivot * _size;
        var br = (Vector2D<float>.One - _pivot) * _size;
        var tr = new Vector2D<float>(br.X, tl.Y);
        var bl = new Vector2D<float>(tl.X, br.Y);

        // Transform corners to World Space
        var matrix = _worldMatrix;  // Use already computed world matrix
        
        var vTL = Vector3D.Transform(new Vector3D<float>(tl.X, tl.Y, 0), matrix);
        var vTR = Vector3D.Transform(new Vector3D<float>(tr.X, tr.Y, 0), matrix);
        var vBL = Vector3D.Transform(new Vector3D<float>(bl.X, bl.Y, 0), matrix);
        var vBR = Vector3D.Transform(new Vector3D<float>(br.X, br.Y, 0), matrix);

        // Find AABB
        var minX = Math.Min(Math.Min(vTL.X, vTR.X), Math.Min(vBL.X, vBR.X));
        var maxX = Math.Max(Math.Max(vTL.X, vTR.X), Math.Max(vBL.X, vBR.X));
        var minY = Math.Min(Math.Min(vTL.Y, vTR.Y), Math.Min(vBL.Y, vBR.Y));
        var maxY = Math.Max(Math.Max(vTL.Y, vTR.Y), Math.Max(vBL.Y, vBR.Y));

        _bounds = new Rectangle<int>((int)minX, (int)minY, (int)(maxX - minX), (int)(maxY - minY));
    }

    public void SetPosition(Vector2D<float> position)
    {
        if (_position != position)
        {
            _position = position;
            InvalidateWorldMatrix();
        }
    }

    public void SetSize(Vector2D<float> size)
    {
        if (_size != size)
        {
            _size = size;
            InvalidateWorldMatrix();
        }
    }

    public void SetRotation(float rotation)
    {
        if (_rotation != rotation)
        {
            _rotation = rotation;
            InvalidateWorldMatrix();
        }
    }

    public void SetScale(Vector2D<float> scale)
    {
        if (_scale != scale)
        {
            _scale = scale;
            InvalidateWorldMatrix();
        }
    }

    public void SetPivot(Vector2D<float> pivot)
    {
        if (_pivot != pivot)
        {
            _pivot = pivot;
            InvalidateWorldMatrix();
        }
    }
}
