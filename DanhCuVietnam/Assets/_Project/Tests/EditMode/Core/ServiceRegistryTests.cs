using System;
using System.Collections.Generic;
using DanhCu.Core.Services;
using NUnit.Framework;

namespace DanhCu.Tests.EditMode.Core
{
    public sealed class ServiceRegistryTests
    {
        private interface IFoo
        {
        }

        private sealed class Foo : IFoo
        {
        }

        [Test]
        public void RegisterAndGet_ByInterface()
        {
            var registry = new ServiceRegistry();
            var foo = new Foo();
            registry.Register<IFoo>(foo);

            Assert.That(registry.Get<IFoo>(), Is.SameAs(foo));
            Assert.That(registry.IsRegistered<Foo>(), Is.False);
        }

        [Test]
        public void Register_Duplicate_Throws()
        {
            var registry = new ServiceRegistry();
            registry.Register<IFoo>(new Foo());

            Assert.Throws<InvalidOperationException>(() => registry.Register<IFoo>(new Foo()));
        }

        [Test]
        public void Get_Missing_ThrowsWithName()
        {
            var ex = Assert.Throws<KeyNotFoundException>(() => new ServiceRegistry().Get<IFoo>());
            Assert.That(ex.Message, Does.Contain(nameof(IFoo)));
        }

        [Test]
        public void TryGet_Missing_ReturnsFalse()
        {
            Assert.That(new ServiceRegistry().TryGet(out IFoo found), Is.False);
            Assert.That(found, Is.Null);
        }

        [Test]
        public void Unregister_RemovesService()
        {
            var registry = new ServiceRegistry();
            registry.Register<IFoo>(new Foo());

            Assert.That(registry.Unregister<IFoo>(), Is.True);
            Assert.That(registry.IsRegistered<IFoo>(), Is.False);
        }
    }
}
