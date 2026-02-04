namespace JedoxTranslator.API.Endpoints;

public static class Endpoints
{
    public static WebApplication RegisterEndpoints(this WebApplication app)
    {
        app.GetAllSids();
        app.GetBySid();
        app.CreateTranslation();
        app.UpdateTranslation();
        app.UpdateSourceText();
        app.DeleteTranslation();
        app.DeleteBySid();
        app.GetAllWithLanguage();

        return app;
    }
}
