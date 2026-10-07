namespace FusionCanvas.Integration.Tests.Stores.Printify;

internal static class PrintifyApiFixtures
{
    public const string Shop = "{\"id\":42,\"title\":\"The Groan Zone\"}";
    public const string CreatedProduct = "{\"id\":\"product-1\",\"title\":\"Dad Joke Loading\",\"visible\":false,\"is_locked\":false}";
    public const string PublishedProduct = "{\"id\":\"product-1\",\"title\":\"Dad Joke Loading\",\"visible\":true,\"is_locked\":false,\"external_id\":\"shopify-1\",\"handle\":\"dad-joke-loading\"}";
    public const string LockedProduct = "{\"id\":\"product-1\",\"visible\":false,\"is_locked\":true}";
    public const string ValidationError = "{\"code\":\"validation_error\",\"message\":\"A variant is invalid.\"}";
    public const string Malformed = "<html>not-json</html>";
}
