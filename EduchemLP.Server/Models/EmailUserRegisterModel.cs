namespace EduchemLP.Server.Models;

public class EmailUserRegisterModel(string passwordNonEncrypted, string webLink, string email) {
    public string PasswordNonEncrypted { get; set; } = passwordNonEncrypted;
    public string WebLink { get; set; } = webLink;
    public string Email { get; set; } = email;
}