namespace EduchemLPR.Models;

public class EmailUserRegisterModel {
    public EmailUserRegisterModel(string authKey, string webLink) {
        AuthKey = authKey;
        WebLink = webLink;
    }

    public string AuthKey { get; set; }
    public string WebLink { get; set; }
}