using System.Text.Json;

namespace App.Domain;

public static class JsonDefaults
{
    public static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);
}
