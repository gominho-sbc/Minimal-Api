#region Usings
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using minimal_api.Dominio.DTOs;
using minimal_api.Dominio.Entidades;
using minimal_api.Dominio.Enuns;
using minimal_api.Dominio.ModelViews;
using minimal_api.Dominio.Servicos;
using MinimalApi.Dominio.Entidades;
using MinimalApi.Dominio.Interfaces;
using MinimalApi.Dominio.Servicos;
using MinimalApi.DTOs;
using MinimalApi.infraestrutura.Db;
#endregion

public class Startup
{
    public Startup(IConfiguration configuration)
    {
            Configuration = configuration;
            key = Configuration?.GetSection("Jwt")?.ToString() ?? "";
    }

    private string key = "";
    public IConfiguration Configuration { get; set; } = default!;

    public void ConfigureServices(IServiceCollection services)
    {

        services.AddAuthentication(option =>
        {
            option.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            option.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(option =>
        {
            option.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateLifetime = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                ValidateIssuer = false,
                ValidateAudience = false,
            };
        });

        services.AddAuthorization();

        services.AddScoped<iAdministradorServico, AdministradorServico>();
        services.AddScoped<IVeiculoServico, VeiculoServico>();

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        services.AddSwaggerGen(options =>
        {
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Insira o token JWT aqui"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
            });

        });

        services.AddDbContext<DbContexto>(options =>
        {
            options.UseMySql(Configuration.GetConnectionString("mysql"),
            ServerVersion.AutoDetect(Configuration.GetConnectionString("mysql")));
        });

    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseSwagger();
        app.UseSwaggerUI();

        app.UseRouting();
        //config JWT
        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            #region Home
            endpoints.MapGet("/", () => Results.Json(new Home())).AllowAnonymous().WithTags("Home");

            #endregion

            #region Administradores

            string GerarTokenJwt(Administrador administrador)
            {

                if (string.IsNullOrEmpty(key))
                {
                    return string.Empty;
                }
                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
                var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

                var claims = new List<Claim>()
                {
                    new Claim("Email", administrador.Email),
                    new Claim("Perfil", administrador.Perfil),
                    new Claim(ClaimTypes.Role, administrador.Email),
                };

                var token = new JwtSecurityToken(
                    claims: claims,
                    expires: DateTime.Now.AddDays(1),
                    signingCredentials: credentials
                );

                return new JwtSecurityTokenHandler().WriteToken(token);
            }

            endpoints.MapPost("/administradores/login", ([FromBody] LoginDTO loginDTO, iAdministradorServico administradorServico) =>
            {
                var adm = administradorServico.Login(loginDTO);

                if (adm != null)
                {
                    string token = GerarTokenJwt(adm);
                    return Results.Ok(new AdministradorLogado
                    {
                        Email = adm.Email,
                        Perfil = adm.Perfil,
                        Token = token

                    });
                }
                else
                {
                    return Results.Unauthorized();
                }
            }).AllowAnonymous().WithTags("Administrador");

            endpoints.MapGet("/administradores", ([FromQuery] int? pagina, iAdministradorServico administradorServico) =>
            {
                var adms = new List<AdministradorModelView>();
                var administradores = administradorServico.Todos(pagina);

                foreach (var adm in administradores)
                {
                    adms.Add(new AdministradorModelView
                    {
                        Id = adm.Id,
                        Email = adm.Email,
                        Perfil = (Perfil)Enum.Parse(typeof(Perfil), adm.Perfil)
                    });
                }

                return Results.Ok(administradorServico.Todos(pagina));

            })
            .RequireAuthorization()
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Adm" })
            .WithTags("Administrador");

            endpoints.MapGet("/administradores/{id}", ([FromQuery] int id, iAdministradorServico administradorServico) =>
            {
                var adm = administradorServico.BuscaPorId(id);

                if (adm == null)
                {
                    return Results.NotFound();
                }
                return Results.Ok(adm);

            })
            .RequireAuthorization()
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Adm" })
            .WithTags("Administrador");

            endpoints.MapPost("/administradores", ([FromBody] AdministradorDTO administradorDTO, iAdministradorServico administradorServico) =>
            {
                var validacao = new ErrosDeValidacao
                {
                    Mensagens = new List<string>()
                };

                if (string.IsNullOrEmpty(administradorDTO.Email))
                    validacao.Mensagens.Add("Campo Email não pode ficar em branco");
                if (string.IsNullOrEmpty(administradorDTO.Senha))
                    validacao.Mensagens.Add("Campo Senha não pode ficar em branco");
                if (administradorDTO.Perfil == null)
                    validacao.Mensagens.Add("Campo Perfil não pode ficar em branco");

                if (validacao.Mensagens.Count > 0)
                {
                    return Results.BadRequest(validacao);
                }

                var adm = new Administrador
                {
                    Email = administradorDTO.Email,
                    Senha = administradorDTO.Senha,
                    Perfil = administradorDTO.Perfil.ToString() ?? Perfil.Editor.ToString()
                };

                administradorServico.Incluir(adm);

                return Results.Created($"/administrador/{adm.Id}", adm);

            })
            .RequireAuthorization()
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Adm" })
            .WithTags("Administrador");


            #endregion

            #region Veiculos

            ErrosDeValidacao validaDTO(VeiculoDTO veiculoDTO)
            {
                var validacao = new ErrosDeValidacao
                {
                    Mensagens = new List<string>()
                };

                if (string.IsNullOrEmpty(veiculoDTO.Nome))
                    validacao.Mensagens.Add("O nome não pode ficar em branco");

                if (string.IsNullOrEmpty(veiculoDTO.Marca))
                    validacao.Mensagens.Add("A marca não pode ficar em branco");

                if (veiculoDTO.Ano < 1950)
                    validacao.Mensagens.Add("Veículo muito antigo, aceito somente anos superiores a 1950");

                return validacao;
            }

            endpoints.MapPost("/veiculos", ([FromBody] VeiculoDTO veiculoDTO, IVeiculoServico veiculoServico) =>
            {
                var validacao = validaDTO(veiculoDTO);

                if (validacao.Mensagens.Count > 0)
                {
                    return Results.BadRequest(validacao);
                }

                var veiculo = new Veiculo
                {
                    Nome = veiculoDTO.Nome,
                    Marca = veiculoDTO.Marca,
                    Ano = veiculoDTO.Ano
                };

                veiculoServico.Incluir(veiculo);
                return Results.Created($"/veiculo/{veiculo.Id}", veiculo);
            })
            .RequireAuthorization()
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Adm,Editor" })
            .WithTags("Veiculo");

            endpoints.MapGet("/veiculos", ([FromQuery] int? pagina, IVeiculoServico veiculoServico) =>
            {
                var veiculos = veiculoServico.Todos(pagina);

                return Results.Ok(veiculos);
            }).RequireAuthorization().WithTags("Veiculo");

            endpoints.MapGet("/veiculos/{id}", ([FromQuery] int id, IVeiculoServico veiculoServico) =>
            {
                var veiculos = veiculoServico.BuscaPorId(id);

                if (veiculos == null)
                {
                    return Results.NotFound();
                }
                return Results.Ok(veiculos);

            })
            .RequireAuthorization()
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Adm,Editor" })
            .WithTags("Veiculo");

            endpoints.MapPut("/veiculos/{id}", ([FromQuery] int id, VeiculoDTO veiculoDTO, IVeiculoServico veiculoServico) =>
            {
                var veiculos = veiculoServico.BuscaPorId(id);

                if (veiculos == null)
                {
                    return Results.NotFound();
                }

                var validacao = validaDTO(veiculoDTO);

                if (validacao.Mensagens.Count > 0)
                {
                    return Results.BadRequest(validacao);
                }

                veiculos.Nome = veiculoDTO.Nome;
                veiculos.Marca = veiculoDTO.Marca;
                veiculos.Ano = veiculoDTO.Ano;

                veiculoServico.Atualizar(veiculos);

                return Results.Ok(veiculos);

            })
            .RequireAuthorization()
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Adm" })
            .WithTags("Veiculo");

            endpoints.MapDelete("/veiculos/{id}", ([FromQuery] int id, IVeiculoServico veiculoServico) =>
            {
                var veiculos = veiculoServico.BuscaPorId(id);

                if (veiculos == null)
                {
                    return Results.NotFound();
                }
                veiculoServico.Apagar(veiculos);

                return Results.NoContent();

            })
            .RequireAuthorization()
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Adm" })
            .WithTags("Veiculo");

            #endregion

            #region CodigoInicial
            // endpoints.MapPost("/login", (LoginDTO loginDTO) =>
            // {
            //     if (loginDTO.Email == "adm@teste.com" && loginDTO.Senha == "123456")
            //     {
            //         return Results.Ok("Login com sucesso");
            //     }
            //     else
            //     {
            //         return Results.Unauthorized();
            //     }
            // });
            #endregion
        });

    }


}
