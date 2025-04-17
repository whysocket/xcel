using Xcel.Services.Email.Interfaces;

namespace Xcel.Services.Email.Templates;

public record TutorRequestDocumentsEmail(
    string FullName
) : IEmail
{
    public string Subject => "You're almost there! Upload your ID and DBS documents";
}