//HintName: Test.ServiceProvider.generated.cs
#nullable enable

namespace Test
{
    public partial class ServiceProvider : System.IServiceProvider, Microsoft.Extensions.DependencyInjection.IServiceScopeFactory,  Microsoft.Extensions.DependencyInjection.IServiceProviderIsService
    {
        private readonly System.Collections.Concurrent.ConcurrentBag<IAsyncDisposable> _asyncDisposables = new();
        private readonly System.Collections.Concurrent.ConcurrentBag<IDisposable> _disposables = new();
        private readonly System.Collections.Generic.Dictionary<Type, Func<object>> _factory = new();
        private readonly System.Lazy<Test.IService1> _Test_IService1;
        private readonly System.Lazy<Test.Service2> _Test_Service2;
        
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
            _Test_IService1 = new System.Lazy<Test.IService1>(CreateTest_IService1);
            _factory[typeof(Test.IService1)] = GetTest_IService1;
            _Test_Service2 = new System.Lazy<Test.Service2>(CreateTest_Service2);
            _factory[typeof(Test.Service2)] = GetTest_Service2;
        }
        public object? GetService(Type serviceType)
        {
            return _factory.TryGetValue(serviceType, out var func) ? func() : null;
        }
        public bool IsService(Type serviceType)
        {
            return _factory.ContainsKey(serviceType);
        }
        private Test.IService1 CreateTest_IService1()
        {
            return new Test.Service1(
                GetTest_Service2()
            );
        }
        private Test.IService1 GetTest_IService1()
        {
            return _Test_IService1.Value;
        }
        private Test.Service2 CreateTest_Service2()
        {
            return new Test.Service2();
        }
        private Test.Service2 GetTest_Service2()
        {
            return _Test_Service2.Value;
        }
        protected class Scope : System.IServiceProvider, System.IAsyncDisposable, Microsoft.Extensions.DependencyInjection.IServiceScope
        {
            private readonly Test.ServiceProvider _root;
            private readonly System.Collections.Concurrent.ConcurrentBag<IAsyncDisposable> _asyncDisposables = new();
            private readonly System.Collections.Concurrent.ConcurrentBag<IDisposable> _disposables = new();
            private readonly System.Collections.Generic.Dictionary<Type, Func<object>> _factory = new();
            private readonly System.Lazy<Test.Service2> _Test_Service2;
            public Scope(Test.ServiceProvider root)
            {
                _root = root;
                _Test_Service2 = new System.Lazy<Test.Service2>(CreateTest_Service2);
                _factory[typeof(Test.Service2)] = GetTest_Service2;
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
            private Test.IService1 CreateTest_IService1()
            {
                return new Test.Service1(
                    GetTest_Service2()
                );
            }
            private Test.IService1 GetTest_IService1()
            {
                return _root.GetTest_IService1();
            }
            private Test.Service2 CreateTest_Service2()
            {
                return new Test.Service2();
            }
            private Test.Service2 GetTest_Service2()
            {
                return _Test_Service2.Value;
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
