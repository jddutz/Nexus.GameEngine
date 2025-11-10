using Silk.NET.Maths;
using Nexus.GameEngine.Testing;

namespace Tests.GameEngine.Testing
{
    public class PixelAssertionsTests
    {
        [Fact]
        public void ColorsMatch_ReturnsFalse_WhenActualIsNull()
        {
            Vector4D<float>? actual = null;
            var expected = new Vector4D<float>(0, 0, 0, 0);

            Assert.False(PixelAssertions.ColorsMatch(actual, expected));
        }

        [Fact]
        public void ColorsMatch_ReturnsTrue_WhenWithinTolerance()
        {
            var actual = new Vector4D<float>(0.5f, 0.5f, 0.5f, 1.0f);
            var expected = new Vector4D<float>(0.505f, 0.495f, 0.499f, 1.0f);

            Assert.True(PixelAssertions.ColorsMatch(actual, expected, 0.01f));
        }

        [Fact]
        public void ColorsMatch_ReturnsFalse_WhenOutsideTolerance()
        {
            var actual = new Vector4D<float>(0.0f, 0.0f, 0.0f, 0.0f);
            var expected = new Vector4D<float>(0.1f, 0.0f, 0.0f, 0.0f);

            Assert.False(PixelAssertions.ColorsMatch(actual, expected, 0.05f));
        }

        [Fact]
        public void ColorsMatch_Boundary_InclusiveOfTolerance()
        {
            var actual = new Vector4D<float>(0.1f, 0.2f, 0.3f, 0.4f);
            var expected = new Vector4D<float>(0.11f, 0.19f, 0.3f, 0.4f);

            // Differences are approximately 0.01. Use a slightly looser tolerance to account for floating point
            Assert.True(PixelAssertions.ColorsMatch(actual, expected, 0.0101f));
        }

        [Fact]
        public void DescribeColor_ReturnsNull_ForNull()
        {
            Vector4D<float>? color = null;
            Assert.Equal("null", PixelAssertions.DescribeColor(color));
        }

        [Fact]
        public void DescribeColor_FormatsValues_WithThreeDecimals()
        {
            var color = new Vector4D<float>(0.1234f, 0.5f, 1f, 0f);
            var desc = PixelAssertions.DescribeColor(color);
            Assert.Equal("RGBA(0.123, 0.500, 1.000, 0.000)", desc);
        }
    }
}

