# api-pix

## O que o projeto faz
Esta API cria cobranças Pix, consulta o status e confirma pagamentos via webhook. A solução usa armazenamento em memória para manter a implementação simples e didática.

## Tecnologias usadas
- ASP.NET Core Web API
- C#
- xUnit para testes
- Swagger / OpenAPI para documentação da API
- Injeção de dependência para isolar o provedor Pix

## Como rodar
1. Clone o projeto:
   ```bash
   git clone https://github.com/ElizioDiniz/api-pix.git
   ```
2. Execute a aplicação:
   ```bash
   dotnet run --project src/ApiPix/ApiPix.csproj
   ```
3. Acesse a documentação da API no Swagger em:
   ```text
   http://localhost:5280/swagger
   ```

## Lista dos endpoints
- POST /cobrancas — cria uma cobrança com valor, nome do pagador e chave Pix
- GET /cobrancas/{id} — consulta o status atual da cobrança
- POST /webhook/confirmacao — recebe a confirmação de pagamento e marca como paga

## O que você aprendeu ou decisões que tomou
- Mantive a solução em memória para focar na regra de negócio e na arquitetura sem depender de banco.
- Isolei a integração com o provedor Pix em uma interface para facilitar trocar por um serviço real depois.
- Validei regras importantes cedo para evitar cobranças inválidas e manter a API previsível.
