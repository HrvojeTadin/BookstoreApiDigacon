namespace BookstoreApi;

public static class DependencyInjection
{
    public static WebApplicationBuilder AddSqlServerDb(this WebApplicationBuilder builder)
    {
        builder.Services.AddDbContext<BookstoreDigaconDbContext>(opts =>
            opts.UseSqlServer(
                builder.Configuration.GetConnectionString("DefaultConnection")));
        return builder;
    }

    public static WebApplicationBuilder AddBookstoreServices(this WebApplicationBuilder builder)
    {
        // BookstoreService, IThirdPartyBookClient…
        builder.Services.AddScoped<IBookstoreService, BookstoreService>();
        builder.Services.AddSingleton<IThirdPartyBookClient, MockThirdPartyBookClient>();
        return builder;
    }

    public static WebApplicationBuilder AddQuartzService(this WebApplicationBuilder builder)
    {
        // 1) Registriraj sekciju "BookImport" u POCO BookImportSettings
        builder.Services.Configure<BookImportSettings>(
            builder.Configuration.GetSection("BookImport"));

        // 2) Quartz konfiguracija: satni import job
        builder.Services.AddQuartz(q =>
        {
            var jobKey = new JobKey("BookImportJob", "ImportGroup");

            // Registriraj sam job (durably = preživi bez trigggera)
            q.AddJob<BookImportJob>(opts => opts
                .WithIdentity(jobKey)
                .StoreDurably()
            );

            // Trigger: svaki puni sat
            q.AddTrigger(opts => opts
                .ForJob(jobKey)
                .WithIdentity("BookImportTrigger", "ImportGroup")
                .WithCronSchedule("0 0 * * * ?",   // quartz cron: sekunda, minuta, sat, dan…, ovdje = svaki puni sat
                    cronOpts => cronOpts
                        .WithMisfireHandlingInstructionDoNothing()
                )
            );
        });

        // 3) Hosted service koji starta QuartzScheduler pri launchu
        builder.Services.AddQuartzHostedService(opts =>
        {
            // pri shutdownu pričekaj da se jobovi dovrše
            opts.WaitForJobsToComplete = true;
        });

        return builder;
    }

    public static WebApplicationBuilder AddJwtAuthentication(this WebApplicationBuilder builder)
    {
        // ──────────────────────────────────────────
        // 1) mapiranje settings
        // ──────────────────────────────────────────
        builder.Services.Configure<JwtSettings>(
            builder.Configuration.GetSection("JwtSettings")
        );
        var jwt = builder.Configuration
            .GetSection("JwtSettings")
            .Get<JwtSettings>()!;

        // ──────────────────────────────────────────
        // 2) Authentication (JWT Bearer)
        // ──────────────────────────────────────────
        builder.Services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = true;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwt.Key)
                    ),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });

        // ──────────────────────────────────────────
        // 3) Authorization policies
        // ──────────────────────────────────────────

        builder.Services.AddAuthorizationBuilder()
              .AddPolicy(AuthPolicies.RequireReadRole, policy =>
                   policy.RequireRole(AuthRoles.Read, AuthRoles.ReadWrite))
              .AddPolicy(AuthPolicies.RequireReadWriteRole, policy =>
                    policy.RequireRole(AuthRoles.ReadWrite));

        return builder;
    }

    public static WebApplicationBuilder AddSwaggerWithJwt(this WebApplicationBuilder builder)
    {
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Bookstore API",
                Version = "v1"
            });

            var jwtScheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.ApiKey,
                Scheme = JwtBearerDefaults.AuthenticationScheme,
                In = ParameterLocation.Header,
                Description = "Enter `Bearer {token}`"
            };
            c.AddSecurityDefinition("Bearer", jwtScheme);
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                [new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Id = "Bearer",
                        Type = ReferenceType.SecurityScheme
                    }
                }
                ] = Array.Empty<string>()
            });
        });

        return builder;
    }
}
