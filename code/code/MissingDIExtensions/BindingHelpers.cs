using System;
using Ninject;

namespace code.MissingDIExtensions
{
	public static class BindingHelpers
	{
		public static void BindToMethod<T>(this IKernelConfiguration config, Func<T> method) => config.Bind<T>().ToMethod(c => method());
	}
}