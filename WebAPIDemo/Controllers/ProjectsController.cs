using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebAPIDemo.Repositories;
using WebAPIDemo.Models;

namespace WebAPIDemo.Controllers;
[Route("api/[controller]")]
[ApiController]
public class ProjectsController(Repository repo) : ControllerBase {
    [HttpGet()]
    public ActionResult<IEnumerable<ProjectApiListMask>> Get() {
        IEnumerable<ProjectApiListMask> projects = repo.GetAll().ToApiListMask();
        return Ok(projects);
    }

    [HttpGet()]
    [Route("{id:int}")]
    public ActionResult<ProjectApiMask> Get(int id) {
        var project = repo.GetById(id);
        if (project == null) {
            return NotFound();
        }
        return Ok(project);
    }

    [HttpPut()]
    public ActionResult<ProjectApiMask> Put([FromBody] ProjectApiMask project) {
        if (project == null) {
            return BadRequest();
        }
        // Simulate a common Entity-Framework-Upadate-Method: First get the id and then assign....
        var proj = repo.GetById(project.Id);
        if (proj == null)
            return NotFound();

        // 2 Ways to do this:
        project.ApplyChangesTo(proj); // Assign all overwritten items by the api. Only Masked properties will be written. Others not!
                                      // other way the fluent API Style - what do you prefer?
                                      //proj.UpdateEntityFrom(project); // Assign all overwritten items by the api. Only Masked properties will be written. Others not!

        if (repo.Update(proj)) {
            // To avoid exposing the entity directly, we return the mask instead of the entity by casting it
            // Althoug the return type of the ActionResult is already the mask asp.net would return the entity - :/
            // The code analyzer gives you a warning when forgetting the cast.
            // You can also use the extension method ToApiMask() to convert the entity to the mask.
            return Ok((ProjectApiMask)proj);
        }
        return NotFound();
    }
}
