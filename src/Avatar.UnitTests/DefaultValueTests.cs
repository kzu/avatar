﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Avatars.UnitTests
{
    public class DefaultValueTests
    {
        [Fact]
        public void DoesNotSetRefValue()
        {
            var method = typeof(IDefaultValues).GetMethod(nameof(IDefaultValues.VoidWithRef))!;
            IAvatarBehavior behavior = new DefaultValueBehavior();
            var value = new object[] { new object() };

            var result = behavior.Execute(MethodInvocation.Create(new object(), method, value), null!);

            Assert.Equal(1, result.Outputs.Count);
            Assert.NotNull(result.Outputs.GetValue(0));
            Assert.Same(result.Outputs.GetValue(0), value);
        }

        [Fact]
        public void DoesNotSetsRefEnumValue()
        {
            var method = typeof(IDefaultValues).GetMethod(nameof(IDefaultValues.VoidWithRefEnum))!;
            IAvatarBehavior behavior = new DefaultValueBehavior();
            var platform = PlatformID.Xbox;

            var result = behavior.Execute(MethodInvocation.Create(new object(), method, platform), null!);

            Assert.Equal(1, result.Outputs.Count);
            Assert.NotNull(result.Outputs.GetValue(0));
            Assert.Equal(platform, result.Outputs.GetValue(0));
        }

        [Fact]
        public void SetsOutValue()
        {
            var method = typeof(IDefaultValues).GetMethod(nameof(IDefaultValues.VoidWithOut))!;
            IAvatarBehavior behavior = new DefaultValueBehavior();

            var result = behavior.Execute(MethodInvocation.Create(new object(), method, Array.Empty<object>()), null!);

            Assert.Equal(1, result.Outputs.Count);
            Assert.NotNull(result.Outputs.GetValue(0));
            Assert.True(result.Outputs.GetValue(0) is object[]);
        }

        [Fact]
        public void SetsReturnEnum()
        {
            var method = typeof(IDefaultValues).GetMethod(nameof(IDefaultValues.ReturnEnum))!;
            IAvatarBehavior behavior = new DefaultValueBehavior();

            var result = behavior.Execute(MethodInvocation.Create(new object(), method), null!);

            Assert.Equal(default(PlatformID), result.ReturnValue);
        }

        [Fact]
        public void DoesNotFailWithNonMethodInfo()
        {
            var ctor = typeof(Foo).GetConstructors().First();
            IAvatarBehavior behavior = new DefaultValueBehavior();

            behavior.Execute(MethodInvocation.Create(new object(), ctor, PlatformID.Win32NT), null!);
        }

        [Fact]
        public void DefaultForNullableValueTypeIsNull()
            => Assert.Null(new DefaultValueProvider().GetDefault<PlatformID?>());

        [Fact]
        public void DefaultForArrayIsEmpty()
        {
            var value = new DefaultValueProvider().GetDefault(typeof(object[]));

            Assert.NotNull(value);
            Assert.IsType<object[]>(value);
            Assert.Empty((Array)value!);
        }

        [Fact]
        public void DefaultForTaskIsCompleted()
        {
            var value = new DefaultValueProvider().GetDefault<Task>();

            Assert.NotNull(value);
            Assert.True(value.IsCompleted);
        }

        [Fact]
        public void DefaultForTaskTIsCompleted()
        {
            var value = new DefaultValueProvider().GetDefault<Task<bool>>();

            Assert.NotNull(value);
            Assert.True(value.IsCompleted);
            Assert.False(value.Result);
        }

        [Fact]
        public void DefaultForValueTaskIsCompleted()
        {
            var value = new DefaultValueProvider().GetDefault<ValueTask>();

            Assert.True(value.IsCompleted);
        }

        [Fact]
        public async Task DefaultForValueTaskTIsCompleted()
        {
            var value = new DefaultValueProvider().GetDefault<ValueTask<bool>>();

            Assert.True(value.IsCompleted);
            Assert.False(await value);
        }

        [Fact]
        public void DefaultForTaskOfEnumIsDefaultValue()
        {
            var value = new DefaultValueProvider().GetDefault<Task<PlatformID>>();

            Assert.NotNull(value);
            Assert.True(value.IsCompleted);
            Assert.Equal(default, value.Result);
        }

        [Fact]
        public void DefaultForEnumerableIsNotNull()
            => Assert.NotNull(new DefaultValueProvider().GetDefault<IEnumerable>());

        [Fact]
        public void DefaultForGenericEnumerableIsEmpty()
            => Assert.Empty(new DefaultValueProvider().GetDefault<IEnumerable<object>>());

        [Fact]
        public void DefaultForQueryableIsNotNull()
            => Assert.NotNull(new DefaultValueProvider().GetDefault<IQueryable>());

        [Fact]
        public void DefaultForGenericQueryableIsEmpty()
            => Assert.Empty(new DefaultValueProvider().GetDefault<IQueryable<object>>());

        [Fact]
        public void DefaultForRefIsElementType()
        {
            var method = typeof(IDefaultValues).GetMethod(nameof(IDefaultValues.VoidWithRef))!;
            var parameter = method.GetParameters()[0];

            Assert.True(parameter.ParameterType.IsByRef);

            var value = new DefaultValueProvider().GetDefault(parameter.ParameterType);

            Assert.NotNull(value);
            Assert.True(value is object[]);
            Assert.Empty((object[])value!);
        }

        [Fact]
        public void DefaultForArrayWithRankIsEmpty()
            => Assert.Empty(new DefaultValueProvider().GetDefault<int[][]>());

        [Fact]
        public void DefaultForValueTupleHasDefaults()
        {
            var (providers, formatter, platform) = new DefaultValueProvider().GetDefault<(IServiceProvider[], Task<IFormatProvider>, PlatformID)>();

            Assert.Empty(providers);
            Assert.True(formatter.IsCompleted);
            Assert.Equal(default, platform);
        }

        [Fact]
        public void RegisterDefaultValue()
        {
            var provider = new DefaultValueProvider();
            var expected = new object[] { 5, 10 };

            provider.Register(typeof(IEnumerable<object>), _ => expected);

            var value = provider.GetDefault<IEnumerable<object>>();

            Assert.Same(expected, value);
        }

        [Fact]
        public void RegisterDefaultValueGeneric()
        {
            var provider = new DefaultValueProvider();
            var expected = new object[] { 5, 10 };

            provider.Register<IEnumerable<object>>(() => expected);

            var value = provider.GetDefault<IEnumerable<object>>();

            Assert.Same(expected, value);
        }

        [Fact]
        public void DeregisterRemovesDefaultValue()
        {
            var provider = new DefaultValueProvider();

            provider.Register(() => new Foo(PlatformID.Win32NT));
            Assert.True(provider.Deregister<Foo>());

            Assert.Null(provider.GetDefault<Foo>());
        }

        public class Foo
        {
            public Foo(PlatformID platform)
            {
            }
        }

        public interface IDefaultValues
        {
            void VoidWithRef(ref object[] refValue);

            void VoidWithRefEnum(ref PlatformID refEnum);

            void VoidWithOut(out object[] refValue);

            PlatformID ReturnEnum();
        }
    }
}
