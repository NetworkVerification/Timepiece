using System.Text;

namespace Timepiece.Angler;

/// <summary>
/// Adds a method to produce a compact string representation of an inheriting class.
/// Properties which are not changed from the default value will be suppressed.
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class DifferentiatedString<T> where T : new()
{
  public string DiffString()
  {
    var thisDefault = new T();
    var builder = new StringBuilder($"{typeof(T).Name}(");
    builder.AppendJoin(',', typeof(T).GetProperties().SelectMany(prop =>
    {
      var property = typeof(T).GetProperty(prop.Name)!;
      dynamic value = property.GetValue(this)!;
      dynamic defaultValue = property.GetValue(thisDefault)!;
      return value != defaultValue ? Enumerable.Repeat($"{prop.Name}={value}", 1) : Enumerable.Empty<string>();
    }));
    builder.Append(')');
    return builder.ToString();
  }
}
