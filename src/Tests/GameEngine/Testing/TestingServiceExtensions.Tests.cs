using Microsoft.Extensions.DependencyInjection;
using Nexus.GameEngine.Testing;

namespace Tests.GameEngine.Testing
{
    public class TestingServiceExtensionsTests
    {
        [Fact]
        public void AddPixelSampling_RegistersTransient_IPixelSampler()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddPixelSampling();

            // Assert
            var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IPixelSampler));
            Assert.NotNull(descriptor);
            Assert.Equal(typeof(VulkanPixelSampler), descriptor!.ImplementationType);
            Assert.Equal(ServiceLifetime.Transient, descriptor.Lifetime);
        }

        [Fact]
        public void AddPixelSampling_ReturnsSameCollection_ForChaining()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            var returned = services.AddPixelSampling();

            // Assert
            Assert.Same(services, returned);
        }
    }
}
