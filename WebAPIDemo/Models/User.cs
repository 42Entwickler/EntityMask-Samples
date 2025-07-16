using _42Entwickler.EntityMask;
using System.ComponentModel.DataAnnotations;
using WebAPIDemo.ModelConverter;

namespace WebAPIDemo.Models;

[EntityMask("Api")]
public class User {
    public int Id { get; set; }

    // Adding attributes in the mask definition for this property.
    [AttributeInMask("Api", ChangeType.Add, typeof(RequiredAttribute))]
    [AttributeInMask("Api", ChangeType.Add, typeof(MaxLengthAttribute), 100)]
    [AttributeInMask("Api", ChangeType.Add, typeof(MinLengthAttribute), 5)]
    public string Name { get; set; }

    // Hide this attribute in the Api mask.
    [Mask("Api")]
    public string PasswordHash { get; set; }

    public string Email { get; set; }

    // Rename the property in the Api mask to state and use a custom value converter to return a string in the mask.
    [RenameInMask("State", "Api")]
    [ConvertInMask(typeof(ApiStateConverter), "Api")]
    public bool IsActive { get; set; }
}
