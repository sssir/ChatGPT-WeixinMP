using ChatGPT_Weixin.Core;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers().AddXmlSerializerFormatters();
builder.Services.AddMemoryCache();
builder.Services.AddChatGPT(builder.Configuration["OpenAI:ApiKey"]);

var app = builder.Build();

app.MapControllers();
app.Run();
