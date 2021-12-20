//HintName: Test.ServiceProvider.generated.cs
#nullable enable

namespace Test
{
    public partial class ServiceProvider : System.IServiceProvider, Microsoft.Extensions.DependencyInjection.IServiceScopeFactory,  Microsoft.Extensions.DependencyInjection.IServiceProviderIsService
    {
        private readonly System.Collections.Concurrent.ConcurrentBag<IAsyncDisposable> _asyncDisposables = new();
        private readonly System.Collections.Concurrent.ConcurrentBag<IDisposable> _disposables = new();
        private readonly System.Collections.Generic.Dictionary<Type, Func<object>> _factory = new();
        private readonly System.Lazy<Test.Service> _Test_Service;
        
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
            _Test_Service = new System.Lazy<Test.Service>(CreateTest_Service);
            _factory[typeof(Test.Service)] = GetTest_Service;
        }
        public object? GetService(Type serviceType)
        {
            return _factory.TryGetValue(serviceType, out var func) ? func() : null;
        }
        public bool IsService(Type serviceType)
        {
            return _factory.ContainsKey(serviceType);
        }
        private Test.Service CreateTest_Service()
        {
            return AddDisposable(new Test.Service());
        }
        private Test.Service GetTest_Service()
        {
            return _Test_Service.Value;
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
            private Test.Service CreateTest_Service()
            {
                return AddDisposable(new Test.Service());
            }
            private Test.Service GetTest_Service()
            {
                return _root.GetTest_Service();
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
