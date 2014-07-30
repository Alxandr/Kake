using System;

namespace Kake
{
    /// <summary>
    /// Summary description for ServiceProviderExtensions
    /// </summary>
    public static class ServiceProviderExtensions
    {
	    public static TService Get<TService>(this IServiceProvider provider)
        {
            return (TService)provider.GetService(typeof(TService));
        }
    }
}