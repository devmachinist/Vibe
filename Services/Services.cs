using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Vibe
{
    public static class ServiceHub
    {
        public static IServiceCollection Services {get;set;} 
        public static IAdvancedServiceProvider ServiceProvider { get; set; }
        public static dynamic Global;
        static ServiceHub()
        {
            var services = new ServiceCollection();
            services.AddSingleton<DependencyInjectorFactory>();
            ServiceProvider = new AdvancedServiceProvider(services);
            Services = ServiceProvider.ServiceCollection;
            foreach(var service in Services){
                if(service.ServiceType != null){
                    Console.WriteLine(ServiceProvider.GetService(service.ServiceType).GetType().Name);
                }
            }
            Global = new ExpandoObject();
        }
        public static IAdvancedServiceProvider Build()
        {
            return ServiceProvider;
        }
    }
    public class InjectAttribute : Attribute { }

    public class DependencyInjectorFactory
    {
        private readonly IAdvancedServiceProvider _serviceProvider;

        public DependencyInjectorFactory(IAdvancedServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public T Create<T>() where T : class, new()
        {
            // Create an instance of the object
            var instance = new T();

            // Inject dependencies
            InjectDependencies(instance);

            return instance;
        }

        private T InjectDependencies<T>(T target)
        {
            var properties = target.GetType().GetProperties()
                .Where(p => p.IsDefined(typeof(InjectAttribute), true));
            

            foreach (var property in properties)
            {
                var dependency = _serviceProvider.GetService(property.PropertyType);
                if (dependency != null)
                {
                    property.SetValue(target, dependency);
                }
            }
            return target;
        }
    }
}
