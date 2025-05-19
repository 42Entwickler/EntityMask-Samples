using _42Entwickler.EntityMask;

namespace WebAPIDemo.Models;

/// <summary>
/// Adding the Api Mask Name here at the project and true to the deep-masking-parameter.
/// So we also get the ApiUser-Mask for the Users in the Project. For the Owner and also for the Users-Collection.
/// </summary>
[EntityMask("Api", true)]
[EntityMask("ApiList", true)]
public class Project {
    public int Id { get; set; }
    public string? Title { get; set; }
    [Mask("ApiList")]
    public string? Description { get; set; }
    public DateTime Start { get; set; }
    [RenameInMask("End", "ApiList")]
    public DateTime PlanedEnd { get; set; }
    [Mask("ApiList")]
    public User? Owner { get; set; }
    [Mask("ApiList")]
    public List<User>? Users { get; set; }
}
