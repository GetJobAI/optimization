namespace GetJobAI.Optimisation.Data.Models;

public class ResumeContent
{
    public ContactInfo? Contact { get; set; }
    public string? Summary { get; set; }
    public List<Experience> Experience { get; set; } = [];
    public List<Education> Education { get; set; } = [];
    public List<SkillGroup> Skills { get; set; } = [];
    public List<Certification> Certifications { get; set; } = [];
    public List<Language> Languages { get; set; } = [];
    public List<Project> Projects { get; set; } = [];
}

public class ContactInfo
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Location { get; set; }
    public string? Linkedin { get; set; }
    public string? Github { get; set; }
}

public class Experience
{
    public string Company { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Dates { get; set; }
    public List<string> Bullets { get; set; } = [];
    public bool Hide { get; set; }
}

public class Education
{
    public string? Institution { get; set; }
    public string? Degree { get; set; }
    public string? Dates { get; set; }
    public string? Location { get; set; }
    public string? Grade { get; set; }
    public bool Hide { get; set; }
}

public class SkillGroup
{
    public string Category { get; set; } = string.Empty;
    public List<string> Items { get; set; } = [];
}

public class Certification
{
    public string? Name { get; set; }
    public string? Issuer { get; set; }
    public string? Date { get; set; }
}

public class Language
{
    public string? Name { get; set; }
    public string? Level { get; set; }
}

public class Project
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Url { get; set; }
}
