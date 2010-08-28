namespace FakeItEasy.Core.Creation
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    public interface IProxyGenerator2
    {
        ProxyGeneratorResult TryCreateProxy(Type typeOfProxy, IEnumerable<Type> additionalInterfacesToImplement, IEnumerable<object> argumentsForConstructor);
        bool MemberCanBeIntercepted(MemberInfo member);
    }
}