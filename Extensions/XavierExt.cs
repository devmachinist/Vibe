using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vibe.Extensions
{
    public static class XavierExt
    {
        public static IServiceCollection UseXavier(this IServiceCollection services)
        {
            XavierGlobal.Memory = new Memory();
            Task.WaitAll(Task.Run(async () =>
            {
                await XavierGlobal.Memory.Init();
            }));
            return services;
        }
    }
}
