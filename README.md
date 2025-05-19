# EntityMask
This is a demo project that represents a showcase for using 42Entwickler.EntityMask sourcegenerator for more comfort dealing with data transfer objects.

EntityMask is a powerful, lightweight framework for creating strongly typed view projections of your domain entities through code generation. It allows you to define "masks" that expose only selected properties of your entities, transform values, and support deep object mapping without runtime reflection.

## Packages

| Package | Description | NuGet |
|---------|-------------|-------|
| `42Entwickler.EntityMask` | Core library with attributes and runtime components | [![NuGet](https://img.shields.io/nuget/v/42Entwickler.EntityMask.svg)](https://www.nuget.org/packages/42Entwickler.EntityMask/) |
| `42Entwickler.EntityMask.Generator` | Source generator that creates mask classes at compile time | [![NuGet](https://img.shields.io/nuget/v/42Entwickler.EntityMask.Generator.svg)](https://www.nuget.org/packages/42Entwickler.EntityMask.Generator/) |

## Getting Started

### Installation

```bash
# Install the core library
dotnet add package 42Entwickler.EntityMask

# Install the source generator
dotnet add package 42Entwickler.EntityMask.Generator
```

### Basic Usage

1. **Define your entity model**
2. **Add mask attributes** to specify how properties should be exposed
3. **Use the generated mask classes** to create projections

```csharp
using _42Entwickler.EntityMask;

// 1. Define entity with mask attributes
[EntityMask("api")]  // Create an API mask
public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; }
    
    // This property will be hidden in the API mask
    [Mask("api")]
    public string InternalNotes { get; set; }
    
    // This collection will be deeply mapped if Customer is accessed through 
    // a mask with deep mapping enabled
    public List<Order> Orders { get; set; }
}

// 2. Use the generated mask
public class CustomerService
{
    public CustomerApiMask GetCustomerForApi(int id)
    {
        Customer customer = _repository.GetCustomer(id);        
        // Return by implicit conversion overload
        return customer;

        // Or by direct conversion to mask using extension method
        // return customer.ToApiMask();
        
        // Or using the generic factory
        // return EntityMask.CreateMask<Customer, CustomerApiMask>(customer, "api");
    }
}
```

## Features

### Multiple Mask Types

Define multiple different mask types for each entity to support different views:

```csharp
[EntityMask("api")]      // Public API mask
[EntityMask("admin")]    // Admin view mask
[EntityMask("internal")] // Internal system mask
public class Product
{
    public int Id { get; set; }
    
    public string Name { get; set; }
    
    [Mask("api")]        // Hide in API mask
    public decimal Cost { get; set; }
    
    [Mask("api", "admin")] // Hide in both API and admin masks
    public string SupplierNotes { get; set; }
}
```

### Property Renaming

Rename properties when exposed through a mask to align with different naming conventions:

```csharp
public class Project
{
    public int Id { get; set; }
    
    // Rename property in API mask
    [RenameInMask("Name", "api")]
    public string Title { get; set; }
    
    // Combine renaming with conversion
    [RenameInMask("StartDate", "api")]
    [ConvertInMask(typeof(DateTimeToStringConverter), "api")]
    public DateTime Start { get; set; }
    
    // Rename in all masks (default)
    [RenameInMask("EndDate")]
    public DateTime PlanedEnd { get; set; }
}

// Generated mask will have:
// - Title as Name
// - Start as StartDate (with conversion to string)
// - PlanedEnd as EndDate
```

### Value Transformers

Transform property values when exposed through a mask:

```csharp
public class User
{
    // Transform phone number in the API mask
    [TransformInMask("FormatPhoneNumber", "api")]
    public string PhoneNumber { get; set; }
    
    // Static transformer method - must be public and static
    public static string FormatPhoneNumber(string phone)
    {
        if (string.IsNullOrEmpty(phone) || phone.Length != 10)
            return phone;
            
        return $"{phone.Substring(0, 3)}-{phone.Substring(3, 3)}-{phone.Substring(6)}";
    }
}
```

### Value Converters

Perform bidirectional conversion of property values with custom converters:

```csharp
public class Customer
{
    // Use a custom converter for date formatting
    [ConvertInMask(typeof(DateTimeToStringConverter))]
    public DateTime BirthDate { get; set; }
}

// Converter must implement IValueConverter<TEntity, TMask>
public class DateTimeToStringConverter : IValueConverter<DateTime, string>
{
    public string ConvertToMask(DateTime value)
    {
        return value.ToString("yyyy-MM-dd");
    }
    
    public DateTime ConvertToEntity(string value)
    {
        return DateTime.Parse(value);
    }
}
```

### Deep Mapping

Enable deep mapping to automatically convert nested objects and collections:

```csharp
// Enable deep mapping for nested objects
[EntityMask("api", EnableDeepMapping = true)]
public class Order
{
    public int Id { get; set; }
    public DateTime OrderDate { get; set; }
    
    // This will be automatically mapped to CustomerApiMask if Customer has an API mask
    public Customer Customer { get; set; }
    
    // Collection properties are also automatically mapped
    public List<OrderItem> Items { get; set; }
}
```

### Custom Mask Class Names

Specify custom class names for your mask types:

```csharp
[EntityMask("api", ClassName = "ProductDto")]
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
}

// Usage
var productDto = product.ToProductDto();  // Instead of .ToApiMask()
```

## Advanced Features

### Entity Updates with ApplyChangesTo and UpdateEntityFrom

When working with Entity Framework or other ORM systems, you'll often need to update existing entities with data from mask objects while preserving information not exposed in the mask:

```csharp
// In an ASP.NET Core controller
[HttpPut]
public ActionResult<ProjectApiMask> Put([FromBody] ProjectApiMask projectMask)
{
    if (projectMask == null)
        return BadRequest();
        
    // Get the existing entity from the database
    var entity = repository.GetById(projectMask.Id);
    if (entity == null)
        return NotFound();
    
    // Option 1: Apply changes from mask perspective
    projectMask.ApplyChangesTo(entity);
    
    // Option 2: Update entity from mask (fluent API)
    // entity.UpdateEntityFrom(projectMask);
    
    // Save changes to database
    repository.Update(entity);
    
    // Return the updated entity as mask
    return Ok((ProjectApiMask)entity); // Cast is required to maintain type safety
}
```

Both `ApplyChangesTo` and `UpdateEntityFrom` methods:
- Only update properties exposed in the mask
- Preserve all other entity properties not accessible through the mask
- Handle complex types including transformations and conversions
- Support deep mapped collections

These methods prevent the common pitfall of accidentally overwriting unexposed properties when using direct entity assignment.

### Collection Handling

EntityMask efficiently handles collections through lazy proxy collections that create mask instances on-demand:

```csharp
// Scenario 1: Repository returns an IEnumerable of entity objects
IEnumerable<Customer> customers = _repository.GetAllCustomers();

// Convert to mask collection with lazy evaluation (O(1) operation, masks created only when accessed)
IEnumerable<CustomerApiMask> customerMasks = customers.ToApiMask();

// When you iterate this collection, entities are converted one-by-one at access time
foreach (var mask in customerMasks)
{
    // CustomerApiMask created here when accessed
    Console.WriteLine(mask.Name);
}

// Scenario 2: Using with arrays
Customer[] customersArray = _repository.GetCustomersArray();
IReadOnlyList<CustomerApiMask> masksArray = customersArray.ToApiMask();

// Scenario 3: Using with lists
List<Customer> customersList = _repository.GetCustomersList();
IList<CustomerApiMask> masksList = customersList.ToApiMask();

// Scenario 4: Direct assignment (implicit conversion)
// This works for various collection types due to implicit operators
IEnumerable<Customer> source = GetCustomers();
IEnumerable<CustomerApiMask> apiMasks = source;  // Implicit conversion
```

### Bidirectional Mapping

Mask classes support bidirectional mapping, allowing you to modify the underlying entity through the mask:

```csharp
// Get entity with mask
var customerMask = customer.ToApiMask();

// Modify through mask
customerMask.Name = "New Name";

// Changes are applied to the original entity
Console.WriteLine(customer.Name);  // Outputs: New Name

// Implicit conversions
Customer originalEntity = customerMask;  // Get the original entity
```

### Custom Collection Converters

Implement custom collection converters for specialized collection mapping:

```csharp
public class CustomCollectionConverter<T> : ICollectionConverter<T, CustomMask<T>>
{
    public IEnumerable<CustomMask<T>> ConvertToMask(IEnumerable<T> collection)
    {
        // Custom conversion logic
        return collection.Select(item => new CustomMask<T>(item));
    }
    
    public IEnumerable<T> ConvertToEntity(IEnumerable<CustomMask<T>> collection)
    {
        // Custom conversion back
        return collection.Select(mask => mask.GetEntity());
    }
}
```

## Performance Considerations

* EntityMask uses **compile-time code generation** for optimal performance
* No runtime reflection is used when accessing properties
* Collection proxies use **lazy loading** to defer mask creation until needed
* All conversions happen through **strongly typed code**
* Collection conversions are **O(1) operations** using lazy proxying

## Source Generator Analyzer

The EntityMask source generator includes analyzers that help you avoid common mistakes:

### Transformer Method Validation (EM001)

Ensures transformer methods are correctly defined:

```csharp
// ERROR: Missing or invalid transformer method
[TransformInMask("FormatAddress")]  // Method doesn't exist or has wrong signature
public string Address { get; set; }

// FIXED:
public static string FormatAddress(string address)
{
    // Implementation
}
```

### Converter Type Validation (EM002)

Ensures converter types implement the correct interface:

```csharp
// ERROR: Invalid converter type
[ConvertInMask(typeof(InvalidConverter))]  // Doesn't implement IValueConverter<DateTime, string>
public DateTime BirthDate { get; set; }

// FIXED:
public class DateConverter : IValueConverter<DateTime, string>
{
    public string ConvertToMask(DateTime value) => value.ToString("yyyy-MM-dd");
    public DateTime ConvertToEntity(string value) => DateTime.Parse(value);
}
```

### Deep Mapping Warning (EM003)

Warns when deep mapping is enabled but not needed:

```csharp
// WARNING: No collection properties to deeply map
[EntityMask("api", EnableDeepMapping = true)]
public class SimpleEntity  // No collection properties
{
    public string Name { get; set; }
}
```

### Masked Classes Require To Be In A Namespace (EM004)

You need to ensure that a class that is using the `EntityMask` attribute is in a namespace.
Either encapsulated or in a file scoped namespace. Using the global default namespace from top level statements is not allowed.
```csharp
// ERROR: Class must be in a namespace
[EntityMask("api")]
class MyClass { ... }

// FIXED:
namespace MyNamespace
{
    [EntityMask("api")]
    public class MyClass { ... }
}
```

### Property Renaming Validation (EM005)

Ensures property names follow C# naming conventions:

```csharp
// WARNING: Renamed property doesn't follow C# conventions
[RenameInMask("lowercaseName")]  // Should use PascalCase for C# properties
public string OriginalName { get; set; }

// FIXED:
[RenameInMask("UpperCaseName")]  // PascalCase follows C# conventions
public string OriginalName { get; set; }
```

### Missing Mask Cast in Controller (EM006)

Detects when an entity is used where a mask is expected in ASP.NET Core controllers:

```csharp
// ERROR: Return value is of type 'Project' but method returns 'ProjectApiMask'
[HttpGet]
public ActionResult<ProjectApiMask> Get(int id)
{
    var project = repository.GetById(id);
    return Ok(project); // Missing cast to ProjectApiMask
}

// FIXED:
[HttpGet]
public ActionResult<ProjectApiMask> Get(int id)
{
    var project = repository.GetById(id);
    return Ok((ProjectApiMask)project); // Explicit cast
    
    // OR
    
    return Ok(project.ToApiMask()); // Using extension method
}
```

This analyzer helps prevent subtle runtime serialization errors by ensuring proper type conversions.

## Examples

Complete example with multiple features:

```csharp
[EntityMask("api")]
[EntityMask("admin", EnableDeepMapping = true)]
public class Customer
{
    public int Id { get; set; }
    
    public string Name { get; set; }
    
    [RenameInMask("DateOfBirth", "api")]
    [ConvertInMask(typeof(DateTimeToStringConverter))]
    public DateTime BirthDate { get; set; }
    
    [TransformInMask("FormatPhoneNumber", "api")]
    public string PhoneNumber { get; set; }
    
    [Mask("api")]
    public decimal CreditLimit { get; set; }
    
    [RenameInMask("PurchaseHistory", "admin")]
    public List<Order> Orders { get; set; }
    
    public static string FormatPhoneNumber(string phone)
    {
        if (string.IsNullOrEmpty(phone) || phone.Length != 10)
            return phone;
            
        return $"{phone.Substring(0, 3)}-{phone.Substring(3, 3)}-{phone.Substring(6)}";
    }
}

public class DateTimeToStringConverter : IValueConverter<DateTime, string>
{
    public string ConvertToMask(DateTime value) => value.ToString("yyyy-MM-dd");
    public DateTime ConvertToEntity(string value) => DateTime.Parse(value);
}

// Usage in an ASP.NET Core Controller
[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerRepository _repository;
    
    // GET api/customers
    [HttpGet]
    public ActionResult<IEnumerable<CustomerApiMask>> GetAll()
    {
        var customers = _repository.GetAll();
        
        // Lazy collection conversion - O(1) operation
        return Ok(customers.ToApiMask());
    }
    
    // GET api/customers/{id}
    [HttpGet("{id}")]
    public ActionResult<CustomerApiMask> GetById(int id)
    {
        var customer = _repository.GetById(id);
        if (customer == null)
            return NotFound();
            
        // Explicit conversion to ensure proper type
        return Ok((CustomerApiMask)customer);
    }
    
    // PUT api/customers/{id}
    [HttpPut("{id}")]
    public ActionResult<CustomerApiMask> Update(int id, [FromBody] CustomerApiMask customerMask)
    {
        if (customerMask == null || customerMask.Id != id)
            return BadRequest();
            
        var customer = _repository.GetById(id);
        if (customer == null)
            return NotFound();
        
        // Update entity from mask (preserves non-masked properties)
        customerMask.ApplyChangesTo(customer);
        
        _repository.Update(customer);
        
        // Return updated entity as mask
        return Ok(customer.ToApiMask());
    }
}
```

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Support & Donations

If you find EntityMask helpful in your projects, consider showing your appreciation with a small donation. Your support helps maintain and improve this library.

[![Donate with PayPal](https://img.shields.io/badge/Donate-PayPal-blue.svg)](https://paypal.me/42Entwickler)

If you want to become a professional supporter and need an invoice - no problem.

Your contributions, whether through code, ideas, bug reports, or financial support, are greatly valued and help keep this project active. While donations are absolutely optional, they provide a wonderful way to say "thanks" and encourage continued development.

Feel free to reach out with questions, feedback, or feature requests at 42entwickler - at - gmail.com.
