﻿// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System.IO;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using IdentityServer4;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Mappers;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using ProjectIvy.Auth.Data;
using ProjectIvy.Auth.Models;
using ProjectIvy.Auth.Services;
using Serilog;

namespace ProjectIvy.Auth
{
    public class Startup
    {
        private string _connectionString = System.Environment.GetEnvironmentVariable("CONNECTION_STRING");

        public IWebHostEnvironment Environment { get; }
        public IConfiguration Configuration { get; }

        public Startup(IWebHostEnvironment environment, IConfiguration configuration)
        {
            Environment = environment;
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();
            services.AddLogging();

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(_connectionString));

            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            var migrationAssembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;

            var builder = services.AddIdentityServer(options =>
            {
                options.Events.RaiseErrorEvents = true;
                options.Events.RaiseInformationEvents = true;
                options.Events.RaiseFailureEvents = true;
                options.Events.RaiseSuccessEvents = true;

                // see https://identityserver4.readthedocs.io/en/latest/topics/resources.html
                options.EmitStaticAudienceClaim = true;
            })
                .AddConfigurationStore(options =>
                {
                    options.ConfigureDbContext = b =>
                        b.UseSqlServer(_connectionString,
                            sql => sql.MigrationsAssembly(migrationAssembly));
                })
                .AddOperationalStore(options =>
                {
                    options.ConfigureDbContext = b =>
                        b.UseSqlServer(_connectionString,
                            sql => sql.MigrationsAssembly(migrationAssembly));
                })
                .AddProfileService<ProfileService>()
                .AddAspNetIdentity<ApplicationUser>();

            string signingCertificate = new StreamReader("/mnt/certificates/sign.cer").ReadToEnd();
            string signingkey = new StreamReader("/mnt/certificates/sign_key.pem").ReadToEnd();

            var cert = X509Certificate2.CreateFromPem(signingCertificate, signingkey);
            var securityKey = new X509SecurityKey(cert);
            builder.AddSigningCredential(new SigningCredentials(securityKey, "RS256"));

            services.AddScoped<IProfileService, ProfileService>();

            services.AddAuthentication()
                .AddFacebook(options =>
                {
                    options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;

                    options.AppId = System.Environment.GetEnvironmentVariable("FB_APP_ID");
                    options.AppSecret = System.Environment.GetEnvironmentVariable("FB_APP_SECRET");
                })
                .AddGoogle(options =>
                {
                    options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;

                    options.ClientId = System.Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID");
                    options.ClientSecret = System.Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET");
                });

            services.Configure<CookieAuthenticationOptions>(IdentityServerConstants.DefaultCookieAuthenticationScheme, options =>
            {
                options.Cookie.SameSite = SameSiteMode.None;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.IsEssential = true;
            });
        }

        public void Configure(IApplicationBuilder app)
        {
            if (Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                //app.UseDatabaseErrorPage();
            }

            app.UseStaticFiles();

            app.UseRouting();

            app.Use(async (context, next) =>
            {
                context.SetIdentityServerOrigin("https://auth.anticevic.net");
                await next();
            });

            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            app.UseSerilogRequestLogging();

            app.UseIdentityServer();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();
            });

            //InitializeDatabase(app);
        }

        private void InitializeDatabase(IApplicationBuilder app)
        {
            using (var serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope())
            {
                serviceScope.ServiceProvider.GetRequiredService<PersistedGrantDbContext>().Database.Migrate();

                var context = serviceScope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();

                var c = new Client()
                {
                    AccessTokenType = AccessTokenType.Reference,
                    ClientId = "project-ivy-proximity",
                    AllowedGrantTypes = GrantTypes.Code,
                    AllowedScopes = new[] {"openid"},
                    ClientSecrets = new[] {new Secret("h87g5w87g68568g68wsggggstdsusij".Sha256()) },
                    RequirePkce = false
                };
                context.Clients.Add(c.ToEntity());
                context.SaveChanges();
            }
        }
    }
}