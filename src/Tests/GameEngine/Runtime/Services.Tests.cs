using System;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Nexus.GameEngine.Runtime;

namespace Tests.GameEngine.Runtime
{
    public class ServicesTests
    {
        private interface ITestMarker { }

        private class TestA : ITestMarker { }
        private class TestB : ITestMarker { }

        [Fact]
        public void AddDiscoveredServices_RegistersConcreteTypesFromAssembly()
        {
            var services = new ServiceCollection();

            // This should discover TestA and TestB declared in this test assembly
            services.AddDiscoveredServices<ITestMarker>();

            var provider = services.BuildServiceProvider();

            // The extension registers the concrete types as themselves (AddTransient(type)),
            // so resolve by concrete type.
            var a = provider.GetService<TestA>();
            var b = provider.GetService<TestB>();

            Assert.NotNull(a);
            Assert.NotNull(b);
            Assert.IsType<TestA>(a);
            Assert.IsType<TestB>(b);
        }
    }
}
