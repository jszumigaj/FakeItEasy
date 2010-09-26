namespace FakeItEasy.IoC
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    /// <summary>
    /// A simple implementation of an IoC container.
    /// </summary>
    internal class DictionaryContainer
        : ServiceLocator
    {
        private static Logger logger = Log.GetLogger<DictionaryContainer>();

        /// <summary>
        /// The dictionary that stores the registered services.
        /// </summary>
        private Dictionary<Type, Func<DictionaryContainer, object>> registeredServices;

        /// <summary>
        /// Initializes a new instance of the <see cref="DictionaryContainer"/> class.
        /// </summary>
        public DictionaryContainer()
        {
            this.registeredServices = new Dictionary<Type, Func<DictionaryContainer, object>>();
        }

        /// <summary>
        /// Resolves an instance of the specified component type.
        /// </summary>
        /// <param name="componentType">Type of the component.</param>
        /// <returns>An instance of the component type.</returns>
        [DebuggerStepThrough]
        internal override object Resolve(Type componentType)
        {
            logger.Debug("Resolving {0}.", componentType);

            Func<DictionaryContainer, object> creator = null;

            if (!this.registeredServices.TryGetValue(componentType, out creator))
            {
                throw new KeyNotFoundException("The specified service '{0}' was not registered in the container.".FormatInvariant(componentType));
            }

            return creator.Invoke(this);
        }

        /// <summary>
        /// Registers the specified resolver.
        /// </summary>
        /// <typeparam name="T">The type of component to register.</typeparam>
        /// <param name="resolver">The resolver.</param>
        [DebuggerStepThrough]
        internal void Register<T>(Func<DictionaryContainer, T> resolver)
        {
            logger.Debug("Registering {0}.", typeof(T));

            this.registeredServices.Add(typeof(T), c => resolver.Invoke(c));
        }

        /// <summary>
        /// Registers the specified resolver as a singleton.
        /// </summary>
        /// <typeparam name="T">The type of component to register.</typeparam>
        /// <param name="resolver">The resolver.</param>
        [DebuggerStepThrough]
        internal void RegisterSingleton<T>(Func<DictionaryContainer, T> resolver)
        {
            logger.Debug("Registering {0} as singleton.", typeof(T));

            var singletonResolver = new SingletonResolver<T>(resolver);

            this.Register<T>(singletonResolver.Resolve);
        }

        private class SingletonResolver<T>
        {
            private SingletonResolverState state;

            public SingletonResolver(Func<DictionaryContainer, T> resolveFunction)
            {
                this.state = new UnresolvedState(this, resolveFunction);
            }

            public T Resolve(DictionaryContainer container)
            {
                return this.state.Resolve(container);
            }

            private abstract class SingletonResolverState
            {
                public abstract T Resolve(DictionaryContainer container);
            }

            private class UnresolvedState
                : SingletonResolverState
            {
                private Func<DictionaryContainer, T> resolveFunction;
                private SingletonResolver<T> resolver;

                public UnresolvedState(SingletonResolver<T> resolver, Func<DictionaryContainer, T> resolveFunction)
                {
                    this.resolveFunction = resolveFunction;
                    this.resolver = resolver;
                }

                private bool SingletonHasNotBeenCreated
                {
                    get
                    {
                        return this.resolveFunction != null;
                    }
                }

                public override T Resolve(DictionaryContainer container)
                {
                    lock (this)
                    {
                        if (this.SingletonHasNotBeenCreated)
                        {
                            var instance = this.resolveFunction(container);
                            this.resolver.state = new ResolvedState(instance);
                            this.SignalThatSingletonHasBeenCreated();
                        }

                        return this.resolver.Resolve(container);
                    }
                }

                private void SignalThatSingletonHasBeenCreated()
                {
                    this.resolveFunction = null;
                }
            }

            private class ResolvedState
                : SingletonResolverState
            {
                private T instance;

                public ResolvedState(T instance)
                {
                    this.instance = instance;
                }

                public override T Resolve(DictionaryContainer container)
                {
                    return this.instance;
                }
            }
        }
    }
}