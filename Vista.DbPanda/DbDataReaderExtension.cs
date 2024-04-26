using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;

namespace Vista.DbPanda;

/// <summary>
/// 參考引用自：[DbDataReaderMapper](https://github.com/LucaMozzo/DbDataReaderMapper/tree/master)
/// </summary>
public static class DbDataReaderExtension
{
    /// <summary>
    /// Maps the current row to the specified type
    /// </summary>
    /// <typeparam name="T">The type of the output object</typeparam>
    /// <param name="dataReader">The data source</param>
    /// <param name="customPropertyConverter">Use a custom converter for certain values</param>
    /// <returns>The object that contains the data in the current row of the reader</returns>
    public static T MapToObject<T>(this DbDataReader dataReader, CustomPropertyConverter customPropertyConverter = null) where T : class
    {
        T obj = Activator.CreateInstance<T>();
        PropertyInfo[] typeProperties = typeof(T).GetProperties();
        var customNameMappings = typeProperties
            .Where(tp => GetColumnAttribute(tp) != null)
            .ToDictionary(tp => GetColumnAttribute(tp), tp => tp);

        for (int i = 0; i < dataReader.FieldCount; ++i)
        {
            string columnName = dataReader.GetName(i);

            var mappedProperty = typeProperties.Where(tp => tp.Name.Equals(columnName)).FirstOrDefault();
            var mappedPropertyCustomName = customNameMappings.ContainsKey(columnName) ? customNameMappings[columnName] : null;

            if (IsAttributePropertyNamingClash(customNameMappings, columnName, mappedProperty, mappedPropertyCustomName))
            {
                /*
                 * If the attribute has the same name as another property in the model that doesn't have a custom name, it causes a clash
                 */
                throw new DbColumnMappingException($"Attribute {columnName} has the same name as a property defined in the model");
            }

            // the attribute name takes precedence over the property name
            var resolvedMappedProperty = mappedPropertyCustomName ?? mappedProperty;

            if (resolvedMappedProperty != null)
            {
                var value = dataReader[columnName];
                if (value is DBNull)
                {
                    value = null;
                }

                try
                {
                    if (customPropertyConverter != null && customPropertyConverter[resolvedMappedProperty] != null)
                    {
                        resolvedMappedProperty.SetValue(obj, customPropertyConverter[resolvedMappedProperty].DynamicInvoke(value));
                    }
                    else
                    {
                        resolvedMappedProperty.SetValue(obj, value);
                    }
                }
                catch
                {
                    throw new InvalidCastException($"Expected type {resolvedMappedProperty.PropertyType} but found {value.GetType()} for property {columnName}");
                }
            }
        }

        return obj;
    }

    /// <summary>
    /// Determines whether the attribute custom name clashes with a property
    /// </summary>
    /// <remarks>
    /// A clash happens in a scenario similar to this
    /// `[DbColumn("Name")]`
    /// `public string Address { get; set; }`
    /// `public string Name { get; set; }`
    /// because column `Name` Can map either to the first property or to the second one
    /// </remarks>
    /// <param name="customNameMappings">The dictionary of custom attribute names -> model property mappings</param>
    /// <param name="columnName">The column name from the database</param>
    /// <param name="mappedProperty">The mapped property from the model definition</param>
    /// <param name="mappedPropertyCustomName">The mapped property from the attributes</param>
    /// <returns>True if there is a clash between the attribute and a property</returns>
    private static bool IsAttributePropertyNamingClash(Dictionary<string, PropertyInfo> customNameMappings,
        string columnName, PropertyInfo mappedProperty, PropertyInfo mappedPropertyCustomName)
        => mappedProperty != null && mappedPropertyCustomName != null && !customNameMappings.Values.Any(tp => tp.Name.Equals(columnName));

    /// <summary>
    /// Gets the custom name attribute from the property
    /// </summary>
    /// <param name="property">The property in the model</param>
    /// <returns>The custom name if it's specified, null otherwise</returns>
    private static string GetColumnAttribute(PropertyInfo property)
    {
        var attributes = property.GetCustomAttributes(true);
        var customName = attributes
            .Select(attr => attr as DbColumnAttribute)
            .Where(attr => attr != null)
            .Select(attr => attr.Name)
            .FirstOrDefault();

        return customName;
    }
}

//-----------------------------------------------------------------------------

public class DbColumnMappingException : Exception
{
    public DbColumnMappingException(string message) : base(message) { }
}

//-----------------------------------------------------------------------------

[AttributeUsage(AttributeTargets.Property)]
public class DbColumnAttribute : Attribute
{
    public string Name { get; private set; }

    /// <summary>
    /// Maps to a column in the result set with the given name
    /// </summary>
    /// <param name="name">The name of the column to map to</param>
    public DbColumnAttribute(string name)
    {
        Name = name;
    }
}

//-----------------------------------------------------------------------------

public class CustomPropertyConverter
{
    private Dictionary<PropertyInfo, Delegate> _conversionFunctions;

    public CustomPropertyConverter()
    {
        _conversionFunctions = new Dictionary<PropertyInfo, Delegate>();
    }

    /// <summary>
    /// Add a custom conversion for serializing values to the DAO
    /// This can be used, for instance, to serialize a string into an Enum value
    /// </summary>
    /// <remarks>
    /// DbNull values are converted to null
    /// </remarks>
    /// <typeparam name="T">The DAO type</typeparam>
    /// <typeparam name="U">The database type</typeparam>
    /// <typeparam name="V">The mapped output type</typeparam>
    /// <param name="property">The output property that needs conversion</param>
    /// <param name="conversionFunction">The conversion function that converts the database output type to the DAO property type</param>
    public CustomPropertyConverter AddConversion<T, U, V>(Expression<Func<T, V>> property, Func<U, V> conversionFunction)
    {
        var memberInfo = ((MemberExpression)property.Body).Member;
        if (memberInfo.MemberType == MemberTypes.Property)
        {
            _conversionFunctions.Add((PropertyInfo)memberInfo, conversionFunction);
        }
        else
        {
            throw new ArgumentException("The property selector should reference a property in the DAO");
        }

        return this;
    }

    internal Delegate this[PropertyInfo key]
    {
        get => _conversionFunctions.ContainsKey(key) ? _conversionFunctions[key] : null;
    }
}

//-----------------------------------------------------------------------------
