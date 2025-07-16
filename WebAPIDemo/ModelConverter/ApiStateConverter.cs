using _42Entwickler.EntityMask;

namespace WebAPIDemo.ModelConverter;

public class ApiStateConverter : IValueConverter<bool, string> {
    public bool ConvertToEntity(string value) => value == "active";


    public string ConvertToMask(bool value) => value ? "active" : "inactive";
    
}
