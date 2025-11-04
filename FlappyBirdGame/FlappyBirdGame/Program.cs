var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// ----- ОСЬ І ВСЕ ВИПРАВЛЕННЯ -----

// 1. Якщо браузер просить https://, перенаправляємо
app.UseHttpsRedirection();

// 2. Цей рядок каже: "Якщо хтось просить /, шукай index.html"
// ВІН МАЄ БУТИ ПЕРЕД UseStaticFiles()
app.UseDefaultFiles();

// 3. Цей рядок каже: "Дозволь браузерам завантажувати файли 
//    (index.html, game.js) з папки wwwroot"
app.UseStaticFiles();

// ------------------------------------

app.Run();