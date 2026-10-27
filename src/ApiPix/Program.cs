using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<CobrancaRepository>();
builder.Services.AddSingleton<IProvedorPix, ProvedorPixSimulado>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ApiPix v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.MapGet("/", () => Results.Redirect("/swagger"));

app.MapPost("/cobrancas", async (CriarCobrancaRequest request, CobrancaRepository repository, IProvedorPix provedorPix) =>
{
    if (request.Valor <= 0)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            [nameof(request.Valor)] = ["O valor deve ser maior que zero."]
        });
    }

    if (string.IsNullOrWhiteSpace(request.ChavePix))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            [nameof(request.ChavePix)] = ["A chave Pix é obrigatória."]
        });
    }

    var cobranca = new Cobranca
    {
        Id = Guid.NewGuid().ToString("N"),
        Valor = request.Valor,
        NomePagador = request.NomePagador,
        ChavePix = request.ChavePix,
        Status = StatusCobranca.Pendente,
        DataVencimento = DateTime.UtcNow.AddHours(24)
    };

    await provedorPix.CriarCobrançaAsync(cobranca);
    repository.Adicionar(cobranca);

    return Results.Created($"/cobrancas/{cobranca.Id}", cobranca.ToResponse());
});

app.MapGet("/cobrancas/{id}", (string id, CobrancaRepository repository) =>
{
    var cobranca = repository.Obter(id);
    if (cobranca is null)
    {
        return Results.NotFound();
    }

    var statusAtual = cobranca.ObterStatusAtual();
    return Results.Ok(cobranca.ToResponse(statusAtual));
});

app.MapPost("/webhook/confirmacao", (ConfirmacaoPagamentoRequest request, CobrancaRepository repository) =>
{
    if (string.IsNullOrWhiteSpace(request.CobrancaId))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            [nameof(request.CobrancaId)] = ["O identificador da cobrança é obrigatório."]
        });
    }

    var cobranca = repository.Obter(request.CobrancaId);
    if (cobranca is null)
    {
        return Results.NotFound();
    }

    cobranca.Status = StatusCobranca.Paga;
    return Results.Ok(new
    {
        Mensagem = "Pagamento confirmado.",
        CobrancaId = cobranca.Id,
        Status = cobranca.Status.ToString()
    });
});

app.Run();

public interface IProvedorPix
{
    Task CriarCobrançaAsync(Cobranca cobranca);
}

public class ProvedorPixSimulado : IProvedorPix
{
    public Task CriarCobrançaAsync(Cobranca cobranca)
    {
        cobranca.IdentificadorProvador = $"pix-simulado-{cobranca.Id}";
        return Task.CompletedTask;
    }
}

public class CobrancaRepository
{
    private readonly ConcurrentDictionary<string, Cobranca> _cobrancas = new();

    public void Adicionar(Cobranca cobranca)
    {
        _cobrancas[cobranca.Id] = cobranca;
    }

    public Cobranca? Obter(string id)
    {
        _cobrancas.TryGetValue(id, out var cobranca);
        return cobranca;
    }
}

public class CriarCobrancaRequest
{
    public decimal Valor { get; set; }
    public string NomePagador { get; set; } = string.Empty;
    public string ChavePix { get; set; } = string.Empty;
}

public class ConfirmacaoPagamentoRequest
{
    public string CobrancaId { get; set; } = string.Empty;
}

public class Cobranca
{
    public string Id { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public string NomePagador { get; set; } = string.Empty;
    public string ChavePix { get; set; } = string.Empty;
    public StatusCobranca Status { get; set; }
    public DateTime DataVencimento { get; set; }
    public string IdentificadorProvador { get; set; } = string.Empty;

    public StatusCobranca ObterStatusAtual()
    {
        if (Status == StatusCobranca.Paga)
        {
            return StatusCobranca.Paga;
        }

        if (DateTime.UtcNow > DataVencimento)
        {
            return StatusCobranca.Expirada;
        }

        return StatusCobranca.Pendente;
    }

    public CobrancaResponse ToResponse(StatusCobranca? statusAtual = null)
    {
        var status = statusAtual ?? Status;
        return new CobrancaResponse
        {
            Id = Id,
            Valor = Valor,
            NomePagador = NomePagador,
            ChavePix = ChavePix,
            Status = status.ToString()
        };
    }
}

public enum StatusCobranca
{
    Pendente,
    Paga,
    Expirada
}

public class CobrancaResponse
{
    public string Id { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public string NomePagador { get; set; } = string.Empty;
    public string ChavePix { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public partial class Program { }
