using System.Collections.Generic;
using WebAPIDemo.Models;

namespace WebAPIDemo.Repositories;

public class Repository {
    private static readonly User user1 = new() { Id = 1, Email = "42 entwickler - at - gmail .com", Name = "Dev Dave", PasswordHash = "SECRET_HASH_YOU_WON'T_KNOW_AND_IT'S_SALTED_TOO" };
    private static readonly User user2 = new() { Id = 2, Email = "max - at - muster.at", Name = "Max Muster", PasswordHash = "YET_ANOTHER_SECRET_HASH_YOU_WON'T_KNOW" };
    private static readonly User user3 = new() { Id = 3, Email = "susi- at - mueller.de", Name = "Susi Müller", PasswordHash = "HASH_IS_HASH" };
    private static readonly User user4 = new() { Id = 4, Email = "schroedinger - at - rheinwerk-verlag.de", Name = "Schrödinger", PasswordHash = "Schrödinger Programmiert C#" };
    private static readonly Dictionary<int, User> _users = new() {
        { 1, user1 },
        { 2, user2 },
        { 3, user3 },
        { 4, user4 }
    };
    private static readonly Dictionary<int, Project> _projects = new()
    {
        { 1, new Project { Id = 1, Title = "Demo Project", Description = "A sample project for demonstration.", Owner = user1, Start = DateTime.UtcNow, PlanedEnd = DateTime.UtcNow.AddMonths(1), Users = [user2, user3] } },
        { 2, new Project { Id = 2, Title = "API Redesign", Description = "Avoid non using properties in classes. Just mask them for the apis.", Owner = user2, Start = DateTime.UtcNow, PlanedEnd = DateTime.UtcNow.AddMonths(1), Users = [user1, user4]  } },
        { 3, new Project { Id = 3, Title = "High Performance Measurement", Description = "We'll need to proove the performance of this implementation.",Owner = user3, Start = DateTime.UtcNow, PlanedEnd = DateTime.UtcNow.AddMonths(1), Users = [user2, user1, user4]  } }
    };

    public IEnumerable<Project> GetAll() {
        return _projects.Values;
    }

    public Project? GetById(int id) {
        _projects.TryGetValue(id, out var project);
        return project;
    }

    public void Add(Project project) {
        var nextId = _projects.Count == 0 ? 1 : _projects.Keys.Max() + 1;
        project.Id = nextId;
        _projects.Add(project.Id, project);
    }

    public bool Update(Project project) {
        if (!_projects.ContainsKey(project.Id))
            return false;
        _projects[project.Id] = project;
        return true;
    }

    public bool Delete(int id) {
        return _projects.Remove(id);
    }

    public IEnumerable<User> GetUsers() => _users.Values;
    public User? GetUser(int id) => _users.TryGetValue(id, out var user) ? user : null;
}
