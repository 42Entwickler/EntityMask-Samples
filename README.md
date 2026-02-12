# EntityMask

EntityMask is a powerful, lightweight framework for creating strongly typed view projections of your domain entities through code generation. It allows you to define "masks" that expose only selected properties of your entities, transform values, support deep object mapping, and provides fine-grained control over attribute inheritance - all without runtime reflection.

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
    public DateTime Start { get; set; }
    
    // Rename in all masks (default)
    [RenameInMask("EndDate", "api")]
    public DateTime PlanedEnd { get; set; }
}

// Generated mask will have:
// - Title as Name
// - Start as StartDate (with conversion to string)
// - PlanedEnd as EndDate
```

**Note:** All mask features (renaming, attribute control, deep mapping, etc.) work seamlessly with inherited properties.

### Hide all Properties except Whitelisted

The `MaskAllExceptAttribute` allows you to specify that **all properties except the listed ones** should be hidden in a given mask. This is useful for quickly exposing only a whitelist of properties for a mask, without having to mark every property individually.

#### Usage Example

```csharp
public class CustomerBase {
    public Guid SomeId { get; set; } // Hidden in api mask
}

[EntityMask("api")]
[MaskAllExcept("api", nameof(Id), nameof(Name))]
public class Customer : CustomerBase: {
    public int Id { get; set; }
    public string Name { get; set; }
    public string InternalNotes { get; set; } // Hidden in api mask
}
```

**Result:**
- The generated `CustomerApiMask` will only contain `Id` and `Name` properties.
- All other properties (`InternalNotes`, `SomeId`, etc.) are excluded from the mask.

#### How it works
- Place `[MaskAllExcept("maskName", ...propertyNames)]` on your entity class.
- Only the listed properties will be visible in the generated mask for that mask name.
- All other properties are automatically hidden for that mask.

#### Generated Object by this example

```csharp
// Generated CustomerApiMask:
public class CustomerApiMask {
    public int Id { get; set; }
    public string Name { get; set; }
    // InternalNotes and SomeId are not present
}
```

### Value Transformers

Transform property values when exposed through a mask:

```csharp
[EntityMask("api")]]
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
[EntityMask]
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

### Attribute Control

EntityMask provides powerful attribute control capabilities, allowing you to precisely manage which attributes from your entity properties are copied to the generated mask classes. This is essential for creating clean APIs, proper JSON serialization, and context-specific validation.

#### Understanding Attribute Inheritance

**By default, EntityMask copies ALL attributes** from entity properties to mask properties. This ensures that serialization, validation, and display attributes work correctly without additional configuration:

```csharp
[EntityMask("api")]
public class User
{
    [Key]                               // Database attribute
    [JsonPropertyName("user_id")]       // JSON serialization
    [Required]                          // Validation
    [Display(Name = "User ID")]         // UI display
    public int Id { get; set; }
}

// Generated UserApiMask will have ALL attributes:
public class UserApiMask
{
    [Key]
    [JsonPropertyName("user_id")]
    [Required]
    [Display(Name = "User ID")]
    public int Id { get; set; }
}
```

#### The AttributeInMask Attribute

Use `AttributeInMaskAttribute` for fine-grained control over attribute inheritance:

```csharp
[AttributeInMask(maskName, changeType, ...)]
```

**ChangeType Options:**
- `ChangeType.Hide` - Remove existing attributes
- `ChangeType.Add` - Add new attributes  
- `ChangeType.Set` - Replace attributes (atomic hide + add)
- `ChangeType.Include` - Override class-level hiding

#### Hiding Attributes

Remove unwanted attributes from mask properties:

```csharp
[EntityMask("api")]
[AttributeInMask("api", ChangeType.Hide, typeof(KeyAttribute), typeof(ColumnAttribute))]
public class Product
{
    [Key]                           // Hidden in API mask
    [Column("product_id")]          // Hidden in API mask  
    [JsonPropertyName("id")]        // Kept in API mask
    [Required]                      // Kept in API mask
    public int Id { get; set; }
}

// Generated ProductApiMask:
public class ProductApiMask
{
    [JsonPropertyName("id")]    // ✅ Database attributes removed
    [Required]                  // ✅ Business attributes preserved
    public int Id { get; set; }
}
```

##### Hide by Namespace

Hide entire namespaces of attributes:

```csharp
[EntityMask("clean")]
[AttributeInMask("clean", ChangeType.Hide, 
    AttributeNamespaces = new[] { "System.ComponentModel.DataAnnotations.Schema" })]
[Table("orders")]                   // Hidden (Schema)  
public class Order
{
    [Key]                           // Kept (DataAnnotations, not Schema)
    [Column("order_id")]            // Hidden (Schema)
    [Required]                      // Kept (DataAnnotations)
    [JsonPropertyName("id")]        // Kept (System.Text.Json)
    public int Id { get; set; }
}

// Clean mask removes all EF Core schema attributes
```

##### Hide All Attributes

Start with a clean slate:

```csharp
[EntityMask("minimal")]
[AttributeInMask("minimal", ChangeType.Hide)]  // Hide ALL attributes
public class Customer
{
    [JsonPropertyName("customer_id")]
    [Required]
    [Display(Name = "Customer ID")]
    public int Id { get; set; }
}

// Generated CustomerMinimalMask has NO attributes:
public class CustomerMinimalMask
{
    public int Id { get; set; }  // Clean property, no attributes
}
```

#### Adding New Attributes

Add mask-specific attributes that don't exist on the entity:

```csharp
[EntityMask("api")]
public class Employee
{
    // Entity has no JSON attributes, but API mask needs them
    [AttributeInMask("api", ChangeType.Add, typeof(JsonPropertyNameAttribute), "employee_id")]
    [AttributeInMask("api", ChangeType.Add, typeof(RequiredAttribute))]
    public int Id { get; set; }

    [AttributeInMask("api", ChangeType.Add, typeof(JsonPropertyNameAttribute), "full_name")]
    [AttributeInMask("api", ChangeType.Add, typeof(StringLengthAttribute), 100)]
    public string Name { get; set; }
}

// Generated EmployeeApiMask adds API-specific attributes:
public class EmployeeApiMask
{
    [JsonPropertyName("employee_id")]   // Added for API
    [Required]                          // Added for API validation
    public int Id { get; set; }

    [JsonPropertyName("full_name")]     // Added for API  
    [StringLength(100)]                 // Added for API validation
    public string Name { get; set; }
}
```

#### Adding Attributes with Named Properties

For complex attributes that use property initialization:

```csharp
[EntityMask("display")]
public class Article
{
    [AttributeInMask("display", ChangeType.Add, typeof(DisplayAttribute),
        PropertyNames = new[] { "Name", "Description", "Order" },
        PropertyValues = new object[] { "Article ID", "Unique identifier", 1 })]
    public int Id { get; set; }
}

// Generated ArticleDisplayMask:
public class ArticleDisplayMask
{
    [Display(Name = "Article ID", Description = "Unique identifier", Order = 1)]
    public int Id { get; set; }
}
```

#### Setting (Replacing) Attributes

Atomically replace existing attributes with new ones:

```csharp
[EntityMask("v2")]
public class Customer
{
    [JsonPropertyName("legacy_customer_id")]    // Old API format
    [StringLength(50)]                          // Old validation rule
    [Required]
    // Replace JSON name and validation in one operation
    [AttributeInMask("v2", ChangeType.Set, typeof(JsonPropertyNameAttribute), "id")]
    [AttributeInMask("v2", ChangeType.Set, typeof(StringLengthAttribute), 100)]
    public int Id { get; set; }
}

// Generated CustomerV2Mask:
public class CustomerV2Mask
{
    [JsonPropertyName("id")]        // ✅ Replaced (old: "legacy_customer_id")
    [StringLength(100)]             // ✅ Replaced (old: 50)
    [Required]                      // ✅ Preserved (not replaced)
    public int Id { get; set; }
}
```

#### Property-Level Overrides

Override class-level rules for specific properties:

```csharp
[EntityMask("selective")]
[AttributeInMask("selective", ChangeType.Hide)]  // Hide ALL attributes by default
public class Document
{
    [Key]
    [Column("doc_id")]
    [JsonPropertyName("id")]
    [Required]
    // Override: include only JSON attribute for this property
    [AttributeInMask("selective", ChangeType.Include, typeof(JsonPropertyNameAttribute))]
    public int Id { get; set; }

    [Column("title")]
    [JsonPropertyName("title")]
    [StringLength(200)]
    [Required]
    // Override: include ALL attributes for this property
    [AttributeInMask("selective", ChangeType.Include)]
    public string Title { get; set; }

    [Column("content")]
    [JsonPropertyName("content")]
    [StringLength(5000)]
    // No override - all attributes hidden due to class-level rule
    public string Content { get; set; }
}

// Generated DocumentSelectiveMask:
public class DocumentSelectiveMask
{
    [JsonPropertyName("id")]                                        // Only JSON name
    public int Id { get; set; }

    [Column("title")]                                               // All attributes
    [JsonPropertyName("title")]
    [StringLength(200)]
    [Required]
    public string Title { get; set; }

    public string Content { get; set; }                             // No attributes
}
```

#### Multi-Mask Attribute Control

Different attribute rules for different masks:

```csharp
[EntityMask("public")]
[EntityMask("internal")]
[AttributeInMask("public", ChangeType.Hide, typeof(KeyAttribute), typeof(ColumnAttribute))]  // Clean public API
public class Task
{
    [Key]
    [Column("task_id")]
    [Required]
    [AttributeInMask("public", ChangeType.Add, typeof(JsonPropertyNameAttribute), "id")]       // Public: clean name
    [AttributeInMask("internal", ChangeType.Add, typeof(JsonPropertyNameAttribute), "task_id")] // Internal: detailed name
    [AttributeInMask("internal", ChangeType.Set, typeof(ColumnAttribute), "taskId")]            // Internal: overwrite the value of the column attribute
    public int Id { get; set; }
}

// Generated TaskPublicMask:
public class TaskPublicMask
{
    [Required]                      // Database attributes hidden
    [JsonPropertyName("id")]        // Clean API name
    public int Id { get; set; }
}

// Generated TaskInternalMask:  
public class TaskInternalMask
{
    [Key]                           // All attributes preserved
    [Column("taskId")]              // Value is overwritten by the ChangeType.Set.
    [Required]
    [JsonPropertyName("task_id")]   // Detailed internal name
    public int Id { get; set; }
}
```

#### Clean REST APIs - Practical Use Cases

Transform database entities into clean API DTOs:

```csharp
[EntityMask("rest")]
[AttributeInMask("rest", ChangeType.Hide)]  // Start clean
public class User
{
    [Key]
    [Column("user_id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [AttributeInMask("rest", ChangeType.Add, typeof(JsonPropertyNameAttribute), "id")]
    [AttributeInMask("rest", ChangeType.Add, typeof(RequiredAttribute))]
    public int Id { get; set; }

    [Column("email_address")]
    [StringLength(255)]
    [EmailAddress]
    [AttributeInMask("rest", ChangeType.Add, typeof(JsonPropertyNameAttribute), "email")]
    [AttributeInMask("rest", ChangeType.Include, typeof(StringLengthAttribute), typeof(EmailAddressAttribute))]
    public string Email { get; set; }

    [Column("password_hash")]
    [StringLength(500)]
    // This property stays hidden (no Include override)
    public string PasswordHash { get; set; }
}

// Generated UserRestMask:
public class UserRestMask
{
    [JsonPropertyName("id")]
    [Required]
    public int Id { get; set; }

    [JsonPropertyName("email")]
    [StringLength(255)]
    [EmailAddress]
    public string Email { get; set; }

    // PasswordHash property doesn't exist in mask (hidden)
}
```

#### Framework Integration - Practical Use Cases

Optimize attributes for different serialization frameworks:

```csharp
[EntityMask("json")]   // System.Text.Json optimized
[EntityMask("xml")]    // XML serialization optimized

[AttributeInMask("json", ChangeType.Hide, AttributeNamespaces = new[] { "System.Xml.Serialization" })]
[AttributeInMask("xml", ChangeType.Hide, AttributeNamespaces = new[] { "System.Text.Json" })]
public class Product
{
    [JsonPropertyName("product_id")]        // For JSON
    [XmlElement("ProductID")]               // For XML
    [Required]
    public int Id { get; set; }
}

// JsonMask: only JSON attributes
// XmlMask: only XML attributes
// Both preserve Required validation
```

Attributes have sooo many possibilities ;-)

### Deep Mapping for Single Objects and Collections

Deep mapping automatically converts both individual nested objects and collections to their corresponding mask types:

```csharp
[EntityMask("Api", true)]  // EnableDeepMapping = true
public class Project
{
    public int Id { get; set; }
    public string Title { get; set; }
    
    // Single object deep mapping - will be converted to UserApiMask
    public User Owner { get; set; }
    
    // Collection deep mapping - will be converted to IEnumerable<UserApiMask>  
    public List<User> Users { get; set; }
}

[EntityMask("Api")]  // User also needs an Api mask for deep mapping to work
public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    
    [Mask("Api")]
    public string PasswordHash { get; set; }  // Hidden in API mask
}

// Generated ProjectApiMask will have:
public class ProjectApiMask
{
    public int Id { get; set; }
    public string Title { get; set; }
    
    // Deep mapped single object - lazy created when accessed
    public UserApiMask? Owner 
    { 
        get => _entity.Owner != null ? new UserApiMask(_entity.Owner) : null;
        set => _entity.Owner = value; // Implicit conversion
    }
    
    // Deep mapped collection - lazy proxy collection
    public IList<UserApiMask> Users { get; set; }
}

// Usage
var project = GetProject();
var projectMask = project.ToApiMask();

// Access deep mapped properties
var ownerMask = projectMask.Owner;        // UserApiMask (password hidden)
var userMasks = projectMask.Users;        // IList<UserApiMask> (passwords hidden)
```

**Important:** Both the parent entity (Project) and child entities (User) must have masks with the same name ("Api") for deep mapping to work.

### MaskAlias: Reuse Child Mask Types Across Different Parent Masks

Sometimes you want a parent entity to provide multiple masks (e.g., "Api" and "Api2"), but the child entity should only expose a single mask (e.g., only "Api"). With `MaskAliasAttribute` you can alias the deep-mapping for specific properties so different parent masks reuse the same child mask type.

Use it on properties that are deep-mapped (single objects or collections):

```csharp
[EntityMask("Api", true)]
[EntityMask("Api2", true)]
public class ProjectWithDeep
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;

    // For the parent mask "Api2", reuse the child mask mapping of "Api"
    [MaskAlias("Api2", "Api")]
    public UserForDeep Owner { get; set; } = new();

    // Works for collections as well
    [MaskAlias("Api2", "Api")]
    public List<UserForDeep> Users { get; set; } = new();
}

[EntityMask("Api")]  // Child exposes only Api mask
public class UserForDeep
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    [Mask("Api")] // Hidden in Api mask
    public string PasswordHash { get; set; } = string.Empty;
}

// Usage
var pro = new ProjectWithDeep();
var apiMask = pro.ToApiMask();
var api2Mask = pro.ToApi2Mask();

// Deep-mapped properties in both masks use UserForDeepApiMask
_ = (UserForDeepApiMask?)apiMask.Owner;
_ = (UserForDeepApiMask?)api2Mask.Owner;
_ = (List<UserForDeepApiMask>)apiMask.Users;
_ = (List<UserForDeepApiMask>)api2Mask.Users;
```

Notes:
- Destination mask is the first parameter of `MaskAlias(destinationMask, sourceMasks...)`.
- One or more source masks can be listed; the generator tries them in order.
- Works for single objects and for collections.
- If a direct mapping exists for the destination mask, it is used; otherwise the alias is applied.

This keeps your child entities simple (one mask) while still offering multiple parent views.

### Nullable Reference Types Support

EntityMask fully supports C# nullable reference types and nullable value types:

```csharp
[EntityMask("Api", true)]
public class Customer
{
    public int Id { get; set; }
    public string? Name { get; set; }           // Nullable string
    public DateTime? BirthDate { get; set; }    // Nullable DateTime
    
    // Nullable object with deep mapping
    public Address? PrimaryAddress { get; set; }
}

[EntityMask("Api")]
public class Address
{
    public string? Street { get; set; }
    public string? City { get; set; }
}

// Generated mask preserves nullability:
public class CustomerApiMask
{
    public string? Name { get; set; }                    // Nullable string preserved
    public DateTime? BirthDate { get; set; }             // Nullable DateTime preserved
    public AddressApiMask? PrimaryAddress { get; set; }  // Deep mapped nullable object
}

// Usage with null safety
CustomerApiMask mask = customer.ToApiMask();
if (mask.PrimaryAddress?.Street != null)
{
    Console.WriteLine($"Street: {mask.PrimaryAddress.Street}");
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

### Generated Extension Methods

For each mask, EntityMask generates extension methods following the pattern `To{MaskName}Mask()`:

```csharp
[EntityMask("Api")]      // Generates: ToApiMask()
[EntityMask("Admin")]    // Generates: ToAdminMask()  
[EntityMask]             // Generates: ToMask() (default mask)
[EntityMask("List", ClassName = "UserListDto")]  // Generates: ToUserListDto()
public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
}

// All generated extension methods:
public static class UserMaskExtensions
{
    // Single entity conversions
    public static UserApiMask ToApiMask(this User entity)
    public static UserAdminMask ToAdminMask(this User entity)
    public static UserMask ToMask(this User entity)
    public static UserListDto ToUserListDto(this User entity)
    
    // Collection conversions (lazy evaluation)
    public static IEnumerable<UserApiMask> ToApiMask(this IEnumerable<User> entities)
    public static IList<UserApiMask> ToApiMask(this IList<User> entities)
    public static IList<UserApiMask> ToApiMask(this List<User> entities)
    public static IReadOnlyList<UserApiMask> ToApiMask(this User[] entities)
    public static IReadOnlyCollection<UserApiMask> ToApiMask(this IReadOnlyCollection<User> entities)
    
    // Fluent API for entity updates
    public static User UpdateFrom(this User entity, UserApiMask mask)
}

// Usage examples:
User user = GetUser();
List<User> users = GetUsers();

// Single conversions
UserApiMask apiMask = user.ToApiMask();
UserAdminMask adminMask = user.ToAdminMask();

// Collection conversions (lazy - no immediate allocation)
IEnumerable<UserApiMask> apiMasks = users.ToApiMask();
IList<UserApiMask> apiMasksList = users.ToApiMask();

// Entity updates
user.UpdateFrom(apiMask);  // Updates user with values from mask
```

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
Customer originalEntity = customerMask;  // No object creation
```

<!--
TODO: NEED TO BE IMPLEMENTED
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
```-->

### Built-in Collection Support

EntityMask provides built-in support for common collection types through lazy proxy collections:

```csharp
[EntityMask("Api", EnableDeepMapping = true)]
public class Order
{
    public int Id { get; set; }
    
    // Supported collection types for deep mapping:
    public List<OrderItem> Items { get; set; }              // → IList<OrderItemApiMask>
    public OrderItem[] ItemsArray { get; set; }             // → IReadOnlyList<OrderItemApiMask>
    public ICollection<OrderItem> ItemsCollection { get; set; } // → IList<OrderItemApiMask>
    public IList<OrderItem> ItemsList { get; set; }         // → IList<OrderItemApiMask>
    public IReadOnlyCollection<OrderItem> ReadOnlyItems { get; set; } // → IReadOnlyCollection<OrderItemApiMask>
    public IEnumerable<OrderItem> EnumerableItems { get; set; } // → IEnumerable<OrderItemApiMask>
}

// Generated lazy proxy collections automatically handle:
// ✅ Lazy evaluation (O(1) conversion)
// ✅ On-demand mask creation
// ✅ Bidirectional conversion (setting collections back to entity)
// ✅ Type preservation (List → IList, Array → IReadOnlyList, etc.)
```

### Automated Entity Framework Projections
Generated masks include a static `Projection` and a `ProjectionSlim` property that provides an Entity Framework projection expression for selecting only the mask’s properties. 
Using this projection yields optimized SQL that selects just those columns, improving the efficiency of database queries. 
The projection currently does not support deep mapping or properties that use transformations or `ValueConverters`; such members are omitted from the expression. 
If a mask exposes no properties suitable for projection, no `Projection` property is generated.

```csharp
IEnumerable<UserApiMask> userDTOs = _dbContext.Users
    .Where(u => u.IsActive)
    .Select(UserApiMask.ProjectionSlim); // Selects only properties defined in UserApiMask
```

There are two projection properties:
- `Projection` - also projects single navigation properties with the given properties of the mask of the projection property. If you use this `Projection` property you need to include the referencing navigation properties. Therefore a you can use the generated `RequiredIncludes` property.
- `ProjectionSlim` - only includes the naive properties without any navigation properties. But consider if you should create an additional mask without the navigation property mapping. It depends on your usecase!

```csharp
IEnumerable<UserApiMask> userDTOs = _dbContext.Users
    .Include(UserApiMask.RequiredIncludes)
    .Where(u => u.IsActive)
    .Select(UserApiMask.Projection); // Selects only properties defined in UserApiMask but also joins any included / masked navigation properties.
```

### Collection Extension Methods

EntityMask generates extension methods for manual collection conversion:

```csharp
List<User> users = GetUsers();

// All supported collection conversions:
IEnumerable<UserApiMask> enumerable = users.ToApiMask();        // Lazy
IList<UserApiMask> list = users.ToApiMask();                    // Lazy proxy
IReadOnlyList<UserApiMask> readOnlyList = users.ToArray().ToApiMask(); // Lazy proxy
IReadOnlyCollection<UserApiMask> readOnly = users.AsReadOnly().ToApiMask(); // Lazy proxy

// Performance: All conversions are O(1) operations using lazy proxies
// Masks are created only when individual items are accessed
foreach (var mask in enumerable.Take(5)) // Only creates 5 masks, not all
{
    Console.WriteLine(mask.Name);
}
```

**Note:** Custom collection converters are not currently supported, but the built-in lazy proxy system handles most use cases efficiently.

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

### Analyzer: EntityMaskAttribute on Abstract Classes or Interfaces (EM007)

EntityMask does **not** support mask generation for abstract classes or interfaces. The analyzer will report an error (EM007) if you apply `[EntityMask]` to an abstract class or interface:

```csharp
// ERROR: EntityMaskAttribute cannot be applied to abstract classes or interfaces
[EntityMask("api")]
public abstract class AbstractEntity { ... }

[EntityMask("api")]
public interface IEntity { ... }
```

**Fix:** Only apply `[EntityMask]` to concrete (non-abstract) classes:

```csharp
[EntityMask("api")]
public class ConcreteEntity { ... }
```

**Diagnostic ID:** EM007

**Message:** EntityMaskAttribute cannot be applied to abstract classes or interfaces (type: 'YourTypeName')

### Analyzer: MaskAllExceptAttribute Property Validation (EM008)

The analyzer checks that all property names specified in `MaskAllExceptAttribute` exist in the class or one of its base classes. If a property name does not exist, an error (EM008) is reported.

```csharp
// ERROR: Property does not exist in class or base class
[EntityMask("api")]
[MaskAllExcept("api", nameof(Id), "NonExistentProperty")]
public class Customer : CustomerBase {
    public int Id { get; set; }
    public string Name { get; set; }
}
```

**Fix:** Only specify property names that exist in the class or its inheritance hierarchy:

```csharp
[EntityMask("api")]
[MaskAllExcept("api", nameof(Id), nameof(Name))]
public class Customer : CustomerBase {
    public int Id { get; set; }
    public string Name { get; set; }
}
```

**Diagnostic ID:** EM008

**Message:** Property '{PropertyName}' specified in MaskAllExceptAttribute does not exist in class or base classes.

---

## Examples

Complete example with multiple features including attribute control:

```csharp
[EntityMask("api")]
[EntityMask("admin", EnableDeepMapping = true)]
// Clean up database attributes in API, but keep them in admin view
[AttributeInMask("api", ChangeType.Hide, typeof(KeyAttribute), typeof(ColumnAttribute))]

public class Customer
{
    [Key]
    [Column("customer_id")]
    [AttributeInMask("api", ChangeType.Add, typeof(JsonPropertyNameAttribute), "id")]
    public int Id { get; set; }
    
    [Column("customer_name")]
    [AttributeInMask("api", ChangeType.Add, typeof(JsonPropertyNameAttribute), "name")]
    public string Name { get; set; }
    
    [Column("birth_date")]
    [RenameInMask("DateOfBirth", "api")]
    [ConvertInMask(typeof(DateTimeToStringConverter))]
    [AttributeInMask("api", ChangeType.Add, typeof(JsonPropertyNameAttribute), "date_of_birth")]
    public DateTime BirthDate { get; set; }
    
    [Column("phone")]
    [TransformInMask("FormatPhoneNumber", "api")]
    [AttributeInMask("api", ChangeType.Add, typeof(JsonPropertyNameAttribute), "phone")]
    public string PhoneNumber { get; set; }
    
    [Column("credit_limit")]
    [Mask("api")]  // Hidden in API, visible in admin
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

The generated API mask will have clean JSON attributes without database-specific attributes:

```json
// CustomerApiMask JSON output:
{
  "id": 123,
  "name": "John Doe",
  "date_of_birth": "1990-05-15",
  "phone": "555-123-4567"
}

// Notice: clean property names, formatted phone, ISO date, no credit_limit
```

## Performance Optimizations

### Direct Property Access (No Caching Overhead)

EntityMask uses a direct property access pattern for optimal performance:

```csharp
// Generated deep mapped property uses direct creation pattern
public UserApiMask? Owner
{
    get => _entity.Owner != null ? new UserApiMask(_entity.Owner) : null;
    set => _entity.Owner = value; // Implicit conversion
}
```

**Benefits:**
- ✅ **Always Current**: No stale cache data issues
- ✅ **Memory Efficient**: No additional backing fields
- ✅ **Simple**: Easy to understand and debug
- ✅ **Thread Safe**: No mutable state between threads

### Lazy Collection Proxies

Collection mappings use lazy proxy patterns for maximum efficiency:

```csharp
IEnumerable<User> users = GetThousandsOfUsers();
IEnumerable<UserApiMask> masks = users.ToApiMask();  // O(1) operation!

// Masks are created only when accessed
foreach (var mask in masks.Take(10))  // Only creates 10 masks, not thousands
{
    Console.WriteLine(mask.Name);
}
```

### Implicit Conversions for Zero-Copy Operations

```csharp
// Zero-copy access to underlying entity
User originalUser = userMask;  // No object creation

// Direct entity updates through mask
userMask.Name = "New Name";    // Directly updates _entity.Name
```

### Inheritance Support

EntityMask fully supports inheritance for your entity models. When you use EntityMask on a derived class, the generated mask will automatically include all properties (and mask-relevant attributes) from the entire inheritance hierarchy, except those explicitly hidden or excluded. This makes it easy to work with transfer objects that inherit from base classes, ensuring all relevant data is available in the mask.

#### How it works
- All public and protected properties from base classes are included in the mask.
- Mask attributes (e.g. `[Mask]`, `[RenameInMask]`, etc.) and attribute control rules from base classes are respected.
- Hidden properties (via `[Mask]`) or excluded attributes are not included in the mask.
- The mask is always a flattened view of the full object tree, ideal for DTOs and transfer objects.

#### Example

```csharp
public class TenantDependendBase
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } // Hidden in mask
}

[EntityMask("pub")]
public class Board : TenantDependendBase
{
    public string Name { get; set; }
    [Mask("pub")] // Hide Tenant property in mask
    public new Tenant Tenant { get; set; }
}

// Generated BoardPubMask will have:
public class BoardPubMask
{
    public Guid Id { get; set; }           // From base class
    public Guid TenantId { get; set; }     // From base class
    public string Name { get; set; }       // From Board
    // No Tenant property (hidden)
}

// Usage
var board = new Board { Id = Guid.NewGuid(), Name = "Test Board", TenantId = tenant.Id };
BoardPubMask mask = board.ToPubMask();
Console.WriteLine(mask.Id);        // Outputs base class property
Console.WriteLine(mask.Name);      // Outputs derived class property
Console.WriteLine(mask.TenantId);  // Outputs base class property
```

## Performance Characteristics

| Operation | Time Complexity | Memory Impact |
|-----------|----------------|---------------|
| Single mask creation | O(1) | 1 object allocation |
| Collection conversion | O(1) | Lazy proxy only |
| Property access | O(1) | No additional allocations |
| Deep mapped access | O(1) | Creates mask on demand |
| Entity updates | O(1) | Direct property assignment |
| Attribute processing | O(1) | Compile-time generation |

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Support & Donations

If you find EntityMask helpful in your projects, consider showing your appreciation with a small donation. Your support helps maintain and improve this library.

[![Donate with PayPal](https://img.shields.io/badge/Donate-PayPal-blue.svg)](https://paypal.me/42Entwickler)

If you want to become a professional supporter and need an invoice - no problem.

Your contributions, whether through code, ideas, bug reports, or financial support, are greatly valued and help keep this project active. While donations are absolutely optional, they provide a wonderful way to say "thanks" and encourage continued development.

Feel free to reach out with questions, feedback, or feature requests at 42entwickler - at - gmail.com.



---
