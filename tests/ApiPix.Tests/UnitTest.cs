using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ApiPix.Tests;

public class CobrancasApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CobrancasApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PostCobrancas_CriaCobrançaValida()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/cobrancas", new
        {
            Valor = 150.50m,
            NomePagador = "Maria Souza",
            ChavePix = "maria@exemplo.com"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var cobranca = await response.Content.ReadFromJsonAsync<CobrancaResponse>();
        Assert.NotNull(cobranca);
        Assert.Equal("Pendente", cobranca!.Status);
        Assert.Equal("Maria Souza", cobranca.NomePagador);
        Assert.Equal(150.50m, cobranca.Valor);
    }

    [Fact]
    public async Task PostCobrancas_QuandoValorEhInvalido_RetornaBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/cobrancas", new
        {
            Valor = 0m,
            NomePagador = "Maria Souza",
            ChavePix = "maria@exemplo.com"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetCobrancasPorId_RetornaStatusAtual()
    {
        var client = _factory.CreateClient();

        var criada = await client.PostAsJsonAsync("/cobrancas", new
        {
            Valor = 75m,
            NomePagador = "João",
            ChavePix = "joao@exemplo.com"
        });

        var cobranca = await criada.Content.ReadFromJsonAsync<CobrancaResponse>();
        Assert.NotNull(cobranca);

        var response = await client.GetAsync($"/cobrancas/{cobranca!.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var resultado = await response.Content.ReadFromJsonAsync<CobrancaResponse>();
        Assert.NotNull(resultado);
        Assert.Equal("Pendente", resultado!.Status);
    }

    [Fact]
    public async Task PostWebhookConfirmacao_AtualizaStatusParaPaga()
    {
        var client = _factory.CreateClient();

        var criada = await client.PostAsJsonAsync("/cobrancas", new
        {
            Valor = 90m,
            NomePagador = "Carlos",
            ChavePix = "carlos@exemplo.com"
        });

        var cobranca = await criada.Content.ReadFromJsonAsync<CobrancaResponse>();
        Assert.NotNull(cobranca);

        var response = await client.PostAsJsonAsync("/webhook/confirmacao", new
        {
            CobrancaId = cobranca!.Id
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var consultada = await client.GetAsync($"/cobrancas/{cobranca.Id}");
        var status = await consultada.Content.ReadFromJsonAsync<CobrancaResponse>();
        Assert.Equal("Paga", status!.Status);
    }

    public sealed class CobrancaResponse
    {
        public string Id { get; set; } = string.Empty;
        public decimal Valor { get; set; }
        public string NomePagador { get; set; } = string.Empty;
        public string ChavePix { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}
