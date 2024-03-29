﻿using System;
using Microsoft.Extensions.DependencyInjection;

namespace AutoMapper.Extensions.MappingProfile
{
    public static class Extensions
    {
        public static IServiceCollection AddMappingProfiles(this IServiceCollection services)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddAutoMapper(typeof(MappingProfile).Assembly);
            return services;
        }
    }
}