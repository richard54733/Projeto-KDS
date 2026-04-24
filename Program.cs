using System.Text.Json;
using System.Collections.Generic;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}



// 🔥 armazenamento em memória
var pedidos = new List<Pedido>();

// 🔥 opções do JSON (ignora maiúscula/minúscula)
var jsonOptions = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true
};

app.MapPost("/webhook", async (HttpRequest request) =>
{
    try
    {
        var pedido = await JsonSerializer.DeserializeAsync<Pedido>(request.Body, jsonOptions);

        if (pedido is null)
            return Results.BadRequest("JSON inválido");

        pedidos.Add(pedido);

        Console.WriteLine("\n🔥 NOVO PEDIDO");
        Console.WriteLine($"🧾 Nº: {pedido.numeroPedido}");
        Console.WriteLine($"👤 Cliente: {pedido.nomeCliente}");
        Console.WriteLine($"💰 Total: R$ {pedido.valorCompra}");

        return Results.Ok(new { status = "recebido" });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Erro: {ex.Message}");
        return Results.BadRequest("Erro ao processar pedido");
    }
});

app.MapGet("/pedidos", () => Results.Json(pedidos));

// 🖥️ KDS (com auto refresh 🔥)
app.MapGet("/kds", () =>
{
    var html = """
    <html>
    <head>
        <title>KDS - Cozinha</title>
        <meta name="viewport" content="width=device-width, initial-scale=1">
        <meta http-equiv="refresh" content="5"> 
        <style>
            body { font-family: Arial; background:#111; color:#fff; }
            .card { background:#222; padding:15px; margin:10px; border-radius:10px; }
            h1 { text-align:center; }
            .item { margin-left:10px; }
        </style>
    </head>
    <body>
        <h1>🔥 PEDIDOS EM TEMPO REAL</h1>
    """;

    foreach (var p in pedidos)
    {
        html += $"""
        <div class="card">
            <h2>Pedido {p.numeroPedido}</h2>
            <p><b>Cliente:</b> {p.nomeCliente}</p>
            <p><b>Tipo:</b> {p.tipoPedido}</p>
            <p><b>Total:</b> R$ {p.valorCompra}</p>

            <ul>
        """;

        foreach (var item in p.produtos)
        {
            html += $"<li class='item'>{item.quantidade}x {item.nome}</li>";
        }

        html += """
            </ul>
        </div>
        """;
    }

    html += """
    </body>
    </html>
    """;

    return Results.Content(html, "text/html");
});

app.Run();

// 🔥 MODELOS
record Produto(string nome, string quantidade, double valor, List<string> adicionais);

record Pedido(
    string loja,
    string nomeCliente,
    string telefoneCliente,
    string tipoPedido,
    string endereco,
    string dataCompra,
    double valorCompra,
    string numeroPedido,
    string tipoPagamento,
    string metodoPagamento,
    List<Produto> produtos
);