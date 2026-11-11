using System.Text.Json;
using System.Text.Json.Serialization;

namespace FreezeIsland.Models.Rules;

/// <summary>
/// 读取规则设置中的枚举：接受字符串或数字，无法识别时回退到默认值。
/// 手改配置写错值时，规则只是判定为默认项，不会因反序列化异常中断宿主的规则集判定。
/// </summary>
public sealed class TolerantEnumJsonConverter<TEnum> : JsonConverter<TEnum>
    where TEnum : struct, Enum
{
    public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                return Enum.TryParse<TEnum>(reader.GetString(), ignoreCase: true, out var parsed)
                    ? parsed
                    : default;
            case JsonTokenType.Number:
                return reader.TryGetInt32(out var value) && Enum.IsDefined(typeof(TEnum), value)
                    ? (TEnum)Enum.ToObject(typeof(TEnum), value)
                    : default;
            case JsonTokenType.Null:
                return default;
            default:
                reader.Skip();
                return default;
        }
    }

    public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString());
}
