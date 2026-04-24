using System.Text.Json;
using System.Collections.Generic;

var port = Environment.GetEnvironmentVariable("PORT") ?? "10000";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 🔥 LISTAS AQUI
var pedidosEmPreparo = new List<Pedido>();
var pedidosFinalizados = new List<Pedido>();


// 🔥 AQUI
//var port = Environment.GetEnvironmentVariable("PORT") ?? "10000";
//app.Urls.Add($"http://0.0.0.0:{port}");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


// 🔥 opções do JSON (ignora maiúscula/minúscula)
var jsonOptions = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true
};

app.MapPost("/webhook", async (HttpRequest request) =>
{
    try
    {
        using var reader = new StreamReader(request.Body);
        var body = await reader.ReadToEndAsync();

        var pedido = JsonSerializer.Deserialize<Pedido>(body, jsonOptions);

        if (pedido is null)
            return Results.BadRequest("JSON inválido");

        pedidosEmPreparo.Add(pedido);

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
        <meta name="viewport" content="width=device-width, initial-scale=1">
        <style>
            body { font-family: Arial; background:#111; color:#fff; }
            .card { background:#222; padding:15px; margin:10px; border-radius:10px; }
            button { background:green; color:#fff; padding:10px; border:none; cursor:pointer; }
        </style>
        <script>
            async function finalizar(numero) {
                await fetch('/finalizar/' + numero, { method: 'POST' });
                location.reload();
            }
        </script>
    </head>
    <body>
        <h1>🔥 EM PREPARO</h1>
    """;

    foreach (var p in pedidosEmPreparo)
    {
        var numero = p.numeroPedido.Split('-')[0];

        html += $"""
        <div class="card">
            <h2>Pedido: {numero}</h2>
            <p><b>Cliente:</b> {p.nomeCliente}</p>
            <p><b>Total:</b> R$ {p.valorCompra}</p>
        """;

        foreach (var prod in p.produtos)
        {
            html += $"<p>{prod.quantidade}x {prod.nome}</p>";

            foreach (var add in prod.adicionais)
            {
                html += $"<p style='margin-left:20px;'>+ {add.quantidade}x {add.nome}</p>";
            }
        }

        html += $"""
            <button onclick="finalizar('{numero}')">✅ Finalizado</button>
        </div>
        """;
    }

    html += """
        <hr>
        <h2>📦 Finalizados (oculto na cozinha)</h2>
    """;

    foreach (var p in pedidosFinalizados)
    {
        var numero = p.numeroPedido.Split('-')[0];

        html += $"""
        <div class="card">
            <h3>Pedido {numero} finalizado</h3>
        </div>
        """;
    }

    html += "</body></html>";

    return Results.Content(html, "text/html");
});

app.MapPost("/finalizar/{numero}", (string numero) =>
{
    var pedido = pedidosEmPreparo.FirstOrDefault(p => p.numeroPedido.StartsWith(numero));

    if (pedido == null)
        return Results.NotFound();

    pedidosEmPreparo.Remove(pedido);
    pedidosFinalizados.Add(pedido);

    return Results.Ok();
});

app.Run();

// 🔥 MODELOS
record Adicional(
    string nome,
    int quantidade,
    double valor
);

record Produto(
    string nome,
    string quantidade,
    double valor,
    List<Adicional> adicionais
);

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