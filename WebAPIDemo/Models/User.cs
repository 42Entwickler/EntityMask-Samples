using _42Entwickler.EntityMask;

namespace WebAPIDemo.Models;

[EntityMask("Api")]
public class User {
    public int Id { get; set; }
    public string Name { get; set; }
    [Mask("Api")]
    public string PasswordHash { get; set; }
    public string Email { get; set; }
}
