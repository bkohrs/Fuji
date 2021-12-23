//HintName: Test.ServiceProvider.generated.cs
#nullable enable

namespace Test
{
    public partial class ServiceProvider : System.IServiceProvider, Microsoft.Extensions.DependencyInjection.IServiceScopeFactory,  Microsoft.Extensions.DependencyInjection.IServiceProviderIsService
    {
        private readonly System.Collections.Concurrent.ConcurrentBag<IAsyncDisposable> _asyncDisposables = new();
        private readonly System.Collections.Concurrent.ConcurrentBag<IDisposable> _disposables = new();
        private readonly System.Collections.Generic.Dictionary<Type, Func<object>> _factory = new();
        private readonly System.Lazy<Test.IService> _Test_Service4;
        private readonly System.Lazy<Test.IService> _Test_Service5;
        
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
            _factory[typeof(Test.IService)] = GetTest_IService;
            _Test_Service4 = new System.Lazy<Test.IService>(CreateTest_Service4);
            _Test_Service5 = new System.Lazy<Test.IService>(CreateTest_Service5);
        }
        public object? GetService(Type serviceType)
        {
            return _factory.TryGetValue(serviceType, out var func) ? func() : null;
        }
        public bool IsService(Type serviceType)
        {
            return _factory.ContainsKey(serviceType);
        }
        private Test.IService GetTest_IService()
        {
            return new Test.Service2();
        }
        private Test.IService GetTest_Service1()
        {
            return new Test.Service1();
        }
        private Test.IService GetTest_Service3()
        {
            return new Test.Service3();
        }
        private Test.IService CreateTest_Service4()
        {
            return new Test.Service4();
        }
        private Test.IService GetTest_Service4()
        {
            return _Test_Service4.Value;
        }
        private Test.IService CreateTest_Service5()
        {
            return new Test.Service5();
        }
        private Test.IService GetTest_Service5()
        {
            return _Test_Service5.Value;
        }
        protected class Scope : System.IServiceProvider, System.IAsyncDisposable, Microsoft.Extensions.DependencyInjection.IServiceScope
        {
            private readonly Test.ServiceProvider _root;
            private readonly System.Collections.Concurrent.ConcurrentBag<IAsyncDisposable> _asyncDisposables = new();
            private readonly System.Collections.Concurrent.ConcurrentBag<IDisposable> _disposables = new();
            private readonly System.Collections.Generic.Dictionary<Type, Func<object>> _factory = new();
            private readonly System.Lazy<Test.IService> _Test_Service4;
            public Scope(Test.ServiceProvider root)
            {
                _root = root;
                _factory[typeof(Test.IService)] = GetTest_IService;
                _Test_Service4 = new System.Lazy<Test.IService>(CreateTest_Service4);
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
            private Test.IService GetTest_IService()
            {
                return new Test.Service2();
            }
            private Test.IService GetTest_Service1()
            {
                return new Test.Service1();
            }
            private Test.IService GetTest_Service3()
            {
                return new Test.Service3();
            }
            private Test.IService CreateTest_Service4()
            {
                return new Test.Service4();
            }
            private Test.IService GetTest_Service4()
            {
                return _Test_Service4.Value;
            }
            private Test.IService CreateTest_Service5()
            {
                return new Test.Service5();
            }
            private Test.IService GetTest_Service5()
            {
                return _root.GetTest_Service5();
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
