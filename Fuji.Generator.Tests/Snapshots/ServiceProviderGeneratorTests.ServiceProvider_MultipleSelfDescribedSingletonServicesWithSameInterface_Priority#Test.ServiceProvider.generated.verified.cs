//HintName: Test.ServiceProvider.generated.cs
#nullable enable

namespace Test
{
    public partial class ServiceProvider : System.IServiceProvider, Microsoft.Extensions.DependencyInjection.IServiceScopeFactory,  Microsoft.Extensions.DependencyInjection.IServiceProviderIsService
    {
        private readonly System.Collections.Concurrent.ConcurrentBag<IAsyncDisposable> _asyncDisposables = new();
        private readonly System.Collections.Concurrent.ConcurrentBag<IDisposable> _disposables = new();
        private readonly System.Collections.Generic.Dictionary<Type, Func<object>> _factory = new();
        private readonly System.Lazy<Test.IService> _Test_IService;
        private readonly System.Lazy<Test.IService> _Test_Service1;
        private readonly System.Lazy<Test.IService> _Test_Service3;
        
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
            _Test_IService = new System.Lazy<Test.IService>(CreateTest_IService);
            _factory[typeof(Test.IService)] = GetTest_IService;
            _Test_Service1 = new System.Lazy<Test.IService>(CreateTest_Service1);
            _Test_Service3 = new System.Lazy<Test.IService>(CreateTest_Service3);
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
        private Test.IService CreateTest_IService()
        {
            return new Test.Service2();
        }
        private Test.IService GetTest_IService()
        {
            return _Test_IService.Value;
        }
        private Test.IService CreateTest_Service1()
        {
            return new Test.Service1();
        }
        private Test.IService GetTest_Service1()
        {
            return _Test_Service1.Value;
        }
        private Test.IService CreateTest_Service3()
        {
            return new Test.Service3();
        }
        private Test.IService GetTest_Service3()
        {
            return _Test_Service3.Value;
        }
        private System.Collections.Generic.IEnumerable<Test.IService> GetEnumerableTest_IService()
        {
            return new Test.IService[]{GetTest_IService(), GetTest_Service1(), GetTest_Service3()};
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
            private Test.IService CreateTest_IService()
            {
                return new Test.Service2();
            }
            private Test.IService GetTest_IService()
            {
                return _root.GetTest_IService();
            }
            private Test.IService CreateTest_Service1()
            {
                return new Test.Service1();
            }
            private Test.IService GetTest_Service1()
            {
                return _root.GetTest_Service1();
            }
            private Test.IService CreateTest_Service3()
            {
                return new Test.Service3();
            }
            private Test.IService GetTest_Service3()
            {
                return _root.GetTest_Service3();
            }
            private System.Collections.Generic.IEnumerable<Test.IService> GetEnumerableTest_IService()
            {
                return new Test.IService[]{GetTest_IService(), GetTest_Service1(), GetTest_Service3()};
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
