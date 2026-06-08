using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Backend_Ingenieria_Purrujas.Api.Services;

public static class AdminAuditDescriptionBuilder
{
    private const int MaxDetails = 6;
    private const int MaxValueLength = 70;

    public static string Created(string entityName, string identifier, IEnumerable<string> values)
    {
        return Build($"{entityName} creado: {identifier}.", values);
    }

    public static string Deleted(string entityName, string identifier)
    {
        return $"{entityName} eliminado: {identifier}.";
    }

    public static string Updated(string entityName, string identifier, object? before, object? after)
    {
        var changes = DescribeObjectChanges(before, after).ToList();
        if (changes.Count == 0)
        {
            return $"{entityName} actualizado: {identifier}. No se detectaron cambios en campos comparables.";
        }

        return Build($"{entityName} actualizado: {identifier}.", changes);
    }

    public static string Updated(string entityName, string identifier, IEnumerable<string> changes)
    {
        var materializedChanges = changes.Where(change => !string.IsNullOrWhiteSpace(change)).ToList();
        if (materializedChanges.Count == 0)
        {
            return $"{entityName} actualizado: {identifier}. No se detectaron cambios en campos comparables.";
        }

        return Build($"{entityName} actualizado: {identifier}.", materializedChanges);
    }

    public static string ContentUpdated(string moduleName, object? before, object? after)
    {
        var changes = DescribeObjectChanges(before, after).ToList();
        if (changes.Count == 0)
        {
            return $"Actualizo el contenido de {moduleName}. No se detectaron cambios en campos comparables.";
        }

        return Build($"Actualizo el contenido de {moduleName}.", changes);
    }

    public static string Changed(string label, object? before, object? after)
    {
        if (before is string beforeText && after is string afterText)
        {
            return ChangedText(label, beforeText, afterText);
        }

        var oldValue = FormatValue(before);
        var newValue = FormatValue(after);

        if (string.Equals(oldValue, newValue, StringComparison.Ordinal))
        {
            return string.Empty;
        }

        if (string.IsNullOrWhiteSpace(oldValue))
        {
            return $"Agrego {label}: '{newValue}'";
        }

        if (string.IsNullOrWhiteSpace(newValue))
        {
            return $"Elimino {label}: '{oldValue}'";
        }

        return $"Cambio {label}: '{oldValue}' -> '{newValue}'";
    }

    public static string Value(string label, object? value)
    {
        return $"{label}: '{FormatValue(value)}'";
    }

    private static string Build(string prefix, IEnumerable<string> details)
    {
        var materializedDetails = details
            .Where(detail => !string.IsNullOrWhiteSpace(detail))
            .Take(MaxDetails + 1)
            .ToList();

        if (materializedDetails.Count == 0)
        {
            return prefix;
        }

        var selectedDetails = materializedDetails.Take(MaxDetails).ToList();
        if (materializedDetails.Count > MaxDetails)
        {
            selectedDetails.Add("otros cambios adicionales");
        }

        return $"{prefix} Cambios: {string.Join("; ", selectedDetails)}.";
    }

    private static IEnumerable<string> DescribeObjectChanges(object? before, object? after)
    {
        if (before is null || after is null)
        {
            yield break;
        }

        foreach (var property in after.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (!property.CanRead)
            {
                continue;
            }

            var beforeProperty = before.GetType().GetProperty(property.Name);
            if (beforeProperty is null || !beforeProperty.CanRead)
            {
                continue;
            }

            var beforeValue = beforeProperty.GetValue(before);
            var afterValue = property.GetValue(after);
            var label = FriendlyLabel(property.Name);

            if (IsStringList(beforeValue, afterValue, out var beforeList, out var afterList))
            {
                foreach (var listChange in DescribeListChanges(label, beforeList, afterList))
                {
                    yield return listChange;
                }

                continue;
            }

            if (IsSimpleValue(property.PropertyType))
            {
                var change = Changed(label, beforeValue, afterValue);
                if (!string.IsNullOrWhiteSpace(change))
                {
                    yield return change;
                }

                continue;
            }

            if (IsEnumerableOfObjects(beforeValue, afterValue, out var beforeObjects, out var afterObjects))
            {
                foreach (var nestedChange in DescribeObjectListChanges(label, beforeObjects, afterObjects))
                {
                    yield return nestedChange;
                }
            }
        }
    }

    private static IEnumerable<string> DescribeListChanges(string label, IReadOnlyList<string> before, IReadOnlyList<string> after)
    {
        var added = after
            .Where(value => !before.Contains(value, StringComparer.OrdinalIgnoreCase))
            .Select(value => $"Agrego en {label}: '{Truncate(value)}'");

        var removed = before
            .Where(value => !after.Contains(value, StringComparer.OrdinalIgnoreCase))
            .Select(value => $"Elimino de {label}: '{Truncate(value)}'");

        foreach (var change in added.Concat(removed))
        {
            yield return change;
        }
    }

    private static IEnumerable<string> DescribeObjectListChanges(
        string label,
        IReadOnlyList<object> before,
        IReadOnlyList<object> after)
    {
        var count = Math.Min(before.Count, after.Count);
        for (var index = 0; index < count; index++)
        {
            foreach (var change in DescribeObjectChanges(before[index], after[index]))
            {
                yield return $"{label} {index + 1}: {LowerFirst(change)}";
            }
        }

        for (var index = count; index < after.Count; index++)
        {
            yield return $"Agrego {label} {index + 1}: '{FormatValue(after[index])}'";
        }

        for (var index = count; index < before.Count; index++)
        {
            yield return $"Elimino {label} {index + 1}: '{FormatValue(before[index])}'";
        }
    }

    private static bool IsStringList(
        object? beforeValue,
        object? afterValue,
        out IReadOnlyList<string> beforeList,
        out IReadOnlyList<string> afterList)
    {
        beforeList = [];
        afterList = [];

        if (beforeValue is not IEnumerable<string> beforeEnumerable ||
            afterValue is not IEnumerable<string> afterEnumerable)
        {
            return false;
        }

        beforeList = beforeEnumerable.Select(value => value.Trim()).Where(value => value.Length > 0).ToList();
        afterList = afterEnumerable.Select(value => value.Trim()).Where(value => value.Length > 0).ToList();
        return true;
    }

    private static bool IsEnumerableOfObjects(
        object? beforeValue,
        object? afterValue,
        out IReadOnlyList<object> beforeObjects,
        out IReadOnlyList<object> afterObjects)
    {
        beforeObjects = [];
        afterObjects = [];

        if (beforeValue is string || afterValue is string ||
            beforeValue is not IEnumerable beforeEnumerable ||
            afterValue is not IEnumerable afterEnumerable)
        {
            return false;
        }

        beforeObjects = beforeEnumerable.Cast<object>().ToList();
        afterObjects = afterEnumerable.Cast<object>().ToList();
        return true;
    }

    private static bool IsSimpleValue(Type type)
    {
        var targetType = Nullable.GetUnderlyingType(type) ?? type;

        return targetType.IsPrimitive ||
            targetType.IsEnum ||
            targetType == typeof(string) ||
            targetType == typeof(decimal) ||
            targetType == typeof(DateTime) ||
            targetType == typeof(DateOnly) ||
            targetType == typeof(TimeOnly) ||
            targetType == typeof(Guid);
    }

    private static string FriendlyLabel(string propertyName)
    {
        var labels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Alt"] = "texto alternativo",
            ["BasePrice"] = "tarifa base",
            ["Caption"] = "leyenda",
            ["Category"] = "categoria",
            ["CollaboratorsCount"] = "cantidad de colaboradores",
            ["CollaboratorsLabel"] = "etiqueta de colaboradores",
            ["CoordinatesDescription"] = "descripcion de coordenadas",
            ["CoordinatesTitle"] = "titulo de coordenadas",
            ["Description"] = "descripcion",
            ["DirectorBiography"] = "biografia del director",
            ["DirectorName"] = "nombre del director",
            ["DirectorTitle"] = "cargo del director",
            ["DirectionsItems"] = "indicaciones",
            ["Discount"] = "descuento",
            ["EndDate"] = "fecha final",
            ["ExperienceLabel"] = "etiqueta de experiencia",
            ["ExperienceYears"] = "anos de experiencia",
            ["GallerySubtext"] = "texto de galeria",
            ["GalleryTag"] = "etiqueta de galeria",
            ["GalleryTitle"] = "titulo de galeria",
            ["HeroEyebrow"] = "etiqueta principal",
            ["HeroSubtitle"] = "subtitulo principal",
            ["HeroTitle"] = "titulo principal",
            ["HighlightDescription"] = "descripcion destacada",
            ["HighlightTitle"] = "titulo destacado",
            ["HistoryDescription"] = "descripcion de historia",
            ["HistoryMilestones"] = "hitos de historia",
            ["HistoryTag"] = "etiqueta de historia",
            ["HistoryTimelineEndLabel"] = "etiqueta final de historia",
            ["HistoryTimelineStartYear"] = "ano inicial de historia",
            ["HistoryTitle"] = "titulo de historia",
            ["LocalTalentLabel"] = "etiqueta de talento local",
            ["LocalTalentPercentage"] = "porcentaje de talento local",
            ["MapButtonLabel"] = "texto del boton de mapa",
            ["Mission"] = "mision",
            ["MissionTitle"] = "titulo de mision",
            ["MvvTag"] = "etiqueta de mision vision valores",
            ["MvvTitle"] = "titulo de mision vision valores",
            ["Name"] = "nombre",
            ["PercentageChange"] = "porcentaje",
            ["PhilosophyDescription"] = "descripcion de filosofia",
            ["PhilosophyQuote"] = "frase de filosofia",
            ["PhilosophyTitle"] = "titulo de filosofia",
            ["PrimaryListItems"] = "lista principal",
            ["PrimaryListTitle"] = "titulo de lista principal",
            ["RoomNumber"] = "numero de habitacion",
            ["RoomStatusId"] = "estado de habitacion",
            ["RoomStatusName"] = "estado de habitacion",
            ["RoomTypeId"] = "tipo de habitacion",
            ["RoomTypeName"] = "tipo de habitacion",
            ["SecondaryListItems"] = "lista secundaria",
            ["SecondaryListTitle"] = "titulo de lista secundaria",
            ["SectionSubtext"] = "texto introductorio",
            ["SectionTag"] = "etiqueta de seccion",
            ["SectionTitle"] = "titulo de seccion",
            ["ServiceCards"] = "tarjeta de servicio",
            ["Src"] = "archivo de imagen",
            ["StartDate"] = "fecha inicial",
            ["TeamTag"] = "etiqueta de equipo",
            ["TeamTitle"] = "titulo de equipo",
            ["Title"] = "titulo",
            ["Values"] = "valores",
            ["ValuesTitle"] = "titulo de valores",
            ["Vision"] = "vision",
            ["VisionTitle"] = "titulo de vision"
        };

        if (labels.TryGetValue(propertyName, out var label))
        {
            return label;
        }

        return Regex.Replace(propertyName, "([a-z])([A-Z])", "$1 $2").ToLowerInvariant();
    }

    private static string FormatValue(object? value)
    {
        return value switch
        {
            null => string.Empty,
            string text => Truncate(text.Trim()),
            DateOnly date => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            DateTime dateTime => dateTime.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
            decimal number => number.ToString("0.##", CultureInfo.InvariantCulture),
            bool boolean => boolean ? "si" : "no",
            _ => Truncate(value.ToString()?.Trim() ?? string.Empty)
        };
    }

    private static string ChangedText(string label, string before, string after)
    {
        var oldValue = before.Trim();
        var newValue = after.Trim();

        if (string.Equals(oldValue, newValue, StringComparison.Ordinal))
        {
            return string.Empty;
        }

        if (string.IsNullOrWhiteSpace(oldValue))
        {
            return $"Agrego {label}: '{Truncate(newValue)}'";
        }

        if (string.IsNullOrWhiteSpace(newValue))
        {
            return $"Elimino {label}: '{Truncate(oldValue)}'";
        }

        var prefixLength = 0;
        while (
            prefixLength < oldValue.Length &&
            prefixLength < newValue.Length &&
            oldValue[prefixLength] == newValue[prefixLength])
        {
            prefixLength++;
        }

        var suffixLength = 0;
        while (
            suffixLength < oldValue.Length - prefixLength &&
            suffixLength < newValue.Length - prefixLength &&
            oldValue[oldValue.Length - 1 - suffixLength] == newValue[newValue.Length - 1 - suffixLength])
        {
            suffixLength++;
        }

        var removedText = oldValue.Substring(prefixLength, oldValue.Length - prefixLength - suffixLength).Trim();
        var addedText = newValue.Substring(prefixLength, newValue.Length - prefixLength - suffixLength).Trim();

        if (!string.IsNullOrWhiteSpace(addedText) && string.IsNullOrWhiteSpace(removedText))
        {
            return $"Agrego texto en {label}: '{Truncate(addedText)}'";
        }

        if (!string.IsNullOrWhiteSpace(removedText) && string.IsNullOrWhiteSpace(addedText))
        {
            return $"Elimino texto de {label}: '{Truncate(removedText)}'";
        }

        if (!string.IsNullOrWhiteSpace(addedText) && !string.IsNullOrWhiteSpace(removedText))
        {
            return $"Cambio texto en {label}: elimino '{Truncate(removedText)}' y agrego '{Truncate(addedText)}'";
        }

        return $"Cambio {label}: '{Truncate(oldValue)}' -> '{Truncate(newValue)}'";
    }

    private static string Truncate(string value)
    {
        return value.Length <= MaxValueLength
            ? value
            : $"{value[..MaxValueLength]}...";
    }

    private static string LowerFirst(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? value
            : char.ToLowerInvariant(value[0]) + value[1..];
    }
}
