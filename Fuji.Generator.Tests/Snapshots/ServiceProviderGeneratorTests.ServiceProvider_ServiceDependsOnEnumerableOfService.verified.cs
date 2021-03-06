//HintName: Test.ServiceProvider.generated.cs
#nullable enable

namespace Test
{
    public partial class ServiceProvider : System.IServiceProvider, Microsoft.Extensions.DependencyInjection.IServiceScopeFactory,  Microsoft.Extensions.DependencyInjection.IServiceProviderIsService
    {
        private readonly System.Collections.Concurrent.ConcurrentBag<IAsyncDisposable> _asyncDisposables = new();
        private readonly System.Collections.Concurrent.ConcurrentBag<IDisposable> _disposables = new();
        private readonly System.Collections.Generic.Dictionary<Type, Func<object>> _factory = new();
        
        protected T AddAsyncDisposable<T>(T asyncDisposable) where T: System.IAsyncDisposable
        {
            _asyncDisposables.Add(asyncDisposable);
            return asyncDisposable;
        }
        protected T AddDisposable<T>(T disposable) where T: System.IDisposable
        {
            _disposables.Add(disposable);
            return disposable;
        }
        public Microsoft.Extensions.DependencyInjection.IServiceScope CreateScope()
        {
            return new Scope(this);
        }
        public async System.Threading.Tasks.ValueTask DisposeAsync()
        {
            foreach (var disposable in _disposables)
                disposable.Dispose();
            _disposables.Clear();
            foreach (var disposable in _asyncDisposables)
                await disposable.DisposeAsync().ConfigureAwait(false);
            _asyncDisposables.Clear();
        }
        public ServiceProvider()
        {
            _factory[typeof(Microsoft.Extensions.DependencyInjection.IServiceScopeFactory)] = () => this;
            _factory[typeof(Test.IDependency)] = GetTest_IDependency;
            _factory[typeof(Test.IService)] = GetTest_IService;
            _factory[typeof(System.Collections.Generic.IEnumerable<Test.IDependency>)] = GetEnumerableTest_IDependency;
            _factory[typeof(System.Collections.Generic.IEnumerable<Test.IService>)] = GetEnumerableTest_IService;
        }
        public object? GetService(Type serviceType)
        {
            return _factory.TryGetValue(serviceType, out var func) ? func() : null;
        }
        public bool IsService(Type serviceType)
        {
            return _factory.ContainsKey(serviceType);
        }
        private Test.IDependency GetTest_IDependency()
        {
            return new Test.Dependency1();
        }
        private Test.IDependency GetTest_Dependency2()
        {
            return new Test.Dependency2();
        }
        private Test.IService GetTest_IService()
        {
            return new Test.Service(
                GetEnumerableTest_IDependency()
            );
        }
        private System.Collections.Generic.IEnumerable<Test.IDependency> GetEnumerableTest_IDependency()
        {
            return new Test.IDependency[]{GetTest_IDependency(), GetTest_Dependency2()};
        }
        private System.Collections.Generic.IEnumerable<Test.IService> GetEnumerableTest_IService()
        {
            return new Test.IService[]{GetTest_IService()};
        }
        protected class Scope : System.IServiceProvider, System.IAsyncDisposable, Microsoft.Extensions.DependencyInjection.IServiceScope
        {
            private readonly Test.ServiceProvider _root;
            private readonly System.Collections.Concurrent.ConcurrentBag<IAsyncDisposable> _asyncDisposables = new();
            private readonly System.Collections.Concurrent.ConcurrentBag<IDisposable> _disposables = new();
            private readonly System.Collections.Generic.Dictionary<Type, Func<object>> _factory = new();
            public Scope(Test.ServiceProvider root)
            {
                _root = root;
                _factory[typeof(Test.IDependency)] = GetTest_IDependency;
                _factory[typeof(Test.IService)] = GetTest_IService;
                _factory[typeof(System.Collections.Generic.IEnumerable<Test.IDependency>)] = GetEnumerableTest_IDependency;
                _factory[typeof(System.Collections.Generic.IEnumerable<Test.IService>)] = GetEnumerableTest_IService;
            }
            public System.IServiceProvider ServiceProvider => this;
            protected T AddAsyncDisposable<T>(T asyncDisposable) where T: System.IAsyncDisposable
            {
                _asyncDisposables.Add(asyncDisposable);
                return asyncDisposable;
            }
            protected T AddDisposable<T>(T disposable) where T: System.IDisposable
            {
                _disposables.Add(disposable);
                return disposable;
            }
            public object? GetService(Type serviceType)
            {
                return _factory.TryGetValue(serviceType, out var func) ? func() : _root.GetService(serviceType);
            }
            private Test.IDependency GetTest_IDependency()
            {
                return new Test.Dependency1();
            }
            private Test.IDependency GetTest_Dependency2()
            {
                return new Test.Dependency2();
            }
            private Test.IService GetTest_IService()
            {
                return new Test.Service(
                    GetEnumerableTest_IDependency()
                );
            }
            private System.Collections.Generic.IEnumerable<Test.IDependency> GetEnumerableTest_IDependency()
            {
                return new Test.IDependency[]{GetTest_IDependency(), GetTest_Dependency2()};
            }
            private System.Collections.Generic.IEnumerable<Test.IService> GetEnumerableTest_IService()
            {
                return new Test.IService[]{GetTest_IService()};
            }
            public async System.Threading.Tasks.ValueTask DisposeAsync()
            {
                foreach (var disposable in _disposables)
                    disposable.Dispose();
                _disposables.Clear();
                foreach (var disposable in _asyncDisposables)
                    await disposable.DisposeAsync().ConfigureAwait(false);
                _asyncDisposables.Clear();
            }
            public void Dispose()
            {
                throw new System.NotSupportedException("Dispose() is not supported on scopes. Use DisposeAsync() instead.");
            }
        }
    }
}
